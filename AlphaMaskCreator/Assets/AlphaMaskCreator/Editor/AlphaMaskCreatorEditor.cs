using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace AlphaMaskCreator
{
    [ExecuteInEditMode()]
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AlphaMaskCreator))]
    public class AlphaMaskCreatorEditor : BaseEditor<AlphaMaskCreator>
    {
        private AlphaMaskCreatorEditor editor;
        private AlphaMaskCreator editorTarget;

        private SerializedProperty assetPath;
        private SerializedProperty imageResolution;
        private SerializedProperty channel;

        void OnEnable()
        {
            this.editor = this;
            this.editorTarget = (AlphaMaskCreator)target;

            assetPath = editor.FindProperty(x => x.texturePath);
            imageResolution = editor.FindProperty(x => x.imageResolution);
            channel = editor.FindProperty(x => x.channel);
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Settings", GUIStyles.GroupTitleStyle);
                {
                    EditorGUILayout.HelpBox("Move and adjust the camera in order to set the depth accordingly. Adjust settings and save alpha mask or brush texture", MessageType.Info);

                    EditorGUILayout.PropertyField(assetPath, new GUIContent("Asset Path", "The path where to store the textures."));
                    EditorGUILayout.PropertyField(imageResolution, new GUIContent("Image Resolution", "The resolution of the saved image."));
                    EditorGUILayout.PropertyField(channel, new GUIContent("Channel", "The color channel to use."));

                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Camera Quick Settings", GUIStyles.GroupTitleStyle);
                {
                    GUILayout.BeginHorizontal();
                    {

                        if (GUILayout.Button("1m"))
                        {
                            AdjustCamera(1f);
                        }
                        else if (GUILayout.Button("10m"))
                        {
                            AdjustCamera(10f);
                        }
                        else if (GUILayout.Button("50m"))
                        {
                            AdjustCamera(50f);
                        }
                        else if (GUILayout.Button("100m"))
                        {
                            AdjustCamera(100f);
                        }
                        else if (GUILayout.Button("250m"))
                        {
                            AdjustCamera(250f);
                        }
                        else if (GUILayout.Button("500m"))
                        {
                            AdjustCamera(500f);
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Texture", GUIStyles.GroupTitleStyle);
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Create Alpha Mask"))
                        {
                            CreateAlphaMask();
                        }

                        if (GUILayout.Button("Create Brush Texture"))
                        {
                            CreateBrushTexture();
                        }

                    }
                    GUILayout.EndHorizontal();

                }
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            // nothing to do currently
        }

        private void CreateAlphaMask()
        {
            // save the texture image file
            editorTarget.SaveTexture();

            // refresh the asset database in order to show the new texture in the project
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        }

        private void CreateBrushTexture()
        {
            // save the texture image file
            string filePath = editorTarget.SaveTexture();

            // refresh the asset database in order to show the new texture in the project
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // create texture file path; needs to be relative to "Assets"
            string fileName = Path.GetFileName(filePath);
            string texturePath = Path.Combine( "Assets", editorTarget.texturePath, fileName);


            // Texture2D texture2D = (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));

            TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(texturePath);

            textureImporter.textureType = TextureImporterType.SingleChannel;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.isReadable = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.npotScale = TextureImporterNPOTScale.None;

            /* TODO: Using SetTextureSettings causes errors. Currently no way to set the singleChannelComponent to Red, 
             * but should work anyway
             * 
            TextureImporterSettings settings = new TextureImporterSettings();
            settings.textureType = TextureImporterType.SingleChannel;
            settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.readable = true;
            settings.mipmapEnabled = false;
            settings.npotScale = TextureImporterNPOTScale.None;

            importer.SetTextureSettings(settings);  
            */

            TextureImporterPlatformSettings ps = new TextureImporterPlatformSettings();
            ps.format = TextureImporterFormat.R16;
            ps.maxTextureSize = (int) editorTarget.imageResolution;

            textureImporter.SetPlatformTextureSettings(ps);

            EditorUtility.SetDirty(textureImporter);
            textureImporter.SaveAndReimport();

        }

        /// <summary>
        /// Shortcut to adjust the camera size quickly
        /// </summary>
        /// <param name="size"></param>
        private void AdjustCamera( float size)
        {
            Camera camera = editorTarget.GetComponent<Camera>();

            camera.orthographicSize = size;
            camera.farClipPlane = size;

            float x = camera.transform.position.x;
            float y = size;
            float z = camera.transform.position.z;
            camera.transform.position = new Vector3( x, y, z);

        }

    }
}