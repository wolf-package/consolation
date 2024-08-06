using Consolation;
using UnityEditor;
using UnityEngine;

namespace ConsolationEditor
{
    [CustomEditor(typeof(ConsoleInGame))]
    class ConsoleInGameEditor : Editor
    {
        SerializedProperty toggleKey;
        SerializedProperty openOnStart;
        SerializedProperty shakeToOpen;
        SerializedProperty shakeRequiresTouch;
        SerializedProperty shakeAcceleration;
        SerializedProperty toggleThresholdSeconds;
        SerializedProperty restrictLogCount;
        SerializedProperty maxLogCount;
        SerializedProperty collapseLogOnStart;
        SerializedProperty logFontSize;
        SerializedProperty scaleFactor;
        private SerializedProperty isCustomSizeWindowConsole;
        private SerializedProperty customWidth;
        private SerializedProperty customHeight;
        SerializedProperty skin;

        void OnEnable()
        {
            toggleKey = serializedObject.FindProperty("toggleKey");
            openOnStart = serializedObject.FindProperty("openOnStart");
            shakeToOpen = serializedObject.FindProperty("shakeToOpen");
            shakeRequiresTouch = serializedObject.FindProperty("shakeRequiresTouch");
            shakeAcceleration = serializedObject.FindProperty("shakeAcceleration");
            toggleThresholdSeconds = serializedObject.FindProperty("toggleThresholdSeconds");
            restrictLogCount = serializedObject.FindProperty("restrictLogCount");
            maxLogCount = serializedObject.FindProperty("maxLogCount");
            collapseLogOnStart = serializedObject.FindProperty("collapseLogOnStart");
            logFontSize = serializedObject.FindProperty("logFontSize");
            scaleFactor = serializedObject.FindProperty("scaleFactor");
            isCustomSizeWindowConsole = serializedObject.FindProperty("isCustomSizeWindowConsole");
            customWidth = serializedObject.FindProperty("customWidth");
            customHeight = serializedObject.FindProperty("customHeight");
            skin = serializedObject.FindProperty("skin");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(toggleKey);
            EditorGUILayout.PropertyField(openOnStart);
            EditorGUILayout.PropertyField(shakeToOpen);

            using (new EditorGUI.DisabledScope(!shakeToOpen.boolValue))
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(shakeRequiresTouch);
                EditorGUILayout.PropertyField(shakeAcceleration);
            }

            EditorGUILayout.PropertyField(toggleThresholdSeconds);
            EditorGUILayout.PropertyField(restrictLogCount);

            using (new EditorGUI.DisabledScope(!restrictLogCount.boolValue))
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(maxLogCount);
            }

            EditorGUILayout.PropertyField(collapseLogOnStart);

            EditorGUILayout.Space();
            GUILayout.Label("Style", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(logFontSize);
            EditorGUILayout.PropertyField(scaleFactor);
            EditorGUILayout.PropertyField(isCustomSizeWindowConsole);
            if (isCustomSizeWindowConsole.boolValue)
            {
                EditorGUILayout.PropertyField(customWidth);
                EditorGUILayout.PropertyField(customHeight);
            }

            EditorGUILayout.PropertyField(skin);

            serializedObject.ApplyModifiedProperties();
        }
    }
}