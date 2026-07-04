# Tools

## check.ps1 — local compile checking

Compiles scripts with the same settings Sansar's importer uses (Roslyn, C# 7.3,
`SERVERSCRIPT_1_1`), referencing the Sansar assemblies and `Mono.Simd.dll` from
`../assemblies` plus `System.ComponentModel.DataAnnotations` when installed.
The local compiler is newer than Sansar's (language version is pinned to 7.3 to
compensate) and compiles against your machine's .NET Framework — in rare corner
cases something could pass here and still fail on import, but not the reverse.

```powershell
.\tools\check.ps1 MyScript.cs                 # single script
.\tools\check.ps1 MyAssembly.json             # multi-file script assembly
.\tools\check.ps1 examples\community          # a directory
.\tools\check.ps1 -All                        # everything in examples/ + templates/
```

Requires Visual Studio 2022+ or the free "Build Tools for Visual Studio". If only the
legacy .NET 4.0 compiler is available it will warn: that compiler is C# 5 only and
falsely rejects modern scripts.

Exit code 0 = all units compiled. Warnings don't fail the check but read them —
`[Obsolete]` warnings name replacement APIs.

## build_sansar.py — multi-file builds (optional)

Merges a multi-file C# project into a single `.cs` file for Sansar upload, driven by a
`*-build.json` config. Requires Python 3. Note that Sansar natively supports multi-file
projects via `.json` script assemblies (see `examples/snippets/ScriptAssemblies/Method2`),
which is simpler — use this tool only if you specifically want a single merged file.

```bash
python tools/build_sansar.py                  # interactive: discover configs
python tools/build_sansar.py MyProject/       # build one project
python tools/build_sansar.py --all            # build everything
python tools/build_sansar.py MyProject/ --compile   # also compile-check the output
```

Config format (`myproject-build.json`):

```json
{
  "project": "MyScript",
  "output": "MyScript.cs",
  "namespace": "MyNamespace",
  "build_order": [
    { "type": "using", "statements": ["System", "Sansar", "Sansar.Script", "Sansar.Simulation"] },
    { "type": "namespace_open" },
    { "type": "file", "path": "Components/Helper.cs", "extract": "class_content" },
    { "type": "file", "path": "Core/Main.cs", "extract": "content_only" },
    { "type": "namespace_close" }
  ]
}
```

Extraction modes: `full` (entire file), `content_only` (inside namespace braces),
`class_content` (class without namespace), `partial_content` (partial class body).
