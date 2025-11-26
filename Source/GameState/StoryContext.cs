using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// WorldComponent that persists story data across saves.
    /// Tracks journal entries, recent events, choice history, and historical records.
    /// </summary>
    public class StoryContext : WorldComponent
    {
        public static StoryContext Instance { get; set; }
        
        // Journal entries for the Story tab
        public List<JournalEntry> JournalEntries = new List<JournalEntry>();
        
        // Recent events for LLM context (last 10)
        public List<string> RecentEvents = new List<string>();
        
        // Track choices made
        public Dictionary<string, string> ChoiceHistory = new Dictionary<string, string>();
        
        // Historical records for narrative callbacks
        public List<DeathRecord> ColonistDeaths = new List<DeathRecord>();
        public List<BattleRecord> MajorBattles = new List<BattleRecord>();
        public List<string> RecruitedPawns = new List<string>();
        public List<string> SignificantInteractions = new List<string>();
        public List<string> HeroicActions = new List<string>();
        
        // API call tracking for rate limiting
        public int CallsToday = 0;
        public int LastCallDay = -1;
        
        // Constants
        private const int MAX_JOURNAL_ENTRIES = 200;
        private const int MAX_RECENT_EVENTS = 10;
        private const int MAX_DEATH_RECORDS = 50;
        private const int MAX_BATTLE_RECORDS = 30;
        private const int MAX_RECRUITED_PAWNS = 50;
        private const int MAX_SIGNIFICANT_INTERACTIONS = 30;
        private const int MAX_HEROIC_ACTIONS = 30;
        
        private bool initialized = false;
        
        public StoryContext(World world) : base(world)
        {
            Instance = this;
        }
        
        /// <summary>
        /// Called every tick. Used for deferred initialization.
        /// </summary>
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            
            if (!initialized)
            {
                initialized = true;
                Instance = this;
                
                // Create founding entry if this is a new game and we have a map
                try
                {
                    if (JournalEntries.Count == 0 && GenDate.DaysPassed <= 1 && Find.CurrentMap != null)
                    {
                        AddFoundingEntry();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[AI Narrator] Could not create founding entry: {ex.Message}");
                }
            }
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            
            // Core data
            Scribe_Collections.Look(ref JournalEntries, "journalEntries", LookMode.Deep);
            Scribe_Collections.Look(ref RecentEvents, "recentEvents", LookMode.Value);
            Scribe_Collections.Look(ref ChoiceHistory, "choiceHistory", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref CallsToday, "callsToday", 0);
            Scribe_Values.Look(ref LastCallDay, "lastCallDay", -1);
            
            // Historical records
            Scribe_Collections.Look(ref ColonistDeaths, "colonistDeaths", LookMode.Deep);
            Scribe_Collections.Look(ref MajorBattles, "majorBattles", LookMode.Deep);
            Scribe_Collections.Look(ref RecruitedPawns, "recruitedPawns", LookMode.Value);
            Scribe_Collections.Look(ref SignificantInteractions, "significantInteractions", LookMode.Value);
            Scribe_Collections.Look(ref HeroicActions, "heroicActions", LookMode.Value);
            
            // Ensure collections are initialized after loading
            if (JournalEntries == null) JournalEntries = new List<JournalEntry>();
            if (RecentEvents == null) RecentEvents = new List<string>();
            if (ChoiceHistory == null) ChoiceHistory = new Dictionary<string, string>();
            if (ColonistDeaths == null) ColonistDeaths = new List<DeathRecord>();
            if (MajorBattles == null) MajorBattles = new List<BattleRecord>();
            if (RecruitedPawns == null) RecruitedPawns = new List<string>();
            if (SignificantInteractions == null) SignificantInteractions = new List<string>();
            if (HeroicActions == null) HeroicActions = new List<string>();
        }
        
        #region Journal Entries
        
        /// <summary>
        /// Add a new journal entry.
        /// </summary>
        public void AddJournalEntry(string text, JournalEntryType type, string choiceMade = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            
            // Prevent accidental duplicates (e.g., double callbacks in the same tick)
            if (JournalEntries.Count > 0)
            {
                var lastEntry = JournalEntries[JournalEntries.Count - 1];
                if (lastEntry != null &&
                    lastEntry.EntryType == type &&
                    lastEntry.GameTick == currentTick &&
                    string.Equals(lastEntry.Text, text, StringComparison.Ordinal) &&
                    string.Equals(lastEntry.ChoiceMade ?? string.Empty, choiceMade ?? string.Empty, StringComparison.Ordinal))
                {
                    return;
                }
            }
            
            var entry = new JournalEntry
            {
                GameTick = Find.TickManager.TicksGame,
                DateString = GetCurrentDateString(),
                Text = text,
                EntryType = type,
                ChoiceMade = choiceMade
            };
            
            JournalEntries.Add(entry);
            
            // Prune if over limit
            while (JournalEntries.Count > MAX_JOURNAL_ENTRIES)
            {
                JournalEntries.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Add an event to recent events for LLM context.
        /// </summary>
        public void AddRecentEvent(string eventSummary)
        {
            RecentEvents.Add(eventSummary);
            
            while (RecentEvents.Count > MAX_RECENT_EVENTS)
            {
                RecentEvents.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Record a choice made by the player.
        /// </summary>
        public void RecordChoice(string choiceId, string optionChosen)
        {
            ChoiceHistory[choiceId] = optionChosen;
        }
        
        #endregion
        
        #region Historical Records
        
        /// <summary>
        /// Record a colonist death for narrative callbacks.
        /// </summary>
        public void RecordColonistDeath(Pawn pawn, DamageInfo? lastDamage = null)
        {
            try
            {
                if (pawn == null) return;
                
                var record = new DeathRecord
                {
                    Name = pawn.Name?.ToStringFull ?? "Unknown",
                    ShortName = pawn.Name?.ToStringShort ?? "Unknown",
                    Role = GetPawnRole(pawn),
                    Age = pawn.ageTracker?.AgeBiologicalYears ?? 0,
                    DateString = GetCurrentDateString(),
                    DayDied = GenDate.DaysPassed,
                    CauseOfDeath = GetCauseOfDeath(pawn, lastDamage),
                    KillerFaction = lastDamage?.Instigator?.Faction?.Name,
                    KillerName = lastDamage?.Instigator?.LabelShort
                };
                
                ColonistDeaths.Add(record);
                
                // Prune if over limit
                while (ColonistDeaths.Count > MAX_DEATH_RECORDS)
                {
                    ColonistDeaths.RemoveAt(0);
                }
                
                Log.Message($"[AI Narrator] Recorded death: {record}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Failed to record death: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a major battle/raid event.
        /// </summary>
        public void RecordBattle(string battleType, string faction, int enemyCount, int casualties, int enemyKills, List<string> heroes = null)
        {
            try
            {
                var record = new BattleRecord
                {
                    DateString = GetCurrentDateString(),
                    DayOccurred = GenDate.DaysPassed,
                    BattleType = battleType,
                    EnemyFaction = faction,
                    EnemyCount = enemyCount,
                    ColonistCasualties = casualties,
                    EnemiesKilled = enemyKills,
                    HeroesOfBattle = heroes ?? new List<string>()
                };
                
                MajorBattles.Add(record);
                
                while (MajorBattles.Count > MAX_BATTLE_RECORDS)
                {
                    MajorBattles.RemoveAt(0);
                }
                
                Log.Message($"[AI Narrator] Recorded battle: {record}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Failed to record battle: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a recruited pawn.
        /// </summary>
        public void RecordRecruitment(Pawn pawn, string method)
        {
            try
            {
                if (pawn == null) return;
                
                string entry = $"{pawn.Name?.ToStringFull ?? "Unknown"} joined on {GetCurrentDateString()} ({method})";
                RecruitedPawns.Add(entry);
                
                while (RecruitedPawns.Count > MAX_RECRUITED_PAWNS)
                {
                    RecruitedPawns.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Failed to record recruitment: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a significant social interaction.
        /// </summary>
        public void RecordSignificantInteraction(string description)
        {
            try
            {
                string entry = $"{GetCurrentDateString()}: {description}";
                SignificantInteractions.Add(entry);
                
                while (SignificantInteractions.Count > MAX_SIGNIFICANT_INTERACTIONS)
                {
                    SignificantInteractions.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Failed to record interaction: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a heroic action.
        /// </summary>
        public void RecordHeroicAction(Pawn pawn, string action)
        {
            try
            {
                if (pawn == null) return;
                
                string entry = $"{GetCurrentDateString()}: {pawn.Name?.ToStringShort ?? "Unknown"} {action}";
                HeroicActions.Add(entry);
                
                while (HeroicActions.Count > MAX_HEROIC_ACTIONS)
                {
                    HeroicActions.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Failed to record heroic action: {ex.Message}");
            }
        }
        
        #endregion
        
        #region API Rate Limiting
        
        /// <summary>
        /// Check if we can make an API call (rate limiting).
        /// </summary>
        public bool CanMakeApiCall()
        {
            try
            {
                // Safety check - ensure game is fully initialized
                if (Find.TickManager == null) return false;
                
                int currentDay = GenDate.DaysPassed;
                
                // Reset counter if it's a new day
                if (currentDay != LastCallDay)
                {
                    CallsToday = 0;
                    LastCallDay = currentDay;
                }
                
                return CallsToday < AINarratorMod.Settings.MaxCallsPerDay;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Register an API call for rate limiting.
        /// </summary>
        public void RegisterApiCall()
        {
            try
            {
                if (Find.TickManager == null) return;
                
                int currentDay = GenDate.DaysPassed;
                
                if (currentDay != LastCallDay)
                {
                    CallsToday = 0;
                    LastCallDay = currentDay;
                }
                
                CallsToday++;
            }
            catch
            {
                // Silently fail if game state isn't ready
            }
        }
        
        /// <summary>
        /// Get remaining API calls for today.
        /// </summary>
        public int GetRemainingCalls()
        {
            try
            {
                if (Find.TickManager == null) return AINarratorMod.Settings.MaxCallsPerDay;
                
                int currentDay = GenDate.DaysPassed;
                
                if (currentDay != LastCallDay)
                {
                    return AINarratorMod.Settings.MaxCallsPerDay;
                }
                
                return AINarratorMod.Settings.MaxCallsPerDay - CallsToday;
            }
            catch
            {
                return AINarratorMod.Settings.MaxCallsPerDay;
            }
        }
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Get journal entries filtered by type.
        /// </summary>
        public List<JournalEntry> GetEntriesByType(JournalEntryType? type)
        {
            if (type == null)
            {
                return JournalEntries.ToList();
            }
            
            return JournalEntries.Where(e => e.EntryType == type.Value).ToList();
        }
        
        /// <summary>
        /// Get journal entries grouped by year.
        /// </summary>
        public Dictionary<int, List<JournalEntry>> GetEntriesGroupedByYear()
        {
            var grouped = new Dictionary<int, List<JournalEntry>>();
            
            foreach (var entry in JournalEntries)
            {
                // Extract year from date string (format: "Quadrum Day, Year")
                int year = ExtractYear(entry.DateString);
                
                if (!grouped.ContainsKey(year))
                {
                    grouped[year] = new List<JournalEntry>();
                }
                
                grouped[year].Add(entry);
            }
            
            return grouped;
        }
        
        /// <summary>
        /// Get deaths from a specific battle or time period.
        /// </summary>
        public List<DeathRecord> GetDeathsFromDay(int day)
        {
            return ColonistDeaths.Where(d => d.DayDied == day).ToList();
        }
        
        /// <summary>
        /// Get the most recent deaths for narrative context.
        /// </summary>
        public List<DeathRecord> GetRecentDeaths(int count = 5)
        {
            int skip = Math.Max(0, ColonistDeaths.Count - count);
            return ColonistDeaths.Skip(skip).ToList();
        }
        
        /// <summary>
        /// Get battles involving a specific faction.
        /// </summary>
        public List<BattleRecord> GetBattlesWithFaction(string factionName)
        {
            return MajorBattles.Where(b => b.EnemyFaction == factionName).ToList();
        }
        
        #endregion
        
        #region Helper Methods
        
        private void AddFoundingEntry()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            
            var snapshot = ColonyStateCollector.GetSnapshot();
            string biome = snapshot.Biome;
            string colonistNames = string.Join(", ", snapshot.Colonists.Take(3).Select(c => c.Split('(')[0].Trim()));
            
            string foundingText = $"Colony {snapshot.ColonyName} founded on the edge of the {biome}. " +
                                 $"{snapshot.ColonistCount} souls against the world: {colonistNames}.";
            
            AddJournalEntry(foundingText, JournalEntryType.Milestone);
        }
        
        private string GetCurrentDateString()
        {
            Map map = Find.CurrentMap;
            if (map == null) return "Unknown Date";
            
            int day = GenLocalDate.DayOfQuadrum(map) + 1;
            float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
            string quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, longitude).Label();
            int year = GenDate.Year(Find.TickManager.TicksAbs, longitude);
            
            return $"{quadrum} {day}, {year}";
        }
        
        private int ExtractYear(string dateString)
        {
            // Try to extract year from "Quadrum Day, Year" format
            var parts = dateString.Split(',');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[1].Trim(), out int year))
                {
                    return year;
                }
            }
            return 5500; // Default RimWorld year
        }
        
        private string GetPawnRole(Pawn pawn)
        {
            var skills = pawn.skills?.skills;
            if (skills == null) return "colonist";
            
            var bestSkill = skills
                .Where(s => s.passion != Passion.None || s.Level >= 8)
                .OrderByDescending(s => s.Level)
                .FirstOrDefault();
            
            if (bestSkill != null && bestSkill.Level >= 5)
            {
                return GetRoleFromSkill(bestSkill.def);
            }
            
            return "colonist";
        }
        
        private string GetRoleFromSkill(SkillDef skill)
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
        
        private string GetCauseOfDeath(Pawn pawn, DamageInfo? lastDamage)
        {
            // Try to determine cause of death
            if (lastDamage.HasValue)
            {
                var dmg = lastDamage.Value;
                if (dmg.Def != null)
                {
                    return dmg.Def.label ?? "unknown damage";
                }
            }
            
            // Check hediffs for lethal conditions
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                // Check for blood loss
                var bloodLoss = hediffs.FirstOrDefault(h => h.def.defName == "BloodLoss" && h.Severity >= 1f);
                if (bloodLoss != null) return "blood loss";
                
                // Check for lethal diseases
                var disease = hediffs.FirstOrDefault(h => h.def.lethalSeverity > 0 && h.Severity >= h.def.lethalSeverity);
                if (disease != null) return disease.def.label;
                
                // Check for infection
                var infection = hediffs.FirstOrDefault(h => h.def.defName.Contains("Infection") || h.def.defName.Contains("WoundInfection"));
                if (infection != null) return "infection";
            }
            
            return "unknown causes";
        }
        
        #endregion
    }
    
    #region Record Classes
    
    /// <summary>
    /// Record of a colonist death for narrative callbacks.
    /// </summary>
    public class DeathRecord : IExposable
    {
        public string Name;
        public string ShortName;
        public string Role;
        public int Age;
        public string DateString;
        public int DayDied;
        public string CauseOfDeath;
        public string KillerFaction;
        public string KillerName;
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "name", "Unknown");
            Scribe_Values.Look(ref ShortName, "shortName", "Unknown");
            Scribe_Values.Look(ref Role, "role", "colonist");
            Scribe_Values.Look(ref Age, "age", 0);
            Scribe_Values.Look(ref DateString, "dateString", "");
            Scribe_Values.Look(ref DayDied, "dayDied", 0);
            Scribe_Values.Look(ref CauseOfDeath, "causeOfDeath", "unknown");
            Scribe_Values.Look(ref KillerFaction, "killerFaction");
            Scribe_Values.Look(ref KillerName, "killerName");
        }
        
        public override string ToString()
        {
            string killer = !string.IsNullOrEmpty(KillerName) ? $" by {KillerName}" : "";
            string faction = !string.IsNullOrEmpty(KillerFaction) ? $" ({KillerFaction})" : "";
            return $"{ShortName} ({Role}, age {Age}) died of {CauseOfDeath}{killer}{faction} on {DateString}";
        }
    }
    
    /// <summary>
    /// Record of a major battle for narrative callbacks.
    /// </summary>
    public class BattleRecord : IExposable
    {
        public string DateString;
        public int DayOccurred;
        public string BattleType;
        public string EnemyFaction;
        public int EnemyCount;
        public int ColonistCasualties;
        public int EnemiesKilled;
        public List<string> HeroesOfBattle = new List<string>();
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref DateString, "dateString", "");
            Scribe_Values.Look(ref DayOccurred, "dayOccurred", 0);
            Scribe_Values.Look(ref BattleType, "battleType", "raid");
            Scribe_Values.Look(ref EnemyFaction, "enemyFaction", "unknown");
            Scribe_Values.Look(ref EnemyCount, "enemyCount", 0);
            Scribe_Values.Look(ref ColonistCasualties, "colonistCasualties", 0);
            Scribe_Values.Look(ref EnemiesKilled, "enemiesKilled", 0);
            Scribe_Collections.Look(ref HeroesOfBattle, "heroesOfBattle", LookMode.Value);
            
            if (HeroesOfBattle == null) HeroesOfBattle = new List<string>();
        }
        
        public override string ToString()
        {
            string heroes = HeroesOfBattle.Any() ? $" Heroes: {string.Join(", ", HeroesOfBattle)}" : "";
            return $"{BattleType} by {EnemyFaction} ({EnemyCount} attackers) on {DateString}. Casualties: {ColonistCasualties} colonists, {EnemiesKilled} enemies killed.{heroes}";
        }
    }
    
    #endregion
}
