using UnityEngine;
using Unity.Netcode;

public class HandJointSynchronizer : NetworkBehaviour
{
    [SerializeField] private GameObject m_NetworkHandJointPrefab;

    [SerializeField] private Transform m_HandJoint;

    public Transform HandJoint => m_HandJoint;

    public override void OnNetworkSpawn()
    {
        SpawnNetworkHandJointServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnNetworkHandJointServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var networkHandJoint = Instantiate(m_NetworkHandJointPrefab);
        networkHandJoint.GetComponent<NetworkObject>().SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
    }
}
