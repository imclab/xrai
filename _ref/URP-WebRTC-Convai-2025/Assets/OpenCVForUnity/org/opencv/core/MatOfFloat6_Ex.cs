using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfFloat6 : Mat
    {
        public MatOfFloat6(params Vec6f[] a)
            : base()
        {

            fromVec6fArray(a);
        }

        public void fromVec6fArray(params Vec6f[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec6f>(a, this, num);

        }

        public Vec6f[] toVec6fArray()
        {
            int num = (int)total();
            Vec6f[] ap = new Vec6f[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec6f>(this, ap, num);

            return ap;

        }

        public MatOfFloat6(params (float v0, float v1, float v2, float v3, float v4, float v5)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float v0, float v1, float v2, float v3, float v4, float v5)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec6f> span = this.AsSpan<Vec6f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].v0;
                    span[i].Item2 = a[i].v1;
                    span[i].Item3 = a[i].v2;
                    span[i].Item4 = a[i].v3;
                    span[i].Item5 = a[i].v4;
                    span[i].Item6 = a[i].v5;
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
                        ptr[6 * i + 0] = (float)a[i].v0;
                        ptr[6 * i + 1] = (float)a[i].v1;
                        ptr[6 * i + 2] = (float)a[i].v2;
                        ptr[6 * i + 3] = (float)a[i].v3;
                        ptr[6 * i + 4] = (float)a[i].v4;
                        ptr[6 * i + 5] = (float)a[i].v5;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 6);

            Span<Vec6f> buffSpan = MemoryMarshal.Cast<float, Vec6f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].v0;
                buffSpan[i].Item2 = a[i].v1;
                buffSpan[i].Item3 = a[i].v2;
                buffSpan[i].Item4 = a[i].v3;
                buffSpan[i].Item5 = a[i].v4;
                buffSpan[i].Item6 = a[i].v5;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 6);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 6];
            for (int i = 0; i < a.Length; i++)
            {
                buff[6 * i + 0] = a[i].v0;
                buff[6 * i + 1] = a[i].v1;
                buff[6 * i + 2] = a[i].v2;
                buff[6 * i + 3] = a[i].v3;
                buff[6 * i + 4] = a[i].v4;
                buff[6 * i + 5] = a[i].v5;

            }

            Converters.copyArrayToMat<float>(buff, this, num * 6);
#endif

        }

        public (float v0, float v1, float v2, float v3, float v4, float v5)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float v0, float v1, float v2, float v3, float v4, float v5)[] ap = new (float v0, float v1, float v2, float v3, float v4, float v5)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec6f> span = this.AsSpan<Vec6f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].v0 = span[i].Item1;
                    ap[i].v1 = span[i].Item2;
                    ap[i].v2 = span[i].Item3;
                    ap[i].v3 = span[i].Item4;
                    ap[i].v4 = span[i].Item5;
                    ap[i].v5 = span[i].Item6;
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
                        ap[i].v0 = (int)ptr[6 * i + 0];
                        ap[i].v1 = (int)ptr[6 * i + 1];
                        ap[i].v2 = (int)ptr[6 * i + 2];
                        ap[i].v3 = (int)ptr[6 * i + 3];
                        ap[i].v4 = (int)ptr[6 * i + 4];
                        ap[i].v5 = (int)ptr[6 * i + 5];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 6);

            Converters.copyMatToArray<float>(this, buff, num * 6);

            Span<Vec6f> buffSpan = MemoryMarshal.Cast<float, Vec6f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].v0 = buffSpan[i].Item1;
                ap[i].v1 = buffSpan[i].Item2;
                ap[i].v2 = buffSpan[i].Item3;
                ap[i].v3 = buffSpan[i].Item4;
                ap[i].v4 = buffSpan[i].Item5;
                ap[i].v5 = buffSpan[i].Item6;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 6];

            Converters.copyMatToArray<float>(this, buff, num * 6);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].v0 = buff[i * 6 + 0];
                ap[i].v1 = buff[i * 6 + 1];
                ap[i].v2 = buff[i * 6 + 2];
                ap[i].v3 = buff[i * 6 + 3];
                ap[i].v4 = buff[i * 6 + 4];
                ap[i].v5 = buff[i * 6 + 5];
            }
#endif
            return ap;

        }
    }
}
