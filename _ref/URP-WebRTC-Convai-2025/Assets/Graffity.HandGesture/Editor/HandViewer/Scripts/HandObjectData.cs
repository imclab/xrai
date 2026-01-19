using System;
using System.Collections.Generic;
using System.Linq;
using Graffity.HandGesture;
using Graffity.HandGesture.Conditions;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;

namespace Graffity.HandGesture.Editor.HandViewer
{
    public class HandObjectData : IDisposable
    {
        // L_若しくはR_
        readonly string sideHeader = "";
        readonly string[] SideHeaderString = { "L_", "R_" };
        readonly HandInfo.HandType handSide;

        public GameObject prefab;
        public SkinnedMeshRenderer meshRenderer;


        public Mesh drawMesh;

        Material material;
        GameObject instance;



        public Dictionary<string, Transform> jointIndex;

        public HandObjectData(GameObject prefab, HandInfo.HandType handType)
        {
            this.prefab = prefab;
            handSide = handType;

            Debug.Assert(handType == HandInfo.HandType.Left || handType == HandInfo.HandType.Right);

            sideHeader = SideHeaderString[(int)handType];
        }

        public HandObjectData(PreviewRenderUtility pru, GameObject prefab, HandInfo.HandType handType) : this(prefab, handType)
        {
            Initialize(pru);
        }

        public void Dispose()
        {
            if (instance != null)
            {
                GameObject.DestroyImmediate(instance);
            }
            instance = null;
            meshRenderer = null;
            drawMesh = null;
            jointIndex?.Clear();
            jointIndex = null;
            prefab = null;
            material = null;
        }

        public void Initialize(PreviewRenderUtility pru)
        {
            var nprefab = prefab;
            Dispose();
            prefab = nprefab;

            Debug.Assert(prefab != null);

            instance = pru.InstantiatePrefabInScene(prefab);
            instance.SetActive(false);

            var mr = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (mr != null && 0 < mr.Length)
            {
                material = mr[0].sharedMaterial;
                meshRenderer = mr[0];
            }

            jointIndex = new();
            jointIndex = instance.transform.GetComponentsInChildren<Transform>().Select(x => (x.gameObject.name, x)).ToDictionary(v => v.name, v => v.x);
        }

        public void SetJointInfo(GestureAsset gestureAsset)
        {
            foreach (var ji in gestureAsset.ConditionAssetList)
            {
                if (ji is JointRotation jointRot)
                {
                    if (jointRot.Joint.Hand != handSide)
                    {
                        continue;
                    }
                    var indexName = $"{sideHeader}{jointRot.Joint.JointID.ToString()}";
                    var joint = jointIndex[indexName];

                    var pitch = (jointRot.PitchMax + jointRot.PitchMin) / 2;
                    var yaw = (jointRot.YawMax + jointRot.YawMin) / 2;
                    var roll = joint.localEulerAngles.z;

                    bool xAdjust = false;

                    // pitch,yawは間が100度以上ある場合は指の元の角度を入れる様にする
                    if (100f <= Mathf.Abs(jointRot.PitchMax - jointRot.PitchMin))
                    {
                        pitch = -joint.localEulerAngles.x;
                        xAdjust = true; // pitchの補正を行った
                    }

                    if (100f <= Mathf.Abs(jointRot.YawMax - jointRot.YawMin))
                    {
                        yaw = joint.localEulerAngles.y;
                    }

                    #region 強引な補正

                    // MEMO:理論に基いた補正処理ではない為、丁寧な改修を入れる予定

                    // 親指付根の場合
                    if (indexName.Contains("ThumbMetacarpal"))
                    {
                        // 指定されたPitchMin〜Maxが任意角に設定されていた場合は補正しない
                        if (!xAdjust)
                        {
                            var offset = pitch - jointRot.PitchMin;
                            pitch = joint.localEulerAngles.x - offset;
                        }
                    }

                    // 左手用の補正
                    if (handSide == HandInfo.HandType.Left)
                    {
                        pitch = -pitch;
                    }

                    #endregion



                    try
                    {
                        jointIndex[indexName].localRotation = Quaternion.Euler(-pitch, yaw, roll);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"exception[{e.ToString()}] : {indexName}");
                    }
                }
            }

        }

        public void Render(PreviewRenderUtility pru, Vector3 pos, Vector3 scale, Quaternion q, int meshSubset)
        {
            drawMesh = new Mesh();
            meshRenderer.BakeMesh(drawMesh);


            var subMeshCount = drawMesh.subMeshCount;
            if (meshSubset < 0 || subMeshCount <= meshSubset)
            {
                for (int i = 0; i < subMeshCount; ++i)
                {
                    pru.DrawMesh(drawMesh, pos, scale, q, material, i, null, null, false);
                }
            }
            else
            {
                pru.DrawMesh(drawMesh, pos, scale, q, material, subMeshCount, null, null, false);
            }
        }
    }
}
