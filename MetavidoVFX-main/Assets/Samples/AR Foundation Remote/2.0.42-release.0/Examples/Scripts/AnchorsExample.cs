#if AR_FOUNDATION_5_0_OR_NEWER
    using ARSessionOrigin = Unity.XR.CoreUtils.XROrigin;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace ARFoundationRemoteExamples {
    public class AnchorsExample : MonoBehaviour {
        [SerializeField] ARAnchorManager anchorManager = null;
        [SerializeField] ARRaycastManager raycastManager = null;
        [SerializeField] ARSessionOrigin origin = null;
        [SerializeField] ARPlaneManager planeManager = null;
        [SerializeField] TrackableType raycastMask = TrackableType.PlaneWithinPolygon;
        [SerializeField] int fontSize = 35;

        AnchorTestType type = AnchorTestType.Add;


        void OnEnable() {
            anchorManager.anchorsChanged += anchorsChanged;
        }

        void OnDisable() {
            anchorManager.anchorsChanged -= anchorsChanged;
        }

        void anchorsChanged(ARAnchorsChangedEventArgs args) {
            /*foreach (var anchor in args.added) {
                print($"Anchor added: {anchor.trackableId}");
            }
            foreach (var anchor in args.removed) {
                print($"Anchor removed: {anchor.trackableId}");
            }*/
        }

        void Update() {
            for (int i = 0; i < InputWrapper.touchCount; i++) {
                var touch = InputWrapper.GetTouch(i);
                if (touch.phase != TouchPhase.Began) {
                    continue;
                }
                
                var ray = origin.GetCamera().ScreenPointToRay(touch.position);
                var hits = new List<ARRaycastHit>();
                var hasHit = raycastManager.Raycast(ray, hits, raycastMask);
                if (hasHit) {
                    switch (type) {
                        case AnchorTestType.Add: {
                            #pragma warning disable 618
                            var first = hits.First();
                            #if AR_FOUNDATION_6_0_OR_NEWER
                            var anchor = anchorManager.TryAddAnchorAsync(first.pose).GetAwaiter().GetResult().value;
                            #else
                            var anchor = anchorManager.AddAnchor(first.pose);
                            #endif
                            #pragma warning restore
                            print($"anchor added: {anchor != null}, hitType: {first.hitType}");
                            break;
                        }
                        case AnchorTestType.AttachToPlane: {
                            var attachedToPlane = tryAttachToPlane(hits);
                            print($"anchor attached successfully: {attachedToPlane}");
                            break;
                        }
                        default:
                            throw new Exception();
                    }
                } else {
                    // print("no hit");
                }
            }
        }

        bool tryAttachToPlane(List<ARRaycastHit> hits) {
            foreach (var hit in hits) {
                var plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null) {
                    var anchor = anchorManager.AttachAnchor(plane, hit.pose);
                    if (anchor != null) {
                        return true;
                    }
                }
            }

            return false;
        }

        void OnGUI() {
            var h = 200;
            var y = Screen.height;

            var style = new GUIStyle(GUI.skin.button) {fontSize = fontSize};

            y -= h;
            if (GUI.Button(new Rect(0, y,400,h), $"Current type: {type}", style)) {
                type = type == AnchorTestType.Add ? AnchorTestType.AttachToPlane : AnchorTestType.Add;
            }

            y -= h;
            if (GUI.Button(new Rect(0, y, 400, h), "Remove all anchors", style)) {
                removeAllAnchors();
            }
        }

        void removeAllAnchors() {
            var copiedAnchors = new HashSet<ARAnchor>();
            foreach (var _ in anchorManager.trackables) {
                copiedAnchors.Add(_);
            }

            foreach (var anchor in copiedAnchors) {
                if (anchor == null) {
                    continue;
                }
                
                #if AR_SUBSYSTEMS_4_1_1_OR_NEWER || AR_FOUNDATION_5_0_OR_NEWER
                DestroyImmediate(anchor.gameObject);
                #else
                var removed = anchorManager.RemoveAnchor(anchor);
                Debug.Log($"Anchor removed {anchor.trackableId}: {removed}");
                #endif
            }
        }
        
        static bool AR_SUBSYSTEMS_4_1_1_OR_NEWER {
            get {
                #if AR_SUBSYSTEMS_4_1_1_OR_NEWER || AR_FOUNDATION_5_0_OR_NEWER
                    return true;
                #else
                return false;
                #endif
            }
        }
        
        enum AnchorTestType {
            Add,
            AttachToPlane
        }
    }
}
