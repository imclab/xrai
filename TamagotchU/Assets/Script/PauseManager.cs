using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;

    // Reference to your free camera script (e.g., SamplesFreeCamera)
    public MonoBehaviour freeCameraScript;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Press Q to toggle pause
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void PauseGame()
    {
        Time.timeScale = 0f;     // Pause the game simulation
        isPaused = true;

        if (freeCameraScript != null)
            freeCameraScript.enabled = true;  // Make sure camera stays active

        // Optional: lock cursor for mouse look while paused
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;     // Resume simulation
        isPaused = false;

        if (freeCameraScript != null)
            freeCameraScript.enabled = true;

        // Lock cursor again when unpausing
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
