using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace TPMQoL;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    internal static ConfigFile ModConfig;

    // Speed suite
    internal static ConfigEntry<float> TrainingXpMultiplier;
    internal static ConfigEntry<float> WorkshopSpeedMultiplier;
    internal static ConfigEntry<float> AnalysisSpeedMultiplier;
    internal static ConfigEntry<float> WildlifeSpeedMultiplier;
    internal static ConfigEntry<float> ExpeditionSpeedMultiplier;
    internal static ConfigEntry<float> StaffMovementMultiplier;
    internal static ConfigEntry<float> ExhibitExtraInstallSpeedMultiplier;

    // Security
    internal static ConfigEntry<float> SecurityCoverageRadius;

    // Knowledge
    internal static ConfigEntry<bool> AnalysisMaxKnowledgeAfterOne;
    internal static ConfigEntry<bool> WildlifeMaxKnowledgeAfterOne;

    // Applicants
    internal static ConfigEntry<int> ApplicantMinimumRank;
    internal static ConfigEntry<bool> ApplicantEmptySkills;

    // Expeditions
    internal static ConfigEntry<float> ExpeditionSurveyXpMultiplier;
    internal static ConfigEntry<bool> ExpeditionMaxSurveyAfterOne;
    internal static ConfigEntry<int> ExpeditionQuality;
    internal static ConfigEntry<bool> ExpeditionForceQuality;

    // Health
    internal static ConfigEntry<bool> FirstAidCuresExpeditionAilments;
    internal static ConfigEntry<bool> FirstAidDiagnostics;

    public override void Load()
    {
        Log = base.Log;
        ModConfig = Config;

        TrainingXpMultiplier = BindSpeed("Speed", "Training", 2.0f,
            "Staff skill-training speed. 1.0 = vanilla.", 20.0f);
        WorkshopSpeedMultiplier = BindSpeed("Speed", "Workshop", 2.0f,
            "Workshop active project speed. 1.0 = vanilla.", 20.0f);
        AnalysisSpeedMultiplier = BindSpeed("Speed", "Analysis", 2.0f,
            "Exhibit analysis speed. 1.0 = vanilla.", 20.0f);
        WildlifeSpeedMultiplier = BindSpeed("Speed", "Wildlife", 1.0f,
            "Vet-room animal treatment/spa speed. 1.0 = vanilla.", 20.0f);
        ExpeditionSpeedMultiplier = BindSpeed("Speed", "Expeditions", 2.0f,
            "Expedition progress speed. 1.0 = vanilla.", 20.0f);
        StaffMovementMultiplier = BindSpeed("Speed", "StaffMovement", 1.0f,
            "Final movement-speed multiplier for staff only. Vanilla energy, fatigue and qualification modifiers are preserved.", 5.0f);
        ExhibitExtraInstallSpeedMultiplier = BindSpeed("Speed", "ExhibitExtras", 10.0f,
            "Exhibit perk/extra installation speed. 1.0 = vanilla.", 20.0f);

        SecurityCoverageRadius = Config.Bind("Security", "CoverageRadius", 1000.0f,
            new ConfigDescription(
                "Security-monitor coverage radius. 0 = vanilla; 1000 is effectively museum-wide.",
                new AcceptableValueRange<float>(0.0f, 5000.0f)));

        AnalysisMaxKnowledgeAfterOne = Config.Bind("Knowledge", "AnalysisMaxAfterOne", true,
            "One exhibit analysis grants enough insight to reach maximum knowledge.");
        WildlifeMaxKnowledgeAfterOne = Config.Bind("Knowledge", "WildlifeMaxAfterOne", true,
            "One successful vet-room animal treatment grants enough insight to reach maximum knowledge.");

        ApplicantMinimumRank = Config.Bind("Applicants", "MinimumRank", 1,
            new ConfigDescription("Minimum rank for newly generated applicants. 1 = vanilla.",
                new AcceptableValueRange<int>(1, 20)));
        ApplicantEmptySkills = Config.Bind("Applicants", "EmptySkills", true,
            "Keep one vanilla-generated base skill on each applicant, but remove additional random skills so unlocked training slots stay free.");

        ExpeditionSurveyXpMultiplier = BindSpeed("Expeditions", "SurveyXPMultiplier", 1.0f,
            "Survey/Erkundungsfortschritt per expedition. 1.0 = vanilla.", 20.0f);
        ExpeditionMaxSurveyAfterOne = Config.Bind("Expeditions", "MaxSurveyAfterOne", false,
            "Set the completed POI to its maximum survey level after one expedition.");
        ExpeditionQuality = Config.Bind("Expeditions", "Quality", -1,
            new ConfigDescription(
                "Expedition exhibit quality: -1=vanilla, 0=Average, 1=Great, 2=Epic, 3=Pristine.",
                new AcceptableValueRange<int>(-1, 3)));
        ExpeditionForceQuality = Config.Bind("Expeditions", "ForceQuality", false,
            "If true, force exactly the configured quality. If false, configured quality acts as a minimum.");

        FirstAidCuresExpeditionAilments = Config.Bind("Health", "FirstAidCuresExpeditionAilments", true,
            "Extend normal staff first-aid interactions to remove expedition ailments once first-aid item IDs are identified.");
        FirstAidDiagnostics = Config.Bind("Diagnostics", "LogFirstAidCandidates", false,
            "Diagnostic logging for first-aid interactions.");

        RuntimeControllerPatch.InitializeConfigTimestamp();

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        TryPatch(harmony, typeof(RuntimeControllerPatch), "Runtime/config controller");

        TryPatch(harmony, typeof(WorkshopProgressPatch), "Workshop active progress");
        TryPatch(harmony, typeof(ApplicantLevelPatch), "Applicant rank floor");
        TryPatch(harmony, typeof(AnalysisKnowledgeRewardPatch), "Analysis max-knowledge reward");
        TryPatch(harmony, typeof(WildlifeKnowledgeRewardPatch), "Wildlife max-knowledge reward");
        TryPatch(harmony, typeof(ExpeditionDurationPatch), "Expedition duration");
        TryPatch(harmony, typeof(ExpeditionSurveyPatch), "Expedition survey XP");
        TryPatch(harmony, typeof(ExpeditionGeneratedQualityPatch), "Expedition reward quality");
        TryPatch(harmony, typeof(ExpeditionDisplayedMaxQualityPatch), "Expedition max quality");
        TryPatch(harmony, typeof(FirstAidInteractionPatch), "First-aid expedition cure");
        TryPatch(harmony, typeof(FirstAidStatusTypeAliasPatch), "First-aid illness type alias");
        TryPatch(harmony, typeof(StaffMovementRegisterPatch), "Staff movement agent register");
        TryPatch(harmony, typeof(StaffMovementUnregisterPatch), "Staff movement agent unregister");
        TryPatch(harmony, typeof(StaffMovementFinalSpeedPatch), "Staff movement final speed");

        Log.LogInfo("Staff movement multiplies final staff nav-agent speed while preserving vanilla energy, fatigue and qualification modifiers.");
        Log.LogInfo("Training uses safe definition-rate runtime; ECS training hook remains disabled.");
        Log.LogInfo("Analysis/wildlife speed use direct manager-config timing arrays; ECS hooks remain disabled.");

        Log.LogInfo("External trainer/config UI supported through config hot reload.");

        Log.LogInfo($"TPM QoL loaded - v{MyPluginInfo.PLUGIN_VERSION}");
        LogCurrentSettings("startup");
    }

    internal static void LogCurrentSettings(string reason)
    {
        Log.LogInfo(
            $"Settings ({reason}): workshop={WorkshopSpeedMultiplier.Value:0.##}x, " +
            $"training={TrainingXpMultiplier.Value:0.##}x, analysis={AnalysisSpeedMultiplier.Value:0.##}x, " +
            $"wildlife={WildlifeSpeedMultiplier.Value:0.##}x, " +
            $"expeditions={ExpeditionSpeedMultiplier.Value:0.##}x, staff movement={StaffMovementMultiplier.Value:0.##}x, " +
            $"exhibit extras={ExhibitExtraInstallSpeedMultiplier.Value:0.##}x, security radius={SecurityCoverageRadius.Value:0.##}");
        Log.LogInfo(
            $"Applicants: rank={ApplicantMinimumRank.Value}, empty skills={ApplicantEmptySkills.Value}; " +
            $"Knowledge: analysis-max={AnalysisMaxKnowledgeAfterOne.Value}, wildlife-max={WildlifeMaxKnowledgeAfterOne.Value}; " +
            $"Expeditions: survey={ExpeditionSurveyXpMultiplier.Value:0.##}x, max-after-one={ExpeditionMaxSurveyAfterOne.Value}, " +
            $"quality={ExpeditionQuality.Value}, force-quality={ExpeditionForceQuality.Value}");
    }

    private ConfigEntry<float> BindSpeed(string section, string key, float defaultValue, string description, float max)
    {
        return Config.Bind(section, key, defaultValue,
            new ConfigDescription(description, new AcceptableValueRange<float>(1.0f, max)));
    }

    private static void TryPatch(Harmony harmony, Type patchType, string name)
    {
        try
        {
            harmony.CreateClassProcessor(patchType).Patch();
            Log.LogInfo($"{name} patch installed.");
        }
        catch (Exception ex)
        {
            Log.LogError($"{name} patch failed: {ex}");
        }
    }
}
