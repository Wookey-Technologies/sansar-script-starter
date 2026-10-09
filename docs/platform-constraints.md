# Sansar Platform Constraints and Gotchas

Hard facts about the Sansar scripting environment, verified against the Sansar client
(47.7.0, October 2026 build) and its script import pipeline. When old forum posts, tutorials or
even comments in the example scripts disagree with this document, trust this document —
and when in doubt, trust the assemblies and API docs in this repository over everything.

## Compilation environment

Sansar compiles your `.cs` files itself when you import them (you never upload DLLs).
The importer uses:

| Setting | Value |
|---|---|
| Compiler | Roslyn (C# compiler), invoked with `-langversion:latest` |
| Effective max C# version | **C# 7.3** (the bundled Roslyn is 2.8.x) |
| Target framework | .NET Framework 4.7.2, class library |
| Preprocessor define | `SERVERSCRIPT_1_1` |
| Referenced assemblies | `Sansar.Script.dll`, `Sansar.Simulation.dll`, `Mono.Simd.dll`, `System.ComponentModel.DataAnnotations.dll` + the standard library (subject to the whitelist below) |

Practical consequences:

- String interpolation (`$"..."`), expression-bodied members, tuples, pattern matching,
  local functions, `out var` — all fine (C# 6/7 features).
- No C# 8+ features: no nullable reference types (`string?`), no ranges (`a[1..2]`),
  no switch expressions, no default interface methods, no `await using`.
- Old advice saying "Sansar only supports C# 5" is obsolete. It dated from people
  validating locally with the legacy .NET 4.0 `csc.exe`, which is C# 5 only.
  `tools/check.ps1` uses a modern compiler with the settings above.

## The API whitelist

After compiling, Sansar runs an IL-level *Enforcer* over your assembly. Only
whitelisted .NET types and methods may be used; anything else fails the import even
though it compiled. The human-readable whitelist ships in this repo:
[`../api-docs/access.html`](../api-docs/access.html).

Notes on commonly-questioned APIs (current whitelist):

- `System.DateTime.Now` / `.UtcNow` / `.Today` — **allowed** (old advice claiming
  otherwise is obsolete). `System.Diagnostics.Stopwatch` is also allowed and is still
  the best choice for measuring intervals.
- `System.Random`, `System.Math`, `System.Linq`, generic collections — allowed.
- The `System.Math` entry in `access.html` is incomplete in practice: it omits
  trigonometry, `Sqrt`, `Pow`, and other members used in shipped and community
  scripts. If an import rejects a Math member, check that entry first.
- .NET reflection, file I/O, sockets and threads — not available. Use the Sansar APIs
  instead: `ScenePrivate.HttpClient` for HTTP, coroutines instead of threads, and
  Sansar's own `Reflective` mechanism (`[RegisterReflective]` / `FindReflective`) for
  calling into other scripts.
- `System.Text.RegularExpressions` — not whitelisted.

## Execution model

- Scripts run as cooperatively-scheduled **microthreads**; the import pipeline injects
  yield points into your code. There is **no per-frame `Update()`** — use events
  (subscriptions) and coroutines with `Wait(...)` instead.
- Your script's entry point is `public override void Init()`.
- `Wait(TimeSpan)`, `Wait(double seconds)`, `WaitFor(...)`, `StartCoroutine(...)` are
  the scheduling primitives (`Sansar.Script.ScriptBase`).
- A script can be **preempted at almost any point**, including between an `IsValid`
  check and the next line. Code touching agents (players) must expect exceptions —
  an agent can log out at any moment (see Gotchas below).

## Things that kill your script

1. **Any unhandled exception terminates the script permanently.** Wrap agent
   interactions and other volatile operations in `try/catch`.
2. **Throttle exceptions.** Rate-limited functions throw `ThrottleException` when
   called too fast. Known throttle rates:

   | Function | Rate |
   |---|---|
   | `AgentPrivate.SendChat` | 64 calls / 2 sec |
   | `AgentPrivate.OverrideAudioStream` | 5 calls / 10 sec |
   | `AgentPrivate.OverrideMediaSource` | 5 calls / 10 sec |
   | `AgentPrivate.PerformMediaAction` | 5 calls / 10 sec |
   | `AgentPublic.SendChat` | 32 calls / 2 sec |
   | `ScenePrivate.Chat.MessageAllUsers` | 32 calls / 2 sec |
   | `ScenePrivate.CreateCluster` | 100 calls / sec |
   | `ScenePrivate.HttpClient.Request` | 10 calls / sec |
   | `ScenePrivate.OverrideAudioStream` | 5 calls / 10 sec |
   | `ScenePrivate.OverrideMediaSource` | 5 calls / 10 sec |
   | `ScenePrivate.PerformMediaAction` | 5 calls / 10 sec |

   This table is the authoritative copy — worked retry patterns are in the
   [scripting guide's Gotchas chapter](scripting-guide.md#throttle-exceptions).
   When spawning many objects (`CreateCluster`), use a queue/coroutine and catch
   `ThrottleException` with retry — see the
   [spawning guide](spawning-and-grid-guide.md).
3. **Memory limits.** Script memory is tracked per pool: all scene scripts share one
   (large) pool. Subscribe to `Memory` events and see
   `examples/official/MemoryExample.cs`.

## Deferred setters

Most `Set...` functions do not apply immediately; the system batches them. Reading a
value straight after setting it will usually return the old value:

```csharp
rigidBody.SetMass(2.0f);
float mass = rigidBody.GetMass();   // very likely still the old mass!
```

Use `WaitFor(rigidBody.SetMass, 2.0f)` when you must observe the applied value, but
don't wrap every setter in `WaitFor` — it serializes execution and each call waits a
frame or more.

## Editor-facing properties

- Public fields on your script class become editable properties in the scene editor.
  Supported types: `bool`, `int`, `float`, `double`, `string`, `Vector`, `Quaternion`,
  `Color`, `Interaction`, `ClusterResource`, `SoundResource`, and `List<T>` of these.
- Limits: **20 properties** for `SceneObjectScript`, **10** for `ObjectScript`.
  Use `List<T>` properties to pack more data in.
- Metadata attributes: `[DefaultValue(...)]`, `[Range(min,max)]`, `[Tooltip("...")]`,
  `[DisplayName("...")]`.

## Object configuration matters

Many APIs silently require the object to be configured correctly in the editor:

- **Lights**: the light's "Scriptable" flag must be On (`lightComp.IsScriptable`).
- **Meshes** (visibility/materials): the mesh must be scriptable
  (`mesh.IsScriptable`).
- **Movers** (non-physical movement): "Movable From Script" must be On; physics
  objects additionally need motion type "keyframed".
- **Motion types** can only be changed from script toward *more restrictive*
  (dynamic → keyframed), never the other way.
- **Animations**: imported FBX animations are resampled to 30 fps.

Always code the failure path (`TryGetFirstComponent` returning false etc.) with a
`Log.Write` so misconfiguration is diagnosable in the debug console (Ctrl+D in world).

## API evolution notes

The API moves. Heed compiler warnings from `tools/check.ps1` — `[Obsolete]` warnings
tell you the current replacement. Known changes you may hit in older community code:

- `InterruptibleWait(...)` — removed; use `Wait(...)`.
- `StreamChannel.AudioChannel1` — renamed to `StreamChannel.AudioChannel`.
- `Vector.Up` (and friends) — obsolete in favor of `ObjectUp` etc.

New in **47.7.0**:

- Ragdoll control on `AgentPrivate`: `SetRagdollEnabled` / `GetRagdollEnabled`,
  `SetRagdollBounce` / `GetRagdollBounce`, `ApplyRagdollImpulse` and
  `ApplyRagdollImpulseToRegions`, with `RagdollRegion` flags for body regions.
- `MirrorComponent` (`ComponentType.MirrorComponent`): `GetIsEnabled` reads the global
  state; `SetIsEnabled` sets it globally or for an individual user.
- Scene library on `ScenePrivate`: `AddToSceneLibrary`, `RemoveFromSceneLibrary`,
  `ClearSceneLibrary` and their `AddToSceneLibraryForAgent`,
  `RemoveFromSceneLibraryForAgent`, `ClearSceneLibraryForAgent` variants.
- On-screen menus via `AgentPrivate.Client.UI.Menu` (`UIMenu`): `Show`, `ShowStyled`,
  `Hide`, `Title` / `Subtitle` / `Footer`, `SelectedIndex` / `SelectedItem`, and
  the item limit `cMaxItems`.
- Seated vehicle input uses existing keypad commands for WASD and the VR/gamepad
  left stick. These are digital press/release events, not analog axis values — see
  [vehicle / seated driving input](scripting-guide.md#vehicle--seated-driving-input).

## Iteration workflow

1. Write your script (start from `templates/NewScript.cs` or an example).
2. Compile-check locally: `powershell -File tools\check.ps1 MyScript.cs`.
   This catches syntax and API-signature errors in seconds instead of a full
   import round-trip. It cannot catch whitelist violations or runtime behavior.
3. In Sansar: **Import → Script**, pick the `.cs` file (or a `.json` script assembly),
   then attach the script to an object and build the scene.
4. Use `Log.Write(...)` and the debug console (Ctrl+D) in the built scene.
   Note: log output is throttled too — keep it lean.
