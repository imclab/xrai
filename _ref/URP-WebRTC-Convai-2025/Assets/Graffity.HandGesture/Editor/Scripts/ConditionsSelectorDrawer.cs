using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Graffity.HandGesture.Attributes;
using Graffity.HandGesture.Conditions;


namespace Graffity.HandGesture.Editor
{


    /// <summary>
    /// Editor extension that allows classes inheriting from IConditionAsset to be set from dropdown
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionSelectorAttribute))]
    public class ConditionsSelectorDrawer : PropertyDrawer
    {


        ConditionSelectorAttribute _attribute { get { return (ConditionSelectorAttribute)attribute; } }
        bool IsInitialized { get; set; } = false;
        int CurrentIndex { get; set; }
        Type[] Types { get; set; } = null;
        string[] PopupNames { get; set; } = null;
        string[] FullNames { get; set; } = null;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference) return;
            if (!IsInitialized)
            {
                Initialize(property);
            }
            int selectedIndex = EditorGUI.Popup(GetPopupPosition(position), CurrentIndex, PopupNames);
            if (CurrentIndex != selectedIndex || property.managedReferenceValue == null)
            {
                CurrentIndex = selectedIndex;
                Type selectedType = Types[selectedIndex];
                property.managedReferenceValue = selectedType == null ? null : Activator.CreateInstance(selectedType);
            }
            if (property.managedReferenceValue is IConditionAsset)
            {
                var conditionAsset = property.managedReferenceValue as IConditionAsset;
                conditionAsset.OnGUI(position, property, label);
                property.managedReferenceValue = conditionAsset;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            position.height += EditorGUIUtility.singleLineHeight;
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property, label, true);
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight;
        }


        void Initialize(SerializedProperty property)
        {
            // Get type
            Type baseType = typeof(IConditionAsset);
            Types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(value => value.GetTypes())
                .Where(value => value != null &&
                    baseType.IsAssignableFrom(value) &&
                    value.IsClass &&
                    !value.IsGenericType &&
                    CheckAssignableFromISequenceConditionInstance(value))
                .ToArray();
            // Save type name
            PopupNames = Types.Select(value => value.Name).ToArray();
            FullNames = Types.Select(value => string.Format("{0} {1}", value.Assembly.ToString().Split(',')[0], value.FullName)).ToArray();
            // Selected Index Initialization
            CurrentIndex = Array.IndexOf(FullNames, property.managedReferenceFullTypename);
            CurrentIndex = CurrentIndex < 0 ? 0 : CurrentIndex;
            IsInitialized = true;
        }


        bool CheckAssignableFromISequenceConditionInstance(Type type)
        {
            // Type sequenceType = typeof(ISequenceConditionInstance);
            // // 通常時はISequenceConditionInstanceを使用している条件は表示しない
            // if (_attribute.Type == ConditionSelectorAttribute.ConditionType.Default)
            // {
            //     if(type.BaseType.GetGenericTypeDefinition() == typeof(ConditionAsset<>))
            //     {
            //         return !sequenceType.IsAssignableFrom(type.BaseType.GetGenericArguments()[0]);
            //     }
            // }
            return true;
        }


        Rect GetPopupPosition(Rect currentPosition)
        {
            Rect popupPosition = new Rect(currentPosition);
            popupPosition.height = EditorGUIUtility.singleLineHeight * 0.8f;
            return popupPosition;
        }


    }


}