using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Harmony patches to intercept vanilla events and add narrative flavor.
    /// Also tracks deaths, battles, and recruitments for historical context.
    /// </summary>
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        #region Event Interception
        
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
        
        #endregion
        
        #region Death Tracking
        
        /// <summary>
        /// Track colonist deaths for narrative context.
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPrefix]
        public static void Pawn_Kill_Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                // Only track player faction colonists
                if (__instance?.Faction != Faction.OfPlayer) return;
                if (!__instance.RaceProps.Humanlike) return;
                if (__instance.Dead) return; // Already dead
                
                // Record the death
                StoryContext.Instance?.RecordColonistDeath(__instance, dinfo);
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error tracking death: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Recruitment Tracking
        
        /// <summary>
        /// Track when pawns are recruited to the colony.
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
        [HarmonyPostfix]
        public static void Pawn_SetFaction_Postfix(Pawn __instance, Faction newFaction, Pawn recruiter)
        {
            try
            {
                // Only track if joining player faction
                if (newFaction != Faction.OfPlayer) return;
                if (!__instance.RaceProps.Humanlike) return;
                
                // Determine recruitment method
                string method = "joined";
                if (recruiter != null)
                {
                    method = $"recruited by {recruiter.Name?.ToStringShort ?? "unknown"}";
                }
                else if (__instance.guest?.IsPrisoner == true)
                {
                    method = "converted from prisoner";
                }
                
                StoryContext.Instance?.RecordRecruitment(__instance, method);
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error tracking recruitment: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Battle Tracking
        
        // Track ongoing battle state
        private static Dictionary<int, BattleTracker> activeBattles = new Dictionary<int, BattleTracker>();
        
        private class BattleTracker
        {
            public int StartTick;
            public string BattleType;
            public string EnemyFaction;
            public HashSet<string> EnemyPawns = new HashSet<string>();
            public int ColonistCasualties;
            public int EnemiesKilled;
            public HashSet<string> Heroes = new HashSet<string>();
        }
        
        /// <summary>
        /// Track when raids begin.
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
        [HarmonyPostfix]
        public static void RaidEnemy_Postfix(IncidentParms parms, bool __result)
        {
            try
            {
                if (!__result) return;
                if (parms?.target is not Map map) return;
                
                var tracker = new BattleTracker
                {
                    StartTick = Find.TickManager.TicksGame,
                    BattleType = "Raid",
                    EnemyFaction = parms.faction?.Name ?? "unknown"
                };
                
                // Count initial enemies
                var hostiles = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.HostileTo(Faction.OfPlayer))
                    .ToList();
                
                foreach (var hostile in hostiles)
                {
                    tracker.EnemyPawns.Add(hostile.ThingID);
                }
                
                activeBattles[map.uniqueID] = tracker;
                
                Log.Message($"[AI Narrator] Battle started: {tracker.EnemyFaction} raid with {tracker.EnemyPawns.Count} enemies");
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error tracking raid start: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Track enemy kills for battle stats and heroes.
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPostfix]
        public static void Pawn_Kill_Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                if (__instance?.Map == null) return;
                
                int mapId = __instance.Map.uniqueID;
                if (!activeBattles.TryGetValue(mapId, out var tracker)) return;
                
                // Track enemy kills
                if (__instance.HostileTo(Faction.OfPlayer))
                {
                    tracker.EnemiesKilled++;
                    
                    // Track who killed them (hero tracking)
                    if (dinfo.HasValue && dinfo.Value.Instigator is Pawn killer)
                    {
                        if (killer.Faction == Faction.OfPlayer)
                        {
                            tracker.Heroes.Add(killer.Name?.ToStringShort ?? "Unknown");
                        }
                    }
                }
                // Track colonist casualties
                else if (__instance.Faction == Faction.OfPlayer && __instance.RaceProps.Humanlike)
                {
                    tracker.ColonistCasualties++;
                }
                
                // Check if battle is over (no more hostile pawns)
                var remainingHostiles = __instance.Map.mapPawns.AllPawnsSpawned
                    .Count(p => p.HostileTo(Faction.OfPlayer) && !p.Dead && !p.Downed);
                
                if (remainingHostiles == 0 && tracker.EnemiesKilled > 0)
                {
                    // Battle ended - record it
                    RecordCompletedBattle(mapId, tracker);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error tracking kill: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Periodic check to clean up stale battle trackers and record completed battles.
        /// </summary>
        public static void CheckBattleStatus()
        {
            try
            {
                var toRemove = new List<int>();
                
                foreach (var kvp in activeBattles)
                {
                    var mapId = kvp.Key;
                    var tracker = kvp.Value;
                    
                    // Find the map
                    var map = Find.Maps.FirstOrDefault(m => m.uniqueID == mapId);
                    if (map == null)
                    {
                        toRemove.Add(mapId);
                        continue;
                    }
                    
                    // Check if battle is stale (over 2 days old)
                    if (Find.TickManager.TicksGame - tracker.StartTick > GenDate.TicksPerDay * 2)
                    {
                        // Record whatever we have
                        if (tracker.EnemiesKilled > 0 || tracker.ColonistCasualties > 0)
                        {
                            RecordCompletedBattle(mapId, tracker);
                        }
                        toRemove.Add(mapId);
                        continue;
                    }
                    
                    // Check if battle ended (no hostiles left)
                    var remainingHostiles = map.mapPawns.AllPawnsSpawned
                        .Count(p => p.HostileTo(Faction.OfPlayer) && !p.Dead && !p.Downed);
                    
                    if (remainingHostiles == 0 && tracker.EnemyPawns.Count > 0)
                    {
                        if (tracker.EnemiesKilled > 0 || tracker.ColonistCasualties > 0)
                        {
                            RecordCompletedBattle(mapId, tracker);
                        }
                        toRemove.Add(mapId);
                    }
                }
                
                foreach (var id in toRemove)
                {
                    activeBattles.Remove(id);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error checking battle status: {ex.Message}");
            }
        }
        
        private static void RecordCompletedBattle(int mapId, BattleTracker tracker)
        {
            try
            {
                StoryContext.Instance?.RecordBattle(
                    tracker.BattleType,
                    tracker.EnemyFaction,
                    tracker.EnemyPawns.Count,
                    tracker.ColonistCasualties,
                    tracker.EnemiesKilled,
                    tracker.Heroes.Take(3).ToList()
                );
                
                // Record heroic actions for top performers
                foreach (var hero in tracker.Heroes.Take(3))
                {
                    StoryContext.Instance?.RecordHeroicAction(
                        Find.CurrentMap?.mapPawns?.FreeColonists?.FirstOrDefault(p => p.Name?.ToStringShort == hero),
                        $"fought valiantly against {tracker.EnemyFaction}"
                    );
                }
                
                Log.Message($"[AI Narrator] Battle recorded: {tracker.BattleType} vs {tracker.EnemyFaction}, {tracker.EnemiesKilled} enemies killed, {tracker.ColonistCasualties} casualties");
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error recording battle: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Social Interaction Tracking
        
        /// <summary>
        /// Track significant social interactions for narrative context.
        /// </summary>
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "Interacted")]
        [HarmonyPostfix]
        public static void RomanceAttempt_Postfix(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks)
        {
            try
            {
                if (initiator?.Faction != Faction.OfPlayer) return;
                
                // Check if romance was accepted (extraSentencePacks contains success rules)
                bool success = extraSentencePacks?.Any(r => r.defName.Contains("Success")) ?? false;
                
                string action = success 
                    ? $"{initiator.Name?.ToStringShort} successfully romanced {recipient?.Name?.ToStringShort}"
                    : $"{initiator.Name?.ToStringShort} was rejected by {recipient?.Name?.ToStringShort}";
                
                StoryContext.Instance?.RecordSignificantInteraction(action);
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error tracking romance: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Track marriage proposals.
        /// </summary>
        [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), "Interacted")]
        [HarmonyPostfix]
        public static void MarriageProposal_Postfix(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks)
        {
            try
            {
                if (initiator?.Faction != Faction.OfPlayer) return;
                
                bool success = extraSentencePacks?.Any(r => r.defName.Contains("Accepted")) ?? false;
                
                string action = success 
                    ? $"{initiator.Name?.ToStringShort} and {recipient?.Name?.ToStringShort} got engaged!"
                    : $"{initiator.Name?.ToStringShort}'s proposal to {recipient?.Name?.ToStringShort} was declined";
                
                StoryContext.Instance?.RecordSignificantInteraction(action);
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error tracking proposal: {ex.Message}");
            }
        }
        
        #endregion
    }
}
