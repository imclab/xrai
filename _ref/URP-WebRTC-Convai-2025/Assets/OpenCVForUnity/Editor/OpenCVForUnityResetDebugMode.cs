#if UNITY_EDITOR
using OpenCVForUnity.UnityUtils;
using UnityEditor;
using UnityEngine;

namespace OpenCVForUnity.Editor
{
    public class OpenCVForUnityResetDebugMode : MonoBehaviour
    {

        [InitializeOnEnterPlayMode]
        static void InitializeOnEnterPlayMode()
        {

            Utils.setDebugMode(false);

        }
    }
}
#endif