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
        /// Execute a sequence of consequences from a choice option.
        /// </summary>
        public static void ExecuteConsequences(IEnumerable<ChoiceConsequence> consequences, Map map)
        {
            if (consequences == null || map == null) return;
            
            foreach (var consequence in consequences)
            {
                ExecuteConsequence(consequence, map);
            }
        }
        
        /// <summary>
        /// Execute a single consequence from a choice option.
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
                        
                    case "weather_change":
                        ChangeWeather(consequence.Parameters, map);
                        break;
                        
                    case "give_inspiration":
                        GiveInspiration(consequence.Parameters, map);
                        break;
                        
                    case "spawn_trader":
                        SpawnTrader(consequence.Parameters, map);
                        break;
                        
                    case "spawn_animal":
                        SpawnAnimal(consequence.Parameters, map);
                        break;
                        
                    case "heal_colonist":
                        HealColonist(consequence.Parameters, map);
                        break;
                        
                    case "skill_xp":
                        GrantSkillXP(consequence.Parameters, map);
                        break;
                    
                    case "trigger_incident":
                        TriggerIncident(consequence.Parameters, map);
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
            bool isRefugee = kind.ToLower() == "refugee";
            
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
            // Refugees need null faction so they can be set as guest of player
            // Colonists are generated directly as player faction members
            PawnKindDef pawnKind = isRefugee 
                ? PawnKindDefOf.Refugee 
                : PawnKindDefOf.Colonist;
            
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                pawnKind,
                isRefugee ? null : Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: true,
                allowFood: true
            ));
            
            // Spawn and notify
            GenSpawn.Spawn(pawn, spawnLoc, map);
            
            // Add as colonist or guest based on kind
            if (isRefugee)
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
            
            // Handle negative counts (removal) vs positive counts (spawning)
            if (count < 0)
            {
                RemoveItems(itemDef, -count, map);
                return;
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
        
        /// <summary>
        /// Remove items from the colony (ground, storage, pawn inventories).
        /// </summary>
        private static void RemoveItems(ThingDef itemDef, int amountToRemove, Map map)
        {
            if (itemDef == null || amountToRemove <= 0 || map == null)
                return;
            
            int remaining = amountToRemove;
            var itemsToRemove = new List<Thing>();
            
            try
            {
                // Find all items of this type in the map (on ground and in storage)
                var spawnedThings = map.listerThings.AllThings
                    .Where(t => t.def == itemDef && t.Spawned)
                    .OrderByDescending(t => t.stackCount)  // Remove from largest stacks first
                    .ToList();
                
                // Find items in pawn inventories
                var inventoryThings = new List<Thing>();
                foreach (var pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn.inventory?.innerContainer == null) continue;
                    
                    var pawnItems = pawn.inventory.innerContainer
                        .Where(t => t.def == itemDef)
                        .ToList();
                    
                    inventoryThings.AddRange(pawnItems);
                }
                
                // Combine all items, prioritizing spawned items first
                var allThings = spawnedThings.Concat(inventoryThings)
                    .OrderByDescending(t => t.stackCount)
                    .ToList();
                
                // Remove items until we've removed enough
                foreach (var thing in allThings)
                {
                    if (remaining <= 0) break;
                    
                    int toRemoveFromStack = Math.Min(remaining, thing.stackCount);
                    
                    if (toRemoveFromStack >= thing.stackCount)
                    {
                        // Remove entire stack
                        itemsToRemove.Add(thing);
                        remaining -= thing.stackCount;
                    }
                    else
                    {
                        // Split stack and remove part
                        Thing splitThing = thing.SplitOff(toRemoveFromStack);
                        if (splitThing != null)
                        {
                            splitThing.Destroy();
                            remaining -= toRemoveFromStack;
                        }
                    }
                }
                
                // Destroy all items marked for removal
                foreach (var thing in itemsToRemove)
                {
                    thing.Destroy();
                }
                
                int actuallyRemoved = amountToRemove - remaining;
                
                if (actuallyRemoved > 0)
                {
                    Find.LetterStack.ReceiveLetter(
                        "Resources Lost",
                        $"The narrator's tale has cost the colony {actuallyRemoved} {itemDef.label}.",
                        LetterDefOf.NegativeEvent
                    );
                    
                    Log.Message($"[AI Narrator] Removed {actuallyRemoved}x {itemDef.label}");
                }
                else
                {
                    Log.Warning($"[AI Narrator] Could not remove {amountToRemove}x {itemDef.label} - not enough items in colony");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AI Narrator] Error removing items: {ex.Message}");
            }
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
            string factionIdentifier = GetParam<string>(parameters, "faction", string.Empty);
            
            if (change == 0) return;
            
            // Gather all humanlike, non-player factions
            var factions = Find.FactionManager.AllFactionsVisible
                .Where(f => !f.IsPlayer && f.def.humanlikeFaction)
                .ToList();
            
            if (factions.Count == 0)
            {
                Log.Warning("[AI Narrator] No suitable factions for relation change");
                return;
            }
            
            Faction faction = null;
            if (!string.IsNullOrWhiteSpace(factionIdentifier))
            {
                faction = factions.FirstOrDefault(f =>
                    string.Equals(f.Name, factionIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.def?.defName, factionIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.def?.label, factionIdentifier, StringComparison.OrdinalIgnoreCase));
                
                if (faction == null)
                {
                    Log.Warning($"[AI Narrator] Could not find faction '{factionIdentifier}', falling back to random.");
                }
            }
            
            faction ??= factions.RandomElement();
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
        
        private static void ChangeWeather(Dictionary<string, object> parameters, Map map)
        {
            string weatherType = GetParam<string>(parameters, "weather", "clear");
            
            WeatherDef weatherDef = GetWeatherDef(weatherType);
            if (weatherDef == null)
            {
                Log.Warning($"[AI Narrator] Unknown weather: {weatherType}, defaulting to clear");
                weatherDef = WeatherDefOf.Clear;
            }
            
            map.weatherManager.TransitionTo(weatherDef);
            
            Messages.Message($"The weather shifts... {weatherDef.label}.", MessageTypeDefOf.NeutralEvent);
            
            Log.Message($"[AI Narrator] Changed weather to {weatherDef.defName}");
        }
        
        private static WeatherDef GetWeatherDef(string weatherType)
        {
            return weatherType?.ToLower() switch
            {
                "clear" => WeatherDefOf.Clear,
                "rain" or "rainy" or "rainstorm" => DefDatabase<WeatherDef>.GetNamedSilentFail("Rain") ?? WeatherDefOf.Clear,
                "fog" or "foggy" => WeatherDefOf.Fog,
                "snow" => DefDatabase<WeatherDef>.GetNamedSilentFail("SnowGentle") ?? WeatherDefOf.Clear,
                "snowhard" or "blizzard" => DefDatabase<WeatherDef>.GetNamedSilentFail("SnowHard") ?? WeatherDefOf.Clear,
                _ => DefDatabase<WeatherDef>.GetNamedSilentFail(weatherType) ?? WeatherDefOf.Clear
            };
        }
        
        private static void GiveInspiration(Dictionary<string, object> parameters, Map map)
        {
            string inspirationType = GetParam<string>(parameters, "type", "random");
            string targetName = GetParam<string>(parameters, "colonist", "");
            
            // Find target colonist
            Pawn targetPawn = null;
            if (!string.IsNullOrEmpty(targetName))
            {
                targetPawn = map.mapPawns.FreeColonists
                    .FirstOrDefault(p => p.Name.ToStringShort.ToLower() == targetName.ToLower());
            }
            
            // Fallback to random colonist capable of inspiration
            if (targetPawn == null)
            {
                targetPawn = map.mapPawns.FreeColonists
                    .Where(p => p.mindState?.inspirationHandler != null && 
                               !p.mindState.inspirationHandler.Inspired)
                    .RandomElementWithFallback();
            }
            
            if (targetPawn == null)
            {
                Log.Warning("[AI Narrator] No suitable colonist found for inspiration");
                return;
            }
            
            // Get inspiration def
            InspirationDef inspirationDef = GetInspirationDef(inspirationType);
            if (inspirationDef == null)
            {
                // Try random valid inspiration
                inspirationDef = DefDatabase<InspirationDef>.AllDefs
                    .Where(i => i.Worker.InspirationCanOccur(targetPawn))
                    .RandomElementWithFallback();
            }
            
            if (inspirationDef == null)
            {
                Log.Warning("[AI Narrator] No valid inspiration found for pawn");
                return;
            }
            
            if (targetPawn.mindState.inspirationHandler.TryStartInspiration(inspirationDef))
            {
                Log.Message($"[AI Narrator] Gave {targetPawn.Name} inspiration: {inspirationDef.defName}");
            }
            else
            {
                Log.Warning($"[AI Narrator] Failed to give inspiration to {targetPawn.Name}");
            }
        }
        
        private static InspirationDef GetInspirationDef(string type)
        {
            return type?.ToLower() switch
            {
                "shooting" or "shoot" => DefDatabase<InspirationDef>.GetNamedSilentFail("Shoot_Frenzy"),
                "melee" or "fight" => DefDatabase<InspirationDef>.GetNamedSilentFail("Frenzy_Melee"),
                "work" or "working" => DefDatabase<InspirationDef>.GetNamedSilentFail("Inspired_Creativity"),
                "craft" or "crafting" => DefDatabase<InspirationDef>.GetNamedSilentFail("Inspired_Creativity"),
                "social" or "recruitment" => DefDatabase<InspirationDef>.GetNamedSilentFail("Inspired_Recruitment"),
                "surgery" or "medical" => DefDatabase<InspirationDef>.GetNamedSilentFail("Inspired_Surgery"),
                "trade" or "trading" => DefDatabase<InspirationDef>.GetNamedSilentFail("Inspired_Trade"),
                _ => null  // Will pick random
            };
        }
        
        private static void SpawnTrader(Dictionary<string, object> parameters, Map map)
        {
            string traderType = GetParam<string>(parameters, "type", "caravan");
            
            if (traderType.ToLower() == "orbital" || traderType.ToLower() == "ship")
            {
                // Spawn orbital trader
                TraderKindDef traderKind = DefDatabase<TraderKindDef>.AllDefs
                    .Where(t => t.orbital)
                    .RandomElementWithFallback();
                
                if (traderKind != null)
                {
                    TradeShip tradeShip = new TradeShip(traderKind);
                    map.passingShipManager.AddShip(tradeShip);
                    tradeShip.GenerateThings();
                    
                    Find.LetterStack.ReceiveLetter(
                        "Trader in Orbit",
                        $"A {traderKind.label} has entered orbit, drawn by tales of your colony.",
                        LetterDefOf.PositiveEvent
                    );
                    
                    Log.Message($"[AI Narrator] Spawned orbital trader: {traderKind.label}");
                }
            }
            else
            {
                // Spawn caravan trader
                Faction faction = Find.FactionManager.AllFactionsVisible
                    .Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                    .RandomElementWithFallback();
                
                if (faction == null)
                {
                    Log.Warning("[AI Narrator] No friendly faction found for trader caravan");
                    return;
                }
                
                IncidentParms parms = new IncidentParms
                {
                    target = map,
                    faction = faction
                };
                
                IncidentDef traderIncident = IncidentDefOf.TraderCaravanArrival;
                if (traderIncident.Worker.TryExecute(parms))
                {
                    Log.Message($"[AI Narrator] Spawned trader caravan from {faction.Name}");
                }
            }
        }
        
        private static void SpawnAnimal(Dictionary<string, object> parameters, Map map)
        {
            string animalType = GetParam<string>(parameters, "animal", "random");
            string behavior = GetParam<string>(parameters, "behavior", "tame");
            int count = GetParam<int>(parameters, "count", 1);
            
            // Get animal kind
            PawnKindDef animalKind = GetAnimalKind(animalType, map);
            if (animalKind == null)
            {
                Log.Warning($"[AI Narrator] Could not find animal: {animalType}");
                return;
            }
            
            count = Math.Max(1, Math.Min(count, 5));  // Clamp to 1-5
            
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
            
            bool isManhunter = behavior.ToLower() == "manhunter" || behavior.ToLower() == "hostile";
            
            for (int i = 0; i < count; i++)
            {
                Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    animalKind,
                    isManhunter ? null : Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer,
                    map.Tile
                ));
                
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnLoc, map, 5);
                GenSpawn.Spawn(animal, loc, map);
                
                if (isManhunter)
                {
                    animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                }
            }
            
            string message = isManhunter
                ? $"A {(count > 1 ? "pack of " : "")}{animalKind.label}{(count > 1 ? "s have" : " has")} gone manhunter nearby!"
                : $"A {(count > 1 ? "group of " : "")}{animalKind.label}{(count > 1 ? "s wander" : " wanders")} into the colony, seeking refuge.";
            
            Find.LetterStack.ReceiveLetter(
                isManhunter ? "Manhunter!" : "Animals Arrive",
                message,
                isManhunter ? LetterDefOf.ThreatBig : LetterDefOf.PositiveEvent,
                new TargetInfo(spawnLoc, map)
            );
            
            Log.Message($"[AI Narrator] Spawned {count}x {animalKind.label} ({behavior})");
        }
        
        private static PawnKindDef GetAnimalKind(string animalType, Map map)
        {
            // Try specific animal first
            PawnKindDef specific = animalType?.ToLower() switch
            {
                "dog" or "husky" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Husky"),
                "cat" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Cat"),
                "wolf" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Wolf_Timber"),
                "bear" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Bear_Grizzly"),
                "boar" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Boar"),
                "deer" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Deer"),
                "chicken" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Chicken"),
                "cow" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Cow"),
                "horse" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Horse"),
                "elephant" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Elephant"),
                "thrumbo" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Thrumbo"),
                "muffalo" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Muffalo"),
                "alpaca" => DefDatabase<PawnKindDef>.GetNamedSilentFail("Alpaca"),
                _ => DefDatabase<PawnKindDef>.GetNamedSilentFail(animalType)
            };
            
            if (specific != null) return specific;
            
            // Random biome-appropriate animal
            return DefDatabase<PawnKindDef>.AllDefs
                .Where(k => k.RaceProps?.Animal == true && 
                           map.Biome.CommonalityOfAnimal(k) > 0)
                .RandomElementWithFallback() ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Rat");
        }
        
        private static void HealColonist(Dictionary<string, object> parameters, Map map)
        {
            string targetName = GetParam<string>(parameters, "colonist", "");
            string healType = GetParam<string>(parameters, "type", "injuries");
            
            // Find target colonist
            Pawn targetPawn = null;
            if (!string.IsNullOrEmpty(targetName))
            {
                targetPawn = map.mapPawns.FreeColonists
                    .FirstOrDefault(p => p.Name.ToStringShort.ToLower() == targetName.ToLower());
            }
            
            // Fallback to most injured colonist
            if (targetPawn == null)
            {
                targetPawn = map.mapPawns.FreeColonists
                    .Where(p => p.health?.hediffSet?.hediffs?.Any(h => h.def.isBad) == true)
                    .OrderByDescending(p => p.health.hediffSet.hediffs.Count(h => h.def.isBad))
                    .FirstOrDefault();
            }
            
            if (targetPawn == null)
            {
                Log.Message("[AI Narrator] No injured colonist found to heal");
                return;
            }
            
            int healed = 0;
            
            if (healType.ToLower() == "all" || healType.ToLower() == "full")
            {
                // Heal everything
                var hediffsToHeal = targetPawn.health.hediffSet.hediffs
                    .Where(h => h.def.isBad && h.def.tendable)
                    .ToList();
                
                foreach (var hediff in hediffsToHeal)
                {
                    targetPawn.health.RemoveHediff(hediff);
                    healed++;
                }
            }
            else
            {
                // Heal just injuries (not diseases)
                var injuries = targetPawn.health.hediffSet.hediffs
                    .Where(h => h is Hediff_Injury)
                    .ToList();
                
                foreach (var injury in injuries)
                {
                    targetPawn.health.RemoveHediff(injury);
                    healed++;
                }
            }
            
            if (healed > 0)
            {
                Messages.Message(
                    $"{targetPawn.Name.ToStringShort}'s wounds mend miraculously.",
                    targetPawn,
                    MessageTypeDefOf.PositiveEvent
                );
                
                Log.Message($"[AI Narrator] Healed {healed} conditions on {targetPawn.Name}");
            }
        }
        
        private static void GrantSkillXP(Dictionary<string, object> parameters, Map map)
        {
            string skillName = GetParam<string>(parameters, "skill", "random");
            int xpAmount = GetParam<int>(parameters, "amount", 5000);
            string targetName = GetParam<string>(parameters, "colonist", "");
            
            xpAmount = Math.Max(1000, Math.Min(xpAmount, 20000));  // Clamp 1k-20k
            
            // Find target colonist
            Pawn targetPawn = null;
            if (!string.IsNullOrEmpty(targetName))
            {
                targetPawn = map.mapPawns.FreeColonists
                    .FirstOrDefault(p => p.Name.ToStringShort.ToLower() == targetName.ToLower());
            }
            
            // Fallback to random colonist
            if (targetPawn == null)
            {
                targetPawn = map.mapPawns.FreeColonists.RandomElementWithFallback();
            }
            
            if (targetPawn == null)
            {
                Log.Warning("[AI Narrator] No colonist found for skill XP");
                return;
            }
            
            // Get skill
            SkillDef skillDef = GetSkillDef(skillName);
            if (skillDef == null)
            {
                // Random skill the pawn isn't incapable of
                skillDef = DefDatabase<SkillDef>.AllDefs
                    .Where(s => !targetPawn.skills.GetSkill(s).TotallyDisabled)
                    .RandomElementWithFallback();
            }
            
            if (skillDef == null)
            {
                Log.Warning("[AI Narrator] No valid skill found");
                return;
            }
            
            SkillRecord skill = targetPawn.skills.GetSkill(skillDef);
            if (skill != null && !skill.TotallyDisabled)
            {
                skill.Learn(xpAmount, direct: true);
                
                Messages.Message(
                    $"{targetPawn.Name.ToStringShort} gains insight in {skillDef.label}.",
                    targetPawn,
                    MessageTypeDefOf.PositiveEvent
                );
                
                Log.Message($"[AI Narrator] Granted {xpAmount} XP in {skillDef.defName} to {targetPawn.Name}");
            }
        }
        
        private static SkillDef GetSkillDef(string skillName)
        {
            return skillName?.ToLower() switch
            {
                "shooting" or "shoot" => SkillDefOf.Shooting,
                "melee" => SkillDefOf.Melee,
                "construction" or "building" => SkillDefOf.Construction,
                "mining" => SkillDefOf.Mining,
                "cooking" or "cook" => SkillDefOf.Cooking,
                "plants" or "growing" or "farming" => SkillDefOf.Plants,
                "animals" or "animal" or "taming" => SkillDefOf.Animals,
                "crafting" or "craft" => SkillDefOf.Crafting,
                "art" or "artistic" => SkillDefOf.Artistic,
                "medicine" or "medical" or "doctor" => SkillDefOf.Medicine,
                "social" or "talking" => SkillDefOf.Social,
                "intellectual" or "research" => SkillDefOf.Intellectual,
                _ => DefDatabase<SkillDef>.GetNamedSilentFail(skillName)
            };
        }
        
        /// <summary>
        /// Trigger any RimWorld incident by its defName.
        /// Allows triggering prebaked events like ship chunks, meteorites, quests, etc.
        /// Supports detailed parameters for specific incident customization.
        /// </summary>
        private static void TriggerIncident(Dictionary<string, object> parameters, Map map)
        {
            string incidentName = GetParam<string>(parameters, "incident", "");
            
            if (string.IsNullOrEmpty(incidentName))
            {
                Log.Warning("[AI Narrator] trigger_incident requires 'incident' parameter");
                return;
            }
            
            // Check if this is a specialized incident that needs custom handling
            string incidentLower = incidentName.ToLower();
            if (TryHandleSpecializedIncident(incidentLower, parameters, map))
            {
                return;  // Handled by specialized method
            }
            
            // Look up incident by defName
            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentName);
            if (incidentDef == null)
            {
                // Try common aliases for easier LLM usage
                incidentDef = GetIncidentByAlias(incidentName);
            }
            
            if (incidentDef == null)
            {
                Log.Warning($"[AI Narrator] Unknown incident: {incidentName}");
                return;
            }
            
            // Create incident parameters
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
            parms.forced = true;  // Force execution even if conditions aren't perfect
            
            // Set faction if specified
            string factionName = GetParam<string>(parameters, "faction", "");
            if (!string.IsNullOrEmpty(factionName))
            {
                parms.faction = Find.FactionManager.AllFactionsVisible
                    .FirstOrDefault(f => 
                        string.Equals(f.def.defName, factionName, StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(f.Name.ToString(), factionName, StringComparison.OrdinalIgnoreCase));
            }
            
            // Set threat points if specified (for raid-like incidents)
            if (parameters.ContainsKey("points"))
            {
                parms.points = GetParam<float>(parameters, "points", StorytellerUtility.DefaultThreatPointsNow(map));
            }
            
            // Set raid arrival mode if specified
            string arrival = GetParam<string>(parameters, "arrival", "");
            if (!string.IsNullOrEmpty(arrival))
            {
                parms.raidArrivalMode = GetArrivalMode(arrival);
            }
            
            // Set raid strategy if specified
            string strategy = GetParam<string>(parameters, "strategy", "");
            if (!string.IsNullOrEmpty(strategy))
            {
                parms.raidStrategy = GetRaidStrategy(strategy);
            }
            
            // Execute the incident
            if (incidentDef.Worker.CanFireNow(parms))
            {
                if (incidentDef.Worker.TryExecute(parms))
                {
                    Log.Message($"[AI Narrator] Successfully triggered incident: {incidentDef.defName}");
                }
                else
                {
                    Log.Warning($"[AI Narrator] Failed to execute incident: {incidentDef.defName}");
                }
            }
            else
            {
                // Try anyway with forced=true
                if (incidentDef.Worker.TryExecute(parms))
                {
                    Log.Message($"[AI Narrator] Forced incident execution: {incidentDef.defName}");
                }
                else
                {
                    Log.Warning($"[AI Narrator] Incident cannot fire: {incidentDef.defName}");
                }
            }
        }
        
        /// <summary>
        /// Handle specialized incidents that need custom parameter processing.
        /// Returns true if handled, false to fall through to standard handling.
        /// </summary>
        private static bool TryHandleSpecializedIncident(string incidentType, Dictionary<string, object> parameters, Map map)
        {
            switch (incidentType)
            {
                case "manhunter":
                case "manhunter_pack":
                    return TriggerManhunterPack(parameters, map);
                    
                case "meteorite":
                case "meteor":
                    return TriggerMeteorite(parameters, map);
                    
                case "disease":
                case "random_disease":
                    return TriggerDisease(parameters, map);
                    
                case "resource_pod":
                case "cargo_pod":
                case "crashedpod":
                    return TriggerResourcePod(parameters, map);
                    
                case "psychic_drone":
                    return TriggerPsychicEvent(parameters, map, false);
                    
                case "psychic_soothe":
                    return TriggerPsychicEvent(parameters, map, true);
                    
                case "item_stash":
                    return TriggerItemStash(parameters, map);
                    
                case "self_tame":
                    return TriggerSelfTame(parameters, map);
                    
                case "animal_herd":
                    return TriggerAnimalHerd(parameters, map);
                    
                default:
                    return false;  // Not a specialized incident
            }
        }
        
        /// <summary>
        /// Trigger a manhunter pack with specific animal type.
        /// </summary>
        private static bool TriggerManhunterPack(Dictionary<string, object> parameters, Map map)
        {
            string animalType = GetParam<string>(parameters, "animal", "random");
            int count = GetParam<int>(parameters, "count", 0);  // 0 = let game decide
            
            PawnKindDef animalKind = null;
            if (animalType.ToLower() != "random")
            {
                animalKind = GetAnimalKind(animalType, map);
            }
            
            // If no specific animal, pick a random manhunter-capable animal
            if (animalKind == null)
            {
                animalKind = DefDatabase<PawnKindDef>.AllDefs
                    .Where(k => k.RaceProps?.Animal == true && 
                               k.combatPower > 30 &&  // Reasonable threat
                               map.Biome.CommonalityOfAnimal(k) > 0)
                    .RandomElementWithFallback();
            }
            
            if (animalKind == null)
            {
                Log.Warning("[AI Narrator] Could not find suitable manhunter animal");
                return false;
            }
            
            // Calculate count based on combat power if not specified
            if (count <= 0)
            {
                float points = StorytellerUtility.DefaultThreatPointsNow(map) * 0.5f;
                count = Math.Max(1, (int)(points / animalKind.combatPower));
            }
            count = Math.Max(1, Math.Min(count, 20));
            
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
            
            // Spawn manhunter animals
            for (int i = 0; i < count; i++)
            {
                Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    animalKind,
                    null,
                    PawnGenerationContext.NonPlayer,
                    map.Tile
                ));
                
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnLoc, map, 10);
                GenSpawn.Spawn(animal, loc, map);
                animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
            }
            
            Find.LetterStack.ReceiveLetter(
                "Manhunter Pack!",
                $"A pack of {count} manhunting {animalKind.label}s have entered the area!",
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnLoc, map)
            );
            
            Log.Message($"[AI Narrator] Triggered manhunter pack: {count}x {animalKind.label}");
            return true;
        }
        
        /// <summary>
        /// Trigger a meteorite with specific resource type.
        /// </summary>
        private static bool TriggerMeteorite(Dictionary<string, object> parameters, Map map)
        {
            string resourceType = GetParam<string>(parameters, "resource", "random");
            
            ThingDef mineralDef = GetMeteoriteMineral(resourceType);
            if (mineralDef == null)
            {
                // Fall back to standard meteorite incident
                return false;
            }
            
            // Find impact location (prefer open areas)
            IntVec3 impactLoc = IntVec3.Invalid;
            for (int i = 0; i < 20; i++)
            {
                IntVec3 testLoc = CellFinder.RandomCell(map);
                if (testLoc.Standable(map) && !testLoc.Roofed(map) && testLoc.GetEdifice(map) == null)
                {
                    impactLoc = testLoc;
                    break;
                }
            }
            
            if (!impactLoc.IsValid)
            {
                impactLoc = CellFinder.RandomCell(map);
            }
            
            // Spawn the meteorite (building version, not mineable)
            ThingDef meteoriteDef = DefDatabase<ThingDef>.GetNamedSilentFail("فلكةMeteorite" + mineralDef.defName);
            if (meteoriteDef == null)
            {
                // Create mineral chunk directly
                int stackCount = Rand.Range(40, 80);
                Thing mineral = ThingMaker.MakeThing(mineralDef);
                mineral.stackCount = Math.Min(stackCount, mineral.def.stackLimit);
                GenPlace.TryPlaceThing(mineral, impactLoc, map, ThingPlaceMode.Near);
                
                // Visual/sound effects (simplified - just the explosion visual)
                GenExplosion.DoExplosion(impactLoc, map, 3f, DamageDefOf.Bomb, null);
            }
            
            Find.LetterStack.ReceiveLetter(
                "Meteorite!",
                $"A meteorite rich in {mineralDef.label} has crashed nearby!",
                LetterDefOf.PositiveEvent,
                new TargetInfo(impactLoc, map)
            );
            
            Log.Message($"[AI Narrator] Triggered meteorite: {mineralDef.label}");
            return true;
        }
        
        private static ThingDef GetMeteoriteMineral(string resourceType)
        {
            return resourceType?.ToLower() switch
            {
                "steel" => ThingDefOf.Steel,
                "silver" => ThingDefOf.Silver,
                "gold" => ThingDefOf.Gold,
                "plasteel" => ThingDefOf.Plasteel,
                "uranium" => ThingDefOf.Uranium,
                "jade" => ThingDefOf.Jade,
                "marble" => DefDatabase<ThingDef>.GetNamedSilentFail("BlocksMarble"),
                "granite" => DefDatabase<ThingDef>.GetNamedSilentFail("BlocksGranite"),
                "slate" => DefDatabase<ThingDef>.GetNamedSilentFail("BlocksSlate"),
                "sandstone" => DefDatabase<ThingDef>.GetNamedSilentFail("BlocksSandstone"),
                "limestone" => DefDatabase<ThingDef>.GetNamedSilentFail("BlocksLimestone"),
                "random" => new ThingDef[] { ThingDefOf.Steel, ThingDefOf.Silver, ThingDefOf.Gold, 
                    ThingDefOf.Plasteel, ThingDefOf.Uranium, ThingDefOf.Jade }.RandomElement(),
                _ => DefDatabase<ThingDef>.GetNamedSilentFail(resourceType)
            };
        }
        
        /// <summary>
        /// Trigger a disease outbreak with specific disease type.
        /// </summary>
        private static bool TriggerDisease(Dictionary<string, object> parameters, Map map)
        {
            string diseaseType = GetParam<string>(parameters, "disease", "random");
            string targetName = GetParam<string>(parameters, "target", "");
            
            // Get the disease incident def
            IncidentDef diseaseDef = GetDiseaseIncident(diseaseType);
            if (diseaseDef == null)
            {
                Log.Warning($"[AI Narrator] Unknown disease: {diseaseType}");
                return false;
            }
            
            // Find target if specified
            Pawn targetPawn = null;
            if (!string.IsNullOrEmpty(targetName))
            {
                targetPawn = map.mapPawns.FreeColonists
                    .FirstOrDefault(p => string.Equals(p.Name.ToStringShort, targetName, StringComparison.OrdinalIgnoreCase));
            }
            
            // Create parameters
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(diseaseDef.category, map);
            parms.forced = true;
            
            if (diseaseDef.Worker.TryExecute(parms))
            {
                Log.Message($"[AI Narrator] Triggered disease: {diseaseDef.defName}");
                return true;
            }
            
            return false;
        }
        
        private static IncidentDef GetDiseaseIncident(string diseaseType)
        {
            string defName = diseaseType?.ToLower() switch
            {
                "plague" => "Disease_Plague",
                "flu" or "influenza" => "Disease_Flu",
                "malaria" => "Disease_Malaria",
                "sleeping_sickness" or "sleepingsickness" => "Disease_SleepingSickness",
                "gut_worms" or "gutworms" => "Disease_GutWorms",
                "muscle_parasites" or "muscleparasites" => "Disease_MuscleParasites",
                "fibrous_mechanites" or "fibrousmechanites" => "Disease_FibrousMechanites",
                "sensory_mechanites" or "sensorymechanites" => "Disease_SensoryMechanites",
                "random" => null,  // Will pick random below
                _ => diseaseType  // Try as defName directly
            };
            
            if (defName == null)
            {
                // Pick random disease
                return DefDatabase<IncidentDef>.AllDefs
                    .Where(d => d.defName.StartsWith("Disease_"))
                    .RandomElementWithFallback();
            }
            
            return DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
        }
        
        /// <summary>
        /// Trigger a resource pod with specific contents.
        /// </summary>
        private static bool TriggerResourcePod(Dictionary<string, object> parameters, Map map)
        {
            string resourceType = GetParam<string>(parameters, "resource", "random");
            int count = GetParam<int>(parameters, "count", 0);
            
            // Get resource def
            ThingDef resourceDef = GetResourcePodContents(resourceType);
            if (resourceDef == null)
            {
                // Fall back to standard resource pod
                return false;
            }
            
            // Calculate count if not specified
            if (count <= 0)
            {
                count = resourceType.ToLower() switch
                {
                    "gold" or "plasteel" or "uranium" => Rand.Range(20, 50),
                    "component" or "medicine" => Rand.Range(10, 25),
                    "silver" => Rand.Range(100, 300),
                    _ => Rand.Range(50, 150)
                };
            }
            
            // Find drop location
            IntVec3 dropLoc = DropCellFinder.RandomDropSpot(map);
            
            // Create the item
            Thing resource = ThingMaker.MakeThing(resourceDef);
            resource.stackCount = Math.Min(count, resource.def.stackLimit > 0 ? resource.def.stackLimit : count);
            
            // Spawn items with skyfaller effect using SkyfallerMaker
            ThingDef dropPodIncoming = ThingDefOf.DropPodIncoming;
            Thing skyfaller = SkyfallerMaker.SpawnSkyfaller(dropPodIncoming, resource, dropLoc, map);
            
            Find.LetterStack.ReceiveLetter(
                "Cargo Pod",
                $"A cargo pod containing {resource.stackCount} {resource.Label} is incoming!",
                LetterDefOf.PositiveEvent,
                new TargetInfo(dropLoc, map)
            );
            
            Log.Message($"[AI Narrator] Triggered resource pod: {resource.stackCount}x {resource.Label}");
            return true;
        }
        
        private static ThingDef GetResourcePodContents(string resourceType)
        {
            return resourceType?.ToLower() switch
            {
                "silver" => ThingDefOf.Silver,
                "gold" => ThingDefOf.Gold,
                "steel" => ThingDefOf.Steel,
                "plasteel" => ThingDefOf.Plasteel,
                "medicine" => ThingDefOf.MedicineIndustrial,
                "component" or "components" => ThingDefOf.ComponentIndustrial,
                "food" or "meals" => ThingDefOf.MealSurvivalPack,
                "chemfuel" => ThingDefOf.Chemfuel,
                "neutroamine" => DefDatabase<ThingDef>.GetNamedSilentFail("Neutroamine"),
                "uranium" => ThingDefOf.Uranium,
                "random" => new ThingDef[] { ThingDefOf.Silver, ThingDefOf.Gold, ThingDefOf.Steel, 
                    ThingDefOf.Plasteel, ThingDefOf.MedicineIndustrial, ThingDefOf.ComponentIndustrial }.RandomElement(),
                _ => DefDatabase<ThingDef>.GetNamedSilentFail(resourceType)
            };
        }
        
        /// <summary>
        /// Trigger psychic drone or soothe with specific gender.
        /// </summary>
        private static bool TriggerPsychicEvent(Dictionary<string, object> parameters, Map map, bool isSoothe)
        {
            string genderStr = GetParam<string>(parameters, "gender", "random");
            
            Gender gender = genderStr?.ToLower() switch
            {
                "male" => Gender.Male,
                "female" => Gender.Female,
                _ => Rand.Bool ? Gender.Male : Gender.Female
            };
            
            string incidentName = isSoothe 
                ? (gender == Gender.Male ? "PsychicSootheMale" : "PsychicSootheFemale")
                : (gender == Gender.Male ? "PsychicDroneMale" : "PsychicDroneFemale");
            
            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentName);
            if (incidentDef == null)
            {
                // Try generic versions
                incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(isSoothe ? "PsychicSoothe" : "PsychicDrone");
            }
            
            if (incidentDef == null)
            {
                Log.Warning($"[AI Narrator] Could not find psychic incident: {incidentName}");
                return false;
            }
            
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
            parms.forced = true;
            
            if (incidentDef.Worker.TryExecute(parms))
            {
                Log.Message($"[AI Narrator] Triggered {incidentName}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Trigger item stash quest with specific item type.
        /// </summary>
        private static bool TriggerItemStash(Dictionary<string, object> parameters, Map map)
        {
            string itemType = GetParam<string>(parameters, "item", "random");
            
            // Item stash is quest-based, we'll try to trigger the quest
            IncidentDef questDef = DefDatabase<IncidentDef>.GetNamedSilentFail("Quest_ItemStash");
            if (questDef == null)
            {
                questDef = DefDatabase<IncidentDef>.AllDefs
                    .FirstOrDefault(d => d.defName.Contains("ItemStash") || d.defName.Contains("Stash"));
            }
            
            if (questDef != null)
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(questDef.category, map);
                parms.forced = true;
                
                if (questDef.Worker.TryExecute(parms))
                {
                    Log.Message($"[AI Narrator] Triggered item stash quest");
                    return true;
                }
            }
            
            // Fallback: just spawn the items directly
            ThingDef itemDef = itemType?.ToLower() switch
            {
                "weapon" => DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.IsWeapon && d.techLevel >= TechLevel.Industrial)
                    .RandomElementWithFallback(),
                "armor" => DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.IsApparel && d.statBases?.Any(s => s.stat == StatDefOf.ArmorRating_Sharp) == true)
                    .RandomElementWithFallback(),
                "artifact" => DefDatabase<ThingDef>.GetNamedSilentFail("PsychicInsanityLance") ?? 
                              DefDatabase<ThingDef>.GetNamedSilentFail("OrbitalTargeterBombardment"),
                "medicine" => ThingDefOf.MedicineUltratech,
                "drugs" => DefDatabase<ThingDef>.GetNamedSilentFail("Luciferium") ?? 
                          DefDatabase<ThingDef>.GetNamedSilentFail("GoJuice"),
                _ => ThingDefOf.Silver
            };
            
            if (itemDef != null)
            {
                Thing item = ThingMaker.MakeThing(itemDef);
                IntVec3 loc = DropCellFinder.RandomDropSpot(map);
                GenPlace.TryPlaceThing(item, loc, map, ThingPlaceMode.Near);
                
                Find.LetterStack.ReceiveLetter(
                    "Hidden Stash Found",
                    $"Your scouts have discovered a hidden cache containing {item.Label}!",
                    LetterDefOf.PositiveEvent,
                    new TargetInfo(loc, map)
                );
                
                Log.Message($"[AI Narrator] Spawned stash item: {item.Label}");
            }
            
            return true;
        }
        
        /// <summary>
        /// Trigger an animal self-taming to the colony.
        /// </summary>
        private static bool TriggerSelfTame(Dictionary<string, object> parameters, Map map)
        {
            string animalType = GetParam<string>(parameters, "animal", "random");
            
            PawnKindDef animalKind = null;
            if (animalType.ToLower() != "random")
            {
                animalKind = GetAnimalKind(animalType, map);
            }
            
            if (animalKind == null)
            {
                // Pick a random tameable animal appropriate for the biome
                animalKind = DefDatabase<PawnKindDef>.AllDefs
                    .Where(k => k.RaceProps?.Animal == true && 
                               k.RaceProps.trainability != null &&  // Can be tamed
                               map.Biome.CommonalityOfAnimal(k) > 0)
                    .RandomElementWithFallback();
            }
            
            if (animalKind == null)
            {
                Log.Warning("[AI Narrator] Could not find suitable animal for self-tame");
                return false;
            }
            
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
            
            Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                animalKind,
                Faction.OfPlayer,  // Directly join player faction
                PawnGenerationContext.NonPlayer,
                map.Tile
            ));
            
            GenSpawn.Spawn(animal, spawnLoc, map);
            
            // Set training if possible
            if (animal.training != null)
            {
                foreach (TrainableDef trainable in DefDatabase<TrainableDef>.AllDefs)
                {
                    if (animal.training.CanBeTrained(trainable))
                    {
                        animal.training.Train(trainable, null, complete: true);
                    }
                }
            }
            
            Find.LetterStack.ReceiveLetter(
                "Animal Self-Tamed",
                $"A wild {animalKind.label} has wandered into the colony and bonded with your colonists!",
                LetterDefOf.PositiveEvent,
                new TargetInfo(animal)
            );
            
            Log.Message($"[AI Narrator] Triggered self-tame: {animalKind.label}");
            return true;
        }
        
        /// <summary>
        /// Trigger an animal herd passing through.
        /// </summary>
        private static bool TriggerAnimalHerd(Dictionary<string, object> parameters, Map map)
        {
            string animalType = GetParam<string>(parameters, "animal", "random");
            int count = GetParam<int>(parameters, "count", 0);
            
            PawnKindDef animalKind = null;
            if (animalType.ToLower() != "random")
            {
                animalKind = GetAnimalKind(animalType, map);
            }
            
            if (animalKind == null)
            {
                // Pick a herd animal
                animalKind = new string[] { "Muffalo", "Deer", "Elk", "Caribou", "Alpaca", "Ibex" }
                    .Select(name => DefDatabase<PawnKindDef>.GetNamedSilentFail(name))
                    .Where(k => k != null)
                    .RandomElementWithFallback();
            }
            
            if (animalKind == null)
            {
                Log.Warning("[AI Narrator] Could not find suitable herd animal");
                return false;
            }
            
            if (count <= 0)
            {
                count = Rand.Range(5, 12);
            }
            count = Math.Max(3, Math.Min(count, 20));
            
            // Find spawn location at edge
            IntVec3 spawnLoc;
            if (!CellFinder.TryFindRandomEdgeCellWith(
                c => c.Walkable(map) && !c.Fogged(map),
                map,
                CellFinder.EdgeRoadChance_Neutral,
                out spawnLoc))
            {
                spawnLoc = CellFinder.RandomEdgeCell(map);
            }
            
            // Spawn herd
            for (int i = 0; i < count; i++)
            {
                Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    animalKind,
                    null,
                    PawnGenerationContext.NonPlayer,
                    map.Tile
                ));
                
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnLoc, map, 10);
                GenSpawn.Spawn(animal, loc, map);
            }
            
            Find.LetterStack.ReceiveLetter(
                "Herd Migration",
                $"A herd of {count} {animalKind.label}s is passing through the area.",
                LetterDefOf.NeutralEvent,
                new TargetInfo(spawnLoc, map)
            );
            
            Log.Message($"[AI Narrator] Triggered animal herd: {count}x {animalKind.label}");
            return true;
        }
        
        /// <summary>
        /// Get raid arrival mode from string.
        /// </summary>
        private static PawnsArrivalModeDef GetArrivalMode(string arrival)
        {
            return arrival?.ToLower() switch
            {
                "edge" or "walk" => PawnsArrivalModeDefOf.EdgeWalkIn,
                "drop" or "droppod" or "droppods" => PawnsArrivalModeDefOf.EdgeDrop,
                "center" or "centerdrop" => PawnsArrivalModeDefOf.CenterDrop,
                "tunnel" or "sappers" => DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail("EdgeWalkInGroups"),
                "breach" => DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail("EdgeWalkInGroups"),
                _ => PawnsArrivalModeDefOf.EdgeWalkIn
            };
        }
        
        /// <summary>
        /// Get raid strategy from string.
        /// </summary>
        private static RaidStrategyDef GetRaidStrategy(string strategy)
        {
            return strategy?.ToLower() switch
            {
                "attack" or "immediate" => RaidStrategyDefOf.ImmediateAttack,
                "siege" => DefDatabase<RaidStrategyDef>.GetNamedSilentFail("Siege"),
                "kidnap" or "steal" => DefDatabase<RaidStrategyDef>.GetNamedSilentFail("ImmediateAttackSmart"),
                "sapper" or "sappers" => DefDatabase<RaidStrategyDef>.GetNamedSilentFail("ImmediateAttackSappers"),
                _ => RaidStrategyDefOf.ImmediateAttack
            };
        }
        
        /// <summary>
        /// Maps common incident names/aliases to IncidentDefs for easier LLM usage.
        /// Comprehensive mapping for all common RimWorld incidents.
        /// </summary>
        private static IncidentDef GetIncidentByAlias(string alias)
        {
            return alias?.ToLower() switch
            {
                // === THREATS ===
                "raid" or "enemy_raid" => IncidentDefOf.RaidEnemy,
                "infestation" => IncidentDefOf.Infestation,
                "mech_cluster" or "mechcluster" or "mechanoid_cluster" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("MechCluster"),
                "ship_part_crash" or "psychic_ship" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("PsychicEmanatorShipPartCrash") ??
                    DefDatabase<IncidentDef>.GetNamedSilentFail("DefoliatorShipPartCrash"),
                
                // === WEATHER/ENVIRONMENT ===
                "flashstorm" or "flash_storm" => DefDatabase<IncidentDef>.GetNamedSilentFail("Flashstorm"),
                "tornado" => DefDatabase<IncidentDef>.GetNamedSilentFail("Tornado"),
                "volcanic_winter" or "volcanicwinter" => DefDatabase<IncidentDef>.GetNamedSilentFail("VolcanicWinter"),
                "toxic_fallout" or "toxicfallout" => DefDatabase<IncidentDef>.GetNamedSilentFail("ToxicFallout"),
                "cold_snap" or "coldsnap" => DefDatabase<IncidentDef>.GetNamedSilentFail("ColdSnap"),
                "heat_wave" or "heatwave" => DefDatabase<IncidentDef>.GetNamedSilentFail("HeatWave"),
                "eclipse" or "solar_eclipse" => DefDatabase<IncidentDef>.GetNamedSilentFail("Eclipse"),
                "aurora" or "aurora_borealis" => DefDatabase<IncidentDef>.GetNamedSilentFail("Aurora"),
                "solar_flare" or "solarflare" => DefDatabase<IncidentDef>.GetNamedSilentFail("SolarFlare"),
                
                // === RESOURCES ===
                "ship_chunk" or "shipchunk" or "ship_chunk_drop" => IncidentDefOf.ShipChunkDrop,
                "ambrosia_sprout" or "ambrosia" => DefDatabase<IncidentDef>.GetNamedSilentFail("AmbrosiaSprout"),
                
                // === ANIMALS ===
                "farm_animals_wander_in" or "farm_animals" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("FarmAnimalsWanderIn"),
                "thrumbo_pass" or "thrumbos" => DefDatabase<IncidentDef>.GetNamedSilentFail("ThrumboPasses"),
                "alphabeavers" or "alpha_beavers" => DefDatabase<IncidentDef>.GetNamedSilentFail("Alphabeavers"),
                
                // === VISITORS ===
                "visitor_group" or "visitors" => IncidentDefOf.VisitorGroup,
                "trader_caravan" or "trader" => IncidentDefOf.TraderCaravanArrival,
                "orbital_trader" or "orbital_trader_arrival" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("OrbitalTraderArrival"),
                
                // === JOINERS ===
                "wanderer_joins" or "wanderer" or "wanderer_join" => IncidentDefOf.WandererJoin,
                "refugee_chased" or "refugee" => DefDatabase<IncidentDef>.GetNamedSilentFail("RefugeeChased"),
                "refugee_pod" or "escape_pod" => DefDatabase<IncidentDef>.GetNamedSilentFail("RefugeePodCrash"),
                "traveler_wounded" or "wounded_traveler" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("TravelerWounded"),
                "prisoner_rescue" or "prisoner_wildman" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("PrisonerWildsCapture"),
                
                // === QUESTS/OPPORTUNITIES ===
                "peace_talks" or "peacetalks" => DefDatabase<IncidentDef>.GetNamedSilentFail("Quest_PeaceTalks"),
                "trade_request" or "traderequest" => DefDatabase<IncidentDef>.GetNamedSilentFail("Quest_TradeRequest"),
                "bandit_camp" or "banditcamp" => DefDatabase<IncidentDef>.GetNamedSilentFail("Quest_BanditCamp"),
                "ancient_danger_revealed" or "ancient_danger" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("Quest_AncientDangerRevealed"),
                "caravan_request" or "caravanrequest" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("CaravanRequest"),
                "quest" or "random_quest" => DefDatabase<IncidentDef>.AllDefs
                    .Where(d => d.defName.StartsWith("Quest_"))
                    .RandomElementWithFallback(),
                
                // === MISC ===
                "crop_blight" or "blight" => DefDatabase<IncidentDef>.GetNamedSilentFail("CropBlight"),
                "short_circuit" or "shortcircuit" or "zzzt" => 
                    DefDatabase<IncidentDef>.GetNamedSilentFail("ShortCircuit"),
                "animal_disease" or "animal_plague" => DefDatabase<IncidentDef>.AllDefs
                    .Where(d => d.defName.Contains("Disease") && d.defName.Contains("Animal"))
                    .RandomElementWithFallback(),
                
                _ => null
            };
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
                
                // Handle numeric type conversions common in JSON
                if (typeof(T) == typeof(int))
                {
                    if (value is long longVal)
                        return (T)(object)(int)longVal;
                    if (value is double doubleVal)
                        return (T)(object)(int)doubleVal;
                    if (value is float floatVal)
                        return (T)(object)(int)floatVal;
                }
                
                if (typeof(T) == typeof(float))
                {
                    if (value is double doubleVal)
                        return (T)(object)(float)doubleVal;
                    if (value is int intVal)
                        return (T)(object)(float)intVal;
                    if (value is long longVal)
                        return (T)(object)(float)longVal;
                }
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

