<!-- a6a62132-2978-4a05-b845-b5dbc5f44159 de663a07-fb18-412a-87b3-efc400acc8c6 -->
# Product Spec: Phase 2 - Deep Memory

## Vision

Transform the AI from an *observer* of current events into a *historian* that remembers *who* was involved in past conflicts, *what* grudges were formed, and *how* legendary deeds shaped the colony's identity. Focus on high-impact, self-contained features that create memorable emergent stories.

## Design Philosophy

- **Emergent over Authored**: Enhance RimWorld's natural storytelling rather than railroad it
- **Ship What Works**: Prioritize features that are self-contained and testable
- **Graceful Degradation**: Systems must handle edge cases (pruned data, dead pawns, destroyed factions)

## Core Features (Priority Order)

### 1. Nemesis System (Primary Feature)

Named enemies who survive battles become recurring characters with personal grudges.

-   **Trigger Conditions**:
    -   Enemy pawn flees the map after dealing significant damage (downed a colonist, killed someone)
    -   Enemy pawn has an existing relationship with a colonist (ex-lover, family member)
    -   Enemy pawn survived multiple engagements with the colony
-   **Data Captured** (`NemesisProfile`):
    -   Pawn appearance, name, skills, traits
    -   Faction affiliation
    -   "Grudge reason" (who they fought, what happened)
    -   Encounter count and last seen date
-   **Re-injection**:
    -   Patch raid generation to force-spawn Nemesis pawns when their faction attacks
    -   Spawn as raid leader or in a prominent position
    -   Generate personalized taunts/narrative referencing past encounters
-   **Limits**:
    -   Maximum 10 active Nemeses (oldest/least significant pruned)
    -   Nemesis "retires" after 3 encounters or if their faction is destroyed

### 2. Legends System (Secondary Feature)

Masterwork and Legendary artworks become part of the colony's mythology.

-   **Trigger**: `CompQuality.SetQuality` produces Masterwork or Legendary quality
-   **Data Captured** (`Legend`):
    -   Art description/tale (from RimWorld's art generator)
    -   Creator name and creation date
    -   Location in colony
    -   LLM-generated mythic summary (1-2 sentences)
-   **Integration**:
    -   Legends injected into narration context when relevant topics arise
    -   Example: *"The raiders hesitate, recognizing the statue depicting 'Maria's Last Stand' that guards your gates."*
-   **Limits**:
    -   Maximum 20 Legends stored
    -   Only generate LLM summary for Legendary items (Masterwork stored but not summarized)

### 3. Enhanced History Tracking

Upgrade existing event tracking with better structure and retrieval.

-   **HistoricalEvent**: Enhanced event record replacing simple strings
    -   UUID for potential future linking
    -   Keywords (auto-extracted: faction names, pawn names, event type)
    -   Participating entity IDs
    -   Significance score (for pruning decisions)
-   **Retrieval Logic** (`HistorySearch`):
    -   Score events by: keyword overlap, entity relevance, recency, significance
    -   Return top N most relevant events for current context
    -   Configurable weights (for tuning)
-   **"Previously on RimWorld"**:
    -   Dynamic prompt section: *"Relevant History: Last year, Maria lost her husband to the Empire. The pirate Vance escaped after killing your cook."*

### 4. Trauma & Growth (Tertiary Feature)

Narrative-driven character development after major events.

-   **Trigger Detection**:
    -   Major tragedy: >50% casualties in <3 days
    -   Personal loss: Colonist's bonded pawn/lover dies
    -   Heroic moment: Colonist single-handedly saves the colony
-   **Mechanic**:
    -   Offer player a choice popup: "How did [Pawn] change from this experience?"
    -   Options grant temporary Hediffs (30-60 day duration) with narrative flavor
    -   Examples: "Shell-shocked" (mood debuff), "Hardened" (pain threshold buff), "Determined" (work speed buff)
-   **Balance Note**: Initial implementation uses mild effects; tuning deferred to playtesting

## Deferred Features (Future Phases)

### Narrative Arcs (Deferred)

Full arc state machines with climax meters are deferred. The complexity of:

- Generic trigger detection across hundreds of event types
- Arc state persistence and edge case handling
- Balancing authored arcs with emergent gameplay

...outweighs the benefit for Phase 2. Instead, let Nemesis encounters and Legends create *implicit* arcs that players recognize naturally.

**Revisit Condition**: After shipping Nemesis + Legends, if players report wanting more structured storylines, design a minimal arc system based on observed usage patterns.

### Complex Relationship Web (Deferred)

Tracking "social memories" beyond RimWorld's opinion system is interesting but:

- Requires extensive Harmony patching of social systems
- Overlap with existing game mechanics
- High maintenance burden

**Revisit Condition**: If Nemesis system proves valuable, extend the pattern to track "rival colonists" or "bonded pairs" using similar infrastructure.

## Technical Constraints

### Memory & Performance

-   **HistoricalEvent**: Cap at 100 entries, prune by significance score
-   **NemesisProfile**: Cap at 10 entries, prune oldest inactive
-   **Legend**: Cap at 20 entries, prune oldest
-   **CompNarrative** (if implemented): Lightweight—only store event UUIDs, not full data

### Failure Modes

-   **Nemesis faction destroyed**: Mark Nemesis as "retired", stop spawning
-   **Legend artwork destroyed**: Keep Legend in lore but mark as "lost relic"
-   **Referenced event pruned**: LLM prompt gracefully omits missing references
-   **Pawn renamed/cloned**: Use ThingID not name for entity tracking

## Success Metrics

-   **Nemesis Engagement**: % of Nemeses that appear in 2+ raids before retirement
-   **Legend References**: % of narrations that reference a Legend when contextually appropriate
-   **Continuity Score**: % of generated events that reference past events >7 days old

### To-dos

- [ ] Implement NemesisProfile data structure and persistence
- [ ] Patch Pawn flee/exit events to detect Nemesis candidates
- [ ] Patch raid generation to inject Nemesis pawns
- [ ] Implement Legend data structure and persistence
- [ ] Patch CompQuality.SetQuality for Masterwork/Legendary detection
- [ ] Implement HistoricalEvent structure (upgrade from string lists)
- [ ] Implement HistorySearch with configurable scoring weights
- [ ] Update PromptBuilder with "Relevant History" section
- [ ] Implement Trauma detection and Hediff-based growth system
- [ ] Add debug actions for testing (Spawn Nemesis, Generate Legend, Trigger Trauma)