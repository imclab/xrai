#if ARFOUNDATION_4_0_2_OR_NEWER
    using XRCameraImageConversionParams = UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams;
    using AsyncCameraImageConversionStatus = UnityEngine.XR.ARSubsystems.XRCpuImage.AsyncConversionStatus;
    using UnityEngine.XR.ARFoundation;
    using CameraImageFormat = UnityEngine.XR.ARSubsystems.XRCpuImage.Format;
    using CameraImageTransformation = UnityEngine.XR.ARSubsystems.XRCpuImage.Transformation;
    using XRCameraImagePlane = UnityEngine.XR.ARSubsystems.XRCpuImage.Plane;
#else
    using XRCpuImage = UnityEngine.XR.ARSubsystems.XRCameraImage;
#endif
using System;
using System.Collections;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;


namespace ARFoundationRemoteExamples {
    public class CpuImagesExample : MonoBehaviour {
        [SerializeField] SetupARFoundationVersionSpecificComponents setup = null;
        [SerializeField] RawImage image = null;
        [SerializeField] Toggle mirrorX = null, mirrorY = null;
        [SerializeField] RectTransform mirrorTogglesHolder = null;
        [SerializeField] bool logWarnings = false;
        [SerializeField] CpuImageType cpuImageType = CpuImageType.Camera;
        [SerializeField] CpuImageMode mode = CpuImageMode.ConvertToRGB;

        [SerializeField, HideInInspector] CpuImageConversionType conversionType = CpuImageConversionType.Sync;
        [SerializeField, HideInInspector, Range(0.01f, 1)] float textureScale = 0.3f;
        [SerializeField, HideInInspector, Range(0, 2)] int rawImagePlaneIndex = 0;
        #pragma warning disable 414
        [SerializeField, HideInInspector, Range(1, 100)] int androidDepthTextureMultiplier = 30;
        [SerializeField, HideInInspector, UsedImplicitly] string debugString;
        [SerializeField] bool createOcclusionManager = true;
        #pragma warning restore 414

        [CanBeNull] Texture2D texture;


        #if ARFOUNDATION_4_0_2_OR_NEWER
        AROcclusionManager occlusionManager;
        void Awake() {
            initOcclusionManager();
        }

        void initOcclusionManager() {
            if (!createOcclusionManager) {
                occlusionManager = setup.cameraManager.GetComponent<AROcclusionManager>();
                Assert.IsNotNull(occlusionManager, "Add AROcclusionManager manually to camera.");
                return;
            }
            
            occlusionManager = setup.cameraManager.gameObject.AddComponent<AROcclusionManager>();
            Assert.IsNotNull(occlusionManager);
            occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;
            occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
            #if ARFOUNDATION_4_1_OR_NEWER
            occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            #endif
        }
        #endif

        [NotNull]
        TryAcquireCpuImageDelegate<XRCpuImage> AcquireCpuImage {
            get {
                switch (cpuImageType) {
                    case CpuImageType.Camera:
                        return setup.cameraManager.TryAcquireLatestCpuImageVersionAgnostic;
                    #if ARFOUNDATION_4_0_2_OR_NEWER
                    case CpuImageType.HumanDepth:
                        return occlusionManager.TryAcquireHumanDepthCpuImage;
                    case CpuImageType.HumanStencil:
                        return occlusionManager.TryAcquireHumanStencilCpuImage;
                    #endif
                    #if ARFOUNDATION_4_1_OR_NEWER
                    case CpuImageType.EnvironmentDepthConfidence:
                        return occlusionManager.TryAcquireEnvironmentDepthConfidenceCpuImage;
                        #if !AR_FOUNDATION_4_2_OR_NEWER
                            case CpuImageType.SmoothedEnvironmentDepth:
                                return occlusionManager.TryAcquireEnvironmentDepthCpuImage;
                        #endif
                    #endif
                    #if AR_FOUNDATION_4_2_OR_NEWER
                    case CpuImageType.SmoothedEnvironmentDepth:
                        return occlusionManager.TryAcquireSmoothedEnvironmentDepthCpuImage;
                    case CpuImageType.RawEnvironmentDepth:
                        return occlusionManager.TryAcquireRawEnvironmentDepthCpuImage;    
                    #endif
                    default:
                        throw new Exception($"Cpu image type is not supported: {cpuImageType}");
                }
            }
        }

        static TextureFormat getFormat(XRCpuImage cpuImage) {
            #if ARFOUNDATION_4_0_2_OR_NEWER
            var format = cpuImage.format.AsTextureFormat();
            if ((int) format != 0) {
                return format;
            }
            #endif

            return TextureFormat.ARGB32;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "IteratorNeverReturns")]
        IEnumerator Start() {
            while (true) {
                if (AcquireCpuImage(out var cpuImage)) {
                    using (cpuImage) {
                        switch (mode) {
                            case CpuImageMode.ConvertToRGB:
                                yield return convertToRgba(cpuImage);
                                break;
                            case CpuImageMode.RawImagePlanes:
                                loadFromRawImagePlane(cpuImage);
                                break;
                        }
                    }
                }

                yield return null;
            }
        }

        IEnumerator convertToRgba(XRCpuImage cpuImage) {
            var format = getFormat(cpuImage);
            var fullWidth = cpuImage.width;
            var fullHeight = cpuImage.height;
            var downsizedWidth = Mathf.RoundToInt(fullWidth * textureScale);
            var downsizedHeight = Mathf.RoundToInt(fullHeight * textureScale);
            var conversionParams = new XRCameraImageConversionParams {
                transformation = Utils.GetTransformation(mirrorX.isOn, mirrorY.isOn),
                inputRect = new RectInt(0, 0, fullWidth, fullHeight),
                outputDimensions = new Vector2Int(downsizedWidth, downsizedHeight),
                outputFormat = format
            };

            switch (conversionType) {
                case CpuImageConversionType.Sync: {
                    var convertedDataSize = tryGetConvertedDataSize();
                    if (convertedDataSize.HasValue) {
                        using (var buffer = new NativeArray<byte>(convertedDataSize.Value, Allocator.Temp)) {
                            if (tryConvert()) {
                                loadRawTextureData(buffer);
                            }

                            bool tryConvert() {
                                try {
                                    cpuImage.ConvertSync(conversionParams, buffer);
                                    return true;
                                } catch (Exception e) {
                                    processException(e);
                                    return false;
                                }
                            }
                        }
                    }

                    int? tryGetConvertedDataSize() {
                        try {
                            return cpuImage.GetConvertedDataSize(conversionParams);
                        } catch (Exception e) {
                            processException(e);
                            return null;
                        }
                    }

                    break;
                }
                case CpuImageConversionType.AsyncCoroutine: {
                    using (var conversion = cpuImage.ConvertAsync(conversionParams)) {
                        while (!conversion.status.IsDone()) {
                            yield return null;
                        }

                        if (conversion.status == AsyncCameraImageConversionStatus.Ready) {
                            loadRawTextureData(conversion.GetData<byte>());
                        } else if (logWarnings) {
                            Debug.LogWarning($"ConvertAsync failed with status: {conversion.status}");
                        }
                    }

                    break;
                }
                case CpuImageConversionType.AsyncCallback: {
                    var isDone = false;
                    cpuImage.ConvertAsync(conversionParams, (status, _, data) => {
                        isDone = true;
                        if (status == AsyncCameraImageConversionStatus.Ready) {
                            Assert.IsTrue(data.IsCreated);
                            loadRawTextureData(data);
                        } else if (logWarnings) {
                            Debug.LogWarning($"ConvertAsync failed with status: {status}");
                        }
                    });

                    while (!isDone) {
                        yield return null;
                    }

                    break;
                }
                default:
                    throw new Exception();
            }

            void loadRawTextureData(NativeArray<byte> data) {
                LoadRawTextureData(downsizedWidth, downsizedHeight, format, data);
            }
            
            void processException(Exception e) {
                if (e.Message.Contains("AR Foundation Remote")) {
                    if (logWarnings) {
                        Debug.LogWarning(e.Message);
                    }
                } else {
                    Debug.LogException(e);
                }
            }
        }

        void loadFromRawImagePlane(XRCpuImage cpuImage) {
            if (rawImagePlaneIndex >= cpuImage.planeCount) {
                debugString = $"Wrong cpu image plane index: {rawImagePlaneIndex}. Total plane count: {cpuImage.planeCount}";
                return;
            }

            if (!cpuImage.TryGetPlane(rawImagePlaneIndex, out var plane)) {
                return;
            }
            
            var data = plane.data;
            var rowStride = plane.rowStride;
            var pixelStride = plane.pixelStride;
            var width = Utils.divideWithoutRemainder(rowStride, pixelStride);
            var height = Mathf.RoundToInt((float) data.Length / rowStride);
            var format = getTextureFormat();
            debugString = $"cpuImage: ({cpuImage.width}, {cpuImage.height}), rowStride: {rowStride}, pixelStride: {pixelStride}, texture: ({width}, {height}, {format})";
            var rawData = appendOneByteIfNeeded(out var shouldDisposeArray);
            tryMultiplyAndroidDepthTexture();
            LoadRawTextureData(width, height, format, rawData);
            if (shouldDisposeArray) {
                rawData.Dispose();
            }
            
            TextureFormat getTextureFormat() {
                switch (cpuImage.format) {
                    #if ARFOUNDATION_4_0_2_OR_NEWER
                    case CameraImageFormat.OneComponent8:
                        return TextureFormat.R8;
                    case CameraImageFormat.DepthFloat32:
                        return TextureFormat.RFloat;
                    #endif
                    #if ARFOUNDATION_4_1_OR_NEWER
                    case CameraImageFormat.DepthUint16:
                        return TextureFormat.R16;
                    #endif
                    case CameraImageFormat.AndroidYuv420_888:
                    case CameraImageFormat.IosYpCbCr420_8BiPlanarFullRange: {
                        switch (pixelStride) {
                            case 1:
                                return TextureFormat.R8;
                            case 2:
                                return TextureFormat.RHalf;
                            case 4:
                                return TextureFormat.RFloat;
                            default:
                                throw new Exception();
                        }
                    }
                    default:
                        throw new Exception($"Unknown cpuImage.format: {(int) cpuImage.format}");
                }
            }
            
            NativeArray<byte> appendOneByteIfNeeded(out bool _shouldDisposeResult) {
                var expectedSize = width * height * pixelStride;
                var diff = expectedSize - data.Length;
                switch (diff) {
                    case 0:
                        _shouldDisposeResult = false;
                        return data;
                    case 1:
                        _shouldDisposeResult = true;
                        var result = new NativeArray<byte>(expectedSize, Allocator.Temp);
                        NativeArray<byte>.Copy(data, result, data.Length);
                        return result;
                    default:
                        throw new Exception($"expectedSize {expectedSize}, actual {data.Length}");
                }
            }

            void tryMultiplyAndroidDepthTexture() {
                #if ARFOUNDATION_4_1_OR_NEWER
                if (cpuImage.format != CameraImageFormat.DepthUint16) {
                    return;
                }
                
                Assert.AreEqual(pixelStride, 2);
                for (int i = 0; i < rawData.Length; i += pixelStride) {
                    var bytes = rawData.GetSubArray(i, pixelStride).ToArray();
                    ushort val = BitConverter.ToUInt16(bytes, 0);
                    var multiplied = Math.Min(val * androidDepthTextureMultiplier, ushort.MaxValue);
                    NativeArray<byte>.Copy(BitConverter.GetBytes(multiplied), 0, rawData, i, pixelStride);
                }
                #endif
            }
        }

        void LoadRawTextureData(int width, int height, TextureFormat format, NativeArray<byte> data) {
            if (texture != null) {
                Destroy(texture);
                texture = null;
            }

            texture = new Texture2D(width, height, format, false);
            texture.LoadRawTextureData(data);
            texture.Apply();
            image.texture = texture;
        }

        enum CpuImageConversionType {
            Sync,
            AsyncCoroutine,
            AsyncCallback
        }

        void Update() {
            mirrorTogglesHolder.gameObject.SetActive(mode == CpuImageMode.ConvertToRGB);
        }
    }
    
    
    enum CpuImageMode {
        ConvertToRGB,
        RawImagePlanes,
        None
    }
    
    enum CpuImageType {
        Camera,
        HumanDepth,
        HumanStencil,
        SmoothedEnvironmentDepth,
        RawEnvironmentDepth,
        EnvironmentDepthConfidence
    }
    
    delegate bool TryAcquireCpuImageDelegate<TResult>(out TResult res);

    static class Utils {
        public static
            #if !ARFOUNDATION_4_0_2_OR_NEWER
            unsafe
            #endif
            void ConvertSync(this XRCpuImage cpuImage, XRCameraImageConversionParams conversionParams, NativeArray<byte> buffer) {
                cpuImage.Convert(conversionParams, 
                    #if ARFOUNDATION_4_0_2_OR_NEWER
    buffer);
                    #else
                    new IntPtr(Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(buffer)), buffer.Length);
                    #endif
        }

        public static bool TryGetPlane(this XRCpuImage image, int index, out XRCameraImagePlane plane) {
            try {
                plane = image.GetPlane(index);
                return true;
            } catch (InvalidOperationException) {
                plane = default;
                return false;
            }
        }

        [Pure]
        public static int divideWithoutRemainder(int a, int b, string debug = null) {
            Assert.AreEqual(0, a % b, $"can't divide without remainder, {debug}, {a}, {b}");
            return a / b;
        }
        
        /// CameraImageTransformation enum has changed between different AR Foundation versions
        /// This method makes CameraImageTransformation compatible between different AR Foundation versions
        [Pure]
        public static CameraImageTransformation GetTransformation(bool mirrorX, bool mirrorY) {
            var result = CameraImageTransformation.None;
            if (mirrorX) {
                result |= CameraImageTransformation.MirrorX;
            }

            if (mirrorY) {
                result |= CameraImageTransformation.MirrorY;
            }

            return result;
        }
    }
}
