using UnityEngine;
namespace Artngame.CommonTools
{
    [CreateAssetMenu(fileName = "NewInfiniNoteData", menuName = "SceneInfiniNOTE/Note Data", order = 1)]
    public class InfiniNoteDataSO : ScriptableObject
    {
        [TextArea(5, 20)]
        public string noteText;

        public int fontSize = 14;
        public bool richText = false;
    }
}