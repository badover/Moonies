using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Shared-role draw: five procedures (2 easy, 2 medium, 1 hard) plus 2–3 globals.</summary>
public static class MissionRunPlan
{
    public const float RunSeconds = 600;
    public static readonly float[] PhaseStarts = { 0, 80, 135, 230, 295, 345, 410, 470 };
    public static readonly string[] PhaseNames = {
        "LEO CHECKOUT · 185 KM PARKING ORBIT", "TLI / EARTH DEPARTURE", "DEEP-SPACE TRANSIT",
        "LUNAR ORBIT / SEPARATION", "LUNAR DESCENT", "LUNAR SURFACE OPERATIONS",
        "LUNAR ASCENT / RENDEZVOUS", "EARTH RETURN / RECOVERY"
    };
    public sealed class Entry
    {
        public ShipMissionDefinition definition;
        public float trigger;
    }
    public static int PhaseAt(float time)
    {
        for (int i = PhaseStarts.Length - 1; i >= 0; i--) if (time >= PhaseStarts[i]) return i;
        return 0;
    }
    public static Entry[] Create(IEnumerable<ShipMissionDefinition> catalog, int seed)
    {
        var random = new Random(seed);
        var all = catalog.Where(x => x != null && !x.startsQuestionCampaign).ToArray();
        var common = all.Where(x => !x.globalEvent).ToArray();
        if (all.Select(x => x.missionId).Distinct().Count() != all.Length)
            throw new InvalidOperationException("Duplicate mission IDs in the catalog.");
        for (int attempt = 0; attempt < 4096; attempt++)
        {
            var selected = new List<ShipMissionDefinition>();
            foreach (var group in new[] { ("Easy", 2), ("Medium", 2), ("Hard", 1) })
            {
                var pool = common.Where(x => x.difficulty == group.Item1).ToList();
                if (pool.Count < group.Item2) throw new InvalidOperationException("Incomplete mission difficulty pool.");
                for (int i = 0; i < group.Item2; i++) { int index = random.Next(pool.Count); selected.Add(pool[index]); pool.RemoveAt(index); }
            }
            if (selected.Select(x => PhaseAt(x.availableFromSeconds)).Distinct().Count() < 3) continue;
            var globals = all.Where(x => x.globalEvent).ToList();
            int count = random.Next(2, 4);
            if (globals.Count < count) throw new InvalidOperationException("Incomplete global event pool.");
            for (int i = 0; i < count; i++) { int index = random.Next(globals.Count); selected.Add(globals[index]); globals.RemoveAt(index); }
            var entries = selected.Select(x => new Entry { definition = x, trigger = x.availableFromSeconds +
                (float)random.NextDouble() * (Math.Min(x.availableUntilSeconds, RunSeconds - EstimatedWork(x)) - x.availableFromSeconds) }).OrderBy(x => x.trigger).ToArray();
            // Reserve procedure verification time, decision time and transitions. Late return tasks remain feasible.
            float cursor = 0;
            foreach (var entry in entries) cursor = Math.Max(cursor, entry.trigger) + EstimatedWork(entry.definition);
            if (cursor <= RunSeconds && entries.Where(x=>!x.definition.globalEvent).Select(x=>PhaseAt(x.trigger)).Distinct().Count() >= 3 && entries.All(x => x.trigger >= x.definition.availableFromSeconds && x.trigger <= x.definition.availableUntilSeconds)) return entries;
        }
        throw new InvalidOperationException("Could not generate a feasible ten-minute mission plan.");
    }
    public static float EstimatedWork(ShipMissionDefinition definition)
    {
        // Four seconds per decision includes reading/acting; the shelter also reserves a central-ladder transition.
        return definition.steps.Sum(x => x.automatic ? x.durationSeconds : 4) + (definition.missionId == "G-02" ? 12 : 4);
    }
}
