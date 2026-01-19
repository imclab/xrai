#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
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
        public Model setInputSize(in (double width, double height) size)
        {
            ThrowIfDisposed();

            return new Model(DisposableObject.ThrowIfNullIntPtr(dnn_Model_setInputSize_10(nativeObj, size.width, size.height)));


        }


        //
        // C++:  Model cv::dnn::Model::setInputMean(Scalar mean)
        //

        /// <summary>
        ///  Set mean value for frame.
        /// </summary>
        public Model setInputMean(in (double v0, double v1, double v2, double v3) mean)
        {
            ThrowIfDisposed();

            return new Model(DisposableObject.ThrowIfNullIntPtr(dnn_Model_setInputMean_10(nativeObj, mean.v0, mean.v1, mean.v2, mean.v3)));


        }


        //
        // C++:  Model cv::dnn::Model::setInputScale(Scalar scale)
        //

        /// <summary>
        ///  Set scalefactor value for frame.
        /// </summary>
        public Model setInputScale(in (double v0, double v1, double v2, double v3) scale)
        {
            ThrowIfDisposed();

            return new Model(DisposableObject.ThrowIfNullIntPtr(dnn_Model_setInputScale_10(nativeObj, scale.v0, scale.v1, scale.v2, scale.v3)));


        }


        //
        // C++:  void cv::dnn::Model::setInputParams(double scale = 1.0, Size size = Size(), Scalar mean = Scalar(), bool swapRB = false, bool crop = false)
        //

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in (double width, double height) size, in (double v0, double v1, double v2, double v3) mean, bool swapRB, bool crop)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_10(nativeObj, scale, size.width, size.height, mean.v0, mean.v1, mean.v2, mean.v3, swapRB, crop);


        }

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in (double width, double height) size, in (double v0, double v1, double v2, double v3) mean, bool swapRB)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_11(nativeObj, scale, size.width, size.height, mean.v0, mean.v1, mean.v2, mean.v3, swapRB);


        }

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in (double width, double height) size, in (double v0, double v1, double v2, double v3) mean)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_12(nativeObj, scale, size.width, size.height, mean.v0, mean.v1, mean.v2, mean.v3);


        }

        /// <summary>
        ///  Set preprocessing parameters for frame.
        /// </summary>
        public void setInputParams(double scale, in (double width, double height) size)
        {
            ThrowIfDisposed();

            dnn_Model_setInputParams_13(nativeObj, scale, size.width, size.height);


        }

    }
}

#endif