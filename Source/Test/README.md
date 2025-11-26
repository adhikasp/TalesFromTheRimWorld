# AI Narrator Test Console

Interactive test console for the Tales from the RimWorld mod's LLM integration. Test AI-generated narration and choice events with mock colony scenarios.

## Architecture

This test project **references the actual `AINarrator` project** to avoid code duplication:

- **Uses actual DTOs** from `AINarrator.LLMRequest` (`ChatCompletionRequest`, `ChoiceEvent`, etc.)
- **Uses actual system prompts** from `AINarrator.PromptBuilder` (same prompts as production)
- **Mock colony context** for testing (since `ColonyStateCollector` requires RimWorld runtime)
- **Standalone HTTP client** (since `OpenRouterClient` requires Unity coroutines)

This ensures tests verify the same prompt logic used in production.

## Quick Start

1. **Build the test project:**

  ```powershell
  cd Source/Test
  dotnet build -c Debug
  ```

2. **Configure your API key** in one of two ways:

  - Edit `bin/Debug/net472/appsettings.json` and replace `YOUR_OPENROUTER_API_KEY_HERE` with your actual OpenRouter API key
  - Or enter it interactively when prompted

3. **Run the test:**

  ```powershell
  .\bin\Debug\net472\NarratorTest.exe
  ```

## Configuration

The `appsettings.json` file supports these options:

```json
{
  "ApiKey": "YOUR_OPENROUTER_API_KEY_HERE",
  "Model": "google/gemini-2.0-flash-001",
  "Temperature": 0.8,
  "MaxNarrationTokens": 200,
  "MaxChoiceTokens": 2000
}
```

### Recommended Models

- `google/gemini-2.0-flash-001` - Fast, cost-effective, good quality (default)
- `anthropic/claude-3-haiku` - Fast and affordable
- `anthropic/claude-3.5-sonnet` - Higher quality, more expensive
- `openai/gpt-4o-mini` - Fast, good quality
- `openai/gpt-4o` - Highest quality, most expensive

You can change the model interactively from the menu (option 5).

## Test Scenarios

The test includes four mock colony scenarios:

### 1. Early Game (Day 3)
- 3 colonists
- Just crashed
- Minimal resources
- No defenses
- Basic survival situation

### 2. Mid Game (Day 45)
- 8 colonists
- Established base
- 2 prisoners
- Some faction history
- Heat wave in arid biome

### 3. Late Game (Day 180)
- 15 colonists
- Wealthy fortress
- Complex social relationships
- Active raid in progress
- Multiple deaths in history

### 4. Crisis (Day 90)
- 5 colonists (survivors)
- Ice sheet biome
- Starvation imminent
- Recent commander death
- Volcanic winter + cold snap

## Menu Options

1. **Test Event Narration** - Generate narrative flavor text for a specific event
2. **Test Choice Event Generation** - Generate player choice dilemmas
3. **Run Full Scenario Test** - Test both narration and choice for a scenario
4. **Test All Scenarios** - Run through all scenario/event combinations
5. **Change Model/Settings** - Modify model and temperature settings
0. **Exit**

## Event Types

Available test events:
- Small Raid / Large Raid
- Trader Arrival
- Wanderer Joins
- Solar Flare
- Psychic Drone
- Manhunter Pack
- Cold Snap
- Mechanoid Cluster

## Example Output

### Event Narration

```
╔══════════════════════════════════════════════════════════════╗
║                    THE NARRATOR SPEAKS                       ║
╚══════════════════════════════════════════════════════════════╝

The morning sun barely warms Ironhold as dust clouds rise on
the horizon. Marcus spots them first—the Forsaken Raiders
return, their numbers greater than before. Chen grips his rifle,
a cold smile crossing his bloodlust-touched features. The ghost
of Rodrigo, fallen to these same raiders forty days ago, demands
vengeance.
```

### Choice Event

```
╔══════════════════════════════════════════════════════════════╗
║                  CHOICE EVENT #1                             ║
╚══════════════════════════════════════════════════════════════╝

Slag, the captured raider, scratches a message into the cell
wall. Elena notices it reads: "My old crew buried supplies near
the eastern ridge." He offers to show your colonists the cache
if you release him unharmed.

OPTIONS:
  [1] Trust the prisoner's word
      Hint: May find valuable supplies, but he could escape
      Consequence: spawn_items {"item":"Silver","count":180}

  [2] Force the information out of him
      Hint: Guaranteed info, but Chen's methods may disturb others
      Consequence: mood_effect {"type":"negative","severity":2}

  [3] Ignore the offer
      Hint: Safe, but you'll never know what you missed
      Consequence: nothing {}
```

## Troubleshooting

### "Invalid API key"
- Verify your OpenRouter API key is correct
- Check that you have credits in your OpenRouter account

### "Rate limited"
- Wait a few seconds and try again
- Consider using a different model

### "Failed to parse choice event"
- The LLM occasionally produces malformed JSON
- Try again—it usually works on retry
- Lower temperature (0.6-0.7) may help consistency

### Build errors
- Ensure you have .NET Framework 4.7.2 SDK installed
- Run `dotnet restore` before building

## Token Usage

The test console displays token usage after each API call:

```
[Tokens: 1523 prompt + 142 completion = 1665 total]
```

This helps you estimate costs when using paid models.

