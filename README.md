# Two Point Museum QoL Trainer

Version 1.0.3 Preview

A configurable quality-of-life mod for Two Point Museum with a multilingual self-installing Windows trainer.

Tested with:
- Two Point Museum 12.0.243206+2026-09-29.2232
- BepInEx 6.0.0-be.788 IL2CPP
- Windows x64

## Installation

1. Extract the download anywhere.
2. Run `TPMQoLTrainer.exe`.
3. Done.

The trainer automatically:
- finds the Two Point Museum Steam installation,
- installs the tested official BepInEx 6 IL2CPP x64 build if BepInEx is missing,
- verifies the BepInEx download by SHA-256,
- applies the TPM-required `UnityLogListening = false` setting,
- installs or updates `TPMQoL.dll`,
- creates the mod config if required.

If Two Point Museum cannot be found automatically, select the game folder containing `TPM.exe`.

An existing compatible BepInEx installation is left intact.

The trainer is self-contained and does not require a separate .NET installation.

## Features

### Speed
- Workshop: 1x-20x
- Training: 1x-20x
- Analysis: 1x-20x
- Veterinary treatment/spa: 1x-20x
- Expeditions: 1x-20x
- Staff movement: 1x-5x
  - staff only,
  - guests are unaffected,
  - vanilla energy, fatigue, robot charging and movement-skill modifiers remain active.
- Exhibit extra/perk installation: 1x-20x
- Attraction / exhibit-ride construction: 1x-20x
- Attraction / exhibit-ride upgrades: 1x-20x (normal item upgrades unaffected)

### Knowledge
- Maximum exhibit knowledge after one analysis
- Maximum wildlife knowledge after one successful veterinary treatment

### Applicants
- Minimum applicant rank: 1-20
- Keep the vanilla base skill while removing additional random skills
- Leave unlocked training slots free

### Expeditions
- Maximum survey after one completed expedition
- Configurable exhibit quality: Vanilla / Normal / Great / Epic / Pristine
- Minimum-quality or forced-exact-quality mode

### Security
- Configurable security-monitor coverage radius

### Health
- First Aid can remove expedition illnesses from staff

### Exhibit preservation
- Exhibits can be kept at 100% condition / grubbiness
- Botany exhibits can be kept at 100% life so they no longer die from life decay
- Aquariums can be kept clean with minimum messiness and maximum filter capacity (999)
- Science-exhibit corrosion can be kept at 100%
- Preservation changes use safe managed/entity-level APIs; direct ECS buffer mutation is not used

## Trainer
- Live config editing while the game is running
- Installed mod version and game-running status
- Automatic config backup
- One-click Vanilla reset
- Launch-game button
- Manual config selection if required
- 15 interface languages
- Automatic Steam game-language detection

Supported languages:
English, French, Italian, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Simplified Chinese, Traditional Chinese, Turkish, Polish, Russian, Spanish (Latin America), Thai.

## Updating

Run the new trainer version. It automatically updates the TPMQoL plugin while preserving the existing config.

If the game is currently running and an update is required, the trainer asks you to close the game first.

## Uninstall

Remove:
- `BepInEx\plugins\TPMQoL\TPMQoL.dll`
- optionally `BepInEx\config\TPMQoL.cfg`

BepInEx itself is not removed automatically because other installed mods may depend on it.

## Notes

- Normal item-upgrade installation speed is intentionally not modified.
- First Aid still cures supported expedition ailments, but its interaction duration is left at vanilla speed.
- Research and Marketing are intentionally not modified.
- The automatic installer downloads BepInEx only from the official BepInEx build server.
