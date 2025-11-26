using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// StorytellerComp that handles LLM-driven narrative events.
    /// Works alongside vanilla storyteller logic.
    /// </summary>
    public class StorytellerComp_LLM : StorytellerComp
    {
        // Choice event tracking
        private int lastChoiceDay = -1;
        private int choicesThisQuadrum = 0;
        private int lastQuadrum = -1;
        
        // Constants
        private const int MIN_DAYS_BETWEEN_CHOICES = 5;
        private const int MAX_CHOICES_PER_QUADRUM = 2;
        private const float CHOICE_CHANCE_PER_DAY = 0.15f;
        
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            // Check for choice event trigger
            if (ShouldTriggerChoiceEvent(target))
            {
                TriggerChoiceEvent(target as Map);
            }
            
            // Let vanilla incidents flow through
            yield break;
        }
        
        private bool ShouldTriggerChoiceEvent(IIncidentTarget target)
        {
            if (!AINarratorMod.Settings.EnableChoiceEvents) return false;
            if (!AINarratorMod.Settings.IsConfigured()) return false;
            
            Map map = target as Map;
            if (map == null) return false;
            
            // Check rate limiting
            if (StoryContext.Instance != null && !StoryContext.Instance.CanMakeApiCall())
            {
                return false;
            }
            
            int currentDay = GenDate.DaysPassed;
            int currentQuadrum = (int)GenDate.Quadrum(Find.TickManager.TicksAbs, 0f);
            
            // Reset quadrum counter if new quadrum
            if (currentQuadrum != lastQuadrum)
            {
                choicesThisQuadrum = 0;
                lastQuadrum = currentQuadrum;
            }
            
            // Check limits
            if (choicesThisQuadrum >= MAX_CHOICES_PER_QUADRUM) return false;
            if (currentDay - lastChoiceDay < MIN_DAYS_BETWEEN_CHOICES) return false;
            
            // Random chance
            return Rand.Chance(CHOICE_CHANCE_PER_DAY);
        }
        
        private void TriggerChoiceEvent(Map map)
        {
            if (map == null) return;
            
            lastChoiceDay = GenDate.DaysPassed;
            choicesThisQuadrum++;
            
            // Register API call for rate limiting
            StoryContext.Instance?.RegisterApiCall();
            
            // Request choice event from LLM
            string systemPrompt = PromptBuilder.GetChoiceSystemPrompt();
            string userPrompt = PromptBuilder.BuildChoicePrompt();
            
            OpenRouterClient.RequestChoiceEvent(systemPrompt, userPrompt,
                onSuccess: (choiceEvent) =>
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        ShowChoiceDialog(choiceEvent, map);
                    });
                },
                onError: (error) =>
                {
                    Log.Warning($"[AI Narrator] Choice event request failed: {error}");
                    
                    // Use fallback choice
                    var fallback = PromptBuilder.GetFallbackChoice();
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        ShowChoiceDialog(fallback, map);
                    });
                }
            );
        }
        
        private void ShowChoiceDialog(ChoiceEvent choiceEvent, Map map)
        {
            if (choiceEvent == null || choiceEvent.Options == null || choiceEvent.Options.Count == 0)
            {
                Log.Warning("[AI Narrator] Invalid choice event, skipping");
                return;
            }
            
            var dialog = new Dialog_StoryChoice(choiceEvent, (selectedIndex) =>
            {
                if (selectedIndex >= 0 && selectedIndex < choiceEvent.Options.Count)
                {
                    var option = choiceEvent.Options[selectedIndex];
                    if (option?.Consequences != null && option.Consequences.Count > 0)
                    {
                        EventMapper.ExecuteConsequences(option.Consequences, map);
                    }
                    else
                    {
                        Log.Message("[AI Narrator] Choice option had no mechanical consequences");
                    }
                }
            });
            
            Find.WindowStack.Add(dialog);
        }
    }
    
    /// <summary>
    /// Properties class for StorytellerComp_LLM.
    /// </summary>
    public class StorytellerCompProperties_LLM : StorytellerCompProperties
    {
        // minDaysPassed is inherited from StorytellerCompProperties
        
        public StorytellerCompProperties_LLM()
        {
            compClass = typeof(StorytellerComp_LLM);
        }
    }
}

