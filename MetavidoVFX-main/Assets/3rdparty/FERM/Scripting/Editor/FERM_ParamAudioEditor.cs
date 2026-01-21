using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FERM_ParamAudio))]
public class FERM_ParamAudioEditor : FERM_ParamAccessEditor {

    SerializedProperty input, inputFrequency, thresholdLevel, minTime, attack, sustain, outputType;
    SerializedProperty rest_float, beat_float, rest_vec, beat_vec, rest_quat, beat_quat;

    new FERM_ParamAudio target { get { return (FERM_ParamAudio)base.target; } }

    private void OnEnable() {
        input = serializedObject.FindProperty("input");
        inputFrequency = serializedObject.FindProperty("inputFrequency");
        thresholdLevel = serializedObject.FindProperty("thresholdLevel");
        minTime = serializedObject.FindProperty("minTime");
        attack = serializedObject.FindProperty("attack");
        sustain = serializedObject.FindProperty("sustain");
        outputType = serializedObject.FindProperty("outputType");

        rest_float = serializedObject.FindProperty("rest_float");
        beat_float = serializedObject.FindProperty("beat_float");
        rest_vec = serializedObject.FindProperty("rest_vec");
        beat_vec = serializedObject.FindProperty("beat_vec");
        rest_quat = serializedObject.FindProperty("rest_quat");
        beat_quat = serializedObject.FindProperty("beat_quat");
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUILayout.PropertyField(input);
        EditorGUILayout.PropertyField(inputFrequency);
        EditorGUILayout.PropertyField(thresholdLevel);

        if(target.rend.showTips)
            TipField();

        EditorGUILayout.PropertyField(minTime);
        EditorGUILayout.PropertyField(attack);
        EditorGUILayout.PropertyField(sustain);
        EditorGUILayout.PropertyField(outputType);
        
        if(target.target != null)
            ExposeAmplitudeParameters(target.target.parameter.type);

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void TipField() {
        GUIStyle g = FERM_EditorUtil.Style.Make(new Color(.4f, 0f, .8f));
        EditorGUILayout.LabelField("Suggested threshold levels", g);
        EditorGUILayout.LabelField("1/10 : 1/50 : 1/100", g);
        EditorGUILayout.LabelField(target.GetAmpGuide(), g);
    }

    private void ExposeAmplitudeParameters(FERM_Parameter.Type type) {
        switch(type) {
        case FERM_Parameter.Type.Integer:
        case FERM_Parameter.Type.Floating:
            EditorGUILayout.PropertyField(rest_float);
            EditorGUILayout.PropertyField(beat_float);
            break;
        case FERM_Parameter.Type.Vector:
        case FERM_Parameter.Type.Vec2:
            EditorGUILayout.PropertyField(rest_vec);
            EditorGUILayout.PropertyField(beat_vec);
            break;
            
        case FERM_Parameter.Type.Quaternion:
            EditorGUILayout.PropertyField(rest_quat, true);
            EditorGUILayout.PropertyField(beat_quat, true);
            break;
        default:
            Debug.LogError("Unkown target type: " + type);
            break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
