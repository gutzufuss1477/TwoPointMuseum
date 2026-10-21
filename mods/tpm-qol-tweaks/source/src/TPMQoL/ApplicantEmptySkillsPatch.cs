using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class ApplicantEmptySkillsRuntime
{
    private static readonly HashSet<long> SeenApplicants = new();
    private static IntPtr _lastManager;

    internal static void EnsureApplied()
    {
        var manager = JobApplicantManager.Instance;
        if (manager == null)
            return;

        if (_lastManager != manager.Pointer)
        {
            _lastManager = manager.Pointer;
            SeenApplicants.Clear();
        }

        var pools = manager.Pools;
        if (pools == null)
            return;

        for (var i = 0; i < pools.Count; i++)
        {
            var pool = pools[i];
            var applicants = pool?.Applicants;
            if (applicants == null)
                continue;

            for (var j = 0; j < applicants.Count; j++)
            {
                var applicant = applicants[j];
                if (applicant == null)
                    continue;

                var key = applicant.Pointer.ToInt64();
                if (key == 0 || !SeenApplicants.Add(key))
                    continue;

                if (!Plugin.ApplicantEmptySkills.Value)
                    continue;

                var qualifications = applicant.Qualifications;
                var list = qualifications?._qualifications;
                if (list == null || list.Count == 0)
                    continue;

                var beforeCount = list.Count;
                var rank = applicant.Rank;
                var beforeSlots = qualifications.NumSlots;
                var baseSkill = list[0];
                var beforeLevel = baseSkill?.XP?.Level ?? 0;

                // Preserve a genuine vanilla "no skill" applicant by doing nothing
                // when the list is empty. If a base skill exists, keep exactly that
                // one skill and normalise it to level 1 so higher rank only unlocks
                // free training slots instead of pre-filling them.
                for (var index = list.Count - 1; index >= 1; index--)
                    list.RemoveAt(index);

                if (baseSkill != null && beforeLevel > 1)
                    baseSkill.SetLevel(1);

                qualifications.CalculateNumSlots();

                var removed = beforeCount - list.Count;
                var afterLevel = baseSkill?.XP?.Level ?? 0;

                Plugin.Log.LogInfo(
                    $"Applicant skills normalised by polling: keptBase={(list.Count > 0 ? 1 : 0)}, " +
                    $"baseLevel={beforeLevel}->{afterLevel}, removedExtra={removed}, " +
                    $"rank={rank}, slots={beforeSlots}->{qualifications.NumSlots}");
            }
        }
    }
}
