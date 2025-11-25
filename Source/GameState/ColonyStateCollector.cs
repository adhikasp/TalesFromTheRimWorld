using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Collects colony state information for LLM context.
    /// Gathers pawns, resources, events, and environment data.
    /// </summary>
    public static class ColonyStateCollector
    {
        /// <summary>
        /// Get a snapshot of the current colony state.
        /// </summary>
        public static ColonySnapshot GetSnapshot()
        {
            Map map = Find.CurrentMap;
            if (map == null) return new ColonySnapshot();
            
            return new ColonySnapshot
            {
                ColonyName = GetColonyName(map),
                ColonyAgeDays = GenDate.DaysPassed,
                Colonists = GetColonistSummaries(map),
                RecentEvents = StoryContext.Instance?.RecentEvents?.ToList() ?? new List<string>(),
                Season = GenLocalDate.Season(map).Label(),
                Biome = map.Biome?.label ?? "unknown",
                Quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile).x).Label(),
                Year = GenLocalDate.Year(map),
                WealthTotal = (int)map.wealthWatcher.WealthTotal,
                ThreatPoints = StorytellerUtility.DefaultThreatPointsNow(map),
                ColonistCount = map.mapPawns.FreeColonistsCount,
                PrisonerCount = map.mapPawns.PrisonersOfColonyCount
            };
        }
        
        /// <summary>
        /// Get detailed context for event narration.
        /// </summary>
        public static string GetNarrationContext(IncidentParms parms = null, IncidentDef incidentDef = null)
        {
            var snapshot = GetSnapshot();
            var sb = new StringBuilder();
            
            sb.AppendLine($"Colony: {snapshot.ColonyName}");
            sb.AppendLine($"Day {snapshot.ColonyAgeDays} - {snapshot.Quadrum}, Year {snapshot.Year}");
            sb.AppendLine($"Season: {snapshot.Season} | Biome: {snapshot.Biome}");
            sb.AppendLine($"Population: {snapshot.ColonistCount} colonists, {snapshot.PrisonerCount} prisoners");
            sb.AppendLine();
            
            // Colonist details
            sb.AppendLine("Key Colonists:");
            foreach (var colonist in snapshot.Colonists.Take(5))
            {
                sb.AppendLine($"- {colonist}");
            }
            sb.AppendLine();
            
            // Recent events for continuity
            if (snapshot.RecentEvents.Any())
            {
                sb.AppendLine("Recent Events:");
                foreach (var evt in snapshot.RecentEvents.Take(3))
                {
                    sb.AppendLine($"- {evt}");
                }
                sb.AppendLine();
            }
            
            // Current event details if provided
            if (incidentDef != null)
            {
                sb.AppendLine($"Current Event: {incidentDef.label}");
                if (parms != null)
                {
                    if (parms.faction != null)
                    {
                        sb.AppendLine($"Faction: {parms.faction.Name}");
                    }
                    if (parms.points > 0)
                    {
                        string severity = parms.points < 300 ? "minor" : 
                                         parms.points < 800 ? "moderate" : "major";
                        sb.AppendLine($"Threat Level: {severity}");
                    }
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get context specifically for choice events.
        /// </summary>
        public static string GetChoiceContext()
        {
            var snapshot = GetSnapshot();
            var sb = new StringBuilder();
            
            sb.AppendLine($"Colony: {snapshot.ColonyName}");
            sb.AppendLine($"Day {snapshot.ColonyAgeDays}, {snapshot.Season}, {snapshot.Biome}");
            sb.AppendLine($"Colonists: {snapshot.ColonistCount}");
            sb.AppendLine();
            
            // More detailed colonist info for choices
            sb.AppendLine("Colonists:");
            foreach (var colonist in snapshot.Colonists)
            {
                sb.AppendLine($"- {colonist}");
            }
            sb.AppendLine();
            
            // Resource state (simplified)
            Map map = Find.CurrentMap;
            if (map != null)
            {
                sb.AppendLine("Resources:");
                sb.AppendLine($"- Wealth: {snapshot.WealthTotal:N0} silver equivalent");
                sb.AppendLine($"- Food: {GetFoodStatus(map)}");
                sb.AppendLine($"- Medicine: {GetMedicineCount(map)} units");
            }
            
            return sb.ToString();
        }
        
        private static string GetColonyName(Map map)
        {
            if (map?.Parent is Settlement settlement)
            {
                return settlement.Label;
            }
            
            var faction = Faction.OfPlayer;
            return faction?.Name ?? "The Colony";
        }
        
        private static List<string> GetColonistSummaries(Map map)
        {
            var colonists = new List<string>();
            
            foreach (var pawn in map.mapPawns.FreeColonists.Take(8))
            {
                string role = GetPawnRole(pawn);
                string traits = GetNotableTraits(pawn);
                string health = GetHealthStatus(pawn);
                
                string summary = $"{pawn.Name.ToStringShort} ({role})";
                if (!string.IsNullOrEmpty(traits))
                    summary += $" - {traits}";
                if (!string.IsNullOrEmpty(health))
                    summary += $" [{health}]";
                    
                colonists.Add(summary);
            }
            
            return colonists;
        }
        
        private static string GetPawnRole(Pawn pawn)
        {
            // Determine primary role based on highest passion/skill
            var skills = pawn.skills?.skills;
            if (skills == null) return "colonist";
            
            var bestSkill = skills
                .Where(s => s.passion != Passion.None || s.Level >= 8)
                .OrderByDescending(s => s.Level + (s.passion == Passion.Major ? 3 : s.passion == Passion.Minor ? 1 : 0))
                .FirstOrDefault();
            
            if (bestSkill != null && bestSkill.Level >= 5)
            {
                return GetRoleFromSkill(bestSkill.def);
            }
            
            return "colonist";
        }
        
        private static string GetRoleFromSkill(SkillDef skill)
        {
            if (skill == SkillDefOf.Medicine) return "medic";
            if (skill == SkillDefOf.Shooting) return "shooter";
            if (skill == SkillDefOf.Melee) return "fighter";
            if (skill == SkillDefOf.Construction) return "builder";
            if (skill == SkillDefOf.Cooking) return "cook";
            if (skill == SkillDefOf.Plants) return "farmer";
            if (skill == SkillDefOf.Mining) return "miner";
            if (skill == SkillDefOf.Crafting) return "crafter";
            if (skill == SkillDefOf.Animals) return "handler";
            if (skill == SkillDefOf.Artistic) return "artist";
            if (skill == SkillDefOf.Social) return "negotiator";
            if (skill == SkillDefOf.Intellectual) return "researcher";
            return "colonist";
        }
        
        private static string GetNotableTraits(Pawn pawn)
        {
            var traits = pawn.story?.traits?.allTraits;
            if (traits == null || traits.Count == 0) return "";
            
            // Pick most notable trait
            var notableTraits = new[] { "Psychopath", "Cannibal", "Bloodlust", "Kind", "Tough", 
                "Brawler", "Neurotic", "Iron-willed", "Pyromaniac", "Ascetic" };
            
            var notable = traits.FirstOrDefault(t => notableTraits.Contains(t.def.defName));
            return notable?.LabelCap ?? "";
        }
        
        private static string GetHealthStatus(Pawn pawn)
        {
            if (pawn.health?.State == PawnHealthState.Dead) return "dead";
            if (pawn.health?.Downed == true) return "incapacitated";
            
            var summaryHealth = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
            if (summaryHealth < 0.5f) return "severely injured";
            if (summaryHealth < 0.8f) return "injured";
            
            return "";
        }
        
        private static string GetFoodStatus(Map map)
        {
            float days = map.resourceCounter.TotalHumanEdibleNutrition / 
                        (map.mapPawns.FreeColonistsCount * 1.6f + 0.1f);
            
            if (days < 1) return "critical";
            if (days < 3) return "low";
            if (days < 10) return "adequate";
            return "abundant";
        }
        
        private static int GetMedicineCount(Map map)
        {
            int count = 0;
            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine))
            {
                count += thing.stackCount;
            }
            return count;
        }
    }
    
    /// <summary>
    /// Data snapshot of colony state for LLM context.
    /// </summary>
    public class ColonySnapshot
    {
        public string ColonyName { get; set; } = "Unknown Colony";
        public int ColonyAgeDays { get; set; }
        public List<string> Colonists { get; set; } = new List<string>();
        public List<string> RecentEvents { get; set; } = new List<string>();
        public string Season { get; set; } = "unknown";
        public string Biome { get; set; } = "unknown";
        public string Quadrum { get; set; } = "unknown";
        public int Year { get; set; } = 5500;
        public int WealthTotal { get; set; }
        public float ThreatPoints { get; set; }
        public int ColonistCount { get; set; }
        public int PrisonerCount { get; set; }
    }
}

