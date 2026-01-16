using System;
using UnityEngine;


namespace ARFoundationRemoteExamples {
    public static class InputWrapper {
        static readonly IInputWrapperImpl impl = createImpl();

        static IInputWrapperImpl createImpl() {
            if (ExamplesUtils.isLegacyInputManagerEnabled) {
                return new LegacyInputWrapper();
            }
            
            #if INPUT_SYSTEM_INSTALLED && ENABLE_INPUT_SYSTEM
            return new InputSystemWrapper();
            #endif

            throw new Exception("Please ensure 'com.unity.inputsystem' package is installed and 'Project Setting/Player Settings/Input Handling' is set to 'Both/Input System (New)'.");
        }

        public static int touchCount => impl.touchCount;
        public static ITouchWrapper GetTouch(int i) => impl.GetTouch(i);
    }


    public interface IInputWrapperImpl {
        int touchCount { get; }
        ITouchWrapper GetTouch(int index);
    }


    public interface ITouchWrapper {
        TouchPhase phase { get; }
        Vector2 position { get; }
        int fingerId { get; }
    }
}
