
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.FaceModule
{
    public partial class Face
    {


        //
        // C++:  void cv::face::drawFacemarks(Mat& image, Mat points, Scalar color = Scalar(255,0,0))
        //

        /// <summary>
        ///  Utility to draw the detected facial landmark points
        /// </summary>
        /// <param name="image">
        /// The input image to be processed.
        /// </param>
        /// <param name="points">
        /// Contains the data of points which will be drawn.
        /// </param>
        /// <param name="color">
        /// The color of points in BGR format represented by cv::Scalar.
        /// </param>
        /// <remarks>
        ///  &lt;B&gt;Example of usage&lt;/B&gt;
        /// </remarks>
        /// <code language="c++">
        ///  std::vector&lt;Rect&gt; faces;
        ///  std::vector&lt;std::vector&lt;Point2f&gt; &gt; landmarks;
        ///  facemark-&gt;getFaces(img, faces);
        ///  facemark-&gt;fit(img, faces, landmarks);
        ///  for(int j=0;j&lt;rects.size();j++){
        ///      face::drawFacemarks(frame, landmarks[j], Scalar(0,0,255));
        ///  }
        /// </code>
        public static void drawFacemarks(Mat image, Mat points, in Vec4d color)
        {
            if (image != null) image.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            face_Face_drawFacemarks_10(image.nativeObj, points.nativeObj, color.Item1, color.Item2, color.Item3, color.Item4);


        }

    }
}
