using System;
using JetBrains.Annotations;
using UnityEngine;
#if AR_FOUNDATION_REMOTE_INSTALLED
    using Input = ARFoundationRemote.Input; // THIS LINE IS REQUIRED FOR LOCATION SERVICES TO WORK WITH AR FOUNDATION EDITOR REMOTE
    #if !ENABLE_AR_FOUNDATION_REMOTE_LOCATION_SERVICES
        using LocationServiceStatus = ARFoundationRemote.LocationServiceStatusDummy;
    #endif
#endif


namespace ARFoundationRemoteExamples {
    public class LocationServicesExample : MonoBehaviour {
        [SerializeField] float desiredAccuracyInMeters = 10f;
        [SerializeField] float updateDistanceInMeters = 10f;
        [SerializeField] int fontSize = 35;
        [SerializeField] bool enableLocationInAwake = true;

        [Header("Location data (read-only)")]
        [SerializeField, UsedImplicitly] bool isEnabledByUser;
        [SerializeField] LocationServiceStatus status;
        [SerializeField, UsedImplicitly] LocationData lastData;


        void Awake() {
            if (ExamplesUtils.UNITY_ANDROID) {
                RequestFineLocationPermission();
            }
            if (!ENABLE_AR_FOUNDATION_REMOTE_LOCATION_SERVICES) {
                #if AR_FOUNDATION_REMOTE_INSTALLED
                Debug.LogError(ARFoundationRemote.LocationServiceRemote.missingDefineError);
                #endif
            }
            if (enableLocationInAwake) {
                startLocationService();
            }
        }

        static bool ENABLE_AR_FOUNDATION_REMOTE_LOCATION_SERVICES {
            get {
                return
                #if ENABLE_AR_FOUNDATION_REMOTE_LOCATION_SERVICES
                    true;
                #else
                    false;
                #endif
            }
        }
        
        static void RequestFineLocationPermission() {
            #if ANDROID_JNI_INSTALLED
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
            return;
            #endif
            
            #pragma warning disable 162
            Debug.LogError($"Please install 'com.unity.modules.androidjni' via Package Manager/Built-in packages.");
            #pragma warning restore 162
        }
        
        void OnGUI() {
            var style = new GUIStyle(GUI.skin.button) {fontSize = fontSize};
            var rect = new Rect(0, 0, 400, 200);
            switch (status) {
                case LocationServiceStatus.Stopped:
                    if (GUI.Button(rect, "Start", style)) {
                        startLocationService();
                    }
                    return;
                case LocationServiceStatus.Initializing:
                case LocationServiceStatus.Running:
                    if (GUI.Button(rect, "Stop", style)) {
                        Input.location.Stop();
                    }
                    return;
            }
        }

        void startLocationService() {
            Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        }
        
        void Update() {
            isEnabledByUser = Input.location.isEnabledByUser;
            status = Input.location.status;
            lastData = Input.location.status == LocationServiceStatus.Running ? LocationData.Create(Input.location.lastData) : default;
        }
    }

    [Serializable]
    struct LocationData {
        public double m_Timestamp;
        public float m_Latitude;
        public float m_Longitude;
        public float m_Altitude;
        public float m_HorizontalAccuracy;
        public float m_VerticalAccuracy;

        public static LocationData Create(LocationInfo _) {
            return new LocationData {
                m_Altitude = _.altitude,
                m_Latitude = _.latitude,
                m_Longitude = _.longitude,
                m_Timestamp = _.timestamp,
                m_HorizontalAccuracy = _.horizontalAccuracy,
                m_VerticalAccuracy = _.verticalAccuracy
            };
        }
    }
}
