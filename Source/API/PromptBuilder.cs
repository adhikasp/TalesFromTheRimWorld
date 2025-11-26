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
- ""faction_relation"": Change faction relations (Parameters: {""change"": -20 to +20})
- ""trigger_raid"": Enemy attack (Parameters: {""severity"": ""small/medium/large""})
- ""weather_change"": Change weather (Parameters: {""weather"": ""clear/rain/fog/snow/blizzard""})
- ""give_inspiration"": Inspire a colonist (Parameters: {""type"": ""shooting/melee/craft/social/surgery/trade/random"", ""colonist"": ""optional name""})
- ""spawn_trader"": Spawn traders (Parameters: {""type"": ""caravan"" or ""orbital""})
- ""spawn_animal"": Spawn animals (Parameters: {""animal"": ""dog/cat/wolf/bear/muffalo/thrumbo/random"", ""behavior"": ""tame/manhunter"", ""count"": 1-5})
- ""heal_colonist"": Heal a colonist (Parameters: {""colonist"": ""optional name"", ""type"": ""injuries/all""})
- ""skill_xp"": Grant skill experience (Parameters: {""skill"": ""shooting/melee/construction/medicine/cooking/crafting/social/research/random"", ""amount"": 3000-10000, ""colonist"": ""optional name""})
- ""trigger_incident"": Trigger any RimWorld incident (Parameters: {""incident"": ""see list below"", ""faction"": ""optional faction name"", ""points"": ""optional threat points for raids""})
  Available incidents:
  - raid / enemy_raid - Enemy faction attack
  - manhunter_pack / manhunter - Animals go manhunter
  - infestation - Insect infestation
  - meteorite / meteor - Meteorite impact with resources
  - ship_chunk / shipchunk - Ship chunk falls from sky
  - resource_pod / crashedpod - Resource pod crash
  - tornado - Tornado event
  - flashstorm - Lightning storm
  - volcanic_winter - Volcanic winter begins
  - visitor_group / visitors - Friendly visitors arrive
  - trader_caravan / trader - Trade caravan arrives
  - wanderer_joins / wanderer - Wanderer joins colony
  - refugee_chased / refugee - Refugee being chased
  - traveler_wounded - Wounded traveler appears
  - quest / random_quest - Random quest event
  - disease / random_disease - Disease outbreak
  (Or use any RimWorld IncidentDef defName directly)
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
            sb.AppendLine(ColonyStateCollector.GetNarrationContext(parms, incident));
            sb.AppendLine();
            sb.AppendLine("TASK: Write atmospheric flavor text for this event. Make it feel like part of an unfolding story.");
            sb.AppendLine("- Reference specific colonists by name when relevant");
            sb.AppendLine("- Consider recent events and social dynamics");
            sb.AppendLine("- Match the atmosphere to current weather and time of day");
            sb.AppendLine("- If colonists have died recently, acknowledge the lingering grief when appropriate");
            
            return sb.ToString();
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
