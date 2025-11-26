using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace AINarrator
{
    /// <summary>
    /// Collects comprehensive colony state information for LLM context.
    /// Gathers pawns, resources, events, environment, and historical data.
    /// </summary>
    public static class ColonyStateCollector
    {
        /// <summary>
        /// Get a comprehensive snapshot of the current colony state.
        /// </summary>
        public static ColonySnapshot GetSnapshot()
        {
            Map map = Find.CurrentMap;
            if (map == null) return new ColonySnapshot();
            
            var snapshot = new ColonySnapshot
            {
                // Basic colony info
                ColonyName = GetColonyName(map),
                ColonyAgeDays = GenDate.DaysPassed,
                Season = GenLocalDate.Season(map).Label(),
                Biome = map.Biome?.label ?? "unknown",
                Quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile).x).Label(),
                Year = GenLocalDate.Year(map),
                
                // Wealth and threat
                WealthTotal = (int)map.wealthWatcher.WealthTotal,
                ThreatPoints = StorytellerUtility.DefaultThreatPointsNow(map),
                
                // Population counts
                ColonistCount = map.mapPawns.FreeColonistsCount,
                PrisonerCount = map.mapPawns.PrisonersOfColonyCount,
                
                // Recent events from StoryContext
                RecentEvents = StoryContext.Instance?.RecentEvents?.ToList() ?? new List<string>(),
                
                // Comprehensive colonist details
                ColonistDetails = GetColonistDetails(map),
                
                // Social interactions and activities
                RecentInteractions = GetRecentInteractions(map),
                RecentActivities = GetRecentActivities(map),
                
                // Environment
                Environment = GetEnvironmentContext(map),
                
                // Resources
                Resources = GetDetailedResources(map),
                
                // Faction relations
                FactionRelations = GetAllFactionRelations(),
                
                // Prisoners
                Prisoners = GetPrisonerDetails(map),
                
                // Animals
                Animals = GetColonyAnimals(map),
                
                // Active threats
                ActiveThreats = GetActiveThreats(map),
                
                // Notable items
                NotableItems = GetNotableItems(map),
                
                // Infrastructure
                Infrastructure = GetInfrastructure(map),
                
                // Historical data from StoryContext
                DeathRecords = StoryContext.Instance?.ColonistDeaths?.Select(d => d.ToString()).ToList() ?? new List<string>(),
                BattleHistory = StoryContext.Instance?.MajorBattles?.Select(b => b.ToString()).ToList() ?? new List<string>()
            };
            
            // Legacy colonists list for backward compatibility
            snapshot.Colonists = snapshot.ColonistDetails.Select(c => c.ToShortSummary()).ToList();
            
            return snapshot;
        }
        
        /// <summary>
        /// Get detailed context for event narration.
        /// Uses shared ContextFormatter for consistent output.
        /// </summary>
        public static string GetNarrationContext(IncidentParms parms = null, IncidentDef incidentDef = null)
        {
            var snapshot = GetSnapshot();
            var eventInfo = EventInfo.FromIncident(incidentDef, parms);
            return ContextFormatter.FormatNarrationContext(snapshot, eventInfo);
        }
        
        /// <summary>
        /// Get context specifically for choice events.
        /// Uses shared ContextFormatter for consistent output.
        /// </summary>
        public static string GetChoiceContext()
        {
            var snapshot = GetSnapshot();
            return ContextFormatter.FormatChoiceContext(snapshot);
        }
        
        #region Helper Methods
        
        /// <summary>
        /// TakeLast helper for .NET Framework 4.7.2 compatibility.
        /// </summary>
        private static IEnumerable<T> TakeLast<T>(IList<T> list, int count)
        {
            if (list == null || count <= 0) return Enumerable.Empty<T>();
            int skip = Math.Max(0, list.Count - count);
            return list.Skip(skip);
        }
        
        private static string GetColonyName(Map map)
        {
            if (map?.Parent is Settlement settlement)
            {
                return settlement.Label;
            }
            
            var faction = Faction.OfPlayer;
            return faction?.Name ?? "The Colony";
        }
        
        #endregion
        
        #region Colonist Collection
        
        private static List<ColonistDetail> GetColonistDetails(Map map)
        {
            var colonists = new List<ColonistDetail>();
            
            // Create a snapshot to avoid "Collection was modified" errors
            var freeColonists = map.mapPawns.FreeColonists.ToList();
            foreach (var pawn in freeColonists)
            {
                var detail = new ColonistDetail
                {
                    Name = pawn.Name.ToStringFull,
                    ShortName = pawn.Name.ToStringShort,
                    Gender = pawn.gender.GetLabel(),
                    Age = pawn.ageTracker.AgeBiologicalYears,
                    Role = GetPawnRole(pawn),
                    
                    // Backstories
                    ChildhoodBackstory = pawn.story?.Childhood?.TitleFor(pawn.gender).CapitalizeFirst() ?? "",
                    AdulthoodBackstory = pawn.story?.Adulthood?.TitleFor(pawn.gender).CapitalizeFirst() ?? "",
                    
                    // All traits (not just notable ones)
                    Traits = GetAllTraits(pawn),
                    
                    // Health
                    HealthPercent = (int)((pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f) * 100),
                    HealthStatus = GetHealthStatus(pawn),
                    Injuries = GetCurrentInjuries(pawn),
                    
                    // Mental state
                    MoodPercent = (int)((pawn.needs?.mood?.CurLevel ?? 0.5f) * 100),
                    MentalState = GetMentalState(pawn),
                    Inspiration = GetInspiration(pawn),
                    
                    // Skills (top 5)
                    TopSkills = GetTopSkills(pawn),
                    
                    // Relationships
                    Relationships = GetColonistRelationships(pawn),
                    
                    // Current activity
                    CurrentActivity = GetCurrentActivity(pawn)
                };
                
                colonists.Add(detail);
            }
            
            return colonists;
        }
        
        private static string GetPawnRole(Pawn pawn)
        {
            var skills = pawn.skills?.skills;
            if (skills == null) return "colonist";
            
            var bestSkill = skills
                .Where(s => s.passion != Passion.None || s.Level >= 8)
                .OrderByDescending(s => s.Level + (s.passion == Passion.Major ? 3 : s.passion == Passion.Minor ? 1 : 0))
                .FirstOrDefault();
            
            if (bestSkill != null && bestSkill.Level >= 5)
            {
                return GetRoleFromSkill(bestSkill.def);
            }
            
            return "colonist";
        }
        
        private static string GetRoleFromSkill(SkillDef skill)
        {
            if (skill == SkillDefOf.Medicine) return "medic";
            if (skill == SkillDefOf.Shooting) return "shooter";
            if (skill == SkillDefOf.Melee) return "fighter";
            if (skill == SkillDefOf.Construction) return "builder";
            if (skill == SkillDefOf.Cooking) return "cook";
            if (skill == SkillDefOf.Plants) return "farmer";
            if (skill == SkillDefOf.Mining) return "miner";
            if (skill == SkillDefOf.Crafting) return "crafter";
            if (skill == SkillDefOf.Animals) return "handler";
            if (skill == SkillDefOf.Artistic) return "artist";
            if (skill == SkillDefOf.Social) return "negotiator";
            if (skill == SkillDefOf.Intellectual) return "researcher";
            return "colonist";
        }
        
        private static List<string> GetAllTraits(Pawn pawn)
        {
            var traits = pawn.story?.traits?.allTraits;
            if (traits == null || traits.Count == 0) return new List<string>();
            
            return traits.Select(t => t.LabelCap).ToList();
        }
        
        private static string GetHealthStatus(Pawn pawn)
        {
            if (pawn.health?.State == PawnHealthState.Dead) return "dead";
            if (pawn.health?.Downed == true) return "incapacitated";
            
            var summaryHealth = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
            if (summaryHealth < 0.5f) return "severely injured";
            if (summaryHealth < 0.8f) return "injured";
            
            return "healthy";
        }
        
        private static List<string> GetCurrentInjuries(Pawn pawn)
        {
            var injuries = new List<string>();
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return injuries;
            
            foreach (var hediff in hediffs)
            {
                // Skip natural body parts and minor things
                if (hediff is Hediff_MissingPart missing)
                {
                    injuries.Add($"Missing {missing.Part.Label}");
                }
                else if (hediff is Hediff_Injury injury && injury.Severity > 5)
                {
                    injuries.Add($"{injury.Label} on {injury.Part?.Label ?? "body"}");
                }
                else if (hediff.def.isBad && hediff.Visible && !(hediff is Hediff_Injury))
                {
                    injuries.Add(hediff.Label);
                }
            }
            
            return injuries.Take(5).ToList();
        }
        
        private static string GetMentalState(Pawn pawn)
        {
            if (pawn.InMentalState)
            {
                return pawn.MentalState?.def?.label ?? "disturbed";
            }
            
            // Check mood danger level
            var mood = pawn.needs?.mood?.CurLevel ?? 0.5f;
            if (mood < 0.1f) return "about to break";
            if (mood < 0.2f) return "very stressed";
            if (mood < 0.3f) return "stressed";
            
            return "stable";
        }
        
        private static string GetInspiration(Pawn pawn)
        {
            if (pawn.Inspired)
            {
                return pawn.InspirationDef?.label ?? "inspired";
            }
            return "";
        }
        
        private static List<string> GetTopSkills(Pawn pawn)
        {
            var skills = pawn.skills?.skills;
            if (skills == null) return new List<string>();
            
            return skills
                .Where(s => s.Level > 0)
                .OrderByDescending(s => s.Level)
                .Take(5)
                .Select(s => $"{s.def.label} {s.Level}" + (s.passion == Passion.Major ? " (passionate)" : s.passion == Passion.Minor ? " (interested)" : ""))
                .ToList();
        }
        
        private static string GetCurrentActivity(Pawn pawn)
        {
            var job = pawn.jobs?.curJob;
            if (job == null) return "idle";
            
            try
            {
                return job.GetReport(pawn)?.CapitalizeFirst() ?? job.def?.reportString ?? "busy";
            }
            catch
            {
                return job.def?.label ?? "busy";
            }
        }
        
        #endregion
        
        #region Relationships
        
        private static List<string> GetColonistRelationships(Pawn pawn)
        {
            var relationships = new List<string>();
            var relations = pawn.relations?.DirectRelations;
            if (relations == null) return relationships;
            
            foreach (var relation in relations)
            {
                if (relation.otherPawn == null) continue;
                
                string otherName = relation.otherPawn.Name?.ToStringShort ?? "someone";
                string relType = relation.def?.label ?? "related";
                bool isAlive = !relation.otherPawn.Dead;
                bool isOnMap = relation.otherPawn.Map == pawn.Map;
                
                string status = isAlive ? (isOnMap ? "" : " (away)") : " (deceased)";
                relationships.Add($"{relType} to {otherName}{status}");
            }
            
                // Also get opinion-based relations (rivals, friends)
                // Create a snapshot to avoid "Collection was modified" errors
                var mapColonists = pawn.Map?.mapPawns?.FreeColonists?.ToList() ?? new List<Pawn>();
                foreach (var other in mapColonists)
            {
                if (other == pawn) continue;
                
                int opinion = pawn.relations?.OpinionOf(other) ?? 0;
                if (opinion >= 80)
                {
                    relationships.Add($"close friend with {other.Name.ToStringShort}");
                }
                else if (opinion >= 40)
                {
                    relationships.Add($"friend with {other.Name.ToStringShort}");
                }
                else if (opinion <= -80)
                {
                    relationships.Add($"hates {other.Name.ToStringShort}");
                }
                else if (opinion <= -40)
                {
                    relationships.Add($"rival of {other.Name.ToStringShort}");
                }
            }
            
            return relationships.Take(10).ToList();
        }
        
        #endregion
        
        #region Social Interactions
        
        private static List<string> GetRecentInteractions(Map map)
        {
            var interactions = new List<string>();
            
            try
            {
                // Get from play log - use ToGameStringFromPOV for text extraction
                var playLog = Find.PlayLog?.AllEntries;
                if (playLog == null) return interactions;
                
                int recentTicks = GenDate.TicksPerDay * 3; // Last 3 days
                int currentTick = Find.TickManager.TicksGame;
                
                foreach (var entry in playLog.Take(50))
                {
                    if (currentTick - entry.Tick > recentTicks) break;
                    
                    // Social interactions - use the text representation
                    if (entry is PlayLogEntry_Interaction)
                    {
                        try
                        {
                            string text = entry.ToGameStringFromPOV(null);
                            if (!string.IsNullOrEmpty(text) && text.Length < 150)
                            {
                                interactions.Add(text);
                            }
                        }
                        catch { /* Ignore failed conversions */ }
                    }
                }
                
                // Also check recent thoughts for social events
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var pawn in map.mapPawns.FreeColonists.ToList().Take(8))
                {
                    var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
                    if (memories == null) continue;
                    
                    foreach (var memory in memories.Take(10))
                    {
                        if (memory.otherPawn != null && memory.age < GenDate.TicksPerDay * 2)
                        {
                            string thoughtLabel = memory.LabelCap;
                            if (!string.IsNullOrEmpty(thoughtLabel))
                            {
                                // Filter for interesting social thoughts
                                string defName = memory.def?.defName ?? "";
                                if (defName.Contains("Insulted") || defName.Contains("SlightedMe") ||
                                    defName.Contains("Romance") || defName.Contains("Social") ||
                                    defName.Contains("RebuffedMyRomance") || defName.Contains("GotSome") ||
                                    defName.Contains("DeepTalk") || defName.Contains("KindWords"))
                                {
                                    interactions.Add($"{pawn.Name.ToStringShort}: {thoughtLabel}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting interactions: {ex.Message}");
            }
            
            return interactions.Distinct().Take(10).ToList();
        }
        
        private static List<string> GetRecentActivities(Map map)
        {
            var activities = new List<string>();
            
            try
            {
                var playLog = Find.PlayLog?.AllEntries;
                if (playLog == null) return activities;
                
                int recentTicks = GenDate.TicksPerDay * 3;
                int currentTick = Find.TickManager.TicksGame;
                
                foreach (var entry in playLog.Take(100))
                {
                    if (currentTick - entry.Tick > recentTicks) break;
                    
                    // Use ToGameStringFromPOV for all battle/activity entries
                    try
                    {
                        string text = entry.ToGameStringFromPOV(null);
                        if (!string.IsNullOrEmpty(text) && text.Length < 100)
                        {
                            // Filter for combat-related entries
                            if (entry.GetType().Name.Contains("Battle") || 
                                entry.GetType().Name.Contains("State"))
                            {
                                activities.Add(text);
                            }
                        }
                    }
                    catch { /* Ignore failed conversions */ }
                }
                
                // Check for recent notable actions from current jobs
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var pawn in map.mapPawns.FreeColonists.ToList())
                {
                    var curJob = pawn.jobs?.curJob;
                    if (curJob != null)
                    {
                        string jobDef = curJob.def?.defName ?? "";
                        if (jobDef == "Hunt" || jobDef == "AttackMelee" || jobDef == "AttackStatic")
                        {
                            activities.Add($"{pawn.Name.ToStringShort} is in combat");
                        }
                        else if (jobDef == "TendPatient" || jobDef == "TendSelf")
                        {
                            activities.Add($"{pawn.Name.ToStringShort} is tending wounds");
                        }
                        else if (jobDef == "Rescue")
                        {
                            activities.Add($"{pawn.Name.ToStringShort} is rescuing someone");
                        }
                        else if (jobDef == "PrisonerAttemptRecruit")
                        {
                            activities.Add($"{pawn.Name.ToStringShort} is attempting to recruit a prisoner");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting activities: {ex.Message}");
            }
            
            return activities.Distinct().Take(10).ToList();
        }
        
        #endregion
        
        #region Environment
        
        private static EnvironmentInfo GetEnvironmentContext(Map map)
        {
            try
            {
                var info = new EnvironmentInfo
                {
                    // Weather
                    Weather = map.weatherManager?.curWeather?.label ?? "clear",
                    
                    // Time of day
                    TimeOfDay = GetTimeOfDay(map),
                    
                    // Temperature
                    Temperature = GetTemperatureString(map),
                    
                    // Active conditions
                    ActiveConditions = GetActiveConditions(map)
                };
                
                return info;
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting environment: {ex.Message}");
                return new EnvironmentInfo();
            }
        }
        
        private static string GetTimeOfDay(Map map)
        {
            float hour = GenLocalDate.HourFloat(map);
            if (hour >= 5 && hour < 9) return "early morning";
            if (hour >= 9 && hour < 12) return "morning";
            if (hour >= 12 && hour < 14) return "midday";
            if (hour >= 14 && hour < 17) return "afternoon";
            if (hour >= 17 && hour < 20) return "evening";
            if (hour >= 20 && hour < 23) return "night";
            return "late night";
        }
        
        private static string GetTemperatureString(Map map)
        {
            float temp = map.mapTemperature.OutdoorTemp;
            string celsius = $"{temp:F0}°C";
            
            if (temp < -20) return $"{celsius} (freezing)";
            if (temp < 0) return $"{celsius} (cold)";
            if (temp < 15) return $"{celsius} (cool)";
            if (temp < 25) return $"{celsius} (comfortable)";
            if (temp < 35) return $"{celsius} (warm)";
            if (temp < 45) return $"{celsius} (hot)";
            return $"{celsius} (extreme heat)";
        }
        
        private static List<string> GetActiveConditions(Map map)
        {
            var conditions = new List<string>();
            
            // Create a snapshot to avoid "Collection was modified" errors
            foreach (var condition in map.gameConditionManager.ActiveConditions.ToList())
            {
                conditions.Add(condition.Label);
            }
            
            return conditions;
        }
        
        #endregion
        
        #region Resources
        
        private static Dictionary<string, string> GetDetailedResources(Map map)
        {
            var resources = new Dictionary<string, string>();
            
            try
            {
                // Key resources
                resources["Silver"] = CountResource(map, ThingDefOf.Silver).ToString("N0");
                resources["Gold"] = CountResource(map, ThingDefOf.Gold).ToString("N0");
                resources["Steel"] = CountResource(map, ThingDefOf.Steel).ToString("N0");
                resources["Plasteel"] = CountResource(map, ThingDefOf.Plasteel).ToString("N0");
                resources["Uranium"] = CountResource(map, ThingDefOf.Uranium).ToString("N0");
                resources["Components"] = CountResource(map, ThingDefOf.ComponentIndustrial).ToString("N0");
                resources["Advanced Components"] = CountResource(map, ThingDefOf.ComponentSpacer).ToString("N0");
                
                // Food
                float foodDays = map.resourceCounter.TotalHumanEdibleNutrition / 
                                (map.mapPawns.FreeColonistsCount * 1.6f + 0.1f);
                resources["Food"] = $"{foodDays:F1} days worth";
                
                // Medicine
                int medCount = 0;
                foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine))
                {
                    medCount += thing.stackCount;
                }
                resources["Medicine"] = medCount.ToString("N0");
                
                // Chemfuel/Power
                resources["Chemfuel"] = CountResource(map, ThingDefOf.Chemfuel).ToString("N0");
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting resources: {ex.Message}");
            }
            
            return resources;
        }
        
        private static int CountResource(Map map, ThingDef def)
        {
            int count = 0;
            foreach (var thing in map.listerThings.ThingsOfDef(def))
            {
                count += thing.stackCount;
            }
            return count;
        }
        
        #endregion
        
        #region Factions
        
        private static List<FactionRelationInfo> GetAllFactionRelations()
        {
            var relations = new List<FactionRelationInfo>();
            
            try
            {
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var faction in Find.FactionManager.AllFactionsVisible.ToList())
                {
                    if (faction.IsPlayer) continue;
                    
                    var info = new FactionRelationInfo
                    {
                        Name = faction.Name,
                        FactionType = faction.def?.label ?? "unknown",
                        Goodwill = faction.GoodwillWith(Faction.OfPlayer),
                        IsHostile = faction.HostileTo(Faction.OfPlayer),
                        RelationType = GetRelationType(faction)
                    };
                    
                    relations.Add(info);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting factions: {ex.Message}");
            }
            
            return relations;
        }
        
        private static string GetRelationType(Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer)) return "hostile";
            
            var goodwill = faction.GoodwillWith(Faction.OfPlayer);
            if (goodwill >= 75) return "allied";
            if (goodwill >= 50) return "warm";
            if (goodwill >= 0) return "neutral";
            if (goodwill >= -50) return "cold";
            return "hostile";
        }
        
        #endregion
        
        #region Prisoners
        
        private static List<PrisonerInfo> GetPrisonerDetails(Map map)
        {
            var prisoners = new List<PrisonerInfo>();
            
            try
            {
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var pawn in map.mapPawns.PrisonersOfColony.ToList())
                {
                    var resistance = pawn.guest?.resistance ?? 0f;
                    var info = new PrisonerInfo
                    {
                        Name = pawn.Name?.ToStringFull ?? "Unknown",
                        OriginalFaction = pawn.Faction?.Name ?? pawn.HostFaction?.Name ?? "unknown",
                        HealthPercent = (int)((pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f) * 100),
                        RecruitDifficulty = resistance.ToString("F0"),
                        MoodPercent = (int)((pawn.needs?.mood?.CurLevel ?? 0.5f) * 100)
                    };
                    
                    prisoners.Add(info);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting prisoners: {ex.Message}");
            }
            
            return prisoners;
        }
        
        #endregion
        
        #region Animals
        
        private static List<string> GetColonyAnimals(Map map)
        {
            var animals = new List<string>();
            
            try
            {
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var animal in map.mapPawns.SpawnedColonyAnimals.ToList())
                {
                    string animalInfo = animal.LabelCap;
                    
                    // Check for bond
                    var master = animal.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond);
                    if (master != null)
                    {
                        animalInfo += $" (bonded to {master.Name.ToStringShort})";
                    }
                    
                    // Check training
                    var training = animal.training;
                    if (training != null)
                    {
                        var trainedAbilities = new List<string>();
                        if (training.HasLearned(TrainableDefOf.Obedience)) trainedAbilities.Add("obedient");
                        if (training.HasLearned(TrainableDefOf.Release)) trainedAbilities.Add("attack-trained");
                        
                        // Check for hauling capability via trainability
                        try
                        {
                            var haulDef = DefDatabase<TrainableDef>.GetNamedSilentFail("Haul");
                            if (haulDef != null && training.HasLearned(haulDef))
                                trainedAbilities.Add("hauls");
                            
                            var rescueDef = DefDatabase<TrainableDef>.GetNamedSilentFail("Rescue");
                            if (rescueDef != null && training.HasLearned(rescueDef))
                                trainedAbilities.Add("rescues");
                        }
                        catch { /* Ignore if defs don't exist */ }
                        
                        if (trainedAbilities.Any())
                        {
                            animalInfo += $" [{string.Join(", ", trainedAbilities)}]";
                        }
                    }
                    
                    animals.Add(animalInfo);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting animals: {ex.Message}");
            }
            
            return animals;
        }
        
        #endregion
        
        #region Threats
        
        private static List<string> GetActiveThreats(Map map)
        {
            var threats = new List<string>();
            
            try
            {
                // Check for hostile pawns on map
                var hostilePawns = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.HostileTo(Faction.OfPlayer) && !p.Dead && !p.Downed)
                    .ToList();
                
                if (hostilePawns.Any())
                {
                    var groupedByFaction = hostilePawns.GroupBy(p => p.Faction?.Name ?? "unknown");
                    foreach (var group in groupedByFaction)
                    {
                        threats.Add($"{group.Count()} hostile pawns ({group.Key})");
                    }
                }
                
                // Check for manhunting animals
                var manhunters = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.MentalStateDef == MentalStateDefOf.Manhunter ||
                               p.MentalStateDef == MentalStateDefOf.ManhunterPermanent)
                    .ToList();
                
                if (manhunters.Any())
                {
                    threats.Add($"{manhunters.Count} manhunting animals");
                }
                
                // Check for infestations
                var hives = map.listerThings.ThingsOfDef(ThingDefOf.Hive);
                if (hives.Any())
                {
                    threats.Add($"Infestation ({hives.Count} hives)");
                }
                
                // Check game conditions for threats
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var condition in map.gameConditionManager.ActiveConditions.ToList())
                {
                    string conditionName = condition.def?.defName ?? "";
                    if (conditionName.Contains("Fallout") || conditionName.Contains("ToxicFallout"))
                    {
                        threats.Add("Toxic fallout");
                    }
                    else if (conditionName.Contains("ColdSnap"))
                    {
                        threats.Add("Cold snap");
                    }
                    else if (conditionName.Contains("HeatWave"))
                    {
                        threats.Add("Heat wave");
                    }
                    else if (conditionName.Contains("Flashstorm"))
                    {
                        threats.Add("Flashstorm");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting threats: {ex.Message}");
            }
            
            return threats;
        }
        
        #endregion
        
        #region Notable Items
        
        private static List<string> GetNotableItems(Map map)
        {
            var items = new List<string>();
            
            try
            {
                // Legendary and masterwork items
                // Create a snapshot to avoid "Collection was modified" errors
                var allThings = map.listerThings.AllThings.ToList();
                foreach (var thing in allThings)
                {
                    var compQuality = thing.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        if (compQuality.Quality == QualityCategory.Legendary)
                        {
                            string owner = GetItemOwner(thing, map);
                            items.Add($"Legendary {thing.Label}{owner}");
                        }
                        else if (compQuality.Quality == QualityCategory.Masterwork)
                        {
                            string owner = GetItemOwner(thing, map);
                            items.Add($"Masterwork {thing.Label}{owner}");
                        }
                    }
                }
                
                // AI persona cores and artifacts
                foreach (var thing in allThings)
                {
                    if (thing.def == ThingDefOf.AIPersonaCore)
                    {
                        items.Add("AI Persona Core");
                    }
                }
                
                // Check colonist bionics
                // Create a snapshot to avoid "Collection was modified" errors
                foreach (var pawn in map.mapPawns.FreeColonists.ToList())
                {
                    var hediffs = pawn.health?.hediffSet?.hediffs;
                    if (hediffs == null) continue;
                    
                    foreach (var hediff in hediffs)
                    {
                        if (hediff.def?.spawnThingOnRemoved?.techLevel >= TechLevel.Spacer)
                        {
                            items.Add($"{hediff.Label} on {pawn.Name.ToStringShort}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting items: {ex.Message}");
            }
            
            return items.Take(15).ToList();
        }
        
        private static string GetItemOwner(Thing thing, Map map)
        {
            // Check if item is equipped
            if (thing.ParentHolder is Pawn_EquipmentTracker equipment)
            {
                return $" (wielded by {equipment.pawn?.Name?.ToStringShort})";
            }
            if (thing.ParentHolder is Pawn_ApparelTracker apparel)
            {
                return $" (worn by {apparel.pawn?.Name?.ToStringShort})";
            }
            return "";
        }
        
        #endregion
        
        #region Infrastructure
        
        private static InfrastructureInfo GetInfrastructure(Map map)
        {
            var info = new InfrastructureInfo();
            
            try
            {
                // Hospital beds
                info.HospitalBeds = map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
                    .Count(b => b.Medical);
                
                // Turrets
                info.Turrets = map.listerBuildings.AllBuildingsColonistOfClass<Building_Turret>().Count();
                
                // Mortars
                info.Mortars = map.listerBuildings.allBuildingsColonist
                    .Count(b => b.def?.building?.IsMortar == true);
                
                // Power generation - use PowerTraderComp
                float powerGen = 0;
                foreach (var building in map.listerBuildings.allBuildingsColonist)
                {
                    var powerComp = building.TryGetComp<CompPowerTrader>();
                    if (powerComp != null && powerComp.PowerOutput > 0)
                    {
                        powerGen += powerComp.PowerOutput;
                    }
                }
                info.PowerGeneration = (int)powerGen;
                
                // Research - use GetProject
                var currentProject = Find.ResearchManager?.GetProject();
                info.ResearchCompleted = currentProject?.label ?? "none in progress";
                info.TechLevel = Faction.OfPlayer.def?.techLevel.ToString() ?? "Industrial";
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error getting infrastructure: {ex.Message}");
            }
            
            return info;
        }
        
        #endregion
    }
    
    #region Data Classes
    
    /// <summary>
    /// Comprehensive data snapshot of colony state for LLM context.
    /// Implements IColonySnapshot for shared formatting.
    /// </summary>
    public class ColonySnapshot : IColonySnapshot
    {
        // Basic colony info
        public string ColonyName { get; set; } = "Unknown Colony";
        public int ColonyAgeDays { get; set; }
        public string Season { get; set; } = "unknown";
        public string Biome { get; set; } = "unknown";
        public string Quadrum { get; set; } = "unknown";
        public int Year { get; set; } = 5500;
        
        // Wealth and threat
        public int WealthTotal { get; set; }
        public float ThreatPoints { get; set; }
        
        // Population
        public int ColonistCount { get; set; }
        public int PrisonerCount { get; set; }
        
        // Legacy colonists list (for backward compatibility)
        public List<string> Colonists { get; set; } = new List<string>();
        
        // Comprehensive colonist details
        public List<ColonistDetail> ColonistDetails { get; set; } = new List<ColonistDetail>();
        
        // Social and activities
        public List<string> RecentInteractions { get; set; } = new List<string>();
        public List<string> RecentActivities { get; set; } = new List<string>();
        
        // Events
        public List<string> RecentEvents { get; set; } = new List<string>();
        
        // Environment
        public EnvironmentInfo Environment { get; set; }
        
        // Resources
        public Dictionary<string, string> Resources { get; set; } = new Dictionary<string, string>();
        
        // Factions
        public List<FactionRelationInfo> FactionRelations { get; set; } = new List<FactionRelationInfo>();
        
        // Prisoners
        public List<PrisonerInfo> Prisoners { get; set; } = new List<PrisonerInfo>();
        
        // Animals
        public List<string> Animals { get; set; } = new List<string>();
        
        // Threats
        public List<string> ActiveThreats { get; set; } = new List<string>();
        
        // Notable items
        public List<string> NotableItems { get; set; } = new List<string>();
        
        // Infrastructure
        public InfrastructureInfo Infrastructure { get; set; }
        
        // Historical data
        public List<string> DeathRecords { get; set; } = new List<string>();
        public List<string> BattleHistory { get; set; } = new List<string>();
        
        // IColonySnapshot interface implementation
        IReadOnlyList<IColonistInfo> IColonySnapshot.ColonistDetails => ColonistDetails.Cast<IColonistInfo>().ToList();
        IReadOnlyList<string> IColonySnapshot.RecentInteractions => RecentInteractions;
        IReadOnlyList<string> IColonySnapshot.RecentActivities => RecentActivities;
        IReadOnlyList<string> IColonySnapshot.RecentEvents => RecentEvents;
        IEnvironmentInfo IColonySnapshot.Environment => Environment;
        IReadOnlyDictionary<string, string> IColonySnapshot.Resources => Resources;
        IReadOnlyList<IFactionRelationInfo> IColonySnapshot.FactionRelations => FactionRelations.Cast<IFactionRelationInfo>().ToList();
        IReadOnlyList<IPrisonerInfo> IColonySnapshot.Prisoners => Prisoners.Cast<IPrisonerInfo>().ToList();
        IReadOnlyList<string> IColonySnapshot.Animals => Animals;
        IReadOnlyList<string> IColonySnapshot.ActiveThreats => ActiveThreats;
        IReadOnlyList<string> IColonySnapshot.NotableItems => NotableItems;
        IInfrastructureInfo IColonySnapshot.Infrastructure => Infrastructure;
        IReadOnlyList<string> IColonySnapshot.DeathRecords => DeathRecords;
        IReadOnlyList<string> IColonySnapshot.BattleHistory => BattleHistory;
    }
    
    /// <summary>
    /// Detailed colonist information.
    /// Implements IColonistInfo for shared formatting.
    /// </summary>
    public class ColonistDetail : IColonistInfo
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Role { get; set; }
        
        // Backstories
        public string ChildhoodBackstory { get; set; }
        public string AdulthoodBackstory { get; set; }
        
        // Traits
        public List<string> Traits { get; set; } = new List<string>();
        
        // Health
        public int HealthPercent { get; set; }
        public string HealthStatus { get; set; }
        public List<string> Injuries { get; set; } = new List<string>();
        
        // Mental
        public int MoodPercent { get; set; }
        public string MentalState { get; set; }
        public string Inspiration { get; set; }
        
        // Skills
        public List<string> TopSkills { get; set; } = new List<string>();
        
        // Relationships
        public List<string> Relationships { get; set; } = new List<string>();
        
        // Current activity
        public string CurrentActivity { get; set; }
        
        // IColonistInfo interface implementation
        IReadOnlyList<string> IColonistInfo.Traits => Traits;
        IReadOnlyList<string> IColonistInfo.Injuries => Injuries;
        IReadOnlyList<string> IColonistInfo.TopSkills => TopSkills;
        IReadOnlyList<string> IColonistInfo.Relationships => Relationships;
        
        /// <summary>
        /// Short summary for backward compatibility.
        /// </summary>
        public string ToShortSummary()
        {
            string traits = Traits.Any() ? $" - {Traits.First()}" : "";
            string health = HealthStatus != "healthy" ? $" [{HealthStatus}]" : "";
            return $"{ShortName} ({Role}){traits}{health}";
        }
        
        /// <summary>
        /// Detailed summary for narration context.
        /// Uses shared ContextFormatter.
        /// </summary>
        public string ToDetailedSummary()
        {
            return ContextFormatter.FormatColonistDetail(this);
        }
        
        /// <summary>
        /// Full summary for choice context.
        /// Uses shared ContextFormatter.
        /// </summary>
        public string ToFullSummary()
        {
            return ContextFormatter.FormatColonistFull(this);
        }
    }
    
    /// <summary>
    /// Environment information.
    /// Implements IEnvironmentInfo for shared formatting.
    /// </summary>
    public class EnvironmentInfo : IEnvironmentInfo
    {
        public string Weather { get; set; } = "clear";
        public string TimeOfDay { get; set; } = "midday";
        public string Temperature { get; set; } = "comfortable";
        public List<string> ActiveConditions { get; set; } = new List<string>();
        
        // IEnvironmentInfo interface implementation
        IReadOnlyList<string> IEnvironmentInfo.ActiveConditions => ActiveConditions;
    }
    
    /// <summary>
    /// Faction relation information.
    /// Implements IFactionRelationInfo for shared formatting.
    /// </summary>
    public class FactionRelationInfo : IFactionRelationInfo
    {
        public string Name { get; set; }
        public string FactionType { get; set; }
        public int Goodwill { get; set; }
        public bool IsHostile { get; set; }
        public string RelationType { get; set; }
        
        public override string ToString()
        {
            return ContextFormatter.FormatFactionRelation(this);
        }
    }
    
    /// <summary>
    /// Prisoner information.
    /// Implements IPrisonerInfo for shared formatting.
    /// </summary>
    public class PrisonerInfo : IPrisonerInfo
    {
        public string Name { get; set; }
        public string OriginalFaction { get; set; }
        public int HealthPercent { get; set; }
        public string RecruitDifficulty { get; set; }
        public int MoodPercent { get; set; }
        
        public override string ToString()
        {
            return ContextFormatter.FormatPrisoner(this);
        }
    }
    
    /// <summary>
    /// Infrastructure information.
    /// Implements IInfrastructureInfo for shared formatting.
    /// </summary>
    public class InfrastructureInfo : IInfrastructureInfo
    {
        public int HospitalBeds { get; set; }
        public int Turrets { get; set; }
        public int Mortars { get; set; }
        public int PowerGeneration { get; set; }
        public string ResearchCompleted { get; set; }
        public string TechLevel { get; set; }
    }
    
    /// <summary>
    /// Event information for narration context.
    /// Implements IEventInfo for shared formatting.
    /// </summary>
    public class EventInfo : IEventInfo
    {
        public string Label { get; set; }
        public string Category { get; set; }
        public string FactionName { get; set; }
        public string ThreatLevel { get; set; }
        
        /// <summary>
        /// Create EventInfo from RimWorld IncidentDef and IncidentParms.
        /// </summary>
        public static EventInfo FromIncident(IncidentDef incidentDef, IncidentParms parms)
        {
            if (incidentDef == null) return null;
            
            string threatLevel = null;
            if (parms?.points > 0)
            {
                threatLevel = parms.points < 300 ? "minor" : 
                             parms.points < 800 ? "moderate" : "major";
            }
            
            return new EventInfo
            {
                Label = incidentDef.label,
                Category = incidentDef.category?.defName ?? "Misc",
                FactionName = parms?.faction?.Name,
                ThreatLevel = threatLevel
            };
        }
    }
    
    #endregion
}
