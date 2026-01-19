using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfDMatch : Mat
    {
        public MatOfDMatch(params Vec4f[] a)
            : base()
        {

            fromVec4fArray(a);
        }

        public void fromVec4fArray(params Vec4f[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec4f>(a, this, num);

        }

        public Vec4f[] toVec4fArray()
        {
            int num = (int)total();
            Vec4f[] ap = new Vec4f[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec4f>(this, ap, num);

            return ap;

        }

        public MatOfDMatch(params (float queryIdx, float trainId, float imgIdx, float distance)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float queryIdx, float trainIdx, float imgIdx, float distance)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4f> span = this.AsSpan<Vec4f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].queryIdx;
                    span[i].Item2 = a[i].trainIdx;
                    span[i].Item3 = a[i].imgIdx;
                    span[i].Item4 = a[i].distance;
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
                        ptr[4 * i] = a[i].queryIdx;
                        ptr[4 * i + 1] = a[i].trainIdx;
                        ptr[4 * i + 2] = a[i].imgIdx;
                        ptr[4 * i + 3] = a[i].distance;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 4);

            Span<Vec4f> buffSpan = MemoryMarshal.Cast<float, Vec4f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].queryIdx;
                buffSpan[i].Item2 = a[i].trainIdx;
                buffSpan[i].Item3 = a[i].imgIdx;
                buffSpan[i].Item4 = a[i].distance;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 4);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 4];
            for (int i = 0; i < num; i++)
            {
                buff[4 * i + 0] = a[i].queryIdx;
                buff[4 * i + 1] = a[i].trainIdx;
                buff[4 * i + 2] = a[i].imgIdx;
                buff[4 * i + 3] = a[i].distance;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 4);
#endif

        }

        public (float queryIdx, float trainIdx, float imgIdx, float distance)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float queryIdx, float trainIdx, float imgIdx, float distance)[] ap = new (float queryIdx, float trainIdx, float imgIdx, float distance)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4f> span = this.AsSpan<Vec4f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].queryIdx = span[i].Item1;
                    ap[i].trainIdx = span[i].Item2;
                    ap[i].imgIdx = span[i].Item3;
                    ap[i].distance = span[i].Item4;
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
                        ap[i].queryIdx = ptr[4 * i];
                        ap[i].trainIdx = ptr[4 * i + 1];
                        ap[i].imgIdx = ptr[4 * i + 2];
                        ap[i].distance = ptr[4 * i + 3];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 4);

            Converters.copyMatToArray<float>(this, buff, num * 4);

            Span<Vec4f> buffSpan = MemoryMarshal.Cast<float, Vec4f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].queryIdx = buffSpan[i].Item1;
                ap[i].trainIdx = buffSpan[i].Item2;
                ap[i].imgIdx = buffSpan[i].Item3;
                ap[i].distance = buffSpan[i].Item4;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 4];

            Converters.copyMatToArray<float>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].queryIdx = buff[i * 4];
                ap[i].trainIdx = buff[i * 4 + 1];
                ap[i].imgIdx = buff[i * 4 + 2];
                ap[i].distance = buff[i * 4 + 3];
            }
#endif


            return ap;

        }

        public MatOfDMatch(params UnityEngine.Vector4[] a)
            : base()
        {

            fromVector4Array(a);
        }

        public void fromVector4Array(params UnityEngine.Vector4[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<UnityEngine.Vector4>(a, this, num);

        }

        public UnityEngine.Vector4[] toVector4Array()
        {
            int num = (int)total();
            UnityEngine.Vector4[] ap = new UnityEngine.Vector4[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<UnityEngine.Vector4>(this, ap, num);

            return ap;

        }
    }
}

