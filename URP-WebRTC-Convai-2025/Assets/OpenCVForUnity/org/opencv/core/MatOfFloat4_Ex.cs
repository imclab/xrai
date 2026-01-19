using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfFloat4 : Mat
    {
        public MatOfFloat4(params Vec4f[] a)
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

        public MatOfFloat4(params (float v0, float v1, float v2, float v3)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float v0, float v1, float v2, float v3)[] a)
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
                    span[i].Item1 = a[i].v0;
                    span[i].Item2 = a[i].v1;
                    span[i].Item3 = a[i].v2;
                    span[i].Item4 = a[i].v3;
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
                        ptr[4 * i + 0] = (float)a[i].v0;
                        ptr[4 * i + 1] = (float)a[i].v1;
                        ptr[4 * i + 2] = (float)a[i].v2;
                        ptr[4 * i + 3] = (float)a[i].v3;
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
                buffSpan[i].Item1 = a[i].v0;
                buffSpan[i].Item2 = a[i].v1;
                buffSpan[i].Item3 = a[i].v2;
                buffSpan[i].Item4 = a[i].v3;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 4);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = a[i].v0;
                buff[4 * i + 1] = a[i].v1;
                buff[4 * i + 2] = a[i].v2;
                buff[4 * i + 3] = a[i].v3;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 4);
#endif

        }

        public (float v0, float v1, float v2, float v3)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float v0, float v1, float v2, float v3)[] ap = new (float v0, float v1, float v2, float v3)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4f> span = this.AsSpan<Vec4f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].v0 = span[i].Item1;
                    ap[i].v1 = span[i].Item2;
                    ap[i].v2 = span[i].Item3;
                    ap[i].v3 = span[i].Item4;
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
                        ap[i].v0 = (int)ptr[4 * i + 0];
                        ap[i].v1 = (int)ptr[4 * i + 1];
                        ap[i].v2 = (int)ptr[4 * i + 2];
                        ap[i].v3 = (int)ptr[4 * i + 3];
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
                ap[i].v0 = buffSpan[i].Item1;
                ap[i].v1 = buffSpan[i].Item2;
                ap[i].v2 = buffSpan[i].Item3;
                ap[i].v3 = buffSpan[i].Item4;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 4];

            Converters.copyMatToArray<float>(this, buff, num * 4);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].v0 = buff[i * 4 + 0];
                ap[i].v1 = buff[i * 4 + 1];
                ap[i].v2 = buff[i * 4 + 2];
                ap[i].v3 = buff[i * 4 + 3];
            }
#endif
            return ap;

        }

        public MatOfFloat4(params UnityEngine.Vector4[] a)
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
