using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceSwap;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.RectangleTrack;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace FaceSwapperExample
{
    /// <summary>
    /// VideoCapture FaceSwapper Example
    /// </summary>
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
    public class VideoCaptureFaceSwapperExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if use dlib face detector.
        /// </summary>
        public bool useDlibFaceDetecter = false;

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
        /// Determines if enables noise filter.
        /// </summary>
        public bool enableNoiseFilter = true;

        /// <summary>
        /// The enable noise filter toggle.
        /// </summary>
        public Toggle enableNoiseFilterToggle;

        /// <summary>
        /// Determines if uses the seamless clone method for the face copy.
        /// </summary>
        public bool useSeamlessClone = false;

        /// <summary>
        /// The use seamless clone toggle.
        /// </summary>
        public Toggle useSeamlessCloneToggle;

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
        /// The video capture to mat helper.
        /// </summary>
        VideoCaptureToMatHelper sourceToMatHelper;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "DlibFaceLandmarkDetector/couple_mjpeg.mjpeg";

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The mean points filter dictionary.
        /// </summary>
        Dictionary<int, LowPassPointsFilter> lowPassFilterDict;

        /// <summary>
        /// The optical flow points filter dictionary.
        /// </summary>
        Dictionary<int, OFPointsFilter> opticalFlowFilterDict;

        /// <summary>
        /// The face Swaper.
        /// </summary>
        DlibFaceSwapper faceSwapper;

        /// <summary>
        /// The detection based tracker.
        /// </summary>
        RectangleTracker rectangleTracker;

        /// <summary>
        /// The frontal face checker.
        /// </summary>
        FrontalFaceChecker frontalFaceChecker;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The sp_human_face_68_dat_filepath.
        /// </summary>
        string sp_human_face_68_dat_filepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            sourceToMatHelper = gameObject.GetComponent<VideoCaptureToMatHelper>();

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
            rectangleTracker = new RectangleTracker();

            faceLandmarkDetector = new FaceLandmarkDetector(sp_human_face_68_dat_filepath);

            lowPassFilterDict = new Dictionary<int, LowPassPointsFilter>();
            opticalFlowFilterDict = new Dictionary<int, OFPointsFilter>();

            faceSwapper = new DlibFaceSwapper();
            faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessClone;
            faceSwapper.isShowingDebugFacePoints = displayDebugFacePoints;

            if (string.IsNullOrEmpty(sourceToMatHelper.requestedVideoFilePath))
                sourceToMatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            sourceToMatHelper.outputColorFormat = VideoCaptureToMatHelper.ColorFormat.RGB;
            sourceToMatHelper.Initialize();

            displayFaceRectsToggle.isOn = displayFaceRects;
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
            enableNoiseFilterToggle.isOn = enableNoiseFilter;
            filterNonFrontalFacesToggle.isOn = filterNonFrontalFaces;
            useSeamlessCloneToggle.isOn = useSeamlessClone;
            displayDebugFacePointsToggle.isOn = displayDebugFacePoints;
        }

        /// <summary>
        /// Raises the video capture to mat helper initialized event.
        /// </summary>
        public void OnVideoCaptureToMatHelperInitialized()
        {
            Debug.Log("OnVideoCaptureToMatHelperInitialized");

            Mat rgbMat = sourceToMatHelper.GetMat();

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);


            gameObject.transform.localScale = new Vector3(rgbMat.cols(), rgbMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbMat.width().ToString());
                fpsMonitor.Add("height", rgbMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = gameObject.transform.localScale.x;
            float height = gameObject.transform.localScale.y;

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

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;



            grayMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
            cascade = new CascadeClassifier(haarcascade_frontalface_alt_xml_filepath);
            //if (cascade.empty())
            //{
            //    Debug.LogError("cascade file is not loaded. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            //}

            frontalFaceChecker = new FrontalFaceChecker(width, height);
        }

        /// <summary>
        /// Raises the video capture to mat helper disposed event.
        /// </summary>
        public void OnVideoCaptureToMatHelperDisposed()
        {
            Debug.Log("OnVideoCaptureToMatHelperDisposed");

            grayMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            rectangleTracker.Reset();

            foreach (var key in lowPassFilterDict.Keys)
            {
                lowPassFilterDict[key].Dispose();
            }
            lowPassFilterDict.Clear();
            foreach (var key in opticalFlowFilterDict.Keys)
            {
                opticalFlowFilterDict[key].Dispose();
            }
            opticalFlowFilterDict.Clear();

            frontalFaceChecker.Dispose();
        }

        /// <summary>
        /// Raises the video capture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnVideoCaptureToMatHelperErrorOccurred(VideoCaptureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnVideoCaptureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {

            if (sourceToMatHelper.IsPlaying() && sourceToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbMat = sourceToMatHelper.GetMat();

                // detect faces.
                List<Rect> detectResult = new List<Rect>();
                if (useDlibFaceDetecter)
                {
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbMat);
                    List<UnityEngine.Rect> result = faceLandmarkDetector.Detect();

                    foreach (var unityRect in result)
                    {
                        detectResult.Add(new Rect((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                    }
                }
                else
                {
                    // convert image to greyscale.
                    Imgproc.cvtColor(rgbMat, grayMat, Imgproc.COLOR_RGB2GRAY);

                    using (Mat equalizeHistMat = new Mat())
                    using (MatOfRect faces = new MatOfRect())
                    {
                        Imgproc.equalizeHist(grayMat, equalizeHistMat);

                        cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size(equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), new Size());

                        detectResult = faces.toList();

                        // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                        foreach (Rect r in detectResult)
                        {
                            r.y += (int)(r.height * 0.1f);
                        }
                    }
                }

                // face tracking.
                List<TrackedRect> trackedRects = new List<TrackedRect>();
                rectangleTracker.UpdateTrackedObjects(detectResult);
                rectangleTracker.GetObjects(trackedRects, true);

                // create noise filter.
                foreach (var openCVRect in trackedRects)
                {
                    if (openCVRect.state == TrackedState.NEW)
                    {
                        if (!lowPassFilterDict.ContainsKey(openCVRect.id))
                            lowPassFilterDict.Add(openCVRect.id, new LowPassPointsFilter((int)faceLandmarkDetector.GetShapePredictorNumParts()));
                        if (!opticalFlowFilterDict.ContainsKey(openCVRect.id))
                            opticalFlowFilterDict.Add(openCVRect.id, new OFPointsFilter((int)faceLandmarkDetector.GetShapePredictorNumParts()));
                    }
                    else if (openCVRect.state == TrackedState.DELETED)
                    {
                        if (lowPassFilterDict.ContainsKey(openCVRect.id))
                        {
                            lowPassFilterDict[openCVRect.id].Dispose();
                            lowPassFilterDict.Remove(openCVRect.id);
                        }
                        if (opticalFlowFilterDict.ContainsKey(openCVRect.id))
                        {
                            opticalFlowFilterDict[openCVRect.id].Dispose();
                            opticalFlowFilterDict.Remove(openCVRect.id);
                        }
                    }
                }

                // detect face landmark points.
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbMat);
                List<List<Vector2>> landmarkPoints = new List<List<Vector2>>();
                foreach (var openCVRect in trackedRects)
                {
                    if (openCVRect.state > TrackedState.NEW_DISPLAYED && openCVRect.state < TrackedState.NEW_HIDED)
                    {

                        UnityEngine.Rect rect = new UnityEngine.Rect(openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                        // apply noise filter.
                        if (enableNoiseFilter)
                        {
                            opticalFlowFilterDict[openCVRect.id].Process(rgbMat, points, points);
                            lowPassFilterDict[openCVRect.id].Process(rgbMat, points, points);
                        }

                        landmarkPoints.Add(points);
                    }
                }

                // filter non frontal faces.
                if (filterNonFrontalFaces)
                {
                    for (int i = 0; i < landmarkPoints.Count; i++)
                    {
                        if (frontalFaceChecker.GetFrontalFaceRate(landmarkPoints[i]) < frontalFaceRateLowerLimit)
                        {
                            trackedRects.RemoveAt(i);
                            landmarkPoints.RemoveAt(i);
                            i--;
                        }
                    }
                }

                // face swapping.
                if (landmarkPoints.Count >= 2)
                {
                    int ann = 0, bob = 1;
                    for (int i = 0; i < landmarkPoints.Count - 1; i += 2)
                    {
                        ann = i;
                        bob = i + 1;

                        faceSwapper.SwapFaces(rgbMat, landmarkPoints[ann], landmarkPoints[bob], 1);
                    }
                }

                // draw face rects.
                if (displayFaceRects)
                {
                    for (int i = 0; i < trackedRects.Count; i++)
                    {
                        Rect openCVRect = trackedRects[i];
                        UnityEngine.Rect rect = new UnityEngine.Rect(openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);
                        OpenCVForUnityUtils.DrawFaceRect(rgbMat, rect, new Scalar(255, 0, 0, 255), 2);
                        //Imgproc.putText (rgbMat, " " + frontalFaceChecker.GetFrontalFaceAngles (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        //Imgproc.putText (rgbMat, " " + frontalFaceChecker.GetFrontalFaceRate (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                }

                //Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbMat, texture);
            }
        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (sourceToMatHelper != null)
                sourceToMatHelper.Dispose();

            if (cascade != null)
                cascade.Dispose();

            if (rectangleTracker != null)
                rectangleTracker.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            foreach (var key in lowPassFilterDict.Keys)
            {
                lowPassFilterDict[key].Dispose();
            }
            lowPassFilterDict.Clear();
            foreach (var key in opticalFlowFilterDict.Keys)
            {
                opticalFlowFilterDict[key].Dispose();
            }
            opticalFlowFilterDict.Clear();

            if (faceSwapper != null)
                faceSwapper.Dispose();

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
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            sourceToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            sourceToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            sourceToMatHelper.Stop();
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
        }

        /// <summary>
        /// Raises the enable noise filter toggle value changed event.
        /// </summary>
        public void OnEnableNoiseFilterToggleValueChanged()
        {
            if (enableNoiseFilterToggle.isOn)
            {
                enableNoiseFilter = true;
                foreach (var key in lowPassFilterDict.Keys)
                {
                    lowPassFilterDict[key].Reset();
                }
                foreach (var key in opticalFlowFilterDict.Keys)
                {
                    opticalFlowFilterDict[key].Reset();
                }
            }
            else
            {
                enableNoiseFilter = false;
            }
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
        }

        /// <summary>
        /// Raises the use seamless clone toggle value changed event.
        /// </summary>
        public void OnUseSeamlessCloneToggleValueChanged()
        {
            if (useSeamlessCloneToggle.isOn)
            {
                useSeamlessClone = true;
            }
            else
            {
                useSeamlessClone = false;
            }
            faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessClone;
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
            if (faceSwapper != null)
                faceSwapper.isShowingDebugFacePoints = displayDebugFacePoints;
        }
    }
}
