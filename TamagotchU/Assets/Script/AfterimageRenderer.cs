using System.Collections.Generic;
using UnityEngine;

namespace AfterimageSample
{
    public class AfterimageRenderer : MonoBehaviour
    {
        [SerializeField] Material _material;
        [SerializeField] public int _duration = 150;
        [SerializeField] int _layer = 6;

        public int Duration { get { return _duration; } set {  _duration = value; } }
        SkinnedMeshRenderer[] _renderers;
        Stack<AfterImage> _pool = new Stack<AfterImage>();
        Queue<AfterImage> _renderQueue = new Queue<AfterImage>();


        void Awake()
        {
            _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        void Update()
        {
            Render();
        }

        /// <summary>
        /// キューに入っているAfterImageのメッシュを描画する.
        /// </summary>
        public void Render()
        {
            for (int i = 0; i < _renderQueue.Count; i++)
            {
                var afterimage = _renderQueue.Dequeue();
                afterimage.RenderMeshes();

                // 描画回数が限度を超えるまで繰り返しキューに入れる.
                // 限度を超えたらプールに返す.
                if (afterimage.FrameCount < _duration)
                {
                    _renderQueue.Enqueue(afterimage);
                }
                else
                {
                    afterimage.Reset();
                    _pool.Push(afterimage);
                }
            }
        }

        /// <summary>
        /// 描画待ちのキューにAfterimageオブジェクトを入れる.
        /// </summary>
        public void Enqueue()
        {
            // get actual mesh count which take account of submesh counts of mesh
            int meshCount = 0;
            for(int i = 0; i < _renderers.Length; i++)
            {
                meshCount += _renderers[i].sharedMesh.subMeshCount;
            }

            AfterImage afterimage;
            if (_pool.Count > 0)
            {
                afterimage = _pool.Pop();
            }
            else
            {
                afterimage = new AfterImage(meshCount);
            }
            afterimage.Setup(_material, _layer, _renderers);
            _renderQueue.Enqueue(afterimage);
        }        
    }
}

