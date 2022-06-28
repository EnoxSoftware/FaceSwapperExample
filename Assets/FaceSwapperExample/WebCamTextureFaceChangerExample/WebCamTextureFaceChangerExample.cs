using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceChange;
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
    /// WebCamTexture FaceChanger Example
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureFaceChangerExample : MonoBehaviour
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
        public bool filterNonFrontalFaces = false;

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
        /// The upload face mask button.
        /// </summary>
        public Button uploadFaceMaskButton;

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
        /// The mean points filter dictionary.
        /// </summary>
        Dictionary<int, LowPassPointsFilter> lowPassFilterDict;

        /// <summary>
        /// The optical flow points filter dictionary.
        /// </summary>
        Dictionary<int, OFPointsFilter> opticalFlowFilterDict;

        /// <summary>
        /// The face Changer
        /// </summary>
        DlibFaceChanger faceChanger;

        /// <summary>
        /// The detection based tracker.
        /// </summary>
        RectangleTracker rectangleTracker;

        /// <summary>
        /// The frontal face checker.
        /// </summary>
        FrontalFaceChecker frontalFaceChecker;

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

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.UnityUtils.Utils.getFilePath("haarcascade_frontalface_alt.xml");
            sp_human_face_68_dat_filepath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath("sp_human_face_68.dat");
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = OpenCVForUnity.UnityUtils.Utils.getFilePathAsync("haarcascade_frontalface_alt.xml", (result) =>
            {
                haarcascade_frontalface_alt_xml_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync("sp_human_face_68.dat", (result) =>
            {
                sp_human_face_68_dat_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            getFilePath_Coroutine = null;

            Run();
            uploadFaceMaskButton.interactable = true;
        }
#endif

        private void Run()
        {
            rectangleTracker = new RectangleTracker();

            faceLandmarkDetector = new FaceLandmarkDetector(sp_human_face_68_dat_filepath);

            lowPassFilterDict = new Dictionary<int, LowPassPointsFilter>();
            opticalFlowFilterDict = new Dictionary<int, OFPointsFilter>();

            faceChanger = new DlibFaceChanger();
            faceChanger.isShowingDebugFacePoints = displayDebugFacePoints;

            webCamTextureToMatHelper.Initialize();

            displayFaceRectsToggle.isOn = displayFaceRects;
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
            enableNoiseFilterToggle.isOn = enableNoiseFilter;
            filterNonFrontalFacesToggle.isOn = filterNonFrontalFaces;
            displayDebugFacePointsToggle.isOn = displayDebugFacePoints;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);


            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
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

            grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
            cascade = new CascadeClassifier(haarcascade_frontalface_alt_xml_filepath);
            //if (cascade.empty())
            //{
            //    Debug.LogError("cascade file is not loaded.Please copy from “FaceTrackerExample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            //}

            frontalFaceChecker = new FrontalFaceChecker(width, height);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

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
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                // detect faces.
                List<Rect> detectResult;
                DetectFaces(rgbaMat, out detectResult, useDlibFaceDetecter);

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
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);
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
                            opticalFlowFilterDict[openCVRect.id].Process(rgbaMat, points, points);
                            lowPassFilterDict[openCVRect.id].Process(rgbaMat, points, points);
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

                // face changing.
                if (faceMaskTexture != null && landmarkPoints.Count >= 1)
                { // Apply face changing between detected faces and a mask image.

                    if (detectedFaceRect.width == 0.0f || detectedFaceRect.height == 0.0f)
                    {
                        DetectFaces(faceMaskMat, out detectResult, useDlibFaceDetecter);
                        if (detectResult.Count >= 1)
                        {
                            detectedFaceRect = new UnityEngine.Rect(detectResult[0].x, detectResult[0].y, detectResult[0].width, detectResult[0].height);
                        }
                    }

                    if (detectedFaceRect.width > 0 || detectedFaceRect.height > 0)
                    {
                        OpenCVForUnityUtils.SetImage(faceLandmarkDetector, faceMaskMat);
                        List<Vector2> souseLandmarkPoint = faceLandmarkDetector.DetectLandmark(detectedFaceRect);

                        faceChanger.SetTargetImage(rgbaMat);
                        for (int i = 0; i < landmarkPoints.Count; i++)
                        {
                            faceChanger.AddFaceChangeData(faceMaskMat, souseLandmarkPoint, landmarkPoints[i], 1);
                        }
                        faceChanger.ChangeFace();

                        if (displayFaceRects)
                        {
                            OpenCVForUnityUtils.DrawFaceRect(faceMaskMat, detectedFaceRect, new Scalar(255, 0, 0, 255), 2);
                        }
                    }
                }
                else if (landmarkPoints.Count >= 2)
                { // Apply face changing between detected faces.
                    faceChanger.SetTargetImage(rgbaMat);
                    for (int i = 1; i < landmarkPoints.Count; i++)
                    {
                        faceChanger.AddFaceChangeData(rgbaMat, landmarkPoints[0], landmarkPoints[i], 1);
                    }
                    faceChanger.ChangeFace();
                }

                // draw face rects.
                if (displayFaceRects)
                {
                    for (int i = 0; i < trackedRects.Count; i++)
                    {
                        Rect openCVRect = trackedRects[i];
                        UnityEngine.Rect rect = new UnityEngine.Rect(openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);
                        OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(255, 0, 0, 255), 2);
                        //Imgproc.putText (rgbaMat, " " + frontalFaceChecker.GetFrontalFaceAngles (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        //Imgproc.putText (rgbaMat, " " + frontalFaceChecker.GetFrontalFaceRate (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                }

                // display face mask image.
                if (faceMaskMat != null)
                {
                    float scale = (rgbaMat.width() / 4f) / faceMaskMat.width();
                    float tx = rgbaMat.width() - faceMaskMat.width() * scale;
                    float ty = 0.0f;
                    Mat trans = new Mat(2, 3, CvType.CV_32F);//1.0, 0.0, tx, 0.0, 1.0, ty);
                    trans.put(0, 0, scale);
                    trans.put(0, 1, 0.0f);
                    trans.put(0, 2, tx);
                    trans.put(1, 0, 0.0f);
                    trans.put(1, 1, scale);
                    trans.put(1, 2, ty);

                    Imgproc.warpAffine(faceMaskMat, rgbaMat, trans, rgbaMat.size(), Imgproc.INTER_LINEAR, Core.BORDER_TRANSPARENT, new Scalar(0));

                    if (displayFaceRects || displayDebugFacePoints)
                        OpenCVForUnity.UnityUtils.Utils.texture2DToMat(faceMaskTexture, faceMaskMat);
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);
            }
        }

        private void DetectFaces(Mat rgbaMat, out List<Rect> detectResult, bool useDlibFaceDetecter)
        {
            detectResult = new List<Rect>();

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
                // convert image to greyscale.
                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

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
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

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

            if (faceChanger != null)
                faceChanger.Dispose();

            if (faceMaskMat != null)
                faceMaskMat.Dispose();

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
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
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
            if (faceChanger != null)
                faceChanger.isShowingDebugFacePoints = displayDebugFacePoints;
        }

        /// <summary>
        /// Raises the set face mask button click event.
        /// </summary>
        public void OnSetFaceMaskButtonClick()
        {
            if (faceMaskMat != null)
            {
                faceMaskMat.Dispose();
            }

            faceMaskTexture = Resources.Load("face_mask") as Texture2D;
            faceMaskMat = new Mat(faceMaskTexture.height, faceMaskTexture.width, CvType.CV_8UC4);
            OpenCVForUnity.UnityUtils.Utils.texture2DToMat(faceMaskTexture, faceMaskMat);
            Debug.Log("faceMaskMat ToString " + faceMaskMat.ToString());
            detectedFaceRect = new UnityEngine.Rect();
        }

        /// <summary>
        /// Raises the reset face mask button click event.
        /// </summary>
        public void OnResetFaceMaskButtonClick()
        {
            if (faceMaskTexture != null)
            {
                faceMaskTexture = null;
                faceMaskMat.Dispose();
                faceMaskMat = null;
            }
        }
    }
}