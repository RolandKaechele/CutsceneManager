using UnityEngine;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// <b>CutsceneTrigger</b> starts a cutscene sequence when a condition is met.
    /// <para>
    /// Supported trigger modes:
    /// <list type="bullet">
    /// <item><b>OnStart</b> — plays on <c>Start()</c>.</item>
    /// <item><b>OnTriggerEnter</b> — plays when a collider with the configured tag enters.</item>
    /// <item><b>OnInteract</b> — plays when <see cref="Trigger"/> is called from code or UI.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Optionally set <see cref="playOnce"/> to prevent the sequence from replaying after the first time.
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Cutscene Trigger")]
    public class CutsceneTrigger : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Sequence")]
        [Tooltip("ID of the CutsceneSequenceData to play.")]
        public string sequenceId;

        [Header("Trigger Mode")]
        public TriggerMode triggerMode = TriggerMode.OnInteract;

        [Tooltip("Only trigger once per scene load.")]
        public bool playOnce = true;

        [Header("OnTriggerEnter settings")]
        [Tooltip("Tag of the collider that activates this trigger (e.g. 'Player').")]
        public string activatorTag = "Player";

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private bool _played;
        private CutsceneManager _manager;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Start()
        {
            _manager = FindObjectOfType<CutsceneManager>();

            if (triggerMode == TriggerMode.OnStart)
                TryPlay();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerMode == TriggerMode.OnTriggerEnter && other.CompareTag(activatorTag))
                TryPlay();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerMode == TriggerMode.OnTriggerEnter && other.CompareTag(activatorTag))
                TryPlay();
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Play the sequence immediately (for OnInteract mode or programmatic calls).</summary>
        public void Trigger()
        {
            TryPlay();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void TryPlay()
        {
            if (playOnce && _played) return;
            if (_manager == null)
            {
                Debug.LogWarning("[CutsceneTrigger] No CutsceneManager found in scene.");
                return;
            }
            if (string.IsNullOrEmpty(sequenceId))
            {
                Debug.LogWarning("[CutsceneTrigger] sequenceId is not set.");
                return;
            }
            _played = true;
            _manager.PlaySequence(sequenceId);
        }

        // -------------------------------------------------------------------------
        // Enum
        // -------------------------------------------------------------------------

        public enum TriggerMode
        {
            OnStart,
            OnTriggerEnter,
            OnInteract
        }
    }
}
