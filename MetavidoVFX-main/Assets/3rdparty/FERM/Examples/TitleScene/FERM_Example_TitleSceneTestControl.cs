using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FERM_Example_TitleSceneTestControl : MonoBehaviour {

    private static FERM_Example_TitleSceneTestControl singleton;

    private void Start() {
        if(singleton)
            Destroy(this.gameObject);

        singleton = this;
        DontDestroyOnLoad(this.gameObject);

        ApplySettings(0);
    }

    private void ApplySettings(int preset) {
        switch(preset) {
        case 0:
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            break;
        case 1:
            Time.timeScale = 1f/6f;
            Application.targetFrameRate = 10;
            QualitySettings.vSyncCount = 0;
            break;
        }
    }

    void Update () {

        //load scene based on key input
        for(int i = 0; i < 9; i++) {
            if(Input.GetKeyDown((KeyCode)(48 + i)))
                SceneManager.LoadScene(i);
        }

        //change settings preset
        for(int i = 0; i < 9; i++) {
            if(Input.GetKeyDown((KeyCode)(256 + i)))
                ApplySettings(i);
        }
    }
}
