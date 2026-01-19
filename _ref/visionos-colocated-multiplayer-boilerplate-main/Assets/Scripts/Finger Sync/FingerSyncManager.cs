using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Hands;
using System.Linq;
using UnityEngine.Events;

public struct TimedHandJointPose
{
    public float Timestamp;
    public Pose HandJointPose;
}

public struct HandJointPosePair
{
    public Vector3 HostHandJointPosition;
    public Vector3 ClientHandJointPosition;
}

// Rotate the client's world root by the theta first, then translate
public struct SyncResult
{
    public float ThetaInDegree;
    public Vector3 Translate;
}

public class FingerSyncManager : NetworkBehaviour
{
    [SerializeField] private XRHandJointID m_SyncHandJoint = XRHandJointID.IndexTip;

    [SerializeField] private TimestampSynchronizer m_TimestampSynchronizer;

    private bool m_IsSyncing = false;

    private Handedness m_Handedness;

    private readonly Queue<TimedHandJointPose> m_TimedHandJointPoseQueue = new();

    private readonly Queue<HandJointPosePair> m_HandJointPosePairQueue = new();

    private readonly Queue<SyncResult> m_SyncResultQueue = new();

    [Header("Host Side Calibration Parameters")]
    [Tooltip("The maximum timestamp gap between the current time and a stored hand joint pose. Host does not store poses with timestamp earlier than this threshold.")]
    [SerializeField] private double m_HandJointPoseTimestampThreshold = 0.6;

    [Tooltip("The maximum acceptable timestamp deviation. We only process camera poses with timestamp deviation smaller than this threshold.")]
    [SerializeField] private double m_TimestampDeviationThreshold = 0.034;

    [Tooltip("The minimum number of elements in the hand joint pose pair queue to start calculating the standard deviation of the queue.")]
    [SerializeField] private int m_HandJointPosePairQueueStablizationCount = 50;

    [Tooltip("The minimum number of elements in the sync result queue to start calculating the standard deviation of the queue.")]
    [SerializeField] private int m_SyncResultQueueStablizationCount = 30;

    [Tooltip("The maximum acceptable standard deivation of the sync result's theta value in degrees.")]
    [SerializeField] private double m_ThetaStandardDeviationThreshold = 0.1;

    public UnityEvent<Vector3, Quaternion> OnFingerSyncCompleted;

    public void StartFingerSync(Handedness handedness)
    {
        if (!IsSpawned)
            return;

        if (!IsHost && !m_TimestampSynchronizer.HasSynced)
        {
            Debug.Log("Failed to start finger sync, timestamp not yet synchronized.");
            return;
        }
        
        if (m_IsSyncing)
            return;

        m_Handedness = handedness;
        m_IsSyncing = true;
        Debug.Log($"Finger sync started: {handedness}");
    }

    public void StopFingerSync()
    {
        if (!m_IsSyncing)
            return;

        OnStopSync();
        Debug.Log("Finger sync stopped");
    }

    public void OnUpdatedHand(Handedness handedness, Hand hand)
    {
        if (!m_IsSyncing || m_Handedness == handedness)
            return;

        if (IsHost)
            OnUpdatedHand_Host(handedness, hand);
        else
            OnUpdatedHand_Client(handedness, hand);
    }

    private void OnUpdatedHand_Host(Handedness handedness, Hand hand)
    {
        TimedHandJointPose timedHandJointPose = new()
        {
            Timestamp = Time.time,
            HandJointPose = hand.GetHandJointPose(m_SyncHandJoint)
        };
        m_TimedHandJointPoseQueue.Enqueue(timedHandJointPose);
        while (m_TimedHandJointPoseQueue.Count > 0 && Time.time - m_TimedHandJointPoseQueue.Peek().Timestamp > m_HandJointPoseTimestampThreshold)
            m_TimedHandJointPoseQueue.Dequeue();
    }

    private void OnUpdatedHand_Client(Handedness handedness, Hand hand)
    {
        var handJointPose = hand.GetHandJointPose(m_SyncHandJoint);
        OnRequestHandJointPoseServerRpc(Time.time + m_TimestampSynchronizer.ResultTimestampOffset, handJointPose.position);
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnRequestHandJointPoseServerRpc(float requestedTimestamp, Vector3 clientHandJointPosition, ServerRpcParams serverRpcParams = default)
    {
        if (m_TimedHandJointPoseQueue.Count <= 0)
            return;

        float minTimestampDeviation = 99f;
        TimedHandJointPose nearestHandJointPose = new();
        foreach (var timedHandJointPose in m_TimedHandJointPoseQueue)
        {
            float timestampDeviation = Mathf.Abs(timedHandJointPose.Timestamp - requestedTimestamp);
            if (timestampDeviation < minTimestampDeviation)
            {
                minTimestampDeviation = timestampDeviation;
                nearestHandJointPose = timedHandJointPose;
            }
        }

        // We don't accept hand joint pose with timestamp not close enough to the requested timestamp
        if (minTimestampDeviation > m_TimestampDeviationThreshold)
            return;

        ClientRpcParams clientRpcParams = new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
            }
        };
        OnRespondHandJointPoseClientRpc(nearestHandJointPose.HandJointPose.position, clientHandJointPosition, clientRpcParams);
    }

    [ClientRpc]
    private void OnRespondHandJointPoseClientRpc(Vector3 hostHandJointPosition, Vector3 clientHandJointPosition, ClientRpcParams clientRpcParams = default)
    {
        if (!m_IsSyncing)
            return;

        m_HandJointPosePairQueue.Enqueue(new HandJointPosePair()
        {
            HostHandJointPosition = hostHandJointPosition,
            ClientHandJointPosition = clientHandJointPosition
        });
        // We wait until the queue reaches the minimal size
        if (m_HandJointPosePairQueue.Count > m_HandJointPosePairQueueStablizationCount)
        {
            var clientSyncResult = CalculateClientSyncResult();
            m_SyncResultQueue.Enqueue(clientSyncResult);
            if (m_SyncResultQueue.Count > m_SyncResultQueueStablizationCount)
            {
                var thetaStdDev = Utils.CalculateStdDev(m_SyncResultQueue.Select(x => x.ThetaInDegree));
                Debug.Log($"thetaStdDev: {thetaStdDev}");
                if (thetaStdDev < m_ThetaStandardDeviationThreshold)
                {
                    SyncResult syncResult = m_SyncResultQueue.Last();
                    OnFingerSyncCompleted?.Invoke(syncResult.Translate, Quaternion.AngleAxis(syncResult.ThetaInDegree, Vector3.up));
                    Debug.Log($"On Finger Sync succeeded with translate: {syncResult.Translate}, theta: {syncResult.ThetaInDegree}");

                    OnStopSync();
                    return;
                }
                else
                {
                    _ = m_SyncResultQueue.Dequeue();
                }
            }
            _ = m_HandJointPosePairQueue.Dequeue();
        }
    }

    private SyncResult CalculateClientSyncResult()
    {
        Vector3 clientHandJointPositionCentroid = new(
            m_HandJointPosePairQueue.Select(o => o.ClientHandJointPosition.x).Average(),
            m_HandJointPosePairQueue.Select(o => o.ClientHandJointPosition.y).Average(),
            m_HandJointPosePairQueue.Select(o => o.ClientHandJointPosition.z).Average());

        Vector3 hostHandJointPositionCentroid = new(
            m_HandJointPosePairQueue.Select(o => o.HostHandJointPosition.x).Average(),
            m_HandJointPosePairQueue.Select(o => o.HostHandJointPosition.y).Average(),
            m_HandJointPosePairQueue.Select(o => o.HostHandJointPosition.z).Average());

        Vector2 tanThetaAB = m_HandJointPosePairQueue.Select(o =>
        {
            var p = o.HostHandJointPosition - hostHandJointPositionCentroid;
            var q = o.ClientHandJointPosition - clientHandJointPositionCentroid;

            var a = p.x * q.x + p.z * q.z;
            var b = -p.x * q.z + p.z * q.x;
            return new Vector2(a, b);
        }).Aggregate(Vector2.zero, (r, o) => r + o);

        float thetaInDegree = Mathf.Atan2(tanThetaAB.y, tanThetaAB.x) / Mathf.Deg2Rad;
        Matrix4x4 rotation = Matrix4x4.Rotate(Quaternion.AngleAxis(thetaInDegree, Vector3.up));
        Vector3 translate = -rotation.MultiplyPoint3x4(hostHandJointPositionCentroid - clientHandJointPositionCentroid);
        return new SyncResult()
        {
            ThetaInDegree = thetaInDegree,
            Translate = translate
        };
    }

    private void OnStopSync()
    {
        m_IsSyncing = false;
        m_TimedHandJointPoseQueue.Clear();
        m_HandJointPosePairQueue.Clear();
        m_SyncResultQueue.Clear();
    }
}
