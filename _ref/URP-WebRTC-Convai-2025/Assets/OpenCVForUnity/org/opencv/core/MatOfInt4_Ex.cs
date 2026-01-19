using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfInt4 : Mat
    {
        public MatOfInt4(params Vec4i[] a)
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

        public MatOfInt4(params (int v0, int v1, int v2, int v3)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (int v0, int v1, int v2, int v3)[] a)
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
                    int* ptr = (int*)this.dataAddr();
                    for (int i = 0; i < a.Length; i++)
                    {
                        ptr[4 * i + 0] = (int)a[i].v0;
                        ptr[4 * i + 1] = (int)a[i].v1;
                        ptr[4 * i + 2] = (int)a[i].v2;
                        ptr[4 * i + 3] = (int)a[i].v3;
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
                buffSpan[i].Item1 = a[i].v0;
                buffSpan[i].Item2 = a[i].v1;
                buffSpan[i].Item3 = a[i].v2;
                buffSpan[i].Item4 = a[i].v3;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];
            for (int i = 0; i < a.Length; i++)
            {
                buff[4 * i + 0] = a[i].v0;
                buff[4 * i + 1] = a[i].v1;
                buff[4 * i + 2] = a[i].v2;
                buff[4 * i + 3] = a[i].v3;
            }

            Converters.copyArrayToMat<int>(buff, this, num * 4);
#endif

        }

        public (int v0, int v1, int v2, int v3)[] toValueTupleArrayAsInt()
        {
            int num = (int)total();
            (int v0, int v1, int v2, int v3)[] ap = new (int v0, int v1, int v2, int v3)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec4i> span = this.AsSpan<Vec4i>();
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
                    int* ptr = (int*)this.dataAddr();

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
            int[] buff = ArrayPool<int>.Shared.Rent(num * 4);

            Converters.copyMatToArray<int>(this, buff, num * 4);

            Span<Vec4i> buffSpan = MemoryMarshal.Cast<int, Vec4i>(buff.AsSpan<int>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].v0 = buffSpan[i].Item1;
                ap[i].v1 = buffSpan[i].Item2;
                ap[i].v2 = buffSpan[i].Item3;
                ap[i].v3 = buffSpan[i].Item4;
            }

            ArrayPool<int>.Shared.Return(buff);
#else
            int[] buff = new int[num * 4];

            Converters.copyMatToArray<int>(this, buff, num * 4);
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
    }
}
