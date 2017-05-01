using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DlibFaceLandmarkDetector;
using OpenCVForUnity;
using OpenCVForUnity.FaceChange;
using OpenCVForUnity.RectangleTrack;
using WebGLFileUploader;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace FaceSwapperExample
{
    /// <summary>
    /// WebCamTexture face changer example.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureFaceChangerExample : MonoBehaviour
    {

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

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
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;
        /// <summary>
        /// The face Changer
        /// </summary>
        DlibFaceChanger faceChanger;

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
        public bool useDlibFaceDetecter = false;

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
        /// The is upload face mask button.
        /// </summary>
        public Button uploadFaceMaskButton;

        /// <summary>
        /// The face mask texture.
        /// </summary>
        Texture2D faceMaskTexture;

        /// <summary>
        /// The face mask mat.
        /// </summary>
        Mat faceMaskMat;

        /// <summary>
        /// The detected face rect.
        /// </summary>
        UnityEngine.Rect detectedFaceRect;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        private string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The shape_predictor_68_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_face_landmarks_dat_filepath;


        // Use this for initialization
        void Start ()
        {
            WebGLFileUploadManager.SetImageEncodeSetting (true);
            WebGLFileUploadManager.SetAllowedFileName ("\\.(png|jpe?g|gif)$");
            WebGLFileUploadManager.SetImageShrinkingSize (640, 480);
            WebGLFileUploadManager.FileUploadEventHandler += fileUploadHandler;

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(getFilePathCoroutine());
            #else
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.Utils.getFilePath ("haarcascade_frontalface_alt.xml");
            shape_predictor_68_face_landmarks_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator getFilePathCoroutine ()
        {
            var getFilePathAsync_0_Coroutine = StartCoroutine (OpenCVForUnity.Utils.getFilePathAsync ("haarcascade_frontalface_alt.xml", (result) => {
                haarcascade_frontalface_alt_xml_filepath = result;
            }));
            var getFilePathAsync_1_Coroutine = StartCoroutine (DlibFaceLandmarkDetector.Utils.getFilePathAsync ("shape_predictor_68_face_landmarks.dat", (result) => {
                shape_predictor_68_face_landmarks_dat_filepath = result;
            }));

            yield return getFilePathAsync_0_Coroutine;
            yield return getFilePathAsync_1_Coroutine;

            Run ();
            uploadFaceMaskButton.interactable = true;
        }
        #endif

        private void Run ()
        {
            rectangleTracker = new RectangleTracker ();

            faceLandmarkDetector = new FaceLandmarkDetector (shape_predictor_68_face_landmarks_dat_filepath);

            faceChanger = new DlibFaceChanger ();
            faceChanger.isShowingDebugFacePoints = isShowingDebugFacePoints;

            webCamTextureToMatHelper.Init ();

            isShowingFaceRectsToggle.isOn = isShowingFaceRects;
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
            isFilteringNonFrontalFacesToggle.isOn = isFilteringNonFrontalFaces;
            enableTrackingToggle.isOn = enableTracking;
            isShowingDebugFacePointsToggle.isOn = isShowingDebugFacePoints;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper inited event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInited ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInited");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            colors = new Color32[webCamTextureMat.cols () * webCamTextureMat.rows ()];
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);


            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = gameObject.transform.localScale.x;
            float height = gameObject.transform.localScale.y;

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            cascade = new CascadeClassifier (haarcascade_frontalface_alt_xml_filepath);
//            if (cascade.empty ()) {
//                Debug.LogError ("cascade file is not loaded.Please copy from “FaceTrackerExample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
//            }

            frontalFaceParam = new FrontalFaceParam ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            grayMat.Dispose ();

            rectangleTracker.Reset ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode){
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {

            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                //face detection
                List<OpenCVForUnity.Rect> detectResult = new List<OpenCVForUnity.Rect> ();
                if (useDlibFaceDetecter) {
                    OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
                    List<UnityEngine.Rect> result = faceLandmarkDetector.Detect ();

                    foreach (var unityRect in result) {
                        detectResult.Add (new OpenCVForUnity.Rect ((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                    }
                } else {
                    //convert image to greyscale
                    Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

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
                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
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

                //face changeing
                if (faceMaskTexture != null && landmarkPoints.Count >= 1) {

                    OpenCVForUnity.Utils.texture2DToMat (faceMaskTexture, faceMaskMat);

                    if (detectedFaceRect.width == 0.0f || detectedFaceRect.height == 0.0f) {
                        if (useDlibFaceDetecter) {
                            OpenCVForUnityUtils.SetImage (faceLandmarkDetector, faceMaskMat);
                            List<UnityEngine.Rect> result = faceLandmarkDetector.Detect ();
                            if (result.Count >= 1)
                                detectedFaceRect = result [0];
                        } else {
                        
                            using (Mat grayMat = new Mat ())
                            using (Mat equalizeHistMat = new Mat ())
                            using (MatOfRect faces = new MatOfRect ()) {
                                //convert image to greyscale
                                Imgproc.cvtColor (faceMaskMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                                Imgproc.equalizeHist (grayMat, equalizeHistMat);

                                cascade.detectMultiScale (equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size (equalizeHistMat.cols () * 0.15, equalizeHistMat.cols () * 0.15), new Size ());

                                //detectResult = faces.toList ();
                                List<OpenCVForUnity.Rect> faceList = faces.toList ();
                                if (faceList.Count >= 1) {
                                    detectedFaceRect = new UnityEngine.Rect (faceList [0].x, faceList [0].y, faceList [0].width, faceList [0].height);
                                    // Adjust to Dilb's result.
                                    detectedFaceRect.y += detectedFaceRect.height * 0.1f;
                                }
                            }
                        }
                    }

                    if (detectedFaceRect.width > 0 || detectedFaceRect.height > 0) {
                        OpenCVForUnityUtils.SetImage (faceLandmarkDetector, faceMaskMat);
                        List<Vector2> souseLandmarkPoint = faceLandmarkDetector.DetectLandmark (detectedFaceRect);

                        faceChanger.SetTargetImage (rgbaMat);
                        for (int i = 0; i < landmarkPoints.Count; i++) {
                            faceChanger.AddFaceChangeData (faceMaskMat, souseLandmarkPoint, landmarkPoints [i], 1);
                        }
                        faceChanger.ChangeFace ();

                        if (isShowingFaceRects) {
                            OpenCVForUnityUtils.DrawFaceRect (faceMaskMat, detectedFaceRect, new Scalar (255, 0, 0, 255), 2);
                        }
                    }
                } else if (landmarkPoints.Count >= 2) {
                    faceChanger.SetTargetImage (rgbaMat);
                    for (int i = 1; i < landmarkPoints.Count; i ++) {
                        faceChanger.AddFaceChangeData (rgbaMat, landmarkPoints [0], landmarkPoints [i], 1);
                    }
                    faceChanger.ChangeFace ();
                }

                //draw face rects
                if (isShowingFaceRects) {
                    for (int i = 0; i < detectResult.Count; i++) {
                        UnityEngine.Rect rect = new UnityEngine.Rect (detectResult [i].x, detectResult [i].y, detectResult [i].width, detectResult [i].height);
                        OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 0, 0, 255), 2);
                        //Imgproc.putText (rgbaMat, " " + frontalFaceParam.getAngleOfFrontalFace (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        //Imgproc.putText (rgbaMat, " " + frontalFaceParam.getFrontalFaceRate (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                }

                //display face mask image.
                if (faceMaskMat != null) {
                    float scale = (rgbaMat.width () / 4f) / faceMaskMat.width ();
                    float tx = rgbaMat.width () - faceMaskMat.width () * scale;
                    float ty = 0.0f;
                    Mat trans = new Mat (2, 3, CvType.CV_32F);//1.0, 0.0, tx, 0.0, 1.0, ty);
                    trans.put (0, 0, scale);
                    trans.put (0, 1, 0.0f);
                    trans.put (0, 2, tx);
                    trans.put (1, 0, 0.0f);
                    trans.put (1, 1, scale);
                    trans.put (1, 2, ty);

                    Imgproc.warpAffine (faceMaskMat, rgbaMat, trans, rgbaMat.size (), Imgproc.INTER_LINEAR, Core.BORDER_TRANSPARENT, new Scalar (0));
                }

                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, colors);
            }
        }


        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            webCamTextureToMatHelper.Dispose ();

            if (cascade != null)
                cascade.Dispose ();

            if (rectangleTracker != null)
                rectangleTracker.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (faceChanger != null)
                faceChanger.Dispose ();

            if (frontalFaceParam != null)
                frontalFaceParam.Dispose ();

            if (faceMaskMat != null)
                faceMaskMat.Dispose ();
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
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton ()
        {
            webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
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
            if (faceChanger != null)
                faceChanger.isShowingDebugFacePoints = isShowingDebugFacePoints;
        }

        /// <summary>
        /// Raises the is set face mask button event.
        /// </summary>
        public void OnSetFaceMaskButton ()
        {
            if (faceMaskMat != null) {
                faceMaskMat.Dispose ();
            }

            faceMaskTexture = Resources.Load ("face_mask") as Texture2D;
            faceMaskMat = new Mat (faceMaskTexture.height, faceMaskTexture.width, CvType.CV_8UC4);
            OpenCVForUnity.Utils.texture2DToMat (faceMaskTexture, faceMaskMat);
            Debug.Log ("faceMaskMat ToString " + faceMaskMat.ToString ());
            detectedFaceRect = new UnityEngine.Rect ();
        }

        /// <summary>
        /// Raises the is upload face mask button event.
        /// </summary>
        public void OnUploadFaceMaskButton ()
        {
            WebGLFileUploadManager.PopupDialog (null, "Select frontal face image file (.png|.jpg|.gif)");
        }

        
        /// <summary>
        /// Files the upload handler.
        /// </summary>
        /// <param name="result">Result.</param>
        private void fileUploadHandler (UploadedFileInfo[] result)
        {
            
            if (result.Length == 0) {
                Debug.Log ("File upload Error!");
                return;
            }
            
            foreach (UploadedFileInfo file in result) {
                if (file.isSuccess) {
                    Debug.Log ("file.filePath: " + file.filePath + " exists:" + File.Exists (file.filePath));
                    
                    faceMaskTexture = new Texture2D (2, 2);
                    byte[] byteArray = File.ReadAllBytes (file.filePath);
                    faceMaskTexture.LoadImage (byteArray);
                    
                    break;
                }
            }
            
            if (faceMaskTexture != null) {
                if (faceMaskMat != null)
                    faceMaskMat.Dispose ();
                
                faceMaskMat = new Mat (faceMaskTexture.height, faceMaskTexture.width, CvType.CV_8UC4);
                OpenCVForUnity.Utils.texture2DToMat (faceMaskTexture, faceMaskMat);
                Debug.Log ("faceMaskMat ToString " + faceMaskMat.ToString ());
                detectedFaceRect = new UnityEngine.Rect ();
            }
        }

        /// <summary>
        /// Raises the is reset face mask button event.
        /// </summary>
        public void OnResetFaceMaskButton ()
        {
            if (faceMaskTexture != null) {
                faceMaskTexture = null;
                faceMaskMat.Dispose ();
                faceMaskMat = null;
            }
        }
    }
}