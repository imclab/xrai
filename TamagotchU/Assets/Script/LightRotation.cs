using UnityEngine;

public class LightRotation : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 30f;

    private float currentRotation = 0f;

    void Update()
    {
        // Increase rotation over time
        currentRotation += rotationSpeed * Time.deltaTime;

        // Wrap angle between 0 and 360
        if (currentRotation >= 360f || currentRotation <= 0f)
            rotationSpeed *= -1;


        // Apply rotation (only on Y axis, keep original X and Z rotation)
        transform.localEulerAngles += new Vector3(rotationSpeed * Time.deltaTime, 0, 0);
    }
}
