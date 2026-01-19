using System;
using R3;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public interface IGestureIconViewModel : IDisposable
    {

        ReadOnlyReactiveProperty<bool> IsChangeResult { get; }
        ReadOnlyReactiveProperty<string> GestureName { get; }

        void Bind(Gesture gesture);
        void Bind(GestureModel gestureModel);

    }


}