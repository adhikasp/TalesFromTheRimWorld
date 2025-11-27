using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Tracks Masterwork and Legendary artworks that become part of colony mythology.
    /// </summary>
    public static class LegendTracker
    {
        /// <summary>
        /// Called when a Masterwork or Legendary artwork is created.
        /// </summary>
        public static void OnMasterworkCreated(Thing artwork, CompArt compArt, QualityCategory quality)
        {
            try
            {
                if (artwork == null || compArt == null) return;
                if (StoryContext.Instance == null) return;
                if (quality < QualityCategory.Masterwork) return;
                
                var legend = new Legend
                {
                    Id = Guid.NewGuid().ToString(),
                    ArtworkLabel = artwork.Label,
                    ArtworkTale = compArt.GenerateImageDescription(),
                    CreatorName = compArt.AuthorName,
                    Quality = quality,
                    CreatedDay = GenDate.DaysPassed,
                    CreatedDateString = GetCurrentDateString(),
                    IsDestroyed = false
                };
                
                // Only generate LLM summary for Legendary items
                if (quality == QualityCategory.Legendary)
                {
                    RequestMythicSummary(legend);
                }
                else
                {
                    // Masterwork - just store it
                    StoryContext.Instance.AddLegend(legend);
                    Log.Message($"[AI Narrator] Recorded Masterwork: {legend.ArtworkLabel} by {legend.CreatorName}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Error creating Legend: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Request LLM to generate a mythic summary for a Legendary artwork.
        /// </summary>
        private static void RequestMythicSummary(Legend legend)
        {
            if (legend == null || StoryContext.Instance == null) return;
            
            // Check rate limiting
            if (!StoryContext.Instance.CanMakeApiCall())
            {
                // Add without summary if rate limited
                StoryContext.Instance.AddLegend(legend);
                Log.Message($"[AI Narrator] Recorded Legendary artwork (no summary - rate limited): {legend.ArtworkLabel}");
                return;
            }
            
            string systemPrompt = @"You are The Narrator, creating mythic summaries for legendary artworks in a RimWorld colony.

Write 1-2 sentences that transform this artwork into part of the colony's mythology. Make it feel like a legend that will be told for generations.

Tone: Gritty, survivalist, sci-fi western. Dark but not hopeless. Smart witty and engaging.
Format: Just the summary text, no formatting or prefixes.";

            string userPrompt = $@"ARTWORK DETAILS:
Label: {legend.ArtworkLabel}
Tale: {legend.ArtworkTale}
Creator: {legend.CreatorName}
Created: {legend.CreatedDateString}

TASK: Write a 1-2 sentence mythic summary that makes this artwork feel legendary and part of the colony's history.";

            StoryContext.Instance.RegisterApiCall();
            
            OpenRouterClient.RequestNarration(systemPrompt, userPrompt,
                onSuccess: (summary) =>
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        legend.MythicSummary = summary.Trim();
                        StoryContext.Instance.AddLegend(legend);
                        Log.Message($"[AI Narrator] Recorded Legendary artwork with mythic summary: {legend.ArtworkLabel}");
                    });
                },
                onError: (error) =>
                {
                    Log.Warning($"[AI Narrator] Failed to generate mythic summary: {error}");
                    // Add without summary (graceful degradation)
                    StoryContext.Instance.AddLegend(legend);
                    Log.Message($"[AI Narrator] Recorded Legendary artwork (no summary): {legend.ArtworkLabel}");
                }
            );
        }
        
        /// <summary>
        /// Get current date string.
        /// </summary>
        private static string GetCurrentDateString()
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
        /// Check if an artwork is a Legend and mark it as destroyed if needed.
        /// </summary>
        public static void OnArtworkDestroyed(Thing artwork)
        {
            if (artwork == null || StoryContext.Instance == null) return;
            
            // Find matching legend by label
            var legend = StoryContext.Instance.Legends
                .FirstOrDefault(l => l.ArtworkLabel == artwork.Label && !l.IsDestroyed);
            
            if (legend != null)
            {
                StoryContext.Instance.MarkLegendDestroyed(legend.Id);
                Log.Message($"[AI Narrator] Marked Legend as destroyed: {legend.ArtworkLabel}");
            }
        }
    }
}

