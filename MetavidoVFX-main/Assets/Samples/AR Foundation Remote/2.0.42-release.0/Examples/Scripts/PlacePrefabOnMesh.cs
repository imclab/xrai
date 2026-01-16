#if AR_FOUNDATION_5_0_OR_NEWER
    using ARSessionOrigin = Unity.XR.CoreUtils.XROrigin;
#endif
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.ARFoundation;


namespace ARFoundationRemoteExamples {
    public class PlacePrefabOnMesh : MonoBehaviour {
        [SerializeField] ARMeshManager meshManager = null;
        [SerializeField] ARSessionOrigin origin = null;
        [CanBeNull] [SerializeField] GameObject optionalPointerPrefab = null;
        [SerializeField] bool disableObjectsOnTouchEnd = false;

        readonly Dictionary<int, Transform> pointers = new Dictionary<int, Transform>();


        void Update() {
            for (int i = 0; i < InputWrapper.touchCount; i++) {
                var touch = InputWrapper.GetTouch(i);
                var pointer = getPointer(touch.fingerId);
                var touchPhase = touch.phase;
                if (touchPhase == TouchPhase.Ended || touchPhase == TouchPhase.Canceled) {
                    if (disableObjectsOnTouchEnd) {
                        pointer.gameObject.SetActive(false);
                    }
                } else {
                    var ray = origin.GetCamera().ScreenPointToRay(touch.position);
                    var hasHit = Physics.Raycast(ray, out var hit, float.PositiveInfinity, 1 << meshManager.meshPrefab.gameObject.layer);
                    if (hasHit) {
                        pointer.position = hit.point;
                        pointer.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    }

                    pointer.gameObject.SetActive(hasHit);
                }
            }
        }

        Transform getPointer(int fingerId) {
            if (pointers.TryGetValue(fingerId, out var existing)) {
                return existing;
            } else {
                var newPointer = createNewPointer();
                pointers[fingerId] = newPointer;
                return newPointer;
            }
        }

        Transform createNewPointer() {
            var result = instantiatePointer();
            Assert.AreNotEqual(result.gameObject.layer, meshManager.meshPrefab.gameObject.layer, "Pointer layer should not be the same as the mesh prefab layer");
            result.parent = transform;
            return result;
        }

        Transform instantiatePointer() {
            if (optionalPointerPrefab != null) {
                return Instantiate(optionalPointerPrefab).transform;
            } else {
                var result = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                result.localScale = Vector3.one * 0.05f;
                result.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                return result;
            }
        }
    }
}
