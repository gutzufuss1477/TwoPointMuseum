# Building the published 1.0.4.4 source

Install the .NET SDK and use your legitimate game installation with the documented
BepInEx IL2CPP interop files. Run `powershell -File Build.ps1 -GameDirectory <path>`.
The plugin is built first and passed into the trainer as an embedded resource;
no precompiled TPMQoL.dll or generated game assemblies are committed.

The original project requires the matching game/BepInEx environment. Imported
source does not imply a new in-game test or a bit-for-bit reproducible compiler.
