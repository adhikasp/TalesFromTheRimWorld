<!-- 1708b90c-6de4-430c-ad62-fc3afd8f7308 512ade73-acee-4bdd-b74c-dd71c1e82e3b -->
# Phase 1: AI Narrator Technical Implementation Plan

## Architecture Overview

```
Source/
├── Core/
│   ├── AINarratorMod.cs          # Mod entry point, settings holder
│   └── ModSettings.cs             # Settings data + UI
├── API/
│   ├── OpenRouterClient.cs        # Async HTTP client
│   ├── LLMRequest.cs              # Request/response DTOs
│   └── PromptBuilder.cs           # System prompts, context formatting
├── GameState/
│   ├── ColonyStateCollector.cs    # Gather pawns, resources, events
│   └── StoryContext.cs            # WorldComponent, IExposable persistence
├── Storyteller/
│   ├── StorytellerComp_LLM.cs     # Intercepts vanilla events
│   └── EventMapper.cs             # Maps LLM outputs → IncidentDefs
├── UI/
│   ├── Dialog_NarrativePopup.cs   # Event flavor text window
│   ├── Dialog_StoryChoice.cs      # Choice event dialog
│   └── MainTabWindow_StoryJournal.cs  # Story journal tab
Defs/
├── Storyteller/LLMStoryteller.xml # Storyteller definition
└── MainTabs/StoryJournalTab.xml   # Tab definition
```

---

## 1. Project Setup and Dependencies

**Files:** `About/About.xml`, `Source/HelloWorldMod.csproj`

Update mod metadata and add required references:

- Rename namespace from `HelloWorldMod` to `AINarrator`
- Add `Newtonsoft.Json` NuGet package (RimWorld ships with it)
- Add `UnityEngine.UnityWebRequestModule` reference for async HTTP
- Update `About.xml` with new mod identity: "AI Narrator"

---

## 2. Settings System

**Files:** `AINarratorMod.cs`, `ModSettings.cs`

```csharp
// ModSettings.cs - Key fields
public string ApiKey = "";
public string SelectedModel = "anthropic/claude-3.5-sonnet";
public float Temperature = 0.7f;
public bool ShowNarrativeNotifications = true;
public bool EnableChoiceEvents = true;
public int ChoiceEventsPerSeasonMin = 1;
public int ChoiceEventsPerSeasonMax = 2;
```

**UI Elements:**

- Password-masked API key text field with Test button
- Dropdown listing OpenRouter models (hardcoded list for MVP)
- Horizontal slider for temperature (0.3-1.0)
- Checkboxes for toggles
- Sliders for choice events per season (min: 0-5, max: min-10)
- Status indicator (Connected/Error/Not Configured)

Use `Listing_Standard` for layout. Test button triggers `OpenRouterClient.TestConnection()`.

---

## 3. OpenRouter API Client

**File:** `API/OpenRouterClient.cs`

```csharp
public static class OpenRouterClient
{
    private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";
    
    public static void RequestNarration(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        // Use UnityWebRequest in coroutine via LongEventHandler
        // POST with JSON body, Authorization header
    }
}
```

**Key Implementation Details:**

- Use `UnityWebRequest.Post()` with `UploadHandlerRaw` for JSON body
- Run via `LongEventHandler.QueueLongEvent()` to avoid blocking
- 10-second timeout with graceful fallback
- Parse JSON response with Newtonsoft.Json

---

## 4. Colony State Collector

**File:** `GameState/ColonyStateCollector.cs`

Gathers context for LLM prompts:

```csharp
public static class ColonyStateCollector
{
    public static ColonySnapshot GetSnapshot()
    {
        return new ColonySnapshot
        {
            ColonyName = Find.CurrentMap?.Parent?.Label,
            ColonyAgeDays = GenDate.DaysPassed,
            Colonists = GetColonistSummaries(),  // Name, role, notable skill
            RecentEvents = StoryContext.Instance?.RecentEvents,
            Season = GenLocalDate.Season(Find.CurrentMap),
            Biome = Find.CurrentMap?.Biome?.label
        };
    }
}
```

---

## 5. Story Context (Persistence)

**File:** `GameState/StoryContext.cs`

WorldComponent with `IExposable` for save/load:

```csharp
public class StoryContext : WorldComponent, IExposable
{
    public static StoryContext Instance;
    
    public List<JournalEntry> JournalEntries = new();
    public List<string> RecentEvents = new();  // Last 5 for context
    public Dictionary<string, string> ChoiceHistory = new();
    
    public override void ExposeData()
    {
        Scribe_Collections.Look(ref JournalEntries, "journalEntries", LookMode.Deep);
        Scribe_Collections.Look(ref RecentEvents, "recentEvents", LookMode.Value);
        // ... etc
    }
}
```

`JournalEntry` class stores: date, text, type (Event/Choice/Milestone), choice made (if applicable).

---

## 6. Storyteller Component

**File:** `Storyteller/StorytellerComp_LLM.cs`

Extends `StorytellerComp` to intercept event firing:

```csharp
public class StorytellerComp_LLM : StorytellerComp
{
    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        foreach (var incident in base.MakeIntervalIncidents(target))
        {
            // Queue narrative request BEFORE yielding
            RequestNarrativeFor(incident);
            yield return incident;
        }
        
        // Roll for choice event (1-2 per season)
        if (ShouldTriggerChoiceEvent())
            TriggerChoiceEvent();
    }
}
```

**Event Hook Strategy:**

- Use Harmony patch on `Storyteller.TryFire()` to intercept before event popup
- Display `Dialog_NarrativePopup` with AI flavor text
- On "Continue", let vanilla event execute normally

---

## 7. Event Mapper

**File:** `Storyteller/EventMapper.cs`

Maps LLM choice responses to game effects:

```csharp
public static class EventMapper
{
    public static void ExecuteConsequence(ChoiceConsequence consequence)
    {
        switch (consequence.Type)
        {
            case "spawn_pawn":
                // Use IncidentWorker_WandererJoin logic
                break;
            case "spawn_items":
                // DropPodUtility or direct spawn
                break;
            case "mood_effect":
                // Apply ThoughtDef to colonists
                break;
            case "faction_relation":
                // Modify faction goodwill
                break;
            case "trigger_raid":
                // Queue IncidentDef raid
                break;
        }
    }
}
```

---

## 8. Narrative Popup Dialog

**File:** `UI/Dialog_NarrativePopup.cs`

```csharp
public class Dialog_NarrativePopup : Window
{
    private string narrativeText;
    private string eventSummary;
    private Action onContinue;
    
    public override Vector2 InitialSize => new(500f, 350f);
    
    public override void DoWindowContents(Rect inRect)
    {
        // Header: "THE NARRATOR SPEAKS"
        // Body: narrativeText (2-4 sentences)
        // Divider
        // Event summary (vanilla info)
        // "Continue" button
    }
}
```

Styled with custom fonts, border decorations using `Widgets` API.

---

## 9. Choice Event Dialog

**File:** `UI/Dialog_StoryChoice.cs`

```csharp
public class Dialog_StoryChoice : Window
{
    private ChoiceEvent choiceEvent;  // Contains prompt + List<ChoiceOption>
    
    public override void DoWindowContents(Rect inRect)
    {
        // Header: "A CHOICE AWAITS"
        // Narrative text
        // Radio buttons for each option with hint text
        // Selected option highlights
        // Confirm button (disabled until selection)
    }
}
```

`ChoiceOption` contains: label, hint text, and `ChoiceConsequence` (type + parameters).

---

## 10. Story Journal Tab

**File:** `UI/MainTabWindow_StoryJournal.cs`

```csharp
public class MainTabWindow_StoryJournal : MainTabWindow
{
    private Vector2 scrollPosition;
    private JournalFilter filter = JournalFilter.All;
    
    public override void DoWindowContents(Rect inRect)
    {
        // Filter dropdown (All/Events/Choices/Milestones)
        // Scrollable list of JournalEntry grouped by year
        // Each entry: date, icon, text, choice indicator if applicable
    }
}
```

---

## 11. XML Definitions

**File:** `Defs/Storyteller/LLMStoryteller.xml`

```xml
<StorytellerDef>
  <defName>LLMNarrator</defName>
  <label>The Narrator</label>
  <description>An AI author who weaves your colony's struggles...</description>
  <listOrder>40</listOrder>
  <comps>
    <li Class="AINarrator.StorytellerCompProperties_LLM">
      <minDaysPassed>0</minDaysPassed>
    </li>
  </comps>
</StorytellerDef>
```

**File:** `Defs/MainTabs/StoryJournalTab.xml`

```xml
<MainButtonDef>
  <defName>StoryJournal</defName>
  <label>Story</label>
  <tabWindowClass>AINarrator.MainTabWindow_StoryJournal</tabWindowClass>
  <order>45</order>
</MainButtonDef>
```

---

## Implementation Order

| Step | Files | Dependency |

|------|-------|------------|

| 1 | Project setup, About.xml, .csproj | None |

| 2 | ModSettings.cs, AINarratorMod.cs | Step 1 |

| 3 | OpenRouterClient.cs, LLMRequest.cs | Step 2 |

| 4 | ColonyStateCollector.cs | Step 1 |

| 5 | StoryContext.cs | Step 4 |

| 6 | PromptBuilder.cs | Steps 4-5 |

| 7 | Dialog_NarrativePopup.cs | Step 3 |

| 8 | StorytellerComp_LLM.cs, XML defs | Steps 5-7 |

| 9 | Dialog_StoryChoice.cs | Steps 6-7 |

| 10 | EventMapper.cs | Step 9 |

| 11 | MainTabWindow_StoryJournal.cs, XML | Step 5 |

| 12 | Integration testing | All |

---

## Key Technical Decisions

1. **Harmony Patching:** Required to intercept `Storyteller.TryFire()` before vanilla UI shows
2. **Async Pattern:** Use `LongEventHandler` + callbacks, never block game thread
3. **Fallback Strategy:** If API fails, skip narration and execute vanilla event normally
4. **Token Budget:** ~650-850 tokens per call (system 300 + context 400 + response 150)
5. **Journal Limit:** Cap at 200 entries, prune oldest when exceeded

### To-dos

- [ ] Update About.xml, .csproj with dependencies, rename namespace to AINarrator
- [ ] Create ModSettings.cs and AINarratorMod.cs with settings UI
- [ ] Implement OpenRouterClient.cs with async UnityWebRequest calls
- [ ] Create ColonyStateCollector.cs to gather colony context for prompts
- [ ] Create StoryContext.cs WorldComponent with IExposable persistence
- [ ] Create PromptBuilder.cs for system prompts and context formatting
- [ ] Create Dialog_NarrativePopup.cs for event flavor text display
- [ ] Create StorytellerComp_LLM.cs with Harmony patches and XML defs
- [ ] Create Dialog_StoryChoice.cs for multiple-choice event popups
- [ ] Create EventMapper.cs mapping LLM outputs to game effects
- [ ] Create MainTabWindow_StoryJournal.cs and XML tab definition
- [ ] Test full flow: settings, API calls, event triggering, save/load