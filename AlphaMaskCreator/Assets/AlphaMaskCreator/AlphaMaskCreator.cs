using System;
using UnityEngine;
using System.IO;

namespace AlphaMaskCreator
{
    /// <summary>
    /// Create an orthographic camera, place it on top of gameobjects (also terrains) and create a depth image
    /// which can be used as alpha mask or as texture for terrain stamps.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [System.Serializable]
    public class AlphaMaskCreator : MonoBehaviour
    {
        public enum ImageResolution
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192
            
        }

        public enum Channel
        {
            Red,
            Green,
            Blue,
            All

        }

        /// <summary>
        /// The resolution of the saved mask image
        /// </summary>
        public ImageResolution imageResolution = ImageResolution._2048;

        /// <summary>
        /// The color channel to use
        /// </summary>
        public Channel channel = Channel.Red;

        /// <summary>
        /// Relative path to the Assets path
        /// </summary>
        public string texturePath = "AlphaMaskCreatorData";

        /// <summary>
        /// The size being used for the orthographic camera and the far clip plane
        /// </summary>
        private int defaultOrthographicSize = 5;

        private Shader _renderDepthShader;
        private Shader RenderDepthShader
        {
            get { return _renderDepthShader != null ? _renderDepthShader : (_renderDepthShader = Shader.Find("AlphaMaskCreator/RenderDepth")); }
        }

        private Material _renderDepthMaterial;
        private Material RenderDepthMaterial
        {
            get
            {
                if (_renderDepthMaterial == null)
                {
                    _renderDepthMaterial = new Material(RenderDepthShader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                return _renderDepthMaterial;
            }
        }

        private void Awake()
        {
            SetupTexturePath();
        }

        private void Start()
        {
            if (RenderDepthShader == null || !RenderDepthShader.isSupported)
            {
                enabled = false;
                Debug.LogError("Shader " + RenderDepthShader.name + " is not supported");
                return;
            }


            SetupCamera();

            ResetViewportRect();

        }

        private void SetupCamera()
        {

            // turn on depth rendering for the camera so that the shader can access it via _CameraDepthTexture
            Camera camera = GetComponent<Camera>();

            camera.depthTextureMode = DepthTextureMode.Depth;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = defaultOrthographicSize;
            camera.transform.position = new Vector3(0, defaultOrthographicSize, 0);
            camera.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = defaultOrthographicSize;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.aspect = 1f;

        }

        /// <summary>
        /// Set viewport to quadratic
        /// </summary>
        public void ResetViewportRect()
        {
            Camera camera = GetComponent<Camera>();

            // 1:1 aspect ratio
            Vector2 targetAspect = new Vector2(1, 1);

            // Determine ratios of screen/window & target, respectively.
            float screenRatio = Screen.width / (float)Screen.height;
            
            float targetRatio = targetAspect.x / targetAspect.y;

            if (Mathf.Approximately(screenRatio, targetRatio))
            {
                // Screen or window is the target aspect ratio: use the whole area.
                camera.rect = new Rect(0, 0, 1, 1);
            }
            else if (screenRatio > targetRatio)
            {
                // Screen or window is wider than the target: pillarbox.
                float normalizedWidth = targetRatio / screenRatio;
                float barThickness = (1f - normalizedWidth) / 2f;
                camera.rect = new Rect(barThickness, 0, normalizedWidth, 1);
            }
            else
            {
                // Screen or window is narrower than the target: letterbox.
                float normalizedHeight = screenRatio / targetRatio;
                float barThickness = (1f - normalizedHeight) / 2f;
                camera.rect = new Rect(0, barThickness, 1, normalizedHeight);
            }
        }

        private void OnDisable()
        {
            if (_renderDepthMaterial != null)
            {
                DestroyImmediate(_renderDepthMaterial);
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (RenderDepthShader != null)
            {
                Graphics.Blit(src, dest, RenderDepthMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        /// <summary>
        /// Path for the alpha masks: AlphaMasks in parallel to the Assets folder
        /// </summary>
        /// <returns></returns>
        public string GetPath()
        {
            string path = Application.dataPath;

            path = Path.Combine("Assets", path, texturePath);

            return path;
        }

        /// <summary>
        /// Filename is a string formatted as "MyScene - 2018.12.09 - 08.12.28.08.png"
        /// </summary>
        /// <returns></returns>
        private static string GetFilename()
        {
            string objectName = "Texture"; //  SceneManager.GetActiveScene().name;

            return string.Format("{0} - {1:yyyy.MM.dd - HH.mm.ss.ff}.png", objectName, DateTime.Now);
        }

        /// <summary>
        /// Set the screenshot path variable and ensure the path exists.
        /// </summary>
        private string SetupTexturePath()
        {
            string texturePath = GetPath();

            if (!Directory.Exists( texturePath))
            {
                Directory.CreateDirectory( texturePath);
            }

            return texturePath;
        }

        /// <summary>
        /// Save the mask image to a file
        /// </summary>
        /// <returns></returns>
        public string SaveTexture()
        {
            // ensure the path exists
            string texturePath = SetupTexturePath();

            Camera maskSaveCamera = GetComponent<Camera>();

            string filepath = Path.Combine( texturePath, GetFilename());

            int resolution = (int)imageResolution;

            int rw = resolution;
            int rh = resolution;
            RenderTexture rt = new RenderTexture(rw, rh, 24);

            maskSaveCamera.targetTexture = rt;

            Texture2D maskTexture = new Texture2D(rw, rh, TextureFormat.R16, false);

            float aspect = maskSaveCamera.aspect;
            maskSaveCamera.aspect = rw / rh;
            maskSaveCamera.Render();

            RenderTexture.active = rt;
            maskTexture.ReadPixels(new Rect(0, 0, rw, rh), 0, 0);
            maskSaveCamera.targetTexture = null;

            RenderTexture.active = null;
            maskSaveCamera.aspect = aspect;
            DestroyImmediate(rt);

            // invalidate color channels depending on setting
            // TODO OPTIMIZE using &
            if (channel != Channel.All)
            {
                for (int i = 0; i < maskTexture.width; i++)
                {
                    for (int j = 0; j < maskTexture.height; j++)
                    {
                        Color pixel = maskTexture.GetPixel(i, j);
                        
                        switch (channel)
                        {
                            case Channel.Red:
                                // pixel.r = 0f;
                                pixel.g = 0f;
                                pixel.b = 0f;
                                break;
                            case Channel.Green:
                                pixel.r = 0f;
                                // pixel.g = 0f;
                                pixel.b = 0f;
                                break;
                            case Channel.Blue:
                                pixel.r = 0f;
                                pixel.g = 0f;
                                // pixel.b = 0f;
                                break;
                            case Channel.All:
                                // nothing to do, default is all channels
                                break;
                        }


                        maskTexture.SetPixel(i, j, pixel);
                    }
                }
                maskTexture.Apply();
            }

            // save file
            byte[] bytes = maskTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filepath, bytes);

            Debug.Log(string.Format("[<color=blue>Alpha Mask Creator</color>] File saved:\n<color=grey>{0}</color>", filepath));

            return filepath;

        }

    }
}