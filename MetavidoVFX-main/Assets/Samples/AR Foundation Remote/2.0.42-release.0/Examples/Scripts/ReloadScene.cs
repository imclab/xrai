using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;


namespace ARFoundationRemoteExamples {
    public class ReloadScene : MonoBehaviour {
        void OnGUI() {
            var w = 400;
            if (GUI.Button(new Rect(Screen.width - w, 0, w, 200), "Reload")) {
                reload();
            }
        }

        static void reload() {
            var xrManagerSettings = XRGeneralSettings.Instance.Manager;
            xrManagerSettings.DeinitializeLoader();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // reload current scene
            xrManagerSettings.InitializeLoaderSync();
        }
    }
}
