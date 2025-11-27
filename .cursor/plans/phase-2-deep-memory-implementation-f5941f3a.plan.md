<!-- f5941f3a-3b16-4ed4-9ef9-05aab9dd2f7b 0e45c8e5-e3c0-446a-a823-b1d099c4e977 -->
# Technical Implementation Plan: Phase 2 - Deep Memory

This plan details the implementation of the "Deep Memory" system, focusing on high-impact, self-contained features: **Nemesis System**, **Legends**, and **Enhanced History Tracking**.

## Scope Summary

| Feature | Priority | Complexity | Status |

|---------|----------|------------|--------|

| Nemesis System | P0 | Medium | Planned |

| Legends System | P1 | Low | Planned |

| HistoricalEvent Upgrade | P1 | Low | Planned |

| HistorySearch | P1 | Medium | Planned |

| Trauma & Growth | P2 | Low | Planned |

| Narrative Arcs | -- | High | **Deferred** |

| CompNarrative | -- | Medium | **Deferred** |

## 1. Data Structures (`Source/GameState/DeepMemory.cs`)

Create a new file for Phase 2 data classes, keeping `StoryContext.cs` as the integration point.

### NemesisProfile

```csharp
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
}
```

### Legend

```csharp
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
}
```

### HistoricalEvent (Upgrade)

```csharp
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
}
```

## 2. StoryContext Updates (`Source/GameState/StoryContext.cs`)

Add new collections and maintain backward compatibility:

```csharp
// New Phase 2 collections
public List<NemesisProfile> Nemeses = new List<NemesisProfile>();
public List<Legend> Legends = new List<Legend>();
public List<HistoricalEvent> History = new List<HistoricalEvent>();

// Constants
private const int MAX_NEMESES = 10;
private const int MAX_LEGENDS = 20;
private const int MAX_HISTORY = 100;
```

**Migration Strategy**: Keep existing `RecentEvents`, `ColonistDeaths`, `MajorBattles` lists. New `History` list runs in parallel. Future phase can migrate old data or deprecate old lists.

## 3. Nemesis System Implementation

### 3.1 Detection (`Source/Storyteller/NemesisTracker.cs`)

```csharp
public static class NemesisTracker
{
    // Called from HarmonyPatches when hostile pawn exits map
    public static void OnHostilePawnExit(Pawn pawn, bool fled)
    {
        if (!ShouldPromoteToNemesis(pawn)) return;
        
        var profile = CreateNemesisProfile(pawn);
        StoryContext.Instance?.AddNemesis(profile);
    }
    
    private static bool ShouldPromoteToNemesis(Pawn pawn)
    {
        // Criteria:
        // 1. Pawn is humanlike and hostile
        // 2. Pawn dealt significant damage this battle OR
        // 3. Pawn killed a colonist OR
        // 4. Pawn has relationship with colonist
        // 5. Not already a Nemesis
    }
}
```

### 3.2 Harmony Patches

**Patch: Detect fleeing pawns**

```csharp
[HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap))]
public static class Pawn_ExitMap_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance, bool allowedToJoinOrCreateCaravan)
    {
        // Check if hostile and fleeing (not forming caravan)
        if (__instance.HostileTo(Faction.OfPlayer) && !allowedToJoinOrCreateCaravan)
        {
            NemesisTracker.OnHostilePawnExit(__instance, fled: true);
        }
    }
}
```

**Patch: Inject Nemesis into raids**

```csharp
[HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
public static class PawnGroupMaker_GeneratePawns_Patch
{
    [HarmonyPostfix]
    public static void Postfix(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
    {
        // If this faction has an active Nemesis, inject them
        var nemesis = NemesisTracker.GetActiveNemesisForFaction(parms.faction);
        if (nemesis != null)
        {
            var nemesisPawn = NemesisTracker.SpawnNemesisPawn(nemesis);
            __result = __result.Concat(new[] { nemesisPawn });
        }
    }
}
```

### 3.3 Nemesis Spawning

Recreate pawn from `NemesisProfile`:

- Use stored appearance data
- Apply stored skills/traits where possible
- Give appropriate equipment for raid
- Mark with custom Hediff `Hediff_Nemesis` for identification

## 4. Legends System Implementation

### 4.1 Art Detection (`Source/Storyteller/LegendTracker.cs`)

```csharp
[HarmonyPatch(typeof(CompQuality), nameof(CompQuality.SetQuality))]
public static class CompQuality_SetQuality_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CompQuality __instance, QualityCategory q)
    {
        if (q < QualityCategory.Masterwork) return;
        
        var thing = __instance.parent;
        var compArt = thing.TryGetComp<CompArt>();
        if (compArt == null) return;
        
        LegendTracker.OnMasterworkCreated(thing, compArt, q);
    }
}
```

### 4.2 Legend Creation

```csharp
public static class LegendTracker
{
    public static void OnMasterworkCreated(Thing artwork, CompArt compArt, QualityCategory quality)
    {
        var legend = new Legend
        {
            Id = Guid.NewGuid().ToString(),
            ArtworkLabel = artwork.Label,
            ArtworkTale = compArt.GenerateImageDescription(), // RimWorld's art text
            CreatorName = compArt.AuthorName,
            Quality = quality,
            CreatedDay = GenDate.DaysPassed,
            CreatedDateString = GetCurrentDateString()
        };
        
        // Only generate LLM summary for Legendary
        if (quality == QualityCategory.Legendary)
        {
            RequestMythicSummary(legend);
        }
        else
        {
            StoryContext.Instance?.AddLegend(legend);
        }
    }
    
    private static void RequestMythicSummary(Legend legend)
    {
        // Async LLM call to summarize art into 1-2 mythic sentences
        // On success: add legend with summary
        // On failure: add legend without summary (graceful degradation)
    }
}
```

## 5. History Search (`Source/Logic/HistorySearch.cs`)

### Scoring Algorithm

```csharp
public static class HistorySearch
{
    // Configurable weights
    public static float KeywordWeight = 2.0f;
    public static float EntityWeight = 3.0f;
    public static float RecencyWeight = 1.0f;
    public static float SignificanceWeight = 1.5f;
    
    public static List<HistoricalEvent> FindRelevantHistory(
        List<string> currentKeywords, 
        List<string> currentEntityIds,
        int maxResults = 5)
    {
        return StoryContext.Instance?.History
            .Where(e => !string.IsNullOrEmpty(e.Summary))
            .Select(e => new { Event = e, Score = ScoreEvent(e, currentKeywords, currentEntityIds) })
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Event)
            .ToList() ?? new List<HistoricalEvent>();
    }
    
    private static float ScoreEvent(HistoricalEvent evt, List<string> keywords, List<string> entityIds)
    {
        float score = 0;
        
        // Keyword overlap
        int keywordMatches = evt.Keywords.Intersect(keywords, StringComparer.OrdinalIgnoreCase).Count();
        score += keywordMatches * KeywordWeight;
        
        // Entity overlap
        int entityMatches = evt.ParticipantIds.Intersect(entityIds).Count();
        score += entityMatches * EntityWeight;
        
        // Recency (decay over 60 days)
        int daysAgo = GenDate.DaysPassed - evt.DayOccurred;
        float recencyFactor = Math.Max(0, 1 - (daysAgo / 60f));
        score += recencyFactor * RecencyWeight;
        
        // Base significance
        score += evt.SignificanceScore * SignificanceWeight;
        
        return score;
    }
}
```

## 6. Trauma & Growth (`Source/Storyteller/TraumaTracker.cs`)

### Detection

```csharp
public static class TraumaTracker
{
    private static int colonistCountAtStartOfPeriod;
    private static int periodStartDay;
    
    public static void OnTick()
    {
        // Check every day
        if (GenDate.DaysPassed == periodStartDay) return;
        
        // Reset tracking period every 3 days
        if (GenDate.DaysPassed - periodStartDay >= 3)
        {
            colonistCountAtStartOfPeriod = GetColonistCount();
            periodStartDay = GenDate.DaysPassed;
            return;
        }
        
        // Check for tragedy
        int currentCount = GetColonistCount();
        float lossRatio = 1 - (currentCount / (float)colonistCountAtStartOfPeriod);
        
        if (lossRatio >= 0.5f && currentCount > 0)
        {
            TriggerTraumaEvent(GetSurvivors());
        }
    }
}
```

### Growth Hediffs (`Defs/Hediffs/TraumaHediffs.xml`)

```xml
<HediffDef>
  <defName>Hediff_Shellshocked</defName>
  <label>shell-shocked</label>
  <description>The horrors of recent events weigh heavily on their mind.</description>
  <hediffClass>HediffWithComps</hediffClass>
  <defaultLabelColor>(0.6, 0.6, 0.8)</defaultLabelColor>
  <stages>
    <li>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>-0.05</offset></li>
      </capMods>
      <statOffsets>
        <MentalBreakThreshold>0.05</MentalBreakThreshold>
      </statOffsets>
    </li>
  </stages>
  <comps>
    <li Class="HediffCompProperties_Disappears">
      <disappearsAfterTicks>900000~1800000</disappearsAfterTicks> <!-- 15-30 days -->
    </li>
  </comps>
</HediffDef>
```

## 7. Prompt Engineering Updates

### PromptBuilder Changes

```csharp
public static string BuildEventPrompt(IncidentDef incident, IncidentParms parms)
{
    var sb = new StringBuilder();
    
    sb.AppendLine("COLONY CONTEXT:");
    sb.AppendLine(ColonyStateCollector.GetNarrationContext(parms, incident));
    
    // NEW: Add relevant history
    var relevantHistory = GetRelevantHistorySection(incident, parms);
    if (!string.IsNullOrEmpty(relevantHistory))
    {
        sb.AppendLine();
        sb.AppendLine("RELEVANT HISTORY:");
        sb.AppendLine(relevantHistory);
    }
    
    // NEW: Add Nemesis info if attacking faction has one
    var nemesisInfo = GetNemesisSection(parms?.faction);
    if (!string.IsNullOrEmpty(nemesisInfo))
    {
        sb.AppendLine();
        sb.AppendLine("RECURRING ENEMY:");
        sb.AppendLine(nemesisInfo);
    }
    
    // NEW: Add relevant Legends
    var legends = GetRelevantLegends(incident);
    if (!string.IsNullOrEmpty(legends))
    {
        sb.AppendLine();
        sb.AppendLine("COLONY LEGENDS:");
        sb.AppendLine(legends);
    }
    
    sb.AppendLine();
    sb.AppendLine("TASK: Write atmospheric flavor text...");
    
    return sb.ToString();
}
```

## 8. Debug Actions (`Source/Core/DebugActions.cs`)

Add new debug tools:

```csharp
[DebugAction("AI Narrator", "Spawn Test Nemesis", actionType = DebugActionType.Action)]
public static void SpawnTestNemesis() { ... }

[DebugAction("AI Narrator", "Create Test Legend", actionType = DebugActionType.Action)]
public static void CreateTestLegend() { ... }

[DebugAction("AI Narrator", "Trigger Trauma Event", actionType = DebugActionType.Action)]
public static void TriggerTraumaEvent() { ... }

[DebugAction("AI Narrator", "Log History Search", actionType = DebugActionType.Action)]
public static void LogHistorySearch() { ... }

[DebugAction("AI Narrator", "List All Nemeses", actionType = DebugActionType.Action)]
public static void ListNemeses() { ... }
```

## 9. Execution Order

### Phase 2a: Foundation (Week 1)

1. Create `DeepMemory.cs` with data structures
2. Update `StoryContext.cs` with new collections and persistence
3. Implement `HistoricalEvent` and migrate recording logic
4. Implement `HistorySearch` with unit tests

### Phase 2b: Nemesis (Week 2)

5. Implement `NemesisTracker` detection logic
6. Add flee/exit Harmony patches
7. Implement Nemesis pawn spawning
8. Add raid injection patch
9. Add Nemesis debug actions

### Phase 2c: Legends (Week 3)

10. Implement `LegendTracker`
11. Add CompQuality patch
12. Implement LLM mythic summary generation
13. Add Legend debug actions

### Phase 2d: Integration (Week 4)

14. Update `PromptBuilder` with history/nemesis/legend sections
15. Implement `TraumaTracker` and growth Hediffs
16. Comprehensive testing and edge case handling
17. Performance profiling and pruning verification

### To-dos

- [ ] Create DeepMemory.cs with NemesisProfile, Legend, HistoricalEvent classes
- [ ] Update `StoryContext.cs` with new collections and ExposeData
- [ ] Implement HistoricalEvent recording (upgrade from string lists)
- [ ] Implement `HistorySearch` with configurable scoring
- [ ] Create `NemesisTracker.cs` with detection logic
- [ ] Patch `Pawn.ExitMap` for flee detection
- [ ] Implement Nemesis pawn recreation from profile
- [ ] Patch `PawnGroupMakerUtility.GeneratePawns` for Nemesis injection
- [ ] Create `LegendTracker.cs` with art detection
- [ ] Patch `CompQuality.SetQuality` for Masterwork/Legendary
- [ ] Implement LLM mythic summary generation for Legends
- [ ] Update `PromptBuilder` with Relevant History section
- [ ] Update `PromptBuilder` with Nemesis section
- [ ] Update `PromptBuilder` with Legends section
- [ ] Implement `TraumaTracker` with casualty monitoring
- [ ] Create Trauma/Growth Hediff definitions
- [ ] Add debug actions for all new systems
- [ ] Add pruning logic for all capped collections