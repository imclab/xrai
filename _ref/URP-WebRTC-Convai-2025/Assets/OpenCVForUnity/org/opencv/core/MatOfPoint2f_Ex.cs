using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfPoint2f : Mat
    {
        public MatOfPoint2f(params Vec2f[] a)
           : base()
        {

            fromVec2fArray(a);
        }

        public void fromVec2fArray(params Vec2f[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec2f>(a, this, num);

        }

        public Vec2f[] toVec2fArray()
        {
            int num = (int)total();
            Vec2f[] ap = new Vec2f[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec2f>(this, ap, num);

            return ap;

        }

        public MatOfPoint2f(params Vec2i[] a)
            : base()
        {

            fromVec2iArray(a);
        }

        public void fromVec2iArray(params Vec2i[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);


#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].Item1;
                    span[i].Item2 = (float)a[i].Item2;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {

                        ptr[i * 2] = (float)a[i].Item1;
                        ptr[i * 2 + 1] = (float)a[i].Item2;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].Item1;
                buffSpan[i].Item2 = (float)a[i].Item2;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (float)a[i].Item1;
                buff[2 * i + 1] = (float)a[i].Item2;
            }
            
            Converters.copyArrayToMat<float>(buff, this, num * 2);
#endif

        }

        public Vec2i[] toVec2iArray()
        {
            int num = (int)total();
            Vec2i[] ap = new Vec2i[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (int)span[i].Item1;
                    ap[i].Item2 = (int)span[i].Item2;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].Item1 = (int)ptr[i * 2];
                        ap[i].Item2 = (int)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Converters.copyMatToArray<float>(this, buff, num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (int)buffSpan[i].Item1;
                ap[i].Item2 = (int)buffSpan[i].Item2;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];

            Converters.copyMatToArray<float>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (int)buff[i * 2];
                ap[i].Item2 = (int)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint2f(params Vec2d[] a)
            : base()
        {

            fromVec2dArray(a);
        }

        public void fromVec2dArray(params Vec2d[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].Item1;
                    span[i].Item2 = (float)a[i].Item2;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {

                        ptr[i * 2] = (float)a[i].Item1;
                        ptr[i * 2 + 1] = (float)a[i].Item2;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].Item1;
                buffSpan[i].Item2 = (float)a[i].Item2;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (float)a[i].Item1;
                buff[2 * i + 1] = (float)a[i].Item2;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);
#endif

        }

        public Vec2d[] toVec2dArray()
        {
            int num = (int)total();
            Vec2d[] ap = new Vec2d[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (double)span[i].Item1;
                    ap[i].Item2 = (double)span[i].Item2;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].Item1 = (double)ptr[i * 2];
                        ap[i].Item2 = (double)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Converters.copyMatToArray<float>(this, buff, num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (double)buffSpan[i].Item1;
                ap[i].Item2 = (double)buffSpan[i].Item2;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];

            Converters.copyMatToArray<float>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (double)buff[i * 2];
                ap[i].Item2 = (double)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint2f(params (float x, float y)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float x, float y)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].x;
                    span[i].Item2 = a[i].y;
                }

                return;
            }

#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {

                        ptr[i * 2] = a[i].x;
                        ptr[i * 2 + 1] = a[i].y;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = a[i].x;
                buff[2 * i + 1] = a[i].y;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);
#endif

        }

        public (float x, float y)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float x, float y)[] ap = new (float x, float y)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = span[i].Item1;
                    ap[i].y = span[i].Item2;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = ptr[i * 2];
                        ap[i].y = ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Converters.copyMatToArray<float>(this, buff, num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];

            Converters.copyMatToArray<float>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = buff[i * 2];
                ap[i].y = buff[i * 2 + 1];
            }
#endif


            return ap;

        }

        public MatOfPoint2f(params (int x, int y)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (int x, int y)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].x;
                    span[i].Item2 = (float)a[i].y;
                }

                return;
            }

#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {

                        ptr[i * 2] = (float)a[i].x;
                        ptr[i * 2 + 1] = (float)a[i].y;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].x;
                buffSpan[i].Item2 = (float)a[i].y;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (float)a[i].x;
                buff[2 * i + 1] = (float)a[i].y;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);
#endif

        }

        public (int x, int y)[] toValueTupleArrayAsInt()
        {
            int num = (int)total();
            (int x, int y)[] ap = new (int x, int y)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (int)span[i].Item1;
                    ap[i].y = (int)span[i].Item2;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (int)ptr[i * 2];
                        ap[i].y = (int)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Converters.copyMatToArray<float>(this, buff, num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (int)buffSpan[i].Item1;
                ap[i].y = (int)buffSpan[i].Item2;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];

            Converters.copyMatToArray<float>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (int)buff[i * 2];
                ap[i].y = (int)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint2f(params (double x, double y)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (double x, double y)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].x;
                    span[i].Item2 = (float)a[i].y;
                }

                return;
            }

#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {

                        ptr[i * 2] = (float)a[i].x;
                        ptr[i * 2 + 1] = (float)a[i].y;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].x;
                buffSpan[i].Item2 = (float)a[i].y;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (float)a[i].x;
                buff[2 * i + 1] = (float)a[i].y;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 2);
#endif

        }

        public (double x, double y)[] toValueTupleArrayAsDouble()
        {
            int num = (int)total();
            (double x, double y)[] ap = new (double x, double y)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2f> span = this.AsSpan<Vec2f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (double)span[i].Item1;
                    ap[i].y = (double)span[i].Item2;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    float* ptr = (float*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (double)ptr[i * 2];
                        ap[i].y = (double)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 2);

            Converters.copyMatToArray<float>(this, buff, num * 2);

            Span<Vec2f> buffSpan = MemoryMarshal.Cast<float, Vec2f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (double)buffSpan[i].Item1;
                ap[i].y = (double)buffSpan[i].Item2;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 2];

            Converters.copyMatToArray<float>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (double)buff[i * 2];
                ap[i].y = (double)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint2f(params Vector2[] a)
            : base()
        {

            fromVector2Array(a);
        }

        public void fromVector2Array(params Vector2[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vector2>(a, this, num);
        }

        public Vector2[] toVector2Array()
        {
            int num = (int)total();
            Vector2[] ap = new Vector2[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vector2>(this, ap, num);

            return ap;
        }
    }
}

