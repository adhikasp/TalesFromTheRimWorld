using HarmonyLib;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Harmony patches to intercept vanilla events and add narrative flavor.
    /// </summary>
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        /// <summary>
        /// Patch Storyteller.TryFire to intercept events and add narrative.
        /// </summary>
        [HarmonyPatch(typeof(Storyteller), nameof(Storyteller.TryFire))]
        [HarmonyPrefix]
        public static bool TryFire_Prefix(FiringIncident fi, ref bool __result, ref bool __state)
        {
            // Store whether we should intercept this event
            __state = ShouldIntercept(fi);
            
            if (__state)
            {
                // Queue the narrative request before the event fires
                RequestNarrativeForIncident(fi);
            }
            
            // Always let the original method run
            return true;
        }
        
        /// <summary>
        /// After the event fires, show the narrative popup if applicable.
        /// </summary>
        [HarmonyPatch(typeof(Storyteller), nameof(Storyteller.TryFire))]
        [HarmonyPostfix]
        public static void TryFire_Postfix(FiringIncident fi, bool __result, bool __state)
        {
            if (__result && __state)
            {
                // Event fired successfully and we intercepted it
                // The narrative will be shown via the callback from RequestNarrativeForIncident
            }
        }
        
        private static bool ShouldIntercept(FiringIncident fi)
        {
            // Check settings
            if (!AINarratorMod.Settings.ShowNarrativeNotifications) return false;
            if (!AINarratorMod.Settings.IsConfigured()) return false;
            
            // Check rate limiting
            if (StoryContext.Instance != null && !StoryContext.Instance.CanMakeApiCall())
            {
                return false;
            }
            
            // Only intercept certain event categories
            if (fi.def == null) return false;
            
            var category = fi.def.category;
            
            // Skip very minor events
            if (category == IncidentCategoryDefOf.Misc) return false;
            
            // Intercept threats, gifts, diseases, etc.
            return category == IncidentCategoryDefOf.ThreatBig ||
                   category == IncidentCategoryDefOf.ThreatSmall ||
                   category == IncidentCategoryDefOf.GiveQuest ||
                   fi.def.defName.Contains("Wanderer") ||
                   fi.def.defName.Contains("Visitor") ||
                   fi.def.defName.Contains("Raid") ||
                   fi.def.defName.Contains("Siege") ||
                   fi.def.defName.Contains("Infestation") ||
                   fi.def.defName.Contains("Disease") ||
                   fi.def.defName.Contains("Eclipse") ||
                   fi.def.defName.Contains("Aurora");
        }
        
        private static void RequestNarrativeForIncident(FiringIncident fi)
        {
            // Register API call for rate limiting
            StoryContext.Instance?.RegisterApiCall();
            
            string systemPrompt = PromptBuilder.GetNarrationSystemPrompt();
            string userPrompt = PromptBuilder.BuildEventPrompt(fi.def, fi.parms);
            string eventSummary = PromptBuilder.GetEventSummary(fi.def, fi.parms);
            
            // Store incident details for the callback
            var incidentDef = fi.def;
            var incidentParms = fi.parms;
            
            OpenRouterClient.RequestNarration(systemPrompt, userPrompt,
                onSuccess: (narrative) =>
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        ShowNarrativePopup(narrative, eventSummary, incidentDef);
                    });
                },
                onError: (error) =>
                {
                    Log.Warning($"[AI Narrator] Narrative request failed: {error}");
                    
                    // Use fallback narrative
                    string fallback = PromptBuilder.GetFallbackNarrative(incidentDef);
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        ShowNarrativePopup(fallback, eventSummary, incidentDef);
                    });
                }
            );
        }
        
        private static void ShowNarrativePopup(string narrative, string eventSummary, IncidentDef incident)
        {
            // Log to story journal
            if (StoryContext.Instance != null)
            {
                StoryContext.Instance.AddJournalEntry(narrative, JournalEntryType.Event);
                StoryContext.Instance.AddRecentEvent(eventSummary);
            }
            
            // Show popup
            var dialog = new Dialog_NarrativePopup(narrative, eventSummary, () =>
            {
                // Callback when player clicks Continue
                // Event has already fired, this is just for UX
            });
            
            Find.WindowStack.Add(dialog);
        }
    }
}

