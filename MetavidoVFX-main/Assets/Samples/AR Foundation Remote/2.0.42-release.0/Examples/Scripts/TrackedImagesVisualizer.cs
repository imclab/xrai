using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace ARFoundationRemote.Runtime {
    public class TrackedImagesVisualizer : MonoBehaviour {
        [SerializeField] ARTrackedImageManager manager = null;
        [SerializeField] bool log = true;

        bool isEnabled;

        
        void Awake() {
            manager.trackedImagesChanged += trackedImagesChanged;
            isEnabled = manager.enabled;
        }

        void OnDestroy() {
            manager.trackedImagesChanged -= trackedImagesChanged;            
        }

        void Update() {
            var newIsEnabled = manager.enabled;
            if (isEnabled != newIsEnabled) {
                isEnabled = newIsEnabled;

                foreach (var trackable in manager.trackables) {
                    trackable.gameObject.SetActive(newIsEnabled);
                }
            }
        }

        void trackedImagesChanged(ARTrackedImagesChangedEventArgs args) {
            if (log) {
                foreach (var _ in args.added) {
                    Debug.Log("added " + _.referenceImage.name);
                }

                foreach (var _ in args.removed) {
                    // removed list is not reliable. It will be always empty on iOS and on Android will be populated only when we restart Image Tracking 
                    Debug.Log("removed " + _.referenceImage.name);
                }
            }

            foreach (var trackedImage in args.added.Concat(args.updated)) {
                var model = trackedImage.transform.GetChild(0);
                model.gameObject.SetActive(trackedImage.trackingState != TrackingState.None);
                var size = trackedImage.size;
                model.localScale = new Vector3(size.x, size.y, 1);
                model.GetComponent<MeshRenderer>().material.mainTexture = trackedImage.referenceImage.texture;
            }
        }
    }
}
