<!-- 29fa5df9-7d3f-4649-9dcc-0f12d5fcf4b8 54f2d74e-45ff-49cc-9906-dcd52da4f182 -->
# Phase 1 Product Specification: AI Narrator MVP

## Scope Summary

Phase 1 delivers a **playable proof-of-concept** that demonstrates core narrative storytelling. Players experience enhanced RimWorld events with AI-generated flavor text, occasional choice dilemmas, and a persistent story journal.

---

## User Interaction Points

### 1. Game Start: Storyteller Selection

**Location:** New Colony Setup Screen (vanilla "Scenario" flow)

**User Action:** Player selects "The Narrator" from the storyteller dropdown alongside Cassandra, Phoebe, and Randy.

**UI Elements:**

- Storyteller portrait/icon (custom art asset needed)
- Description text: *"An AI author who weaves your colony's struggles into an unfolding narrative. Events carry meaning, context, and consequence."*
- Difficulty settings remain vanilla (Peaceful → Losing is Fun)

**Flow:**

```
Colony Setup → Select Storyteller → Choose "The Narrator" → Proceed to landing site
```

---

### 2. Mod Settings Panel

**Location:** Options → Mod Settings → AI Narrator

**User Actions:**

- Enter/paste API key (OpenRouter)
- Select LLM model from dropdown (e.g., Claude 3.5 Sonnet, GPT-4o, Llama 3)
- Adjust temperature slider (0.3-1.0, default 0.7)
- Toggle "Show API status" indicator
- Test connection button

**UI Layout:**

```
┌─────────────────────────────────────────────┐
│ AI Narrator Settings                        │
├─────────────────────────────────────────────┤
│ API Key: [••••••••••••••••] [Test]         │
│ Status: ● Connected                         │
│                                             │
│ Model: [Claude 3.5 Sonnet      ▼]          │
│                                             │
│ Creativity: [====●=====] 0.7                │
│ (Lower = consistent, Higher = surprising)   │
│                                             │
│ □ Show narrative notifications              │
│ □ Enable choice events                      │
│   Choice events per season: 1-2             │
│   Minimum: [===●======] 1                   │
│   Maximum: [====●=====] 2                   │
└─────────────────────────────────────────────┘
```

**Validation:**

- API key format check
- Connection test with timeout handling
- Graceful fallback message if API unreachable

---

### 3. Narrative Event Popup

**Trigger:** Any vanilla incident fires (raid, wanderer joins, solar flare, etc.)

**User Experience:**

1. Game pauses briefly (configurable)
2. Narrative popup appears with AI-generated flavor text
3. Player reads context, clicks "Continue" or presses Enter
4. Vanilla event executes as normal

**UI Mockup:**

```
┌─────────────────────────────────────────────┐
│ ❝ THE NARRATOR SPEAKS ❞                     │
├─────────────────────────────────────────────┤
│                                             │
│  The harsh winds of Aprimay carry more      │
│  than frost tonight. A desperate band       │
│  from the Crimson Reavers, starving and     │
│  emboldened by hunger, sets their sights    │
│  on your supplies. They've been watching.   │
│  They know your defenses.                   │
│                                             │
│  ═══════════════════════════════════════    │
│  RAID: Crimson Reavers (7 enemies)          │
│                                             │
│                         [Continue →]        │
└─────────────────────────────────────────────┘
```

**Content Rules:**

- 2-4 sentences maximum
- Reference colony name, colonist names when relevant
- Contextualize the vanilla event (WHY is this happening)
- Never spoil mechanical details beyond vanilla notification

---

### 4. Choice Event Dialog

**Trigger:** Random chance during storyteller tick (separate from vanilla events)

**Frequency:** 1-2 per in-game season (configurable)

**User Experience:**

1. Game pauses
2. Choice dialog presents a dilemma
3. Player selects one of 2-3 options
4. Immediate consequence executes
5. Choice logged to story journal

**UI Mockup:**

```
┌─────────────────────────────────────────────┐
│ A CHOICE AWAITS                             │
├─────────────────────────────────────────────┤
│                                             │
│  A wounded stranger collapses at your       │
│  gates, clutching a satchel. She gasps      │
│  that raiders are hunting her - and the     │
│  satchel contains medicine worth a fortune. │
│                                             │
│  Your colonist Mira recognizes her accent.  │
│  She's from the Outlander Union.            │
│                                             │
├─────────────────────────────────────────────┤
│ ○ Take her in and tend her wounds           │
│   → Gain a guest, medicine, possible ally   │
│                                             │
│ ○ Take the medicine, leave her outside      │
│   → Gain medicine, mood penalty             │
│                                             │
│ ○ Turn her away with her belongings         │
│   → She leaves, no immediate effect         │
└─────────────────────────────────────────────┘
```

**Choice Design Principles (Phase 1):**

- All consequences are IMMEDIATE (no delayed effects)
- Options clearly hint at outcomes (no hidden gotchas in MVP)
- Consequences map to existing game systems:
  - Spawn pawn (wanderer joins)
  - Spawn items (resource drop)
  - Mood effects (thought applied)
  - Faction relation changes
  - Small raids/attacks

---

### 5. Story Journal Tab

**Location:** New tab in the bottom-right info panel (alongside History, Factions)

**User Actions:**

- Scroll through chronological narrative entries
- Click entry to jump to that date in History tab
- Filter by entry type (Events, Choices, Milestones)

**UI Layout:**

```
┌─────────────────────────────────────────────┐
│ STORY JOURNAL                    [Filter ▼] │
├─────────────────────────────────────────────┤
│ ▼ 5507                                      │
│   ├─ Aprimay 3rd                            │
│   │  "Colony New Hope founded on the edge   │
│   │   of the boreal forest. Five souls      │
│   │   against the world."                   │
│   │                                         │
│   ├─ Aprimay 7th ★ CHOICE                   │
│   │  The stranger at the gates...           │
│   │  → Took her in                          │
│   │                                         │
│   ├─ Aprimay 15th                           │
│   │  "The Crimson Reavers came with the    │
│   │   dawn. They left with nothing."        │
│   │                                         │
│   └─ Jugust 2nd                             │
│      "A cargo pod fell from the heavens..." │
└─────────────────────────────────────────────┘
```

**Auto-Generated Entries:**

- Colony founding (game start)
- Every narrated event
- Every choice made (with outcome)
- Major milestones (first death, first prisoner, first trade)

---

## Gameplay Flow Diagrams

### Flow A: Standard Event Narration

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Storyteller │────▶│  Event Queue │────▶│ LLM Request  │
│    Tick      │     │  (Vanilla)   │     │   (Async)    │
└──────────────┘     └──────────────┘     └──────┬───────┘
                                                  │
                     ┌──────────────┐             │
                     │ Show Popup   │◀────────────┘
                     │ with Flavor  │
                     └──────┬───────┘
                            │
                     ┌──────▼───────┐     ┌──────────────┐
                     │ Player Clicks│────▶│Execute Event │
                     │  "Continue"  │     │ (Vanilla)    │
                     └──────────────┘     └──────┬───────┘
                                                  │
                                          ┌──────▼───────┐
                                          │ Log to Story │
                                          │   Journal    │
                                          └──────────────┘
```

### Flow B: Choice Event

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Storyteller │────▶│ Choice Roll  │────▶│ LLM Request  │
│    Tick      │     │ (% chance)   │     │(Generate Evt)│
└──────────────┘     └──────────────┘     └──────┬───────┘
                                                  │
                     ┌──────────────┐             │
                     │ Parse LLM    │◀────────────┘
                     │  Response    │
                     └──────┬───────┘
                            │
                     ┌──────▼───────┐
                     │ Show Choice  │
                     │   Dialog     │
                     └──────┬───────┘
                            │
┌──────────────┐     ┌──────▼───────┐
│ Map Outcome  │◀────│Player Selects│
│ to Game Sys  │     │   Option     │
└──────┬───────┘     └──────────────┘
       │
┌──────▼───────┐     ┌──────────────┐
│Execute Effect│────▶│ Log Choice + │
│(spawn, mood) │     │   Outcome    │
└──────────────┘     └──────────────┘
```

---

## Story Context (What the LLM Knows)

### Data Sent Per Request

| Data Point | Example | Purpose |

|------------|---------|---------|

| Colony name | "New Hope" | Personalization |

| Colony age | "47 days" | Timeline awareness |

| Colonist names + roles | "Mira (Doctor), Jake (Shooter)" | Character references |

| Recent major events (last 3-5) | "Raid survived, prisoner recruited" | Continuity |

| Current season/biome | "Winter, Boreal Forest" | Atmosphere |

| Current event type | "Raid by Crimson Reavers, 7 enemies" | Context for narration |

### Token Budget (Phase 1)

- **System prompt:** ~300 tokens
- **Context payload:** ~200-400 tokens  
- **Response limit:** ~150 tokens
- **Total per call:** ~650-850 tokens

---

## Error States and Fallbacks

| Scenario | User Experience |

|----------|-----------------|

| No API key configured | Small banner: "Configure AI Narrator in Mod Settings to enable narrative mode" |

| API timeout (>10s) | Skip narration, execute event normally, log warning |

| Invalid API key | Settings panel shows error, events execute without narration |

| LLM returns unparseable response | Use generic fallback text: "Events unfold..." |

| Rate limited | Queue events, show cached/generic text, retry later |

---

## Save/Load Behavior

**Saved Data (via IExposable):**

- Story journal entries (all)
- Choice history
- Recent context buffer (last 5 events)
- Running narrative themes (simple tags)

**NOT Saved:**

- API key (stored in mod config, not save file)
- Pending LLM requests (re-requested on load)

---

## Performance Considerations

- LLM calls are **async** - never block the game thread
- Pre-fetch narration when event is queued (before player sees it)
- Cache generic narrations for common event types
- Maximum 1 LLM call per in-game day (configurable)
- Story journal entries capped at 200 (oldest pruned)

---

## Out of Scope for Phase 1

- Event consequences that span multiple in-game days
- AI "remembering" specific colonist actions/kills
- Custom events not based on vanilla incidents
- Narrator personality selection
- Story arc/branching narrative
- Colonist-specific targeted events

These features are explicitly deferred to Phase 2+.

### To-dos

- [ ] Update About.xml and .csproj with Newtonsoft.Json dependency
- [ ] Create ModSettings.cs with API key, model dropdown, temperature slider UI
- [ ] Implement OpenRouterClient.cs with async UnityWebRequest calls
- [ ] Create ColonyStateCollector.cs to gather pawns, resources, context
- [ ] Create StoryContext.cs with IExposable save/load as WorldComponent
- [ ] Create StorytellerComp_LLM.cs that intercepts vanilla event intervals
- [ ] Create EventMapper.cs mapping LLM choice outputs to IncidentDefs
- [ ] Create Dialog_StoryChoice.cs for multiple-choice event popups
- [ ] Create Dialog_NarrativePopup.cs for event flavor text display
- [ ] Create MainTabWindow_StoryJournal.cs for the story journal UI
- [ ] Create LLMStoryteller.xml storyteller definition with portrait
- [ ] Test full flow: settings, API calls, event triggering, save/load