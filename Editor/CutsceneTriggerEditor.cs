using UnityEditor;
using UnityEngine;

namespace CutsceneManager.Editor
{
    [CustomEditor(typeof(CutsceneManager.Runtime.CutsceneTrigger))]
    public class CutsceneTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Trigger (Play Mode)"))
                ((CutsceneManager.Runtime.CutsceneTrigger)target).Trigger();
            GUI.enabled = true;
        }
    }
}
