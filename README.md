# Sansar Script Starter

Everything you (and your AI coding agent) need to write C# scripts for
[Sansar](https://www.sansar.com/) — in one folder.

This kit bundles the Sansar scripting API assemblies, the full API reference, a large
collection of compile-verified example scripts, topic guides, and a local
compile-checker that uses the same compiler settings as Sansar's own script importer.
It is designed to be **dropped into your project while you develop and deleted when
you're done** — it's reference material, not a runtime dependency.

## Quick start

### Working with an AI agent (recommended)

1. Copy or clone this repository into your project folder, e.g. `my-world/sansar-script-starter/`.
2. Tell your agent:

   > Read `sansar-script-starter/AGENTS.md` before writing any Sansar scripts.

   (Claude Code discovers the instructions automatically via `CLAUDE.md` when you work
   inside this folder.)
3. Ask for what you want: *"Make a door that opens when clicked and closes after 5
   seconds."* The agent has the API docs, working examples, platform constraints and a
   compile-checker — everything needed to hand you a clean, importable `.cs` file.
4. In Sansar: **Import → Script**, choose the `.cs` file, drag it onto an object,
   build the scene.

### Working by hand

Start with [docs/scripting-guide.md](docs/scripting-guide.md) — a full introduction to
Sansar scripting. Then browse [examples/](examples/) and use
[api-docs/index.html](api-docs/index.html) as your API reference.

## What's inside

| Path | Contents |
|---|---|
| [AGENTS.md](AGENTS.md) | Instructions for AI coding agents: rules, workflow, and a task → example index. |
| [docs/scripting-guide.md](docs/scripting-guide.md) | The main scripting tutorial (from Linden Lab's `sansar-script`, updated for this kit). |
| [docs/platform-constraints.md](docs/platform-constraints.md) | Verified platform facts: C# 7.3 / Roslyn / .NET 4.7.2, API whitelist, throttle rates, execution model. |
| [docs/material-api-guide.md](docs/material-api-guide.md) | Runtime material control (tint, emissive, animation). |
| [docs/spawning-and-grid-guide.md](docs/spawning-and-grid-guide.md) | Object spawning, grid layouts, throttle-safe batching. |
| [api-docs/](api-docs/) | Full generated API reference (`index.html`) and the allowed-.NET-API list (`access.html`). |
| [assemblies/](assemblies/) | `Sansar.Script.dll` / `Sansar.Simulation.dll` + IntelliSense XML — what scripts compile against. |
| [examples/official/](examples/official/) | Example scripts shipped with Sansar, incl. the Scene Scripts Library and Quest library source. |
| [examples/snippets/](examples/snippets/) | Small single-purpose scripts from the scripting guide. |
| [examples/community/](examples/community/) | Community-contributed scripts (binah, GranddadGotMojo, evoav, leslie-linden, and more). |
| [examples/games/](examples/games/) | Complete games with assets (minesweeper). |
| [tools/check.ps1](tools/check.ps1) | Local compile check. `.\tools\check.ps1 MyScript.cs` or `-All`. |
| [tools/build_sansar.py](tools/build_sansar.py) | Optional multi-file → single `.cs` build tool ([docs](tools/README.md)). |
| [templates/NewScript.cs](templates/NewScript.cs) | Starting skeleton with the common patterns in place. |

## Local compile checking

```powershell
.\tools\check.ps1 MyScript.cs        # one script
.\tools\check.ps1 MyAssembly.json    # a multi-file script assembly
.\tools\check.ps1 -All               # every example in the kit
```

The checker mirrors Sansar's import settings (Roslyn, C# 7.3, .NET Framework 4.7.2,
`SERVERSCRIPT_1_1`). It needs a modern C# compiler: Visual Studio 2022+ or the free
[Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/)
(look under "Tools for Visual Studio"). Every example in this kit passes `-All`.

A pass here means the script will compile on import; Sansar additionally enforces its
.NET API whitelist ([api-docs/access.html](api-docs/access.html)) and, of course,
runtime behavior is only testable in-world.

## Freshness

The assemblies, API docs, whitelist and official examples in this kit are a snapshot
of the **March 2026 Sansar client**. To refresh them from a newer installation, copy:

- `C:\Program Files\Sansar\Client\ScriptApi\Assemblies\*` → `assemblies/`
- `C:\Program Files\Sansar\Client\ScriptApi\Documentation\*` → `api-docs/`
- `C:\Program Files\Sansar\Client\ScriptApi\access.html` → `api-docs/access.html`
- `C:\Program Files\Sansar\Client\ScriptApi\Examples\*` → `examples/official/`

then run `.\tools\check.ps1 -All` to see whether any examples were broken by API
changes.

## Attribution

- Official examples, the Scene Scripts Library, the scripting guide, API assemblies
  and documentation are © Linden Research, Inc., from the Sansar client distribution
  and the public [`lindenlab/sansar-script`](https://github.com/lindenlab/sansar-script)
  repository.
- Community example scripts are by their respective authors (binah, GranddadGotMojo,
  evoav, leslie-linden, DarkfyreAlgoma, bagnaria), collected from that same public
  repository. Minor fixes were applied so that everything compiles against the current
  API.
