using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class KnowledgeSpeedRuntime
{
    private sealed class AnalysisBaseline
    {
        internal long Pointer;
        internal int[] Times;
    }

    private sealed class VetBaseline
    {
        internal long Pointer;
        internal int[] HealTimes;
        internal int[] SpaTimes;
    }

    private static AnalysisBaseline _analysis;
    private static VetBaseline _vet;
    private static float _lastAnalysisMultiplier = float.NaN;
    private static float _lastVetMultiplier = float.NaN;

    internal static void EnsureApplied()
    {
        ApplyAnalysis();
        ApplyVet();
    }

    private static void ApplyAnalysis()
    {
        try
        {
            var manager = AnalysisRoomManager.Instance;
            var config = manager?.Config;
            var times = config?.DeconstructionTimeExhibitQuality;
            if (config == null || times == null || times.Length == 0)
                return;

            var ptr = config.Pointer.ToInt64();
            if (_analysis == null || _analysis.Pointer != ptr)
            {
                var vanilla = new int[times.Length];
                for (var i = 0; i < times.Length; i++)
                    vanilla[i] = times[i];

                _analysis = new AnalysisBaseline { Pointer = ptr, Times = vanilla };
                Plugin.Log.LogInfo(
                    $"Analysis base times discovered: [{string.Join(", ", vanilla)}]s");
                _lastAnalysisMultiplier = float.NaN;
            }

            var multiplier = Plugin.AnalysisSpeedMultiplier.Value;
            for (var i = 0; i < times.Length && i < _analysis.Times.Length; i++)
                times[i] = Math.Max(1, (int)Math.Ceiling(_analysis.Times[i] / multiplier));

            if (float.IsNaN(_lastAnalysisMultiplier) ||
                Math.Abs(_lastAnalysisMultiplier - multiplier) > 0.0001f)
            {
                _lastAnalysisMultiplier = multiplier;
                var applied = new int[times.Length];
                for (var i = 0; i < times.Length; i++)
                    applied[i] = times[i];
                Plugin.Log.LogInfo(
                    $"Analysis base times applied ({multiplier:0.##}x): [{string.Join(", ", applied)}]s");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Analysis config speed skipped: {ex.Message}");
        }
    }

    private static void ApplyVet()
    {
        try
        {
            var manager = VetRoomManager.Instance;
            var config = manager?.Config;
            var heal = config?.AnimalHealTimeExhibitQuality;
            var spa = config?.AnimalSpaTimeExhibitQuality;
            if (config == null || heal == null || spa == null ||
                heal.Length == 0 || spa.Length == 0)
                return;

            var ptr = config.Pointer.ToInt64();
            if (_vet == null || _vet.Pointer != ptr)
            {
                var vanillaHeal = new int[heal.Length];
                var vanillaSpa = new int[spa.Length];

                for (var i = 0; i < heal.Length; i++)
                    vanillaHeal[i] = heal[i];
                for (var i = 0; i < spa.Length; i++)
                    vanillaSpa[i] = spa[i];

                _vet = new VetBaseline
                {
                    Pointer = ptr,
                    HealTimes = vanillaHeal,
                    SpaTimes = vanillaSpa
                };

                Plugin.Log.LogInfo(
                    $"Vet base heal times discovered: [{string.Join(", ", vanillaHeal)}]s");
                Plugin.Log.LogInfo(
                    $"Vet base spa times discovered: [{string.Join(", ", vanillaSpa)}]s");
                _lastVetMultiplier = float.NaN;
            }

            var multiplier = Plugin.WildlifeSpeedMultiplier.Value;

            for (var i = 0; i < heal.Length && i < _vet.HealTimes.Length; i++)
                heal[i] = Math.Max(1, (int)Math.Ceiling(_vet.HealTimes[i] / multiplier));

            for (var i = 0; i < spa.Length && i < _vet.SpaTimes.Length; i++)
                spa[i] = Math.Max(1, (int)Math.Ceiling(_vet.SpaTimes[i] / multiplier));

            if (float.IsNaN(_lastVetMultiplier) ||
                Math.Abs(_lastVetMultiplier - multiplier) > 0.0001f)
            {
                _lastVetMultiplier = multiplier;

                var appliedHeal = new int[heal.Length];
                var appliedSpa = new int[spa.Length];
                for (var i = 0; i < heal.Length; i++) appliedHeal[i] = heal[i];
                for (var i = 0; i < spa.Length; i++) appliedSpa[i] = spa[i];

                Plugin.Log.LogInfo(
                    $"Vet heal times applied ({multiplier:0.##}x): [{string.Join(", ", appliedHeal)}]s");
                Plugin.Log.LogInfo(
                    $"Vet spa times applied ({multiplier:0.##}x): [{string.Join(", ", appliedSpa)}]s");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Vet config speed skipped: {ex.Message}");
        }
    }

    internal static int MaxKnowledgeInsightThreshold()
    {
        try
        {
            var ratings = ExhibitKnowledgeRatings.Instance;
            var config = ratings?._config;
            var levels = config?.KnowledgeInsightLevels;
            if (levels == null || levels.Length == 0)
                return 0;

            var max = 0;
            for (var i = 0; i < levels.Length; i++)
                if (levels[i] > max)
                    max = levels[i];

            return max;
        }
        catch
        {
            return 0;
        }
    }
}
