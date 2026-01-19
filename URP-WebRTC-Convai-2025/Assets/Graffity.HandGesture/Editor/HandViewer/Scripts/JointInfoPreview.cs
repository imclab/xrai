using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Graffity.HandGesture.Conditions;
using Unity.Plastic.Antlr3.Runtime.Misc;

namespace Graffity.HandGesture.Editor.HandViewer
{
    [CustomEditor(typeof(GestureAsset))]
    public class JointInfoPreview : UnityEditor.Editor
    {
        readonly GUIContent _title = new("JointInfoPreview");

        PreviewRenderUtility previewRenderUtility;

        GameObject leftHandPrefab;
        GameObject rightHandPrefab;

        HandObjectData leftHand;
        HandObjectData rightHand;

        static Vector2 previewDir = new(-180f, 0f);

        static readonly float HandX = 0.1f;
        static readonly float HandY = -0.1f;
        readonly Vector3 LeftHandPos = new(-HandX, HandY, 0f);
        readonly Vector3 RightHandPos = new(HandX, HandY, 0f);
        readonly Vector3 CameraPos = new(0f, 0f, 1f);

        readonly float HandScaleValue = 0.8f;

        readonly float CamRotDeltaMin = 1f;
        readonly float CamRotDeltaMax = 3f;

        readonly float CamRotSpeed = 140f;

        readonly float PreviewWindowWidthMin = 50f;
        readonly float DropAreaWidth = 180f;

        readonly float PrefabFieldLabelWidth = 48;


        readonly string PACKAGE_PREFAB_PATH = "Packages/com.graffityinc.handgesture/Editor/HandViewer/Prefabs";
        readonly string ASSET_PREFAB_PATH = "Assets/Graffity.Handgesture/Editor/HandViewer/Prefabs";

        public Vector2 Drag2D(Vector2 scrollPosition, Rect position)
        {
            int controlID = GUIUtility.GetControlID("Slider".GetHashCode(), FocusType.Passive);
            Event current = Event.current;

            switch (current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && PreviewWindowWidthMin < position.width)
                    {
                        GUIUtility.hotControl = controlID;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        scrollPosition -= current.delta * (float)((!current.shift) ? CamRotDeltaMin : CamRotDeltaMax) / Mathf.Min(position.width, position.height) * CamRotSpeed;
                        scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
                        current.Use();
                        GUI.changed = true;
                    }
                    break;
            }
            return scrollPosition;
        }

        public override void OnPreviewSettings()
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = PrefabFieldLabelWidth;
            var lhp = EditorGUILayout.ObjectField("Left", leftHandPrefab, typeof(GameObject), false, GUILayout.Width(DropAreaWidth)) as GameObject;
            if (lhp != leftHandPrefab)
            {
                if (lhp != null)
                {
                    leftHand?.Dispose();
                    leftHand = null;
                }
                leftHandPrefab = lhp;
            }
            var rhp = EditorGUILayout.ObjectField("Right", rightHandPrefab, typeof(GameObject), false, GUILayout.Width(DropAreaWidth)) as GameObject;
            if (rhp != rightHandPrefab)
            {
                if (rhp != null)
                {
                    rightHand?.Dispose();
                    rightHand = null;
                }
                rightHandPrefab = rhp;
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        void Initialize()
        {
            if (previewRenderUtility != null)
            {
                return;
            }

            previewRenderUtility = new();
            previewRenderUtility.camera.farClipPlane = 100;
            previewRenderUtility.camera.transform.SetPositionAndRotation(
                CameraPos,
                Quaternion.Euler(0, 0, 0)
                );


            RefreshPreviewInstance();
        }

        void DoRenderPreview()
        {
            if (leftHand == null || rightHand == null)
            {
                RefreshPreviewInstance();
            }

            RenderMeshPreview(leftHand, rightHand, previewRenderUtility, previewDir, -1);
        }

        void RefreshPreviewInstance()
        {
            if (leftHand == null && leftHandPrefab != null)
            {
                leftHand?.Dispose();
                leftHand = new HandObjectData(previewRenderUtility, leftHandPrefab, HandInfo.HandType.Left);
            }
            if (rightHand == null && rightHandPrefab != null)
            {
                rightHand?.Dispose();
                rightHand = new HandObjectData(previewRenderUtility, rightHandPrefab, HandInfo.HandType.Right);
            }


            leftHand?.SetJointInfo(target as GestureAsset);
            rightHand?.SetJointInfo(target as GestureAsset);
        }

        void RenderMeshPreview(HandObjectData left, HandObjectData right, PreviewRenderUtility pru, Vector2 dir, int meshSubset)
        {
            if (pru == null)
            {
                return;
            }

            pru.camera.backgroundColor = Color.gray;
            pru.camera.clearFlags = CameraClearFlags.Color;

            pru.camera.nearClipPlane = 0.3f;
            pru.camera.farClipPlane = 100f;

            var q = Quaternion.Euler(dir.y, dir.x, 0f);
            var lookAtPos = (LeftHandPos + RightHandPos) / 2;
            var campos = q * CameraPos;

            pru.camera.transform.SetPositionAndRotation(campos, q);
            lookAtPos.y = 0f;
            pru.camera.transform.LookAt(lookAtPos);

            var fogState = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            var meshAngle = Quaternion.Euler(0, 90, -90);
            left?.Render(pru, LeftHandPos, new Vector3(HandScaleValue, HandScaleValue, HandScaleValue), meshAngle, meshSubset);
            right?.Render(pru, RightHandPos, new Vector3(HandScaleValue, HandScaleValue, HandScaleValue), meshAngle, meshSubset);

            pru.camera.Render();

            Unsupported.SetRenderSettingsUseFogNoDirty(fogState);
        }

        public override bool HasPreviewGUI()
        {
            // pickするのは
            // JointRotation : これは直接関節の数と状態
            // GestureCheck : 対象のGestureAssetの通りの状態にならないといけない

            GestureAsset asset = target as GestureAsset;

            var al = asset.ConditionAssetList;
            var gestureCondition = al.Where(ax => ax is JointRotation).ToArray();

            // gestureConditionが1つも無い場合は手の動きの設定が無い為previewGUIを無効にする

            return 0 < gestureCondition.Length;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            Initialize();
            previewDir = Drag2D(previewDir, r);
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            previewRenderUtility.BeginPreview(r, background);

            DoRenderPreview();
            var image = previewRenderUtility.EndPreview();

            GUI.DrawTexture(r, image, ScaleMode.StretchToFill, false);
            EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 24f), target.name);
        }

        /// <summary>prefabNameで指定されたassetがあれば該当のassetのfullpathを返す</summary>
        /// <param name="prefabName">prefab名(拡張子含む)</param>
        /// <returns>想定したpathにprefabNameのファイルがあればそのAssetPathを。無ければnullを返す</returns>
        string GetHandPrefabPath(string prefabName)
        {
            string[] patharray = { PACKAGE_PREFAB_PATH, ASSET_PREFAB_PATH };

            Debug.Assert(!string.IsNullOrEmpty(prefabName));

            return patharray
                .Select(p => $"{p}/{prefabName}")
                .Where(p => System.IO.File.Exists(System.IO.Path.GetFullPath(p)))
                .FirstOrDefault();
        }

        void Reset()
        {
            Initialize();

            if (leftHand == null)
            {
                var path = GetHandPrefabPath("LeftHandPrefab.prefab");
                Debug.Assert(!string.IsNullOrEmpty(path));

                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                leftHandPrefab = obj;
            }
            if (rightHand == null)
            {
                var path = GetHandPrefabPath("RightHandPrefab.prefab");
                Debug.Assert(!string.IsNullOrEmpty(path));

                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                rightHandPrefab = obj;
            }
            RefreshPreviewInstance();

            Resources.UnloadUnusedAssets();
            leftHandPrefab = rightHandPrefab = null;
        }

        void OnDisable()
        {
            OnDestroy();
        }

        void OnDestroy()
        {
            previewRenderUtility?.Cleanup();
            previewRenderUtility = null;

            leftHand?.Dispose();
            leftHand = null;
            rightHand?.Dispose();
            rightHand = null;

            previewDir = new(-180f, 0f);
        }
    }
}
