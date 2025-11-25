using System.Text;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Builds system prompts and formats context for LLM requests.
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
- Use present tense and dramatic tone
- Reference colonist names and colony details when relevant
- Explain WHY the event is happening in story terms
- Create atmosphere matching the biome and season
- Never reveal mechanical game details beyond what the player will see
- Never break the fourth wall or mention it's a game
- Match the tone to the event (raids are threatening, gifts are hopeful, etc.)

Response format: Just the narrative text, no formatting or prefixes.";
        }
        
        /// <summary>
        /// System prompt for choice events.
        /// </summary>
        public static string GetChoiceSystemPrompt()
        {
            return @"You are The Narrator, creating a choice dilemma for a RimWorld colony. Generate an engaging scenario with 2-3 meaningful choices.

Response format (JSON):
{
    ""NarrativeText"": ""2-4 sentences describing the situation"",
    ""Options"": [
        {
            ""Label"": ""Short action description"",
            ""HintText"": ""Brief hint at consequences"",
            ""Consequence"": {
                ""Type"": ""consequence_type"",
                ""Parameters"": { }
            }
        }
    ]
}

Available consequence types:
- ""spawn_pawn"": Add a colonist/refugee (Parameters: {""kind"": ""Colonist"" or ""Refugee""})
- ""spawn_items"": Drop resources (Parameters: {""item"": ""Silver/Medicine/Steel/Component"", ""count"": 50-200})
- ""mood_effect"": Colony mood change (Parameters: {""type"": ""positive/negative"", ""severity"": 1-3})
- ""faction_relation"": Change faction relations (Parameters: {""change"": -20 to +20})
- ""trigger_raid"": Small attack (Parameters: {""severity"": ""small/medium""})
- ""nothing"": No mechanical effect

Guidelines:
- Create morally interesting dilemmas relevant to colony survival
- Balance risk and reward across options
- Reference colonist names and current situation
- Make choices feel meaningful but not game-breaking
- Keep consequences immediate (no delayed effects)";
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
            
            // Category-based fallbacks
            if (incident.category == IncidentCategoryDefOf.ThreatBig ||
                incident.category == IncidentCategoryDefOf.ThreatSmall)
            {
                return "Danger approaches the colony. The colonists ready themselves for what comes.";
            }
            
            if (incident.defName.Contains("Visitor") || incident.defName.Contains("Trade"))
            {
                return "Visitors arrive at the colony gates, their intentions yet unknown.";
            }
            
            if (incident.defName.Contains("Join") || incident.defName.Contains("Wanderer"))
            {
                return "A stranger appears on the horizon, seeking shelter from the harsh world.";
            }
            
            if (incident.defName.Contains("Eclipse") || incident.defName.Contains("Aurora") ||
                incident.defName.Contains("Solar") || incident.defName.Contains("Cold"))
            {
                return "The skies shift, heralding a change in fortune for the colony.";
            }
            
            if (incident.defName.Contains("Crop") || incident.defName.Contains("Blight"))
            {
                return "The fields whisper of troubles to come.";
            }
            
            if (incident.defName.Contains("Pod") || incident.defName.Contains("Ship"))
            {
                return "Something falls from the sky, a gift from the stars or a harbinger of doom.";
            }
            
            return $"The story continues as {incident.label?.ToLower() ?? "events"} unfold...";
        }
        
        /// <summary>
        /// Get a fallback choice event when API fails.
        /// </summary>
        public static ChoiceEvent GetFallbackChoice()
        {
            var snapshot = ColonyStateCollector.GetSnapshot();
            string colonistName = snapshot.Colonists.Count > 0 
                ? snapshot.Colonists[0].Split('(')[0].Trim() 
                : "A colonist";
            
            return new ChoiceEvent
            {
                NarrativeText = $"A merchant passes by the colony, offering a deal. {colonistName} could negotiate, but the merchant seems nervous, glancing at the horizon...",
                Options = new System.Collections.Generic.List<ChoiceOption>
                {
                    new ChoiceOption
                    {
                        Label = "Trade fairly",
                        HintText = "Gain some supplies, maintain good relations",
                        Consequence = new ChoiceConsequence
                        {
                            Type = "spawn_items",
                            Parameters = new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "item", "Silver" },
                                { "count", 100 }
                            }
                        }
                    },
                    new ChoiceOption
                    {
                        Label = "Send them away",
                        HintText = "No risk, no reward",
                        Consequence = new ChoiceConsequence
                        {
                            Type = "nothing",
                            Parameters = new System.Collections.Generic.Dictionary<string, object>()
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
    }
}

