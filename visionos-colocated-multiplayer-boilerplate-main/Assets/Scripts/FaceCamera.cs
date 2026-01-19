using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] private Transform m_MainCamera;

    private void Update()
    {
        transform.LookAt(m_MainCamera.position);

        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.x = 0f;
        eulerAngles.y += 180f;
        eulerAngles.z = 0f;
        transform.eulerAngles = eulerAngles;
    }
}
