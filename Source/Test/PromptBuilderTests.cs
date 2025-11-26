using System.Linq;
using Xunit;

namespace AINarrator.Test
{
    public class PromptBuilderTests
    {
        [Fact]
        public void FormatNarrationContext_IncludesCurrentEventDetails()
        {
            var context = MockScenarios.GetMidGameScenario();
            var mockEvent = new TestEvent
            {
                Label = "Major Raid",
                Category = "ThreatBig",
                FactionName = "The Forsaken Raiders",
                ThreatLevel = "major"
            };

            string formatted = ContextFormatter.FormatNarrationContext(context, mockEvent);

            Assert.Contains("=== CURRENT EVENT ===", formatted);
            Assert.Contains(mockEvent.Label, formatted);
            Assert.Contains($"Faction: {mockEvent.FactionName}", formatted);
            Assert.Contains($"Threat Level: {mockEvent.ThreatLevel}", formatted);
        }

        [Fact]
        public void GetChoiceSuggestions_IncludesPrisonerHookWhenPrisonersPresent()
        {
            var context = MockScenarios.GetMidGameScenario();

            var suggestions = ContextFormatter.GetChoiceSuggestions(context);

            Assert.Contains(suggestions, line => line.Contains("prisoner"));
        }

        [Fact]
        public void GetChoiceSuggestions_LimitsResultsToFiveEntries()
        {
            var context = MockScenarios.GetCrisisScenario();

            var suggestions = ContextFormatter.GetChoiceSuggestions(context);

            Assert.InRange(suggestions.Count, 1, 5);
        }

        [Fact]
        public void FormatChoiceContext_ListsAllColonists()
        {
            var context = MockScenarios.GetEarlyGameScenario();

            string formatted = ContextFormatter.FormatChoiceContext(context);

            foreach (var colonist in context.Colonists)
            {
                Assert.Contains(colonist.Name, formatted);
            }
        }

        private sealed class TestEvent : IEventInfo
        {
            public string Label { get; set; }
            public string Category { get; set; }
            public string FactionName { get; set; }
            public string ThreatLevel { get; set; }
        }
    }
}



