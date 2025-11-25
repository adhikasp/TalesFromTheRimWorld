<!-- 57736bff-17d9-48fd-8552-d978eacc45c7 dc1c257f-c81a-4506-8117-04ff655fe866 -->
# AI Narrator: LLM-Powered Storyteller Mod

## Vision Statement

Transform RimWorld from a "things happen to you" simulation into a **personalized narrative experience** where an AI author crafts a story specifically for YOUR colony. Every raid has a reason. Every stranger has a past. Every disaster advances a plot only your colony will ever experience.

---

## Core Gameplay Pillars

### 1. Narrative Coherence

Unlike vanilla storytellers that roll dice, the AI remembers **everything**. That raider you captured 2 years ago? The AI knows his tribe wants revenge. The relationship between your doctor and your cook? The AI weaves it into events.

**Player Experience:**

- Events feel like chapters, not random occurrences
- "Why is this happening?" always has an answer
- Colony history shapes future events

### 2. Meaningful Choices

Regular "story events" pause the game and present dilemmas with real consequences. Not just "accept wanderer yes/no" but **morally complex decisions** that shape your colony's identity.

**Example Choice Event:**

> *"A wounded traveler collapses at your gates. She claims to be fleeing a pirate warband - but your warden notices the brand on her neck marks her as a deserter from that very band. She begs for sanctuary."*

>

> - **Give her shelter** → She becomes a colonist (hidden: 40% chance pirates track her here within a quadrum)

> - **Turn her away** → She leaves (hidden: she may return as a raider, or die in the wild affecting mood)

> - **Interrogate her** → Learn the truth but risk her mental break

> - **Offer conditional sanctuary** → She must prove loyalty through dangerous tasks

### 3. Adaptive Difficulty Through Story

The AI doesn't just scale raid points - it crafts **appropriate challenges**. Thriving colony? The AI introduces political intrigue and rival factions. Struggling colony? It sends hope: a trader with exactly what you need, a wanderer with critical skills.

**Difficulty feels earned, not arbitrary:**

- Wealth-based scaling still exists but is narratively justified
- "You've grown powerful. Word of your prosperity reaches the pirate king..."
- Disasters feel like plot points, not punishment

### 4. Character-Driven Drama

The AI pays special attention to your colonists as *characters*. It creates events specifically targeting their backstories, relationships, and growth arcs.

**Examples:**

- A colonist's estranged father arrives as a refugee
- Two rivals forced to work together during a crisis
- A pacifist colonist faces a moral test
- The colony's leader must make an impossible choice

---

## Roadmap: Development Phases

### Phase 1: Foundation (MVP)

**Goal:** Playable proof-of-concept that demonstrates the core experience

**Features:**

- Custom storyteller selectable at game start: **"The Narrator"**
- AI generates narrative flavor text for standard RimWorld events
- Basic story context tracks major events and colonist names
- Simple choice events (2-3 options, immediate consequences)
- Story journal: In-game log showing the AI's narrative

**Player Experience at Phase 1:**

> You select "The Narrator" storyteller. Early game plays like vanilla, but each event comes with a story snippet: *"The harsh winter drives desperate souls to your door..."* before a raid. Occasionally, a choice popup appears with a small dilemma. A "Story" tab shows your colony's ongoing narrative.

---

### Phase 2: Deep Memory

**Goal:** The AI truly *remembers* and *references* your colony's history

**Features:**

- **Character Tracking:** AI remembers all colonists - their skills, relationships, backstories, notable actions (who killed the most raiders, who had mental breaks, who saved lives)
- **Event Memory:** Past events influence future ones (captured raiders' factions hold grudges, saved travelers return with gifts)
- **Relationship Web:** AI tracks inter-colonist drama and creates events around it
- **Callback Events:** "Remember when..." moments that reference colony history

**Player Experience at Phase 2:**

> Your colony's best shooter, Maria, killed the pirate chief's son in a raid last year. Now the AI sends a targeted assassination squad specifically hunting Maria. The narrative popup reads: *"The Crimson Reavers haven't forgotten the death of young Vance. Tonight, they've sent their best to settle the score..."*

---

### Phase 3: Branching Narratives

**Goal:** Player choices create diverging story paths with long-term consequences

**Features:**

- **Multi-stage story arcs:** Decisions create branching paths (3-5 decision points per arc)
- **Hidden consequences:** Some choices have delayed or unexpected outcomes
- **Faction reputation narrative:** Your choices define how factions see you (merciful, ruthless, opportunistic)
- **Colony identity:** The AI recognizes and reinforces your playstyle in the narrative

**Example Story Arc - "The Deserter" (spans several in-game seasons):**

```
Chapter 1: A deserter from a hostile faction begs for refuge
  → Accept: Move to Chapter 2A
  → Refuse: Move to Chapter 2B

Chapter 2A: The faction demands you hand her over
  → Comply: She's taken, faction relations improve, colony mood drops
  → Refuse: Faction sends raid, she proves her loyalty in battle
  → Negotiate: Attempt diplomacy (skill check based on colonist)

Chapter 2B: You find her corpse weeks later with a datapad
  → The datapad reveals her faction's weakness (raid opportunity)
  → Or a trap that leads to ambush

Chapter 3+: Consequences unfold over the next year...
```

**Player Experience at Phase 3:**

> Every major decision feels weighty because you know it matters. Players discuss their "story paths" like they discuss different game endings. "In my playthrough, I sided with the deserter and it led to a full faction war, but in my friend's game, they negotiated and unlocked a secret trading route."

---

### Phase 4: Dynamic World Events

**Goal:** AI creates unique events that don't exist in vanilla RimWorld

**Features:**

- **Custom Incident Generation:** AI describes novel situations, mod interprets into game effects
- **Political Intrigue:** Faction leaders with personalities, alliances, betrayals
- **Mystery Events:** Strange occurrences that unfold over time (what's in the ancient ruins? who's sending the mysterious gifts?)
- **Colony Milestones:** AI celebrates and challenges major achievements with bespoke events

**Example Custom Events:**

- *"A rival colony has risen nearby. They've sent an envoy with an... interesting proposal."* (Player can ally, ignore, or preemptively strike)
- *"Colonist Jake hasn't been sleeping. He's been sneaking out at night. What has he found?"* (Investigation mini-arc)
- *"A plague ship crashes nearby. The survivors beg for help, but your doctor warns of contagion risk."*

**Player Experience at Phase 4:**

> Players encounter events they've never seen in hundreds of hours of vanilla RimWorld. Each colony generates unique story moments that feel handcrafted. Screenshots and story recaps become shareable content.

---

### Phase 5: Personality & Themes

**Goal:** Players can customize their storytelling experience

**Features:**

- **Narrator Personalities:** Choose the AI's storytelling style
  - *The Tragedian* - Shakespearean drama, everyone suffers beautifully
  - *The Adventurer* - Pulp action, heroes and villains, daring escapes
  - *The Chronicler* - Historical epic, rise and fall of civilizations
  - *The Trickster* - Dark humor, ironic twists, cosmic jokes
  - *The Humanitarian* - Focus on relationships, emotional depth, hope

- **Theme Selection:** Set the narrative tone for your playthrough
  - Survival horror
  - Political thriller
  - Found family drama
  - Revenge epic
  - Redemption story

- **Content Preferences:** Adjust what types of events you want
  - More/less combat
  - More/less moral dilemmas
  - More/less colonist drama

**Player Experience at Phase 5:**

> Starting a new colony becomes exciting again. "This time I'm playing with The Tragedian on a revenge theme - my colonists are survivors of a massacre seeking justice." Each combination creates a fundamentally different experience.

---

### Phase 6: Community & Polish

**Goal:** Share experiences, refine the system

**Features:**

- **Story Export:** Generate a formatted narrative of your colony's history
- **Seed Sharing:** Share story seeds that create similar narrative beats
- **Memorable Moments Gallery:** Screenshot integration with AI-generated captions
- **Difficulty Tuning:** Fine-grained control over AI decision-making
- **Performance Mode:** Reduced API calls for players concerned about costs

---

## What Makes This Fun?

| Vanilla RimWorld | AI Narrator |

|------------------|-------------|

| Random raid | Raid with context, motivation, named enemies |

| Wanderer joins | Wanderer with connection to colony history |

| Solar flare happens | Solar flare as dramatic timing ("Just as the raiders approach, the sky burns...") |

| Mad animal event | Animal sacred to nearby tribe, killing it has consequences |

| Trader arrives | Trader who remembers your past dealings |

**The magic:** Every player's story becomes genuinely unique. Not just "different events happened" but "a different narrative unfolded." Players will share stories like: *"My colony's doctor fell in love with a prisoner we captured, then had to choose between the colony and saving him when his faction came to rescue him. She chose him. They both died in the escape attempt. It was the most memorable moment in 500 hours of RimWorld."*

---

## Success Metrics (How We Know It's Working)

1. **Engagement:** Players talk about their colony's "story" not just their colony's "stats"
2. **Replayability:** Players start new colonies to experience different narrative paths
3. **Emotional Investment:** Players make suboptimal gameplay decisions for narrative reasons
4. **Shareability:** Players screenshot and share story moments on social media
5. **Session Length:** Players keep playing "just one more event" to see what happens next

---

## Risks & Mitigations

| Risk | Mitigation |

|------|------------|

| API costs for players | Free tier support, caching, reduced call frequency options |

| AI generates nonsense | Strong prompt engineering, fallback to vanilla events |

| Story becomes repetitive | Diverse prompt templates, personality system, memory pruning |

| Breaks game balance | AI trained to respect game economy, difficulty guardrails |

| Long response times | Async calls, don't block gameplay, queued event system |

### To-dos

- [ ] Update About.xml, .csproj with dependencies (Newtonsoft.Json), rename namespace
- [ ] Create ModSettings.cs with API key, model selection, temperature UI
- [ ] Implement OpenRouterClient.cs with async UnityWebRequest calls
- [ ] Create ColonyStateCollector.cs to gather pawns, resources, threats
- [ ] Create StoryContext.cs with IExposable save/load as WorldComponent
- [ ] Create StorytellerComp_LLM.cs that intercepts vanilla intervals
- [ ] Create EventMapper.cs mapping LLM outputs to IncidentDefs
- [ ] Create Dialog_StoryChoice.cs for multiple-choice events
- [ ] Create LLMStoryteller.xml storyteller definition
- [ ] Test in-game: settings UI, API calls, event triggering, save/load