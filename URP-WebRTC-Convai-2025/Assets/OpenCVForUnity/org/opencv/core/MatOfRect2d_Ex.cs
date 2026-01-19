using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfRect2d : Mat
    {

        public MatOfRect2d(params Vec4d[] a)
         : base()
        {

            fromVec4dArray(a);
        }

        public void fromVec4dArray(params Vec4d[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec4d>(a, this, num);

        }

        public Vec4d[] toVec4dArray()
        {
            int num = (int)total();
            Vec4d[] ap = new Vec4d[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec4d>(this, ap, num);

            return ap;

        }

        public MatOfRect2d(params Vec4i[] a)
            : base()
        {

            fromVec4iArray(a);
        }

        public void fromVec4iArray(params Vec4i[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (double)a[i].Item1;
                    span[i].Item2 = (double)a[i].Item2;
                    span[i].Item3 = (double)a[i].Item3;
                    span[i].Item4 = (double)a[i].Item4;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (double)a[i].Item1;
                        ptr[4 * i + 1] = (double)a[i].Item2;
                        ptr[4 * i + 2] = (double)a[i].Item3;
                        ptr[4 * i + 3] = (double)a[i].Item4;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (double)a[i].Item1;
                buffSpan[i].Item2 = (double)a[i].Item2;
                buffSpan[i].Item3 = (double)a[i].Item3;
                buffSpan[i].Item4 = (double)a[i].Item4;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (double)a[i].Item1;
                buff[4 * i + 1] = (double)a[i].Item2;
                buff[4 * i + 2] = (double)a[i].Item3;
                buff[4 * i + 3] = (double)a[i].Item4;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);
#endif

        }

        public Vec4i[] toVec4iArray()
        {
            int num = (int)total();
            Vec4i[] ap = new Vec4i[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (int)span[i].Item1;
                    ap[i].Item2 = (int)span[i].Item2;
                    ap[i].Item3 = (int)span[i].Item3;
                    ap[i].Item4 = (int)span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].Item1 = (int)ptr[4 * i + 0];
                        ap[i].Item2 = (int)ptr[4 * i + 1];
                        ap[i].Item3 = (int)ptr[4 * i + 2];
                        ap[i].Item4 = (int)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Converters.copyMatToArray<double>(this, buff, num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (int)buffSpan[i].Item1;
                ap[i].Item2 = (int)buffSpan[i].Item2;
                ap[i].Item3 = (int)buffSpan[i].Item3;
                ap[i].Item4 = (int)buffSpan[i].Item4;
            }

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];

            Converters.copyMatToArray<double>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (int)buff[i * 4];
                ap[i].Item2 = (int)buff[i * 4 + 1];
                ap[i].Item3 = (int)buff[i * 4 + 2];
                ap[i].Item4 = (int)buff[i * 4 + 3];
            }
#endif

            return ap;

        }

        public MatOfRect2d(params (double x, double y, double width, double height)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (double x, double y, double width, double height)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].x;
                    span[i].Item2 = a[i].y;
                    span[i].Item3 = a[i].width;
                    span[i].Item4 = a[i].height;
                }

                return;
            }

#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (double)a[i].x;
                        ptr[4 * i + 1] = (double)a[i].y;
                        ptr[4 * i + 2] = (double)a[i].width;
                        ptr[4 * i + 3] = (double)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
                buffSpan[i].Item3 = a[i].width;
                buffSpan[i].Item4 = a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num + 4);

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = a[i].x;
                buff[4 * i + 1] = a[i].y;
                buff[4 * i + 2] = a[i].width;
                buff[4 * i + 3] = a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);
#endif

        }

        public (double x, double y, double width, double height)[] toValueTupleArrayAsDouble()
        {
            int num = (int)total();
            (double x, double y, double width, double height)[] ap = new (double x, double y, double width, double height)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = span[i].Item1;
                    ap[i].y = span[i].Item2;
                    ap[i].width = span[i].Item3;
                    ap[i].height = span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (int)ptr[4 * i + 0];
                        ap[i].y = (int)ptr[4 * i + 1];
                        ap[i].width = (int)ptr[4 * i + 2];
                        ap[i].height = (int)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Converters.copyMatToArray<double>(this, buff, num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
                ap[i].width = buffSpan[i].Item3;
                ap[i].height = buffSpan[i].Item4;
            }

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];

            Converters.copyMatToArray<double>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = buff[i * 4];
                ap[i].y = buff[i * 4 + 1];
                ap[i].width = buff[i * 4 + 2];
                ap[i].height = buff[i * 4 + 3];
            }
#endif


            return ap;

        }

        public MatOfRect2d(params (int x, int y, int width, int height)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (int x, int y, int width, int height)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (double)a[i].x;
                    span[i].Item2 = (double)a[i].y;
                    span[i].Item3 = (double)a[i].width;
                    span[i].Item4 = (double)a[i].height;
                }

                return;
            }

#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (double)a[i].x;
                        ptr[4 * i + 1] = (double)a[i].y;
                        ptr[4 * i + 2] = (double)a[i].width;
                        ptr[4 * i + 3] = (double)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (double)a[i].x;
                buffSpan[i].Item2 = (double)a[i].y;
                buffSpan[i].Item3 = (double)a[i].width;
                buffSpan[i].Item4 = (double)a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (double)a[i].x;
                buff[4 * i + 1] = (double)a[i].y;
                buff[4 * i + 2] = (double)a[i].width;
                buff[4 * i + 3] = (double)a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);
#endif

        }

        public (int x, int y, int width, int height)[] toValueTupleArrayAsInt()
        {
            int num = (int)total();
            (int x, int y, int width, int height)[] ap = new (int x, int y, int width, int height)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (int)span[i].Item1;
                    ap[i].y = (int)span[i].Item2;
                    ap[i].width = (int)span[i].Item3;
                    ap[i].height = (int)span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (int)ptr[4 * i + 0];
                        ap[i].y = (int)ptr[4 * i + 1];
                        ap[i].width = (int)ptr[4 * i + 2];
                        ap[i].height = (int)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Converters.copyMatToArray<double>(this, buff, num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (int)buffSpan[i].Item1;
                ap[i].y = (int)buffSpan[i].Item2;
                ap[i].width = (int)buffSpan[i].Item3;
                ap[i].height = (int)buffSpan[i].Item4;
            }

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];

            Converters.copyMatToArray<double>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (int)buff[i * 4];
                ap[i].y = (int)buff[i * 4 + 1];
                ap[i].width = (int)buff[i * 4 + 2];
                ap[i].height = (int)buff[i * 4 + 3];
            }
#endif

            return ap;

        }

        public MatOfRect2d(params UnityEngine.RectInt[] a)
            : base()
        {

            fromRectIntArray(a);
        }

        public void fromRectIntArray(params UnityEngine.RectInt[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (double)a[i].x;
                    span[i].Item2 = (double)a[i].y;
                    span[i].Item3 = (double)a[i].width;
                    span[i].Item4 = (double)a[i].height;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (double)a[i].x;
                        ptr[4 * i + 1] = (double)a[i].y;
                        ptr[4 * i + 2] = (double)a[i].width;
                        ptr[4 * i + 3] = (double)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (double)a[i].x;
                buffSpan[i].Item2 = (double)a[i].y;
                buffSpan[i].Item3 = (double)a[i].width;
                buffSpan[i].Item4 = (double)a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (double)a[i].x;
                buff[4 * i + 1] = (double)a[i].y;
                buff[4 * i + 2] = (double)a[i].width;
                buff[4 * i + 3] = (double)a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);
#endif

        }

        public UnityEngine.RectInt[] toRectIntArray()
        {
            int num = (int)total();
            UnityEngine.RectInt[] ap = new UnityEngine.RectInt[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (int)span[i].Item1;
                    ap[i].y = (int)span[i].Item2;
                    ap[i].width = (int)span[i].Item3;
                    ap[i].height = (int)span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (int)ptr[4 * i + 0];
                        ap[i].y = (int)ptr[4 * i + 1];
                        ap[i].width = (int)ptr[4 * i + 2];
                        ap[i].height = (int)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Converters.copyMatToArray<double>(this, buff, num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (int)buffSpan[i].Item1;
                ap[i].y = (int)buffSpan[i].Item2;
                ap[i].width = (int)buffSpan[i].Item3;
                ap[i].height = (int)buffSpan[i].Item4;
            }

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];

            Converters.copyMatToArray<double>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (int)buff[i * 4];
                ap[i].y = (int)buff[i * 4 + 1];
                ap[i].width = (int)buff[i * 4 + 2];
                ap[i].height = (int)buff[i * 4 + 3];
            }
#endif

            return ap;

        }

        public MatOfRect2d(params UnityEngine.Rect[] a)
            : base()
        {

            fromRectArray(a);
        }

        public void fromRectArray(params UnityEngine.Rect[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (double)a[i].x;
                    span[i].Item2 = (double)a[i].y;
                    span[i].Item3 = (double)a[i].width;
                    span[i].Item4 = (double)a[i].height;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (double)a[i].x;
                        ptr[4 * i + 1] = (double)a[i].y;
                        ptr[4 * i + 2] = (double)a[i].width;
                        ptr[4 * i + 3] = (double)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (double)a[i].x;
                buffSpan[i].Item2 = (double)a[i].y;
                buffSpan[i].Item3 = (double)a[i].width;
                buffSpan[i].Item4 = (double)a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (double)a[i].x;
                buff[4 * i + 1] = (double)a[i].y;
                buff[4 * i + 2] = (double)a[i].width;
                buff[4 * i + 3] = (double)a[i].height;
            }

            Converters.copyArrayToMat<double>(buff, this, num * 4);
#endif

        }

        public UnityEngine.Rect[] toRectArray()
        {
            int num = (int)total();
            UnityEngine.Rect[] ap = new UnityEngine.Rect[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4d> span = this.AsSpan<Vec4d>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (float)span[i].Item1;
                    ap[i].y = (float)span[i].Item2;
                    ap[i].width = (float)span[i].Item3;
                    ap[i].height = (float)span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    double* ptr = (double*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (float)ptr[4 * i + 0];
                        ap[i].y = (float)ptr[4 * i + 1];
                        ap[i].width = (float)ptr[4 * i + 2];
                        ap[i].height = (float)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            double[] buff = ArrayPool<double>.Shared.Rent(num * 4);

            Converters.copyMatToArray<double>(this, buff, num * 4);

            Span<Vec4d> buffSpan = MemoryMarshal.Cast<double, Vec4d>(buff.AsSpan<double>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (float)buffSpan[i].Item1;
                ap[i].y = (float)buffSpan[i].Item2;
                ap[i].width = (float)buffSpan[i].Item3;
                ap[i].height = (float)buffSpan[i].Item4;
            }

            ArrayPool<double>.Shared.Return(buff);
#else
            double[] buff = new double[num * 4];

            Converters.copyMatToArray<double>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (float)buff[i * 4];
                ap[i].y = (float)buff[i * 4 + 1];
                ap[i].width = (float)buff[i * 4 + 2];
                ap[i].height = (float)buff[i * 4 + 3];
            }
#endif

            return ap;

        }
    }
}

