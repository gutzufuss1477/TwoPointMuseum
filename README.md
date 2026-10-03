# Two Point Museum QoL Trainer

Version 1.0.0

A configurable quality-of-life mod for Two Point Museum with a separate multilingual Windows trainer.

Tested with:
- Two Point Museum 12.0.243206+2026-09-29.2232
- BepInEx 6.0.0-be.788 IL2CPP
- Windows x64

## Features

### Speed
- Workshop project speed: 1x-20x
- Staff training speed: 1x-20x
- Exhibit analysis speed: 1x-20x
- Veterinary treatment and spa speed: 1x-20x
- Expedition progress speed: 1x-20x
- Staff movement speed: 1x-5x
  - Staff only; guests are not affected.
  - Vanilla energy, fatigue and movement-skill modifiers are preserved.

### Knowledge
- Optional maximum exhibit knowledge after one analysis
- Optional maximum wildlife knowledge after one successful veterinary treatment

### Applicants
- Minimum applicant rank: 1-20
- Optional applicant skill cleanup:
  - Keeps the vanilla-generated base skill when present
  - Removes additional randomly generated skills
  - Leaves unlocked training slots free

### Expeditions
- Optional maximum survey after one completed expedition
- Configurable exhibit quality:
  - Vanilla
  - Normal
  - Great
  - Epic
  - Pristine
- Quality can be used as a minimum or forced exactly

### Security
- Configurable security-monitor coverage radius
- Up to 5000; 1000 is effectively museum-wide in normal maps

### Health
- First Aid Kits can remove expedition illnesses from staff

## External Trainer

TPMQoLTrainer.exe provides a simple graphical interface for all main settings.

Features:
- Detects whether Two Point Museum is running
- Shows the installed mod version
- Reads and writes the existing BepInEx config
- Settings are applied by the mod through live config reload
- "All Vanilla" reset
- Launch-game button
- Automatic backup of the config before the trainer first modifies it
- Manual config-file selection for non-standard installations

The trainer is self-contained for Windows x64 and does not require a separate .NET installation.

### Trainer languages

The trainer supports the same 15 interface languages offered by Two Point Museum:
- English
- French
- Italian
- German
- Spanish - Spain
- Japanese
- Korean
- Portuguese - Brazil
- Simplified Chinese
- Traditional Chinese
- Turkish
- Polish
- Russian
- Spanish - Latin America
- Thai

Language selection order:
1. Previously selected trainer language
2. Two Point Museum language configured in Steam
3. Windows display language
4. English fallback

The language can be changed at any time from the trainer.

## Installation

BepInEx 6 IL2CPP x64 is required.

1. Install BepInEx 6 IL2CPP for Two Point Museum.
2. Extract this archive directly into the Two Point Museum game folder.
3. Start the game once so BepInEx creates TPMQoL.cfg.
4. Run TPMQoLTrainer\TPMQoLTrainer.exe to configure the mod.

After extraction the important files are:

Two Point Museum\
- BepInEx\plugins\TPMQoL\TPMQoL.dll
- TPMQoLTrainer\TPMQoLTrainer.exe

The trainer normally finds the config automatically. If it does not, use "Choose config" and select:

BepInEx\config\TPMQoL.cfg

## Updating

Replace TPMQoL.dll and TPMQoLTrainer.exe with the newer versions.

Do not delete TPMQoL.cfg unless you intentionally want to reset all settings.

## Uninstall

Remove:
- BepInEx\plugins\TPMQoL\TPMQoL.dll
- TPMQoLTrainer\
- optionally BepInEx\config\TPMQoL.cfg

## Notes

- Changes made in the trainer are written to TPMQoL.cfg and picked up by the mod while the game is running.
- Some gameplay changes naturally become visible on the next relevant action, applicant generation, expedition, analysis, treatment, etc.
- Research and Marketing are intentionally not modified.
