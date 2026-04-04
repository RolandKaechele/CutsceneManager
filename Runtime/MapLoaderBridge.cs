#if CUTSCENEMANAGER_MLF
using System;
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace CutsceneManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderBridge</b> connects CutsceneManager to MapLoaderFramework without creating a hard
    /// compile-time dependency. It is conditionally compiled when the
    /// <c>CUTSCENEMANAGER_MLF</c> scripting define symbol is present in the project.
    /// <para>
    /// <b>Integration steps:</b>
    /// <list type="number">
    /// <item>Add MapLoaderFramework to the project (UPM or Assets).</item>
    /// <item>Add <c>CUTSCENEMANAGER_MLF</c> to <em>Project Settings › Player › Scripting Define Symbols</em>.</item>
    /// <item>Attach this component to the same GameObject as <c>MapLoaderFramework.Runtime.MapLoaderManager</c>.</item>
    /// <item>CutsceneManager cutscene steps of type <see cref="CutsceneStepType.TriggerMapLoad"/> and
    /// <see cref="CutsceneStepType.TriggerChapter"/> will now work.</item>
    /// <item>Chapter transitions in MapLoaderFramework will automatically use FadeController for the
    /// fade-out/fade-in if a <see cref="FadeController"/> is present in the scene.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("CutsceneManager/Map Loader Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderBridge : MonoBehaviour
    {
        private MapLoaderManager _manager;
        private FadeController _fade;

        private void Awake()
        {
            _manager = GetComponent<MapLoaderManager>() ?? FindObjectOfType<MapLoaderManager>();
            _fade    = FindObjectOfType<FadeController>();
            HookTransitionCallback();
        }

        /// <summary>
        /// Sets <c>MapLoaderFramework.TransitionCallback</c> so that every chapter load
        /// is wrapped in a FadeController fade-out / fade-in.
        /// </summary>
        private void HookTransitionCallback()
        {
            var framework = GetComponent<MapLoaderFramework>() ?? FindObjectOfType<MapLoaderFramework>();

            if (framework == null || _fade == null) return;

            framework.TransitionCallback = (displayName, doLoad) =>
            {
                _fade.FadeOutAndIn(
                    onMiddle: doLoad,
                    onComplete: null
                );
            };

            Debug.Log("[MapLoaderBridge] Hooked FadeController into MapLoaderFramework.TransitionCallback.");
        }

        /// <summary>Load a map by id or name via MapLoaderManager.</summary>
        public void LoadMap(string mapId)
        {
            if (_manager != null) _manager.LoadMap(mapId);
            else Debug.LogWarning("[MapLoaderBridge] MapLoaderManager not found.");
        }

        /// <summary>Load a chapter by number via MapLoaderManager.</summary>
        public void LoadChapter(int chapterId)
        {
            if (_manager != null) _manager.LoadChapter(chapterId);
            else Debug.LogWarning("[MapLoaderBridge] MapLoaderManager not found.");
        }
    }
}
#endif
