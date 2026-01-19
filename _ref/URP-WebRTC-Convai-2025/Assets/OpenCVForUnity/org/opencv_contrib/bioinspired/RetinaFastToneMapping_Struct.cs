
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.BioinspiredModule
{
    public partial class RetinaFastToneMapping : Algorithm
    {


        //
        // C++: static Ptr_RetinaFastToneMapping cv::bioinspired::RetinaFastToneMapping::create(Size inputSize)
        //

        public static RetinaFastToneMapping create(in Vec2d inputSize)
        {


            return RetinaFastToneMapping.__fromPtr__(DisposableObject.ThrowIfNullIntPtr(bioinspired_RetinaFastToneMapping_create_10(inputSize.Item1, inputSize.Item2)));


        }

    }
}
