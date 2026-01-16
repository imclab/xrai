using UnityEngine;


public class CheckPackageVersions : MonoBehaviour {
    void Awake() {
        #if !AR_SUBSYSTEMS_4_0_1_OR_NEWER && !AR_FOUNDATION_5_0_OR_NEWER
        Debug.LogError("Please install AR Foundation >= 4.0.2 to enable Body Tracking.");
        #endif
    }
}
