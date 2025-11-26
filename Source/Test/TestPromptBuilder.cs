using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Use actual PromptBuilder and ContextFormatter from main project
using AINarrator;

namespace AINarrator.Test
{
    /// <summary>
    /// Builds test prompts using:
    /// - ACTUAL system prompts from AINarrator.PromptBuilder (no duplication!)
    /// - ACTUAL formatting from AINarrator.ContextFormatter (no duplication!)
    /// - Mock colony context for testing (since real ColonyStateCollector needs RimWorld)
    /// </summary>
    public static class TestPromptBuilder
    {
        /// <summary>
        /// Get narration system prompt - delegates to ACTUAL PromptBuilder.
        /// This ensures test uses the same prompt as production.
        /// </summary>
        public static string GetNarrationSystemPrompt()
        {
            // Use the ACTUAL system prompt from the real PromptBuilder
            return PromptBuilder.GetNarrationSystemPrompt();
        }

        /// <summary>
        /// Get choice system prompt - delegates to ACTUAL PromptBuilder.
        /// This ensures test uses the same prompt as production.
        /// </summary>
        public static string GetChoiceSystemPrompt()
        {
            // Use the ACTUAL system prompt from the real PromptBuilder
            return PromptBuilder.GetChoiceSystemPrompt();
        }

        /// <summary>
        /// Build event prompt using mock context.
        /// Uses shared ContextFormatter for consistent output with production.
        /// </summary>
        public static string BuildEventPrompt(MockColonyContext context, MockEvent mockEvent)
        {
            var sb = new StringBuilder();

            sb.AppendLine("COLONY CONTEXT:");
            // Use shared ContextFormatter with mock data implementing IColonySnapshot
            sb.AppendLine(ContextFormatter.FormatNarrationContext(context, mockEvent));
            sb.AppendLine();
            sb.AppendLine("TASK: Write atmospheric flavor text for this event. Make it feel like part of an unfolding story.");
            sb.AppendLine("- Reference specific colonists by name when relevant");
            sb.AppendLine("- Consider recent events and social dynamics");
            sb.AppendLine("- Match the atmosphere to current weather and time of day");
            sb.AppendLine("- If colonists have died recently, acknowledge the lingering grief when appropriate");

            return sb.ToString();
        }

        /// <summary>
        /// Build choice prompt using mock context.
        /// Uses shared ContextFormatter for consistent output with production.
        /// </summary>
        public static string BuildChoicePrompt(MockColonyContext context)
        {
            var sb = new StringBuilder();

            sb.AppendLine("COLONY CONTEXT:");
            // Use shared ContextFormatter with mock data implementing IColonySnapshot
            sb.AppendLine(ContextFormatter.FormatChoiceContext(context));
            sb.AppendLine();
            sb.AppendLine("TASK: Create a choice dilemma relevant to this colony's current situation. Output as JSON.");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Mock event data for testing.
    /// Implements IEventInfo for shared formatting.
    /// </summary>
    public class MockEvent : IEventInfo
    {
        public string Label { get; set; }
        public string Category { get; set; }
        public string FactionName { get; set; }
        public string ThreatLevel { get; set; }

        // Predefined events for testing
        public static MockEvent RaidSmall => new MockEvent
        {
            Label = "Raid",
            Category = "ThreatSmall",
            FactionName = "The Forsaken Raiders",
            ThreatLevel = "minor"
        };

        public static MockEvent RaidLarge => new MockEvent
        {
            Label = "Major Raid",
            Category = "ThreatBig",
            FactionName = "The Forsaken Raiders",
            ThreatLevel = "major"
        };

        public static MockEvent TraderArrival => new MockEvent
        {
            Label = "Trader Caravan",
            Category = "Misc",
            FactionName = "Proxima Trading Company",
            ThreatLevel = null
        };

        public static MockEvent WandererJoins => new MockEvent
        {
            Label = "Wanderer Joins",
            Category = "Misc",
            FactionName = null,
            ThreatLevel = null
        };

        public static MockEvent SolarFlare => new MockEvent
        {
            Label = "Solar Flare",
            Category = "Misc",
            FactionName = null,
            ThreatLevel = null
        };

        public static MockEvent PsychicDrone => new MockEvent
        {
            Label = "Psychic Drone (Male)",
            Category = "ThreatSmall",
            FactionName = null,
            ThreatLevel = "moderate"
        };

        public static MockEvent ManhunterPack => new MockEvent
        {
            Label = "Manhunter Pack",
            Category = "ThreatSmall",
            FactionName = null,
            ThreatLevel = "moderate"
        };

        public static MockEvent ColdSnap => new MockEvent
        {
            Label = "Cold Snap",
            Category = "Misc",
            FactionName = null,
            ThreatLevel = "environmental"
        };

        public static MockEvent MechanoidCluster => new MockEvent
        {
            Label = "Mechanoid Cluster",
            Category = "ThreatBig",
            FactionName = "The Mechanoid Hive",
            ThreatLevel = "major"
        };
    }
}
