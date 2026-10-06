using BepInEx.Logging;
using HarmonyLib;
using TPMQoL;

namespace TPMQoLLive;

// Development template only. This DLL is not shipped in Nexus packages.
// Put experimental no-restart code here and deploy it with ..\deploy-live.ps1.
public sealed class LiveModule : ITPMQoLLiveModule
{
    private ManualLogSource _log;

    public void Start(Harmony harmony, ManualLogSource log)
    {
        _log = log;
        _log.LogInfo("TPMQoLLive development module loaded.");
    }

    public void Tick()
    {
    }

    public void Stop()
    {
        _log?.LogInfo("TPMQoLLive development module unloaded.");
    }
}
