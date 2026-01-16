#if UNITY_EDITOR && AR_FOUNDATION_REMOTE_INSTALLED
using Input = ARFoundationRemote.Input;
#endif
using UnityEngine;
// ReSharper disable NotAccessedField.Local


namespace ARFoundationRemoteExamples {
    public class GyroscopeExample : MonoBehaviour {
        [SerializeField] bool gyroEnabled = true;
        [SerializeField] float updateInterval;
        [SerializeField] Quaternion attitude;
        [SerializeField] Vector3 gravity;
        [SerializeField] Vector3 rotationRate;
        [SerializeField] Vector3 userAcceleration;
        [SerializeField] Vector3 rotationRateUnbiased;
        [SerializeField] Vector3 acceleration;


        void Awake() {
            var gyro = Input.gyro;
            gyro.enabled = gyroEnabled;
            gyro.updateInterval = updateInterval;
        }

        void Update() {
            var gyro = Input.gyro;
            gyroEnabled = gyro.enabled;
            attitude = gyro.attitude;
            gravity = gyro.gravity;
            rotationRate = gyro.rotationRate;
            updateInterval = gyro.updateInterval;
            userAcceleration = gyro.userAcceleration;
            rotationRateUnbiased = gyro.rotationRateUnbiased;
            acceleration = Input.acceleration;
        }
    }
}
