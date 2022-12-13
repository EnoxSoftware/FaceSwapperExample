using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceChange;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace FaceSwapperExample
{
    /// <summary>
    /// Texture2D FaceChanger Example
    /// </summary>
    public class Texture2DFaceChangerExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if use dlib face detector.
        /// </summary>
        public bool useDlibFaceDetecter = true;

        /// <summary>
        /// The use dlib face detecter toggle.
        /// </summary>
        public Toggle useDlibFaceDetecterToggle;

        /// <summary>
        /// Determines if filters non frontal faces.
        /// </summary>
        public bool filterNonFrontalFaces;

        /// <summary>
        /// The filter non frontal faces toggle.
        /// </summary>
        public Toggle filterNonFrontalFacesToggle;

        /// <summary>
        /// The frontal face rate lower limit.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float frontalFaceRateLowerLimit;

        /// <summary>
        /// Determines if displays face rects.
        /// </summary>
        public bool displayFaceRects = false;

        /// <summary>
        /// The toggle for switching face rects display state.
        /// </summary>
        public Toggle displayFaceRectsToggle;

        /// <summary>
        /// Determines if displays debug face points.
        /// </summary>
        public bool displayDebugFacePoints = false;

        /// <summary>
        /// The toggle for switching debug face points display state.
        /// </summary>
        public Toggle displayDebugFacePointsToggle;

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
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The sp_human_face_68_dat_filepath.
        /// </summary>
        string sp_human_face_68_dat_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.UnityUtils.Utils.getFilePath("DlibFaceLandmarkDetector/haarcascade_frontalface_alt.xml");
            sp_human_face_68_dat_filepath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath("DlibFaceLandmarkDetector/sp_human_face_68.dat");
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = OpenCVForUnity.UnityUtils.Utils.getFilePathAsync("DlibFaceLandmarkDetector/haarcascade_frontalface_alt.xml", (result) =>
            {
                haarcascade_frontalface_alt_xml_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync("DlibFaceLandmarkDetector/sp_human_face_68.dat", (result) =>
            {
                sp_human_face_68_dat_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        private void Run()
        {
            displayFaceRectsToggle.isOn = displayFaceRects;
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
            filterNonFrontalFacesToggle.isOn = filterNonFrontalFaces;
            displayDebugFacePointsToggle.isOn = displayDebugFacePoints;

            if (imgTexture == null)
                imgTexture = Resources.Load("family") as Texture2D;

            gameObject.transform.localScale = new Vector3(imgTexture.width, imgTexture.height, 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = 0;
            float height = 0;

            width = gameObject.transform.localScale.x;
            height = gameObject.transform.localScale.y;


            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            Mat rgbaMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVForUnity.UnityUtils.Utils.texture2DToMat(imgTexture, rgbaMat);
            Debug.Log("rgbaMat ToString " + rgbaMat.ToString());

            if (faceLandmarkDetector == null)
                faceLandmarkDetector = new FaceLandmarkDetector(sp_human_face_68_dat_filepath);

            FrontalFaceChecker frontalFaceChecker = new FrontalFaceChecker(width, height);

            // detect faces.
            List<Rect> detectResult = new List<Rect>();
            if (useDlibFaceDetecter)
            {
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);
                List<UnityEngine.Rect> result = faceLandmarkDetector.Detect();

                foreach (var unityRect in result)
                {
                    detectResult.Add(new Rect((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                }
            }
            else
            {
                if (cascade == null)
                    cascade = new CascadeClassifier(haarcascade_frontalface_alt_xml_filepath);
                //if (cascade.empty ()) {
                //    Debug.LogError ("cascade file is not loaded. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
                //}

                // convert image to greyscale.
                Mat gray = new Mat();
                Imgproc.cvtColor(rgbaMat, gray, Imgproc.COLOR_RGBA2GRAY);

                MatOfRect faces = new MatOfRect();
                Imgproc.equalizeHist(gray, gray);
                cascade.detectMultiScale(gray, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size(gray.cols() * 0.05, gray.cols() * 0.05), new Size());
                //Debug.Log ("faces " + faces.dump ());

                detectResult = faces.toList();

                // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                foreach (Rect r in detectResult)
                {
                    r.y += (int)(r.height * 0.1f);
                }

                gray.Dispose();
            }

            // detect face landmark points.
            OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);
            List<List<Vector2>> landmarkPoints = new List<List<Vector2>>();
            foreach (var openCVRect in detectResult)
            {
                UnityEngine.Rect rect = new UnityEngine.Rect(openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);

                Debug.Log("face : " + rect);
                //OpenCVForUnityUtils.DrawFaceRect(imgMat, rect, new Scalar(255, 0, 0, 255), 2);

                List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);
                //OpenCVForUnityUtils.DrawFaceLandmark(imgMat, points, new Scalar(0, 255, 0, 255), 2);
                landmarkPoints.Add(points);
            }


            // filter non frontal faces.
            if (filterNonFrontalFaces)
            {
                for (int i = 0; i < landmarkPoints.Count; i++)
                {
                    if (frontalFaceChecker.GetFrontalFaceRate(landmarkPoints[i]) < frontalFaceRateLowerLimit)
                    {
                        detectResult.RemoveAt(i);
                        landmarkPoints.RemoveAt(i);
                        i--;
                    }
                }
            }


            // change faces.
            int[] face_nums = new int[landmarkPoints.Count];
            for (int i = 0; i < face_nums.Length; i++)
            {
                face_nums[i] = i;
            }
            face_nums = face_nums.OrderBy(i => System.Guid.NewGuid()).ToArray();
            if (landmarkPoints.Count >= 2)
            {
                DlibFaceChanger faceChanger = new DlibFaceChanger();
                faceChanger.isShowingDebugFacePoints = displayDebugFacePoints;

                faceChanger.SetTargetImage(rgbaMat);

                for (int i = 1; i < face_nums.Length; i++)
                {
                    faceChanger.AddFaceChangeData(rgbaMat, landmarkPoints[face_nums[0]], landmarkPoints[face_nums[i]], 1);
                }

                faceChanger.ChangeFace();
                faceChanger.Dispose();
            }

            // draw face rects.
            if (displayFaceRects && face_nums.Count() > 0)
            {
                int ann = face_nums[0];
                UnityEngine.Rect rect_ann = new UnityEngine.Rect(detectResult[ann].x, detectResult[ann].y, detectResult[ann].width, detectResult[ann].height);
                OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect_ann, new Scalar(255, 255, 0, 255), 2);

                int bob = 0;
                for (int i = 1; i < face_nums.Length; i++)
                {
                    bob = face_nums[i];
                    UnityEngine.Rect rect_bob = new UnityEngine.Rect(detectResult[bob].x, detectResult[bob].y, detectResult[bob].width, detectResult[bob].height);
                    OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect_bob, new Scalar(255, 0, 0, 255), 2);
                }
            }

            frontalFaceChecker.Dispose();

            Texture2D texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.matToTexture2D(rgbaMat, texture);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            rgbaMat.Dispose();
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (cascade != null)
                cascade.Dispose();

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("FaceSwapperExample");
        }

        /// <summary>
        /// Raises the shuffle button click event.
        /// </summary>
        public void OnShuffleButtonClick()
        {
            if (imgTexture != null)
                Run();
        }

        /// <summary>
        /// Raises the use Dlib face detector toggle value changed event.
        /// </summary>
        public void OnUseDlibFaceDetecterToggleValueChanged()
        {
            if (useDlibFaceDetecterToggle.isOn)
            {
                useDlibFaceDetecter = true;
            }
            else
            {
                useDlibFaceDetecter = false;
            }

            if (imgTexture != null)
                Run();
        }

        /// <summary>
        /// Raises the filter non frontal faces toggle value changed event.
        /// </summary>
        public void OnFilterNonFrontalFacesToggleValueChanged()
        {
            if (filterNonFrontalFacesToggle.isOn)
            {
                filterNonFrontalFaces = true;
            }
            else
            {
                filterNonFrontalFaces = false;
            }

            if (imgTexture != null)
                Run();
        }

        /// <summary>
        /// Raises the display face rects toggle value changed event.
        /// </summary>
        public void OnDisplayFaceRectsToggleValueChanged()
        {
            if (displayFaceRectsToggle.isOn)
            {
                displayFaceRects = true;
            }
            else
            {
                displayFaceRects = false;
            }

            if (imgTexture != null)
                Run();
        }

        /// <summary>
        /// Raises the display debug face points toggle value changed event.
        /// </summary>
        public void OnDisplayDebugFacePointsToggleValueChanged()
        {
            if (displayDebugFacePointsToggle.isOn)
            {
                displayDebugFacePoints = true;
            }
            else
            {
                displayDebugFacePoints = false;
            }

            if (imgTexture != null)
                Run();
        }
    }
}