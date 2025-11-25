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

