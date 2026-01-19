#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.DnnModule
{
    public partial class Model : DisposableOpenCVObject
    {


        //
        // C++:  Model cv::dnn::Model::setInputSize(Size size)
        //

        /// <summary>
        ///  Set input size for frame.
        /// </summary>
        public Model setInputSize(in Vec2d size)
        {
            ThrowIfDisposed();

            return new Model(DisposableObject.ThrowIfNullIntPtr(dnn_Model_setInputSize_10(nativeObj, size.Item1, size.Item2)));


        }


        //
        // C++:  Model cv::dnn::Model::setInputMean(Scalar mean)
        //

        /// <summary>
        ///  Set mean value for frame.
        /// </summary>
        public Model setInputMean(in Vec4d mean)
        {
            ThrowIfDisposed();

            return new Model(DisposableObject.ThrowIfNullIntPtr(dnn_Model_setInputMean_10(nativeObj, mean.Item1, mean.Item2, mean.Item3, mean.Item4)));


        }


        //
        // C++:  Model cv::dnn::Model::setInputScale(Scalar scale)
        //

        /// <summary>
        ///  Set scalefactor value for frame.
        /// </summary>
        public Model setInputScale(in Vec4d scale)
        {
            ThrowIfDisposed();

            return new Model(DisposableObject.ThrowIfNullIntPtr(dnn_Model_setInputScale_10(nativeObj, scale.Item1, scale.Item2, scale.Item3, scale.Item4)));


        }


        //
        // C++:  void cv::dnn::Model::setInputParams(double scale = 1.0, Size size = Size(), Scalar mean = Scalar(), bool swapRB = false, bool crop = false)
        //

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in Vec2d size, in Vec4d mean, bool swapRB, bool crop)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_10(nativeObj, scale, size.Item1, size.Item2, mean.Item1, mean.Item2, mean.Item3, mean.Item4, swapRB, crop);


        }

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in Vec2d size, in Vec4d mean, bool swapRB)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_11(nativeObj, scale, size.Item1, size.Item2, mean.Item1, mean.Item2, mean.Item3, mean.Item4, swapRB);


        }

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in Vec2d size, in Vec4d mean)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_12(nativeObj, scale, size.Item1, size.Item2, mean.Item1, mean.Item2, mean.Item3, mean.Item4);


        }

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in Vec2d size)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_13(nativeObj, scale, size.Item1, size.Item2);


        }

    }
}

#endif