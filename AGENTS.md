# Sansar Script Starter — Agent Instructions

You are helping a creator write C# scripts for **Sansar** (the social VR platform).
This folder is a self-contained reference kit: API assemblies, full API docs, a large
set of compile-verified examples, and a local compile-checker. Everything you need is
in here — you do not need the Sansar client installed to write and validate scripts.

## Hard rules

1. **C# 7.3 maximum.** Sansar compiles imported scripts with Roslyn 2.8 /
   `.NET Framework 4.7.2`. C# 6/7 features are fine (string interpolation, tuples,
   pattern matching); C# 8+ features are not (no `string?`, ranges, switch
   expressions).
2. **Only whitelisted .NET APIs.** No file I/O, threads, sockets, .NET reflection, or
   `Regex`. The full allowed list is `api-docs/access.html`. Use Sansar equivalents:
   `ScenePrivate.HttpClient` (HTTP), coroutines (`StartCoroutine`/`Wait`) instead of
   threads, and Sansar's own `Reflective` mechanism (`[RegisterReflective]` /
   `FindReflective`) for cross-script calls — that one is fine.
3. **Unhandled exceptions permanently kill the script.** Wrap calls on agents
   (players) in `try/catch` — an agent can disconnect between an `IsValid` check and
   the next line. Catch `ThrottleException` on rate-limited calls (chat, media,
   `CreateCluster`, HTTP — table in `docs/platform-constraints.md`).
4. **No `Update()` loop exists.** Scripts are event-driven: subscribe to events, use
   coroutines with `Wait(TimeSpan.FromSeconds(...))` for periodic work. Entry point is
   `public override void Init()` on a class deriving from `SceneObjectScript`.
5. **Setters are deferred.** A `Get` right after a `Set` usually returns the old
   value; use `WaitFor(...)` only when you must observe the applied result.
6. **Compile-check before handing scripts to the user:**
   `powershell -ExecutionPolicy Bypass -File tools/check.ps1 path/to/Script.cs`
   (exit code 0 = pass). Fix errors *and heed `[Obsolete]` warnings* — they name the
   current replacement API.

## Workflow

1. Read the relevant example(s) below and `docs/platform-constraints.md` before
   writing nontrivial code.
2. Write the script as a single `.cs` file when possible. Multiple cooperating classes
   can share one file under a namespace; multi-file projects need a `.json` script
   assembly (see `examples/snippets/ScriptAssemblies/`).
3. Run `tools/check.ps1` on it. Iterate until clean.
4. The **user** then imports the `.cs` (or `.json`) in Sansar: *Import → Script*, drags
   the script onto an object, and builds the scene. Scripts cannot be uploaded as DLLs
   and cannot be tested outside Sansar — in-world behavior must be verified by the user.
5. Debugging in-world: `Log.Write(...)` output appears in the debug console (Ctrl+D),
   visible only to the scene owner.

## What's where

| Path | Contents |
|---|---|
| `docs/scripting-guide.md` | Main tutorial: properties, interaction, movement, sound, chat commands, inter-script messaging, HTTP/JSON, gotchas. Read the section for your task. |
| `docs/platform-constraints.md` | Verified platform facts: compiler, whitelist, throttles, execution model, API evolution notes. |
| `docs/material-api-guide.md` | Runtime material control: tint, emissive glow, animated transitions. |
| `docs/spawning-and-grid-guide.md` | `CreateCluster` spawning, grids, throttle-safe batch spawning. |
| `api-docs/index.html` | Full generated API reference (per-class HTML). `api-docs/access.html` = allowed .NET API whitelist. |
| `assemblies/` | `Sansar.Script.dll` + `Sansar.Simulation.dll` (+ IntelliSense XML). The authoritative API surface — when docs disagree with the XML, trust these. |
| `examples/official/` | ~40 examples shipped with Sansar + the Scene Scripts Library source (`ScriptLibrary/`) and Quest library. |
| `examples/snippets/` | Small single-purpose scripts extracted from the scripting guide. |
| `examples/community/` | Real-world community scripts (larger, messier, battle-tested patterns). |
| `examples/games/minesweeper/` | Complete game with assets. For a large single-script game, also see the guides above. |
| `tools/check.ps1` | Local compile check with Sansar's exact settings. `-All` checks every example. |
| `tools/build_sansar.py` | Optional: merges multi-file projects into one importable `.cs`. |
| `templates/NewScript.cs` | Starting skeleton with common patterns. |

## Task → reference index

| Task | Look at |
|---|---|
| Hello world / first script | `examples/official/MyScripts/HelloWorld.cs`, `examples/official/SimpleScriptExample.cs`, guide "Getting started" |
| Editor-configurable properties | `examples/snippets/PropertiesExampleScript.cs` (limit: 20 per script) |
| Click interactions | `examples/official/InteractionExample.cs`, `examples/snippets/AddInteractionScript.cs` |
| Keyboard/controller input | `examples/official/CommandExample.cs`, `examples/snippets/AllAgentsCommandScript.cs` |
| Vehicle / seated driving input | guide "Vehicle / seated driving input", `api-docs/Sansar.Simulation/CommandData.html` |
| Held-object input (guns, flashlights) | `examples/snippets/FlashlightScript.cs`, `examples/official/PewPewExample.cs` |
| Moving objects (non-physics) | `examples/official/MoverExample1.cs`, `MoverExample2.cs`, `MoverExample3.cs`, `examples/snippets/PatrolMoverScript.cs` |
| Physics: forces, collisions | `examples/snippets/RigidBodyImpulseScript.cs`, guide "How to control physical objects" |
| Agent ragdoll | `api-docs/Sansar.Simulation/AgentPrivate.html`, `api-docs/Sansar.Simulation/RagdollRegion.html` |
| Trigger volumes | `examples/snippets/TriggerVolumeScript.cs` |
| Raycasts / shapecasts | `examples/official/CastRayExample.cs` |
| Sounds / audio streams | `examples/snippets/SoundScript.cs`, `examples/community/binah/audio/` |
| Lights | `examples/snippets/LightOffScript.cs`, guide "How to turn lights on and off" |
| Materials: tint, glow | `docs/material-api-guide.md`, `examples/community/binah/material-api/` |
| Mesh visibility | `examples/official/MeshVisibilityExample.cs` |
| Mirrors | `api-docs/Sansar.Simulation/MirrorComponent.html` |
| Animations | `examples/official/AnimationExample.cs`, `examples/community/leslie-linden/animation/` |
| Spawning objects | `docs/spawning-and-grid-guide.md`, `examples/official/ScriptLibrary/Scene/Dispenser.cs` |
| Scene library | `api-docs/Sansar.Simulation/ScenePrivate.html` (scene library methods) |
| Chat + chat commands | `examples/snippets/LowGravityChatCommandScript.cs`, guide "How to implement chat commands" |
| Dialogs / UI hints | guide "How to see text in world", `examples/community/GranddadGotMojo/Hint.cs` |
| Scriptable on-screen menu (UIMenu) | `api-docs/Sansar.Simulation/UIMenu.html`, `api-docs/Sansar.Simulation/UI.html` |
| Logging | `examples/official/LogExample.cs`, guide "How to use the debug console" |
| Teleporting agents | `examples/official/TeleportHotkeys.cs` |
| Coroutines & timing | `examples/official/CoroutineExample.cs`, `AdvancedCoroutineExample.cs` |
| Inter-script messaging | `examples/snippets/MessagingScripts.cs`, guide "How to send and receive messages between scripts" |
| Direct cross-script calls | `examples/snippets/ReflectiveCallerScript.cs` + `ReflectiveReceiverScript.cs` |
| Talking to built-in "simple scripts" | `examples/snippets/SimpleSenderScript.cs` + `SimpleListenerScript.cs`, `examples/official/ScriptLibrary/LibraryBase.cs` |
| HTTP / REST / JSON | `examples/official/HttpClientExample.cs`, `examples/snippets/HTTPFortuneJSONScript.cs` |
| Persistent storage | `examples/official/DataStoreExample.cs`, `examples/community/evoav/SafeDataStore/` |
| Quests | `examples/official/ScriptLibrary/Quest/`, `examples/community/binah/intro-to-quests/` |
| Player scale / gravity | `examples/official/AgentScaleExample.cs`, `GravityExample.cs` |
| Memory limits | `examples/official/MemoryExample.cs` |
| Multi-script assemblies | `examples/snippets/ScriptAssemblies/Method1` (one file) and `Method2` (json) |
| Complete game architecture | `examples/games/minesweeper/minesweeper.cs` |

## Notes

- Every example in this kit compiles against the current API (`tools/check.ps1 -All`
  passes). Community examples still reflect their authors' styles and eras — prefer
  `examples/official/` and the guides for canonical patterns.
- This kit is reference material. It is typically vendored into a project during
  development and deleted when done — don't build hard dependencies on its location.
