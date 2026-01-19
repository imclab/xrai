#if UNITY_EDITOR
using OpenCVForUnity.UnityUtils;
using UnityEditor;
using UnityEngine;

namespace OpenCVForUnity.Editor
{
    public class OpenCVForUnityResetDebugMatUtils : MonoBehaviour
    {

        [InitializeOnEnterPlayMode]
        static void InitializeOnEnterPlayMode()
        {

            DebugMatUtils.clear();

        }
    }
}
#endif