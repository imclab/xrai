
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.Calib3dModule
{
    public partial class StereoBM : StereoMatcher
    {


        //
        // C++:  Rect cv::StereoBM::getROI1()
        //

        public (int x, int y, int width, int height) getROI1AsValueTuple()
        {
            ThrowIfDisposed();

            double[] tmpArray = new double[4];
            calib3d_StereoBM_getROI1_10(nativeObj, tmpArray);
            (int x, int y, int width, int height) retVal = ((int)tmpArray[0], (int)tmpArray[1], (int)tmpArray[2], (int)tmpArray[3]);

            return retVal;
        }


        //
        // C++:  void cv::StereoBM::setROI1(Rect roi1)
        //

        public void setROI1(in (int x, int y, int width, int height) roi1)
        {
            ThrowIfDisposed();

            calib3d_StereoBM_setROI1_10(nativeObj, roi1.x, roi1.y, roi1.width, roi1.height);


        }


        //
        // C++:  Rect cv::StereoBM::getROI2()
        //

        public (int x, int y, int width, int height) getROI2AsValueTuple()
        {
            ThrowIfDisposed();

            double[] tmpArray = new double[4];
            calib3d_StereoBM_getROI2_10(nativeObj, tmpArray);
            (int x, int y, int width, int height) retVal = ((int)tmpArray[0], (int)tmpArray[1], (int)tmpArray[2], (int)tmpArray[3]);

            return retVal;
        }


        //
        // C++:  void cv::StereoBM::setROI2(Rect roi2)
        //

        public void setROI2(in (int x, int y, int width, int height) roi2)
        {
            ThrowIfDisposed();

            calib3d_StereoBM_setROI2_10(nativeObj, roi2.x, roi2.y, roi2.width, roi2.height);


        }

    }
}
