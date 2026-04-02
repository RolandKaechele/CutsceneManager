using UnityEngine;
using UnityEngine.UI;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// <b>SubtitleController</b> displays a single line of subtitle or dialogue text.
    /// <para>
    /// Attach to a Canvas element containing a Text label.
    /// The <see cref="CutsceneManager"/> drives this automatically during
    /// <see cref="CutsceneStepType.ShowText"/> steps.
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Subtitle Controller")]
    [DisallowMultipleComponent]
    public class SubtitleController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel of the subtitle bar. Activated/deactivated by Show/Hide.")]
        [SerializeField] private GameObject subtitleRoot;

        [Tooltip("Text label that displays the subtitle.")]
        [SerializeField] private Text subtitleText;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (subtitleRoot != null) subtitleRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Show the subtitle bar with <paramref name="text"/>.</summary>
        public void Show(string text)
        {
            if (subtitleText != null) subtitleText.text = text ?? string.Empty;
            if (subtitleRoot != null) subtitleRoot.SetActive(true);
        }

        /// <summary>Hide the subtitle bar immediately.</summary>
        public void Hide()
        {
            if (subtitleRoot != null) subtitleRoot.SetActive(false);
        }

        /// <summary>True while the subtitle root is active.</summary>
        public bool IsVisible => subtitleRoot != null && subtitleRoot.activeSelf;
    }
}
