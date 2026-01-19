using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.CoreModule
{
    public partial class MatOfKeyPoint : Mat
    {

        public MatOfKeyPoint(params Vec7f[] a)
           : base()
        {

            fromVec7fArray(a);
        }

        public void fromVec7fArray(params Vec7f[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

            Converters.copyArrayToMat<Vec7f>(a, this, num);

        }

        public Vec7f[] toVec7fArray()
        {
            int num = (int)total();
            Vec7f[] ap = new Vec7f[num];
            if (num == 0)
                return ap;

            Converters.copyMatToArray<Vec7f>(this, ap, num);

            return ap;

        }

        public MatOfKeyPoint(params (float x, float y, float size, float angle, float response, float octave, float class_id)[] a)
            : base()
        {

            fromValueTupleArray(a);
        }

        public void fromValueTupleArray(params (float x, float y, float size, float angle, float response, float octave, float class_id)[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length;
            alloc(num);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec7f> span = this.AsSpan<Vec7f>();
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Item1 = a[i].x;
                    span[i].Item2 = a[i].y;
                    span[i].Item3 = a[i].size;
                    span[i].Item4 = a[i].angle;
                    span[i].Item5 = a[i].response;
                    span[i].Item6 = a[i].octave;
                    span[i].Item7 = a[i].class_id;
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
                        ptr[7 * i] = a[i].x;
                        ptr[7 * i + 1] = a[i].y;
                        ptr[7 * i + 2] = a[i].size;
                        ptr[7 * i + 3] = a[i].angle;
                        ptr[7 * i + 4] = a[i].response;
                        ptr[7 * i + 5] = a[i].octave;
                        ptr[7 * i + 6] = a[i].class_id;
                    }
                }
                return;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 7);

            Span<Vec7f> buffSpan = MemoryMarshal.Cast<float, Vec7f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                buffSpan[i].Item1 = a[i].x;
                buffSpan[i].Item2 = a[i].y;
                buffSpan[i].Item3 = a[i].size;
                buffSpan[i].Item4 = a[i].angle;
                buffSpan[i].Item5 = a[i].response;
                buffSpan[i].Item6 = a[i].octave;
                buffSpan[i].Item7 = a[i].class_id;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 7);

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 7];
            for (int i = 0; i < a.Length; i++)
            {
                buff[7 * i + 0] = a[i].x;
                buff[7 * i + 1] = a[i].y;
                buff[7 * i + 2] = a[i].size;
                buff[7 * i + 3] = a[i].angle;
                buff[7 * i + 4] = a[i].response;
                buff[7 * i + 5] = a[i].octave;
                buff[7 * i + 6] = a[i].class_id;
            }

            Converters.copyArrayToMat<float>(buff, this, num * 7);
#endif

        }

        public (float x, float y, float size, float angle, float response, float octave, float class_id)[] toValueTupleArrayAsFloat()
        {
            int num = (int)total();
            (float x, float y, float size, float angle, float response, float octave, float class_id)[] ap = new (float x, float y, float size, float angle, float response, float octave, float class_id)[num];
            if (num == 0)
                return ap;

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            if (this.isContinuous())
            {
                Span<Vec7f> span = this.AsSpan<Vec7f>();
                for (int i = 0; i < span.Length; i++)
                {
                    ap[i].x = span[i].Item1;
                    ap[i].y = span[i].Item2;
                    ap[i].size = span[i].Item3;
                    ap[i].angle = span[i].Item4;
                    ap[i].response = span[i].Item5;
                    ap[i].octave = span[i].Item6;
                    ap[i].class_id = span[i].Item7;
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
                        ap[i].x = ptr[7 * i];
                        ap[i].y = ptr[7 * i + 1];
                        ap[i].size = ptr[7 * i + 2];
                        ap[i].angle = ptr[7 * i + 3];
                        ap[i].response = ptr[7 * i + 4];
                        ap[i].octave = ptr[7 * i + 5];
                        ap[i].class_id = ptr[7 * i + 6];
                    }
                }
                return ap;
            }
#endif

#if NET_STANDARD_2_1
            float[] buff = ArrayPool<float>.Shared.Rent(num * 7);

            Converters.copyMatToArray<float>(this, buff, num * 7);

            Span<Vec7f> buffSpan = MemoryMarshal.Cast<float, Vec7f>(buff.AsSpan<float>()).Slice(0, num);
            for (int i = 0; i < buffSpan.Length; i++)
            {
                ap[i].x = buffSpan[i].Item1;
                ap[i].y = buffSpan[i].Item2;
                ap[i].size = buffSpan[i].Item3;
                ap[i].angle = buffSpan[i].Item4;
                ap[i].response = buffSpan[i].Item5;
                ap[i].octave = buffSpan[i].Item6;
                ap[i].class_id = buffSpan[i].Item7;
            }

            ArrayPool<float>.Shared.Return(buff);
#else
            float[] buff = new float[num * 7];

            Converters.copyMatToArray<float>(this, buff, num * 7);
            for (int i = 0; i < ap.Length; i++)
            {
                ap[i].x = buff[i * 7];
                ap[i].y = buff[i * 7 + 1];
                ap[i].size = buff[i * 7 + 2];
                ap[i].angle = buff[i * 7 + 3];
                ap[i].response = buff[i * 7 + 4];
                ap[i].octave = buff[i * 7 + 5];
                ap[i].class_id = buff[i * 7 + 5];
            }
#endif


            return ap;

        }

    }
}

