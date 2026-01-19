using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfPoint : Mat
    {

        public MatOfPoint(params Vec2i[] a)
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

            Converters.copyArrayToMat<Vec2i>(a, this, num);

        }

        public Vec2i[] toVec2iArray()
        {
            int num = (int)total();
            Vec2i[] ap = new Vec2i[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec2i>(this, ap, num);

            return ap;

        }

        public MatOfPoint(params Vec2f[] a)
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


#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].Item1;
                    span[i].Item2 = (int)a[i].Item2;
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

                        ptr[i * 2] = (int)a[i].Item1;
                        ptr[i * 2 + 1] = (int)a[i].Item2;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].Item1;
                buffSpan[i].Item2 = (int)a[i].Item2;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (int)a[i].Item1;
                buff[2 * i + 1] = (int)a[i].Item2;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);
#endif

        }

        public Vec2f[] toVec2fArray()
        {
            int num = (int)total();
            Vec2f[] ap = new Vec2f[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (float)span[i].Item1;
                    ap[i].Item2 = (float)span[i].Item2;
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
                        ap[i].Item1 = (float)ptr[i * 2];
                        ap[i].Item2 = (float)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Converters.copyMatToArray<int>(this, buff, num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (float)buffSpan[i].Item1;
                ap[i].Item2 = (float)buffSpan[i].Item2;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];

            Converters.copyMatToArray<int>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (float)buff[i * 2];
                ap[i].Item2 = (float)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint(params Vec2d[] a)
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].Item1;
                    span[i].Item2 = (int)a[i].Item2;
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

                        ptr[i * 2] = (int)a[i].Item1;
                        ptr[i * 2 + 1] = (int)a[i].Item2;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].Item1;
                buffSpan[i].Item2 = (int)a[i].Item2;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (int)a[i].Item1;
                buff[2 * i + 1] = (int)a[i].Item2;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
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
                    int* ptr = (int*)this.dataAddr();

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
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Converters.copyMatToArray<int>(this, buff, num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (double)buffSpan[i].Item1;
                ap[i].Item2 = (double)buffSpan[i].Item2;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];

            Converters.copyMatToArray<int>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (double)buff[i * 2];
                ap[i].Item2 = (double)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint(params (int x, int y)[] a)
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
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
                    int* ptr = (int*)this.dataAddr();
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
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = a[i].x;
                buff[2 * i + 1] = a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
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
                    int* ptr = (int*)this.dataAddr();

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
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Converters.copyMatToArray<int>(this, buff, num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];

            Converters.copyMatToArray<int>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = buff[i * 2];
                ap[i].y = buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint(params (float x, float y)[] a)
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].x;
                    span[i].Item2 = (int)a[i].y;
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

                        ptr[i * 2] = (int)a[i].x;
                        ptr[i * 2 + 1] = (int)a[i].y;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].x;
                buffSpan[i].Item2 = (int)a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (int)a[i].x;
                buff[2 * i + 1] = (int)a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (float)span[i].Item1;
                    ap[i].y = (float)span[i].Item2;
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
                        ap[i].x = (float)ptr[i * 2];
                        ap[i].y = (float)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Converters.copyMatToArray<int>(this, buff, num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (float)buffSpan[i].Item1;
                ap[i].y = (float)buffSpan[i].Item2;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];

            Converters.copyMatToArray<int>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (float)buff[i * 2];
                ap[i].y = (float)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint(params (double x, double y)[] a)
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].x;
                    span[i].Item2 = (int)a[i].y;
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

                        ptr[i * 2] = (int)a[i].x;
                        ptr[i * 2 + 1] = (int)a[i].y;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].x;
                buffSpan[i].Item2 = (int)a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (int)a[i].x;
                buff[2 * i + 1] = (int)a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);
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
                Span<Vec2i> span = this.AsSpan<Vec2i>();
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
                    int* ptr = (int*)this.dataAddr();

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
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Converters.copyMatToArray<int>(this, buff, num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (double)buffSpan[i].Item1;
                ap[i].y = (double)buffSpan[i].Item2;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];

            Converters.copyMatToArray<int>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (double)buff[i * 2];
                ap[i].y = (double)buff[i * 2 + 1];
            }
#endif

            return ap;

        }

        public MatOfPoint(params UnityEngine.Vector2[] a)
            : base()
        {

            fromVector2Array(a);
        }

        public void fromVector2Array(params UnityEngine.Vector2[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);


#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (int)a[i].x;
                    span[i].Item2 = (int)a[i].y;
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

                        ptr[i * 2] = (int)a[i].x;
                        ptr[i * 2 + 1] = (int)a[i].y;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (int)a[i].x;
                buffSpan[i].Item2 = (int)a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];
            for (int i = 0; i < a.Length; i++)
            {
                buff[2 * i + 0] = (int)a[i].x;
                buff[2 * i + 1] = (int)a[i].y;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 2);
#endif

        }

        public UnityEngine.Vector2[] toVector2Array()
        {
            int num = (int)total();
            UnityEngine.Vector2[] ap = new UnityEngine.Vector2[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec2i> span = this.AsSpan<Vec2i>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (float)span[i].Item1;
                    ap[i].y = (float)span[i].Item2;
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
                        ap[i].x = (float)ptr[i * 2];
                        ap[i].y = (float)ptr[i * 2 + 1];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            int[] buff = ArrayPool<int>.Shared.Rent(num * 2);

            Converters.copyMatToArray<int>(this, buff, num * 2);

            Span<Vec2i> buffSpan = MemoryMarshal.Cast<int, Vec2i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (float)buffSpan[i].Item1;
                ap[i].y = (float)buffSpan[i].Item2;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 2];

            Converters.copyMatToArray<int>(this, buff, num * 2);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (float)buff[i * 2];
                ap[i].y = (float)buff[i * 2 + 1];
            }
#endif

            return ap;

        }
    }
}

