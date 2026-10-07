using System;
using System.IO;
using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(Metagame), nameof(Metagame.OnUpdate))]
internal static class RuntimeControllerPatch
{
    private static int _ticks;
    private static bool _haveConfigTimestamp;
    private static DateTime _lastConfigWriteUtc;
    private static DateTime _pendingConfigWriteUtc;
    private static int _pendingStablePolls;

    internal static void InitializeConfigTimestamp()
    {
        var path = Plugin.ModConfig?.ConfigFilePath;
        _lastConfigWriteUtc =
            !string.IsNullOrEmpty(path) && File.Exists(path)
                ? File.GetLastWriteTimeUtc(path)
                : DateTime.MinValue;
        _haveConfigTimestamp = true;
        _pendingConfigWriteUtc = DateTime.MinValue;
        _pendingStablePolls = 0;
    }

    private static void Postfix(Metagame __instance)
    {
        if (__instance == null)
            return;

        _ticks++;

        try
        {
            if (_ticks % 15 == 0)
            {
                LiveModuleHost.EnsureApplied();
                ExhibitPreservationRuntime.EnsureApplied();
            }

            if (_ticks % 60 != 0)
                return;

            CheckConfigReload();
            TrainingDefinitionRuntime.EnsureApplied();
            KnowledgeSpeedRuntime.EnsureApplied();
            ExpeditionSurveyRuntime.EnsureApplied();
            FirstAidRuntime.EnsureApplied();
            ExhibitExtraSpeedRuntime.EnsureApplied();
            ExhibitRideSpeedRuntime.EnsureApplied();
            ApplicantEmptySkillsRuntime.EnsureApplied();
            StaffMovementAgentRegistry.EnsureExistingRegistered();
            SecurityCoverageRuntime.EnsureApplied();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Runtime controller failed: {ex}");
        }
    }

    private static void CheckConfigReload()
    {
        var path = Plugin.ModConfig.ConfigFilePath;
        var stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;

        if (!_haveConfigTimestamp)
        {
            _haveConfigTimestamp = true;
            _lastConfigWriteUtc = stamp;
            return;
        }

        if (stamp == DateTime.MinValue || stamp == _lastConfigWriteUtc)
        {
            _pendingConfigWriteUtc = DateTime.MinValue;
            _pendingStablePolls = 0;
            return;
        }

        if (stamp != _pendingConfigWriteUtc)
        {
            _pendingConfigWriteUtc = stamp;
            _pendingStablePolls = 0;
            return;
        }

        if (++_pendingStablePolls < 2) return;

        var previousStaffMovement = Plugin.StaffMovementMultiplier.Value;
        Plugin.ModConfig.Reload();

        if (Math.Abs(previousStaffMovement - Plugin.StaffMovementMultiplier.Value) > 0.0001f)
            StaffMovementAgentRegistry.RefreshExistingSpeeds();

        ExhibitExtraSpeedRuntime.Reapply();
        ExhibitRideSpeedRuntime.Reapply();
        ExhibitPreservationRuntime.Reapply();

        _lastConfigWriteUtc = File.Exists(path)
            ? File.GetLastWriteTimeUtc(path)
            : stamp;
        _pendingConfigWriteUtc = DateTime.MinValue;
        _pendingStablePolls = 0;

        Plugin.LogCurrentSettings("hot reload");
    }
}
