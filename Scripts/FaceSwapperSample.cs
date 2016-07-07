using UnityEngine;
using System.Collections;

#if UNITY_5_3
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
						#if UNITY_5_3
			            SceneManager.LoadScene ("ShowLicense");
						#else
						Application.LoadLevel ("ShowLicense");
                        #endif
				}

                public void OnTexture2DFaceSwapperSample ()
				{
						#if UNITY_5_3
			            SceneManager.LoadScene ("Texture2DFaceSwapperSample");
                        #else
                        Application.LoadLevel("Texture2DFaceSwapperSample");
						#endif
				}

                public void OnWebCamTextureFaceSwapperSample()
				{
						#if UNITY_5_3
			            SceneManager.LoadScene ("WebCamTextureFaceSwapperSample");
                        #else
                        Application.LoadLevel("WebCamTextureFaceSwapperSample");
						#endif
				}

                public void OnVideoCaptureFaceSwapperSample()
                {
                        #if UNITY_5_3
			            SceneManager.LoadScene ("VideoCaptureFaceSwapperSample");
                        #else
                        Application.LoadLevel("VideoCaptureFaceSwapperSample");
                        #endif
                }
		}
}