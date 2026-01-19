using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfRect : Mat
    {
        public MatOfRect(params Vec4i[] a)
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

            Converters.copyArrayToMat<Vec4i>(a, this, num);

        }

        public Vec4i[] toVec4iArray()
        {
            int num = (int)total();
            Vec4i[] ap = new Vec4i[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec4i>(this, ap, num);

            return ap;

        }

        public MatOfRect(params Vec4d[] a)
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

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4i> span = this.AsSpan<Vec4i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].Item1;
                    span[i].Item2 = (int)a[i].Item2;
                    span[i].Item3 = (int)a[i].Item3;
                    span[i].Item4 = (int)a[i].Item4;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    int* ptr = (int*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (int)a[i].Item1;
                        ptr[4 * i + 1] = (int)a[i].Item2;
                        ptr[4 * i + 2] = (int)a[i].Item3;
                        ptr[4 * i + 3] = (int)a[i].Item4;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].Item1;
                buffSpan[i].Item2 = (int)a[i].Item2;
                buffSpan[i].Item3 = (int)a[i].Item3;
                buffSpan[i].Item4 = (int)a[i].Item4;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (int)a[i].Item1;
                buff[4 * i + 1] = (int)a[i].Item2;
                buff[4 * i + 2] = (int)a[i].Item3;
                buff[4 * i + 3] = (int)a[i].Item4;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);
#endif

        }

        public Vec4d[] toVec4dArray()
        {
            int num = (int)total();
            Vec4d[] ap = new Vec4d[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4i> span = this.AsSpan<Vec4i>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (double)span[i].Item1;
                    ap[i].Item2 = (double)span[i].Item2;
                    ap[i].Item3 = (double)span[i].Item3;
                    ap[i].Item4 = (double)span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    int* ptr = (int*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].Item1 = (double)ptr[4 * i + 0];
                        ap[i].Item2 = (double)ptr[4 * i + 1];
                        ap[i].Item3 = (double)ptr[4 * i + 2];
                        ap[i].Item4 = (double)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Converters.copyMatToArray<int>(this, buff, num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (double)buffSpan[i].Item1;
                ap[i].Item2 = (double)buffSpan[i].Item2;
                ap[i].Item3 = (double)buffSpan[i].Item3;
                ap[i].Item4 = (double)buffSpan[i].Item4;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];

            Converters.copyMatToArray<int>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (double)buff[i * 4];
                ap[i].Item2 = (double)buff[i * 4 + 1];
                ap[i].Item3 = (double)buff[i * 4 + 2];
                ap[i].Item4 = (double)buff[i * 4 + 3];
            }
#endif

            return ap;

        }

        public MatOfRect(params (int x, int y, int width, int height)[] a)
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
                Span<Vec4i> span = this.AsSpan<Vec4i>();
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
                    int* ptr = (int*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (int)a[i].x;
                        ptr[4 * i + 1] = (int)a[i].y;
                        ptr[4 * i + 2] = (int)a[i].width;
                        ptr[4 * i + 3] = (int)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
                buffSpan[i].Item3 = a[i].width;
                buffSpan[i].Item4 = a[i].height;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = a[i].x;
                buff[4 * i + 1] = a[i].y;
                buff[4 * i + 2] = a[i].width;
                buff[4 * i + 3] = a[i].height;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);
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
                Span<Vec4i> span = this.AsSpan<Vec4i>();
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
                    int* ptr = (int*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = ptr[4 * i + 0];
                        ap[i].y = ptr[4 * i + 1];
                        ap[i].width = ptr[4 * i + 2];
                        ap[i].height = ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Converters.copyMatToArray<int>(this, buff, num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
                ap[i].width = buffSpan[i].Item3;
                ap[i].height = buffSpan[i].Item4;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];

            Converters.copyMatToArray<int>(this, buff, num * 4);
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

        public MatOfRect(params (double x, double y, double width, double height)[] a)
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
                Span<Vec4i> span = this.AsSpan<Vec4i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].x;
                    span[i].Item2 = (int)a[i].y;
                    span[i].Item3 = (int)a[i].width;
                    span[i].Item4 = (int)a[i].height;
                }

                return;
            }

#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    int* ptr = (int*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (int)a[i].x;
                        ptr[4 * i + 1] = (int)a[i].y;
                        ptr[4 * i + 2] = (int)a[i].width;
                        ptr[4 * i + 3] = (int)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].x;
                buffSpan[i].Item2 = (int)a[i].y;
                buffSpan[i].Item3 = (int)a[i].width;
                buffSpan[i].Item4 = (int)a[i].height;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (int)a[i].x;
                buff[4 * i + 1] = (int)a[i].y;
                buff[4 * i + 2] = (int)a[i].width;
                buff[4 * i + 3] = (int)a[i].height;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);
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
                Span<Vec4i> span = this.AsSpan<Vec4i>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (double)span[i].Item1;
                    ap[i].y = (double)span[i].Item2;
                    ap[i].width = (double)span[i].Item3;
                    ap[i].height = (double)span[i].Item4;
                }

                return ap;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    int* ptr = (int*)this.dataAddr();

                    for (int i = 0; i < ap.Length; i++)
                    {
                        ap[i].x = (double)ptr[4 * i + 0];
                        ap[i].y = (double)ptr[4 * i + 1];
                        ap[i].width = (double)ptr[4 * i + 2];
                        ap[i].height = (double)ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Converters.copyMatToArray<int>(this, buff, num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (double)buffSpan[i].Item1;
                ap[i].y = (double)buffSpan[i].Item2;
                ap[i].width = (double)buffSpan[i].Item3;
                ap[i].height = (double)buffSpan[i].Item4;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];

            Converters.copyMatToArray<int>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (double)buff[i * 4];
                ap[i].y = (double)buff[i * 4 + 1];
                ap[i].width = (double)buff[i * 4 + 2];
                ap[i].height = (double)buff[i * 4 + 3];
            }
#endif

            return ap;

        }

        public MatOfRect(params RectInt[] a)
            : base()
        {

            fromRectIntArray(a);
        }

        public void fromRectIntArray(params RectInt[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<RectInt>(a, this, num);

        }

        public RectInt[] toRectIntArray()
        {
            int num = (int)total();
            RectInt[] ap = new RectInt[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<RectInt>(this, ap, num);

            return ap;

        }

        public MatOfRect(params UnityEngine.Rect[] a)
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
                Span<Vec4i> span = this.AsSpan<Vec4i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].x;
                    span[i].Item2 = (int)a[i].y;
                    span[i].Item3 = (int)a[i].width;
                    span[i].Item4 = (int)a[i].height;
                }

                return;
            }
#endif

#if !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                unsafe
                {
                    int* ptr = (int*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (int)a[i].x;
                        ptr[4 * i + 1] = (int)a[i].y;
                        ptr[4 * i + 2] = (int)a[i].width;
                        ptr[4 * i + 3] = (int)a[i].height;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].x;
                buffSpan[i].Item2 = (int)a[i].y;
                buffSpan[i].Item3 = (int)a[i].width;
                buffSpan[i].Item4 = (int)a[i].height;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = (int)a[i].x;
                buff[4 * i + 1] = (int)a[i].y;
                buff[4 * i + 2] = (int)a[i].width;
                buff[4 * i + 3] = (int)a[i].height;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);
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
                Span<Vec4i> span = this.AsSpan<Vec4i>();
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
                    int* ptr = (int*)this.dataAddr();

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
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Converters.copyMatToArray<int>(this, buff, num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (float)buffSpan[i].Item1;
                ap[i].y = (float)buffSpan[i].Item2;
                ap[i].width = (float)buffSpan[i].Item3;
                ap[i].height = (float)buffSpan[i].Item4;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];

            Converters.copyMatToArray<int>(this, buff, num * 4);
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

