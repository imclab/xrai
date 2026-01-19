using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

public class DebugManager : MonoBehaviour
{
    [SerializeField] private GameObject m_NetworkPanel;

    [SerializeField] private GameObject m_DebugPanel;

    [SerializeField] private FingerSyncManager m_FingerSyncManager;

    [SerializeField] private Hand m_LeftHand;

    public UnityEvent<Handedness, Hand> OnUpdatedHand;

    private void Start()
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
        {
            Destroy(m_DebugPanel);
            Destroy(gameObject);
            return;
        }

        m_LeftHand.gameObject.SetActive(true);
        m_LeftHand.transform.position = new Vector3(0f, 0f, 1f);
    }

    private void Update()
    {
        if (!m_NetworkPanel.activeSelf)
        {
            m_NetworkPanel.SetActive(true);
            m_NetworkPanel.transform.position = new Vector3(0f, 0f, 0.5f);
        }

        OnUpdatedHand?.Invoke(Handedness.Left, m_LeftHand);
    }

    public void StartFingerSync()
    {
        m_FingerSyncManager.StartFingerSync(Handedness.Right);
    }
}
