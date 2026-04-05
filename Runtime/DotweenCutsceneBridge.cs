#if CUTSCENEMANAGER_DOTWEEN
using UnityEngine;
using DG.Tweening;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// Optional bridge that replaces coroutine-based <see cref="FadeController"/> screen transitions
    /// with DOTween-driven equivalents and adds a slide-in animation to subtitle panels.
    /// Enable define <c>CUTSCENEMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Set <see cref="FadeController.defaultFadeOutDuration"/> and
    /// <see cref="FadeController.defaultFadeInDuration"/> to <c>0</c> so that the built-in
    /// coroutine fades complete instantly while this bridge owns the easing.
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenCutsceneBridge : MonoBehaviour
    {
        [Header("Screen Fade")]
        [Tooltip("Fade-to-black duration at the start of a sequence.")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Tooltip("Fade-to-clear duration at the end of a sequence.")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Tooltip("DOTween ease curve applied to screen fade transitions.")]
        [SerializeField] private Ease fadeEase = Ease.InOutSine;

        [Header("Subtitle Slide-In")]
        [Tooltip("Duration of the subtitle RectTransform slide-in animation.")]
        [SerializeField] private float subtitleSlideDuration = 0.3f;

        [Tooltip("Vertical pixel offset from which the subtitle slides into view.")]
        [SerializeField] private float subtitleSlideOffset = 60f;

        [Tooltip("DOTween ease curve applied to the subtitle slide.")]
        [SerializeField] private Ease subtitleEase = Ease.OutBack;

        // -------------------------------------------------------------------------

        private CutsceneManager    _csm;
        private FadeController     _fade;
        private SubtitleController _subtitle;
        private Tween              _fadeTween;

        private void Awake()
        {
            _csm      = GetComponent<CutsceneManager>()    ?? FindFirstObjectByType<CutsceneManager>();
            _fade     = GetComponent<FadeController>()     ?? FindFirstObjectByType<FadeController>();
            _subtitle = GetComponent<SubtitleController>() ?? FindFirstObjectByType<SubtitleController>();

            if (_csm  == null) Debug.LogWarning("[CutsceneManager/DotweenCutsceneBridge] CutsceneManager not found.");
            if (_fade == null) Debug.LogWarning("[CutsceneManager/DotweenCutsceneBridge] FadeController not found.");
        }

        private void OnEnable()
        {
            if (_csm == null) return;
            _csm.OnSequenceStarted   += OnSequenceStarted;
            _csm.OnSequenceCompleted += OnSequenceEnded;
            _csm.OnSequenceSkipped   += OnSequenceSkipped;
        }

        private void OnDisable()
        {
            if (_csm == null) return;
            _csm.OnSequenceStarted   -= OnSequenceStarted;
            _csm.OnSequenceCompleted -= OnSequenceEnded;
            _csm.OnSequenceSkipped   -= OnSequenceSkipped;
        }

        // -------------------------------------------------------------------------

        private void OnSequenceStarted(string sequenceId)
        {
            if (_fade != null)
            {
                _fadeTween?.Kill();
                _fade.StopAllCoroutines();
                _fadeTween = DOVirtual.Float(
                    _fade.GetCurrentAlpha(), 1f, fadeOutDuration,
                    a => _fade.SetAlpha(a)
                ).SetEase(fadeEase);
            }

            SlideSubtitleIn();
        }

        private void OnSequenceEnded(string sequenceId)
        {
            if (_fade != null)
            {
                _fadeTween?.Kill();
                _fade.StopAllCoroutines();
                _fadeTween = DOVirtual.Float(
                    _fade.GetCurrentAlpha(), 0f, fadeInDuration,
                    a => _fade.SetAlpha(a)
                ).SetEase(fadeEase);
            }
        }

        private void OnSequenceSkipped(string sequenceId)
        {
            _fadeTween?.Kill();
            _fade?.StopAllCoroutines();
            _fade?.SetAlpha(0f);
        }

        private void SlideSubtitleIn()
        {
            if (_subtitle == null) return;
            var rt = _subtitle.GetComponent<RectTransform>();
            if (rt == null) return;

            Vector2 dest = rt.anchoredPosition;
            rt.anchoredPosition = dest + Vector2.down * subtitleSlideOffset;
            DOTween.Kill(rt);
            rt.DOAnchorPos(dest, subtitleSlideDuration).SetEase(subtitleEase);
        }
    }
}
#else
namespace CutsceneManager.Runtime
{
    /// <summary>No-op stub — enable define <c>CUTSCENEMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("CutsceneManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenCutsceneBridge : UnityEngine.MonoBehaviour { }
}
#endif
