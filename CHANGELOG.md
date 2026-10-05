# Changelog

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
