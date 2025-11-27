using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// GameComponent that handles debug keyboard shortcuts.
    /// </summary>
    public class DebugKeyboardHandler : GameComponent
    {
        public DebugKeyboardHandler(Game game) : base() { }
        
        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            
            if (!Prefs.DevMode) return;
            if (Current.ProgramState != ProgramState.Playing) return;
            if (Find.CurrentMap == null) return;
            
            var e = UnityEngine.Event.current;
            if (e == null || e.type != UnityEngine.EventType.KeyDown) return;
            
            // Ctrl+Shift shortcuts
            if (e.control && e.shift && !e.alt)
            {
                if (e.keyCode == UnityEngine.KeyCode.N)
                {
                    DebugActions.TriggerNarrative();
                    e.Use();
                }
                else if (e.keyCode == UnityEngine.KeyCode.C)
                {
                    DebugActions.TriggerChoiceEvent();
                    e.Use();
                }
            }
        }
    }
    
    /// <summary>
    /// Debug actions for testing AI Narrator.
    /// 
    /// Keyboard shortcuts (Dev Mode only):
    ///   Ctrl+Shift+N - Trigger Narrative
    ///   Ctrl+Shift+C - Trigger Choice Event
    /// </summary>
    public static class DebugActions
    {
        /// <summary>
        /// Trigger a narrative popup using the LLM with real colony context.
        /// </summary>
        [DebugAction("AI Narrator", "Trigger Narrative", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TriggerNarrative()
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                Messages.Message("AI Narrator: API key not configured!", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var dialog = Dialog_NarrativePopup.CreateLoading("Generating narrative...");
            Find.WindowStack.Add(dialog);
            
            // Add debug event context to system prompt
            string systemPrompt = PromptBuilder.GetNarrationSystemPrompt() + 
                "\n\nCurrent Event: Something stirs on the horizon. The colonists sense change approaching.";
            
            // User prompt contains only the colony context
            string userPrompt = ColonyStateCollector.GetNarrationContext();
            
            StoryContext.Instance?.RegisterApiCall();
            
            OpenRouterClient.RequestNarration(systemPrompt, userPrompt,
                onSuccess: (narrative) =>
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        dialog.SetNarrative(narrative, () =>
                        {
                            StoryContext.Instance?.AddJournalEntry(narrative, JournalEntryType.Event);
                        });
                    });
                },
                onError: (error) =>
                {
                    Log.Error($"[AI Narrator] API error: {error}");
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        dialog.Close();
                        Messages.Message($"API Error: {error}", MessageTypeDefOf.RejectInput);
                    });
                }
            );
        }
        
        /// <summary>
        /// Trigger a choice event using the LLM with real colony context.
        /// </summary>
        [DebugAction("AI Narrator", "Trigger Choice Event", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TriggerChoiceEvent()
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                Messages.Message("AI Narrator: API key not configured!", MessageTypeDefOf.RejectInput);
                return;
            }
            
            Messages.Message("Generating choice event...", MessageTypeDefOf.NeutralEvent);
            
            string systemPrompt = PromptBuilder.GetChoiceSystemPrompt();
            string userPrompt = PromptBuilder.BuildChoicePrompt();
            
            StoryContext.Instance?.RegisterApiCall();
            
            OpenRouterClient.RequestChoiceEvent(systemPrompt, userPrompt,
                onSuccess: (choiceEvent) =>
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        ShowChoiceDialog(choiceEvent);
                    });
                },
                onError: (error) =>
                {
                    Log.Warning($"[AI Narrator] Choice event failed: {error}");
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        // Fallback to generated choice
                        var fallback = PromptBuilder.GetFallbackChoice();
                        ShowChoiceDialog(fallback);
                    });
                }
            );
        }
        
        private static void ShowChoiceDialog(ChoiceEvent choiceEvent)
        {
            if (choiceEvent?.Options == null || choiceEvent.Options.Count == 0)
            {
                Messages.Message("Failed to generate choice event", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var dialog = new Dialog_StoryChoice(choiceEvent, (selectedIndex) =>
            {
                if (selectedIndex >= 0 && selectedIndex < choiceEvent.Options.Count)
                {
                    var option = choiceEvent.Options[selectedIndex];
                    if (option?.Consequences != null && option.Consequences.Count > 0)
                    {
                        EventMapper.ExecuteConsequences(option.Consequences, Find.CurrentMap);
                    }
                }
            });
            
            Find.WindowStack.Add(dialog);
        }
        
        /// <summary>
        /// Reset the API call rate limit counter for testing.
        /// </summary>
        [DebugAction("AI Narrator", "Reset Rate Limit", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetRateLimit()
        {
            if (StoryContext.Instance == null)
            {
                Messages.Message("StoryContext not available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            StoryContext.Instance.CallsToday = 0;
            StoryContext.Instance.LastCallDay = -1;
            Messages.Message("API rate limit reset", MessageTypeDefOf.TaskCompletion);
        }
        
        #region Phase 2: Deep Memory Debug Actions
        
        /// <summary>
        /// Spawn a test Nemesis for the first hostile faction.
        /// </summary>
        [DebugAction("AI Narrator", "Spawn Test Nemesis", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnTestNemesis()
        {
            if (Find.CurrentMap == null)
            {
                Messages.Message("No map available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var hostileFaction = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.HostileTo(Faction.OfPlayer));
            
            if (hostileFaction == null)
            {
                Messages.Message("No hostile faction found", MessageTypeDefOf.RejectInput);
                return;
            }
            
            // Create a test Nemesis profile
            var profile = new NemesisProfile
            {
                PawnId = "TEST_NEMESIS_" + Guid.NewGuid().ToString(),
                Name = "Test Nemesis",
                FactionId = hostileFaction.def.defName,
                FactionName = hostileFaction.Name,
                Gender = Gender.Male,
                AgeBiological = 30,
                CreatedDay = GenDate.DaysPassed,
                LastSeenDay = GenDate.DaysPassed - 10,
                EncounterCount = 1,
                GrudgeReason = "Test grudge",
                GrudgeTarget = Find.CurrentMap.mapPawns?.FreeColonists?.FirstOrDefault()?.Name?.ToStringShort ?? "Unknown",
                IsRetired = false
            };
            
            StoryContext.Instance?.AddNemesis(profile);
            
            var pawn = NemesisTracker.SpawnNemesisPawn(profile, Find.CurrentMap);
            if (pawn != null)
            {
                Messages.Message($"Spawned test Nemesis: {profile.Name}", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Failed to spawn Nemesis", MessageTypeDefOf.RejectInput);
            }
        }
        
        /// <summary>
        /// Create a test Legend.
        /// </summary>
        [DebugAction("AI Narrator", "Create Test Legend", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CreateTestLegend()
        {
            if (StoryContext.Instance == null)
            {
                Messages.Message("StoryContext not available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var legend = new Legend
            {
                Id = Guid.NewGuid().ToString(),
                ArtworkLabel = "Test Masterwork Statue",
                ArtworkTale = "A statue depicting a colonist standing defiant against the harsh world.",
                CreatorName = Find.CurrentMap?.mapPawns?.FreeColonists?.FirstOrDefault()?.Name?.ToStringShort ?? "Unknown",
                Quality = QualityCategory.Masterwork,
                CreatedDay = GenDate.DaysPassed,
                CreatedDateString = GetCurrentDateString(),
                IsDestroyed = false
            };
            
            StoryContext.Instance.AddLegend(legend);
            Messages.Message($"Created test Legend: {legend.ArtworkLabel}", MessageTypeDefOf.PositiveEvent);
        }
        
        /// <summary>
        /// Trigger a trauma event manually.
        /// </summary>
        [DebugAction("AI Narrator", "Trigger Trauma Event", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TriggerTraumaEvent()
        {
            if (Find.CurrentMap == null)
            {
                Messages.Message("No map available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var survivors = Find.CurrentMap.mapPawns?.FreeColonists?.Where(p => !p.Dead && !p.Destroyed).ToList();
            if (survivors == null || survivors.Count == 0)
            {
                Messages.Message("No colonists available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            // Simulate trauma by recording a historical event
            StoryContext.Instance?.RecordHistoricalEvent(
                "Major tragedy struck the colony",
                "Tragedy",
                significance: 5.0f
            );
            
            Messages.Message($"Triggered trauma event for {survivors.Count} survivors", MessageTypeDefOf.NeutralEvent);
        }
        
        /// <summary>
        /// Log history search results.
        /// </summary>
        [DebugAction("AI Narrator", "Log History Search", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void LogHistorySearch()
        {
            if (StoryContext.Instance == null)
            {
                Messages.Message("StoryContext not available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var keywords = new List<string> { "Raid", "Battle", "Death" };
            var entityIds = Find.CurrentMap?.mapPawns?.FreeColonists?.Select(c => c.ThingID).ToList() ?? new List<string>();
            
            var results = HistorySearch.FindRelevantHistory(keywords, entityIds, maxResults: 10);
            
            Log.Message($"[AI Narrator] History search found {results.Count} relevant events:");
            foreach (var evt in results)
            {
                Log.Message($"  - {evt.DateString}: {evt.Summary} (Significance: {evt.SignificanceScore})");
            }
            
            Messages.Message($"Found {results.Count} relevant historical events (see log)", MessageTypeDefOf.NeutralEvent);
        }
        
        /// <summary>
        /// List all Nemeses.
        /// </summary>
        [DebugAction("AI Narrator", "List All Nemeses", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ListNemeses()
        {
            if (StoryContext.Instance == null)
            {
                Messages.Message("StoryContext not available", MessageTypeDefOf.RejectInput);
                return;
            }
            
            var nemeses = StoryContext.Instance.Nemeses;
            if (nemeses == null || nemeses.Count == 0)
            {
                Messages.Message("No Nemeses recorded", MessageTypeDefOf.NeutralEvent);
                return;
            }
            
            Log.Message($"[AI Narrator] Found {nemeses.Count} Nemeses:");
            foreach (var nemesis in nemeses)
            {
                string status = nemesis.IsRetired ? "RETIRED" : "ACTIVE";
                Log.Message($"  - {nemesis.Name} ({nemesis.FactionName}) - {status} - Encounters: {nemesis.EncounterCount}, Last seen: {GenDate.DaysPassed - nemesis.LastSeenDay} days ago");
            }
            
            Messages.Message($"Listed {nemeses.Count} Nemeses (see log)", MessageTypeDefOf.NeutralEvent);
        }
        
        private static string GetCurrentDateString()
        {
            Map map = Find.CurrentMap;
            if (map == null) return "Unknown Date";
            
            int day = GenLocalDate.DayOfQuadrum(map) + 1;
            float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
            string quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, longitude).Label();
            int year = GenDate.Year(Find.TickManager.TicksAbs, longitude);
            
            return $"{quadrum} {day}, {year}";
        }
        
        #endregion
    }
}
