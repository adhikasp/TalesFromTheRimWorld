using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Builds system prompts and formats comprehensive context for LLM requests.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// System prompt for event narration.
        /// </summary>
        public static string GetNarrationSystemPrompt()
        {
            return @"You are The Narrator, an AI storyteller for a RimWorld colony. Your role is to provide atmospheric, immersive flavor text for game events.

Guidelines:
- Write 2-4 evocative sentences maximum
- Tone: Gritty, survivalist, sci-fi western. Dark but not hopeless. Smart witty and engaging.
- Reference colonist names and relationships when relevant
- Reference past events, deaths, and battles when they connect to current events
- Create atmosphere matching the biome, season, weather, and time of day
- Explain WHY the event is happening in story terms
- Never reveal mechanical game details beyond what the player will see
- Never break the fourth wall or mention it's a game
- Match the tone to the event (raids are threatening, gifts are hopeful, etc.)
- Use character traits, backstories, and relationships to add depth
- Reference recent social dynamics when relevant (romances, rivalries, friendships)

Response format: Just the narrative text, no formatting or prefixes.";
        }
        
        /// <summary>
        /// System prompt for choice events.
        /// </summary>
        public static string GetChoiceSystemPrompt()
        {
            return @"You are The Narrator, creating choice dilemmas for a RimWorld colony. Generate 3 DIFFERENT engaging scenarios, each with 2-3 meaningful choices.

### STYLE GUIDE
- Tone: Gritty, survivalist, sci-fi western. Dark but not hopeless. Smart witty and engaging.
- Content: Focus on sensory details (smell, sound, temperature) and colonist emotions.
- Constraints: Do NOT mention game mechanics (HP, stats, RNG). Do NOT break the fourth wall.

Response format (JSON array):
{
    ""Events"": [
        {
            ""NarrativeText"": ""2-4 sentences describing the situation"",
            ""Options"": [
                {
                    ""Label"": ""Short action description"",
                    ""HintText"": ""Brief hint at consequences"",
                    ""Consequences"": [
                        {
                            ""Type"": ""consequence_type"",
                            ""Parameters"": { }
                        }
                    ]
                }
            ]
        }
    ]
}

IMPORTANT: Generate exactly 3 different and varied scenarios in the Events array. Each should have a distinct theme (e.g., one moral dilemma, one opportunity, one threat-related choice). One will be randomly selected.

Available consequence types:
- ""spawn_pawn"": Add a colonist/refugee (Parameters: {""kind"": ""Colonist"" or ""Refugee""})
- ""spawn_items"": Drop resources (Parameters: {""item"": ""Silver/Gold/Steel/Plasteel/Component/Medicine/Food/Wood/Uranium/Jade"", ""count"": 50-200})
- ""mood_effect"": Colony mood change (Parameters: {""type"": ""positive/negative"", ""severity"": 1-3})
- ""faction_relation"": Change faction relations (Parameters: {""change"": -20 to +20, ""faction"": ""optional faction name or defName""})
- ""trigger_raid"": Enemy attack (Parameters: {""severity"": ""small/medium/large""})
- ""weather_change"": Change weather (Parameters: {""weather"": ""clear/rain/fog/snow/blizzard""})
- ""give_inspiration"": Inspire a colonist (Parameters: {""type"": ""shooting/melee/craft/social/surgery/trade/random"", ""colonist"": ""optional name""})
- ""spawn_trader"": Spawn traders (Parameters: {""type"": ""caravan"" or ""orbital""})
- ""spawn_animal"": Spawn animals (Parameters: {""animal"": ""dog/cat/wolf/bear/muffalo/thrumbo/random"", ""behavior"": ""tame/manhunter"", ""count"": 1-5})
- ""heal_colonist"": Heal a colonist (Parameters: {""colonist"": ""optional name"", ""type"": ""injuries/all""})
- ""skill_xp"": Grant skill experience (Parameters: {""skill"": ""shooting/melee/construction/medicine/cooking/crafting/social/research/random"", ""amount"": 3000-10000, ""colonist"": ""optional name""})
- ""trigger_incident"": Trigger any RimWorld incident with optional specifics
  Base Parameters: {""incident"": ""type"", ""faction"": ""optional faction name"", ""points"": ""optional threat points""}
  
  RAID INCIDENTS:
  - ""raid"" / ""enemy_raid"": {""arrival"": ""edge/drop/tunnel/breach"", ""strategy"": ""attack/siege/kidnap""}
  - ""mech_cluster"": Mechanoid cluster lands
  
  ANIMAL INCIDENTS:
  - ""manhunter"": {""animal"": ""bear/wolf/boar/rat/squirrel/deer/elk/caribou/tortoise/megasloth/thrumbo"", ""count"": 1-20}
  - ""animal_herd"": {""animal"": ""muffalo/deer/elk/caribou/alpaca"", ""count"": 5-15}
  - ""self_tame"": {""animal"": ""any animal kind""} - Animal self-tames to colony
  - ""farm_animals_wander_in"": Domestic animals join
  - ""thrumbo_pass"": Thrumbos pass through
  
  RESOURCE INCIDENTS:
  - ""meteorite"": {""resource"": ""steel/silver/gold/plasteel/uranium/jade/marble/granite/slate/sandstone/limestone""}
  - ""ship_chunk"": Ship chunk with components
  - ""resource_pod"": {""resource"": ""silver/gold/steel/plasteel/medicine/component/food/chemfuel/neutroamine"", ""count"": 25-200}
  - ""cargo_pod"": Random valuable cargo
  
  WEATHER/ENVIRONMENT:
  - ""flashstorm"": Lightning strikes
  - ""tornado"": Tornado spawns
  - ""volcanic_winter"": Long cold period
  - ""toxic_fallout"": Toxic dust
  - ""cold_snap"": Temperature drops
  - ""heat_wave"": Temperature rises
  - ""eclipse"": Solar eclipse
  - ""aurora"": Aurora borealis
  - ""psychic_drone"": {""gender"": ""male/female""} - Mood penalty
  - ""psychic_soothe"": {""gender"": ""male/female""} - Mood bonus
  
  DISEASE INCIDENTS:
  - ""disease"": {""disease"": ""plague/flu/malaria/sleeping_sickness/gut_worms/muscle_parasites/fibrous_mechanites/sensory_mechanites"", ""target"": ""optional colonist name""}
  - ""animal_disease"": Disease hits animals
  
  VISITOR INCIDENTS:
  - ""visitor_group"": Friendly faction visits
  - ""trader_caravan"": {""trader_type"": ""bulk/combat/exotic""}
  - ""wanderer_joins"": Random pawn joins
  - ""refugee_chased"": Refugee with pursuers
  - ""refugee_pod"": {""faction"": ""optional - makes them from faction""}
  - ""prisoner_rescue"": Prisoner rescue opportunity
  
  QUEST-LIKE:
  - ""peace_talks"": Opportunity for peace with hostile faction
  - ""trade_request"": Faction requests specific goods
  - ""bandit_camp"": Opportunity to raid bandit camp nearby
  - ""item_stash"": {""item"": ""weapon/armor/artifact/medicine/drugs""} - Location revealed
  - ""caravan_request"": Faction needs caravan help
  - ""ancient_danger_revealed"": Ancient danger location
  
  MISC:
  - ""ambrosia_sprout"": Ambrosia plants grow
  - ""crop_blight"": Crops get blight
  - ""short_circuit"": Electrical fire
  - ""solar_flare"": Electronics disabled
  (Or use any RimWorld IncidentDef defName directly for full control)
- ""nothing"": No mechanical effect

Guidelines:
- Create morally interesting dilemmas relevant to colony survival
- Balance risk and reward across options
- A single option can have multiple Consequences that resolve in order
- Reference specific colonist names, traits, and relationships
- Tie choices to recent events, deaths, or social dynamics when possible
- Use the colony's history (past battles, fallen colonists) for emotional weight
- Make choices feel meaningful but not game-breaking
- Keep consequences immediate (no delayed effects)
- Consider current faction relations when choices involve outsiders
- Use prisoner situations, animal bonds, or resource scarcity for drama";
        }
        
        /// <summary>
        /// Build user prompt for event narration.
        /// </summary>
        public static string BuildEventPrompt(IncidentDef incident, IncidentParms parms)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("COLONY CONTEXT:");
            string context = ColonyStateCollector.GetNarrationContext(parms, incident);
            sb.AppendLine(context);
            
            // Phase 2: Add relevant history (only if not already in context from snapshot)
            // ContextFormatter includes Phase 2 sections from snapshot, so check if already present
            if (!context.Contains("=== RELEVANT HISTORY ==="))
            {
                var relevantHistory = GetRelevantHistorySection(incident, parms);
                if (!string.IsNullOrEmpty(relevantHistory))
                {
                    sb.AppendLine();
                    sb.AppendLine("RELEVANT HISTORY:");
                    sb.AppendLine(relevantHistory);
                }
            }
            
            // Phase 2: Add Nemesis info if attacking faction has one (only if not already in context)
            if (!context.Contains("=== ACTIVE NEMESES ==="))
            {
                var nemesisInfo = GetNemesisSection(parms?.faction);
                if (!string.IsNullOrEmpty(nemesisInfo))
                {
                    sb.AppendLine();
                    sb.AppendLine("RECURRING ENEMY:");
                    sb.AppendLine(nemesisInfo);
                }
            }
            
            // Phase 2: Add relevant Legends (only if not already in context)
            if (!context.Contains("=== COLONY LEGENDS ==="))
            {
                var legends = GetRelevantLegends(incident);
                if (!string.IsNullOrEmpty(legends))
                {
                    sb.AppendLine();
                    sb.AppendLine("COLONY LEGENDS:");
                    sb.AppendLine(legends);
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("TASK: Write atmospheric flavor text for this event. Make it feel like part of an unfolding story.");
            sb.AppendLine("- Reference specific colonists by name when relevant");
            sb.AppendLine("- Consider recent events and social dynamics");
            sb.AppendLine("- Match the atmosphere to current weather and time of day");
            sb.AppendLine("- If colonists have died recently, acknowledge the lingering grief when appropriate");
            sb.AppendLine("- Reference past events, Nemeses, or Legends when they connect to the current situation");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get relevant history section for prompt.
        /// Checks snapshot first (for tests), then falls back to StoryContext.
        /// </summary>
        private static string GetRelevantHistorySection(IncidentDef incident, IncidentParms parms)
        {
            // Check if snapshot has relevant history (for tests)
            try
            {
                var snapshot = ColonyStateCollector.GetSnapshot();
                if (snapshot?.RelevantHistory != null && snapshot.RelevantHistory.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var evt in snapshot.RelevantHistory.OrderByDescending(e => e.Significance).Take(5))
                    {
                        sb.AppendLine($"- {evt.DateString}: {evt.Summary}");
                    }
                    return sb.ToString().Trim();
                }
            }
            catch { /* Snapshot not available, fall through */ }
            
            // Fall back to StoryContext (production)
            if (StoryContext.Instance == null) return null;
            
            // Extract keywords from current event
            var keywords = new List<string>();
            if (incident != null)
            {
                keywords.Add(incident.label);
                if (parms?.faction != null)
                {
                    keywords.Add(parms.faction.Name);
                }
            }
            
            // Extract entity IDs (colonists involved)
            var entityIds = new List<string>();
            if (Find.CurrentMap != null)
            {
                var colonists = Find.CurrentMap.mapPawns?.FreeColonists;
                if (colonists != null)
                {
                    entityIds.AddRange(colonists.Select(c => c.ThingID));
                }
            }
            
            // Find relevant history
            var relevantEvents = HistorySearch.FindRelevantHistory(keywords, entityIds, maxResults: 5);
            
            if (relevantEvents == null || relevantEvents.Count == 0) return null;
            
            var sb2 = new StringBuilder();
            foreach (var evt in relevantEvents)
            {
                sb2.AppendLine($"- {evt.DateString}: {evt.Summary}");
            }
            
            return sb2.ToString().Trim();
        }
        
        /// <summary>
        /// Get Nemesis section for prompt.
        /// Checks snapshot first (for tests), then falls back to StoryContext.
        /// </summary>
        private static string GetNemesisSection(Faction faction)
        {
            if (faction == null) return null;
            
            // Check if snapshot has nemesis data (for tests)
            try
            {
                var snapshot = ColonyStateCollector.GetSnapshot();
                if (snapshot?.ActiveNemeses != null)
                {
                    var nemesis = snapshot.ActiveNemeses
                        .FirstOrDefault(n => !n.IsRetired && n.FactionName == faction.Name);
                    if (nemesis != null)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"{nemesis.Name} ({nemesis.FactionName}) - {nemesis.GrudgeReason}");
                        sb.Append($"This is encounter #{nemesis.EncounterCount}.");
                        if (!string.IsNullOrEmpty(nemesis.GrudgeTarget))
                        {
                            sb.Append($" Holds a grudge against {nemesis.GrudgeTarget}.");
                        }
                        return sb.ToString();
                    }
                }
            }
            catch { /* Snapshot not available, fall through */ }
            
            // Fall back to StoryContext (production)
            if (StoryContext.Instance == null) return null;
            
            var nemesis2 = StoryContext.Instance.GetActiveNemesisForFaction(faction);
            if (nemesis2 == null || nemesis2.IsRetired) return null;
            
            var sb2 = new StringBuilder();
            sb2.AppendLine($"{nemesis2.Name} ({nemesis2.FactionName}) - {nemesis2.GrudgeReason}");
            sb2.Append($"Last seen {GenDate.DaysPassed - nemesis2.LastSeenDay} days ago. ");
            sb2.Append($"This is encounter #{nemesis2.EncounterCount}.");
            
            if (!string.IsNullOrEmpty(nemesis2.GrudgeTarget))
            {
                sb2.Append($" Holds a grudge against {nemesis2.GrudgeTarget}.");
            }
            
            return sb2.ToString();
        }
        
        /// <summary>
        /// Get relevant Legends for prompt.
        /// Checks snapshot first (for tests), then falls back to StoryContext.
        /// </summary>
        private static string GetRelevantLegends(IncidentDef incident)
        {
            // Check if snapshot has legends (for tests)
            try
            {
                var snapshot = ColonyStateCollector.GetSnapshot();
                if (snapshot?.Legends != null)
                {
                    var legends = snapshot.Legends
                        .Where(l => !l.IsDestroyed && !string.IsNullOrEmpty(l.MythicSummary))
                        .Take(3)
                        .ToList();
                    
                    if (legends != null && legends.Any())
                    {
                        var sb = new StringBuilder();
                        foreach (var legend in legends)
                        {
                            sb.AppendLine($"{legend.ArtworkLabel} by {legend.CreatorName} ({legend.CreatedDateString}): {legend.MythicSummary}");
                        }
                        return sb.ToString().Trim();
                    }
                }
            }
            catch { /* Snapshot not available, fall through */ }
            
            // Fall back to StoryContext (production)
            if (StoryContext.Instance == null) return null;
            
            var legends2 = StoryContext.Instance.Legends
                .Where(l => !l.IsDestroyed && !string.IsNullOrEmpty(l.MythicSummary))
                .OrderByDescending(l => l.Quality)
                .ThenByDescending(l => l.CreatedDay)
                .Take(3)
                .ToList();
            
            if (legends2 == null || legends2.Count == 0) return null;
            
            var sb2 = new StringBuilder();
            foreach (var legend in legends2)
            {
                sb2.AppendLine($"{legend.ArtworkLabel} by {legend.CreatorName} ({legend.CreatedDateString}): {legend.MythicSummary}");
            }
            
            return sb2.ToString().Trim();
        }
        
        /// <summary>
        /// Build user prompt for choice events.
        /// </summary>
        public static string BuildChoicePrompt()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("COLONY CONTEXT:");
            sb.AppendLine(ColonyStateCollector.GetChoiceContext());
            sb.AppendLine();
            sb.AppendLine("TASK: Create a choice dilemma relevant to this colony's current situation. Output as JSON.");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get a fallback narrative when API fails.
        /// </summary>
        public static string GetFallbackNarrative(IncidentDef incident)
        {
            if (incident == null) return "Events unfold as fate decrees...";
            
            var snapshot = ColonyStateCollector.GetSnapshot();
            string colony = snapshot.ColonyName;
            string weather = snapshot.Environment?.Weather ?? "clear";
            string time = snapshot.Environment?.TimeOfDay ?? "day";
            
            // Category-based fallbacks with atmosphere
            if (incident.category == IncidentCategoryDefOf.ThreatBig ||
                incident.category == IncidentCategoryDefOf.ThreatSmall)
            {
                if (snapshot.DeathRecords.Any())
                {
                    var recentDeath = snapshot.DeathRecords.Last();
                    return $"Danger approaches {colony}. The colonists steel themselves, memories of {recentDeath.Split('(')[0].Trim()}'s sacrifice still fresh.";
                }
                return $"Under the {weather} {time} sky, danger approaches {colony}. The colonists ready themselves for what comes.";
            }
            
            if (incident.defName.Contains("Visitor") || incident.defName.Contains("Trade"))
            {
                return $"Visitors arrive at {colony}'s gates under the {weather} sky, their intentions yet unknown.";
            }
            
            if (incident.defName.Contains("Join") || incident.defName.Contains("Wanderer"))
            {
                return $"A stranger appears on the horizon, seeking shelter from the harsh world. {colony} may have a new soul.";
            }
            
            if (incident.defName.Contains("Eclipse") || incident.defName.Contains("Aurora") ||
                incident.defName.Contains("Solar") || incident.defName.Contains("Cold"))
            {
                return $"The skies above {colony} shift, heralding a change in fortune for the colony.";
            }
            
            if (incident.defName.Contains("Crop") || incident.defName.Contains("Blight"))
            {
                return $"The fields of {colony} whisper of troubles to come.";
            }
            
            if (incident.defName.Contains("Pod") || incident.defName.Contains("Ship"))
            {
                return $"Something falls from the sky above {colony}, a gift from the stars or a harbinger of doom.";
            }
            
            return $"The story of {colony} continues as {incident.label?.ToLower() ?? "events"} unfold...";
        }
        
        /// <summary>
        /// Get a fallback choice event when API fails.
        /// </summary>
        public static ChoiceEvent GetFallbackChoice()
        {
            var snapshot = ColonyStateCollector.GetSnapshot();
            string colonistName = snapshot.ColonistDetails.Count > 0 
                ? snapshot.ColonistDetails[0].ShortName 
                : "A colonist";
            
            // Try to make the fallback contextual
            if (snapshot.Prisoners.Any())
            {
                var prisoner = snapshot.Prisoners.First();
                return new ChoiceEvent
                {
                    NarrativeText = $"The prisoner {prisoner.Name} scratches a message into their cell wall. {colonistName} notices it reads: 'I know where supplies are hidden.' Do you investigate?",
                    Options = new List<ChoiceOption>
                    {
                        new ChoiceOption
                        {
                            Label = "Investigate the lead",
                            HintText = "Might find supplies, might be a trap",
                            Consequences = new List<ChoiceConsequence>
                            {
                                new ChoiceConsequence
                                {
                                    Type = "spawn_items",
                                    Parameters = new Dictionary<string, object>
                                    {
                                        { "item", "Silver" },
                                        { "count", 150 }
                                    }
                                }
                            }
                        },
                        new ChoiceOption
                        {
                            Label = "Ignore it",
                            HintText = "Safe, but opportunity lost",
                            Consequences = new List<ChoiceConsequence>
                            {
                                new ChoiceConsequence
                                {
                                    Type = "nothing",
                                    Parameters = new Dictionary<string, object>()
                                }
                            }
                        }
                    }
                };
            }
            
            return new ChoiceEvent
            {
                NarrativeText = $"A merchant passes by {snapshot.ColonyName}, offering a deal. {colonistName} could negotiate, but the merchant seems nervous, glancing at the horizon...",
                Options = new List<ChoiceOption>
                {
                    new ChoiceOption
                    {
                        Label = "Trade fairly",
                        HintText = "Gain some supplies, maintain good relations",
                        Consequences = new List<ChoiceConsequence>
                        {
                            new ChoiceConsequence
                            {
                                Type = "spawn_items",
                                Parameters = new Dictionary<string, object>
                                {
                                    { "item", "Silver" },
                                    { "count", 100 }
                                }
                            }
                        }
                    },
                    new ChoiceOption
                    {
                        Label = "Send them away",
                        HintText = "No risk, no reward",
                        Consequences = new List<ChoiceConsequence>
                        {
                            new ChoiceConsequence
                            {
                                Type = "nothing",
                                Parameters = new Dictionary<string, object>()
                            }
                        }
                    }
                }
            };
        }
        
        /// <summary>
        /// Generate a summary of an event for the journal.
        /// </summary>
        public static string GetEventSummary(IncidentDef incident, IncidentParms parms)
        {
            if (incident == null) return "An event occurred.";
            
            var sb = new StringBuilder();
            sb.Append(incident.label ?? "Event");
            
            if (parms?.faction != null)
            {
                sb.Append($" - {parms.faction.Name}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Build a context summary for debugging or logs.
        /// </summary>
        public static string BuildContextSummary()
        {
            var snapshot = ColonyStateCollector.GetSnapshot();
            var sb = new StringBuilder();
            
            sb.AppendLine($"=== Context Summary for {snapshot.ColonyName} ===");
            sb.AppendLine($"Day {snapshot.ColonyAgeDays}, {snapshot.Season}, {snapshot.Biome}");
            sb.AppendLine($"Population: {snapshot.ColonistCount} colonists, {snapshot.PrisonerCount} prisoners");
            sb.AppendLine($"Colonist details collected: {snapshot.ColonistDetails.Count}");
            sb.AppendLine($"Recent interactions: {snapshot.RecentInteractions.Count}");
            sb.AppendLine($"Recent activities: {snapshot.RecentActivities.Count}");
            sb.AppendLine($"Faction relations: {snapshot.FactionRelations.Count}");
            sb.AppendLine($"Death records: {snapshot.DeathRecords.Count}");
            sb.AppendLine($"Battle records: {snapshot.BattleHistory.Count}");
            sb.AppendLine($"Active threats: {snapshot.ActiveThreats.Count}");
            sb.AppendLine($"Notable items: {snapshot.NotableItems.Count}");
            
            return sb.ToString();
        }
    }
}
