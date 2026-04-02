#if CUTSCENEMANAGER_SM
using UnityEngine;
using SaveManager.Runtime;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// Optional bridge between CutsceneManager and SaveManager.
    /// Enable define <c>CUTSCENEMANAGER_SM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// <list type="bullet">
    /// <item>Records a <c>"cutscene_seen_{sequenceId}"</c> flag in SaveManager when a sequence
    /// completes or is skipped, preventing unwanted replays.</item>
    /// <item>Exposes <see cref="HasSeen"/> so other systems can gate replay without
    /// a direct SaveManager reference.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Save Cutscene Bridge")]
    [DisallowMultipleComponent]
    public class SaveCutsceneBridge : MonoBehaviour
    {
        [Tooltip("Prefix used for seen flags. Full flag: '{prefix}{sequenceId}'.")]
        [SerializeField] private string flagPrefix = "cutscene_seen_";

        private CutsceneManager _cutscene;
        private SaveManager.Runtime.SaveManager _save;

        private void Awake()
        {
            _cutscene = GetComponent<CutsceneManager>() ?? FindFirstObjectByType<CutsceneManager>();
            _save     = GetComponent<SaveManager.Runtime.SaveManager>()
                        ?? FindFirstObjectByType<SaveManager.Runtime.SaveManager>();

            if (_cutscene == null)
                Debug.LogWarning("[SaveCutsceneBridge] CutsceneManager not found.");
            if (_save == null)
                Debug.LogWarning("[SaveCutsceneBridge] SaveManager not found.");
        }

        private void OnEnable()
        {
            if (_cutscene != null)
            {
                _cutscene.OnSequenceCompleted += MarkSeen;
                _cutscene.OnSequenceSkipped   += MarkSeen;
            }
        }

        private void OnDisable()
        {
            if (_cutscene != null)
            {
                _cutscene.OnSequenceCompleted -= MarkSeen;
                _cutscene.OnSequenceSkipped   -= MarkSeen;
            }
        }

        private void MarkSeen(string sequenceId)
        {
            if (_save == null || string.IsNullOrEmpty(sequenceId)) return;
            _save.SetFlag(flagPrefix + sequenceId);
        }

        /// <summary>Returns true if this sequence has been seen (completed or skipped).</summary>
        public bool HasSeen(string sequenceId)
        {
            if (_save == null || string.IsNullOrEmpty(sequenceId)) return false;
            return _save.IsSet(flagPrefix + sequenceId);
        }
    }
}
#else
// CUTSCENEMANAGER_SM not defined — bridge is inactive.
namespace CutsceneManager.Runtime
{
    /// <summary>No-op stub. Enable CUTSCENEMANAGER_SM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("CutsceneManager/Save Cutscene Bridge")]
    public class SaveCutsceneBridge : UnityEngine.MonoBehaviour
    {
        public bool HasSeen(string sequenceId) => false;

        private void Awake()
        {
            UnityEngine.Debug.Log("[SaveCutsceneBridge] SaveManager integration is disabled. " +
                                  "Add the scripting define CUTSCENEMANAGER_SM to enable it.");
        }
    }
}
#endif
