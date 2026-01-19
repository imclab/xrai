namespace OpenCVForUnity.UnityUtils.Helper
{
    public interface IMatUpdateFPSProvider
    {
        float requestedMatUpdateFPS
        {
            get;
            set;
        }

        float GetMatUpdateFPS();
    }
}