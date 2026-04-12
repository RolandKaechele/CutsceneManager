using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// <b>CutsceneManager</b> is the central orchestrator for cutscene playback.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Load <see cref="CutsceneSequenceData"/> from <c>Resources/Cutscenes/</c> and an optional external folder.</item>
    /// <item>Play, pause, and skip sequences step-by-step.</item>
    /// <item>Drive <see cref="FadeController"/>, <see cref="NameCardController"/>, <see cref="SubtitleController"/>
    /// and fullscreen image display automatically.</item>
    /// <item>Broadcast custom events and Lua triggers for game-specific logic.</item>
    /// <item>Integrate with MapLoaderFramework via <see cref="MapLoaderBridge"/> (optional).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to a persistent manager GameObject. Assign the sub-controllers in the Inspector.
    /// Place cutscene JSON files in <c>Assets/Resources/Cutscenes/</c>.
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Cutscene Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class CutsceneManager : SerializedMonoBehaviour
#else
    public class CutsceneManager : MonoBehaviour
#endif
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Sub-controllers (auto-resolved if not assigned)")]
        [SerializeField] private FadeController fadeController;
        [SerializeField] private NameCardController nameCardController;
        [SerializeField] private SubtitleController subtitleController;

        [Header("Full-screen image (optional)")]
        [Tooltip("UnityEngine.UI.Image used to show full-screen cutscene art.")]
        [SerializeField] private UnityEngine.UI.Image fullscreenImage;

        [Header("Skip input")]
        [Tooltip("KeyCode that skips the current sequence (when skipAllowed is true).")]
        [SerializeField] private KeyCode skipKey = KeyCode.Escape;
        [Tooltip("KeyCode that advances a step waiting for input.")]
        [SerializeField] private KeyCode confirmKey = KeyCode.Space;

        [Header("Loaded sequences (read-only, set at runtime)")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField] private List<string> loadedSequenceIds = new List<string>();

        // -------------------------------------------------------------------------
        // Audio delegate hooks (set by CutsceneAudioBridge when AudioManager is present)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Optional callback that handles <see cref="CutsceneStepType.PlayAudio"/> steps.
        /// Signature: (resourcePath, loop). When set, the built-in AudioSource fallback is bypassed.
        /// Set automatically by <c>AudioManager.Runtime.CutsceneAudioBridge</c>.
        /// </summary>
        public Action<string, bool> PlayAudioCallback;

        /// <summary>
        /// Optional callback that handles <see cref="CutsceneStepType.StopAudio"/> steps.
        /// When set, the built-in AudioSource stop is bypassed.
        /// Set automatically by <c>AudioManager.Runtime.CutsceneAudioBridge</c>.
        /// </summary>
        public Action StopAudioCallback;

        // -------------------------------------------------------------------------
        // Video delegate hooks (set by CutsceneVideoBridge when VideoManager is present)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Optional callback that handles <see cref="CutsceneStepType.PlayVideo"/> steps.
        /// Signature: (resourceOrId). When set, the built-in single-step wait fallback is bypassed.
        /// Set automatically by <c>VideoManager.Runtime.CutsceneVideoBridge</c>.
        /// </summary>
        public Action<string> PlayVideoCallback;

        /// <summary>
        /// Optional callback that handles <see cref="CutsceneStepType.StopVideo"/> steps.
        /// When set, the built-in no-op fallback is bypassed.
        /// Set automatically by <c>VideoManager.Runtime.CutsceneVideoBridge</c>.
        /// </summary>
        public Action StopVideoCallback;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when a sequence starts. Parameter: sequence id.</summary>
        public event Action<string> OnSequenceStarted;
        /// <summary>Fired when a sequence finishes naturally. Parameter: sequence id.</summary>
        public event Action<string> OnSequenceCompleted;
        /// <summary>Fired when a sequence is skipped by the player. Parameter: sequence id.</summary>
        public event Action<string> OnSequenceSkipped;
        /// <summary>
        /// Fired when a <see cref="CutsceneStepType.Custom"/> step executes.
        /// Parameters: (sequence id, custom event name).
        /// </summary>
        public event Action<string, string> OnCustomEvent;

        // -------------------------------------------------------------------------
        // Internal state
        // -------------------------------------------------------------------------

        private Dictionary<string, CutsceneSequenceData> _sequences = new();
        private Coroutine _activeCoroutine;
        private bool _skipRequested;
        private bool _confirmRequested;

        /// <summary>True while a sequence is playing.</summary>
        public bool IsPlaying => _activeCoroutine != null;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            // Auto-resolve sub-controllers in the scene
            if (fadeController == null) fadeController = FindObjectOfType<FadeController>();
            if (nameCardController == null) nameCardController = FindObjectOfType<NameCardController>();
            if (subtitleController == null) subtitleController = FindObjectOfType<SubtitleController>();

            LoadAllSequences();
        }

        private void Update()
        {
            if (Input.GetKeyDown(skipKey)) _skipRequested = true;
            if (Input.GetKeyDown(confirmKey)) _confirmRequested = true;
        }

        // -------------------------------------------------------------------------
        // Loading
        // -------------------------------------------------------------------------

        /// <summary>
        /// Loads all cutscene JSON files from <c>Resources/Cutscenes/</c> and the external Cutscenes folder.
        /// Call again at runtime to reload after mod changes.
        /// </summary>
        public void LoadAllSequences()
        {
            _sequences.Clear();
            loadedSequenceIds.Clear();

            // Load from Resources/Cutscenes
            var resourceSequences = Resources.LoadAll<TextAsset>("Cutscenes");
            foreach (var asset in resourceSequences)
            {
                RegisterSequenceFromJson(asset.text);
            }

            // Load from external folder (mods / DLC)
            string externalDir = GetExternalCutscenesDirectory();
            if (Directory.Exists(externalDir))
            {
                foreach (var file in Directory.GetFiles(externalDir, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        RegisterSequenceFromJson(File.ReadAllText(file));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CutsceneManager] Failed to load sequence from {file}: {ex.Message}");
                    }
                }
            }

            Debug.Log($"[CutsceneManager] Loaded {_sequences.Count} cutscene sequence(s).");
        }

        private void RegisterSequenceFromJson(string json)
        {
            try
            {
                var seq = JsonUtility.FromJson<CutsceneSequenceData>(json);
                if (seq == null || string.IsNullOrEmpty(seq.id)) return;
                seq.rawJson = json;
                _sequences[seq.id] = seq;
                if (!loadedSequenceIds.Contains(seq.id))
                    loadedSequenceIds.Add(seq.id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CutsceneManager] Failed to parse sequence JSON: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // Playback
        // -------------------------------------------------------------------------

        /// <summary>
        /// Play the cutscene sequence with the given <paramref name="id"/>.
        /// If a sequence is already playing it is stopped first.
        /// </summary>
        public void PlaySequence(string id)
        {
            if (!_sequences.TryGetValue(id, out var seq))
            {
                Debug.LogWarning($"[CutsceneManager] Sequence '{id}' not found.");
                return;
            }
            PlaySequence(seq);
        }

        /// <summary>Play a <see cref="CutsceneSequenceData"/> directly (e.g. built at runtime).</summary>
        public void PlaySequence(CutsceneSequenceData sequence)
        {
            if (sequence == null) return;
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _skipRequested = false;
            _confirmRequested = false;
            _activeCoroutine = StartCoroutine(PlaySequenceCoroutine(sequence));
        }

        /// <summary>Stop the currently playing sequence immediately.</summary>
        public void StopSequence()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            CleanupUI();
        }

        // -------------------------------------------------------------------------
        // Playback coroutine
        // -------------------------------------------------------------------------

        private IEnumerator PlaySequenceCoroutine(CutsceneSequenceData sequence)
        {
            OnSequenceStarted?.Invoke(sequence.id);

            if (sequence.steps != null)
            {
                foreach (var step in sequence.steps)
                {
                    // Skip-check before each step
                    if (sequence.skipAllowed && _skipRequested)
                    {
                        _skipRequested = false;
                        break;
                    }

                    yield return ExecuteStep(step, sequence.id);
                }
            }

            CleanupUI();

            if (sequence.skipAllowed && _skipRequested)
            {
                _skipRequested = false;
                OnSequenceSkipped?.Invoke(sequence.id);
            }
            else
            {
                OnSequenceCompleted?.Invoke(sequence.id);
            }

            _activeCoroutine = null;
        }

        private IEnumerator ExecuteStep(CutsceneStep step, string sequenceId)
        {
            switch (step.stepType)
            {
                case CutsceneStepType.Fade:
                    yield return HandleFadeStep(step);
                    break;

                case CutsceneStepType.Wait:
                    yield return HandleWaitStep(step);
                    break;

                case CutsceneStepType.ShowImage:
                    yield return HandleShowImageStep(step);
                    break;

                case CutsceneStepType.HideImage:
                    HideFullscreenImage();
                    break;

                case CutsceneStepType.ShowText:
                    yield return HandleShowTextStep(step);
                    break;

                case CutsceneStepType.HideText:
                    if (subtitleController != null) subtitleController.Hide();
                    break;

                case CutsceneStepType.ShowNameCard:
                    yield return HandleNameCardStep(step);
                    break;

                case CutsceneStepType.PlayAudio:
                    HandlePlayAudio(step);
                    break;

                case CutsceneStepType.StopAudio:
                    HandleStopAudio();
                    break;

                case CutsceneStepType.TriggerMapLoad:
                    HandleTriggerMapLoad(step);
                    break;

                case CutsceneStepType.TriggerChapter:
                    HandleTriggerChapter(step);
                    break;

                case CutsceneStepType.TriggerLua:
                    HandleTriggerLua(step);
                    break;

                case CutsceneStepType.CameraShake:
                    yield return HandleCameraShake(step);
                    break;

                case CutsceneStepType.Custom:
                    OnCustomEvent?.Invoke(sequenceId, step.customEvent);
                    break;

                case CutsceneStepType.PlayVideo:
                    yield return HandlePlayVideo(step);
                    break;

                case CutsceneStepType.StopVideo:
                    HandleStopVideo(step);
                    break;
            }
        }

        // -------------------------------------------------------------------------
        // Step handlers
        // -------------------------------------------------------------------------

        private IEnumerator HandleFadeStep(CutsceneStep step)
        {
            if (fadeController == null) yield break;
            bool isToOpaque = step.fadeColor != "clear";
            bool done = false;
            if (isToOpaque)
                fadeController.FadeOut(step.duration, () => done = true);
            else
                fadeController.FadeIn(step.duration, () => done = true);
            yield return new WaitUntil(() => done);
        }

        private IEnumerator HandleWaitStep(CutsceneStep step)
        {
            if (step.waitForInput)
            {
                _confirmRequested = false;
                yield return new WaitUntil(() => _confirmRequested);
                _confirmRequested = false;
            }
            else if (step.duration > 0f)
            {
                yield return new WaitForSeconds(step.duration);
            }
        }

        private IEnumerator HandleShowImageStep(CutsceneStep step)
        {
            if (!string.IsNullOrEmpty(step.imageResource))
            {
                var sprite = Resources.Load<Sprite>(step.imageResource);
                if (fullscreenImage != null && sprite != null)
                {
                    fullscreenImage.sprite = sprite;
                    fullscreenImage.gameObject.SetActive(true);
                }
                else if (sprite == null)
                {
                    Debug.LogWarning($"[CutsceneManager] Image not found at Resources/{step.imageResource}");
                }
            }
            if (step.duration > 0f && !step.waitForInput)
                yield return new WaitForSeconds(step.duration);
            else if (step.waitForInput)
            {
                _confirmRequested = false;
                yield return new WaitUntil(() => _confirmRequested);
                _confirmRequested = false;
            }
        }

        private IEnumerator HandleShowTextStep(CutsceneStep step)
        {
            string resolved = string.IsNullOrEmpty(step.text) ? step.localizationKey : step.text;
            if (subtitleController != null) subtitleController.Show(resolved);
            if (step.waitForInput)
            {
                _confirmRequested = false;
                yield return new WaitUntil(() => _confirmRequested);
                _confirmRequested = false;
            }
            else if (step.duration > 0f)
            {
                yield return new WaitForSeconds(step.duration);
            }
        }

        private IEnumerator HandleNameCardStep(CutsceneStep step)
        {
            string resolved = string.IsNullOrEmpty(step.text) ? step.localizationKey : step.text;
            if (nameCardController != null) nameCardController.Show(resolved);
            if (step.duration > 0f) yield return new WaitForSeconds(step.duration);
            if (nameCardController != null) nameCardController.Hide();
        }

        private AudioSource _audioSource;

        private void HandlePlayAudio(CutsceneStep step)
        {
            if (string.IsNullOrEmpty(step.audioResource)) return;

            // Delegate to AudioManager bridge if available
            if (PlayAudioCallback != null)
            {
                PlayAudioCallback(step.audioResource, step.audioLoop);
                return;
            }

            // Built-in fallback: play via a local AudioSource
            var clip = Resources.Load<AudioClip>(step.audioResource);
            if (clip == null)
            {
                Debug.LogWarning($"[CutsceneManager] AudioClip not found at Resources/{step.audioResource}");
                return;
            }
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = clip;
            _audioSource.loop = step.audioLoop;
            _audioSource.Play();
        }

        private void HandleStopAudio()
        {
            // Delegate to AudioManager bridge if available
            if (StopAudioCallback != null) { StopAudioCallback(); return; }

            // Built-in fallback
            if (_audioSource != null) _audioSource.Stop();
        }

        private void HandleTriggerMapLoad(CutsceneStep step)
        {
#if CUTSCENEMANAGER_MLF
            var bridge = GetComponent<MapLoaderBridge>() ?? FindObjectOfType<MapLoaderBridge>();
            if (bridge != null && !string.IsNullOrEmpty(step.mapId))
                bridge.LoadMap(step.mapId);
            else
                Debug.LogWarning("[CutsceneManager] TriggerMapLoad: no MapLoaderBridge found or mapId is empty.");
#else
            Debug.LogWarning("[CutsceneManager] TriggerMapLoad: define CUTSCENEMANAGER_MLF scripting symbol to enable MapLoaderFramework integration.");
#endif
        }

        private void HandleTriggerChapter(CutsceneStep step)
        {
#if CUTSCENEMANAGER_MLF
            var bridge = GetComponent<MapLoaderBridge>() ?? FindObjectOfType<MapLoaderBridge>();
            if (bridge != null && step.chapterId > 0)
                bridge.LoadChapter(step.chapterId);
            else
                Debug.LogWarning("[CutsceneManager] TriggerChapter: no MapLoaderBridge found or chapterId is 0.");
#else
            Debug.LogWarning("[CutsceneManager] TriggerChapter: define CUTSCENEMANAGER_MLF scripting symbol to enable MapLoaderFramework integration.");
#endif
        }

        private void HandleTriggerLua(CutsceneStep step)
        {
            if (string.IsNullOrEmpty(step.luaScript)) return;
#if CUTSCENEMANAGER_MLF
            MapLoaderFramework.Runtime.LuaScriptLoader.RunScriptByFileName(step.luaScript);
#else
            Debug.Log($"[CutsceneManager] TriggerLua '{step.luaScript}': define CUTSCENEMANAGER_MLF scripting symbol to enable Lua integration.");
#endif
        }

        private IEnumerator HandleCameraShake(CutsceneStep step)
        {
            var cam = Camera.main;
            if (cam == null || step.duration <= 0f) yield break;
            Vector3 originalPos = cam.transform.localPosition;
            float elapsed = 0f;
            while (elapsed < step.duration)
            {
                elapsed += Time.deltaTime;
                float x = (UnityEngine.Random.value * 2f - 1f) * step.shakeMagnitude;
                float y = (UnityEngine.Random.value * 2f - 1f) * step.shakeMagnitude;
                cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }
            cam.transform.localPosition = originalPos;
        }

        private IEnumerator HandlePlayVideo(CutsceneStep step)
        {
            if (string.IsNullOrEmpty(step.videoResource))
            {
                Debug.LogWarning("[CutsceneManager] PlayVideo step has no videoResource set.");
                yield break;
            }

            if (PlayVideoCallback != null)
            {
                // Delegate to VideoManager bridge — the bridge drives completion internally.
                PlayVideoCallback(step.videoResource);
                // Wait one frame so the video player can start before the next step executes.
                yield return null;
            }
            else
            {
                Debug.Log($"[CutsceneManager] PlayVideo '{step.videoResource}': define VIDEOMANAGER_CSM and add CutsceneVideoBridge to enable VideoManager integration.");
            }
        }

        private void HandleStopVideo(CutsceneStep step)
        {
            if (StopVideoCallback != null)
                StopVideoCallback();
            else
                Debug.Log("[CutsceneManager] StopVideo: define VIDEOMANAGER_CSM and add CutsceneVideoBridge to enable VideoManager integration.");
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void CleanupUI()
        {
            if (subtitleController != null) subtitleController.Hide();
            if (nameCardController != null) nameCardController.Hide();
            HideFullscreenImage();
        }

        private void HideFullscreenImage()
        {
            if (fullscreenImage != null) fullscreenImage.gameObject.SetActive(false);
        }

        /// <summary>Returns all loaded sequence ids.</summary>
        public IReadOnlyList<string> GetSequenceIds() => loadedSequenceIds;

        /// <summary>Returns the sequence data for <paramref name="id"/>, or null if not found.</summary>
        public CutsceneSequenceData GetSequence(string id) =>
            _sequences.TryGetValue(id, out var seq) ? seq : null;

        // -------------------------------------------------------------------------
        // Directory resolution
        // -------------------------------------------------------------------------

        private static string GetExternalCutscenesDirectory()
        {
#if UNITY_EDITOR
            return System.IO.Path.Combine(UnityEngine.Application.dataPath, "Cutscenes");
#else
            return System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Cutscenes");
#endif
        }
    }
}
