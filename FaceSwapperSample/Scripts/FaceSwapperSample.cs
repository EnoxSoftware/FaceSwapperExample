using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace FaceSwapperSample
{
    /// <summary>
    /// Face swapper sample.
    /// </summary>
    public class FaceSwapperSample : MonoBehaviour
    {

        // Use this for initialization
        void Start ()
        {

        }

        // Update is called once per frame
        void Update ()
        {

        }

        public void OnShowLicenseButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnTexture2DFaceSwapperSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DFaceSwapperSample");
            #else
            Application.LoadLevel ("Texture2DFaceSwapperSample");
            #endif
        }

        public void OnWebCamTextureFaceSwapperSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureFaceSwapperSample");
            #else
            Application.LoadLevel ("WebCamTextureFaceSwapperSample");
            #endif
        }

        public void OnVideoCaptureFaceSwapperSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureFaceSwapperSample");
            #else
            Application.LoadLevel ("VideoCaptureFaceSwapperSample");
            #endif
        }

        public void OnTexture2DFaceChangerSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DFaceChangerSample");
            #else
            Application.LoadLevel ("Texture2DFaceChangerSample");
            #endif
        }

        public void OnWebCamTextureFaceChangerSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureFaceChangerSample");
            #else
            Application.LoadLevel ("WebCamTextureFaceChangerSample");
            #endif
        }
    }
}