#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_6000
using UnityEditor.Build;
#endif

namespace Ragendom
{
    [InitializeOnLoad]
    public static class DefineManager
    {
        public enum DefineType
        {
            Static,
            Project,
            Auto,
            ThirdParty
        }

        public struct DefineState
        {
            public string Define;
            public bool ShouldBeEnabled;

            public DefineState(string define, bool shouldBeEnabled)
            {
                Define = define;
                ShouldBeEnabled = shouldBeEnabled;
            }
        }

        private struct RegisteredDefine
        {
            public string Define;
            public string AssemblyType;

            public RegisteredDefine(string define, string assemblyType)
            {
                Define = define;
                AssemblyType = assemblyType;
            }
        }

        static DefineManager()
        {
            EditorApplication.delayCall += CheckAutoDefines;
        }

        public static bool HasDefine(string define)
        {
            string definesLine = GetCurrentDefines();
            string[] defines = definesLine.Split(';');
            return Array.FindIndex(defines, x => x == define) != -1;
        }

        public static void EnableDefine(string define)
        {
            if (HasDefine(define))
                return;

            string definesLine = GetCurrentDefines();

            if (string.IsNullOrEmpty(definesLine))
                definesLine = define;
            else
                definesLine += ";" + define;

            SetDefines(definesLine);

            Debug.Log($"[DefineManager]: Enabled define: {define}");
        }

        public static void DisableDefine(string define)
        {
            if (!HasDefine(define))
                return;

            string definesLine = GetCurrentDefines();
            string[] defines = definesLine.Split(';');
            List<string> newDefines = new List<string>();

            for (int i = 0; i < defines.Length; i++)
            {
                if (defines[i] != define)
                    newDefines.Add(defines[i]);
            }

            SetDefines(string.Join(";", newDefines));

            Debug.Log($"[DefineManager]: Disabled define: {define}");
        }

        public static void CheckAutoDefines()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CheckAutoDefines;
                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Collect all [Define] attributes with AssemblyType specified
            List<RegisteredDefine> registeredDefines = CollectRegisteredDefines(assemblies);

            if (registeredDefines.Count == 0)
                return;

            // For each registered define, check if the target type exists
            List<DefineState> states = new List<DefineState>();
            foreach (var rd in registeredDefines)
            {
                bool found = false;
                foreach (Assembly asm in assemblies)
                {
                    try
                    {
                        if (asm.GetType(rd.AssemblyType, false) != null)
                        {
                            found = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Ignore assembly load errors
                    }
                }
                states.Add(new DefineState(rd.Define, found));
            }

            // Apply changes
            ChangeAutoDefinesState(states);
        }

        private static List<RegisteredDefine> CollectRegisteredDefines(Assembly[] assemblies)
        {
            List<RegisteredDefine> registeredDefines = new List<RegisteredDefine>();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        DefineAttribute[] attributes = (DefineAttribute[])type.GetCustomAttributes(typeof(DefineAttribute), false);

                        if (attributes != null)
                        {
                            foreach (DefineAttribute attribute in attributes)
                            {
                                if (!string.IsNullOrEmpty(attribute.AssemblyType))
                                {
                                    registeredDefines.Add(new RegisteredDefine(attribute.Define, attribute.AssemblyType));
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore assemblies that can't be reflected
                }
            }

            return registeredDefines;
        }

        private static void ChangeAutoDefinesState(List<DefineState> states)
        {
            bool hasChanges = false;

            foreach (var state in states)
            {
                bool currentlyEnabled = HasDefine(state.Define);

                if (state.ShouldBeEnabled && !currentlyEnabled)
                {
                    EnableDefine(state.Define);
                    hasChanges = true;
                }
                else if (!state.ShouldBeEnabled && currentlyEnabled)
                {
                    DisableDefine(state.Define);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                Debug.Log("[DefineManager]: Auto defines updated.");
            }
        }

        private static string GetCurrentDefines()
        {
#if UNITY_6000
            return PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(
                    BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif
        }

        private static void SetDefines(string defines)
        {
#if UNITY_6000
            PlayerSettings.SetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(
                    BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)),
                defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                defines);
#endif
        }

        public static string[] GetAllDefines()
        {
            string definesLine = GetCurrentDefines();
            if (string.IsNullOrEmpty(definesLine))
                return new string[0];

            return definesLine.Split(';');
        }
    }
}
#endif
