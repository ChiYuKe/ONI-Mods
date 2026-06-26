# Geyser World-Output Fallback Configuration Design

## Goal

Add a global StorageNetwork configuration option that controls whether a network-connected geyser may emit material into the world when direct network output cannot store it.

## Configuration

Add a boolean `AllowGeyserWorldOutputFallback` property to `Config`.

- Default: `true`.
- `true`: preserve the current behavior and allow material that cannot be stored in the network to reach the world through `ElementEmitter.ForceEmit` or the native-emitter fallback.
- `false`: while the geyser is enrolled in the network with direct output enabled, never emit its output into the world when network storage is unavailable. Keep the native emitter disabled.
- The configuration dialog exposes the property as a boolean toggle with localized title and description.

Existing configuration files that do not contain the property receive the constructor default of `true`, preserving compatibility.

## Runtime Behavior

`StorageNetworkGeyserOutput.Sim200ms` remains responsible for checking compatible storage capacity every 200 ms.

While the geyser is enrolled in the network with direct output enabled, the disabled policy applies when:

- No compatible storage exists for the emitted element.
- Compatible storage exists but has no available capacity.
- Compatible storage accepts only part of the generated mass.
- The network core is offline.
- A specifically selected target server is missing or unavailable.

When no compatible server has available capacity:

- With `AllowGeyserWorldOutputFallback` enabled, emit the entire generated mass into the world as today.
- With it disabled, emit nothing into the world and return while leaving the native emitter disabled.

When compatible servers accept only part of the generated mass:

- With the option enabled, emit the remaining mass into the world.
- With it disabled, do not emit the remaining mass.

When no compatible server exists, an offline core prevents access, or the selected server is unavailable:

- With the option enabled, retain the current native-output fallback.
- With it disabled, keep the native emitter disabled and emit nothing into the world.

Suppressed mass is discarded. It is not queued or restored later. Once a valid network destination with capacity becomes available, the next 200 ms simulation update resumes direct storage automatically.

Turning direct network output off or removing the geyser from the storage network always restores vanilla emitter behavior, regardless of this configuration.

## Status and Localization

Keep the existing overflow status when `AllowGeyserWorldOutputFallback` is enabled. Add paused statuses for the disabled policy so the UI distinguishes a full server from another unavailable-network condition. English status text includes `Server full, output paused` and `Network unavailable, output paused`.

Add localized configuration strings and paused-status text to `STRINGS.cs` and `translations/en.po`.

## Validation

The repository has no automated StorageNetwork test project. Validation will consist of:

- Compiling `StorageNetwork.csproj`.
- Inspecting the final diff for the full-capacity, partial-capacity, missing-target, offline-core, and no-compatible-storage branches.
- Verifying the default value is `true` and the new English localization entries exist.
