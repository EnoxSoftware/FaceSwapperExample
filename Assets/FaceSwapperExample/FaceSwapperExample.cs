using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace FaceSwapperExample
{
    /// <summary>
    /// Face swapper example.
    /// </summary>
    public class FaceSwapperExample : MonoBehaviour
    {
        public Text exampleTitle;
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        // Use this for initialization
        void Start ()
        {
            exampleTitle.text = "FaceSwapper Example " + Application.version;

            versionInfo.text = OpenCVForUnity.Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnity.Utils.getVersion () + " (" + OpenCVForUnity.Core.VERSION + ")";
            versionInfo.text += " / " + "dlibfacelandmarkdetector" + " " + DlibFaceLandmarkDetector.Utils.getVersion ();
            versionInfo.text += " / UnityEditor " + Application.unityVersion;
            versionInfo.text += " / ";

            #if UNITY_EDITOR
            versionInfo.text += "Editor";
            #elif UNITY_STANDALONE_WIN
            versionInfo.text += "Windows";
            #elif UNITY_STANDALONE_OSX
            versionInfo.text += "Mac OSX";
            #elif UNITY_STANDALONE_LINUX
            versionInfo.text += "Linux";
            #elif UNITY_ANDROID
            versionInfo.text += "Android";
            #elif UNITY_IOS
            versionInfo.text += "iOS";
            #elif UNITY_WSA
            versionInfo.text += "WSA";
            #elif UNITY_WEBGL
            versionInfo.text += "WebGL";
            #endif
            versionInfo.text +=  " ";
            #if ENABLE_MONO
            versionInfo.text +=  "Mono";
            #elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
            #elif ENABLE_DOTNET
            versionInfo.text += ".NET";
            #endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;
        }

        // Update is called once per frame
        void Update ()
        {

        }

        public void OnScrollRectValueChanged ()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }

        public void OnShowLicenseButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnTexture2DFaceSwapperExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DFaceSwapperExample");
            #else
            Application.LoadLevel ("Texture2DFaceSwapperExample");
            #endif
        }

        public void OnWebCamTextureFaceSwapperExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureFaceSwapperExample");
            #else
            Application.LoadLevel ("WebCamTextureFaceSwapperExample");
            #endif
        }

        public void OnVideoCaptureFaceSwapperExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureFaceSwapperExample");
            #else
            Application.LoadLevel ("VideoCaptureFaceSwapperExample");
            #endif
        }

        public void OnTexture2DFaceChangerExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DFaceChangerExample");
            #else
            Application.LoadLevel ("Texture2DFaceChangerExample");
            #endif
        }

        public void OnWebCamTextureFaceChangerExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureFaceChangerExample");
            #else
            Application.LoadLevel ("WebCamTextureFaceChangerExample");
            #endif
        }
    }
}