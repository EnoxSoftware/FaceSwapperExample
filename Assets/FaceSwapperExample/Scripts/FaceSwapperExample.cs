using UnityEngine;
using System.Collections;

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

        public void OnTexture2DFaceSwapperExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DFaceSwapperExample");
            #else
            Application.LoadLevel ("Texture2DFaceSwapperExample");
            #endif
        }

        public void OnWebCamTextureFaceSwapperExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureFaceSwapperExample");
            #else
            Application.LoadLevel ("WebCamTextureFaceSwapperExample");
            #endif
        }

        public void OnVideoCaptureFaceSwapperExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureFaceSwapperExample");
            #else
            Application.LoadLevel ("VideoCaptureFaceSwapperExample");
            #endif
        }

        public void OnTexture2DFaceChangerExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DFaceChangerExample");
            #else
            Application.LoadLevel ("Texture2DFaceChangerExample");
            #endif
        }

        public void OnWebCamTextureFaceChangerExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureFaceChangerExample");
            #else
            Application.LoadLevel ("WebCamTextureFaceChangerExample");
            #endif
        }
    }
}