using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DlibFaceLandmarkDetector;
using OpenCVForUnity;
using OpenCVForUnity.FaceSwap;
using UnityEngine;
using UnityEngine.UI;
using WebGLFileUploader;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace FaceSwapperSample
{
    /// <summary>
    /// Texture2D face swapper sample.
    /// </summary>
    public class Texture2DFaceSwapperSample : MonoBehaviour
    {
        /// <summary>
        /// The image texture.
        /// </summary>
        Texture2D imgTexture;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

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
        /// The is showing debug face points.
        /// </summary>
        public bool isShowingDebugFacePoints = false;

        /// <summary>
        /// The is showing debug face points toggle.
        /// </summary>
        public Toggle isShowingDebugFacePointsToggle;

        /// <summary>
        /// The is upload image button.
        /// </summary>
        public Button uploadImageButton;

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
            uploadImageButton.interactable = true;
        }
        #endif

        private void Run ()
        {
            isShowingFaceRectsToggle.isOn = isShowingFaceRects;
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
            isFilteringNonFrontalFacesToggle.isOn = isFilteringNonFrontalFaces;
            useSeamlessCloneForPasteFacesToggle.isOn = useSeamlessCloneForPasteFaces;
            isShowingDebugFacePointsToggle.isOn = isShowingDebugFacePoints;

            if (imgTexture == null)
                imgTexture = Resources.Load ("family") as Texture2D;

            gameObject.transform.localScale = new Vector3 (imgTexture.width, imgTexture.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = 0;
            float height = 0;

            width = gameObject.transform.localScale.x;
            height = gameObject.transform.localScale.y;


            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            Mat rgbaMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVForUnity.Utils.texture2DToMat (imgTexture, rgbaMat);
            Debug.Log ("rgbaMat ToString " + rgbaMat.ToString ());

            if (faceLandmarkDetector == null)
                faceLandmarkDetector = new FaceLandmarkDetector (shape_predictor_68_face_landmarks_dat_filepath);

            FrontalFaceParam frontalFaceParam = new FrontalFaceParam ();

            //face detection
            List<OpenCVForUnity.Rect> detectResult = new List<OpenCVForUnity.Rect> ();
            if (useDlibFaceDetecter) {
                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
                List<UnityEngine.Rect> result = faceLandmarkDetector.Detect ();

                foreach (var unityRect in result) {
                    detectResult.Add (new OpenCVForUnity.Rect ((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                }
            } else {
                if (cascade == null)
                    cascade = new CascadeClassifier (haarcascade_frontalface_alt_xml_filepath);
                if (cascade.empty ()) {
                    Debug.LogError ("cascade file is not loaded.Please copy from “FaceTrackerSample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
                }

                //convert image to greyscale
                Mat gray = new Mat ();
                Imgproc.cvtColor (rgbaMat, gray, Imgproc.COLOR_RGBA2GRAY);

                //detect Faces
                MatOfRect faces = new MatOfRect ();
                Imgproc.equalizeHist (gray, gray);
                cascade.detectMultiScale (gray, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size (gray.cols () * 0.05, gray.cols () * 0.05), new Size ());
                //Debug.Log ("faces " + faces.dump ());

                detectResult = faces.toList ();

                gray.Dispose ();
            }

            //detect face landmark
            OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
            List<List<Vector2>> landmarkPoints = new List<List<Vector2>> ();
            foreach (var openCVRect in detectResult) {
                UnityEngine.Rect rect = new UnityEngine.Rect (openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);

                Debug.Log ("face : " + rect);

                //OpenCVForUnityUtils.DrawFaceRect(imgMat, rect, new Scalar(255, 0, 0, 255), 2);

                List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);
                if (points.Count > 0) {

                    //OpenCVForUnityUtils.DrawFaceLandmark(imgMat, points, new Scalar(0, 255, 0, 255), 2);
                    landmarkPoints.Add (points);
                }
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


            //swap faces
            //Debug.Log("face points count : " + points.Count);
            int[] face_nums = new int[landmarkPoints.Count];
            for (int i = 0; i < face_nums.Length; i++) {
                face_nums [i] = i;
            }
            face_nums = face_nums.OrderBy (i => System.Guid.NewGuid ()).ToArray ();
            if (landmarkPoints.Count >= 2) {
                DlibFaceSwapper faceSwapper = new DlibFaceSwapper ();
                faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessCloneForPasteFaces;
                faceSwapper.isShowingDebugFacePoints = isShowingDebugFacePoints;

                int ann = 0, bob = 0;
                for (int i = 0; i < face_nums.Length - 1; i += 2) {
                    ann = face_nums [i];
                    bob = face_nums [i + 1];

                    faceSwapper.SwapFaces (rgbaMat, landmarkPoints [ann], landmarkPoints [bob], 1);

                }
                faceSwapper.Dispose ();
            }

            //show face rects
            if (isShowingFaceRects) {
                int ann = 0, bob = 0;
                for (int i = 0; i < face_nums.Length - 1; i += 2) {
                    ann = face_nums [i];
                    bob = face_nums [i + 1];

                    UnityEngine.Rect rect_ann = new UnityEngine.Rect (detectResult [ann].x, detectResult [ann].y, detectResult [ann].width, detectResult [ann].height);
                    UnityEngine.Rect rect_bob = new UnityEngine.Rect (detectResult [bob].x, detectResult [bob].y, detectResult [bob].width, detectResult [bob].height);
                    Scalar color = new Scalar (Random.Range (0, 256), Random.Range (0, 256), Random.Range (0, 256), 255);
                    OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect_ann, color, 2);
                    OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect_bob, color, 2);
                    //Imgproc.putText (rgbaMat, "" + i % 2, new Point (rect_ann.xMin, rect_ann.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, color, 2, Imgproc.LINE_AA, false);
                    //Imgproc.putText (rgbaMat, "" + (i % 2 + 1), new Point (rect_bob.xMin, rect_bob.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, color, 2, Imgproc.LINE_AA, false);
                }

            }

            frontalFaceParam.Dispose ();

            Texture2D texture = new Texture2D (rgbaMat.cols (), rgbaMat.rows (), TextureFormat.RGBA32, false);
            OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture);
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            rgbaMat.Dispose ();
        }

        private void fileUploadHandler (UploadedFileInfo[] result)
        {

            if (result.Length == 0) {
                Debug.Log ("File upload Error!");
                return;
            }

            foreach (UploadedFileInfo file in result) {
                if (file.isSuccess) {
                    Debug.Log ("file.filePath: " + file.filePath + " exists:" + File.Exists (file.filePath));

                    imgTexture = new Texture2D (2, 2);
                    byte[] byteArray = File.ReadAllBytes (file.filePath);
                    imgTexture.LoadImage (byteArray);

                    break;
                }
            }
            Run ();
        }

        // Update is called once per frame
        void Update ()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            WebGLFileUploadManager.FileUploadEventHandler -= fileUploadHandler;
            WebGLFileUploadManager.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (cascade != null)
                cascade.Dispose ();
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("FaceSwapperSample");
            #else
            Application.LoadLevel ("FaceSwapperSample");
            #endif
        }

        /// <summary>
        /// Raises the shuffle button event.
        /// </summary>
        public void OnShuffleButton ()
        {
            if (imgTexture != null)
                Run ();
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

            if (imgTexture != null)
                Run ();
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

            if (imgTexture != null)
                Run ();
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

            if (imgTexture != null)
                Run ();
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

            if (imgTexture != null)
                Run ();
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

            if (imgTexture != null)
                Run ();
        }

        /// <summary>
        /// Raises the is upload image button event.
        /// </summary>
        public void OnUploadImageButton ()
        {
            WebGLFileUploadManager.PopupDialog (null, "Select image file (.png|.jpg|.gif)");
        }
    }
}
