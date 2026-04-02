using UnityEditor;
using UnityEngine;
using CutsceneManager.Runtime;

namespace CutsceneManager.Editor
{
    [CustomEditor(typeof(CutsceneManager.Runtime.CutsceneManager))]
    public class CutsceneManagerEditor : UnityEditor.Editor
    {
        private string _playId = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Playback", EditorStyles.boldLabel);

            var mgr = (CutsceneManager.Runtime.CutsceneManager)target;
            var ids = mgr.GetSequenceIds();
            if (ids.Count == 0)
            {
                EditorGUILayout.HelpBox("No sequences loaded. Place cutscene JSON files in Resources/Cutscenes/.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"{ids.Count} sequence(s) loaded.", MessageType.None);
                foreach (var id in ids)
                    EditorGUILayout.LabelField("  •", id);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play by ID (Play Mode only)", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _playId = EditorGUILayout.TextField(_playId);
            GUI.enabled = Application.isPlaying && !string.IsNullOrEmpty(_playId);
            if (GUILayout.Button("Play", GUILayout.Width(60)))
                mgr.PlaySequence(_playId);
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
                mgr.StopSequence();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("Reload Sequences (Play Mode)"))
            {
                if (Application.isPlaying) mgr.LoadAllSequences();
                else Debug.Log("[CutsceneManager] Reload available in Play Mode only.");
            }
        }
    }
}
