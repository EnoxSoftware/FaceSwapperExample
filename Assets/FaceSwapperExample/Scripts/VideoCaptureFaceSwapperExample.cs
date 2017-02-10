using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DlibFaceLandmarkDetector;
using OpenCVForUnity;
using OpenCVForUnity.FaceSwap;
using OpenCVForUnity.RectangleTrack;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace FaceSwapperExample
{
    /// <summary>
    /// Face Swapper from videoCapture example.
    /// </summary>
    public class VideoCaptureFaceSwapperExample : MonoBehaviour
    {

        /// <summary>
        /// The width of the frame.
        /// </summary>
        private double frameWidth = 320;

        /// <summary>
        /// The height of the frame.
        /// </summary>
        private double frameHeight = 240;

        /// <summary>
        /// The capture.
        /// </summary>
        VideoCapture capture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The face Swapper.
        /// </summary>
        DlibFaceSwapper faceSwapper;

        /// <summary>
        /// The detection based tracker.
        /// </summary>
        RectangleTracker rectangleTracker;

        /// <summary>
        /// The frontal face parameter.
        /// </summary>
        FrontalFaceParam frontalFaceParam;

        /// <summary>
        /// The is showing face rects.
        /// </summary>
        public bool isShowingFaceRects = false;

        /// <summary>
        /// The is showing face rects toggle.
        /// </summary>
        public Toggle isShowingFaceRectsToggle;

        /// <summary>
        /// The use Dlib face detector flag.
        /// </summary>
        public bool useDlibFaceDetecter = true;

        /// <summary>
        /// The use dlib face detecter toggle.
        /// </summary>
        public Toggle useDlibFaceDetecterToggle;

        /// <summary>
        /// The is filtering non frontal faces.
        /// </summary>
        public bool isFilteringNonFrontalFaces;

        /// <summary>
        /// The is filtering non frontal faces toggle.
        /// </summary>
        public Toggle isFilteringNonFrontalFacesToggle;

        /// <summary>
        /// The frontal face rate lower limit.
        /// </summary>
        [Range (0.0f, 1.0f)]
        public float
            frontalFaceRateLowerLimit;

        /// <summary>
        /// The use seamless clone for paste faces.
        /// </summary>
        public bool useSeamlessCloneForPasteFaces = false;

        /// <summary>
        /// The use seamless clone for paste faces toggle.
        /// </summary>
        public Toggle useSeamlessCloneForPasteFacesToggle;

        /// <summary>
        /// The enable tracker flag.
        /// </summary>
        public bool enableTracking = true;

        /// <summary>
        /// The enable tracking toggle.
        /// </summary>
        public Toggle enableTrackingToggle;

        /// <summary>
        /// The is showing debug face points.
        /// </summary>
        public bool isShowingDebugFacePoints = false;

        /// <summary>
        /// The is showing debug face points toggle.
        /// </summary>
        public Toggle isShowingDebugFacePointsToggle;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        private string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The shape_predictor_68_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_face_landmarks_dat_filepath;

        /// <summary>
        /// The couple_avi_filepath.
        /// </summary>
        private string couple_avi_filepath;

        // Use this for initialization
        void Start ()
        {
            capture = new VideoCapture ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(getFilePathCoroutine());
            #else
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.Utils.getFilePath ("haarcascade_frontalface_alt.xml");
            shape_predictor_68_face_landmarks_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat");
            couple_avi_filepath = OpenCVForUnity.Utils.getFilePath ("couple.avi");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator getFilePathCoroutine()
        {
            var getFilePathAsync_0_Coroutine = StartCoroutine (OpenCVForUnity.Utils.getFilePathAsync ("haarcascade_frontalface_alt.xml", (result) => {
                haarcascade_frontalface_alt_xml_filepath = result;
            }));
            var getFilePathAsync_1_Coroutine = StartCoroutine (DlibFaceLandmarkDetector.Utils.getFilePathAsync ("shape_predictor_68_face_landmarks.dat", (result) => {
                shape_predictor_68_face_landmarks_dat_filepath = result;
            }));
            var getFilePathAsync_2_Coroutine = StartCoroutine (OpenCVForUnity.Utils.getFilePathAsync ("couple.avi", (result) => {
                couple_avi_filepath = result;
            }));

            yield return getFilePathAsync_0_Coroutine;
            yield return getFilePathAsync_1_Coroutine;
            yield return getFilePathAsync_2_Coroutine;

            Run ();
        }
        #endif

        private void Run ()
        {
            rectangleTracker = new RectangleTracker ();

            frontalFaceParam = new FrontalFaceParam ();


            faceLandmarkDetector = new FaceLandmarkDetector (shape_predictor_68_face_landmarks_dat_filepath);

            faceSwapper = new DlibFaceSwapper ();
            faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessCloneForPasteFaces;
            faceSwapper.isShowingDebugFacePoints = isShowingDebugFacePoints;

            rgbMat = new Mat ();

            capture.open (couple_avi_filepath);

            if (capture.isOpened ()) {
                Debug.Log ("capture.isOpened() true");
            } else {
                Debug.Log ("capture.isOpened() false");
            }


            Debug.Log ("CAP_PROP_FORMAT: " + capture.get (Videoio.CAP_PROP_FORMAT));
            Debug.Log ("CV_CAP_PROP_PREVIEW_FORMAT: " + capture.get (Videoio.CV_CAP_PROP_PREVIEW_FORMAT));
            Debug.Log ("CAP_PROP_POS_MSEC: " + capture.get (Videoio.CAP_PROP_POS_MSEC));
            Debug.Log ("CAP_PROP_POS_FRAMES: " + capture.get (Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log ("CAP_PROP_POS_AVI_RATIO: " + capture.get (Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log ("CAP_PROP_FRAME_COUNT: " + capture.get (Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log ("CAP_PROP_FPS: " + capture.get (Videoio.CAP_PROP_FPS));
            Debug.Log ("CAP_PROP_FRAME_WIDTH: " + capture.get (Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log ("CAP_PROP_FRAME_HEIGHT: " + capture.get (Videoio.CAP_PROP_FRAME_HEIGHT));


            texture = new Texture2D ((int)(frameWidth), (int)(frameHeight), TextureFormat.RGBA32, false);
            gameObject.transform.localScale = new Vector3 ((float)frameWidth, (float)frameHeight, 1);
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            grayMat = new Mat ((int)frameHeight, (int)frameWidth, CvType.CV_8UC1);
            cascade = new CascadeClassifier (haarcascade_frontalface_alt_xml_filepath);
            if (cascade.empty ()) {
                Debug.LogError ("cascade file is not loaded.Please copy from “FaceTrackerExample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            isShowingFaceRectsToggle.isOn = isShowingFaceRects;
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
            isFilteringNonFrontalFacesToggle.isOn = isFilteringNonFrontalFaces;
            useSeamlessCloneForPasteFacesToggle.isOn = useSeamlessCloneForPasteFaces;
            enableTrackingToggle.isOn = enableTracking;
            isShowingDebugFacePointsToggle.isOn = isShowingDebugFacePoints;
        }

        // Update is called once per frame
        void Update ()
        {
            //Loop play
            if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
            if (capture.grab ()) {

                capture.retrieve (rgbMat, 0);

                Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                //Debug.Log ("Mat toString " + rgbMat.ToString ());


                //face detection
                List<OpenCVForUnity.Rect> detectResult = new List<OpenCVForUnity.Rect> ();
                if (useDlibFaceDetecter) {
                    OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbMat);
                    List<UnityEngine.Rect> result = faceLandmarkDetector.Detect ();

                    foreach (var unityRect in result) {
                        detectResult.Add (new OpenCVForUnity.Rect ((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                    }
                } else {
                    //convert image to greyscale
                    Imgproc.cvtColor (rgbMat, grayMat, Imgproc.COLOR_RGB2GRAY);

                    //convert image to greyscale
                    using (Mat equalizeHistMat = new Mat ())
                    using (MatOfRect faces = new MatOfRect ()) {
                        Imgproc.equalizeHist (grayMat, equalizeHistMat);

                        cascade.detectMultiScale (equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size (equalizeHistMat.cols () * 0.15, equalizeHistMat.cols () * 0.15), new Size ());

                        detectResult = faces.toList ();

                        // Adjust to Dilb's result.
                        foreach (OpenCVForUnity.Rect r in detectResult) {
                            r.y += (int)(r.height * 0.1f);
                        }
                    }
                }

                //face traking
                if (enableTracking) {
                    rectangleTracker.UpdateTrackedObjects (detectResult);
                    detectResult = new List<OpenCVForUnity.Rect> ();
                    rectangleTracker.GetObjects (detectResult, true);
                }

                //face landmark detection
                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbMat);
                List<List<Vector2>> landmarkPoints = new List<List<Vector2>> ();
                foreach (var openCVRect in detectResult) {
                    UnityEngine.Rect rect = new UnityEngine.Rect (openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);

                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);
                    landmarkPoints.Add (points);
                }

                //filter nonfrontalface
                if (isFilteringNonFrontalFaces) {
                    for (int i = 0; i < landmarkPoints.Count; i++) {
                        if (frontalFaceParam.getFrontalFaceRate (landmarkPoints [i]) < frontalFaceRateLowerLimit) {
                            detectResult.RemoveAt (i);
                            landmarkPoints.RemoveAt (i);
                            i--;
                        }
                    }
                }

                //face swapping
                if (landmarkPoints.Count >= 2) {
                    int ann = 0, bob = 1;
                    for (int i = 0; i < landmarkPoints.Count - 1; i += 2) {
                        ann = i;
                        bob = i + 1;

                        faceSwapper.SwapFaces (rgbMat, landmarkPoints [ann], landmarkPoints [bob], 1);
                    }
                }

                //draw face rects
                if (isShowingFaceRects) {
                    for (int i = 0; i < detectResult.Count; i++) {
                        UnityEngine.Rect rect = new UnityEngine.Rect (detectResult [i].x, detectResult [i].y, detectResult [i].width, detectResult [i].height);
                        OpenCVForUnityUtils.DrawFaceRect (rgbMat, rect, new Scalar (255, 0, 0, 255), 2);
                        //Imgproc.putText (rgbMat, " " + frontalFaceParam.getFrontalFaceRate (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                }
                    
                Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.Utils.matToTexture2D (rgbMat, texture);

            }
        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDestroy ()
        {
            capture.release ();

            if (rgbMat != null)
                rgbMat.Dispose ();
            if (grayMat != null)
                grayMat.Dispose ();

            if (rectangleTracker != null)
                rectangleTracker.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (faceSwapper != null)
                faceSwapper.Dispose ();

            if (frontalFaceParam != null)
                frontalFaceParam.Dispose ();
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("FaceSwapperExample");
            #else
            Application.LoadLevel ("FaceSwapperExample");
            #endif
        }

        /// <summary>
        /// Raises the is showing face rects toggle event.
        /// </summary>
        public void OnIsShowingFaceRectsToggle ()
        {
            if (isShowingFaceRectsToggle.isOn) {
                isShowingFaceRects = true;
            } else {
                isShowingFaceRects = false;
            }
        }

        /// <summary>
        /// Raises the use Dlib face detector toggle event.
        /// </summary>
        public void OnUseDlibFaceDetecterToggle ()
        {
            if (useDlibFaceDetecterToggle.isOn) {
                useDlibFaceDetecter = true;
            } else {
                useDlibFaceDetecter = false;
            }
        }

        /// <summary>
        /// Raises the is filtering non frontal faces toggle event.
        /// </summary>
        public void OnIsFilteringNonFrontalFacesToggle ()
        {
            if (isFilteringNonFrontalFacesToggle.isOn) {
                isFilteringNonFrontalFaces = true;
            } else {
                isFilteringNonFrontalFaces = false;
            }
        }

        /// <summary>
        /// Raises the use seamless clone for paste faces toggle event.
        /// </summary>
        public void OnUseSeamlessCloneForPasteFacesToggle ()
        {
            if (useSeamlessCloneForPasteFacesToggle.isOn) {
                useSeamlessCloneForPasteFaces = true;
            } else {
                useSeamlessCloneForPasteFaces = false;
            }
            faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessCloneForPasteFaces;
        }

        /// <summary>
        /// Raises the enable tracking toggle event.
        /// </summary>
        public void OnEnableTrackingToggle ()
        {
            if (enableTrackingToggle.isOn) {
                enableTracking = true;
            } else {
                enableTracking = false;
            }
        }

        /// <summary>
        /// Raises the is showing debug face points toggle event.
        /// </summary>
        public void OnIsShowingDebugFacePointsToggle ()
        {
            if (isShowingDebugFacePointsToggle.isOn) {
                isShowingDebugFacePoints = true;
            } else {
                isShowingDebugFacePoints = false;
            }
            faceSwapper.isShowingDebugFacePoints = isShowingDebugFacePoints;
        }
    }
}