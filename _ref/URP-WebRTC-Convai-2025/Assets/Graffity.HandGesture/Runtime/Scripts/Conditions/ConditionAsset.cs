using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Interface for setting gesture conditions
    /// </summary>
    public interface IConditionAsset : IConditionInstanceCreator
    {
#if UNITY_EDITOR
        void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label);
#endif
    }



    /// <summary>
    /// Base class for setting gesture conditions
    /// </summary>
    [Serializable]
    public abstract class ConditionAsset<TInstance> : IConditionAsset where TInstance : IConditionInstance, new()
    {


        /// <summary>
        /// Description of this condition (for ease of viewing on the inspector)
        /// </summary>
        [field: SerializeField]
        public string Detail { get; protected set; } = default;


        /// <summary>
        /// Create an instance that uses this asset to make updates
        /// </summary>
        IConditionInstance IConditionInstanceCreator.CreateInstance()
        {
            var instance = new TInstance();
            instance.Initialize(this);
            return instance;
        }


#if UNITY_EDITOR
        public virtual void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label){}
#endif


    }


}