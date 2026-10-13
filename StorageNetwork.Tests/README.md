# StorageNetwork verification

Run the complete release gate from the repository root:

```powershell
pwsh -NoProfile -File .\StorageNetwork.Tests\Verify.ps1
```

Use `-GameManagedDir <path>` when Oxygen Not Included is installed somewhere other
than the default path in the script.

The gate builds the mod and deterministic harness in Release, executes the linked
runtime/algorithm checks, enforces static hot-path contracts, compares the public
API with `PublicApiBaseline.txt`, resolves every Harmony target against the current
game assemblies, and runs `git diff --check` on the in-scope files.

`PublicApiBaseline.txt` is intentionally exact. Do not regenerate it to make a
failure disappear: review any public addition, removal, optional default, or type
change as an API compatibility decision first.

Prepare an isolated copy of a benchmark save while ONI is closed:

```powershell
pwsh -NoProfile -File .\StorageNetwork.Tests\PrepareBenchmarkFixture.ps1 `
  -SourceSave 'C:\path\to\fixed_fixture.sav'
```

The command copies the save, preview, and StorageNetwork sidecars to a separate
colony directory and writes a SHA-256 manifest. Load only that copy during long
profiling runs so ONI auto-save cannot overwrite the fixed source fixture.

Analyze the latest profiler session after the requested warm-up windows:

```powershell
pwsh -NoProfile -File .\StorageNetwork.Tests\AnalyzeFrameProfile.ps1 `
  -ExpectedActivePorts 28 -SkipWindows 2 -MinimumWindows 5
```

For a visible main/category panel run, also pass `-RequireMainPanel`,
`-RequireCategorySummary`, `-MaxLayoutCalls 0`, and
`-MaxWorldSnapshotRebuilds 0`.

Algorithm optimization results and pending game benchmarks are recorded in
`ALGORITHM_OPTIMIZATION.md`. The full gate intentionally still reports the
pre-existing global Sprite compatibility scan; its rule has not been relaxed.

Run production-linked runtime tests independently of source contracts:

```powershell
dotnet run --project .\StorageNetwork.Tests\StorageNetwork.Tests.csproj -c Release -- --runtime-only
```

Run all deterministic/runtime/source-contract checks without the static scan gate:

```powershell
dotnet run --project .\StorageNetwork.Tests\StorageNetwork.Tests.csproj -c Release
```

`AnalyzeFrameProfile.ps1 -OutputJson <path>` exports selected CPU, allocation,
GC and algorithm operation metrics. Use `CompareFrameProfiles.ps1` with three
independent reports per version to check the relative performance targets.
`VerifyProfileComparison.ps1` tests this pipeline with explicitly synthetic logs.
`VerifyLogicMaterialSerialization.ps1` verifies the actual compiled mod against a
JSON fixture exported from the pre-optimization DLL; do not regenerate the fixture
merely to hide a serialization mismatch.
