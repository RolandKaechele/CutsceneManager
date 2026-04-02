#if CUTSCENEMANAGER_MGM
using UnityEngine;
using MiniGameManager.Runtime;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// Optional bridge between CutsceneManager and MiniGameManager.
    /// Enable define <c>CUTSCENEMANAGER_MGM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Triggers configured cutscene sequences in response to mini-game lifecycle events.
    /// Useful for showing intro/outro cinematics around mini-games.
    /// </para>
    /// <para><b>Sequence id conventions (all configurable in the Inspector):</b></para>
    /// <list type="bullet">
    /// <item><c>"{miniGameId}_start"</c> — played when a mini-game starts</item>
    /// <item><c>"{miniGameId}_complete"</c> — played when a mini-game completes</item>
    /// <item><c>"{miniGameId}_abort"</c> — played when a mini-game is aborted</item>
    /// </list>
    /// <para>If no sequence with the derived id exists, nothing happens silently.</para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Mini Game Cutscene Bridge")]
    [DisallowMultipleComponent]
    public class MiniGameCutsceneBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Suffix appended to the mini-game id to derive the start sequence id.")]
        [SerializeField] private string startSuffix    = "_start";

        [Tooltip("Suffix appended to the mini-game id to derive the complete sequence id.")]
        [SerializeField] private string completeSuffix = "_complete";

        [Tooltip("Suffix appended to the mini-game id to derive the abort sequence id.")]
        [SerializeField] private string abortSuffix    = "_abort";

        // ─── References ──────────────────────────────────────────────────────────
        private CutsceneManager _cutscene;
        private MiniGameManager.Runtime.MiniGameManager _mgr;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cutscene = GetComponent<CutsceneManager>() ?? FindFirstObjectByType<CutsceneManager>();
            _mgr      = GetComponent<MiniGameManager.Runtime.MiniGameManager>()
                        ?? FindFirstObjectByType<MiniGameManager.Runtime.MiniGameManager>();

            if (_cutscene == null) Debug.LogWarning("[MiniGameCutsceneBridge] CutsceneManager not found.");
            if (_mgr      == null) Debug.LogWarning("[MiniGameCutsceneBridge] MiniGameManager not found.");
        }

        private void OnEnable()
        {
            if (_mgr != null)
            {
                _mgr.OnMiniGameStarted   += OnStarted;
                _mgr.OnMiniGameCompleted += OnCompleted;
                _mgr.OnMiniGameAborted   += OnAborted;
            }
        }

        private void OnDisable()
        {
            if (_mgr != null)
            {
                _mgr.OnMiniGameStarted   -= OnStarted;
                _mgr.OnMiniGameCompleted -= OnCompleted;
                _mgr.OnMiniGameAborted   -= OnAborted;
            }
        }

        // ─── Handlers ────────────────────────────────────────────────────────────
        private void OnStarted(string id) =>
            TryPlay(id + startSuffix);

        private void OnCompleted(MiniGameResult result) =>
            TryPlay(result.miniGameId + completeSuffix);

        private void OnAborted(string id) =>
            TryPlay(id + abortSuffix);

        private void TryPlay(string sequenceId)
        {
            if (_cutscene == null || string.IsNullOrEmpty(sequenceId)) return;
            if (_cutscene.GetSequence(sequenceId) != null)
                _cutscene.PlaySequence(sequenceId);
        }
    }
}
#else
namespace CutsceneManager.Runtime
{
    /// <summary>No-op stub. Enable CUTSCENEMANAGER_MGM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("CutsceneManager/Mini Game Cutscene Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MiniGameCutsceneBridge : UnityEngine.MonoBehaviour { }
}
#endif
