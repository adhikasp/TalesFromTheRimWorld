using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AINarrator
{
    /// <summary>
    /// Shared context formatting logic for LLM prompts.
    /// Used by both production code (ColonyStateCollector) and test code (TestPromptBuilder).
    /// </summary>
    public static class ContextFormatter
    {
        /// <summary>
        /// TakeLast helper for .NET Framework 4.7.2 compatibility.
        /// </summary>
        private static IEnumerable<T> TakeLast<T>(IReadOnlyList<T> list, int count)
        {
            if (list == null || count <= 0) return Enumerable.Empty<T>();
            int skip = Math.Max(0, list.Count - count);
            return list.Skip(skip);
        }
        
        /// <summary>
        /// Format colony context for event narration.
        /// </summary>
        public static string FormatNarrationContext(IColonySnapshot snapshot, IEventInfo currentEvent = null)
        {
            var sb = new StringBuilder();
            
            // Colony header
            sb.AppendLine($"=== COLONY: {snapshot.ColonyName} ===");
            sb.AppendLine($"Day {snapshot.ColonyAgeDays} - {snapshot.Quadrum}, Year {snapshot.Year}");
            sb.AppendLine($"Season: {snapshot.Season} | Biome: {snapshot.Biome}");
            sb.AppendLine($"Population: {snapshot.ColonistCount} colonists, {snapshot.PrisonerCount} prisoners");
            if (!string.IsNullOrWhiteSpace(snapshot.ScenarioName))
            {
                sb.AppendLine($"Scenario: {snapshot.ScenarioName}");
            }
            sb.AppendLine();
            
            // Environment
            if (snapshot.Environment != null)
            {
                sb.AppendLine("=== ENVIRONMENT ===");
                sb.AppendLine($"Time: {snapshot.Environment.TimeOfDay} | Weather: {snapshot.Environment.Weather}");
                sb.AppendLine($"Temperature: {snapshot.Environment.Temperature}");
                if (snapshot.Environment.ActiveConditions.Any())
                {
                    sb.AppendLine($"Conditions: {string.Join(", ", snapshot.Environment.ActiveConditions)}");
                }
                sb.AppendLine();
            }
            
            // Colonist details (top 6 for narration)
            sb.AppendLine("=== COLONISTS ===");
            foreach (var colonist in snapshot.ColonistDetails.Take(6))
            {
                sb.AppendLine(FormatColonistDetail(colonist));
            }
            if (snapshot.ColonistDetails.Count > 6)
            {
                sb.AppendLine($"...and {snapshot.ColonistDetails.Count - 6} more colonists");
            }
            sb.AppendLine();
            
            // Rooms & comfort
            if (snapshot.RoomSummary != null)
            {
                var rooms = snapshot.RoomSummary;
                int totalRooms = rooms.PrivateBedrooms + rooms.Barracks + rooms.DiningRooms + rooms.Kitchens + rooms.RecreationRooms + rooms.Hospitals + rooms.PrisonCells;
                bool hasHighlights = rooms.Highlights != null && rooms.Highlights.Any();
                if (totalRooms > 0 || hasHighlights)
                {
                    sb.AppendLine("=== ROOMS & COMFORT ===");
                    sb.AppendLine($"- Private Bedrooms: {rooms.PrivateBedrooms}");
                    sb.AppendLine($"- Barracks: {rooms.Barracks}");
                    sb.AppendLine($"- Dining Rooms: {rooms.DiningRooms}");
                    sb.AppendLine($"- Kitchens: {rooms.Kitchens}");
                    sb.AppendLine($"- Recreation Rooms: {rooms.RecreationRooms}");
                    sb.AppendLine($"- Hospitals: {rooms.Hospitals}");
                    sb.AppendLine($"- Prison Cells: {rooms.PrisonCells}");
                    if (hasHighlights)
                    {
                        foreach (var highlight in rooms.Highlights.Take(3))
                        {
                            sb.AppendLine($"  • {highlight}");
                        }
                    }
                    sb.AppendLine();
                }
            }
            
            // Recent interactions
            if (snapshot.RecentInteractions.Any())
            {
                sb.AppendLine("=== RECENT SOCIAL INTERACTIONS ===");
                foreach (var interaction in snapshot.RecentInteractions.Take(5))
                {
                    sb.AppendLine($"- {interaction}");
                }
                sb.AppendLine();
            }
            
            // Active threats
            if (snapshot.ActiveThreats.Any())
            {
                sb.AppendLine("=== ACTIVE THREATS ===");
                foreach (var threat in snapshot.ActiveThreats)
                {
                    sb.AppendLine($"- {threat}");
                }
                sb.AppendLine();
            }
            
            // Phase 2: Relevant history for long-form callbacks
            if (snapshot.RelevantHistory != null && snapshot.RelevantHistory.Any())
            {
                sb.AppendLine("=== RELEVANT HISTORY ===");
                foreach (var evt in snapshot.RelevantHistory
                             .OrderByDescending(e => e.Significance)
                             .Take(5))
                {
                    sb.AppendLine($"- {evt.DateString}: {evt.Summary}");
                }
                sb.AppendLine();
            }
            
            // Phase 2: Nemesis callouts (exclude retired rivals)
            if (snapshot.ActiveNemeses != null && snapshot.ActiveNemeses.Any(n => !n.IsRetired))
            {
                sb.AppendLine("=== ACTIVE NEMESES ===");
                foreach (var nemesis in snapshot.ActiveNemeses.Where(n => !n.IsRetired).Take(5))
                {
                    sb.Append($"{nemesis.Name} ({nemesis.FactionName}) - {nemesis.GrudgeReason}");
                    if (!string.IsNullOrEmpty(nemesis.GrudgeTarget))
                    {
                        sb.Append($" Target: {nemesis.GrudgeTarget}.");
                    }
                    if (nemesis.EncounterCount > 0)
                    {
                        sb.Append($" Encounters: {nemesis.EncounterCount}");
                    }
                    if (nemesis.LastSeenDay > 0)
                    {
                        sb.Append($" | Last seen Day {nemesis.LastSeenDay}");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            
            // Phase 2: Colony legends/myths
            if (snapshot.Legends != null && snapshot.Legends.Any(l => !l.IsDestroyed && !string.IsNullOrEmpty(l.MythicSummary)))
            {
                sb.AppendLine("=== COLONY LEGENDS ===");
                foreach (var legend in snapshot.Legends
                             .Where(l => !l.IsDestroyed && !string.IsNullOrEmpty(l.MythicSummary))
                             .Take(3))
                {
                    sb.AppendLine($"{legend.ArtworkLabel} by {legend.CreatorName} ({legend.CreatedDateString}): {legend.MythicSummary}");
                }
                sb.AppendLine();
            }
            
            // Faction relations (most relevant ones)
            var relevantFactions = snapshot.FactionRelations
                .Where(f => f.Goodwill != 0 || f.IsHostile)
                .OrderByDescending(f => Math.Abs(f.Goodwill))
                .Take(5)
                .ToList();
            if (relevantFactions.Any())
            {
                sb.AppendLine("=== FACTION RELATIONS ===");
                foreach (var faction in relevantFactions)
                {
                    sb.AppendLine($"- {FormatFactionRelation(faction)}");
                }
                sb.AppendLine();
            }
            
            // Death records for narrative callbacks
            if (snapshot.DeathRecords.Any())
            {
                sb.AppendLine("=== FALLEN COLONISTS ===");
                foreach (var death in TakeLast(snapshot.DeathRecords, 5))
                {
                    sb.AppendLine($"- {death}");
                }
                sb.AppendLine();
            }
            
            // Recent events for continuity
            if (snapshot.RecentEvents.Any())
            {
                sb.AppendLine("=== RECENT EVENTS ===");
                foreach (var evt in snapshot.RecentEvents.Take(5))
                {
                    sb.AppendLine($"- {evt}");
                }
                sb.AppendLine();
            }
            
            // Current event details if provided
            if (currentEvent != null)
            {
                sb.AppendLine("=== CURRENT EVENT ===");
                sb.AppendLine($"Event: {currentEvent.Label}");
                sb.AppendLine($"Category: {currentEvent.Category}");
                if (!string.IsNullOrEmpty(currentEvent.FactionName))
                {
                    sb.AppendLine($"Faction: {currentEvent.FactionName}");
                    var factionInfo = snapshot.FactionRelations.FirstOrDefault(f => f.Name == currentEvent.FactionName);
                    if (factionInfo != null)
                    {
                        sb.AppendLine($"Relation: {factionInfo.RelationType} (Goodwill: {factionInfo.Goodwill})");
                    }
                }
                if (!string.IsNullOrEmpty(currentEvent.ThreatLevel))
                {
                    sb.AppendLine($"Threat Level: {currentEvent.ThreatLevel}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format colony context for choice events.
        /// </summary>
        public static string FormatChoiceContext(IColonySnapshot snapshot)
        {
            var sb = new StringBuilder();
            
            // Colony header
            sb.AppendLine($"=== COLONY: {snapshot.ColonyName} ===");
            sb.AppendLine($"Day {snapshot.ColonyAgeDays}, {snapshot.Season}, {snapshot.Biome}");
            sb.AppendLine($"Population: {snapshot.ColonistCount} colonists");
            if (!string.IsNullOrWhiteSpace(snapshot.ScenarioName))
            {
                sb.AppendLine($"Scenario: {snapshot.ScenarioName}");
            }
            sb.AppendLine();
            
            // Environment for atmosphere
            if (snapshot.Environment != null)
            {
                sb.AppendLine("=== ENVIRONMENT ===");
                sb.AppendLine($"Time: {snapshot.Environment.TimeOfDay} | Weather: {snapshot.Environment.Weather}");
                sb.AppendLine($"Temperature: {snapshot.Environment.Temperature}");
                if (snapshot.Environment.ActiveConditions.Any())
                {
                    sb.AppendLine($"Active Conditions: {string.Join(", ", snapshot.Environment.ActiveConditions)}");
                }
                sb.AppendLine();
            }
            
            // Full colonist details for choices
            sb.AppendLine("=== COLONISTS ===");
            foreach (var colonist in snapshot.ColonistDetails)
            {
                sb.AppendLine(FormatColonistFull(colonist));
            }
            sb.AppendLine();
            
            // Resources
            sb.AppendLine("=== RESOURCES ===");
            sb.AppendLine($"- Total Wealth: {snapshot.WealthTotal:N0} silver equivalent");
            foreach (var resource in snapshot.Resources)
            {
                sb.AppendLine($"- {resource.Key}: {resource.Value}");
            }
            sb.AppendLine();
            
            // Rooms & comfort
            if (snapshot.RoomSummary != null)
            {
                var rooms = snapshot.RoomSummary;
                int totalRooms = rooms.PrivateBedrooms + rooms.Barracks + rooms.DiningRooms + rooms.Kitchens + rooms.RecreationRooms + rooms.Hospitals + rooms.PrisonCells;
                bool hasHighlights = rooms.Highlights != null && rooms.Highlights.Any();
                if (totalRooms > 0 || hasHighlights)
                {
                    sb.AppendLine("=== ROOMS & COMFORT ===");
                    sb.AppendLine($"- Private Bedrooms: {rooms.PrivateBedrooms}");
                    sb.AppendLine($"- Barracks: {rooms.Barracks}");
                    sb.AppendLine($"- Dining Rooms: {rooms.DiningRooms}");
                    sb.AppendLine($"- Kitchens: {rooms.Kitchens}");
                    sb.AppendLine($"- Recreation Rooms: {rooms.RecreationRooms}");
                    sb.AppendLine($"- Hospitals: {rooms.Hospitals}");
                    sb.AppendLine($"- Prison Cells: {rooms.PrisonCells}");
                    if (hasHighlights)
                    {
                        foreach (var highlight in rooms.Highlights)
                        {
                            sb.AppendLine($"  • {highlight}");
                        }
                    }
                    sb.AppendLine();
                }
            }
            
            // Faction relations
            if (snapshot.FactionRelations.Any())
            {
                sb.AppendLine("=== FACTION RELATIONS ===");
                foreach (var faction in snapshot.FactionRelations.OrderByDescending(f => Math.Abs(f.Goodwill)).Take(8))
                {
                    sb.AppendLine($"- {FormatFactionRelation(faction)}");
                }
                sb.AppendLine();
            }
            
            // Prisoners for choices involving them
            if (snapshot.Prisoners.Any())
            {
                sb.AppendLine("=== PRISONERS ===");
                foreach (var prisoner in snapshot.Prisoners)
                {
                    sb.AppendLine($"- {FormatPrisoner(prisoner)}");
                }
                sb.AppendLine();
            }
            
            // Active threats
            if (snapshot.ActiveThreats.Any())
            {
                sb.AppendLine("=== ACTIVE THREATS ===");
                foreach (var threat in snapshot.ActiveThreats)
                {
                    sb.AppendLine($"- {threat}");
                }
                sb.AppendLine();
            }
            
            // Colony animals (bonded especially)
            if (snapshot.Animals.Any())
            {
                sb.AppendLine("=== COLONY ANIMALS ===");
                foreach (var animal in snapshot.Animals.Take(10))
                {
                    sb.AppendLine($"- {animal}");
                }
                sb.AppendLine();
            }
            
            // Phase 2: Relevant History
            if (snapshot.RelevantHistory != null && snapshot.RelevantHistory.Any())
            {
                sb.AppendLine("=== RELEVANT HISTORY ===");
                foreach (var evt in snapshot.RelevantHistory.OrderByDescending(e => e.Significance).Take(5))
                {
                    sb.AppendLine($"- {evt.DateString}: {evt.Summary}");
                }
                sb.AppendLine();
            }
            
            // Phase 2: Active Nemeses
            if (snapshot.ActiveNemeses != null && snapshot.ActiveNemeses.Any(n => !n.IsRetired))
            {
                sb.AppendLine("=== ACTIVE NEMESES ===");
                foreach (var nemesis in snapshot.ActiveNemeses.Where(n => !n.IsRetired))
                {
                    sb.Append($"{nemesis.Name} ({nemesis.FactionName}) - {nemesis.GrudgeReason}");
                    if (!string.IsNullOrEmpty(nemesis.GrudgeTarget))
                    {
                        sb.Append($" Holds a grudge against {nemesis.GrudgeTarget}.");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            
            // Phase 2: Colony Legends
            if (snapshot.Legends != null && snapshot.Legends.Any(l => !l.IsDestroyed && !string.IsNullOrEmpty(l.MythicSummary)))
            {
                sb.AppendLine("=== COLONY LEGENDS ===");
                foreach (var legend in snapshot.Legends.Where(l => !l.IsDestroyed && !string.IsNullOrEmpty(l.MythicSummary)).Take(3))
                {
                    sb.AppendLine($"{legend.ArtworkLabel} by {legend.CreatorName} ({legend.CreatedDateString}): {legend.MythicSummary}");
                }
                sb.AppendLine();
            }
            
            // Notable items
            if (snapshot.NotableItems.Any())
            {
                sb.AppendLine("=== NOTABLE ITEMS ===");
                foreach (var item in snapshot.NotableItems)
                {
                    sb.AppendLine($"- {item}");
                }
                sb.AppendLine();
            }
            
            // Infrastructure
            if (snapshot.Infrastructure != null)
            {
                sb.AppendLine("=== INFRASTRUCTURE ===");
                sb.AppendLine($"- Hospital Beds: {snapshot.Infrastructure.HospitalBeds}");
                sb.AppendLine($"- Turrets: {snapshot.Infrastructure.Turrets}");
                sb.AppendLine($"- Mortars: {snapshot.Infrastructure.Mortars}");
                sb.AppendLine($"- Power Generation: {snapshot.Infrastructure.PowerGeneration} W");
                sb.AppendLine($"- Research: {snapshot.Infrastructure.ResearchCompleted}");
                sb.AppendLine();
            }
            
            // Death records for narrative callbacks
            if (snapshot.DeathRecords.Any())
            {
                sb.AppendLine("=== FALLEN COLONISTS ===");
                foreach (var death in snapshot.DeathRecords)
                {
                    sb.AppendLine($"- {death}");
                }
                sb.AppendLine();
            }
            
            // Battle history
            if (snapshot.BattleHistory.Any())
            {
                sb.AppendLine("=== BATTLE HISTORY ===");
                foreach (var battle in TakeLast(snapshot.BattleHistory, 5))
                {
                    sb.AppendLine($"- {battle}");
                }
                sb.AppendLine();
            }
            
            // Story timeline: recent narrations and choices (newest first)
            // Wrapped in try-catch because StoryContext requires RimWorld assemblies at runtime
            try
            {
                AppendStoryTimeline(sb);
            }
            catch (Exception)
            {
                // StoryContext not available (running in test environment without RimWorld)
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Append story timeline from StoryContext (requires RimWorld assemblies).
        /// Separated into its own method to allow graceful failure in test environments.
        /// </summary>
        private static void AppendStoryTimeline(StringBuilder sb)
        {
            var storyContext = StoryContext.Instance;
            if (storyContext?.JournalEntries != null && storyContext.JournalEntries.Any())
            {
                // Get narrations (Event) and choices (Choice), sorted by tick descending (newest first)
                var timelineEntries = storyContext.JournalEntries
                    .Where(e => e.EntryType == JournalEntryType.Event || e.EntryType == JournalEntryType.Choice)
                    .OrderByDescending(e => e.GameTick)
                    .Take(10)  // Limit to most recent 10 entries to manage token budget
                    .ToList();
                
                if (timelineEntries.Any())
                {
                    sb.AppendLine("=== RECENT STORY TIMELINE ===");
                    foreach (var entry in timelineEntries)
                    {
                        string prefix = entry.EntryType == JournalEntryType.Choice ? "★ CHOICE" : "● EVENT";
                        sb.AppendLine($"[{entry.DateString}] {prefix}:");
                        sb.AppendLine($"  {entry.Text}");
                        
                        if (entry.EntryType == JournalEntryType.Choice && !string.IsNullOrEmpty(entry.ChoiceMade))
                        {
                            sb.AppendLine($"  → Chose: {entry.ChoiceMade}");
                        }
                        
                        sb.AppendLine();
                    }
                }
            }
        }
        
        /// <summary>
        /// Format a colonist for detail view (narration context).
        /// </summary>
        public static string FormatColonistDetail(IColonistInfo colonist)
        {
            var sb = new StringBuilder();
            sb.Append($"• {colonist.Name} ({colonist.Age}y {colonist.Gender} {colonist.Role})");
            
            if (colonist.Traits.Any())
            {
                sb.Append($" - Traits: {string.Join(", ", colonist.Traits)}");
            }
            
            if (colonist.HealthStatus != "healthy")
            {
                sb.Append($" [Health: {colonist.HealthPercent}% - {colonist.HealthStatus}]");
            }
            
            if (colonist.MentalState != "stable")
            {
                sb.Append($" [Mood: {colonist.MoodPercent}% - {colonist.MentalState}]");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a colonist for full view (choice context).
        /// </summary>
        public static string FormatColonistFull(IColonistInfo colonist)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"• {colonist.Name} ({colonist.Age}y {colonist.Gender}, {colonist.Role})");
            
            if (!string.IsNullOrEmpty(colonist.ChildhoodBackstory) || !string.IsNullOrEmpty(colonist.AdulthoodBackstory))
            {
                sb.AppendLine($"  Background: {colonist.ChildhoodBackstory} / {colonist.AdulthoodBackstory}");
            }
            
            if (colonist.Traits.Any())
            {
                sb.AppendLine($"  Traits: {string.Join(", ", colonist.Traits)}");
            }
            
            if (colonist.TopSkills.Any())
            {
                sb.AppendLine($"  Skills: {string.Join(", ", colonist.TopSkills.Take(3))}");
            }
            
            sb.AppendLine($"  Health: {colonist.HealthPercent}% ({colonist.HealthStatus}) | Mood: {colonist.MoodPercent}% ({colonist.MentalState})");
            
            if (colonist.Relationships.Any())
            {
                sb.AppendLine($"  Relations: {string.Join("; ", colonist.Relationships.Take(3))}");
            }
            
            if (!string.IsNullOrEmpty(colonist.CurrentActivity) && colonist.CurrentActivity != "idle")
            {
                sb.AppendLine($"  Currently: {colonist.CurrentActivity}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a faction relation.
        /// </summary>
        public static string FormatFactionRelation(IFactionRelationInfo faction)
        {
            return $"{faction.Name} ({faction.FactionType}): {faction.RelationType} (Goodwill: {faction.Goodwill})";
        }
        
        /// <summary>
        /// Format a prisoner.
        /// </summary>
        public static string FormatPrisoner(IPrisonerInfo prisoner)
        {
            return $"{prisoner.Name} (from {prisoner.OriginalFaction}) - Health: {prisoner.HealthPercent}%, Recruitment resistance: {prisoner.RecruitDifficulty}";
        }
        
        /// <summary>
        /// Generate choice suggestions based on colony state.
        /// </summary>
        public static List<string> GetChoiceSuggestions(IColonySnapshot snapshot)
        {
            var suggestions = new List<string>();
            
            // Recent deaths
            if (snapshot.DeathRecords.Any())
            {
                suggestions.Add("A choice related to honoring the fallen or their unfinished business");
            }
            
            // Prisoners
            if (snapshot.Prisoners.Any())
            {
                var prisoner = snapshot.Prisoners.First();
                suggestions.Add($"A moral dilemma involving prisoner {prisoner.Name}");
            }
            
            // Relationships
            var colonistsWithRelations = snapshot.ColonistDetails.Where(c => c.Relationships.Any()).ToList();
            if (colonistsWithRelations.Any())
            {
                var colonist = colonistsWithRelations.First();
                var relation = colonist.Relationships.First();
                suggestions.Add($"A choice involving {colonist.ShortName}'s relationship ({relation})");
            }
            
            // Resource scarcity
            if (snapshot.Resources.TryGetValue("Food", out string food) && food.Contains("days"))
            {
                string daysStr = food.Split(' ')[0];
                if (float.TryParse(daysStr, out float days) && days < 5)
                {
                    suggestions.Add("A difficult choice about food or rationing");
                }
            }
            
            // Faction relations
            var hostileFactions = snapshot.FactionRelations.Where(f => f.IsHostile).ToList();
            var friendlyFactions = snapshot.FactionRelations.Where(f => f.Goodwill > 50).ToList();
            if (hostileFactions.Any())
            {
                suggestions.Add($"A choice involving the hostile {hostileFactions.First().Name}");
            }
            if (friendlyFactions.Any())
            {
                suggestions.Add($"An opportunity involving allied {friendlyFactions.First().Name}");
            }
            
            // Active threats
            if (snapshot.ActiveThreats.Any())
            {
                suggestions.Add($"A strategic choice regarding: {snapshot.ActiveThreats.First()}");
            }
            
            // Mental states
            var stressedColonists = snapshot.ColonistDetails.Where(c => c.MentalState != "stable").ToList();
            if (stressedColonists.Any())
            {
                suggestions.Add($"A choice involving {stressedColonists.First().ShortName} who is {stressedColonists.First().MentalState}");
            }
            
            // Default suggestions
            if (!suggestions.Any())
            {
                suggestions.Add("A stranger arrives with an unusual request");
                suggestions.Add("A moral dilemma about colony resources");
                suggestions.Add("An opportunity that may bring risk or reward");
            }
            
            return suggestions.Take(5).ToList();
        }
    }
}


