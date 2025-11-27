using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Tracks major tragedies and offers player choices for character growth.
    /// </summary>
    public static class TraumaTracker
    {
        private static int colonistCountAtStartOfPeriod;
        private static int periodStartDay;
        private static bool initialized = false;
        
        /// <summary>
        /// Called every tick to check for trauma events.
        /// </summary>
        public static void OnTick()
        {
            try
            {
                if (Find.TickManager == null || Find.CurrentMap == null) return;
                
                int currentDay = GenDate.DaysPassed;
                
                // Initialize on first call
                if (!initialized)
                {
                    colonistCountAtStartOfPeriod = GetColonistCount();
                    periodStartDay = currentDay;
                    initialized = true;
                    return;
                }
                
                // Reset tracking period every 3 days
                if (currentDay - periodStartDay >= 3)
                {
                    colonistCountAtStartOfPeriod = GetColonistCount();
                    periodStartDay = currentDay;
                    return;
                }
                
                // Check for tragedy (>50% casualties in <3 days)
                int currentCount = GetColonistCount();
                if (colonistCountAtStartOfPeriod <= 0) return;
                
                float lossRatio = 1 - (currentCount / (float)colonistCountAtStartOfPeriod);
                
                if (lossRatio >= 0.5f && currentCount > 0)
                {
                    TriggerTraumaEvent(GetSurvivors());
                    // Reset period after triggering
                    colonistCountAtStartOfPeriod = currentCount;
                    periodStartDay = currentDay;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error in TraumaTracker: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check for personal loss (bonded pawn/lover dies).
        /// </summary>
        public static void CheckPersonalLoss(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.Faction != Faction.OfPlayer) return;
                if (Find.CurrentMap == null) return;
                
                // Check for bonded animals
                var bonded = pawn.relations?.DirectRelations
                    .FirstOrDefault(r => r.def == PawnRelationDefOf.Bond);
                
                if (bonded != null && bonded.otherPawn.Dead)
                {
                    TriggerPersonalTrauma(pawn, "lost bonded animal", bonded.otherPawn);
                    return;
                }
                
                // Check for lovers/spouses
                var lover = pawn.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover);
                if (lover == null)
                {
                    lover = pawn.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
                }
                
                if (lover != null && lover.Dead)
                {
                    TriggerPersonalTrauma(pawn, "lost lover", lover);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error checking personal loss: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Trigger trauma event for survivors after major tragedy.
        /// </summary>
        private static void TriggerTraumaEvent(List<Pawn> survivors)
        {
            if (survivors == null || survivors.Count == 0) return;
            
            // For now, just log - full implementation would show choice dialog
            Log.Message($"[AI Narrator] Major tragedy detected: {survivors.Count} survivors after >50% casualties");
            
            // TODO: Show choice dialog for each survivor
            // "How did [Pawn] change from this experience?"
            // Options: Shell-shocked, Hardened, Determined
        }
        
        /// <summary>
        /// Trigger personal trauma for a specific pawn.
        /// </summary>
        private static void TriggerPersonalTrauma(Pawn pawn, string reason, Pawn lostPawn)
        {
            if (pawn == null) return;
            
            Log.Message($"[AI Narrator] Personal loss detected: {pawn.Name?.ToStringShort} {reason} ({lostPawn?.Name?.ToStringShort})");
            
            // TODO: Show choice dialog
            // "How did [Pawn] change from losing [LostPawn]?"
        }
        
        /// <summary>
        /// Get current colonist count.
        /// </summary>
        private static int GetColonistCount()
        {
            Map map = Find.CurrentMap;
            if (map == null) return 0;
            
            return map.mapPawns?.FreeColonists?.Count ?? 0;
        }
        
        /// <summary>
        /// Get surviving colonists.
        /// </summary>
        private static List<Pawn> GetSurvivors()
        {
            Map map = Find.CurrentMap;
            if (map == null) return new List<Pawn>();
            
            return map.mapPawns?.FreeColonists?.Where(p => !p.Dead && !p.Destroyed).ToList() ?? new List<Pawn>();
        }
    }
}

