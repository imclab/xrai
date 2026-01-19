using UnityEngine;
using UnityEngine.XR.Hands;

public class HandGestureLogger : MonoBehaviour
{
    public void OnHandGestureChanged(Handedness handedness, HandGesture oldGesture, HandGesture newGesture)
    {
        Debug.Log($"OnHandGestureChanged: {handedness}, {newGesture}");
    }
}
