using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Main tab window displaying the story journal.
    /// Shows chronological narrative entries grouped by year.
    /// </summary>
    public class MainTabWindow_StoryJournal : MainTabWindow
    {
        private Vector2 scrollPosition;
        private JournalFilter currentFilter = JournalFilter.All;
        
        // Styling
        private static readonly Color HeaderColor = new Color(0.9f, 0.85f, 0.7f);
        private static readonly Color YearColor = new Color(0.8f, 0.75f, 0.6f);
        private static readonly Color DateColor = new Color(0.7f, 0.65f, 0.55f);
        private static readonly Color TextColor = new Color(0.9f, 0.88f, 0.82f);
        private static readonly Color ChoiceColor = new Color(0.6f, 0.8f, 0.6f);
        private static readonly Color MilestoneColor = new Color(0.9f, 0.75f, 0.4f);
        
        public override Vector2 RequestedTabSize => new Vector2(700f, 500f);
        
        public override void DoWindowContents(Rect inRect)
        {
            // Header
            Rect headerRect = new Rect(0f, 0f, inRect.width, 40f);
            DrawHeader(headerRect);
            
            float y = 45f;
            
            // Filter dropdown
            Rect filterRect = new Rect(inRect.width - 150f, y, 140f, 28f);
            DrawFilterDropdown(filterRect);
            
            y += 35f;
            
            // Journal content
            Rect contentRect = new Rect(0f, y, inRect.width, inRect.height - y);
            DrawJournalContent(contentRect);
        }
        
        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = HeaderColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            
            Widgets.Label(new Rect(rect.x + 10f, rect.y, rect.width - 160f, rect.height), "📖 STORY JOURNAL");
            
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            
            // Divider
            GUI.color = new Color(0.5f, 0.45f, 0.4f, 0.6f);
            Widgets.DrawLineHorizontal(0f, rect.yMax, rect.width);
            GUI.color = Color.white;
        }
        
        private void DrawFilterDropdown(Rect rect)
        {
            string filterLabel = currentFilter switch
            {
                JournalFilter.Events => "Events Only",
                JournalFilter.Choices => "Choices Only",
                JournalFilter.Milestones => "Milestones Only",
                _ => "All Entries"
            };
            
            if (Widgets.ButtonText(rect, $"Filter: {filterLabel}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("All Entries", () => currentFilter = JournalFilter.All),
                    new FloatMenuOption("Events Only", () => currentFilter = JournalFilter.Events),
                    new FloatMenuOption("Choices Only", () => currentFilter = JournalFilter.Choices),
                    new FloatMenuOption("Milestones Only", () => currentFilter = JournalFilter.Milestones)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        
        private void DrawJournalContent(Rect rect)
        {
            if (StoryContext.Instance == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "No story data available.\nStart a new game with The Narrator storyteller.");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }
            
            // Get filtered entries
            var entries = GetFilteredEntries();
            
            if (entries.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "No entries yet.\nYour story will unfold...");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }
            
            // Group by year
            var grouped = entries
                .GroupBy(e => ExtractYear(e.DateString))
                .OrderByDescending(g => g.Key)
                .ToList();
            
            // Calculate total height
            float totalHeight = CalculateTotalHeight(grouped, rect.width - 30f);
            
            // Scroll view
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            float currentY = 0f;
            
            foreach (var yearGroup in grouped)
            {
                currentY = DrawYearSection(viewRect, currentY, yearGroup.Key, yearGroup.ToList());
            }
            
            Widgets.EndScrollView();
        }
        
        private float DrawYearSection(Rect viewRect, float y, int year, List<JournalEntry> entries)
        {
            // Year header
            Rect yearRect = new Rect(0f, y, viewRect.width, 30f);
            Text.Font = GameFont.Medium;
            GUI.color = YearColor;
            
            bool expanded = true;  // Could add collapsing later
            string yearLabel = expanded ? $"▼ {year}" : $"▶ {year}";
            Widgets.Label(yearRect, yearLabel);
            
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            
            y += 35f;
            
            if (expanded)
            {
                foreach (var entry in entries.OrderByDescending(e => e.GameTick))
                {
                    y = DrawEntry(viewRect, y, entry);
                }
            }
            
            return y + 10f;  // Gap after year
        }
        
        private float DrawEntry(Rect viewRect, float y, JournalEntry entry)
        {
            float entryWidth = viewRect.width - 40f;
            
            // Calculate text height
            float textHeight = Text.CalcHeight(entry.Text, entryWidth);
            float totalHeight = textHeight + 30f;  // Date + padding
            
            if (!string.IsNullOrEmpty(entry.ChoiceMade))
            {
                totalHeight += 20f;  // Choice indicator
            }
            
            Rect entryRect = new Rect(20f, y, viewRect.width - 20f, totalHeight);
            
            // Tree line
            GUI.color = new Color(0.4f, 0.38f, 0.35f, 0.5f);
            Widgets.DrawLineVertical(10f, y, totalHeight);
            Widgets.DrawLineHorizontal(10f, y + 12f, 15f);
            GUI.color = Color.white;
            
            // Type indicator
            float indicatorX = 30f;
            DrawTypeIndicator(indicatorX, y + 5f, entry.EntryType);
            
            // Date
            Rect dateRect = new Rect(50f, y, entryWidth, 20f);
            GUI.color = DateColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(dateRect, entry.DateString);
            Text.Font = GameFont.Small;
            
            // Text
            Rect textRect = new Rect(50f, y + 18f, entryWidth, textHeight + 5f);
            GUI.color = GetTextColor(entry.EntryType);
            Widgets.Label(textRect, $"\"{entry.Text}\"");
            
            float currentY = y + 18f + textHeight + 5f;
            
            // Choice made indicator
            if (!string.IsNullOrEmpty(entry.ChoiceMade))
            {
                Rect choiceRect = new Rect(50f, currentY, entryWidth, 18f);
                GUI.color = ChoiceColor;
                Text.Font = GameFont.Tiny;
                Widgets.Label(choiceRect, $"→ {entry.ChoiceMade}");
                Text.Font = GameFont.Small;
                currentY += 20f;
            }
            
            GUI.color = Color.white;
            
            return currentY + 10f;  // Gap after entry
        }
        
        private void DrawTypeIndicator(float x, float y, JournalEntryType type)
        {
            Rect iconRect = new Rect(x, y, 15f, 15f);
            
            switch (type)
            {
                case JournalEntryType.Choice:
                    GUI.color = ChoiceColor;
                    Widgets.Label(iconRect, "★");
                    break;
                    
                case JournalEntryType.Milestone:
                    GUI.color = MilestoneColor;
                    Widgets.Label(iconRect, "◆");
                    break;
                    
                default:
                    GUI.color = new Color(0.6f, 0.6f, 0.55f);
                    Widgets.Label(iconRect, "●");
                    break;
            }
            
            GUI.color = Color.white;
        }
        
        private Color GetTextColor(JournalEntryType type)
        {
            return type switch
            {
                JournalEntryType.Choice => new Color(0.85f, 0.95f, 0.85f),
                JournalEntryType.Milestone => new Color(0.95f, 0.9f, 0.75f),
                _ => TextColor
            };
        }
        
        private List<JournalEntry> GetFilteredEntries()
        {
            var allEntries = StoryContext.Instance?.JournalEntries ?? new List<JournalEntry>();
            
            return currentFilter switch
            {
                JournalFilter.Events => allEntries.Where(e => e.EntryType == JournalEntryType.Event).ToList(),
                JournalFilter.Choices => allEntries.Where(e => e.EntryType == JournalEntryType.Choice).ToList(),
                JournalFilter.Milestones => allEntries.Where(e => e.EntryType == JournalEntryType.Milestone).ToList(),
                _ => allEntries.ToList()
            };
        }
        
        private float CalculateTotalHeight(List<IGrouping<int, JournalEntry>> grouped, float width)
        {
            float total = 0f;
            
            foreach (var yearGroup in grouped)
            {
                total += 45f;  // Year header
                
                foreach (var entry in yearGroup)
                {
                    float textHeight = Text.CalcHeight(entry.Text, width - 60f);
                    total += textHeight + 30f;
                    
                    if (!string.IsNullOrEmpty(entry.ChoiceMade))
                    {
                        total += 20f;
                    }
                    
                    total += 10f;  // Entry gap
                }
                
                total += 10f;  // Year gap
            }
            
            return total + 50f;  // Bottom padding
        }
        
        private int ExtractYear(string dateString)
        {
            // Extract year from "Quadrum Day, Year" format
            if (string.IsNullOrEmpty(dateString)) return 5500;
            
            var parts = dateString.Split(',');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[1].Trim(), out int year))
                {
                    return year;
                }
            }
            return 5500;
        }
    }
    
    public enum JournalFilter
    {
        All,
        Events,
        Choices,
        Milestones
    }
}

