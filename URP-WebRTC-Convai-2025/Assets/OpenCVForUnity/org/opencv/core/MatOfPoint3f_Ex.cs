using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfPoint3f : Mat
    {
        public MatOfPoint3f(params Vec3f[] a)
            : base()
        {

            fromVec3fArray(a);
        }

        public void fromVec3fArray(params Vec3f[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec3f>(a, this, num);

        }

        public Vec3f[] toVec3fArray()
        {
            int num = (int)total();
            Vec3f[] ap = new Vec3f[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec3f>(this, ap, num);

            return ap;

        }

        public MatOfPoint3f(params Vec3i[] a)
            : base()
        {

            fromVec3iArray(a);
        }

        public void fromVec3iArray(params Vec3i[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);


#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].Item1;
                    span[i].Item2 = (float)a[i].Item2;
                    span[i].Item3 = (float)a[i].Item3;
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

                        ptr[i * 3] = (float)a[i].Item1;
                        ptr[i * 3 + 1] = (float)a[i].Item2;
                        ptr[i * 3 + 2] = (float)a[i].Item3;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].Item1;
                buffSpan[i].Item2 = (float)a[i].Item2;
                buffSpan[i].Item3 = (float)a[i].Item3;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];
            for (int i = 0; i < a.Length; i++)
            {
                buff[3 * i + 0] = (float)a[i].Item1;
                buff[3 * i + 1] = (float)a[i].Item2;
                buff[3 * i + 2] = (float)a[i].Item3;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);
#endif

        }

        public Vec3i[] toVec3iArray()
        {
            int num = (int)total();
            Vec3i[] ap = new Vec3i[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (int)span[i].Item1;
                    ap[i].Item2 = (int)span[i].Item2;
                    ap[i].Item3 = (int)span[i].Item3;
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
                        ap[i].Item1 = (int)ptr[i * 3];
                        ap[i].Item2 = (int)ptr[i * 3 + 1];
                        ap[i].Item3 = (int)ptr[i * 3 + 2];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Converters.copyMatToArray<float>(this, buff, num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (int)buffSpan[i].Item1;
                ap[i].Item2 = (int)buffSpan[i].Item2;
                ap[i].Item3 = (int)buffSpan[i].Item3;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];

            Converters.copyMatToArray<float>(this, buff, num * 3);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (int)buff[i * 3];
                ap[i].Item2 = (int)buff[i * 3 + 1];
                ap[i].Item3 = (int)buff[i * 3 + 2];
            }
#endif

            return ap;

        }

        public MatOfPoint3f(params Vec3d[] a)
            : base()
        {

            fromVec3dArray(a);
        }

        public void fromVec3dArray(params Vec3d[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].Item1;
                    span[i].Item2 = (float)a[i].Item2;
                    span[i].Item3 = (float)a[i].Item3;
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

                        ptr[i * 3] = (float)a[i].Item1;
                        ptr[i * 3 + 1] = (float)a[i].Item2;
                        ptr[i * 3 + 2] = (float)a[i].Item3;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].Item1;
                buffSpan[i].Item2 = (float)a[i].Item2;
                buffSpan[i].Item3 = (float)a[i].Item3;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];
            for (int i = 0; i < a.Length; i++)
            {
                buff[3 * i + 0] = (float)a[i].Item1;
                buff[3 * i + 1] = (float)a[i].Item2;
                buff[3 * i + 2] = (float)a[i].Item3;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);
#endif

        }

        public Vec3d[] toVec3dArray()
        {
            int num = (int)total();
            Vec3d[] ap = new Vec3d[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].Item1 = (double)span[i].Item1;
                    ap[i].Item2 = (double)span[i].Item2;
                    ap[i].Item3 = (double)span[i].Item3;
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
                        ap[i].Item1 = (double)ptr[i * 3];
                        ap[i].Item2 = (double)ptr[i * 3 + 1];
                        ap[i].Item3 = (double)ptr[i * 3 + 2];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Converters.copyMatToArray<float>(this, buff, num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].Item1 = (double)buffSpan[i].Item1;
                ap[i].Item2 = (double)buffSpan[i].Item2;
                ap[i].Item3 = (double)buffSpan[i].Item3;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];

            Converters.copyMatToArray<float>(this, buff, num * 3);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].Item1 = (double)buff[i * 3];
                ap[i].Item2 = (double)buff[i * 3 + 1];
                ap[i].Item3 = (double)buff[i * 3 + 2];
            }
#endif

            return ap;

        }

        public MatOfPoint3f(params (float x, float y, float z)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float x, float y, float z)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].x;
                    span[i].Item2 = a[i].y;
                    span[i].Item3 = a[i].z;
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

                        ptr[i * 3] = a[i].x;
                        ptr[i * 3 + 1] = a[i].y;
                        ptr[i * 3 + 2] = a[i].z;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
                buffSpan[i].Item3 = a[i].z;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];
            for (int i = 0; i < a.Length; i++)
            {
                buff[3 * i + 0] = a[i].x;
                buff[3 * i + 1] = a[i].y;
                buff[3 * i + 2] = a[i].z;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);
#endif

        }

        public (float x, float y, float z)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float x, float y, float z)[] ap = new (float x, float y, float z)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = span[i].Item1;
                    ap[i].y = span[i].Item2;
                    ap[i].z = span[i].Item3;
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
                        ap[i].x = ptr[i * 3];
                        ap[i].y = ptr[i * 3 + 1];
                        ap[i].z = ptr[i * 3 + 2];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Converters.copyMatToArray<float>(this, buff, num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
                ap[i].z = buffSpan[i].Item3;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];

            Converters.copyMatToArray<float>(this, buff, num * 3);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = buff[i * 3];
                ap[i].y = buff[i * 3 + 1];
                ap[i].z = buff[i * 3 + 2];
            }
#endif


            return ap;

        }

        public MatOfPoint3f(params (int x, int y, int z)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (int x, int y, int z)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].x;
                    span[i].Item2 = (float)a[i].y;
                    span[i].Item3 = (float)a[i].z;
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

                        ptr[i * 3] = (float)a[i].x;
                        ptr[i * 3 + 1] = (float)a[i].y;
                        ptr[i * 3 + 2] = (float)a[i].z;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].x;
                buffSpan[i].Item2 = (float)a[i].y;
                buffSpan[i].Item3 = (float)a[i].z;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];
            for (int i = 0; i < a.Length; i++)
            {
                buff[3 * i + 0] = (float)a[i].x;
                buff[3 * i + 1] = (float)a[i].y;
                buff[3 * i + 2] = (float)a[i].z;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);
#endif

        }

        public (int x, int y, int z)[] toValueTupleArrayAsInt()
        {
            int num = (int)total();
            (int x, int y, int z)[] ap = new (int x, int y, int z)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (int)span[i].Item1;
                    ap[i].y = (int)span[i].Item2;
                    ap[i].z = (int)span[i].Item3;
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
                        ap[i].x = (int)ptr[i * 3];
                        ap[i].y = (int)ptr[i * 3 + 1];
                        ap[i].z = (int)ptr[i * 3 + 2];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Converters.copyMatToArray<float>(this, buff, num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (int)buffSpan[i].Item1;
                ap[i].y = (int)buffSpan[i].Item2;
                ap[i].z = (int)buffSpan[i].Item3;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];

            Converters.copyMatToArray<float>(this, buff, num * 3);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (int)buff[i * 3];
                ap[i].y = (int)buff[i * 3 + 1];
                ap[i].z = (int)buff[i * 3 + 2];
            }
#endif

            return ap;

        }

        public MatOfPoint3f(params (double x, double y, double z)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (double x, double y, double z)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = (float)a[i].x;
                    span[i].Item2 = (float)a[i].y;
                    span[i].Item3 = (float)a[i].z;
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

                        ptr[i * 3] = (float)a[i].x;
                        ptr[i * 3 + 1] = (float)a[i].y;
                        ptr[i * 3 + 2] = (float)a[i].z;
                    }

                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = (float)a[i].x;
                buffSpan[i].Item2 = (float)a[i].y;
                buffSpan[i].Item3 = (float)a[i].z;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];
            for (int i = 0; i < a.Length; i++)
            {
                buff[3 * i + 0] = (float)a[i].x;
                buff[3 * i + 1] = (float)a[i].y;
                buff[3 * i + 2] = (float)a[i].z;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 3);
#endif

        }

        public (double x, double y, double z)[] toValueTupleArrayAsDouble()
        {
            int num = (int)total();
            (double x, double y, double z)[] ap = new (double x, double y, double z)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec3f> span = this.AsSpan<Vec3f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = (double)span[i].Item1;
                    ap[i].y = (double)span[i].Item2;
                    ap[i].z = (double)span[i].Item3;
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
                        ap[i].x = (double)ptr[i * 3];
                        ap[i].y = (double)ptr[i * 3 + 1];
                        ap[i].z = (double)ptr[i * 3 + 2];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 3);

            Converters.copyMatToArray<float>(this, buff, num * 3);

            Span<Vec3f> buffSpan = MemoryMarshal.Cast<float, Vec3f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = (double)buffSpan[i].Item1;
                ap[i].y = (double)buffSpan[i].Item2;
                ap[i].z = (double)buffSpan[i].Item3;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 3];

            Converters.copyMatToArray<float>(this, buff, num * 3);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = (double)buff[i * 3];
                ap[i].y = (double)buff[i * 3 + 1];
                ap[i].z = (double)buff[i * 3 + 2];
            }
#endif

            return ap;

        }

        public MatOfPoint3f(params Vector3[] a)
            : base()
        {

            fromVector3Array(a);
        }

        public void fromVector3Array(params Vector3[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vector3>(a, this, num);

        }

        public Vector3[] toVector3Array()
        {
            int num = (int)total();
            Vector3[] ap = new Vector3[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vector3>(this, ap, num);

            return ap;

        }
    }
}

