using UnityEngine;
namespace Artngame.CommonTools
{
    public class SceneInfiniNOTE : MonoBehaviour
    {
        [TextArea(5, 20)]
        public string note = "Write your notes here...";

        [Range(10, 40)]
        public int fontSize = 24;

        public int panelSize = 200;

        public bool useRichText = false;

        // Scene view overlay toggle
        public bool showInSceneView = false;
    }
}