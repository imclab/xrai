using System.Collections.Generic;
using UnityEngine;
using Graffity.HandGesture.Attributes;
using Graffity.HandGesture.Conditions;


namespace Graffity.HandGesture
{


    /// <summary>
    /// Asset of conditions representing a single gesture
    /// </summary>
    [CreateAssetMenu(fileName = "NewGesture", menuName = "Graffity/HandGesture/GestureAsset")]
    public class GestureAsset : ScriptableObject
    {


        [field: SerializeField, Tooltip("Gesture Description"), TextArea]
        public string Description { get; private set; } = default;


        [field: SerializeReference, Tooltip("List of gesture conditions"), ConditionSelector]
        public List<IConditionAsset> ConditionAssetList { get; private set; } = new();


    }


}