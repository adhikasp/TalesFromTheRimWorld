# Tales from the RimWorld

An AI-powered storyteller mod for RimWorld that transforms your colony's journey into a personalized narrative experience. The custom storyteller, **The Narrator**, intercepts incidents, gathers detailed colony context, and calls an LLM through OpenRouter to explain *why* events are happening and to surface meaningful player choices.

## Overview

The AI Narrator MVP focuses on three interaction points inspired by the product spec:
- **Storyteller selection**: Choose The Narrator during new colony setup and play with standard RimWorld difficulties.
- **Mod Settings panel**: Enter your OpenRouter API key, pick a model, tweak creativity, and control how often narrative popups and dilemmas appear.
- **Narrative surfaces**: Event popups, choice dialogs, and a Story Journal tab that archives every narrated moment.

## Features

- **AI-generated event narration**: Vanilla events pause briefly while the LLM delivers 2-4 sentences of atmospheric flavor text grounded in your current colony state.
- **Choice dilemmas with immediate consequences**: 1-2 times per season you receive moral decisions mapped to real RimWorld systems (pawns joining, resource drops, mood effects, faction goodwill, raids).
- **Persistent Story Journal**: A bottom-toolbar tab that groups entries by year, tags choices and milestones, and survives save/load cycles.
- **Adaptive pacing**: The storyteller component throttles API calls, respects rate limits, and downgrades gracefully to vanilla notifications when connectivity fails.
- **Customizable runtime**: Toggle notifications, enable/disable choice events, adjust creativity, select from curated OpenRouter models, and auto-pause when narratives appear.
- **Detailed context gathering**: Colony snapshots include colonist bios, relationships, threats, weather, resources, and history so the LLM remembers why factions hold grudges or why a colonist is under stress.

## Requirements

- RimWorld 1.4 or 1.5
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) mod
- OpenRouter API key (free tier available)
- .NET Framework 4.7.2 SDK (for local builds)

> **Build config:** Before building, override `RimWorldPath` to point at your RimWorld installation either inside `Source/AINarrator.csproj` or by running `dotnet build /p:RimWorldPath="C:\Games\RimWorld"`.

## Installation

1. Subscribe to the mod on Steam Workshop, **or**
2. Download and extract the repository into your RimWorld `Mods` folder.

### Getting an API Key

1. Go to [OpenRouter.ai](https://openrouter.ai)
2. Create a free account
3. Navigate to [API Keys](https://openrouter.ai/keys)
4. Create a new key and copy it
5. In RimWorld: Options → Mod Settings → Tales from the RimWorld → Paste your key and click **Test**

## Usage

### Selecting The Narrator

1. Start a new colony.
2. On the storyteller screen, pick **The Narrator** (appears beside Cassandra/Phoebe/Randy).
3. Choose your usual difficulty and commitment mode.
4. Land and let the AI author begin chronicling your story.

### Playing With The Narrator

#### Narrative Events

1. Vanilla storyteller queues an incident.
2. `ColonyStateCollector` snapshots colonists, relationships, threats, resources, weather, and recent history.
3. `OpenRouterClient` sends the prompt via async UnityWebRequest.
4. A custom popup titled **“The Narrator Speaks”** displays the prose plus a concise event summary.
5. Clicking **Continue** allows the vanilla incident to resolve, then the entry is logged to the journal.

#### Choice Dilemmas

- Rolled 1-2 times per season (configurable) after standard incidents.
- Each dialog provides 2-3 clearly explained options with immediate, mapped consequences (spawn pawn, drop resources, mood thoughts, faction changes, micro raids).
- Choices, their outcomes, and any notable follow-up hooks persist in the Story Journal for future callbacks.

#### Story Journal Tab

- Accessible via the new **Story** main button in the bottom-right toolbar.
- Entries are grouped by year and labeled as Event, Choice, or Milestone.
- Filter dropdown lets you focus on events, choices, or major achievements.
- Clicking an entry highlights its in-world date so you can cross-reference History tab data.

### Configuration

Open RimWorld → Options → Mod Settings → Tales from the RimWorld.

| Setting | Description | Notes |
|---------|-------------|-------|
| API Key | OpenRouter key used for all requests | Masked input with **Test** button |
| Model | Curated list (Claude, GPT-4o, Llama 3, etc.) | Swappable at runtime |
| Creativity | Temperature slider (0.3–1.0) | Lower = grounded, higher = surprising |
| Show Narratives | Toggle popup display | Can mute narration without disabling storyteller |
| Enable Choices | Enables seasonal dilemmas | Frequency tracks plan spec (1–2 per season) |
| Pause on Narrative | Auto-pauses RimWorld when popups appear | Recommended for intense combat events |
| Choice Range | Min/Max sliders for seasonal choices | Guards against spam or drought |

Status indicator lights (● Connected / ● Error / ● Not Configured) reflect the last **Test Connection** result so you can trust the integration before hopping in-game.

## Building from Source

### Prerequisites

- Windows with .NET Framework 4.7.2 SDK or Visual Studio targeting .NET Framework 4.7.2
- Local RimWorld installation for assembly references

### Build Steps

1. Clone the repository.
2. Ensure `RimWorldPath` inside `Source/AINarrator.csproj` points to your install or pass `/p:RimWorldPath="C:\Games\RimWorld"` via CLI.
3. `cd Source` and run `dotnet build -c Debug` (or Release). Visual Studio works as well.
4. Compiled DLLs are copied to `Assemblies/` (ignored by git, drop them into your RimWorld Mods folder for manual installs).

## Project Structure

  ```text
  TalesFromTheRimWorld/
  ├── About/
  │   └── About.xml              # Mod metadata
  ├── Assemblies/
  │   └── AINarrator.dll         # Compiled mod output
  ├── Defs/
  │   ├── Storyteller/
  │   │   └── LLMStoryteller.xml # Storyteller definition
  │   ├── MainTabs/
  │   │   └── StoryJournalTab.xml
  │   └── WorldComponents/
  ├── Source/
  │   ├── Core/                  # Mod entry + settings UI
  │   ├── API/                   # OpenRouter client + DTOs + prompts
  │   ├── GameState/             # Colony state + world components
  │   ├── Storyteller/           # Incident interception + mapping
  │   └── UI/                    # Dialogs + main tab window
  └── README.md
  ```

## Technical Architecture

- **Harmony bootstrap (`AINarratorMod.cs`)**: Applies patches, loads settings, and registers world components.
- **StoryContext WorldComponent**: Persists journal entries, rate limits, battle history, death records, and other long-lived narrative data via `IExposable`.
- **ColonyStateCollector**: Aggregates colonist bios, relationships, mental states, weather, threats, infrastructure, prisoners, animals, and resources for prompt context.
- **OpenRouterClient + PromptBuilder**: Formats the system prompt, injects colony context, and executes async UnityWebRequest calls (10-second timeout, graceful fallback).
- **StorytellerComp_LLM**: Extends vanilla storyteller intervals, queues narration requests before yielding incidents, and rolls for seasonal choice events.
- **EventMapper**: Converts parsed LLM consequences into RimWorld actions (spawn pawn, drop pods, thoughts, guilt, raids, faction goodwill).
- **UI Layer**: `Dialog_NarrativePopup`, `Dialog_StoryChoice`, and `MainTabWindow_StoryJournal` implement the custom windows mocked up in the product spec.

## LLM Context & Data Capture

The enrichment plan drives a comprehensive context payload so AI prose references real history.

| Category | Data Collected | Narrative Impact |
|----------|----------------|------------------|
| Colony basics | Name, biome, season, time of day, weather, active game conditions | Sets tone and atmosphere |
| Characters | Colonist names, skills/roles, backstories, traits, mental states, inspirations, current jobs | Enables character-driven drama |
| Relationships | Family ties, romances, rivalries, social memories, recent interactions | Fuels interpersonal story hooks |
| History & memory | Recent incidents, major battles, colonist deaths with cause/date, heroic actions, rescued prisoners | Provides “remember when…” callbacks |
| Resources & infrastructure | Wealth, food, medicine, silver, research progress, defenses, hospitals, power grid | Grounds stakes for raids, disasters, and hope events |
| Prisoners, animals, threats | Prisoner status, recruitment progress, bonded animals, trained beasts, ongoing threats (sieges, infestations) | Allows dilemmas about mercy, sacrifice, or tactical choices |
| Factions & diplomacy | Goodwill, alliances, hostilities, notable past interactions | Justifies raids, negotiations, and political intrigue |

All context gets pruned to stay within the ~400-token budget outlined in the technical plan.

## API Usage & Costs

- Each request budgets ~650–850 tokens (system prompt ~300, context ~200–400, response cap ~150).
- Default rate limiting targets ≤50 calls per in-game day; additional guardrails pause requests if the OpenRouter response is slow or the quota is exhausted.
- Timeouts (>10s), invalid keys, or malformed responses skip narration, log a warning (`[AI Narrator]`), and allow the vanilla incident to proceed to keep the game stable.
- OpenRouter offers generous free tiers; pick cost-effective models if you plan for long sessions.

## Roadmap

1. **Phase 1 – Foundation (Done)**: Custom storyteller, narrative popups, simple choice events, Story Journal MVP.
2. **Phase 2 – Deep Memory (Current work)**: Track colonist arcs, faction grudges, relationship webs, and reference them in future events.
3. **Phase 3 – Branching Narratives**: Multi-stage arcs where choices create divergent outcomes and delayed consequences.
4. **Phase 4 – Dynamic World Events**: AI-generated incidents beyond vanilla definitions plus political intrigue and mysteries.
5. **Phase 5 – Personality & Themes**: Selectable narrator personas, thematic sliders, and content preferences for replayability.
6. **Phase 6 – Community & Polish**: Story exports, seed sharing, screenshot captions, and performance/cost tuning.

## Troubleshooting

- **"Not Configured" banner**: Enter a valid API key, press **Test**, and ensure the status indicator shows ● Connected before playing.
- **No narratives**: Confirm The Narrator is the active storyteller, `Show Narratives` is enabled, and you have not exceeded the daily request cap.
- **Choice dialogs never appear**: Increase the seasonal min/max sliders or ensure you survived long enough for the roll (1-2 per season per product spec).
- **API errors / timeouts**: Verify your OpenRouter balance, try a different model, or reduce concurrent RimWorld network mods. The mod logs `[AI Narrator]` errors to `Player.log` for inspection.
- **World generation crash**: Ensure `Defs/Storyteller/LLMStoryteller.xml` retains `ParentName="BaseStoryteller"` so required population curves exist.

## License

MIT License - Feel free to modify and distribute.

## Credits

- Built for RimWorld by Ludeon Studios
- Powered by OpenRouter API
- Uses Harmony for patching
