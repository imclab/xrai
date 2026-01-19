using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
#if UNITY_EDITOR
using OpenCVForUnity.UnityUtils.Helper.Editor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// A helper component for managing AR (Augmented Reality) functionalities.
    /// </summary>
    /// <remarks>
    /// The <see cref="ARHelper"/> class simplifies AR-related calculations such as 
    /// object pose estimation and Transform updates by specifying the objectPoints, imagePoints, 
    /// camMatrix, and distCoeff for OpenCV's solvePnP() method.
    /// This class provides the following features:
    /// <list type="bullet">
    ///     <item><description>Configuring and updating Transforms for AR GameObject and AR Camera</description></item>
    ///     <item><description>Calculating and applying AR matrices</description></item>
    ///     <item><description>Low-pass filtering for pose stabilization</description></item>
    ///     <item><description>Calculating and managing camera parameters</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Attach this component to a GameObject to enable AR functionality:
    /// <code>
    /// // Example usage of the ARHelper component
    /// ARHelper arHelper = gameObject.AddComponent&lt;ARHelper&gt;();
    /// arHelper.Initialize(1920, 1080, 640, 480);
    ///
    /// arHelper.imagePoints = imagePointsMatOfPoint2f.toVector2Array();
    /// arHelper.objectPoints = objectPointsMatOfPoint3f.toVector3Array();
    ///
    /// arHelper.CalculateARMatrix();
    /// arHelper.UpdateTransform();
    /// </code>
    /// </example>
    public class ARHelper : MonoBehaviour
    {

        /// <summary>
        /// Target AR GameObject
        /// </summary>
        [TooltipAttribute("Target AR GameObject")]
        public GameObject arGameObject;

        /// <summary>
        /// Target AR Camera
        /// </summary>
        [TooltipAttribute("Target AR Camera")]
        public Camera arCamera;

        /// <summary>
        /// UpdateTarget Type
        /// </summary>
        public enum UpdateTarget
        {
            ARGameObject,
            ARCamera
        }

        [Space(10)]

        /// <summary>
        /// Update Target
        /// </summary>
        [TooltipAttribute("Specifies the GameObject whose Transform is to be updated.")]
        public UpdateTarget updateTarget;

        [Space(10)]

        /// <summary>
        /// If true, the CalculateARMatrix() method is automatically called in the LateUpdate() method; if false, you must call it yourself.
        /// </summary>
        [TooltipAttribute("If true, the CalculateARMatrix() method is automatically called in the LateUpdate() method; if false, you must call it yourself.")]
        public bool calculateARMatrixInLateUpdate = true;

        /// <summary>
        /// If true, the UpdateTransform() method is automatically called in the LateUpdate() method; if false, you must call it yourself.
        /// </summary>
        [TooltipAttribute("If true, the UpdateTransform() method is automatically called in the LateUpdate() method; if false, you must call it yourself.")]
        public bool updateTransformInLateUpdate = true;


        [Header("Camera Parameters")]


        [SerializeField, FormerlySerializedAs("screenWidth"), TooltipAttribute("Set the width of screen.")]
        protected int _screenWidth = 640;

        /// <summary>
        /// Set the width of screen.
        /// </summary>
        public virtual int screenWidth
        {
            get { return _screenWidth; }
            set
            {
                int _value = Mathf.Clamp(value, 0, int.MaxValue);
                if (_screenWidth != _value)
                {
                    _screenWidth = _value;
                    if (hasInitDone) Initialize();
                }
            }
        }


        [SerializeField, FormerlySerializedAs("screenHeight"), TooltipAttribute("Set the height of screen.")]
        protected int _screenHeight = 480;

        /// <summary>
        /// Set the height of screen.
        /// </summary>
        public virtual int screenHeight
        {
            get { return _screenHeight; }
            set
            {
                int _value = Mathf.Clamp(value, 0, int.MaxValue);
                if (_screenHeight != _value)
                {
                    _screenHeight = _value;
                    if (hasInitDone) Initialize();
                }
            }
        }


        [SerializeField, FormerlySerializedAs("imageWidth"), TooltipAttribute("Set the width of image.")]
        protected int _imageWidth = 640;

        /// <summary>
        /// Set the width of image.
        /// </summary>
        public virtual int imageWidth
        {
            get { return _imageWidth; }
            set
            {
                int _value = Mathf.Clamp(value, 0, int.MaxValue);
                if (_imageWidth != _value)
                {
                    _imageWidth = _value;
                    if (hasInitDone) Initialize();
                }
            }
        }


        [SerializeField, FormerlySerializedAs("imageHeight"), TooltipAttribute("Set the height of image.")]
        protected int _imageHeight = 480;

        /// <summary>
        /// Set the height of image.
        /// </summary>
        public virtual int imageHeight
        {
            get { return _imageHeight; }
            set
            {
                int _value = Mathf.Clamp(value, 0, int.MaxValue);
                if (_imageHeight != _value)
                {
                    _imageHeight = _value;
                    if (hasInitDone) Initialize();
                }
            }
        }


        [SerializeField, FormerlySerializedAs("camMatrixValues"), Tooltip("Specifies the initial value of camMatrix used for camera calibration. If the number of elements in this array is less than 9, it is automatically calculated from the Screen's Width and Height and the Image's Width and Height.")]
#if UNITY_EDITOR
        [LabeledArray("f_x", "01", "c_x", "10", "f_y", "c_y", "20", "21", "22")]
#endif
        protected double[] _camMatrixValues = null;

        /// <summary>
        /// Specifies the initial value of camMatrix used for camera calibration.  If the number of elements in this array is less than 9, it is automatically calculated from the Screen's Width and Height and the Image's Width and Height.
        /// </summary>
        public virtual double[] camMatrixValues
        {
            get { return _camMatrixValues; }
            set
            {
                _camMatrixValues = value;
                _camMatrixValues = ValidateArraySize(_camMatrixValues, CAMMATRIXVALUES_MAX_SIZE);
                if (hasInitDone) Initialize();
            }
        }


        [SerializeField, FormerlySerializedAs("distCoeffsValues"), Tooltip("Specifies the initial value of distCoeffs used for camera calibration. If the number of elements in this array is less than 5, all elements are set to 0.")]
#if UNITY_EDITOR
        [LabeledArray("k_1", "k_2", "k_3", "p_1", "p_2", "k_4", "k_5", "k_6", "s_1", "s_2", "s_3", "s_4", "τ_x", "τ_y")]
#endif
        protected double[] _distCoeffsValues = null;

        /// <summary>
        /// Specifies the initial value of distCoeffs used for camera calibration. If the number of elements in this array is less than 5, all elements are set to 0.
        /// </summary>
        public virtual double[] distCoeffsValues
        {
            get { return _distCoeffsValues; }
            set
            {
                _distCoeffsValues = value;
                _distCoeffsValues = ValidateArraySize(_distCoeffsValues, DISTCOEFFSVALUES_MAX_SIZE);
                if (hasInitDone) Initialize();
            }
        }


        [Header("2D Points")]

        /// <summary>
        /// Specify the imagePoints argument to the Calib3d.solvePnP() method.
        /// </summary>
        [TooltipAttribute("Specify the imagePoints argument to the Calib3d.solvePnP() method.")]
        public Vector2[] imagePoints;

        [Header("3D Points")]

        /// <summary>
        /// Enable this flag if the object point is a left-hand coordinate system (OpenCV).
        /// </summary>
        [TooltipAttribute("Enable this flag if the object point is a left-hand coordinate system (Unity).")]
        public bool leftHandedCoordinates = false;

        /// <summary>
        /// Specify the objectPoints argument to the Calib3d.solvePnP() method.
        /// </summary>
        [TooltipAttribute("Specify the objectPoints argument to the Calib3d.solvePnP() method.")]
        public Vector3[] objectPoints;



        [Header("LowPassFilter")]

        /// <summary>
        /// When enabled, LowPassFilter suppresses noise.
        /// </summary>
        [TooltipAttribute("When enabled, LowPassFilter suppresses noise.")]
        public bool useLowPassFilter = false;

        /// <summary>
        /// Position parameter of LowPassFilter (Value in meters)
        /// </summary>
        [TooltipAttribute("Position parameter of LowPassFilter")]
        [Range(0.0f, 10.0f)]
        public float positionLowPassParam = 4f;

        /// <summary>
        /// Rotation parameter of LowPassFilter (Value in degrees)
        /// </summary>
        [TooltipAttribute("Rotation parameter of LowPassFilter")]
        [Range(0.0f, 10.0f)]
        public float rotationLowPassParam = 2f;


        [Header("Apply axis inversion to ARMatrix")]

        /// <summary>
        /// Apply X-axis inversion to ARMatrix.
        /// </summary>
        [TooltipAttribute("Apply X-axis inversion to ARMatrix.")]
        public bool applyXaxisInversionToARMatrix = false;

        /// <summary>
        /// Apply Y-axis inversion to ARMatrix.
        /// </summary>
        [TooltipAttribute("Apply Y-axis inversion to ARMatrix.")]
        public bool applyYaxisInversionToARMatrix = false;

        /// <summary>
        /// Apply X-axis inversion to ARMatrix.
        /// </summary>
        [TooltipAttribute("Apply Z-axis inversion to ARMatrix.")]
        public bool applyZaxisInversionToARMatrix = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool hasInitDone = false;

        /// <summary>
        /// The old pose data.
        /// </summary>
        private PoseData oldPoseData;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        private Mat camMatrix;

        /// <summary>
        /// The dist coeffs.
        /// </summary>
        private MatOfDouble distCoeffs;

        /// <summary>
        /// The matrix that inverts the X axis.
        /// </summary>
        private Matrix4x4 invertXMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1, 1, 1));

        /// <summary>
        /// The matrix that inverts the Y axis.
        /// </summary>
        private Matrix4x4 invertYMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));

        /// <summary>
        /// The matrix that inverts the Z axis.
        /// </summary>
        private Matrix4x4 invertZMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));

        /// <summary>
        /// The matrix for calculating isARGameObjectInARCameraViewport.
        /// </summary>
        private Matrix4x4 opencvCameraProjectionMatrix;

        /// <summary>
        /// The transformation matrix from poseData.
        /// </summary>
        private Matrix4x4 transformMatrix;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        private Matrix4x4 arMatrix;

        /// <summary>
        /// The rvec Mat for Calib3d.solvePnP().
        /// </summary>
        private Mat rvec;

        /// <summary>
        /// The tvec Mat for Calib3d.solvePnP().
        /// </summary>
        private Mat tvec;

        /// <summary>
        /// Is the ARGameObject in the ARCameraViewport? This flag is updated when CalculateARMatrix() is called.
        /// </summary>
        private bool isARGameObjectInARCameraViewport = false;

        /// <summary>
        /// CAMMATRIXVALUES_MIN_SIZE
        /// </summary>
        private const int CAMMATRIXVALUES_MIN_SIZE = 9;

        /// <summary>
        /// CAMMATRIXVALUES_MAX_SIZE
        /// </summary>
        private const int CAMMATRIXVALUES_MAX_SIZE = 9;

        /// <summary>
        /// DISTCOEFFSVALUES_MIN_SIZE
        /// </summary>
        private const int DISTCOEFFSVALUES_MIN_SIZE = 5;

        /// <summary>
        /// DISTCOEFFSVALUES_MAX_SIZE
        /// </summary>
        private const int DISTCOEFFSVALUES_MAX_SIZE = 14;

        /// <summary>
        /// arGameObjectDefaultLocalPosition
        /// </summary>
        private Vector3 arGameObjectDefaultLocalPosition;

        /// <summary>
        /// arGameObjectDefaultLocalRotation
        /// </summary>
        private Quaternion arGameObjectDefaultLocalRotation;

        /// <summary>
        /// arGameObjectDefaultLocalScale
        /// </summary>
        private Vector3 arGameObjectDefaultLocalScale;

        /// <summary>
        /// arCameraDefaultLocalPosition
        /// </summary>
        private Vector3 arCameraDefaultLocalPosition;

        /// <summary>
        /// arCameraDefaultLocalRotation
        /// </summary>
        private Quaternion arCameraDefaultLocalRotation;

        /// <summary>
        /// arCameraDefaultLocalScale
        /// </summary>
        private Vector3 arCameraDefaultLocalScale;

        /// <summary>
        /// arCameraDefaultFieldOfView
        /// </summary>
        private float arCameraDefaultFieldOfView;

        protected virtual void OnValidate()
        {
            //Debug.Log("OnValidate");

            _screenWidth = Mathf.Clamp(_screenWidth, 0, int.MaxValue);
            _screenHeight = Mathf.Clamp(_screenHeight, 0, int.MaxValue);
            _imageWidth = Mathf.Clamp(_imageWidth, 0, int.MaxValue);
            _imageHeight = Mathf.Clamp(_imageHeight, 0, int.MaxValue);

            _camMatrixValues = ValidateArraySize(_camMatrixValues, CAMMATRIXVALUES_MAX_SIZE);
            _distCoeffsValues = ValidateArraySize(_distCoeffsValues, DISTCOEFFSVALUES_MAX_SIZE);

            if (hasInitDone) Initialize();
        }

        /// <summary>
        /// Restricts the size of the array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="maxSize">The maximum size.</param>
        /// <returns>The array after applying the size restriction.</returns>
        private double[] ValidateArraySize(double[] array, int maxSize)
        {
            if (array != null && array.Length > maxSize)
            {
                Debug.LogWarning($"The size of the array exceeds the limit of {maxSize}. It will be truncated.");
                System.Array.Resize(ref array, maxSize);
            }
            return array;
        }

        void LateUpdate()
        {
            //Debug.Log("LateUpdate");

            if (!hasInitDone) return;

            if (calculateARMatrixInLateUpdate)
                CalculateARMatrix();
            if (updateTransformInLateUpdate)
                UpdateTransform();
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (hasInitDone)
                Dispose();
        }

        /// <summary>
        /// Initializes the <see cref="ARHelper"/>.
        /// </summary>
        public virtual void Initialize()
        {
            //Debug.Log("Initialize");

            _Initialize();
        }

        /// <summary>
        /// Initializes the <see cref="ARHelper"/>.
        /// </summary>
        public virtual void Initialize(int screenWidth, int screenHeight, int imageWidth, int imageHeight, double[] camMatrixValues = null, double[] distCoeffsValues = null, Vector2[] imagePoints = null, Vector3[] objectPoints = null)
        {
            //Debug.Log("Initialize");

            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _imageWidth = imageWidth;
            _imageHeight = imageHeight;

            if (camMatrixValues != null)
                _camMatrixValues = ValidateArraySize(camMatrixValues, 9);
            if (distCoeffsValues != null)
                _distCoeffsValues = ValidateArraySize(distCoeffsValues, 14);

            if (imagePoints != null)
                this.imagePoints = imagePoints;

            if (objectPoints != null)
                this.objectPoints = objectPoints;

            _Initialize();
        }

        /// <summary>
        /// Releases all resource used by the <see cref="ARHelper"/>.
        /// </summary>
        public virtual void Dispose()
        {
            //Debug.Log("Dispose");

            ResetUpdateTargetTransform();

            hasInitDone = false;

            if (camMatrix != null)
            {
                camMatrix.Dispose();
                camMatrix = null;
            }

            if (distCoeffs != null)
            {
                distCoeffs.Dispose();
                distCoeffs = null;
            }

            if (rvec != null)
            {
                rvec.Dispose();
                rvec = null;
            }

            if (tvec != null)
            {
                tvec.Dispose();
                tvec = null;
            }

        }

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized()
        {
            return hasInitDone;
        }

        private void _Initialize()
        {
            //Debug.Log("_Initialize");

            if (hasInitDone)
            {
                Dispose();
            }

            if (arGameObject == null)
                return;

            if (arCamera == null)
                return;


            // Save initial values for ARGameObject and ARCamera.
            arGameObjectDefaultLocalPosition = arGameObject.transform.localPosition;
            arGameObjectDefaultLocalRotation = arGameObject.transform.localRotation;
            arGameObjectDefaultLocalScale = arGameObject.transform.localScale;

            arCameraDefaultLocalPosition = arCamera.transform.localPosition;
            arCameraDefaultLocalRotation = arCamera.transform.localRotation;
            arCameraDefaultLocalScale = arCamera.transform.localScale;
            arCameraDefaultFieldOfView = arCamera.fieldOfView;


            //Debug.Log("Screen.width " + Screen.width);
            //Debug.Log("Screen.height " + Screen.height);

            float width = _imageWidth;
            float height = _imageHeight;
            //Debug.Log("width " + width);
            //Debug.Log("height " + height);

            float imageSizeScale = 1.0f;
            float widthScale = (float)_screenWidth / width;
            float heightScale = (float)_screenHeight / height;
            if (widthScale < heightScale)
            {
                imageSizeScale = (float)_screenHeight / (float)_screenWidth;
            }
            else
            {

            }
            //Debug.Log("imageSizeScale " + imageSizeScale);
            //Debug.Log("widthScale " + widthScale);
            //Debug.Log("heightScale " + heightScale);


            // Initialise camMatrix.
            //Debug.Log("camMatrixValues.Length " + _camMatrixValues.Length);
            if (_camMatrixValues.Length < CAMMATRIXVALUES_MIN_SIZE)
            {
                _camMatrixValues = new double[CAMMATRIXVALUES_MIN_SIZE];

                //set cameraparam
                int max_d = (int)Mathf.Max(width, height);
                double _fx = max_d;
                double _fy = max_d;
                double _cx = width / 2.0f;
                double _cy = height / 2.0f;

                _camMatrixValues[0] = _fx;
                _camMatrixValues[1] = 0;
                _camMatrixValues[2] = _cx;
                _camMatrixValues[3] = 0;
                _camMatrixValues[4] = _fy;
                _camMatrixValues[5] = _cy;
                _camMatrixValues[6] = 0;
                _camMatrixValues[7] = 0;
                _camMatrixValues[8] = 1.0f;
            }
            camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            MatUtils.copyToMat<double>(_camMatrixValues, camMatrix);
            Debug.Log("camMatrix " + camMatrix.dump());


            // Initialise distCoeff.
            //Debug.Log("distCoeffsValues.Length " + _distCoeffsValues.Length);
            if (_distCoeffsValues.Length < DISTCOEFFSVALUES_MIN_SIZE)
            {
                _distCoeffsValues = new double[DISTCOEFFSVALUES_MIN_SIZE];
            }
            distCoeffs = new MatOfDouble(_distCoeffsValues);
            Debug.Log("distCoeffs " + distCoeffs.dump());



            //calibration camera
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            //To convert the difference of the FOV value of the OpenCV and Unity. 
            double fx = _camMatrixValues[0];
            double fy = _camMatrixValues[4];
            double cx = _camMatrixValues[2];
            double cy = _camMatrixValues[5];
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);

            if (arCamera != null)
            {
                //Adjust Unity Camera FOV
                if (widthScale < heightScale)
                {
                    arCamera.fieldOfView = (float)(fovx[0] * fovXScale);
                }
                else
                {
                    arCamera.fieldOfView = (float)(fovy[0] * fovYScale);
                }
            }

            oldPoseData = new PoseData();
            transformMatrix = Matrix4x4.identity;
            arMatrix = Matrix4x4.identity;

            // create opencvCameraProjectionMatrix for calculating isARGameObjectInARCameraViewport
            Matrix4x4 openGLCameraProjectionMatrix = ARUtils.CalculateProjectionMatrixFromCameraMatrixValues((float)fx, (float)fy, (float)cx, (float)cy, width, height, arCamera.nearClipPlane, arCamera.farClipPlane);
            Matrix4x4 zaxisInvertionMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            opencvCameraProjectionMatrix = openGLCameraProjectionMatrix * zaxisInvertionMatrix;

            isARGameObjectInARCameraViewport = false;

            hasInitDone = true;
        }

        /// <summary>
        /// Calculate ARMatrix from set parameters.
        /// </summary>
        public virtual void CalculateARMatrix()
        {
            if (!hasInitDone) return;

            if (arGameObject == null)
                return;
            if (arCamera == null)
                return;

            if (camMatrix == null)
                return;
            if (distCoeffs == null)
                return;

            if (this.imagePoints == null)
                return;
            if (this.objectPoints == null)
                return;

            if (this.imagePoints.Length != this.objectPoints.Length)
                return;
            if (this.imagePoints.Length < 4)
                return;
            if (this.objectPoints.Length < 4)
                return;


            Vector2[] imagePoints = this.imagePoints;
            Vector3[] objectPoints;
            if (leftHandedCoordinates)
            {
                objectPoints = new Vector3[this.objectPoints.Length];
                for (int i = 0; i < this.objectPoints.Length; i++)
                    objectPoints[i] = new Vector3(this.objectPoints[i].x, -this.objectPoints[i].y, this.objectPoints[i].z);
            }
            else
            {
                objectPoints = this.objectPoints;
            }

            using (MatOfPoint2f m_markerCorners2d = new MatOfPoint2f(imagePoints))
            using (MatOfPoint3f m_markerCorners3d = new MatOfPoint3f(objectPoints))
            {

                // First call to solvePnP
                if (rvec == null || tvec == null)
                {
                    rvec = new Mat(3, 1, CvType.CV_64FC1);
                    tvec = new Mat(3, 1, CvType.CV_64FC1);
                    //Calib3d.solvePnP(m_markerCorners3d, m_markerCorners2d, camMatrix, distCoeffs, rvec, tvec);
                    Calib3d.solvePnP(m_markerCorners3d, m_markerCorners2d, camMatrix, distCoeffs, rvec, tvec, false, Calib3d.SOLVEPNP_UPNP);
                }


                isARGameObjectInARCameraViewport = false;
                double[] tvecValue = new double[3];
                tvec.get(0, 0, tvecValue);
                Vector4 pos = opencvCameraProjectionMatrix * new Vector4((float)tvecValue[0], (float)tvecValue[1], (float)tvecValue[2], 1.0f);
                if (pos.w != 0)
                {
                    float x = pos.x / pos.w, y = pos.y / pos.w, z = pos.z / pos.w;
                    if (x >= -1 && x <= 1 && y >= -1 && y <= 1 && z >= 0 && z <= 1)
                    {
                        isARGameObjectInARCameraViewport = true;
                    }
                }


                if (double.IsNaN(tvecValue[2]) || !isARGameObjectInARCameraViewport)
                {
                    // if tvec is wrong data, do not use extrinsic guesses. (the estimated object is not in the camera field of view)
                    //Calib3d.solvePnP(m_markerCorners3d, m_markerCorners2d, camMatrix, distCoeffs, rvec, tvec);
                    Calib3d.solvePnP(m_markerCorners3d, m_markerCorners2d, camMatrix, distCoeffs, rvec, tvec, false, Calib3d.SOLVEPNP_UPNP);
                }
                else
                {
                    Calib3d.solvePnP(m_markerCorners3d, m_markerCorners2d, camMatrix, distCoeffs, rvec, tvec, true, Calib3d.SOLVEPNP_ITERATIVE);
                }

                //Debug.Log("objectPoints.dump() " + m_markerCorners3d.dump());
                //Debug.Log("imagePoints.dump() " + m_markerCorners2d.dump());


                if (isARGameObjectInARCameraViewport)
                {
                    // Convert to unity pose data.
                    double[] rvecArr = new double[3];
                    rvec.get(0, 0, rvecArr);
                    double[] tvecArr = new double[3];
                    tvec.get(0, 0, tvecArr);
                    PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                    //Debug.Log("poseData.pos " + poseData.pos);
                    //Debug.Log("poseData.rot " + poseData.rot);

                    //Changes in pos / rot below these thresholds are ignored.
                    if (useLowPassFilter)
                    {
                        ARUtils.LowpassPoseData(ref oldPoseData, ref poseData, positionLowPassParam, rotationLowPassParam);
                    }
                    oldPoseData = poseData;

                    transformMatrix = ARUtils.ConvertPoseDataToMatrix(ref oldPoseData, true);
                }
            }

            arMatrix = transformMatrix;

            if (applyXaxisInversionToARMatrix) arMatrix = arMatrix * invertXMatrix;
            if (applyYaxisInversionToARMatrix) arMatrix = arMatrix * invertYMatrix;
            if (applyZaxisInversionToARMatrix) arMatrix = arMatrix * invertZMatrix;
        }

        /// <summary>
        /// Update the Transform of UpdateTarget using ARMatrix.
        /// </summary>
        public virtual void UpdateTransform()
        {
            if (!hasInitDone) return;

            if (arMatrix.isIdentity)
                return;

            if (updateTarget == UpdateTarget.ARCamera)
            {
                arMatrix = arGameObject.transform.localToWorldMatrix * arMatrix.inverse;
                ARUtils.SetTransformFromMatrix(arCamera.transform, ref arMatrix);
            }
            else
            {
                arMatrix = arCamera.transform.localToWorldMatrix * arMatrix;
                ARUtils.SetTransformFromMatrix(arGameObject.transform, ref arMatrix);
            }
        }

        /// <summary>
        /// Resets the Transform of updateTarget to its initial value at the time the Initialize() method is called.
        /// </summary>
        public virtual void ResetUpdateTargetTransform()
        {
            if (!hasInitDone) return;

            if (IsObjectDestroyed(arGameObject)) return;
            if (IsObjectDestroyed(arCamera)) return;

            arGameObject.transform.localPosition = arGameObjectDefaultLocalPosition;
            arGameObject.transform.localRotation = arGameObjectDefaultLocalRotation;
            arGameObject.transform.localScale = arGameObjectDefaultLocalScale;

            arCamera.transform.localPosition = arCameraDefaultLocalPosition;
            arCamera.transform.localRotation = arCameraDefaultLocalRotation;
            arCamera.transform.localScale = arCameraDefaultLocalScale;
            arCamera.fieldOfView = arCameraDefaultFieldOfView;
        }

        private bool IsObjectDestroyed(UnityEngine.Object obj)
        {
            return obj == null;
        }

        /// <summary>
        /// Get camMatrix.
        /// </summary>
        /// <returns></returns>
        public Mat GetCamMatrix()
        {
            return camMatrix;
        }

        /// <summary>
        /// Get distCoeffs.
        /// </summary>
        /// <returns></returns>
        public virtual MatOfDouble GetDistCoeffs()
        {
            return distCoeffs;
        }

        /// <summary>
        /// Get rvec.
        /// </summary>
        /// <returns></returns>
        public virtual Mat GetRvec()
        {
            return rvec;
        }

        /// <summary>
        /// Get tvec.
        /// </summary>
        /// <returns></returns>
        public virtual Mat GetTvec()
        {
            return tvec;
        }

        /// <summary>
        /// Get ARMatrix.
        /// </summary>
        /// <returns>AR Matrix</returns>
        public virtual Matrix4x4 GetARMatrix()
        {
            return arMatrix;
        }

        /// <summary>
        /// Is the ARGameObject in the ARCameraViewport? This flag is updated when CalculateARMatrix() is called.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsARGameObjectInARCameraViewport()
        {
            return isARGameObjectInARCameraViewport;
        }

        /// <summary>
        /// Set camMatrix.
        /// </summary>
        /// <param name="camMatrix"></param>
        public virtual void SetCamMatrix(Mat camMatrix)
        {
            if (camMatrix != null && !camMatrix.empty())
            {
                _camMatrixValues = new double[camMatrix.total()];
                camMatrix.get(0, 0, _camMatrixValues);
                _camMatrixValues = ValidateArraySize(_camMatrixValues, 9);

                if (hasInitDone) Initialize();
            }

        }

        /// <summary>
        /// Set distCoeffs.
        /// </summary>
        /// <param name="distCoeffs"></param>
        public virtual void SetDistCoeffs(MatOfDouble distCoeffs)
        {
            if (distCoeffs != null && !distCoeffs.empty())
            {
                _distCoeffsValues = new double[distCoeffs.total()];
                distCoeffs.get(0, 0, _distCoeffsValues);
                _distCoeffsValues = ValidateArraySize(distCoeffsValues, 14);

                if (hasInitDone) Initialize();
            }
        }
    }
}
