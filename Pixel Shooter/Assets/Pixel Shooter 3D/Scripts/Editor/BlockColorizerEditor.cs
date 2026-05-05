using UnityEngine;
using UnityEditor;

namespace PixelShooter3D
{
    [CustomEditor(typeof(BlockColorizer))]
    public class BlockColorizerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            BlockColorizer colorizer = (BlockColorizer)target;

            // Draw default inspector first
            DrawDefaultInspector();

            // Check if source image is assigned and readable
            if (colorizer.sourceImage != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(colorizer.sourceImage);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null && !importer.isReadable)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("The selected Source Image is not readable. Please enable 'Read/Write' in its Import Settings.", MessageType.Warning);

                    if (GUILayout.Button("Fix Now"))
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }
}
