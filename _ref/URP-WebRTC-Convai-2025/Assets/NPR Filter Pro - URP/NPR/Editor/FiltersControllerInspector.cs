using UnityEngine;
using UnityEditor;
using System.Collections;

namespace QDY.NprFilterProURP
{
	[CustomEditor(typeof(FiltersController))]
	public class FiltersControllerInspector : Editor
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

			m_Drawables = new DrawableFilter[15];
			m_Drawables[0] = new DrawableFilter(serializedObject.FindProperty("m_Brick"), "Use Brick", (int)FiltersController.EFilter.Effect1);
			m_Drawables[1] = new DrawableFilter(serializedObject.FindProperty("m_WaterColor"), "Use WaterColor", (int)FiltersController.EFilter.Effect2);
			m_Drawables[2] = new DrawableFilter(serializedObject.FindProperty("m_SketchDrawing"), "Use SketchDrawing", (int)FiltersController.EFilter.Effect3);
			m_Drawables[3] = new DrawableFilter(serializedObject.FindProperty("m_Knitwear"), "Use Knitwear", (int)FiltersController.EFilter.Effect4);
			m_Drawables[4] = new DrawableFilter(serializedObject.FindProperty("m_DotDrawing"), "Use DotDrawing", (int)FiltersController.EFilter.Effect5);
			m_Drawables[5] = new DrawableFilter(serializedObject.FindProperty("m_OneBit"), "Use OneBit", (int)FiltersController.EFilter.Effect6);
			m_Drawables[6] = new DrawableFilter(serializedObject.FindProperty("m_PolygonColor"), "Use PolygonColor", (int)FiltersController.EFilter.Effect7);
			m_Drawables[7] = new DrawableFilter(serializedObject.FindProperty("m_Tiles"), "Use Tiles", (int)FiltersController.EFilter.Effect8);
			m_Drawables[8] = new DrawableFilter(serializedObject.FindProperty("m_OilPaint"), "Use OilPaint", (int)FiltersController.EFilter.Effect9);
			m_Drawables[9] = new DrawableFilter(serializedObject.FindProperty("m_BayerDither"), "Use BayerDither", (int)FiltersController.EFilter.Effect10);
			m_Drawables[10] = new DrawableFilter(serializedObject.FindProperty("m_SketchNotebook"), "Use SketchNotebook", (int)FiltersController.EFilter.Effect11);
			m_Drawables[11] = new DrawableFilter(serializedObject.FindProperty("m_Aquarelle"), "Use Aquarelle", (int)FiltersController.EFilter.Effect12);
			m_Drawables[12] = new DrawableFilter(serializedObject.FindProperty("m_Kuwahara"), "Use Kuwahara", (int)FiltersController.EFilter.Effect13);
			m_Drawables[13] = new DrawableFilter(serializedObject.FindProperty("m_Weave"), "Use Weave", (int)FiltersController.EFilter.Effect14);
			m_Drawables[14] = new DrawableFilter(serializedObject.FindProperty("m_BlackWhiteScary"), "Use BlackWhiteScary", (int)FiltersController.EFilter.Effect15);
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

			if (GUILayout.Button("Disable Filter"))
				m_SpEnableFilter.enumValueIndex = 0;

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