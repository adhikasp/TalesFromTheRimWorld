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
            
            string systemPrompt = PromptBuilder.GetNarrationSystemPrompt();
            string userPrompt = ColonyStateCollector.GetNarrationContext() + 
                "\nCurrent Event: Something stirs on the horizon. The colonists sense change approaching." +
                "\nTASK: Write atmospheric flavor text for this moment in the colony's story.";
            
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
                    
                    // Log to journal
                    string journalText = $"{choiceEvent.NarrativeText}\n→ Choice: {option.Label}";
                    StoryContext.Instance?.AddJournalEntry(journalText, JournalEntryType.Choice, option.Label);
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
    }
}
