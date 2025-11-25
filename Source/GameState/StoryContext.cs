using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// WorldComponent that persists story data across saves.
    /// Tracks journal entries, recent events, and choice history.
    /// </summary>
    public class StoryContext : WorldComponent
    {
        public static StoryContext Instance { get; set; }
        
        // Journal entries for the Story tab
        public List<JournalEntry> JournalEntries = new List<JournalEntry>();
        
        // Recent events for LLM context (last 5)
        public List<string> RecentEvents = new List<string>();
        
        // Track choices made
        public Dictionary<string, string> ChoiceHistory = new Dictionary<string, string>();
        
        // API call tracking for rate limiting
        public int CallsToday = 0;
        public int LastCallDay = -1;
        
        // Constants
        private const int MAX_JOURNAL_ENTRIES = 200;
        private const int MAX_RECENT_EVENTS = 5;
        
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
                catch (System.Exception ex)
                {
                    Log.Warning($"[AI Narrator] Could not create founding entry: {ex.Message}");
                }
            }
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Collections.Look(ref JournalEntries, "journalEntries", LookMode.Deep);
            Scribe_Collections.Look(ref RecentEvents, "recentEvents", LookMode.Value);
            Scribe_Collections.Look(ref ChoiceHistory, "choiceHistory", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref CallsToday, "callsToday", 0);
            Scribe_Values.Look(ref LastCallDay, "lastCallDay", -1);
            
            // Ensure collections are initialized after loading
            if (JournalEntries == null) JournalEntries = new List<JournalEntry>();
            if (RecentEvents == null) RecentEvents = new List<string>();
            if (ChoiceHistory == null) ChoiceHistory = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Add a new journal entry.
        /// </summary>
        public void AddJournalEntry(string text, JournalEntryType type, string choiceMade = null)
        {
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
    }
}

