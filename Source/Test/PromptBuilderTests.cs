using System.Linq;
using Xunit;
using AINarrator;

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
            
            // Verify the test assumption: mid-game scenario should have prisoners
            Assert.True(context.Prisoners.Count > 0, "Test requires mid-game scenario to have prisoners");

            var suggestions = ContextFormatter.GetChoiceSuggestions(context);

            Assert.Contains(suggestions, line => line.ToLower().Contains("prisoner"));
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
        
        [Fact]
        public void FormatNarrationContext_IncludesRelevantHistory_WhenPresent()
        {
            var context = MockScenarios.GetMidGameScenario();
            // Mid game scenario has RelevantHistory populated
            Assert.True(context.RelevantHistory.Count > 0, "Test requires mid-game scenario to have relevant history");

            var mockEvent = new TestEvent { Label = "Major Raid", Category = "ThreatBig", FactionName = "The Forsaken Raiders", ThreatLevel = "major" };
            string formatted = ContextFormatter.FormatNarrationContext(context, mockEvent);

            Assert.Contains("=== RELEVANT HISTORY ===", formatted);
            Assert.Contains(context.RelevantHistory[0].Summary, formatted);
        }
        
        [Fact]
        public void FormatNarrationContext_IncludesActiveNemeses_WhenPresent()
        {
            var context = MockScenarios.GetMidGameScenario();
            // Mid game scenario has ActiveNemeses populated
            Assert.True(context.ActiveNemeses.Count > 0, "Test requires mid-game scenario to have active nemeses");

            var mockEvent = new TestEvent { Label = "Major Raid", Category = "ThreatBig", FactionName = "The Forsaken Raiders", ThreatLevel = "major" };
            string formatted = ContextFormatter.FormatNarrationContext(context, mockEvent);

            Assert.Contains("=== ACTIVE NEMESES ===", formatted);
            Assert.Contains(context.ActiveNemeses[0].Name, formatted);
            Assert.Contains(context.ActiveNemeses[0].GrudgeReason, formatted);
        }
        
        [Fact]
        public void FormatNarrationContext_IncludesLegends_WhenPresent()
        {
            var context = MockScenarios.GetLateGameScenario();
            // Late game scenario has Legends populated
            Assert.True(context.Legends.Count > 0, "Test requires late-game scenario to have legends");

            var mockEvent = new TestEvent { Label = "Mechanoid Cluster", Category = "ThreatBig", FactionName = "The Mechanoid Hive", ThreatLevel = "major" };
            string formatted = ContextFormatter.FormatNarrationContext(context, mockEvent);

            Assert.Contains("=== COLONY LEGENDS ===", formatted);
            // Check for legend with mythic summary
            var legendWithSummary = context.Legends.FirstOrDefault(l => !string.IsNullOrEmpty(l.MythicSummary));
            if (legendWithSummary != null)
            {
                Assert.Contains(legendWithSummary.ArtworkLabel, formatted);
                Assert.Contains(legendWithSummary.MythicSummary, formatted);
            }
        }
        
        [Fact]
        public void FormatNarrationContext_ExcludesRetiredNemeses()
        {
            var context = MockScenarios.GetEarlyGameScenario();
            // Add a retired nemesis
            context.ActiveNemeses.Add(new MockNemesis
            {
                Name = "Retired Enemy",
                FactionName = "Test Faction",
                GrudgeReason = "Old grudge",
                IsRetired = true
            });

            var mockEvent = new TestEvent { Label = "Raid", Category = "ThreatSmall", FactionName = "The Forsaken Raiders", ThreatLevel = "minor" };
            string formatted = ContextFormatter.FormatNarrationContext(context, mockEvent);

            // Should not include retired nemesis
            if (formatted.Contains("=== ACTIVE NEMESES ==="))
            {
                Assert.DoesNotContain("Retired Enemy", formatted);
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




