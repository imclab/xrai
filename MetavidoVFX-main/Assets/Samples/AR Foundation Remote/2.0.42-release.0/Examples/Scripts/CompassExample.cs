#if UNITY_EDITOR && AR_FOUNDATION_REMOTE_INSTALLED
using Input = ARFoundationRemote.Input;
#endif
using UnityEngine;
// ReSharper disable NotAccessedField.Local


namespace ARFoundationRemoteExamples {
    public class CompassExample : MonoBehaviour {
        [SerializeField] bool compassEnabled = true;
        [SerializeField] double timestamp;
        [SerializeField] float headingAccuracy;
        [SerializeField] float magneticHeading;
        [SerializeField] Vector3 rawVector;
        [SerializeField] float trueHeading;


        void Awake() {
            Input.compass.enabled = compassEnabled;
        }

        void Update() {
            var compass = Input.compass;
            compassEnabled = compass.enabled;
            timestamp = compass.timestamp;
            headingAccuracy = compass.headingAccuracy;
            magneticHeading = compass.magneticHeading;
            rawVector = compass.rawVector;
            trueHeading = compass.trueHeading;
        }
    }
}
