using AOT;
using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if !OPENCV_DONT_USE_UNSAFE_CODE
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace OpenCVForUnity.UnityUtils
{
    public static partial class Utils
    {
        /// <summary>
        /// Returns this "OpenCV for Unity" version number.
        /// </summary>
        /// <returns>
        ///  this "OpenCV for Unity" version number
        /// </returns>
        public static string getVersion()
        {
            return "2.6.5";
        }

        #region metToTexture2D

        /// <summary>
        /// Converts an OpenCV Mat to a Unity Texture2D.
        /// </summary>
        /// <remarks>
        /// This method converts an OpenCV Mat to a Unity Texture2D. Conversion is possible even when the number of bytes per pixel differs, such as from Mat(8UC1) to Texture2D(RGBA32).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// Performance is optimal when the per-pixel data size and color order match, such as with Mat(8UC4) and Texture2D(RGBA32).
        /// If the texture format is not RGBA32, BGRA32, RGB24, Alpha8, or R8, the <a href="https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html">Texture2D.SetPixels32</a> and <a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a> methods are used. In such cases, it is recommended to use the <paramref name="pixels32Buffer"/> argument to avoid repeated memory allocations.
        /// </remarks>
        /// <param name="mat">
        /// The source Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE). For other CvTypes or color orders, use <see cref="matToTexture2DRaw">matToTexture2DRaw</see>.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D must have the same size as the source Mat.
        /// The destination Texture2D supports the following formats. (<a href="https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html">Texture2D.SetPixels32</a>)
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the Mat is flipped before conversion.
        /// The default is <c>true</c>, as the Mat must be flipped to align with the coordinate system of the destination Texture2D image.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Mat: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <param name="updateMipmaps">
        /// If <c>true</c>, mipmaps are recalculated after conversion.
        /// The default is <c>false</c>.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, system memory copy of a texture is released.
        /// The default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="mat"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void matToTexture2D(Mat mat, Texture2D texture2D, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            matToTexture2D(mat, texture2D, null, null, flip, flipCode, updateMipmaps, makeNoLongerReadable);
        }

        /// <summary>
        /// Converts an OpenCV Mat to a Unity Texture2D.
        /// </summary>
        /// <remarks>
        /// This method converts an OpenCV Mat to a Unity Texture2D. Conversion is possible even when the number of bytes per pixel differs, such as from Mat(8UC1) to Texture2D(RGBA32).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// Performance is optimal when the per-pixel data size and color order match, such as with Mat(8UC4) and Texture2D(RGBA32).
        /// If the texture format is not RGBA32, BGRA32, RGB24, Alpha8, or R8, the <a href="https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html">Texture2D.SetPixels32</a> and <a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a> methods are used. In such cases, it is recommended to use the <paramref name="pixels32Buffer"/> argument to avoid repeated memory allocations.
        /// </remarks>
        /// <param name="mat">
        /// The source Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE). For other CvTypes or color orders, use <see cref="matToTexture2DRaw">matToTexture2DRaw</see>.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D must have the same size as the source Mat.
        /// The destination Texture2D supports the following formats. (<a href="https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html">Texture2D.SetPixels32</a>)
        /// </param>
        /// <param name="pixels32Buffer">
        /// An optional array for receiving pixel data as Color32. Using this array helps avoid memory allocation each frame.
        /// Ensure the array is initialized to a length matching the texture’s width * height. (<a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a>)
        /// </param>
        /// <param name="rawTextureDataBuffer">
        /// An optional array for receiving raw texture data. This only works when the "OPENCV_DONT_USE_UNSAFE_CODE" symbol is defined and <paramref name="texture2D"/> has no mipmaps (mipmapCount == 1).
        /// Passing a byte array can help avoid memory allocation each frame. Ensure the array length matches the texture's raw data size. (<a href="https://docs.unity3d.com/ScriptReference/Texture2D.GetRawTextureData.html">Texture2D.GetRawTextureData</a>)
        /// The default value is <c>null</c>.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the Mat is flipped before conversion.
        /// The default is <c>true</c>, as the Mat must be flipped to align with the coordinate system of the destination Texture2D image.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Mat: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <param name="updateMipmaps">
        /// If <c>true</c>, mipmaps are recalculated after conversion.
        /// The default is <c>false</c>.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, system memory copy of a texture is released.
        /// The default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="mat"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static void matToTexture2D(Mat mat, Texture2D texture2D, Color32[] pixels32Buffer, byte[] rawTextureDataBuffer = null, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat.cols() != texture2D.width || mat.rows() != texture2D.height)
                throw new ArgumentException("The Texture2D must have the same size.");

            int type = mat.type();
            TextureFormat format = texture2D.format;
            if (!(type == CvType.CV_8UC1 || type == CvType.CV_8UC3 || type == CvType.CV_8UC4))
                throw new ArgumentException("The Mat must have the types 'CV_8UC4' (RGBA) , 'CV_8UC3' (RGB) or 'CV_8UC1' (GRAY).");

#if OPENCV_DONT_USE_UNSAFE_CODE
            if (((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))) && texture2D.mipmapCount == 1 && mat.isContinuous())
            {
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }
                texture2D.LoadRawTextureData((IntPtr)mat.dataAddr(), (int)(mat.total() * mat.elemSize()));
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return;
            }
#endif

            if (format == TextureFormat.RGBA32 || format == TextureFormat.BGRA32 || format == TextureFormat.RGB24 || format == TextureFormat.Alpha8 || format == TextureFormat.R8)
            {
#if !OPENCV_DONT_USE_UNSAFE_CODE
                unsafe
                {
                    OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(texture2D.GetRawTextureData<byte>()), (int)format, flip, flipCode);
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);

                return;
#else
                if (texture2D.mipmapCount == 1 && (rawTextureDataBuffer != null || pixels32Buffer == null))
                {
                    GCHandle rawTextureDataHandle;
                    if (rawTextureDataBuffer == null)
                    {
                        byte[] rawTextureData = texture2D.GetRawTextureData();
                        rawTextureDataHandle = GCHandle.Alloc(rawTextureData, GCHandleType.Pinned);
                        OpenCVForUnity_MatToTexture(mat.nativeObj, rawTextureDataHandle.AddrOfPinnedObject(), (int)format, flip, flipCode);
                        texture2D.LoadRawTextureData(rawTextureDataHandle.AddrOfPinnedObject(), rawTextureData.Length);
                    }
                    else
                    {
                        int textureChannels = 1;
                        switch (format)
                        {
                            case TextureFormat.RGBA32:
                            case TextureFormat.BGRA32:
                                textureChannels = 4;
                                break;
                            case TextureFormat.RGB24:
                                textureChannels = 3;
                                break;
                            case TextureFormat.Alpha8:
                            case TextureFormat.R8:
                            default:
                                textureChannels = 1;
                                break;
                        }
                        if (rawTextureDataBuffer.Length != texture2D.width * texture2D.height * textureChannels)
                            throw new ArgumentException("The rawTextureDataBuffer array length must match the size of texture2D data.");

                        rawTextureDataHandle = GCHandle.Alloc(rawTextureDataBuffer, GCHandleType.Pinned);
                        OpenCVForUnity_MatToTexture(mat.nativeObj, rawTextureDataHandle.AddrOfPinnedObject(), (int)format, flip, flipCode);
                        texture2D.LoadRawTextureData(rawTextureDataHandle.AddrOfPinnedObject(), rawTextureDataBuffer.Length);
                    }
                    texture2D.Apply(updateMipmaps, makeNoLongerReadable);
                    rawTextureDataHandle.Free();

                    return;
                }
#endif
            }

            //You can use SetPixels32 with the following texture formats:
            //https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Texture2D.SetPixels32.html
#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (pixels32Buffer == null)
            {
                Color32[] pixels32 = texture2D.GetPixels32();
                unsafe
                {
                    fixed (Color32* ptr = pixels32)
                    {
                        OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)ptr, (int)TextureFormat.RGBA32, flip, flipCode);
                    }
                }

                convertToGrayscaleIfFormatMatches(pixels32, texture2D.format);

                texture2D.SetPixels32(pixels32);
            }
            else
            {
                if (pixels32Buffer.Length != mat.total())
                    throw new ArgumentException("The pixels32Buffer array length must match the number of mat elements.");

                unsafe
                {
                    fixed (Color32* ptr = pixels32Buffer)
                    {
                        OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)ptr, (int)TextureFormat.RGBA32, flip, flipCode);
                    }
                }

                convertToGrayscaleIfFormatMatches(pixels32Buffer, texture2D.format);

                texture2D.SetPixels32(pixels32Buffer);
            }
            texture2D.Apply(updateMipmaps, makeNoLongerReadable);
#else
            GCHandle pixels32Handle;
            if (pixels32Buffer == null)
            {
                Color32[] pixels32 = texture2D.GetPixels32();

                pixels32Handle = GCHandle.Alloc(pixels32, GCHandleType.Pinned);
                OpenCVForUnity_MatToTexture(mat.nativeObj, pixels32Handle.AddrOfPinnedObject(), (int)TextureFormat.RGBA32, flip, flipCode);

                convertToGrayscaleIfFormatMatches(pixels32, texture2D.format);

                texture2D.SetPixels32(pixels32);
            }
            else
            {
                if (pixels32Buffer.Length != mat.total())
                    throw new ArgumentException("The pixels32Buffer array length must match the number of mat elements.");

                pixels32Handle = GCHandle.Alloc(pixels32Buffer, GCHandleType.Pinned);
                OpenCVForUnity_MatToTexture(mat.nativeObj, pixels32Handle.AddrOfPinnedObject(), (int)TextureFormat.RGBA32, flip, flipCode);

                convertToGrayscaleIfFormatMatches(pixels32Buffer, texture2D.format);

                texture2D.SetPixels32(pixels32Buffer);
            }
            texture2D.Apply(updateMipmaps, makeNoLongerReadable);
            pixels32Handle.Free();
#endif
        }

        /// <summary>
        /// Converts an OpenCV Mat to a Unity Texture2D, targeting the specified mipmap level.
        /// </summary>
        /// <remarks>
        /// This method converts an OpenCV Mat to a Unity Texture2D, targeting the specified mipmap level. Conversion is possible even when the number of bytes per pixel differs, such as from Mat(8UC1) to Texture2D(RGBA32).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// It is recommended to use the <paramref name="pixels32Buffer"/> argument to avoid repeated memory allocations.
        /// </remarks>
        /// <param name="mat">
        /// The source Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE). For other CvTypes or color orders, use <see cref="matToTexture2DRaw">matToTexture2DRaw</see>.
        /// The source Mat must be the same size as the mipmap of the destination Texture2D.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D most be supports mipmaps, as the conversion will target the<paramref name="mipLevel"/>.
        /// The destination Texture2D supports the following formats. (<a href="https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html">Texture2D.SetPixels32</a>)
        /// </param>
        /// <param name="mipLevel">
        /// The mipmap level to which the Mat will be converted. The level must be within the range supported by the destination Texture2D.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the Mat is flipped before conversion.
        /// The default is <c>true</c>, as the Mat must be flipped to align with the coordinate system of the destination Texture2D image.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Mat: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <param name="updateMipmaps">
        /// If <c>true</c>, mipmaps are recalculated after conversion.
        /// The default is <c>false</c>.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, system memory copy of a texture is released.
        /// The default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="mat"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void matToTexture2D(Mat mat, Texture2D texture2D, int mipLevel, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            matToTexture2D(mat, texture2D, mipLevel, null, flip, flipCode, updateMipmaps, makeNoLongerReadable);
        }

        /// <summary>
        /// Converts an OpenCV Mat to a Unity Texture2D, targeting the specified mipmap level.
        /// </summary>
        /// <remarks>
        /// This method converts an OpenCV Mat to a Unity Texture2D, targeting the specified mipmap level. Conversion is possible even when the number of bytes per pixel differs, such as from Mat(8UC1) to Texture2D(RGBA32).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// It is recommended to use the <paramref name="pixels32Buffer"/> argument to avoid repeated memory allocations.
        /// </remarks>
        /// <param name="mat">
        /// The source Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE). For other CvTypes or color orders, use <see cref="matToTexture2DRaw">matToTexture2DRaw</see>.
        /// The source Mat must be the same size as the mipmap of the destination Texture2D.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D most be supports mipmaps, as the conversion will target the<paramref name="mipLevel"/>.
        /// The destination Texture2D supports the following formats. (<a href="https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html">Texture2D.SetPixels32</a>)
        /// </param>
        /// <param name="mipLevel">
        /// The mipmap level to which the Mat will be converted. The level must be within the range supported by the destination Texture2D.
        /// </param>
        /// <param name="pixels32Buffer">
        /// An optional array for receiving pixel data as Color32. Using this array helps avoid memory allocation each frame.
        /// Ensure the array is initialized to a length matching the  texture’s mipmap width * height. (<a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a>)
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the Mat is flipped before conversion.
        /// The default is <c>true</c>, as the Mat must be flipped to align with the coordinate system of the destination Texture2D image.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Mat: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <param name="updateMipmaps">
        /// If <c>true</c>, mipmaps are recalculated after conversion.
        /// The default is <c>false</c>.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, system memory copy of a texture is released.
        /// The default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="mat"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static void matToTexture2D(Mat mat, Texture2D texture2D, int mipLevel, Color32[] pixels32Buffer, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            int type = mat.type();
            TextureFormat format = texture2D.format;
            if (!(type == CvType.CV_8UC1 || type == CvType.CV_8UC3 || type == CvType.CV_8UC4))
                throw new ArgumentException("The Mat must have the types 'CV_8UC4' (RGBA) , 'CV_8UC3' (RGB) or 'CV_8UC1' (GRAY).");

            //You can use SetPixels32 with the following texture formats:
            //https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Texture2D.SetPixels32.html
#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (pixels32Buffer == null)
            {
                Color32[] pixels32 = texture2D.GetPixels32(mipLevel);

                if (pixels32.Length != mat.total())
                    throw new ArgumentException("The number of mat elements must match the Color32 array length of the specified mipLevel.");

                unsafe
                {
                    fixed (Color32* ptr = pixels32)
                    {
                        OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)ptr, (int)TextureFormat.RGBA32, flip, flipCode);
                    }
                }

                convertToGrayscaleIfFormatMatches(pixels32, texture2D.format);

                texture2D.SetPixels32(pixels32, mipLevel);
            }
            else
            {
                if (pixels32Buffer.Length != mat.total())
                    throw new ArgumentException("The pixels32Buffer array length must match the number of mat elements.");

                unsafe
                {
                    fixed (Color32* ptr = pixels32Buffer)
                    {
                        OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)ptr, (int)TextureFormat.RGBA32, flip, flipCode);
                    }
                }

                convertToGrayscaleIfFormatMatches(pixels32Buffer, texture2D.format);

                texture2D.SetPixels32(pixels32Buffer, mipLevel);
            }
            texture2D.Apply(updateMipmaps, makeNoLongerReadable);
#else
            GCHandle pixels32Handle;
            if (pixels32Buffer == null)
            {
                Color32[] pixels32 = texture2D.GetPixels32(mipLevel);

                if (pixels32.Length != mat.total())
                    throw new ArgumentException("The number of mat elements must match the Color32 array length of the specified mipLevel.");

                pixels32Handle = GCHandle.Alloc(pixels32, GCHandleType.Pinned);
                OpenCVForUnity_MatToTexture(mat.nativeObj, pixels32Handle.AddrOfPinnedObject(), (int)TextureFormat.RGBA32, flip, flipCode);

                convertToGrayscaleIfFormatMatches(pixels32, texture2D.format);

                texture2D.SetPixels32(pixels32, mipLevel);
            }
            else
            {
                if (pixels32Buffer.Length != mat.total())
                    throw new ArgumentException("The pixels32Buffer array length must match the number of mat elements.");

                pixels32Handle = GCHandle.Alloc(pixels32Buffer, GCHandleType.Pinned);
                OpenCVForUnity_MatToTexture(mat.nativeObj, pixels32Handle.AddrOfPinnedObject(), (int)TextureFormat.RGBA32, flip, flipCode);

                convertToGrayscaleIfFormatMatches(pixels32Buffer, texture2D.format);

                texture2D.SetPixels32(pixels32Buffer, mipLevel);
            }
            texture2D.Apply(updateMipmaps, makeNoLongerReadable);
            pixels32Handle.Free();
#endif
        }

        private static void convertToGrayscaleIfFormatMatches(Color32[] pixels32, TextureFormat format)
        {
            if (format == TextureFormat.Alpha8 || format == TextureFormat.R8 || format == TextureFormat.R16 || format == TextureFormat.RFloat || format == TextureFormat.RHalf)
            {
#if NET_STANDARD_2_1
                Span<Color32> pixelSpan = pixels32;
                foreach (ref Color32 color in pixelSpan)
                {
                    float grayValue = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
                    byte gray = (byte)(grayValue + 0.5f);// Round up to the nearest integer (based on OpenCV's cvRound)
                    color = new Color32(gray, gray, gray, gray);
                }
#else
                for (int i = 0; i < pixels32.Length; i++)
                {
                    Color32 color = pixels32[i];
                    float grayValue = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
                    byte gray = (byte)(grayValue + 0.5f);// Round up to the nearest integer (based on OpenCV's cvRound)
                    pixels32[i] = new Color32(gray, gray, gray, gray);
                }
#endif
            }
        }

        /// <summary>
        /// @deprecated Use matToTexture2DRaw method instead.
        /// </summary>
        [Obsolete("This method is deprecated. Use matToTexture2DRaw method instead.")]
        public static int fastMatToTexture2D(Mat mat, Texture2D texture2D, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat.dims() != 2)
                throw new ArgumentException("mat.dims() != 2");

#if !OPENCV_DONT_USE_UNSAFE_CODE
            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetRawTextureData<byte>();

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                unsafe
                {
                    OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawTexData), (int)texture2D.format, flip, flipCode);
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);

                return matBytesNum;
            }
            else
            {
                int bytesNum = 0;

                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }
                unsafe
                {
                    bytesNum = OpenCVForUnity_MatDataToByteArray(mat.nativeObj, rawTexData.Length, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawTexData));
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
#else
            if (!mat.isContinuous())
            {
                Debug.LogError("mat.isContinuous() must be true.");
                return 0;
            }

            var rawTexData = texture2D.GetRawTextureData<byte>();
            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length > matBytesNum)
            {
                Debug.LogError("The size of the mat data must fill the entire texture according to its width, height and mipmapCount, and the data layout must match the texture format.");
                return 0;
            }

            if (flip)
            {
                Core.flip(mat, mat, flipCode);
            }
            texture2D.LoadRawTextureData((IntPtr)mat.dataAddr(), rawTexData.Length);
            texture2D.Apply(updateMipmaps, makeNoLongerReadable);
            if (flip)
            {
                Core.flip(mat, mat, flipCode);
            }

            return rawTexData.Length;
#endif
        }

        /// <summary>
        /// Copies raw data from an OpenCV Mat to a Unity Texture2D.
        /// </summary>
        /// <remarks>
        /// This method copies raw data from an OpenCV Mat to a Unity Texture2D.
        /// There are no specific requirements for the size or type of the Mat and Texture2D; data is copied up to the maximum size that fits within the data size of the destination Texture2D, including mipmaps.
        /// A common use case for this method is writing a Mat with BGRA color order to a Texture2D in the BGRA32 format.
        /// If the "OPENCV_DONT_USE_UNSAFE_CODE" symbol is defined, the following operating conditions are added: mat.isContinuous() == true, and the data size of Texture2D is the same as or larger than the data size of Mat.
        /// </remarks>
        /// <param name="mat">
        /// The source Mat must be 2-dimensional.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the Mat is flipped before copies.
        /// The default is <c>true</c>, as the Mat must be flipped to align with the coordinate system of the destination Texture2D image.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Mat: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <param name="updateMipmaps">
        /// If <c>true</c>, mipmaps are recalculated after copies.
        /// The default is <c>false</c>.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, system memory copy of a texture is released.
        /// The default is <c>false</c>.
        /// </param>
        /// <returns>
        /// Returns the number of bytes actually written to the destination Texture2D.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="mat"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static int matToTexture2DRaw(Mat mat, Texture2D texture2D, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat.dims() != 2)
                throw new ArgumentException("mat.dims() != 2");

#if !OPENCV_DONT_USE_UNSAFE_CODE
            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetRawTextureData<byte>();

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                unsafe
                {
                    OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawTexData), (int)texture2D.format, flip, flipCode);
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);

                return matBytesNum;
            }
            else
            {
                int bytesNum = 0;

                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }
                unsafe
                {
                    bytesNum = OpenCVForUnity_MatDataToByteArray(mat.nativeObj, rawTexData.Length, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawTexData));
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
#else
            if (!mat.isContinuous())
            {
                Debug.LogError("mat.isContinuous() must be true.");
                return 0;
            }

            var rawTexData = texture2D.GetRawTextureData<byte>();
            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length > matBytesNum)
            {
                Debug.LogError("The size of the mat data must fill the entire texture according to its width, height and mipmapCount, and the data layout must match the texture format.");
                return 0;
            }

            if (flip)
            {
                Core.flip(mat, mat, flipCode);
            }
            texture2D.LoadRawTextureData((IntPtr)mat.dataAddr(), rawTexData.Length);
            texture2D.Apply(updateMipmaps, makeNoLongerReadable);
            if (flip)
            {
                Core.flip(mat, mat, flipCode);
            }

            return rawTexData.Length;
#endif
        }

#if !OPENCV_DONT_USE_UNSAFE_CODE
        /// <summary>
        /// Copies raw data from an OpenCV Mat to a Unity Texture2D, targeting the specified mipmap level.
        /// </summary>
        /// <remarks>
        /// This method copies raw data from an OpenCV Mat to a Unity Texture2D, targeting the specified mipmap level.
        /// There are no specific requirements for the size or type of the Mat and Texture2D; data is copied up to the maximum size that fits within the data size of the mipmap of the destination Texture2D.
        /// </remarks>
        /// <param name="mat">
        /// The source Mat must be 2-dimensional.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D.
        /// </param>
        /// <param name="mipLevel">
        /// The mipmap level to which the Mat will be converted. The level must be within the range supported by the destination Texture2D.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the Mat is flipped before copies.
        /// The default is <c>true</c>, as the Mat must be flipped to align with the coordinate system of the destination Texture2D image.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Mat: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <param name="updateMipmaps">
        /// If <c>true</c>, mipmaps are recalculated after copies.
        /// The default is <c>false</c>.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, system memory copy of a texture is released.
        /// The default is <c>false</c>.
        /// </param>
        /// <returns>
        /// Returns the number of bytes actually written to the destination Texture2D.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="mat"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static int matToTexture2DRaw(Mat mat, Texture2D texture2D, int mipLevel, bool flip = true, int flipCode = 0, bool updateMipmaps = false, bool makeNoLongerReadable = false)
        {
            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat.dims() != 2)
                throw new ArgumentException("mat.dims() != 2");

            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetPixelData<byte>(mipLevel);

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                unsafe
                {
                    OpenCVForUnity_MatToTexture(mat.nativeObj, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawTexData), (int)texture2D.format, flip, flipCode);
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);

                return matBytesNum;
            }
            else
            {
                int bytesNum = 0;

                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }
                unsafe
                {
                    bytesNum = OpenCVForUnity_MatDataToByteArray(mat.nativeObj, rawTexData.Length, (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawTexData));
                }
                texture2D.Apply(updateMipmaps, makeNoLongerReadable);
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
        }
#endif

        #endregion

        #region texture2DToMat

        /// <summary>
        /// Converts a Unity Texture2D to an OpenCV Mat.
        /// </summary>
        /// <remarks>
        /// This method converts a Unity Texture2D to an OpenCV Mat. Conversion is possible even when the number of bytes per pixel differs, such as from Texture2D(RGBA32) to Mat(8UC1).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// Performance is optimal when the per-pixel data size and color order match, such as with Texture2D(RGBA32) and Mat(8UC4).
        /// If the texture format is not RGBA32, BGRA32, RGB24, Alpha8, or R8, the <a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a> methods are used.
        /// </remarks>
        /// <param name="texture2D">
        /// The source Texture2D must have the same size as the destination Mat.
        /// The source Texture2D supports the following formats. (<a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a>)
        /// </param>
        /// <param name="mat">
        /// The destination Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE). For other CvTypes or color orders, use <see cref="texture2DToMatRaw">texture2DToMatRaw</see>.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the pixel data retrieved from the Texture2D is flipped before conversion. 
        /// The default is <c>true</c>, as the pixel data must be flipped to align with the coordinate system of the destination Mat.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Textrue2D image: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="texture2D"/> or <paramref name="mat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static void texture2DToMat(Texture2D texture2D, Mat mat, bool flip = true, int flipCode = 0)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (mat.cols() != texture2D.width || mat.rows() != texture2D.height)
                throw new ArgumentException("The Mat must have the same size.");

            int type = mat.type();
            TextureFormat format = texture2D.format;
            if (!(type == CvType.CV_8UC1 || type == CvType.CV_8UC3 || type == CvType.CV_8UC4))
                throw new ArgumentException("The Mat must have the types 'CV_8UC4' (RGBA) , 'CV_8UC3' (RGB) or 'CV_8UC1' (GRAY).");

            if (format == TextureFormat.RGBA32 || format == TextureFormat.BGRA32 || format == TextureFormat.RGB24 || format == TextureFormat.Alpha8 || format == TextureFormat.R8)
            {
#if !OPENCV_DONT_USE_UNSAFE_CODE
                unsafe
                {
                    OpenCVForUnity_TextureToMat((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(texture2D.GetRawTextureData<byte>()), mat.nativeObj, (int)format, flip, flipCode);
                }
#else
                GCHandle RawTextureDataHandle = GCHandle.Alloc(texture2D.GetRawTextureData(), GCHandleType.Pinned);
                OpenCVForUnity_TextureToMat(RawTextureDataHandle.AddrOfPinnedObject(), mat.nativeObj, (int)format, flip, flipCode);
                RawTextureDataHandle.Free();
#endif

                return;
            }

            Color32[] pixels32 = texture2D.GetPixels32();

            convertToRGBAIfFormatMatches(pixels32, texture2D.format);

#if !OPENCV_DONT_USE_UNSAFE_CODE
            unsafe
            {
                fixed (Color32* ptr = pixels32)
                {
                    OpenCVForUnity_TextureToMat((IntPtr)ptr, mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);
                }
            }
#else
            GCHandle pixels32Handle = GCHandle.Alloc(pixels32, GCHandleType.Pinned);
            OpenCVForUnity_TextureToMat(pixels32Handle.AddrOfPinnedObject(), mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);
            pixels32Handle.Free();
#endif
        }

        /// <summary>
        /// Converts a Unity Texture2D to an OpenCV Mat, targeting the specified mipmap level.
        /// </summary>
        /// <remarks>
        /// This method converts a Unity Texture2D to an OpenCV Mat, targeting the specified mipmap level. Conversion is possible even when the number of bytes per pixel differs, such as from Texture2D(RGBA32) to Mat(8UC1).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// </remarks>
        /// <param name="texture2D">
        /// The source Texture2D supports the following formats. (<a href="http://docs.unity3d.com/ScriptReference/Texture2D.GetPixels32.html">Texture2D.GetPixels32</a>)
        /// The source Texture2D most be supports mipmaps, as the conversion will target the<paramref name="mipLevel"/>.
        /// </param>
        /// <param name="mat">
        /// The destination Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE). For other CvTypes or color orders, use <see cref="texture2DToMatRaw">texture2DToMatRaw</see>.
        /// The destination Mat must be the same size as the mipmap of the destination Texture2D.
        /// </param>
        /// <param name="mipLevel">
        /// The mipmap level to which the Mat will be converted. The level must be within the range supported by the source Texture2D.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the pixel data retrieved from the Texture2D is flipped before conversion. 
        /// The default is <c>true</c>, as the pixel data must be flipped to align with the coordinate system of the destination Mat.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Textrue2D image: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="texture2D"/> or <paramref name="mat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static void texture2DToMat(Texture2D texture2D, Mat mat, int mipLevel, bool flip = true, int flipCode = 0)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            int type = mat.type();
            TextureFormat format = texture2D.format;
            if (!(type == CvType.CV_8UC1 || type == CvType.CV_8UC3 || type == CvType.CV_8UC4))
                throw new ArgumentException("The Mat object must have the types 'CV_8UC4' (RGBA) , 'CV_8UC3' (RGB) or 'CV_8UC1' (GRAY).");

            Color32[] pixels32 = texture2D.GetPixels32(mipLevel);
            if (pixels32.Length != mat.total())
                throw new ArgumentException("The number of mat elements must match the Color32 array length of the specified mipLevel.");

            convertToRGBAIfFormatMatches(pixels32, texture2D.format);

#if !OPENCV_DONT_USE_UNSAFE_CODE
            unsafe
            {
                fixed (Color32* ptr = pixels32)
                {
                    OpenCVForUnity_TextureToMat((IntPtr)ptr, mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);
                }
            }
#else
            GCHandle pixels32Handle = GCHandle.Alloc(pixels32, GCHandleType.Pinned);
            OpenCVForUnity_TextureToMat(pixels32Handle.AddrOfPinnedObject(), mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);
            pixels32Handle.Free();
#endif
        }

        private static void convertToRGBAIfFormatMatches(Color32[] pixels32, TextureFormat format)
        {
            if (format == TextureFormat.Alpha8)
            {
#if NET_STANDARD_2_1
                Span<Color32> pixelSpan = pixels32;
                foreach (ref Color32 color in pixelSpan)
                {
                    color = new Color32(color.a, color.a, color.a, color.a);
                }
#else
                for (int i = 0; i < pixels32.Length; i++)
                {
                    Color32 color = pixels32[i];
                    pixels32[i] = new Color32(color.a, color.a, color.a, color.a);
                }
#endif
            }
            else if (format == TextureFormat.R8 || format == TextureFormat.R16 || format == TextureFormat.RFloat || format == TextureFormat.RHalf)
            {
#if NET_STANDARD_2_1
                Span<Color32> pixelSpan = pixels32;
                foreach (ref Color32 color in pixelSpan)
                {
                    color = new Color32(color.r, color.r, color.r, color.r);
                }
#else
                for (int i = 0; i < pixels32.Length; i++)
                {
                    Color32 color = pixels32[i];
                    pixels32[i] = new Color32(color.r, color.r, color.r, color.r);
                }
#endif
            }
        }

        /// <summary>
        /// @deprecated Use texture2DToMatRaw method instead.
        /// </summary>
        [Obsolete("This method is deprecated. Use texture2DToMatRaw method instead.")]
        public static int fastTexture2DToMat(Texture2D texture2D, Mat mat, bool flip = true, int flipCode = 0)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (mat.dims() != 2)
                throw new ArgumentException("mat.dims() != 2");

#if !OPENCV_DONT_USE_UNSAFE_CODE
            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetRawTextureData<byte>();

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                unsafe
                {
                    OpenCVForUnity_TextureToMat((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTexData), mat.nativeObj, (int)texture2D.format, flip, flipCode);
                }

                return matBytesNum;
            }
            else
            {
                int bytesNum = 0;

                unsafe
                {
                    bytesNum = OpenCVForUnity_ByteArrayToMatData((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTexData), rawTexData.Length, mat.nativeObj);
                }
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
#else
            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetRawTextureData();

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                GCHandle RawTextureDataHandle = GCHandle.Alloc(rawTexData, GCHandleType.Pinned);
                OpenCVForUnity_TextureToMat(RawTextureDataHandle.AddrOfPinnedObject(), mat.nativeObj, (int)texture2D.format, flip, flipCode);
                RawTextureDataHandle.Free();

                return matBytesNum;
            }
            else
            {
                GCHandle RawTextureDataHandle = GCHandle.Alloc(rawTexData, GCHandleType.Pinned);
                int bytesNum = OpenCVForUnity_ByteArrayToMatData(RawTextureDataHandle.AddrOfPinnedObject(), rawTexData.Length, mat.nativeObj);
                RawTextureDataHandle.Free();
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
#endif
        }

        /// <summary>
        /// Copies raw data from a Unity Texture2D to an OpenCV Mat.
        /// </summary>
        /// <remarks>
        /// This method copies raw data from a Unity Texture2D to an OpenCV Mat.
        /// There are no specific requirements for the size or type of the Texture2D and Mat; data is copied up to the maximum size that fits within the data size of the destination Mat.
        /// A common use case for this method is writing a Texture2D in the BGRA32 format to a Mat with BGRA color order.
        /// </remarks>
        /// <param name="texture2D">
        /// The source Texture2D.
        /// </param>
        /// <param name="mat">
        /// The destination Mat must be 2-dimensional.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the pixel data retrieved from the Texture2D is flipped before copies.
        /// The default is <c>true</c>, as  the pixel data must be flipped to align with the coordinate system of the destination Mat.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Textrue2D image: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <returns>
        /// Returns the number of bytes actually written to the destination Mat.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="texture2D"/> or <paramref name="mat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static int texture2DToMatRaw(Texture2D texture2D, Mat mat, bool flip = true, int flipCode = 0)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (mat.dims() != 2)
                throw new ArgumentException("mat.dims() != 2");

#if !OPENCV_DONT_USE_UNSAFE_CODE
            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetRawTextureData<byte>();

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                unsafe
                {
                    OpenCVForUnity_TextureToMat((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTexData), mat.nativeObj, (int)texture2D.format, flip, flipCode);
                }

                return matBytesNum;
            }
            else
            {
                int bytesNum = 0;

                unsafe
                {
                    bytesNum = OpenCVForUnity_ByteArrayToMatData((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTexData), rawTexData.Length, mat.nativeObj);
                }
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
#else
            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetRawTextureData();

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                GCHandle RawTextureDataHandle = GCHandle.Alloc(rawTexData, GCHandleType.Pinned);
                OpenCVForUnity_TextureToMat(RawTextureDataHandle.AddrOfPinnedObject(), mat.nativeObj, (int)texture2D.format, flip, flipCode);
                RawTextureDataHandle.Free();

                return matBytesNum;
            }
            else
            {
                GCHandle RawTextureDataHandle = GCHandle.Alloc(rawTexData, GCHandleType.Pinned);
                int bytesNum = OpenCVForUnity_ByteArrayToMatData(RawTextureDataHandle.AddrOfPinnedObject(), rawTexData.Length, mat.nativeObj);
                RawTextureDataHandle.Free();
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
#endif
        }

#if !OPENCV_DONT_USE_UNSAFE_CODE
        /// <summary>
        /// Copies raw data from a Unity Texture2D to an OpenCV Mat, targeting the specified mipmap level.
        /// </summary>
        /// <remarks>
        /// This method copies raw data from a Unity Texture2D to an OpenCV Mat, targeting the specified mipmap level.
        /// There are no specific requirements for the size or type of the Texture2D and Mat; data is copied up to the maximum size that fits within the data size of the mipmap of the destination Mat.
        /// </remarks>
        /// <param name="texture2D">
        /// The source Texture2D.
        /// </param>
        /// <param name="mat">
        /// The destination Mat must be 2-dimensional.
        /// </param>
        /// <param name="mipLevel">
        /// The mipmap level to which the Mat will be converted. The level must be within the range supported by the source Texture2D.
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the pixel data retrieved from the Texture2D is flipped before copies.
        /// The default is <c>true</c>, as  the pixel data must be flipped to align with the coordinate system of the destination Mat.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the Textrue2D image: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode &gt; 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode &lt; 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <returns>
        /// Returns the number of bytes actually written to the destination Mat.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="texture2D"/> or <paramref name="mat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static int texture2DToMatRaw(Texture2D texture2D, Mat mat, int mipLevel, bool flip = true, int flipCode = 0)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (mat.dims() != 2)
                throw new ArgumentException("mat.dims() != 2");

            int type = mat.type();
            TextureFormat format = texture2D.format;
            var rawTexData = texture2D.GetPixelData<byte>(mipLevel);

            int matBytesNum = (int)(mat.total() * mat.elemSize());

            if (rawTexData.Length >= matBytesNum &&
                ((type == CvType.CV_8UC4 && format == TextureFormat.RGBA32) || (type == CvType.CV_8UC3 && format == TextureFormat.RGB24) || (type == CvType.CV_8UC1 && (format == TextureFormat.Alpha8 || format == TextureFormat.R8))))
            {
                unsafe
                {
                    OpenCVForUnity_TextureToMat((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTexData), mat.nativeObj, (int)texture2D.format, flip, flipCode);
                }

                return matBytesNum;
            }
            else
            {
                int bytesNum = 0;

                unsafe
                {
                    bytesNum = OpenCVForUnity_ByteArrayToMatData((IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTexData), rawTexData.Length, mat.nativeObj);
                }
                if (flip)
                {
                    Core.flip(mat, mat, flipCode);
                }

                return bytesNum;
            }
        }
#endif

        #endregion

        #region webCamTextureToMat

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

        /// <summary>
        /// Converts a Unity WebCamTexture to an OpenCV Mat.
        /// </summary>
        /// <remarks>
        /// This method converts a Unity WebCamTexture image to an OpenCV Mat. Conversion is possible even when the number of bytes per pixel differs, such as from WebCamTexture(RGBA32) to Mat(8UC1).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// Performance is optimal when the per-pixel data size and color order match, such as with Texture2D(RGBA32) and Mat(8UC4).
        /// It is recommended to use the <paramref name="pixels32Buffer"/> argument to avoid repeated memory allocations.
        /// </remarks>
        /// <param name="webCamTexture">
        /// The source WebCamTexture must have the same size as the destination Mat.
        /// </param>
        /// <param name="mat">
        /// The destination Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE).
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the pixel data retrieved from the WebCamTexture is flipped before conversion.
        /// The default is <c>true</c>, as the pixel data must be flipped to align with the coordinate system of the destination Mat.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the WebCamTexture image: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode > 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode < 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="webCamTexture"/> or <paramref name="mat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void webCamTextureToMat(WebCamTexture webCamTexture, Mat mat, bool flip = true, int flipCode = 0)
        {
            webCamTextureToMat(webCamTexture, mat, null, flip, flipCode);
        }

        /// <summary>
        /// Converts a Unity WebCamTexture to an OpenCV Mat.
        /// </summary>
        /// <remarks>
        /// This method converts a Unity WebCamTexture image to an OpenCV Mat. Conversion is possible even when the number of bytes per pixel differs, such as from WebCamTexture(RGBA32) to Mat(8UC1).
        /// In the case of multi-channel color to 1-channel, it is converted to grayscale. 
        /// Performance is optimal when the per-pixel data size and color order match, such as with Texture2D(RGBA32) and Mat(8UC4).
        /// It is recommended to use the <paramref name="pixels32Buffer"/> argument to avoid repeated memory allocations.
        /// </remarks>
        /// <param name="webCamTexture">
        /// The source WebCamTexture must have the same size as the destination Mat.
        /// </param>
        /// <param name="mat">
        /// The destination Mat must be 2-dimensional, with a CvType of 'CV_8UC4' (RGBA), 'CV_8UC3' (RGB), or 'CV_8UC1' (GRAYSCALE).
        /// </param>
        /// <param name="pixels32Buffer">
        /// An optional array for receiving pixel data as Color32. Using this array helps avoid memory allocation each frame.
        /// Ensure the array is initialized to a length matching the  texture’s width * height. (<a href="http://docs.unity3d.com/ScriptReference/WebCamTexture.GetPixels32.html">WebCamTexture.GetPixels32</a>)
        /// </param>
        /// <param name="flip">
        /// If <c>true</c>, the pixel data retrieved from the WebCamTexture is flipped before conversion.
        /// The default is <c>true</c>, as the pixel data must be flipped to align with the coordinate system of the destination Mat.
        /// </param>
        /// <param name="flipCode">
        /// Specifies how to flip the WebCamTexture image: Vertical flipping of the image (flipCode == 0) to flip around the x-axis,
        /// horizontal flipping of the image (flipCode > 0, e.g., 1) to flip around the y-axis, and simultaneous horizontal and vertical flipping (flipCode < 0, e.g., -1) to flip around both axes.
        /// The default is <c>0</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="webCamTexture"/> or <paramref name="mat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static void webCamTextureToMat(WebCamTexture webCamTexture, Mat mat, Color32[] pixels32Buffer, bool flip = true, int flipCode = 0)
        {
            if (webCamTexture == null)
                throw new ArgumentNullException(nameof(webCamTexture));

            if (mat == null)
                throw new ArgumentNullException(nameof(mat));
            if (mat != null)
                mat.ThrowIfDisposed();

            if (mat.cols() != webCamTexture.width || mat.rows() != webCamTexture.height)
                throw new ArgumentException("The Mat must have the same size.");

            int type = mat.type();
            if (!(type == CvType.CV_8UC1 || type == CvType.CV_8UC3 || type == CvType.CV_8UC4))
                throw new ArgumentException("The Mat must have the types 'CV_8UC4' (RGBA) , 'CV_8UC3' (RGB) or 'CV_8UC1' (GRAY).");

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (pixels32Buffer == null)
            {
                unsafe
                {
                    fixed (Color32* ptr = webCamTexture.GetPixels32())
                    {
                        OpenCVForUnity_TextureToMat((IntPtr)ptr, mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);
                    }
                }
            }
            else
            {
                webCamTexture.GetPixels32(pixels32Buffer);
                unsafe
                {
                    fixed (Color32* ptr = pixels32Buffer)
                    {
                        OpenCVForUnity_TextureToMat((IntPtr)ptr, mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);
                    }
                }
            }
#else
            GCHandle pixels32Handle;
            if (pixels32Buffer == null)
            {

                Color32[] colors = webCamTexture.GetPixels32();

                pixels32Handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            }
            else
            {
                webCamTexture.GetPixels32(pixels32Buffer);

                pixels32Handle = GCHandle.Alloc(pixels32Buffer, GCHandleType.Pinned);
            }

            OpenCVForUnity_TextureToMat(pixels32Handle.AddrOfPinnedObject(), mat.nativeObj, (int)TextureFormat.RGBA32, flip, flipCode);

            pixels32Handle.Free();
#endif
        }

#endif //!OPENCV_DONT_USE_WEBCAMTEXTURE_API

        #endregion

        #region TextureToTexture2D

        /// <summary>
        /// Converts a Texture to a Texture2D.
        /// </summary>
        /// <remarks>
        /// This method converts a Texture to a Texture2D.
        /// The Texture and the Texture2D must be the same size.
        /// </remarks>
        /// <param name="texture">
        /// The source Texture.
        /// </param>
        /// <param name="texture2D">
        /// The destination Texture2D. It must have a TextureFormat of RGBA32, ARGB32, RGB24, RGBAFloat, or RGBAHalf.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="texture"/> or <paramref name="texture2D"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static void textureToTexture2D(Texture texture, Texture2D texture2D)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (texture.width != texture2D.width || texture.height != texture2D.height)
                throw new ArgumentException("The Texture and the Texture2D must be the same size.");

            RenderTexture prevRT = RenderTexture.active;

            if (texture is RenderTexture)
            {
                RenderTexture.active = (RenderTexture)texture;
                texture2D.ReadPixels(new UnityEngine.Rect(0f, 0f, texture.width, texture.height), 0, 0, false);
                texture2D.Apply(false, false);
            }
            else
            {
                RenderTexture tempRT = RenderTexture.GetTemporary(texture.width, texture.height, 0, (texture.isDataSRGB) ? GraphicsFormat.B8G8R8A8_SRGB : GraphicsFormat.B8G8R8A8_UNorm);
                Graphics.Blit(texture, tempRT);

                RenderTexture.active = tempRT;
                texture2D.ReadPixels(new UnityEngine.Rect(0f, 0f, texture.width, texture.height), 0, 0, false);
                texture2D.Apply(false, false);
                RenderTexture.ReleaseTemporary(tempRT);
            }

            RenderTexture.active = prevRT;
        }

        #endregion

        #region getFilePath

        /// <summary>
        /// Gets the readable path of a file in the "StreamingAssets" directory.
        /// </summary>
        /// <remarks>
        /// Provide a relative file path based on the "StreamingAssets" directory. e.g., "foobar.txt" or "hogehoge/foobar.txt".
        /// [Android] The target file that exists in the "StreamingAssets" directory is copied to the the Application.persistentDataPath directory.
        /// [WebGL] If the target file has not yet been copied to WebGL's virtual filesystem, it is necessary to use <see cref="getFilePathAsync">getFilePathAsync</see> of <see cref="getFilePathAsyncTask">getFilePathAsyncTask</see> at first.
        /// </remarks>
        /// <param name="filepath">
        /// A file path relative to the "StreamingAssets" directory.
        /// </param>
        /// <param name="refresh">
        /// [Android] If false, the file is not copied if it already exists. If true, the file is always copied.
        /// </param>
        /// <param name="timeout">
        /// [Android] Sets the UnityWebRequest to abort after the specified number of seconds. If set to 0, no timeout is applied. The default is 0.
        /// </param>
        /// <returns>
        /// Returns the readable file path in case of success and returns <c>string.Empty</c> in case of error.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filepath"/> is null.
        /// </exception>
        public static string getFilePath(string filepath, bool refresh = false, int timeout = 0)
        {
            if (filepath == null)
                throw new ArgumentNullException(nameof(filepath));

            filepath = filepath.TrimStart(chTrims);

            if (string.IsNullOrEmpty(filepath) || string.IsNullOrEmpty(Path.GetExtension(filepath)))
                return string.Empty;

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
#if UNITY_ANDROID
            string srcPath = Path.Combine(Application.streamingAssetsPath, filepath);
            string destPath = Path.Combine(Application.persistentDataPath, "opencvforunity");
            destPath = Path.Combine(destPath, filepath);

            if (!refresh && File.Exists(destPath))
                return destPath;

            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(srcPath))
            {
                if (timeout > 0)
                    request.timeout = timeout;
                request.SendWebRequest();

                while (!request.isDone) {; }

                switch (request.result)
                {
                    case UnityEngine.Networking.UnityWebRequest.Result.Success:
                        // create directory and write downlorded data
                        string dirPath = Path.GetDirectoryName(destPath);
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                        File.WriteAllBytes(destPath, request.downloadHandler.data);
                        return destPath;
                    case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
                    case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
                    case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError($"UnityWebRequest error occurred: {filepath}, {request.error}, {request.responseCode}");
                        return string.Empty;
                    default:
                        return string.Empty;
                }
            }
#else // UNITY_WEBGL
            string destPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "opencvforunity");
            destPath = Path.Combine(destPath, filepath);
            return File.Exists(destPath) ? destPath : string.Empty;
#endif
#else // (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            string destPath = Path.Combine(Application.streamingAssetsPath, filepath);
            return File.Exists(destPath) ? destPath : string.Empty;
#endif // (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
        }

        /// <summary>
        /// Gets the multiple readable paths of a file in the "StreamingAssets" directory.
        /// </summary>
        /// <remarks>
        /// Provide a relative file path based on the "StreamingAssets" directory.  e.g., "foobar.txt" or "hogehoge/foobar.txt".
        /// [Android] The target file that exists in the "StreamingAssets" directory is copied to the the Application.persistentDataPath directory.
        /// [WebGL] If the target file has not yet been copied to WebGL's virtual filesystem, it is necessary to use <see cref="getMultipleFilePathsAsync">getMultipleFilePathsAsync</see> of <see cref="getMultipleFilePathsAsyncTask">getMultipleFilePathsAsyncTask</see> at first.
        /// </remarks>
        /// <param name="filepaths">
        /// The list of file paths relative to the "StreamingAssets" directory.
        /// </param>
        /// <param name="refresh">
        /// [Android] If false, the file is not copied if it already exists. If true, the file is always copied.
        /// </param>
        /// <param name="timeout">
        /// [Android] Sets the UnityWebRequest to abort after the specified number of seconds. If set to 0, no timeout is applied. The default is 0.
        /// </param>
        /// <returns>
        /// Returns the list of readable file paths in case of success and returns <c>string.Empty</c> in case of error.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filepath"/> is null.
        /// </exception>
        public static IReadOnlyList<string> getMultipleFilePaths(IReadOnlyList<string> filepaths, bool refresh = false, int timeout = 0)
        {
            if (filepaths == null)
                throw new ArgumentNullException(nameof(filepaths));

            var results = new string[filepaths.Count];

            for (int i = 0; i < filepaths.Count; i++)
            {
                results[i] = getFilePath(filepaths[i], refresh, timeout);
            }

            return results;
        }

        #endregion

        #region getFilePathAsync

        /// <summary>
        /// Asynchronously retrieves the readable path of a file in the "StreamingAssets" directory using coroutines.
        /// </summary>
        /// <remarks>
        /// Provide a relative file path based on the "StreamingAssets" directory.  e.g., "foobar.txt" or "hogehoge/foobar.txt".
        /// [Android] The target file that exists in the "StreamingAssets" directory is copied to the the Application.persistentDataPath directory.
        /// [WebGL] The target file in the "StreamingAssets" directory is copied to the WebGL's virtual filesystem.
        /// </remarks>
        /// <param name="filepath">
        /// A file path relative to the "StreamingAssets" directory.
        /// </param>
        /// <param name="completed">
        /// A callback that is called when the process is completed. Returns a readable file path in case of success and returns <c>string.Empty</c> in case of error.
        /// </param>
        /// <param name="progressChanged">
        /// An optional callback that is called when the process is the progress. Returns the file path and a progress value (0.0 to 1.0).
        /// </param>
        /// <param name="errorOccurred">
        /// An optional callback that is called when the process is error occurred. Returns the file path and an error string and an error response code.
        /// </param>
        /// <param name="refresh">
        /// [Android][WebGL] If false, the file is not copied if it already exists. If true, the file is always copied.
        /// </param>
        /// <param name="timeout">
        /// [Android][WebGL] Sets the UnityWebRequest to abort after the specified number of seconds. If set to 0, no timeout is applied. The default is 0.
        /// </param>
        /// <returns>
        /// Returns an IEnumerator object. Yielding the IEnumerator in a coroutine pauses the coroutine until the UnityWebRequest completes or encounters a system error. 
        /// <strong>Note:</strong> that if the IEnumerator is externally stoped, the UnityWebRequest's Abort method will not be called, meaning the download will continue in the background.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filepath"/> is null.
        /// </exception>
        public static IEnumerator getFilePathAsync(
            string filepath,
            Action<string> completed,
            Action<string, float> progressChanged = null,
            Action<string, string, long> errorOccurred = null,
            bool refresh = false,
            int timeout = 0)
        {
            if (filepath == null)
                throw new ArgumentNullException(nameof(filepath));

            filepath = filepath.TrimStart(chTrims);

            if (string.IsNullOrEmpty(filepath) || string.IsNullOrEmpty(Path.GetExtension(filepath)))
            {
                progressChanged?.Invoke(filepath, 0);
                yield return null;
                progressChanged?.Invoke(filepath, 1);
                errorOccurred?.Invoke(filepath, "Invalid file path.", -1);
                completed?.Invoke(string.Empty);

                yield break;
            }

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            string srcPath = Path.Combine(Application.streamingAssetsPath, filepath);
#if UNITY_ANDROID
            string destPath = Path.Combine(Application.persistentDataPath, "opencvforunity");
#else // UNITY_WEBGL
            string destPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "opencvforunity");
#endif
            destPath = Path.Combine(destPath, filepath);

            if (!refresh && File.Exists(destPath))
            {
                progressChanged?.Invoke(filepath, 0);
                yield return null;
                progressChanged?.Invoke(filepath, 1);
                completed?.Invoke(destPath);
                yield break;
            }

            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(srcPath))
            {
                if (timeout > 0)
                    request.timeout = timeout;

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    progressChanged?.Invoke(filepath, request.downloadProgress);
                    yield return null;
                }

                switch (request.result)
                {
                    case UnityEngine.Networking.UnityWebRequest.Result.Success:
                        progressChanged?.Invoke(filepath, request.downloadProgress);
                        // create directory and write downlorded data
                        string dirPath = Path.GetDirectoryName(destPath);
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                        File.WriteAllBytes(destPath, request.downloadHandler.data);
                        completed?.Invoke(destPath);
                        yield break;
                    case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
                    case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
                    case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
                        //Debug.LogError($"UnityWebRequest error occurred: {filepath}, {request.error}, {request.responseCode}");
                        errorOccurred?.Invoke(filepath, request.error, request.responseCode);
                        completed?.Invoke(string.Empty);
                        yield break;
                    default:
                        completed?.Invoke(string.Empty);
                        yield break;
                }
            }
#else // (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            string destPath = Path.Combine(Application.streamingAssetsPath, filepath);

            progressChanged?.Invoke(filepath, 0);
            yield return null;
            progressChanged?.Invoke(filepath, 1);

            if (File.Exists(destPath))
            {
                completed?.Invoke(destPath);
            }
            else
            {
                errorOccurred?.Invoke(filepath, "File does not exist.", -1);
                completed?.Invoke(string.Empty);
            }
            yield break;
#endif // (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR

        }


        /// <summary>
        /// Asynchronously retrieves the multiple readable paths of files in the "StreamingAssets" directory using coroutines.
        /// </summary>
        /// <remarks>
        /// Provide a relative file path based on the "StreamingAssets" directory.  e.g., "foobar.txt" or "hogehoge/foobar.txt".
        /// [Android] The target file that exists in the "StreamingAssets" directory is copied to the the Application.persistentDataPath directory.
        /// [WebGL] The target file in the "StreamingAssets" directory is copied to the WebGL's virtual filesystem.
        /// </remarks>
        /// <param name="filepaths">
        /// The list of file paths relative to the "StreamingAssets" directory.
        /// </param>
        /// <param name="allCompleted">
        /// A callback that is called when all processes are completed. Returns a list of file paths. Returns a readable file path in case of success and returns <c>string.Empty</c> in case of error.
        /// </param>
        /// <param name="completed">
        /// An optional callback that is called when one process is completed. Returns a readable file path in case of success and returns empty in case of error.
        /// </param>
        /// <param name="progressChanged">
        /// An optional callback that is called when one process is the progress. Returns the file path and a progress value (0.0 to 1.0).
        /// </param>
        /// <param name="errorOccurred">
        /// An optional callback that is called when one process is error occurred. Returns the file path and an error string and an error response code.
        /// </param>
        /// <param name="refresh">
        /// [Android][WebGL] If false, the file is not copied if it already exists. If true, the file is always copied.
        /// </param>
        /// <param name="timeout">
        /// [Android][WebGL] Sets the UnityWebRequest to abort after the specified number of seconds. If set to 0, no timeout is applied. The default is 0.
        /// </param>
        /// <returns>
        /// Returns an IEnumerator object. Yielding the IEnumerator in a coroutine pauses the coroutine until the UnityWebRequest completes or encounters a system error. 
        /// <strong>Note:</strong> that if the IEnumerator is externally stoped, the UnityWebRequest's Abort method will not be called, meaning the download will continue in the background.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filepath"/> is null.
        /// </exception>
        public static IEnumerator getMultipleFilePathsAsync(
            IReadOnlyList<string> filepaths,
            Action<IReadOnlyList<string>> allCompleted,
            Action<string> completed = null,
            Action<string, float> progressChanged = null,
            Action<string, string, long> errorOccurred = null,
            bool refresh = false,
            int timeout = 0)
        {
            if (filepaths == null)
                throw new ArgumentNullException(nameof(filepaths));

            string[] readableFilePaths = new string[filepaths.Count];

            for (int i = 0; i < filepaths.Count; i++)
            {
                yield return getFilePathAsync(filepaths[i],
                (path) =>
                {
                    readableFilePaths[i] = path;
                    completed?.Invoke(path);
                },
                progressChanged,
                errorOccurred,
                refresh, timeout);
            }

            allCompleted?.Invoke(readableFilePaths);
        }

        #endregion

        #region getFilePathAsyncTask

        /// <summary>
        /// Asynchronously retrieves the readable path of a file in the "StreamingAssets" directory using a Task.
        /// </summary>
        /// <remarks>
        /// Provide a relative file path based on the "StreamingAssets" directory.  e.g., "foobar.txt" or "hogehoge/foobar.txt".
        /// [Android] The target file that exists in the "StreamingAssets" directory is copied to the the Application.persistentDataPath directory.
        /// [WebGL] The target file in the "StreamingAssets" directory is copied to the WebGL's virtual filesystem.
        /// </remarks>
        /// <param name="filepath">
        /// A file path relative to the "StreamingAssets" directory.
        /// </param>
        /// <param name="progressChanged">
        /// An optional callback that is called when the process is the progress. Returns the file path and a progress value (0.0 to 1.0).
        /// </param>
        /// <param name="errorOccurred">
        /// An optional callback that is called when the process is error occurred. Returns the file path and an error string and an error response code.
        /// </param>
        /// <param name="refresh">
        /// [Android][WebGL] If false, the file is not copied if it already exists. If true, the file is always copied.
        /// </param>
        /// <param name="timeout">
        /// [Android][WebGL] Sets the UnityWebRequest to abort after the specified number of seconds. If set to 0, no timeout is applied. The default is 0.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the download operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous download operation. The result is a readable file path where the downloaded file was saved, or <c>string.Empty</c> if the download fails.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filepath"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this method is called from a non-main thread.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the download operation is canceled.
        /// </exception>
        public static async Task<string> getFilePathAsyncTask(
            string filepath,
            Action<string, float> progressChanged = null,
            Action<string, string, long> errorOccurred = null,
            bool refresh = false,
            int timeout = 0,
            CancellationToken cancellationToken = default)
        {
            if (filepath == null)
                throw new ArgumentNullException(nameof(filepath));

            if (SynchronizationContext.Current == null)
                throw new InvalidOperationException("This method must be called from the main thread.");

            cancellationToken.ThrowIfCancellationRequested();

            filepath = filepath.TrimStart(chTrims);

            if (string.IsNullOrEmpty(filepath) || string.IsNullOrEmpty(Path.GetExtension(filepath)))
            {
                progressChanged?.Invoke(filepath, 0);
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();

                progressChanged?.Invoke(filepath, 1);
                errorOccurred?.Invoke(filepath, "Invalid file path.", -1);

                return string.Empty;
            }

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            string srcPath = Path.Combine(Application.streamingAssetsPath, filepath);
#if UNITY_ANDROID
            string destPath = Path.Combine(Application.persistentDataPath, "opencvforunity");
#else // UNITY_WEBGL
            string destPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), "opencvforunity");
#endif
            destPath = Path.Combine(destPath, filepath);

            if (!refresh && File.Exists(destPath))
            {
                progressChanged?.Invoke(filepath, 0);
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();

                progressChanged?.Invoke(filepath, 1);
                return destPath;
            }

            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(srcPath))
            {
                if (timeout > 0)
                    request.timeout = timeout;

                var operation = request.SendWebRequest();

                try
                {
                    while (!operation.isDone)
                    {
                        progressChanged?.Invoke(filepath, request.downloadProgress);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            //Debug.Log("IsCancellationRequested");
                            request.Abort();
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        await Task.Yield();
                    }

                    switch (request.result)
                    {
                        case UnityEngine.Networking.UnityWebRequest.Result.Success:
                            progressChanged?.Invoke(filepath, request.downloadProgress);
                            // create directory and write downlorded data
                            string dirPath = Path.GetDirectoryName(destPath);
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllBytes(destPath, request.downloadHandler.data);
                            return destPath;
                        case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
                        case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
                        case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
                            //Debug.LogError($"An UnityWebRequest error occurred: {filepath}, {request.error}, {request.responseCode}");
                            errorOccurred?.Invoke(filepath, request.error, request.responseCode);
                            return string.Empty;
                        default:
                            return string.Empty;
                    }
                }
                catch (OperationCanceledException)
                {
                    //Debug.Log($"Download canceled: {filepath}");
                    throw;
                }
                catch (Exception ex)
                {
                    //Debug.LogError($"An error occurred: {filepath}, {ex.Message}");
                    errorOccurred?.Invoke(filepath, ex.Message, -1);
                    return string.Empty;
                }
            }
#else // (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            string destPath = Path.Combine(Application.streamingAssetsPath, filepath);

            progressChanged?.Invoke(filepath, 0);
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            progressChanged?.Invoke(filepath, 1);

            if (File.Exists(destPath))
            {
                return destPath;
            }
            else
            {
                errorOccurred?.Invoke(filepath, "File does not exist.", -1);
                return string.Empty;
            }
#endif // (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
        }

        /// <summary>
        /// Asynchronously retrieves the multiple readable paths of files in the "StreamingAssets" directory using a Task.
        /// </summary>
        /// <remarks>
        /// Provide a relative file path based on the "StreamingAssets" directory.  e.g., "foobar.txt" or "hogehoge/foobar.txt".
        /// [Android] The target file that exists in the "StreamingAssets" directory is copied to the the Application.persistentDataPath directory.
        /// [WebGL] The target file in the "StreamingAssets" directory is copied to the WebGL's virtual filesystem.
        /// </remarks>
        /// <param name="filepaths">
        /// The list of file paths relative to the "StreamingAssets" directory.
        /// </param>
        /// <param name="completed">
        /// An optional callback that is called when one process is completed. Returns a readable file path in case of success and returns empty in case of error.
        /// </param>
        /// <param name="progressChanged">
        /// An optional callback that is called when one process is the progress. Returns the file path and a progress value (0.0 to 1.0).
        /// </param>
        /// <param name="errorOccurred">
        /// An optional callback that is called when one process is error occurred. Returns the file path and an error string and an error response code.
        /// </param>
        /// <param name="refresh">
        /// [Android][WebGL] If false, the file is not copied if it already exists. If true, the file is always copied.
        /// </param>
        /// <param name="timeout">
        /// [Android][WebGL] Sets the UnityWebRequest to abort after the specified number of seconds. If set to 0, no timeout is applied. The default is 0.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the download operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous download operation. The result is a list of readable file paths where the downloaded file was saved, or <c>string.Empty</c> if the download fails.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filepath"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this method is called from a non-main thread.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the download operation is canceled.
        /// </exception>
        public static async Task<IReadOnlyList<string>> getMultipleFilePathsAsyncTask(
            IReadOnlyList<string> filepaths,
            Action<string> completed = null,
            Action<string, float> progressChanged = null,
            Action<string, string, long> errorOccurred = null,
            bool refresh = false,
            int timeout = 0,
            CancellationToken cancellationToken = default)
        {
            if (filepaths == null)
                throw new ArgumentNullException(nameof(filepaths));

            if (SynchronizationContext.Current == null)
                throw new InvalidOperationException("This method must be called from the main thread.");

            var downloadTasks = new List<Task<string>>(filepaths.Count);
            var results = new string[filepaths.Count];

            for (int i = 0; i < filepaths.Count; i++)
            {
                int index = i;

                var task = getFilePathAsyncTask(
                    filepaths[index],
                    progressChanged,
                    errorOccurred,
                    refresh,
                    timeout,
                    cancellationToken
                ).ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        var result = t.Result;
                        completed?.Invoke(result);
                        return result;
                    }
                    return string.Empty;

                }, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.FromCurrentSynchronizationContext());

                downloadTasks.Add(task);
            }

            results = await Task.WhenAll(downloadTasks);

            return results;
        }

        #endregion

        private static char[] chTrims = {
            '.',
#if UNITY_WINRT_8_1 && !UNITY_EDITOR
            '/',
            '\\'
#else
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar
#endif
        };

#pragma warning disable 0414
        /// <summary>
        /// If true, CvException is thrown instead of calling Debug.LogError (msg).
        /// </summary>
        private static bool throwOpenCVException = false;

        /// <summary>
        /// Callback callback called when an OpenCV error occurs on the Native side.
        /// </summary>
        private static Action<string> openCVSetDebugModeCallback;
#pragma warning restore 0414


        /// <summary>
        /// Sets the debug mode.
        /// </summary>
        /// <remarks>
        /// If debugMode is true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.However, if throwException is true, CvException is thrown instead of calling Debug.LogError (msg).
        /// </remarks>
        /// <example>
        /// Please use as follows.
        /// <code>
        /// {
        ///     // CVException handling
        ///     // Publish CVException to Debug.LogError.
        ///     Utils.setDebugMode(true, false);
        ///     
        ///     Mat m3 = m1 / m2; // element type is different.
        ///     
        ///     Utils.setDebugMode(false);
        ///     
        /// 
        ///     // Throw CVException.
        ///     Utils.setDebugMode(true, true);
        ///     
        ///     try
        ///     {
        ///         Mat m4 = m1 / m2; // element type is different.
        ///     }
        ///     catch (Exception e)
        ///     {
        ///         Debug.Log("CVException: " + e);
        ///     }
        ///     
        ///     Utils.setDebugMode(false);
        /// }
        /// </code>
        /// </example>
        /// <param name="debugMode">
        /// If true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
        /// </param>
        /// <param name="throwException">
        /// If true, CvException is thrown instead of calling Debug.LogError (msg).
        /// </param>
        /// <param name="callback">
        /// Callback called when an OpenCV error occurs on the Native side.
        /// </param>
        public static void setDebugMode(bool debugMode, bool throwException = false, Action<string> callback = null)
        {
            OpenCVForUnity_SetDebugMode(debugMode);

            if (debugMode)
            {
                OpenCVForUnity_SetDebugLogFunc(debugLogFunc);
                //OpenCVForUnity_DebugLogTest ();

                throwOpenCVException = throwException;
                openCVSetDebugModeCallback = callback;
            }
            else
            {
                OpenCVForUnity_SetDebugLogFunc(null);

                throwOpenCVException = false;
                openCVSetDebugModeCallback = null;
            }
        }

        private delegate void DebugLogDelegate(string str);

        [MonoPInvokeCallback(typeof(DebugLogDelegate))]
        private static void debugLogFunc(string str)
        {
            if (openCVSetDebugModeCallback != null) openCVSetDebugModeCallback.Invoke(str);

            if (throwOpenCVException)
            {
#if UNITY_2022_2_OR_NEWER && UNITY_ANDROID && ENABLE_IL2CPP
                Debug.LogError(str);
#else
                throw new CvException(str);
#endif
            }
            else
            {
                Debug.LogError(str);
            }
        }

        internal static int URShift(int number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2 << ~bits);
        }

        internal static long URShift(long number, int bits)//TODO:@check
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2 << ~bits);
        }

        internal static int HashContents<T>(this IEnumerable<T> enumerable)//TODO:@check
        {
            int hash = 0x218A9B2C;
            foreach (var item in enumerable)
            {
                int thisHash = item.GetHashCode();
                //mix up the bits.
                hash = thisHash ^ ((hash << 5) + hash);
            }
            return hash;
        }


#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif

        [DllImport(LIBNAME)]
        private static extern void OpenCVForUnity_SetDebugMode([MarshalAs(UnmanagedType.U1)] bool flag);

        [DllImport(LIBNAME)]
        private static extern void OpenCVForUnity_SetDebugLogFunc(DebugLogDelegate func);

        [DllImport(LIBNAME)]
        private static extern void OpenCVForUnity_DebugLogTest();

        [DllImport(LIBNAME)]
        private static extern void OpenCVForUnity_MatToTexture(IntPtr mat, IntPtr textureColors, int textureFormat, [MarshalAs(UnmanagedType.U1)] bool flip, int flipCode);

        [DllImport(LIBNAME)]
        private static extern void OpenCVForUnity_TextureToMat(IntPtr textureColors, IntPtr Mat, int textureFormat, [MarshalAs(UnmanagedType.U1)] bool flip, int flipCode);

        [DllImport(LIBNAME)]
        private static extern int OpenCVForUnity_MatDataToByteArray(IntPtr mat, int length, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr byteArray);

        [DllImport(LIBNAME)]
        private static extern int OpenCVForUnity_ByteArrayToMatData([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr byteArray, int length, IntPtr Mat);
    }
}
