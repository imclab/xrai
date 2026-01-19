using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkHandJoint : NetworkBehaviour
{
    private NetworkVariable<Vector3> m_HandJointPosition = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<Quaternion> m_HandJointRotation = new(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Transform m_HandJoint;

    private Transform m_WorldRoot;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            m_HandJoint = FindObjectOfType<HandJointSynchronizer>().HandJoint;  
        }

        m_HandJointPosition.OnValueChanged += OnHandJointPositionChanged;
        m_HandJointRotation.OnValueChanged += OnHandJointRotationChanged;

        GetComponent<MeshRenderer>().material.color = OwnerClientId == 0 ? Color.red : Color.blue;
        m_WorldRoot = GameObject.Find("World Root").transform;
    }

    private void FixedUpdate()
    {
        if (IsSpawned && IsOwner)
        {
            Vector3 relativePosition = m_WorldRoot.InverseTransformPoint(m_HandJoint.position);
            Quaternion relativeRotation = Quaternion.Inverse(m_WorldRoot.rotation) * m_HandJoint.rotation;

            m_HandJointPosition.Value = relativePosition;
            m_HandJointRotation.Value = relativeRotation;

            transform.SetPositionAndRotation(m_HandJoint.position, m_HandJoint.rotation);
        }
    }

    private void OnHandJointPositionChanged(Vector3 previousValue, Vector3 newValue)
    {
        if (!IsOwner)
        {
            transform.position = m_WorldRoot.TransformPoint(newValue);
        }
    }

    private void OnHandJointRotationChanged(Quaternion previousValue, Quaternion newValue)
    {
        if (!IsOwner)
        {
            transform.rotation = m_WorldRoot.rotation * newValue;
        }   
    }
}
