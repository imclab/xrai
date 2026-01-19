
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.PhotoModule
{
    public partial class Photo
    {


        //
        // C++:  void cv::seamlessClone(Mat src, Mat dst, Mat mask, Point p, Mat& blend, int flags)
        //

        /// <summary>
        ///  Image editing tasks concern either global changes (color/intensity corrections, filters,
        ///  deformations) or local changes concerned to a selection. Here we are interested in achieving local
        ///  changes, ones that are restricted to a region manually selected (ROI), in a seamless and effortless
        ///  manner. The extent of the changes ranges from slight distortions to complete replacement by novel
        ///  content @cite PM03 .
        /// </summary>
        /// <param name="src">
        /// Input 8-bit 3-channel image.
        /// </param>
        /// <param name="dst">
        /// Input 8-bit 3-channel image.
        /// </param>
        /// <param name="mask">
        /// Input 8-bit 1 or 3-channel image.
        /// </param>
        /// <param name="p">
        /// Point in dst image where object is placed.
        /// </param>
        /// <param name="blend">
        /// Output image with the same size and type as dst.
        /// </param>
        /// <param name="flags">
        /// Cloning method that could be cv::NORMAL_CLONE, cv::MIXED_CLONE or cv::MONOCHROME_TRANSFER
        /// </param>
        public static void seamlessClone(Mat src, Mat dst, Mat mask, in (double x, double y) p, Mat blend, int flags)
        {
            if (src != null) src.ThrowIfDisposed();
            if (dst != null) dst.ThrowIfDisposed();
            if (mask != null) mask.ThrowIfDisposed();
            if (blend != null) blend.ThrowIfDisposed();

            photo_Photo_seamlessClone_10(src.nativeObj, dst.nativeObj, mask.nativeObj, p.x, p.y, blend.nativeObj, flags);


        }

    }
}
