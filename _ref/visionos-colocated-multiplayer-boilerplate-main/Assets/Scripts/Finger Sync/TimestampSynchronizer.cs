using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.Events;

public class TimestampSynchronizer : NetworkBehaviour
{
    public bool HasSynced => m_HasSynced;

    public float ResultTimestampOffset => m_ResultTimestampOffset;

    private bool m_IsSyncing = false;

    private bool m_HasSynced = false;

    private readonly Queue<float> m_TimestampOffsetQueue = new();

    private float m_ResultTimestampOffset;

    [Header("Client Side Calibration Parameters")]
    [Tooltip("The minimum number of elements in the timestamp offset queue to start calculating the standard deviation of the queue.")]
    [SerializeField] private int m_TimestampOffsetQueueStablizationCount = 10;

    [Tooltip("The maximum acceptable standard deviation of the timestamp offset queue.")]
    [SerializeField] private float m_TimestampOffsetQueueStandardDeviationThreshold = 0.1f;

    public UnityEvent<float> OnTimestampOffsetSynced;

    public override void OnNetworkSpawn()
    {
        if (!IsHost)
            m_IsSyncing = true;
    }

    private void FixedUpdate()
    {
        if (m_IsSyncing)
            OnRequestTimestampServerRpc(Time.time);
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnRequestTimestampServerRpc(float clientTimestamp, ServerRpcParams serverRpcParams = default)
    {
        float hostTimestamp = Time.time;
        ClientRpcParams clientRpcParams = new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
            }
        };
        OnRespondTimestampClientRpc(hostTimestamp, clientTimestamp, clientRpcParams);
    }

    [ClientRpc]
    private void OnRespondTimestampClientRpc(float hostTimestamp, float oldClientTimestamp, ClientRpcParams clientRpcParams = default)
    {
        if (!m_IsSyncing)
            return;

        float newClientTimestamp = Time.time;
        float timestampOffset = hostTimestamp + (newClientTimestamp - oldClientTimestamp) / 2f - newClientTimestamp;
        m_TimestampOffsetQueue.Enqueue(timestampOffset);
        if (m_TimestampOffsetQueue.Count > m_TimestampOffsetQueueStablizationCount)
        {
            float stdDev = Utils.CalculateStdDev(m_TimestampOffsetQueue);
            if (stdDev < m_TimestampOffsetQueueStandardDeviationThreshold)
            {
                m_ResultTimestampOffset = m_TimestampOffsetQueue.Average();
                m_IsSyncing = false;
                m_HasSynced = true;
                m_TimestampOffsetQueue.Clear();

                OnTimestampOffsetSynced?.Invoke(m_ResultTimestampOffset);
                Debug.Log($"On Timestamp synced: {m_ResultTimestampOffset}");
            }
            else
            {
                _ = m_TimestampOffsetQueue.Dequeue();
            }
        }
    }
}
