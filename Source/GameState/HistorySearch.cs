using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Searches historical events by relevance scoring.
    /// Uses keyword overlap, entity relevance, recency, and significance.
    /// </summary>
    public static class HistorySearch
    {
        // Configurable weights
        public static float KeywordWeight = 2.0f;
        public static float EntityWeight = 3.0f;
        public static float RecencyWeight = 1.0f;
        public static float SignificanceWeight = 1.5f;
        
        /// <summary>
        /// Find relevant historical events for current context.
        /// </summary>
        public static List<HistoricalEvent> FindRelevantHistory(
            List<string> currentKeywords, 
            List<string> currentEntityIds,
            int maxResults = 5)
        {
            if (StoryContext.Instance == null || StoryContext.Instance.History == null)
            {
                return new List<HistoricalEvent>();
            }
            
            currentKeywords = currentKeywords ?? new List<string>();
            currentEntityIds = currentEntityIds ?? new List<string>();
            
            return StoryContext.Instance.History
                .Where(e => !string.IsNullOrEmpty(e.Summary))
                .Select(e => new { Event = e, Score = ScoreEvent(e, currentKeywords, currentEntityIds) })
                .Where(x => x.Score > 0) // Only return events with some relevance
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Event)
                .ToList();
        }
        
        /// <summary>
        /// Score an event's relevance to current context.
        /// </summary>
        private static float ScoreEvent(HistoricalEvent evt, List<string> keywords, List<string> entityIds)
        {
            float score = 0;
            
            // Keyword overlap
            if (evt.Keywords != null && keywords != null && keywords.Count > 0)
            {
                int keywordMatches = evt.Keywords.Intersect(keywords, StringComparer.OrdinalIgnoreCase).Count();
                score += keywordMatches * KeywordWeight;
            }
            
            // Entity overlap
            if (evt.ParticipantIds != null && entityIds != null && entityIds.Count > 0)
            {
                int entityMatches = evt.ParticipantIds.Intersect(entityIds).Count();
                score += entityMatches * EntityWeight;
            }
            
            // Recency (decay over 60 days)
            int daysAgo = GenDate.DaysPassed - evt.DayOccurred;
            float recencyFactor = Math.Max(0, 1 - (daysAgo / 60f));
            score += recencyFactor * RecencyWeight;
            
            // Base significance
            score += evt.SignificanceScore * SignificanceWeight;
            
            return score;
        }
        
        /// <summary>
        /// Extract keywords from text (simple implementation).
        /// </summary>
        public static List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();
            
            var keywords = new List<string>();
            
            // Extract faction names, pawn names, etc.
            // This is a simple implementation - could be enhanced with NLP
            var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                // Capitalized words are likely names
                if (word.Length > 2 && char.IsUpper(word[0]))
                {
                    keywords.Add(word);
                }
            }
            
            return keywords.Distinct().ToList();
        }
    }
}

