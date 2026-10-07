# Changelog

## 1.0.3 Preview

- Added a separate science-exhibit corrosion protection option (keeps corrosion at 100%)
- Added attraction / exhibit-ride construction speed control (1x-20x)
- Added attraction / exhibit-ride upgrade speed control (1x-20x)
- Attraction upgrade acceleration only touches ride upgrades; normal item upgrades remain unchanged
- Confirmed attraction construction and upgrade acceleration in live gameplay testing
- Fire-extinguishing acceleration was investigated but intentionally not added because preventing science-exhibit deterioration avoids the relevant fire condition
- Updated trainer controls and config hot reload for the new options
## 1.0.2

- Added exhibit preservation options:
  - keep exhibit condition / grubbiness at 100%
  - keep botany exhibit life at 100%
  - keep aquariums clean with filter capacity at maximum (999)
- Reworked preservation to use the stable, tested entity-level attribute APIs
- Removed experimental preservation hooks that could affect unrelated wildlife
- Added a safe live-module development host for future no-restart testing
- Clarified trainer labels for speeds, applicants, expeditions, First Aid, security coverage and preservation
- Updated the embedded plugin payload and trainer to version 1.0.2
## 1.0.1

- Added configurable exhibit extra/perk installation speed (1x-20x)
- Fixed max-knowledge rewards to respect each exhibit's own maximum
- Improved First Aid expedition-illness routing, including Cure-Machine-only illnesses
- Corrected First Aid item detection to the Expedition Recovery Device
- Kept normal item-upgrade speed and First Aid interaction speed at vanilla because those acceleration paths were not reliable
- Updated the trainer and embedded plugin payload for the confirmed 1.0.1 feature set

## 1.0.0

Initial public release.

- Workshop, training, analysis, veterinary and expedition speed controls
- Staff movement multiplier with vanilla energy/fatigue/skill behaviour preserved
- Maximum-knowledge options
- Applicant rank and skill-slot controls
- Expedition survey and quality options
- Security-monitor radius
- First Aid support for expedition illnesses
- External trainer with live config reload
- 15 trainer languages with Steam-language detection
- Automatic config backup and Vanilla reset
- Self-installing trainer:
  - detects the Steam game folder,
  - installs official BepInEx 6 IL2CPP x64 when required,
  - verifies the BepInEx download,
  - installs/updates TPMQoL automatically,
  - preserves existing compatible BepInEx installations and user config.
