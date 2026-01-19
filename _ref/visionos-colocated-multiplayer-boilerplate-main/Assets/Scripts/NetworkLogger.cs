using UnityEngine;
using Unity.Netcode;

public class NetworkLogger : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"OnClientConnected with client ID {clientId}");
    }
}
