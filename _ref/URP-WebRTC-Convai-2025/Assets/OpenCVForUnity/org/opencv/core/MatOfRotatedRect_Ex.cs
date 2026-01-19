using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfRotatedRect : Mat
    {

        public MatOfRotatedRect(params Vec5f[] a)
           : base()
        {

            fromVec5fArray(a);
        }

        public void fromVec5fArray(params Vec5f[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec5f>(a, this, num);

        }

        public Vec5f[] toVec5fArray()
        {
            int num = (int)total();
            Vec5f[] ap = new Vec5f[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec5f>(this, ap, num);

            return ap;

        }

        public MatOfRotatedRect(params Vec5d[] a)
            : base()
        {

            fromVec5dArray(a);
        }

        public void fromVec5dArray(params Vec5d[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec5f> span = this.AsSpan<Vec5f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].Item1;
                    span[i].Item2 = (float)a[i].Item2;
                    span[i].Item3 = (float)a[i].Item3;
                    span[i].Item4 = (float)a[i].Item4;
                    span[i].Item5 = (float)a[i].Item5;
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
                        ptr[5 * i] = (float)a[i].Item1;
                        ptr[5 * i + 1] = (float)a[i].Item2;
                        ptr[5 * i + 2] = (float)a[i].Item3;
                        ptr[5 * i + 3] = (float)a[i].Item4;
                        ptr[5 * i + 4] = (float)a[i].Item5;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 5);

            Span<Vec5f> buffSpan = MemoryMarshal.Cast<float, Vec5f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].Item1;
                buffSpan[i].Item2 = (float)a[i].Item2;
                buffSpan[i].Item3 = (float)a[i].Item3;
                buffSpan[i].Item4 = (float)a[i].Item4;
                buffSpan[i].Item5 = (float)a[i].Item5;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 5);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 5];
            for (int i = 0; i < a.Length; i++)
            {
                buff[5 * i + 0] = (float)a[i].Item1;
                buff[5 * i + 1] = (float)a[i].Item2;
                buff[5 * i + 2] = (float)a[i].Item3;
                buff[5 * i + 3] = (float)a[i].Item4;
                buff[5 * i + 4] = (float)a[i].Item5;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 5);
#endif

        }

        public Vec5d[] toVec5dArray()
        {
            int num = (int)total();
            Vec5d[] ap = new Vec5d[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec5f> span = this.AsSpan<Vec5f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (double)span[i].Item1;
                    ap[i].Item2 = (double)span[i].Item2;
                    ap[i].Item3 = (double)span[i].Item3;
                    ap[i].Item4 = (double)span[i].Item4;
                    ap[i].Item5 = (double)span[i].Item5;
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
                        ap[i].Item1 = ptr[5 * i];
                        ap[i].Item2 = ptr[5 * i + 1];
                        ap[i].Item3 = ptr[5 * i + 2];
                        ap[i].Item4 = ptr[5 * i + 3];
                        ap[i].Item5 = ptr[5 * i + 4];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 5);

            Converters.copyMatToArray<float>(this, buff, num * 5);

            Span<Vec5f> buffSpan = MemoryMarshal.Cast<float, Vec5f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (double)buffSpan[i].Item1;
                ap[i].Item2 = (double)buffSpan[i].Item2;
                ap[i].Item3 = (double)buffSpan[i].Item3;
                ap[i].Item4 = (double)buffSpan[i].Item4;
                ap[i].Item5 = (double)buffSpan[i].Item5;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 5];

            Converters.copyMatToArray<float>(this, buff, num * 5);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (double)buff[i * 5];
                ap[i].Item2 = (double)buff[i * 5 + 1];
                ap[i].Item3 = (double)buff[i * 5 + 2];
                ap[i].Item4 = (double)buff[i * 5 + 3];
                ap[i].Item5 = (double)buff[i * 5 + 4];
            }
#endif

            return ap;

        }

        public MatOfRotatedRect(params (float x, float y, float width, float height, float angle)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float x, float y, float width, float height, float angle)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec5f> span = this.AsSpan<Vec5f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].x;
                    span[i].Item2 = a[i].y;
                    span[i].Item3 = a[i].width;
                    span[i].Item4 = a[i].height;
                    span[i].Item5 = a[i].angle;
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
                        ptr[5 * i] = (float)a[i].x;
                        ptr[5 * i + 1] = (float)a[i].y;
                        ptr[5 * i + 2] = (float)a[i].width;
                        ptr[5 * i + 3] = (float)a[i].height;
                        ptr[5 * i + 4] = (float)a[i].angle;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 5);

            Span<Vec5f> buffSpan = MemoryMarshal.Cast<float, Vec5f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
                buffSpan[i].Item3 = a[i].width;
                buffSpan[i].Item4 = a[i].height;
                buffSpan[i].Item5 = a[i].angle;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 5);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 5];
            for (int i = 0; i < a.Length; i++)
            {
                buff[5 * i + 0] = a[i].x;
                buff[5 * i + 1] = a[i].y;
                buff[5 * i + 2] = a[i].width;
                buff[5 * i + 3] = a[i].height;
                buff[5 * i + 4] = a[i].angle;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 5);
#endif

        }

        public (float x, float y, float width, float height, float angle)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float x, float y, float width, float height, float angle)[] ap = new (float x, float y, float width, float height, float angle)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec5f> span = this.AsSpan<Vec5f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = span[i].Item1;
                    ap[i].y = span[i].Item2;
                    ap[i].width = span[i].Item3;
                    ap[i].height = span[i].Item4;
                    ap[i].angle = span[i].Item5;
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
                        ap[i].x = ptr[5 * i];
                        ap[i].y = ptr[5 * i + 1];
                        ap[i].width = ptr[5 * i + 2];
                        ap[i].height = ptr[5 * i + 3];
                        ap[i].angle = ptr[5 * i + 4];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 5);

            Converters.copyMatToArray<float>(this, buff, num * 5);

            Span<Vec5f> buffSpan = MemoryMarshal.Cast<float, Vec5f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
                ap[i].width = buffSpan[i].Item3;
                ap[i].height = buffSpan[i].Item4;
                ap[i].angle = buffSpan[i].Item5;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 5];

            Converters.copyMatToArray<float>(this, buff, num * 5);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = buff[i * 5];
                ap[i].y = buff[i * 5 + 1];
                ap[i].width = buff[i * 5 + 2];
                ap[i].height = buff[i * 5 + 3];
                ap[i].angle = buff[i * 5 + 4];
            }
#endif


            return ap;

        }

        public MatOfRotatedRect(params (double x, double y, double width, double height, double angle)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (double x, double y, double width, double height, double angle)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec5f> span = this.AsSpan<Vec5f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].x;
                    span[i].Item2 = (float)a[i].y;
                    span[i].Item3 = (float)a[i].width;
                    span[i].Item4 = (float)a[i].height;
                    span[i].Item5 = (float)a[i].angle;
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
                        ptr[5 * i] = (float)a[i].x;
                        ptr[5 * i + 1] = (float)a[i].y;
                        ptr[5 * i + 2] = (float)a[i].width;
                        ptr[5 * i + 3] = (float)a[i].height;
                        ptr[5 * i + 4] = (float)a[i].angle;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 5);

            Span<Vec5f> buffSpan = MemoryMarshal.Cast<float, Vec5f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].x;
                buffSpan[i].Item2 = (float)a[i].y;
                buffSpan[i].Item3 = (float)a[i].width;
                buffSpan[i].Item4 = (float)a[i].height;
                buffSpan[i].Item5 = (float)a[i].angle;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 5);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 5];
            for (int i = 0; i < a.Length; i++)
            {
                buff[5 * i + 0] = (float)a[i].x;
                buff[5 * i + 1] = (float)a[i].y;
                buff[5 * i + 2] = (float)a[i].width;
                buff[5 * i + 3] = (float)a[i].height;
                buff[5 * i + 4] = (float)a[i].angle;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 5);
#endif

        }

        public (double x, double y, double width, double height, double angle)[] toValueTupleArrayAsDouble()
        {
            int num = (int)total();
            (double x, double y, double width, double height, double angle)[] ap = new (double x, double y, double width, double height, double angle)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec5f> span = this.AsSpan<Vec5f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (double)span[i].Item1;
                    ap[i].y = (double)span[i].Item2;
                    ap[i].width = (double)span[i].Item3;
                    ap[i].height = (double)span[i].Item4;
                    ap[i].angle = (double)span[i].Item5;
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
                        ap[i].x = ptr[5 * i];
                        ap[i].y = ptr[5 * i + 1];
                        ap[i].width = ptr[5 * i + 2];
                        ap[i].height = ptr[5 * i + 3];
                        ap[i].angle = ptr[5 * i + 4];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 5);

            Converters.copyMatToArray<float>(this, buff, num * 5);

            Span<Vec5f> buffSpan = MemoryMarshal.Cast<float, Vec5f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (double)buffSpan[i].Item1;
                ap[i].y = (double)buffSpan[i].Item2;
                ap[i].width = (double)buffSpan[i].Item3;
                ap[i].height = (double)buffSpan[i].Item4;
                ap[i].angle = (double)buffSpan[i].Item5;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 5];

            Converters.copyMatToArray<float>(this, buff, num * 5);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (double)buff[i * 5];
                ap[i].y = (double)buff[i * 5 + 1];
                ap[i].width = (double)buff[i * 5 + 2];
                ap[i].height = (double)buff[i * 5 + 3];
                ap[i].angle = (double)buff[i * 5 + 4];
            }
#endif

            return ap;

        }

    }
}

