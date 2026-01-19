using UnityEngine;
using UnityEditor;
using System.Collections;

namespace QDY.NprFilterProURP
{
	[CustomEditor(typeof(Mosaic))]
	public class MosaicInspector : Editor
	{
		class DrawableFilter
		{
			public SerializedProperty m_Sp = null;
			public string m_BtnDisplay;
			public int m_EnumIndex;
			public DrawableFilter(SerializedProperty sp, string display, int enumIdx)
			{
				m_Sp = sp;
				m_BtnDisplay = display;
				m_EnumIndex = enumIdx;
			}
		}
		SerializedProperty m_SpPipeline;
		SerializedProperty m_SpFeature;
		SerializedProperty m_SpEnableFilter;
		SerializedProperty m_SpArea;
		SerializedProperty m_SpReverse;
		SerializedProperty m_SpCircleRadius;
		SerializedProperty m_SpCircleFading;
		DrawableFilter[] m_Drawables;

		void OnEnable()
		{
			m_SpPipeline = serializedObject.FindProperty("m_Pipeline");
			m_SpFeature = serializedObject.FindProperty("m_Feature");
			m_SpEnableFilter = serializedObject.FindProperty("m_EnableFilter");
			m_SpArea = serializedObject.FindProperty("m_Area");
			m_SpReverse = serializedObject.FindProperty("m_Reverse");
			m_SpCircleRadius = serializedObject.FindProperty("m_CircleRadius");
			m_SpCircleFading = serializedObject.FindProperty("m_CircleFading");

			m_Drawables = new DrawableFilter[5];
			m_Drawables[0] = new DrawableFilter(serializedObject.FindProperty("m_Quad"), "Use Quad", 1);
			m_Drawables[1] = new DrawableFilter(serializedObject.FindProperty("m_Triangle"), "Use Triangle", 2);
			m_Drawables[2] = new DrawableFilter(serializedObject.FindProperty("m_Diamond"), "Use Diamond", 3);
			m_Drawables[3] = new DrawableFilter(serializedObject.FindProperty("m_Circle"), "Use Circle", 4);
			m_Drawables[4] = new DrawableFilter(serializedObject.FindProperty("m_Hexagon"), "Use Hexagon", 5);
		}
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			EditorGUILayout.PropertyField(m_SpPipeline, true);
			EditorGUILayout.PropertyField(m_SpFeature, true);
			
			GUILayout.Label("Regional Effect Params ---------------------------");
			EditorGUILayout.PropertyField(m_SpArea, true);
			EditorGUILayout.PropertyField(m_SpReverse, true);
			EditorGUILayout.PropertyField(m_SpCircleRadius, true);
			EditorGUILayout.PropertyField(m_SpCircleFading, true);
			GUILayout.Label("--------------------------------------------------");
			GUILayout.Space(10f);
			
			GUI.enabled = (m_SpEnableFilter.enumValueIndex == 0) ? false : true;
			if (GUILayout.Button("Disable Filter"))
				m_SpEnableFilter.enumValueIndex = 0;
			GUI.enabled = true;

			foreach (DrawableFilter v in m_Drawables)
			{
				if (v == null || v.m_Sp == null)
					continue;

				GUI.enabled = (m_SpEnableFilter.enumValueIndex == v.m_EnumIndex) ? false : true;
				if (GUILayout.Button(v.m_BtnDisplay))
					m_SpEnableFilter.enumValueIndex = v.m_EnumIndex;

				if (Application.isPlaying)
					GUI.enabled = (m_SpEnableFilter.enumValueIndex == v.m_EnumIndex) ? true : false;
				else
					GUI.enabled = true;
				EditorGUILayout.PropertyField(v.m_Sp, true);
				GUI.enabled = true;
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}