#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Ragendom
{
    public class DefineManagerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string[] currentDefines;
        private Dictionary<string, bool> defineToggles = new Dictionary<string, bool>();

        [MenuItem("Window/Ragendom Core/Define Manager")]
        public static void ShowWindow()
        {
            DefineManagerWindow window = GetWindow<DefineManagerWindow>("Define Manager");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshDefines();
        }

        private void RefreshDefines()
        {
            currentDefines = DefineManager.GetAllDefines();
            defineToggles.Clear();

            for (int i = 0; i < currentDefines.Length; i++)
            {
                if (!string.IsNullOrEmpty(currentDefines[i]))
                    defineToggles[currentDefines[i]] = true;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scripting Define Symbols", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (defineToggles.Count == 0)
            {
                EditorGUILayout.HelpBox("No scripting define symbols found.", MessageType.Info);
            }
            else
            {
                List<string> keys = new List<string>(defineToggles.Keys);
                foreach (string define in keys)
                {
                    defineToggles[define] = EditorGUILayout.ToggleLeft(define, defineToggles[define]);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Defines", GUILayout.Height(30)))
            {
                ApplyDefines();
            }

            if (GUILayout.Button("Check Auto Defines", GUILayout.Height(30)))
            {
                DefineManager.CheckAutoDefines();
                RefreshDefines();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Refresh", GUILayout.Height(25)))
            {
                RefreshDefines();
            }

            EditorGUILayout.Space(5);
        }

        private void ApplyDefines()
        {
            List<string> enabledDefines = new List<string>();

            foreach (var kvp in defineToggles)
            {
                if (kvp.Value)
                    enabledDefines.Add(kvp.Key);
            }

            string definesString = string.Join(";", enabledDefines);

#if UNITY_6000
            PlayerSettings.SetScriptingDefineSymbols(
                UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(
                    BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)),
                definesString);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                definesString);
#endif

            Debug.Log("[DefineManager]: Defines applied successfully.");
        }
    }
}
#endif
