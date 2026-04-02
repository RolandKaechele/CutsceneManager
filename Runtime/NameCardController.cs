using UnityEngine;
using UnityEngine.UI;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// <b>NameCardController</b> displays a chapter or location name card on screen.
    /// <para>
    /// Attach to a Canvas element containing a background panel and a Text label.
    /// Call <see cref="Show"/> with the title string; call <see cref="Hide"/> to dismiss it.
    /// Optionally assign a subtitle text for secondary information (e.g. episode number).
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Name Card Controller")]
    [DisallowMultipleComponent]
    public class NameCardController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel or image of the name card. Activated/deactivated by Show/Hide.")]
        [SerializeField] private GameObject cardRoot;

        [Tooltip("Primary title text (chapter name, location name, etc.).")]
        [SerializeField] private Text titleText;

        [Tooltip("Optional secondary subtitle text (e.g. episode number, tagline).")]
        [SerializeField] private Text subtitleText;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (cardRoot != null) cardRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Show the name card with the given <paramref name="title"/> and optional <paramref name="subtitle"/>.</summary>
        public void Show(string title, string subtitle = null)
        {
            if (titleText != null) titleText.text = title ?? string.Empty;
            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? string.Empty;
                subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }
            if (cardRoot != null) cardRoot.SetActive(true);
        }

        /// <summary>Hide the name card immediately.</summary>
        public void Hide()
        {
            if (cardRoot != null) cardRoot.SetActive(false);
        }

        /// <summary>True while the name card root is active.</summary>
        public bool IsVisible => cardRoot != null && cardRoot.activeSelf;
    }
}
