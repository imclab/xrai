using UnityEngine;

namespace OpenCVForUnity.UnityUtils.Helper
{
    public interface ITextureSource2MatHelper : ISource2MatHelper
    {
        Texture sourceTexture
        {
            get;
            set;
        }
    }
}