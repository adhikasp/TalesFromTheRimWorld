using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Maps LLM choice consequences to game effects.
    /// Executes spawning, mood effects, faction changes, etc.
    /// </summary>
    public static class EventMapper
    {
        /// <summary>
        /// Execute a consequence from a choice option.
        /// </summary>
        public static void ExecuteConsequence(ChoiceConsequence consequence, Map map)
        {
            if (consequence == null || map == null) return;
            
            try
            {
                switch (consequence.Type?.ToLower())
                {
                    case "spawn_pawn":
                        SpawnPawn(consequence.Parameters, map);
                        break;
                        
                    case "spawn_items":
                        SpawnItems(consequence.Parameters, map);
                        break;
                        
                    case "mood_effect":
                        ApplyMoodEffect(consequence.Parameters, map);
                        break;
                        
                    case "faction_relation":
                        ChangeFactionRelation(consequence.Parameters, map);
                        break;
                        
                    case "trigger_raid":
                        TriggerRaid(consequence.Parameters, map);
                        break;
                        
                    case "nothing":
                    default:
                        // No effect
                        Log.Message("[AI Narrator] Choice had no mechanical consequence");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AI Narrator] Failed to execute consequence: {ex.Message}");
            }
        }
        
        private static void SpawnPawn(Dictionary<string, object> parameters, Map map)
        {
            string kind = GetParam<string>(parameters, "kind", "Colonist");
            
            // Find spawn location
            IntVec3 spawnLoc;
            if (!CellFinder.TryFindRandomEdgeCellWith(
                c => c.Walkable(map) && !c.Fogged(map), 
                map, 
                CellFinder.EdgeRoadChance_Neutral, 
                out spawnLoc))
            {
                spawnLoc = CellFinder.RandomEdgeCell(map);
            }
            
            // Generate pawn
            PawnKindDef pawnKind = kind.ToLower() == "refugee" 
                ? PawnKindDefOf.Refugee 
                : PawnKindDefOf.Colonist;
            
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                pawnKind,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: true,
                allowFood: true
            ));
            
            // Spawn and notify
            GenSpawn.Spawn(pawn, spawnLoc, map);
            
            // Add as colonist or guest based on kind
            if (kind.ToLower() == "refugee")
            {
                pawn.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
            }
            
            Find.LetterStack.ReceiveLetter(
                "Stranger Arrives",
                $"{pawn.Name.ToStringFull} has arrived at the colony, drawn by the narrator's tale.",
                LetterDefOf.PositiveEvent,
                new TargetInfo(pawn)
            );
            
            Log.Message($"[AI Narrator] Spawned pawn: {pawn.Name}");
        }
        
        private static void SpawnItems(Dictionary<string, object> parameters, Map map)
        {
            string itemName = GetParam<string>(parameters, "item", "Silver");
            int count = GetParam<int>(parameters, "count", 100);
            
            // Map item names to defs
            ThingDef itemDef = GetItemDef(itemName);
            if (itemDef == null)
            {
                Log.Warning($"[AI Narrator] Unknown item: {itemName}, defaulting to silver");
                itemDef = ThingDefOf.Silver;
            }
            
            // Find spawn location (center of colony)
            IntVec3 spawnLoc = map.Center;
            if (!spawnLoc.Walkable(map))
            {
                spawnLoc = CellFinder.RandomClosewalkCellNear(map.Center, map, 20);
            }
            
            // Create and spawn items
            Thing items = ThingMaker.MakeThing(itemDef);
            items.stackCount = Math.Min(count, items.def.stackLimit > 0 ? items.def.stackLimit : count);
            
            GenPlace.TryPlaceThing(items, spawnLoc, map, ThingPlaceMode.Near);
            
            Find.LetterStack.ReceiveLetter(
                "Resources Found",
                $"The narrator's guidance has led to finding {items.stackCount} {items.Label}.",
                LetterDefOf.PositiveEvent,
                new TargetInfo(items)
            );
            
            Log.Message($"[AI Narrator] Spawned {items.stackCount}x {items.Label}");
        }
        
        private static void ApplyMoodEffect(Dictionary<string, object> parameters, Map map)
        {
            string effectType = GetParam<string>(parameters, "type", "positive");
            int severity = GetParam<int>(parameters, "severity", 1);
            
            // Choose thought def based on type and severity
            ThoughtDef thoughtDef = GetMoodThought(effectType, severity);
            if (thoughtDef == null)
            {
                Log.Warning("[AI Narrator] Could not find appropriate mood thought");
                return;
            }
            
            // Apply to all colonists
            int affected = 0;
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.needs?.mood?.thoughts?.memories != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                    affected++;
                }
            }
            
            string message = effectType == "positive" 
                ? "The narrator's words bring hope to the colonists."
                : "A shadow of doubt crosses the colonists' minds.";
            
            Messages.Message(message, MessageTypeDefOf.NeutralEvent);
            
            Log.Message($"[AI Narrator] Applied mood effect to {affected} colonists");
        }
        
        private static void ChangeFactionRelation(Dictionary<string, object> parameters, Map map)
        {
            int change = GetParam<int>(parameters, "change", 0);
            
            if (change == 0) return;
            
            // Find a random non-player faction
            var factions = Find.FactionManager.AllFactionsVisible
                .Where(f => !f.IsPlayer && f.def.humanlikeFaction)
                .ToList();
            
            if (factions.Count == 0)
            {
                Log.Warning("[AI Narrator] No suitable factions for relation change");
                return;
            }
            
            Faction faction = factions.RandomElement();
            faction.TryAffectGoodwillWith(Faction.OfPlayer, change, canSendMessage: true, canSendHostilityLetter: true);
            
            Log.Message($"[AI Narrator] Changed {faction.Name} relations by {change}");
        }
        
        private static void TriggerRaid(Dictionary<string, object> parameters, Map map)
        {
            string severity = GetParam<string>(parameters, "severity", "small");
            
            // Calculate threat points
            float points = severity.ToLower() switch
            {
                "small" => StorytellerUtility.DefaultThreatPointsNow(map) * 0.5f,
                "medium" => StorytellerUtility.DefaultThreatPointsNow(map) * 0.8f,
                "large" => StorytellerUtility.DefaultThreatPointsNow(map) * 1.2f,
                _ => StorytellerUtility.DefaultThreatPointsNow(map) * 0.5f
            };
            
            points = Math.Max(points, 100f);  // Minimum threat
            
            // Find enemy faction
            Faction enemyFaction = Find.FactionManager.AllFactionsVisible
                .Where(f => f.HostileTo(Faction.OfPlayer))
                .RandomElementWithFallback();
            
            if (enemyFaction == null)
            {
                Log.Warning("[AI Narrator] No hostile faction found for raid");
                return;
            }
            
            // Create raid
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = enemyFaction,
                points = points,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn
            };
            
            IncidentDef raidDef = IncidentDefOf.RaidEnemy;
            
            if (raidDef.Worker.TryExecute(parms))
            {
                Log.Message($"[AI Narrator] Triggered {severity} raid with {points} points");
            }
            else
            {
                Log.Warning("[AI Narrator] Failed to execute raid");
            }
        }
        
        private static ThingDef GetItemDef(string itemName)
        {
            return itemName?.ToLower() switch
            {
                "silver" => ThingDefOf.Silver,
                "gold" => ThingDefOf.Gold,
                "steel" => ThingDefOf.Steel,
                "plasteel" => ThingDefOf.Plasteel,
                "component" or "components" => ThingDefOf.ComponentIndustrial,
                "advancedcomponent" or "advanced component" => ThingDefOf.ComponentSpacer,
                "medicine" => ThingDefOf.MedicineIndustrial,
                "herbal medicine" or "herbalmedicine" => ThingDefOf.MedicineHerbal,
                "food" or "meal" => ThingDefOf.MealSimple,
                "wood" => ThingDefOf.WoodLog,
                "uranium" => ThingDefOf.Uranium,
                "jade" => ThingDefOf.Jade,
                _ => DefDatabase<ThingDef>.GetNamedSilentFail(itemName)
            };
        }
        
        private static ThoughtDef GetMoodThought(string type, int severity)
        {
            // Use vanilla thoughts for mood effects
            bool positive = type?.ToLower() == "positive";
            
            // Try to find appropriate thoughts using DefDatabase lookups
            if (positive)
            {
                // Try to find a positive thought - use DefDatabase for flexibility
                ThoughtDef thought = severity switch
                {
                    >= 3 => DefDatabase<ThoughtDef>.GetNamedSilentFail("Catharsis") ?? ThoughtDefOf.GotSomeLovin,
                    2 => DefDatabase<ThoughtDef>.GetNamedSilentFail("AteFineMeal") ?? ThoughtDefOf.GotSomeLovin,
                    _ => ThoughtDefOf.GotSomeLovin
                };
                return thought ?? ThoughtDefOf.GotSomeLovin;
            }
            else
            {
                ThoughtDef thought = severity switch
                {
                    >= 3 => ThoughtDefOf.ColonistBanished ?? ThoughtDefOf.AteRawFood,
                    2 => ThoughtDefOf.AteRawFood,
                    _ => ThoughtDefOf.AteRawFood
                };
                return thought ?? ThoughtDefOf.AteRawFood;
            }
        }
        
        private static T GetParam<T>(Dictionary<string, object> parameters, string key, T defaultValue)
        {
            if (parameters == null || !parameters.ContainsKey(key))
                return defaultValue;
            
            try
            {
                object value = parameters[key];
                
                if (value is T typed)
                    return typed;
                
                if (typeof(T) == typeof(int) && value is long longVal)
                    return (T)(object)(int)longVal;
                    
                if (typeof(T) == typeof(int) && value is double doubleVal)
                    return (T)(object)(int)doubleVal;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

