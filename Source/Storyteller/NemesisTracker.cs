using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Tracks and manages Nemesis profiles - recurring enemies with personal grudges.
    /// </summary>
    public static class NemesisTracker
    {
        /// <summary>
        /// Called when a hostile pawn exits the map (flees or escapes).
        /// </summary>
        public static void OnHostilePawnExit(Pawn pawn, bool fled)
        {
            try
            {
                if (pawn == null) return;
                if (StoryContext.Instance == null) return;
                
                if (!ShouldPromoteToNemesis(pawn)) return;
                
                var profile = CreateNemesisProfile(pawn);
                if (profile != null)
                {
                    StoryContext.Instance.AddNemesis(profile);
                    Log.Message($"[AI Narrator] Created Nemesis profile: {profile.Name} ({profile.FactionName})");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error creating Nemesis profile: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if a pawn should be promoted to Nemesis status.
        /// </summary>
        private static bool ShouldPromoteToNemesis(Pawn pawn)
        {
            // Must be humanlike and hostile
            if (!pawn.RaceProps.Humanlike) return false;
            if (!pawn.HostileTo(Faction.OfPlayer)) return false;
            
            // Check if already a Nemesis
            if (StoryContext.Instance != null)
            {
                var existing = StoryContext.Instance.Nemeses.FirstOrDefault(n => n.PawnId == pawn.ThingID);
                if (existing != null && !existing.IsRetired) return false;
            }
            
            // Criteria for promotion:
            // 1. Pawn killed a colonist this battle
            // 2. Pawn downed a colonist this battle
            // 3. Pawn has relationship with a colonist
            // 4. Pawn survived multiple engagements (tracked separately)
            
            Map map = pawn.Map ?? Find.CurrentMap;
            if (map == null) return false;
            
            // Check for recent colonist deaths/kills
            var recentDeaths = StoryContext.Instance?.ColonistDeaths
                .Where(d => d.DayDied == GenDate.DaysPassed)
                .ToList();
            
            if (recentDeaths != null && recentDeaths.Any())
            {
                // Check if this pawn was involved in any deaths
                foreach (var death in recentDeaths)
                {
                    if (death.KillerName == pawn.Name?.ToStringShort || 
                        death.KillerName == pawn.LabelShort)
                    {
                        return true;
                    }
                }
            }
            
            // Check for relationships with colonists
            if (pawn.relations != null)
            {
                var colonists = map.mapPawns?.FreeColonists;
                if (colonists != null)
                {
                    foreach (var colonist in colonists)
                    {
                        if (colonist.relations != null)
                        {
                            var relation = colonist.relations.DirectRelations
                                .FirstOrDefault(r => r.otherPawn == pawn && 
                                    (r.def == PawnRelationDefOf.ExLover ||
                                     r.def == PawnRelationDefOf.ExSpouse ||
                                     r.def == PawnRelationDefOf.Sibling ||
                                     r.def == PawnRelationDefOf.Parent ||
                                     r.def == PawnRelationDefOf.Child));
                            
                            if (relation != null) return true;
                        }
                    }
                }
            }
            
            // Check if pawn dealt significant damage (downed a colonist)
            // This is tracked via battle system - if a colonist was downed, we'll know
            var recentBattles = StoryContext.Instance?.MajorBattles
                .Where(b => b.DayOccurred == GenDate.DaysPassed)
                .ToList();
            
            if (recentBattles != null && recentBattles.Any(b => b.ColonistCasualties > 0))
            {
                // If there were casualties and this pawn was in the battle, promote
                // (Simplified - in a full implementation we'd track which pawns were in which battles)
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Create a Nemesis profile from a pawn.
        /// </summary>
        private static NemesisProfile CreateNemesisProfile(Pawn pawn)
        {
            if (pawn == null) return null;
            
            var profile = new NemesisProfile
            {
                PawnId = pawn.ThingID,
                Name = pawn.Name?.ToStringFull ?? pawn.LabelShort,
                FactionId = pawn.Faction?.def?.defName,
                FactionName = pawn.Faction?.Name ?? "Unknown",
                Gender = pawn.gender,
                AgeBiological = pawn.ageTracker?.AgeBiologicalYears ?? 0,
                CreatedDay = GenDate.DaysPassed,
                LastSeenDay = GenDate.DaysPassed,
                EncounterCount = 1,
                IsRetired = false
            };
            
            // Appearance
            if (pawn.story != null)
            {
                profile.BodyType = pawn.story.bodyType?.defName;
                profile.HeadType = pawn.story.headType?.defName;
                profile.HairDef = pawn.story.hairDef?.defName;
                // Note: beardDef, hairColor, skinColor may not be available in all RimWorld versions
                // These are stored but may not be restorable
            }
            
            // Combat skills (top 3)
            if (pawn.skills != null)
            {
                var combatSkills = pawn.skills.skills
                    .Where(s => s.def == SkillDefOf.Shooting || 
                               s.def == SkillDefOf.Melee ||
                               s.def == SkillDefOf.Medicine)
                    .OrderByDescending(s => s.Level)
                    .Take(3)
                    .Select(s => $"{s.def.label}: {s.Level}")
                    .ToList();
                
                profile.Skills = combatSkills;
            }
            
            // Notable traits
            if (pawn.story != null && pawn.story.traits != null)
            {
                profile.Traits = pawn.story.traits.allTraits
                    .Where(t => t.def == TraitDefOf.Bloodlust ||
                               t.def == TraitDefOf.Psychopath ||
                               t.def == TraitDefOf.Kind ||
                               t.def.label.ToLower().Contains("cannibal"))
                    .Select(t => t.def.label)
                    .ToList();
            }
            
            // Determine grudge reason
            profile.GrudgeReason = DetermineGrudgeReason(pawn);
            profile.GrudgeTarget = DetermineGrudgeTarget(pawn);
            
            return profile;
        }
        
        /// <summary>
        /// Determine why this pawn has a grudge.
        /// </summary>
        private static string DetermineGrudgeReason(Pawn pawn)
        {
            // Check recent deaths
            var recentDeaths = StoryContext.Instance?.ColonistDeaths
                .Where(d => d.DayDied == GenDate.DaysPassed)
                .ToList();
            
            if (recentDeaths != null && recentDeaths.Any())
            {
                var killed = recentDeaths.FirstOrDefault(d => 
                    d.KillerName == pawn.Name?.ToStringShort || 
                    d.KillerName == pawn.LabelShort);
                
                if (killed != null)
                {
                    return $"Killed {killed.ShortName}";
                }
            }
            
            // Check for relationships
            Map map = pawn.Map ?? Find.CurrentMap;
            if (map != null)
            {
                var colonists = map.mapPawns?.FreeColonists;
                if (colonists != null)
                {
                    foreach (var colonist in colonists)
                    {
                        if (colonist.relations != null)
                        {
                            var relation = colonist.relations.DirectRelations
                                .FirstOrDefault(r => r.otherPawn == pawn);
                            
                            if (relation != null)
                            {
                                return $"Former {relation.def.label} of {colonist.Name?.ToStringShort}";
                            }
                        }
                    }
                }
            }
            
            return "Survived battle with colony";
        }
        
        /// <summary>
        /// Determine who the grudge is against.
        /// </summary>
        private static string DetermineGrudgeTarget(Pawn pawn)
        {
            Map map = pawn.Map ?? Find.CurrentMap;
            if (map == null) return null;
            
            var colonists = map.mapPawns?.FreeColonists;
            if (colonists == null) return null;
            
            // Find colonist with relationship
            foreach (var colonist in colonists)
            {
                if (colonist.relations != null)
                {
                    var relation = colonist.relations.DirectRelations
                        .FirstOrDefault(r => r.otherPawn == pawn);
                    
                    if (relation != null)
                    {
                        return colonist.Name?.ToStringShort;
                    }
                }
            }
            
            // Default to first colonist
            var firstColonist = colonists.FirstOrDefault();
            return firstColonist?.Name?.ToStringShort;
        }
        
        /// <summary>
        /// Get active Nemesis for a faction.
        /// </summary>
        public static NemesisProfile GetActiveNemesisForFaction(Faction faction)
        {
            if (faction == null || StoryContext.Instance == null) return null;
            
            return StoryContext.Instance.GetActiveNemesisForFaction(faction);
        }
        
        /// <summary>
        /// Spawn a Nemesis pawn from profile.
        /// </summary>
        public static Pawn SpawnNemesisPawn(NemesisProfile profile, Map map = null)
        {
            if (profile == null) return null;
            if (map == null) map = Find.CurrentMap;
            if (map == null) return null;
            
            try
            {
                // Get faction
                Faction faction = null;
                if (!string.IsNullOrEmpty(profile.FactionId))
                {
                    faction = Find.FactionManager.AllFactions
                        .FirstOrDefault(f => f.def.defName == profile.FactionId);
                }
                
                if (faction == null)
                {
                    // Fallback to hostile faction
                    faction = Find.FactionManager.AllFactions
                        .FirstOrDefault(f => f.HostileTo(Faction.OfPlayer));
                }
                
                if (faction == null) return null;
                
                // Create pawn request
                var request = new PawnGenerationRequest(
                    kind: PawnKindDefOf.SpaceRefugee, // Will be overridden by faction
                    faction: faction,
                    tile: map.Tile,
                    allowAddictions: true
                );
                
                var pawn = PawnGenerator.GeneratePawn(request);
                
                // Apply appearance
                if (pawn.story != null)
                {
                    if (!string.IsNullOrEmpty(profile.BodyType))
                    {
                        var bodyType = DefDatabase<BodyTypeDef>.GetNamedSilentFail(profile.BodyType);
                        if (bodyType != null) pawn.story.bodyType = bodyType;
                    }
                    
                    if (!string.IsNullOrEmpty(profile.HeadType))
                    {
                        var headType = DefDatabase<HeadTypeDef>.GetNamedSilentFail(profile.HeadType);
                        if (headType != null) pawn.story.headType = headType;
                    }
                    
                    if (!string.IsNullOrEmpty(profile.HairDef))
                    {
                        var hairDef = DefDatabase<HairDef>.GetNamedSilentFail(profile.HairDef);
                        if (hairDef != null) pawn.story.hairDef = hairDef;
                    }
                    
                    // Note: beardDef, hairColor, skinColor restoration not available in this RimWorld version
                    // Appearance will be generated by RimWorld
                }
                
                // Note: Name is stored for reference, but RimWorld will generate a new name
                // We track Nemeses by ThingID, so the name can differ
                
                // Apply skills (approximate)
                if (pawn.skills != null && profile.Skills != null)
                {
                    foreach (var skillStr in profile.Skills)
                    {
                        var parts = skillStr.Split(':');
                        if (parts.Length == 2)
                        {
                            var skillName = parts[0].Trim();
                            if (int.TryParse(parts[1].Trim(), out int level))
                            {
                                // Find matching skill and set level
                                var skill = pawn.skills.skills.FirstOrDefault(s => 
                                    s.def.label == skillName || 
                                    s.def.defName.Contains(skillName));
                                
                                if (skill != null)
                                {
                                    skill.Level = Math.Min(level, 20);
                                }
                            }
                        }
                    }
                }
                
                // Mark as Nemesis (could use custom Hediff if needed)
                Log.Message($"[AI Narrator] Spawned Nemesis: {profile.Name} for faction {profile.FactionName}");
                
                return pawn;
            }
            catch (Exception ex)
            {
                Log.Error($"[AI Narrator] Failed to spawn Nemesis pawn: {ex.Message}");
                return null;
            }
        }
    }
}

