using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Data structures for Phase 2 Deep Memory system.
    /// Includes Nemesis profiles, Legends, and enhanced historical events.
    /// </summary>
    
    #region NemesisProfile
    
    /// <summary>
    /// Profile of a recurring enemy who has survived encounters with the colony.
    /// </summary>
    public class NemesisProfile : IExposable
    {
        public string PawnId;           // ThingID for tracking
        public string Name;             // Display name
        public string FactionId;        // Faction defName
        public string FactionName;      // Faction display name
        public Gender Gender;
        public int AgeBiological;
        
        // Appearance (for re-spawning)
        public string BodyType;         // BodyTypeDef.defName
        public string HeadType;         // HeadTypeDef.defName  
        public string HairDef;          // HairDef.defName
        public string BeardDef;         // BeardDef.defName (if applicable)
        public Color HairColor;
        public Color SkinColor;
        
        // Combat stats
        public List<string> Skills;     // Top 3 combat skills with levels
        public List<string> Traits;     // Notable traits
        
        // Grudge tracking
        public string GrudgeReason;     // "Killed [name]", "Wounded by [name]", etc.
        public string GrudgeTarget;     // Colonist name they have grudge against
        public int EncounterCount;
        public int LastSeenDay;
        public int CreatedDay;
        
        // State
        public bool IsRetired;          // No longer spawns
        public string RetiredReason;    // "Killed", "Faction destroyed", "Too old"
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnId, "pawnId");
            Scribe_Values.Look(ref Name, "name");
            Scribe_Values.Look(ref FactionId, "factionId");
            Scribe_Values.Look(ref FactionName, "factionName");
            Scribe_Values.Look(ref Gender, "gender", Gender.None);
            Scribe_Values.Look(ref AgeBiological, "ageBiological", 0);
            
            Scribe_Values.Look(ref BodyType, "bodyType");
            Scribe_Values.Look(ref HeadType, "headType");
            Scribe_Values.Look(ref HairDef, "hairDef");
            Scribe_Values.Look(ref BeardDef, "beardDef");
            Scribe_Values.Look(ref HairColor, "hairColor", Color.white);
            Scribe_Values.Look(ref SkinColor, "skinColor", Color.white);
            
            Scribe_Collections.Look(ref Skills, "skills", LookMode.Value);
            Scribe_Collections.Look(ref Traits, "traits", LookMode.Value);
            
            Scribe_Values.Look(ref GrudgeReason, "grudgeReason");
            Scribe_Values.Look(ref GrudgeTarget, "grudgeTarget");
            Scribe_Values.Look(ref EncounterCount, "encounterCount", 0);
            Scribe_Values.Look(ref LastSeenDay, "lastSeenDay", 0);
            Scribe_Values.Look(ref CreatedDay, "createdDay", 0);
            
            Scribe_Values.Look(ref IsRetired, "isRetired", false);
            Scribe_Values.Look(ref RetiredReason, "retiredReason");
            
            if (Skills == null) Skills = new List<string>();
            if (Traits == null) Traits = new List<string>();
        }
    }
    
    #endregion
    
    #region Legend
    
    /// <summary>
    /// Record of a Masterwork or Legendary artwork that becomes part of colony mythology.
    /// </summary>
    public class Legend : IExposable
    {
        public string Id;               // UUID
        public string ArtworkLabel;     // "Large marble statue"
        public string ArtworkTale;      // RimWorld's generated art description
        public string MythicSummary;    // LLM-generated 1-2 sentence summary (Legendary only)
        public string CreatorName;
        public QualityCategory Quality;
        public string CreatedDateString;
        public int CreatedDay;
        public bool IsDestroyed;        // Artwork no longer exists
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_Values.Look(ref ArtworkLabel, "artworkLabel");
            Scribe_Values.Look(ref ArtworkTale, "artworkTale");
            Scribe_Values.Look(ref MythicSummary, "mythicSummary");
            Scribe_Values.Look(ref CreatorName, "creatorName");
            Scribe_Values.Look(ref Quality, "quality", QualityCategory.Awful);
            Scribe_Values.Look(ref CreatedDateString, "createdDateString");
            Scribe_Values.Look(ref CreatedDay, "createdDay", 0);
            Scribe_Values.Look(ref IsDestroyed, "isDestroyed", false);
        }
    }
    
    #endregion
    
    #region HistoricalEvent
    
    /// <summary>
    /// Enhanced event record with structured data for relevance scoring.
    /// </summary>
    public class HistoricalEvent : IExposable
    {
        public string Id;               // UUID
        public string Summary;          // Short description
        public string EventType;        // "Raid", "Death", "Recruitment", etc.
        public int DayOccurred;
        public string DateString;
        
        // For relevance scoring
        public List<string> Keywords;           // Auto-extracted: faction names, pawn names
        public List<string> ParticipantIds;     // ThingIDs of involved pawns
        public float SignificanceScore;         // For pruning (deaths > injuries > minor events)
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_Values.Look(ref Summary, "summary");
            Scribe_Values.Look(ref EventType, "eventType");
            Scribe_Values.Look(ref DayOccurred, "dayOccurred", 0);
            Scribe_Values.Look(ref DateString, "dateString");
            
            Scribe_Collections.Look(ref Keywords, "keywords", LookMode.Value);
            Scribe_Collections.Look(ref ParticipantIds, "participantIds", LookMode.Value);
            Scribe_Values.Look(ref SignificanceScore, "significanceScore", 0f);
            
            if (Keywords == null) Keywords = new List<string>();
            if (ParticipantIds == null) ParticipantIds = new List<string>();
        }
    }
    
    #endregion
}

