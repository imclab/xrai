using UnityEngine;
using UnityEngine.XR.Hands;

public class FingerSyncTrigger : MonoBehaviour
{
    [SerializeField] private FingerSyncManager m_FingerSyncManager;

    private bool m_IsSyncing = false;

    private bool m_IsFisting = false;

    private float m_FistingTime;

    private Handedness m_Handedness;

    private const float FIST_THRESHOLD = 1f;

    public void OnHandGestureChanged(Handedness handedness, HandGesture oldGesture, HandGesture newGesture)
    {
        if (newGesture == HandGesture.Fisting) // Start finger sync
        {
            m_Handedness = handedness;
            m_IsFisting = true;
            m_FistingTime = Time.time;
            return;
        }

        if (oldGesture == HandGesture.Fisting) // Stop finger sync
        {
            if (m_IsSyncing)
                StopFingerSync();
            return;
        }
    }

    private void Update()
    {
        if (!m_IsSyncing && m_IsFisting)
        {
            if (Time.time - m_FistingTime > FIST_THRESHOLD)
                StartFingerSync();
        }
    }

    private void StartFingerSync()
    {
        m_IsSyncing = true;
        m_FingerSyncManager.StartFingerSync(m_Handedness);
    }

    private void StopFingerSync()
    {
        m_IsFisting = false;
        m_IsSyncing = false;
        m_FingerSyncManager.StopFingerSync();
    }
}
