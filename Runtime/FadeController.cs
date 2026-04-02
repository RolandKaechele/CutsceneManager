using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// <b>FadeController</b> drives full-screen fade-in / fade-out transitions.
    /// <para>
    /// Attach to a Canvas (Screen Space – Overlay). Assign a full-screen black <see cref="Image"/>
    /// as <c>fadeImage</c>. The component manages alpha internally.
    /// </para>
    /// <para>
    /// This component was extracted from <c>MapLoaderFramework.Runtime.MapTransitionController</c>
    /// so it can be owned by the CutsceneManager package while remaining usable standalone.
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Fade Controller")]
    [DisallowMultipleComponent]
    public class FadeController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Image")]
        [Tooltip("Full-screen overlay Image used for the fade effect. Alpha 0 = fully transparent.")]
        [SerializeField] private Image fadeImage;

        [Header("Timing")]
        [Tooltip("Default fade-to-black duration (seconds).")]
        [SerializeField] private float defaultFadeOutDuration = 0.4f;

        [Tooltip("Default fade-from-black duration (seconds).")]
        [SerializeField] private float defaultFadeInDuration = 0.5f;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when the screen reaches the target colour (fade-out or colour-to complete).</summary>
        public event Action OnFadeOutComplete;
        /// <summary>Fired when the screen is fully transparent again (fade-in complete).</summary>
        public event Action OnFadeInComplete;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private bool _isFading;
        /// <summary>True while a fade coroutine is running.</summary>
        public bool IsFading => _isFading;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            SetAlpha(0f);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Fade the screen to black using the default duration.</summary>
        public void FadeOut(Action onComplete = null) =>
            FadeOut(defaultFadeOutDuration, onComplete);

        /// <summary>Fade the screen to black over <paramref name="duration"/> seconds.</summary>
        public void FadeOut(float duration, Action onComplete = null) =>
            StartCoroutine(FadeCoroutine(GetCurrentAlpha(), 1f, duration, () =>
            {
                OnFadeOutComplete?.Invoke();
                onComplete?.Invoke();
            }));

        /// <summary>Fade the screen from black to transparent using the default duration.</summary>
        public void FadeIn(Action onComplete = null) =>
            FadeIn(defaultFadeInDuration, onComplete);

        /// <summary>Fade the screen from black to transparent over <paramref name="duration"/> seconds.</summary>
        public void FadeIn(float duration, Action onComplete = null) =>
            StartCoroutine(FadeCoroutine(GetCurrentAlpha(), 0f, duration, () =>
            {
                OnFadeInComplete?.Invoke();
                onComplete?.Invoke();
            }));

        /// <summary>
        /// Fade out, invoke <paramref name="onMiddle"/> while black, then fade in.
        /// </summary>
        /// <param name="onMiddle">Called when screen is fully black. Perform scene/map swaps here.</param>
        /// <param name="fadeOutDuration">Override fade-out duration (0 = use default).</param>
        /// <param name="fadeInDuration">Override fade-in duration (0 = use default).</param>
        /// <param name="onComplete">Called when fade-in finishes.</param>
        public void FadeOutAndIn(Action onMiddle,
            float fadeOutDuration = 0f,
            float fadeInDuration = 0f,
            Action onComplete = null)
        {
            float outD = fadeOutDuration > 0f ? fadeOutDuration : defaultFadeOutDuration;
            float inD  = fadeInDuration  > 0f ? fadeInDuration  : defaultFadeInDuration;
            StartCoroutine(FadeOutAndInCoroutine(onMiddle, outD, inD, onComplete));
        }

        /// <summary>Set the fade overlay to a specific alpha immediately (0 = clear, 1 = opaque).</summary>
        public void SetAlpha(float alpha)
        {
            if (fadeImage == null) return;
            var c = fadeImage.color;
            c.a = Mathf.Clamp01(alpha);
            fadeImage.color = c;
        }

        /// <summary>Returns the current alpha of the fade image.</summary>
        public float GetCurrentAlpha() =>
            fadeImage != null ? fadeImage.color.a : 0f;

        // -------------------------------------------------------------------------
        // Coroutines
        // -------------------------------------------------------------------------

        private IEnumerator FadeOutAndInCoroutine(Action onMiddle, float outD, float inD, Action onComplete)
        {
            _isFading = true;
            yield return FadeCoroutine(GetCurrentAlpha(), 1f, outD, null);
            OnFadeOutComplete?.Invoke();
            onMiddle?.Invoke();
            yield return null; // allow one frame for scene changes
            yield return FadeCoroutine(GetCurrentAlpha(), 0f, inD, null);
            OnFadeInComplete?.Invoke();
            onComplete?.Invoke();
            _isFading = false;
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete)
        {
            _isFading = true;
            float elapsed = 0f;
            if (duration <= 0f)
            {
                SetAlpha(to);
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                    yield return null;
                }
                SetAlpha(to);
            }
            onComplete?.Invoke();
            _isFading = false;
        }
    }
}
