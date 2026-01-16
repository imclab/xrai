using UnityEngine;
#if UNITY_2019_2
    using Input = ARFoundationRemote.Input;
#else
    using Input = UnityEngine.Input;
#endif


namespace ARFoundationRemoteExamples {
    public class LegacyInputWrapper : IInputWrapperImpl {
        int IInputWrapperImpl.touchCount => Input.touchCount;

        ITouchWrapper IInputWrapperImpl.GetTouch(int index) {
            var touch = Input.GetTouch(index);
            return new LegacyTouchWrapper(touch);
        }

        readonly struct LegacyTouchWrapper : ITouchWrapper {
            readonly Touch touch;

            public LegacyTouchWrapper(Touch touch) {
                this.touch = touch;
            }

            TouchPhase ITouchWrapper.phase => touch.phase;
            Vector2 ITouchWrapper.position => touch.position;
            public int fingerId => touch.fingerId;
        }
    }
}
