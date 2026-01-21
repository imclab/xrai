using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FERM_Component), true)]
public class FERM_ComponentEditor : Editor {

    new FERM_Component target { get { return (FERM_Component)base.target; } }

    public override void OnInspectorGUI() {
        if(target.parameters == null) {
            ErrorField("Component is not initialized. Please rebuild the shader.");
            return;
        }
        if(target.rend == null)
            ErrorField("This component must be in the hierarchy of a FERM_Renderer to work.");
        else if(!target.IsDisplayable())
            ErrorField("Component does not contribute to rendering in this state. The parent GameObject may need a FERM_Mixer component.");

        if(target is FERM_Mixer && !FERM_Util.IsIdentity(target.transform))
            WarningField("Moving a mixer might yield unexpected results. Consider leaving the transform at default and adding a FERM_Modifier of type 'transform' in stead.");

        FieldInfo typeField = target.GetType().GetField("type");
        if(typeField != null) {
            Enum typeValue = (Enum)typeField.GetValue(target);
            Enum newTypeValue = EditorGUILayout.EnumPopup((Enum)typeValue);
            if(typeValue != newTypeValue) {
                Undo.RecordObject(target, "Changed FERM Component type");
                typeField.SetValue(target, newTypeValue);
            }
        }

        if(target.rend != null && target.rend.showTips)
            TipField(target.GetGeometry());

        if(target is FERM_Shape) {
            FERM_Shape el = (FERM_Shape)target;
            FERM_Shape.UnityTransformMode utm = (FERM_Shape.UnityTransformMode)
                EditorGUILayout.EnumPopup("Unity transform mode", el.unityTransformMode);
            if(utm != el.unityTransformMode) {
                Undo.RecordObject(target, "Changed FERM Unity Transform mode");
                el.unityTransformMode = utm;
            }
        }

        foreach(FERM_Parameter parameter in target.parameters) {
            //ignore transform parameters
            if(parameter.name.StartsWith("t_"))
                continue;

            InspectorValueField(parameter);
        }

        if(GUI.changed) {
            target.rend.ChangeTrigger();
            EditorUtility.SetDirty(target);
        }
    }

    private void InspectorValueField(FERM_Parameter param) {
        
        switch(param.type) {
        case FERM_Parameter.Type.Floating:
            float floatValue = (float)param.GetValue();
            floatValue = EditorGUILayout.FloatField(param.name, floatValue);
            param.SetValue(floatValue);
            break;
        case FERM_Parameter.Type.Integer:
            int intValue = (int)param.GetValue();
            intValue = EditorGUILayout.IntField(param.name, intValue);
            param.SetValue(intValue);
            break;
        case FERM_Parameter.Type.Vector:
            Vector3 vecValue = (Vector3)param.GetValue();
            vecValue = EditorGUILayout.Vector3Field(param.name, vecValue);
            param.SetValue(vecValue);
            break;
        case FERM_Parameter.Type.Vec2:
            Vector2 vec2Value = (Vector2)param.GetValue();
            vec2Value = EditorGUILayout.Vector2Field(param.name, vec2Value);
            param.SetValue(vec2Value);
            break;
        case FERM_Parameter.Type.Quaternion:
            Quaternion quatValue = (Quaternion)param.GetValue();
            Vector3 eulerValue = quatValue.eulerAngles;
            eulerValue = EditorGUILayout.Vector3Field(param.name, eulerValue);
            quatValue = Quaternion.Euler(eulerValue);
            param.SetValue(quatValue);
            break;
        case FERM_Parameter.Type.Axis:
            FERM_Parameter.Axis axisValue = (FERM_Parameter.Axis)param.GetValue();
            axisValue = (FERM_Parameter.Axis)EditorGUILayout.EnumPopup(param.name, axisValue);
            param.SetValue(axisValue);
            break;

        }
    }

    private void ErrorField(string message) {
        GUIStyle s = FERM_EditorUtil.Style.Make(new Color(.8f, 0f, 0f));
        GUILayout.Label(message, s);
    }

    private void WarningField(string message) {
        if(target.rend == null || !target.rend.showTips)
            return;
        GUIStyle s = FERM_EditorUtil.Style.Make(new Color(.5f, .3f, 0f));
        GUILayout.Label(message, s);
    }

    private void TipField(FERM_Component.Geometry geometry) {
        GUIStyle s;
        switch(geometry) {
        case FERM_Component.Geometry.Exact:
            s = FERM_EditorUtil.Style.Make(new Color(0f, .7f, 0f));
            GUILayout.Label("Safe and quick.", s);
            break;
        case FERM_Component.Geometry.ExactConditonal:
            s = FERM_EditorUtil.Style.Make(new Color(.4f, .6f, 0f));
            GUILayout.Label("Safe and quick, given reasonable parameters.", s);
            break;
        case FERM_Component.Geometry.Complex:
            s = FERM_EditorUtil.Style.Make(new Color(.5f, .5f, 0f));
            GUILayout.Label("Mostly safe, but expensive to render. Use sparingly.", s);
            break;
        case FERM_Component.Geometry.Recurse:
            s = FERM_EditorUtil.Style.Make(new Color(.5f, 5f, 0f));
            GUILayout.Label("Use in combination with safe modifiers for best results, high iteration count can make this very expensive to render.", s);
            break;
        case FERM_Component.Geometry.Bounded:
            s = FERM_EditorUtil.Style.Make(new Color(.6f, .4f, 0f));
            GUILayout.Label("May create minor artefacts and slow down rendering. Avoid if possible.", s);
            break;
        case FERM_Component.Geometry.BoundedConditonal:
            s = FERM_EditorUtil.Style.Make(new Color(.6f, .3f, 0f));
            GUILayout.Label("May slow down rendering, but is safe given reasonable parameters.", s);
            break;
        case FERM_Component.Geometry.Distorted:
            s = FERM_EditorUtil.Style.Make(new Color(.8f, 0f, 0f));
            GUILayout.Label("Likely to yield artefacts. Use carefully and increase oversampling if necessary.", s);
            break;
        default:
            s = FERM_EditorUtil.Style.Make(new Color(.5f, .5f, .5f));
            GUILayout.Label("Unkown geometry type: " + geometry, s);
            break;
        }
    }


}
