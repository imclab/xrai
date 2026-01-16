using UnityEngine;
// using Unity.WebRTC;

namespace H3M.Network
{
    public class WebRTCReceiver : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] string _connectionStatus = "Disabled (WebRTC Missing)";

        // RTCDataChannel _dataChannel;
        // RTCPeerConnection _pc;

        void Start()
        {
            // StartCoroutine(WebRTC.Update());
            Initialize();
        }

        public void Initialize()
        {
            /*
            _pc = new RTCPeerConnection();
            _pc.OnDataChannel = channel =>
            {
                _dataChannel = channel;
                _dataChannel.OnMessage = bytes => Debug.Log($"[WebRTC] Received {bytes.Length} bytes");
                _connectionStatus = "Connected";
            };
            */

            Debug.Log("[WebRTC] Stubbed out to fix Unity 6 compatibility/build errors.");
        }

        void OnDestroy()
        {
            // _pc?.Close();
            // _pc?.Dispose();
        }
    }
}
