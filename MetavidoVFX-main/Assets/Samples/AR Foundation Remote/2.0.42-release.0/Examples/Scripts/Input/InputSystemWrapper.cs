#if INPUT_SYSTEM_INSTALLED
using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;


namespace ARFoundationRemoteExamples {
    public class InputSystemWrapper : IInputWrapperImpl {
        public InputSystemWrapper() {
            EnhancedTouchSupport.Enable();
        }
        
        int IInputWrapperImpl.touchCount => Touch.activeTouches.Count;

        ITouchWrapper IInputWrapperImpl.GetTouch(int index) {
            var touch = Touch.activeTouches[index];
            return new InputSystemTouchWrapper(touch);
        }

        readonly struct InputSystemTouchWrapper : ITouchWrapper {
            readonly Touch touch;

            public InputSystemTouchWrapper(Touch touch) {
                this.touch = touch;
            }

            TouchPhase ITouchWrapper.phase {
                get {
                    switch (touch.phase) {
                        case UnityEngine.InputSystem.TouchPhase.Began:
                            return TouchPhase.Began;
                        case UnityEngine.InputSystem.TouchPhase.Canceled:
                            return TouchPhase.Canceled;
                        case UnityEngine.InputSystem.TouchPhase.Ended:
                            return TouchPhase.Ended;
                        case UnityEngine.InputSystem.TouchPhase.Moved:
                            return TouchPhase.Moved;
                        case UnityEngine.InputSystem.TouchPhase.Stationary:
                            return TouchPhase.Stationary;
                        case UnityEngine.InputSystem.TouchPhase.None:
                            return (TouchPhase) (-1);
                        default:
                            throw new Exception(touch.phase.ToString());
                    }
                }
            }

            Vector2 ITouchWrapper.position => touch.screenPosition;
            public int fingerId => touch.finger.index;
        }
    }
}
#endif
