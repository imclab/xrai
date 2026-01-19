using UnityEngine;
using UnityEngine.XR.Hands;

public class NetworkPanelToggler : MonoBehaviour
{
    [SerializeField] private GameObject m_NetworkPanel;

    private void Start()
    {
        m_NetworkPanel.SetActive(false);
    }

    public void OnHandGestureChanged(Handedness handedness, HandGesture _, HandGesture newGesture)
    {
        if (handedness == Handedness.Left)
            m_NetworkPanel.SetActive(newGesture == HandGesture.FacingSelf);
    }
}
