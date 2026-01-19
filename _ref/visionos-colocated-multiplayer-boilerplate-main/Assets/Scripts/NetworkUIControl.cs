using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class NetworkUIControl : MonoBehaviour
{
    [SerializeField] private Button m_StartHostButton;

    [SerializeField] private Button m_StartClientButton;

    [SerializeField] private Button m_ShutdownButton;

    [SerializeField] private TMP_Text m_NetworkStatusText;

    private void Start()
    {
        m_StartHostButton.gameObject.SetActive(true);
        m_StartClientButton.gameObject.SetActive(true);
        m_ShutdownButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            m_NetworkStatusText.text = "Status: Not Connected";

            if (!m_StartHostButton.gameObject.activeSelf && !m_StartClientButton.gameObject.activeSelf && m_ShutdownButton.gameObject.activeSelf)
            {
                m_StartHostButton.gameObject.SetActive(true);
                m_StartClientButton.gameObject.SetActive(true);
                m_ShutdownButton.gameObject.SetActive(false);
            }
        }
        else
        {
            if (NetworkManager.Singleton.IsHost)
            {
                m_NetworkStatusText.text = $"Status: Host({NetworkManager.Singleton.ConnectedClients.Count})";
            }
            else
            {
                m_NetworkStatusText.text = $"Status: Joined({NetworkManager.Singleton.LocalClientId})";
            }

            if (m_StartHostButton.gameObject.activeSelf && m_StartClientButton.gameObject.activeSelf && !m_ShutdownButton.gameObject.activeSelf)
            {
                m_StartHostButton.gameObject.SetActive(false);
                m_StartClientButton.gameObject.SetActive(false);
                m_ShutdownButton.gameObject.SetActive(true);
            }
        }
    }
}
