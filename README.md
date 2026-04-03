# CutsceneManager

A lightweight, data-driven cutscene framework for Unity.  
Sequences are defined in plain JSON, played back step-by-step at runtime, and optionally integrated with [MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework) for seamless map/chapter transitions.


## Features

- **14 step types** — fade, wait, image slide, subtitle/dialogue, name card, audio, camera shake, map/chapter load, Lua trigger, custom events
- **JSON-authored sequences** — no code required to create or edit cutscenes
- **Runtime hot-loading** — sequences in `persistentDataPath/Cutscenes/` are loaded at startup alongside bundled `Resources/Cutscenes/` files
- **Skip support** — per-sequence `skipAllowed` flag; skip key is configurable (default: Escape)
- **MapLoaderFramework integration** — optional bridge hooks the framework's `TransitionCallback` so fade transitions apply on every map/chapter load (activated via a scripting define)
- **SaveManager integration** — `SaveCutsceneBridge` records seen sequences as save flags to prevent repeated first-play cutscenes (activated via `CUTSCENEMANAGER_SM`)
- **InventoryManager integration** — `InventoryCutsceneBridge` interprets `Custom` step payloads as inventory commands to add, remove, or use items during a cutscene (activated via `CUTSCENEMANAGER_IM`)
- **MiniGameManager integration** — `MiniGameCutsceneBridge` plays cutscene sequences automatically when a mini-game starts, completes, or is aborted (activated via `CUTSCENEMANAGER_MGM`)
- **Lua trigger step** — run named Lua scripts during a sequence (requires MapLoaderFramework with MoonSharp)
- **Custom Inspector** — play, stop, and reload sequences from the Unity Editor
- **Modular architecture** — each controller (fade, name card, subtitle) is a standalone component; use only what you need


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/CutsceneManager.git
   ```

### Option B — Clone into your project

```bash
git clone https://github.com/RolandKaechele/CutsceneManager.git Assets/CutsceneManager
```

### Option C — Manual copy

Copy the `CutsceneManager/` folder into your project's `Assets/` directory.


## Folder Structure

After installation the post-install script creates the following working directories in your project root (next to `Assets/`):

```
Resources/
  Cutscenes/       ← bundled JSON sequences (loaded via Resources.Load)
Cutscenes/         ← external/mod JSON sequences (loaded from disk)
Scripts/           ← Lua scripts referenced by TriggerLua steps
```

Example sequences are copied from `CutsceneManager/Examples/Cutscenes/` into `Resources/Cutscenes/`.


## Quick Start

### 1. Add the CutsceneManager to your scene

Create an empty GameObject, add the `CutsceneManager` component, then attach:

| Component | Purpose |
| --------- | ------- |
| `CutsceneManager` | Main orchestrator (required) |
| `FadeController` | Screen fade — needs a full-screen `Image` |
| `NameCardController` | Chapter/location name card UI |
| `SubtitleController` | Subtitle / dialogue text UI |

Wire the optional controllers to the **CutsceneManager** inspector fields.

### 2. Write a sequence (JSON)

Place a `.json` file in `Resources/Cutscenes/` (or `Cutscenes/` for runtime loading):

```json
{
  "id": "my_intro",
  "title": "My Intro",
  "skipAllowed": true,
  "steps": [
    { "stepType": 0, "duration": 0.4, "fadeColor": "#000000" },
    { "stepType": 6, "text": "Chapter 1", "duration": 2.5 },
    { "stepType": 4, "text": "A long time ago…", "duration": 0.0, "waitForInput": true },
    { "stepType": 5 },
    { "stepType": 0, "duration": 0.5, "fadeColor": "#000000" }
  ]
}
```

### 3. Play from code

```csharp
CutsceneManager manager = FindFirstObjectByType<CutsceneManager>();
manager.PlaySequence("my_intro");
```

### 4. Play via trigger

Add `CutsceneTrigger` to any GameObject and set:

| Field | Value |
| ----- | ----- |
| Sequence Id | `my_intro` |
| Trigger Mode | `OnStart` / `OnTriggerEnter` / `OnInteract` |
| Play Once | ✓ |


## Step Type Reference

All 14 step types and the JSON properties each reads:

| Value | Name | Properties used |
| ----- | ---- | --------------- |
| 0 | `Fade` | `duration`, `fadeColor` |
| 1 | `Wait` | `duration`, `waitForInput` |
| 2 | `ShowImage` | `imageResource`, `duration`, `waitForInput` |
| 3 | `HideImage` | *(none)* |
| 4 | `ShowText` | `text`, `localizationKey`, `duration`, `waitForInput` |
| 5 | `HideText` | *(none)* |
| 6 | `ShowNameCard` | `text` (title), `localizationKey` (subtitle), `duration` |
| 7 | `PlayAudio` | `audioResource`, `audioLoop` |
| 8 | `StopAudio` | *(none)* |
| 9 | `TriggerMapLoad` | `mapId` |
| 10 | `TriggerChapter` | `chapterId` |
| 11 | `TriggerLua` | `luaScript` (script name without extension) |
| 12 | `CameraShake` | `duration`, `shakeMagnitude` |
| 13 | `Custom` | `customEvent` (arbitrary string) |

### Common Step Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `stepType` | int | Step type value (see table above) |
| `duration` | float | Duration in seconds (0 = instant or indefinite) |
| `waitForInput` | bool | Pause until the player presses the confirm key |
| `fadeColor` | string | Hex colour for Fade steps, e.g. `"#000000"` |
| `imageResource` | string | `Resources`-relative path to a `Sprite` or `Texture2D` |
| `text` | string | Display text (subtitle, name card title…) |
| `localizationKey` | string | Localisation key (overrides `text` when resolved) |
| `audioResource` | string | `Resources`-relative path to an `AudioClip` |
| `audioLoop` | bool | Loop the audio clip |
| `mapId` | string | Map ID for `TriggerMapLoad` (requires MLF bridge) |
| `chapterId` | int | Chapter index for `TriggerChapter` (requires MLF bridge) |
| `luaScript` | string | Script filename (without `.lua`) for `TriggerLua` |
| `shakeMagnitude` | float | Shake intensity for `CameraShake` |
| `customEvent` | string | Event name broadcast via `OnCustomEvent` |


## MapLoaderFramework Integration

CutsceneManager can drive map/chapter loads through **MapLoaderFramework** without creating a compile-time dependency in either direction.

### Enable the integration

1. Go to **Edit → Project Settings → Player → Scripting Define Symbols**
2. Add `CUTSCENEMANAGER_MLF`

### Add the bridge

Add the `MapLoaderBridge` component to the same (or any) GameObject.  
It automatically finds `MapLoaderManager` and `FadeController` in the scene and hooks into `MapLoaderFramework.TransitionCallback`.

From that point:

- Every map/chapter load triggered by MapLoaderFramework will fade out → load → fade in via `FadeController`
- `TriggerMapLoad` and `TriggerChapter` cutscene steps will call MapLoaderFramework directly

### Call from code

```csharp
MapLoaderBridge bridge = FindFirstObjectByType<MapLoaderBridge>();
bridge.LoadMap("my_map_id");
bridge.LoadChapter(3);
```


## Runtime API

### `CutsceneManager`

| Member | Description |
| ------ | ----------- |
| `PlaySequence(string id)` | Play a loaded sequence by ID |
| `PlaySequence(CutsceneSequenceData)` | Play a sequence object directly |
| `StopSequence()` | Interrupt the active sequence |
| `LoadAllSequences()` | Reload all sequences from disk and Resources |
| `GetSequenceIds()` | Return all known sequence IDs |
| `GetSequence(string id)` | Return a sequence by ID (or null) |
| `IsPlaying` | True while a sequence is running |
| `OnSequenceStarted` | `UnityEvent<string>` — fires when a sequence starts |
| `OnSequenceCompleted` | `UnityEvent<string>` — fires when a sequence ends normally |
| `OnSequenceSkipped` | `UnityEvent<string>` — fires when the player skips |
| `OnCustomEvent` | `UnityEvent<string>` — fires on `Custom` steps |
| `PlayAudioCallback` | `Action<string, bool>` delegate — override audio playback for `PlayAudio` steps |
| `StopAudioCallback` | `Action` delegate — override audio stop for `StopAudio` steps |

### `CutsceneTrigger`

| Member | Description |
| ------ | ----------- |
| `Trigger()` | Fire the sequence from code or UI |
| `sequenceId` | ID of the sequence to play |
| `triggerMode` | `OnStart` / `OnTriggerEnter` / `OnInteract` |
| `playOnce` | Only trigger once per scene load |

### `FadeController`

| Member | Description |
| ------ | ----------- |
| `FadeOut(duration, onComplete)` | Fade to opaque |
| `FadeIn(duration, onComplete)` | Fade to transparent |
| `FadeOutAndIn(onMiddle, outDuration, inDuration, onComplete)` | Full fade cycle |
| `SetAlpha(float)` | Set alpha directly |
| `GetCurrentAlpha()` | Return current alpha |

### `NameCardController`

| Member | Description |
| ------ | ----------- |
| `Show(title, subtitle)` | Display the name card |
| `Hide()` | Hide the name card |
| `IsVisible` | True when the card is shown |

### `SubtitleController`

| Member | Description |
| ------ | ----------- |
| `Show(text)` | Display subtitle text |
| `Hide()` | Hide subtitle text |
| `IsVisible` | True when subtitle is shown |

### `MapLoaderBridge` *(requires `CUTSCENEMANAGER_MLF`)*

| Member | Description |
| ------ | ----------- |
| `LoadMap(string mapId)` | Load a map through MapLoaderFramework |
| `LoadChapter(int chapterId)` | Load a chapter through MapLoaderFramework |

### `SaveCutsceneBridge` *(requires `CUTSCENEMANAGER_SM`)*

| Member | Description |
| ------ | ----------- |
| `HasSeen(string sequenceId) → bool` | Returns true if the sequence has been recorded as seen |
| `flagPrefix` | Inspector — prefix prepended to sequence ID (default: `"cutscene_seen_"`) |

### `InventoryCutsceneBridge` *(requires `CUTSCENEMANAGER_IM`)*

Subscribes to `OnCustomEvent` and dispatches payloads matching the verb prefixes to `InventoryManager.AddItem`, `RemoveItem`, or `UseItem`. All verb prefixes are configurable in the Inspector.

### `MiniGameCutsceneBridge` *(requires `CUTSCENEMANAGER_MGM`)*

Listens to `MiniGameManager.OnMiniGameStarted`, `OnMiniGameCompleted`, and `OnMiniGameAborted`. When any event fires, it derives a cutscene sequence id by appending a configurable suffix to the mini-game id and plays it if it exists.

| Inspector Field | Default | Description |
| --------------- | ------- | ----------- |
| `Start Suffix` | `"_start"` | e.g. `puzzle_01_start` plays when mini-game `puzzle_01` starts |
| `Complete Suffix` | `"_complete"` | e.g. `puzzle_01_complete` plays on completion |
| `Abort Suffix` | `"_abort"` | e.g. `puzzle_01_abort` plays on abort |


## AudioManager Integration

CutsceneManager exposes two delegate hooks so an external audio system can handle `PlayAudio` and `StopAudio` steps through its own mixer, with volume scaling and channel routing.

### Enable

1. Install [AudioManager](https://github.com/RolandKaechele/AudioManager)
2. Add `AUDIOMANAGER_CSM` to **Edit → Project Settings → Player → Scripting Define Symbols**
3. Attach `CutsceneAudioBridge` to any GameObject in your scene

`CutsceneAudioBridge.Awake()` wires `PlayAudioCallback` and `StopAudioCallback` automatically. Without the bridge, CutsceneManager falls back to its built-in raw `AudioSource`.

You can also wire the delegates manually:

```csharp
var mgr = FindFirstObjectByType<CutsceneManager.Runtime.CutsceneManager>();
mgr.PlayAudioCallback = (resource, loop) => myAudio.PlayMusic(resource);
mgr.StopAudioCallback = () => myAudio.StopMusic();
```


## SaveManager Integration

CutsceneManager can record which sequences have been seen in **SaveManager** so first-play cutscenes do not repeat after loading a save.

### Enable

1. Add `CUTSCENEMANAGER_SM` to **Scripting Define Symbols**
2. Attach `SaveCutsceneBridge` to any GameObject in your scene.

`SaveCutsceneBridge` subscribes to `OnSequenceCompleted` and `OnSequenceSkipped`. When either fires it calls `SaveManager.SetFlag("{flagPrefix}{sequenceId}")` (default prefix: `"cutscene_seen_"`).

```csharp
var bridge = FindFirstObjectByType<SaveCutsceneBridge>();
if (!bridge.HasSeen("intro_chapter_01"))
    cutsceneManager.PlaySequence("intro_chapter_01");
```

### Inspector Fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `Flag Prefix` | `"cutscene_seen_"` | Prepended to the sequence ID when writing the seen flag |


## InventoryManager Integration

CutsceneManager can modify the player's inventory during a sequence using `Custom` step payloads.

### Enable

1. Add `CUTSCENEMANAGER_IM` to **Scripting Define Symbols**
2. Attach `InventoryCutsceneBridge` to any GameObject in your scene.

`InventoryCutsceneBridge` listens to `OnCustomEvent`. Payloads matching the configured verbs are dispatched to `InventoryManager`:

| Payload format | Effect |
| -------------- | ------ |
| `"inventory.add:sword"` | Adds 1 unit of `sword` |
| `"inventory.add:sword:3"` | Adds 3 units of `sword` |
| `"inventory.remove:sword"` | Removes 1 unit of `sword` |
| `"inventory.remove:sword:3"` | Removes 3 units |
| `"inventory.use:sword"` | Calls `InventoryManager.UseItem("sword")` |

### Inspector Fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `Add Verb` | `"inventory.add"` | Payload prefix for granting items |
| `Remove Verb` | `"inventory.remove"` | Payload prefix for removing items |
| `Use Verb` | `"inventory.use"` | Payload prefix for using items |

### Example sequence step

```json
{ "stepType": 13, "customEvent": "inventory.add:key_reactor_room" }
```


## Custom Events

Listen to named custom events from `CutsceneManager.OnCustomEvent`:

```csharp
var mgr = FindFirstObjectByType<CutsceneManager.Runtime.CutsceneManager>();
mgr.OnCustomEvent.AddListener(evt =>
{
    if (evt == "spawn_enemy") SpawnEnemy();
});
```


## Examples

The `Examples/` folder contains ready-to-run sequences:

| File | Description |
| ---- | ----------- |
| `Cutscenes/example_chapter01_intro.json` | Chapter intro — fade, splash image, name card, subtitle, chapter load |
| `Cutscenes/example_location_transition.json` | Location warp — fade, ambient audio, name card, map load |
| `Scripts/example_cutscene_trigger.lua` | Lua script called by a `TriggerLua` step |


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| TextMeshPro | optional | Used by `NameCardController` / `SubtitleController` if present |
| MapLoaderFramework | optional | Required only when `CUTSCENEMANAGER_MLF` is defined |
| MoonSharp | optional | Required for `TriggerLua` steps (included in MapLoaderFramework) |
| SaveManager | optional | Required when `CUTSCENEMANAGER_SM` is defined |
| InventoryManager | optional | Required when `CUTSCENEMANAGER_IM` is defined |
| MiniGameManager | optional | Required when `CUTSCENEMANAGER_MGM` is defined |


## License

MIT — see [LICENSE](LICENSE).
