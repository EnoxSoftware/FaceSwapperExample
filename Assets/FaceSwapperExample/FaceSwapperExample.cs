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