using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutsceneManager.Runtime
{
    // -------------------------------------------------------------------------
    // Step type enum
    // -------------------------------------------------------------------------

    /// <summary>
    /// Defines the action performed by a single <see cref="CutsceneStep"/>.
    /// </summary>
    public enum CutsceneStepType
    {
        /// <summary>Fade the screen to or from a colour.</summary>
        Fade,
        /// <summary>Wait for a fixed duration or until the player presses a button.</summary>
        Wait,
        /// <summary>Display a full-screen or framed image (e.g. anime art, chapter splash).</summary>
        ShowImage,
        /// <summary>Hide the image displayed by a preceding ShowImage step.</summary>
        HideImage,
        /// <summary>Display a subtitle or dialogue line on screen.</summary>
        ShowText,
        /// <summary>Hide the subtitle/text displayed by a preceding ShowText step.</summary>
        HideText,
        /// <summary>Show the chapter/location name card.</summary>
        ShowNameCard,
        /// <summary>Play an audio clip (music or SFX).</summary>
        PlayAudio,
        /// <summary>Stop the currently playing audio.</summary>
        StopAudio,
        /// <summary>Trigger a MapLoaderFramework map load via <see cref="MapLoaderBridge"/>.</summary>
        TriggerMapLoad,
        /// <summary>Trigger a MapLoaderFramework chapter load via <see cref="MapLoaderBridge"/>.</summary>
        TriggerChapter,
        /// <summary>Run a named Lua script via MapLoaderFramework.Runtime.LuaScriptLoader.</summary>
        TriggerLua,
        /// <summary>Apply a camera-shake effect for the specified duration and magnitude.</summary>
        CameraShake,
        /// <summary>Broadcast a named custom event string on <see cref="CutsceneManager.OnCustomEvent"/>.</summary>
        Custom,
        /// <summary>Play a video clip with optional SRT subtitles via VideoManager (or built-in VideoPlayer fallback).</summary>
        PlayVideo,
        /// <summary>Stop the currently playing video.</summary>
        StopVideo
    }


    // -------------------------------------------------------------------------
    // CutsceneStep
    // -------------------------------------------------------------------------

    /// <summary>
    /// A single step within a <see cref="CutsceneSequenceData"/>. All fields are optional
    /// depending on <see cref="stepType"/>; unused fields are simply ignored at runtime.
    /// </summary>
    [Serializable]
    public class CutsceneStep
    {
        // --- Step identity ---

        /// <summary>Step type determining which action is performed.</summary>
        public CutsceneStepType stepType;

        // --- Timing ---

        /// <summary>
        /// Duration in seconds.
        /// <list type="bullet">
        /// <item><see cref="CutsceneStepType.Fade"/> — fade duration</item>
        /// <item><see cref="CutsceneStepType.Wait"/> / <see cref="CutsceneStepType.ShowImage"/> /
        /// <see cref="CutsceneStepType.ShowText"/> — hold duration (0 = indefinite until input)</item>
        /// <item><see cref="CutsceneStepType.CameraShake"/> — shake duration</item>
        /// </list>
        /// </summary>
        public float duration = 1f;

        /// <summary>If true, the step does not advance until the player presses the configured skip/confirm button.</summary>
        public bool waitForInput = false;

        // --- Fade ---

        /// <summary>Target colour for a Fade step. <c>"black"</c> or a hex colour (#RRGGBB). Default is black.</summary>
        public string fadeColor = "black";

        // --- Image ---

        /// <summary>
        /// Path to the image relative to a <c>Resources/</c> folder (without extension).
        /// Used by <see cref="CutsceneStepType.ShowImage"/>.
        /// </summary>
        public string imageResource;

        // --- Text ---

        /// <summary>Text content for <see cref="CutsceneStepType.ShowText"/> or <see cref="CutsceneStepType.ShowNameCard"/>.</summary>
        public string text;

        /// <summary>Localization key for the text (overrides <see cref="text"/> if the localization system resolves it).</summary>
        public string localizationKey;

        // --- Audio ---

        /// <summary>
        /// Path to the audio clip relative to a <c>Resources/</c> folder (without extension).
        /// Used by <see cref="CutsceneStepType.PlayAudio"/>.
        /// </summary>
        public string audioResource;

        /// <summary>Whether to loop the audio clip.</summary>
        public bool audioLoop = false;

        // --- Map / Chapter loading ---

        /// <summary>Map id or name passed to <c>MapLoaderBridge.LoadMap</c>.</summary>
        public string mapId;

        /// <summary>Chapter number passed to <c>MapLoaderBridge.LoadChapter</c>.</summary>
        public int chapterId;

        // --- Lua ---

        /// <summary>Lua script file name (with extension) passed to <c>LuaScriptLoader.RunScriptByFileName</c>.</summary>
        public string luaScript;

        // --- Camera shake ---

        /// <summary>Shake intensity (world units).</summary>
        public float shakeMagnitude = 0.3f;

        // --- Custom ---

        /// <summary>Arbitrary event name broadcast via <see cref="CutsceneManager.OnCustomEvent"/>.</summary>
        public string customEvent;

        // --- Video ---

        /// <summary>
        /// Registered VideoManager clip id or StreamingAssets-relative path (without extension).
        /// Used by <see cref="CutsceneStepType.PlayVideo"/>.
        /// </summary>
        public string videoResource;
    }


    // -------------------------------------------------------------------------
    // CutsceneSequenceData
    // -------------------------------------------------------------------------

    /// <summary>
    /// Data container for a complete cutscene sequence — a named, ordered list of <see cref="CutsceneStep"/>s.
    /// <para>
    /// Load from JSON placed in <c>Resources/Cutscenes/</c> or any path registered with <see cref="CutsceneManager"/>.
    /// </para>
    /// <para><b>Minimal JSON example:</b></para>
    /// <code>
    /// {
    ///   "id": "chapter_01_intro",
    ///   "title": "Chapter 1 — The Beginning",
    ///   "skipAllowed": true,
    ///   "steps": [
    ///     { "stepType": 0, "fadeColor": "black", "duration": 0.4 },
    ///     { "stepType": 6, "text": "Chapter 1" },
    ///     { "stepType": 9, "chapterId": 1 },
    ///     { "stepType": 0, "fadeColor": "clear", "duration": 0.5 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    [Serializable]
    public class CutsceneSequenceData
    {
        /// <summary>Unique identifier used to look up and play this sequence.</summary>
        public string id;

        /// <summary>Human-readable title (used on name cards and in the Inspector).</summary>
        public string title;

        /// <summary>Whether the player may skip this sequence via the configured skip button.</summary>
        public bool skipAllowed = true;

        /// <summary>Ordered list of steps that form this sequence.</summary>
        public List<CutsceneStep> steps;

        /// <summary>Raw JSON string (set at load time, not stored in the JSON file itself).</summary>
        [NonSerialized]
        public string rawJson;
    }
}
