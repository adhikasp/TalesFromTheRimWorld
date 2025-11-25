# AGENTS.md - AI Coding Agent Guide

This document provides essential information for AI coding agents working on the **Tales from the RimWorld** mod.

## Project Overview

**Tales from the RimWorld** is a RimWorld mod that adds an AI-powered storyteller called "The Narrator". It uses the OpenRouter API to generate narrative text for game events and player choices.

### Tech Stack

- **Language**: C# (.NET Framework 4.7.2)
- **Game Engine**: Unity (via RimWorld)
- **Build System**: MSBuild / dotnet CLI
- **Dependencies**: 
  - Harmony (runtime patching)
  - Newtonsoft.Json (API communication)
  - RimWorld/Verse assemblies (game API)

## Project Structure

```
TalesFromTheRimWorld/
├── About/                    # Mod metadata for RimWorld
│   └── About.xml            # Mod info, dependencies, version
├── Assemblies/              # Compiled DLLs (output)
│   ├── AINarrator.dll       # Main mod assembly
│   ├── 0Harmony.dll         # Harmony library
│   └── Newtonsoft.Json.dll  # JSON library
├── Defs/                    # XML definitions (loaded by RimWorld)
│   ├── MainTabs/            # UI tab definitions
│   └── Storyteller/         # Storyteller definition
├── Source/                  # C# source code
│   ├── API/                 # OpenRouter API client
│   ├── Core/                # Mod initialization, settings
│   ├── GameState/           # Colony state tracking
│   ├── Storyteller/         # Custom storyteller logic
│   └── UI/                  # Dialog windows
└── Textures/                # Images (storyteller portraits)
```

## Building the Project

### Prerequisites

- .NET SDK or Visual Studio with .NET Framework 4.7.2 support
- RimWorld installed (for assembly references)

### Build Commands

```powershell
# Navigate to Source directory
cd Source

# Debug build (includes PDB for debugging)
dotnet build -c Debug

# Release build
dotnet build -c Release
```

Output goes directly to `Assemblies/AINarrator.dll`.

### Build Configuration

The `.csproj` references RimWorld assemblies from Steam's default location. If RimWorld is installed elsewhere, update the `RimWorldPath` property in `AINarrator.csproj`.

## Debugging

### Finding RimWorld Logs

RimWorld logs are essential for debugging. Location:

```
Windows: %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
macOS:   ~/Library/Logs/Unity/Player.log
Linux:   ~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log
```

### Reading Logs via PowerShell

```powershell
# View last 200 lines
Get-Content "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log" -Tail 200

# Search for errors
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log" -Pattern "Exception|Error|AI Narrator" -Context 3,3
```

### Log Prefixes

The mod uses these log prefixes:
- `[Tales from the RimWorld]` - Main mod initialization
- `[AI Narrator]` - Runtime operations, API calls, errors

### Common Crash Patterns

1. **NullReferenceException during world generation**
   - Usually means accessing game state before it's initialized
   - Check if `Find.CurrentMap`, `Find.TickManager`, etc. are null

2. **StorytellerUtilityPopulation.CalculatePopulationIntent crash**
   - Missing `ParentName="BaseStoryteller"` in StorytellerDef XML
   - Missing required population curves

3. **Harmony patch crashes**
   - Wrap patch code in try-catch to prevent game crashes
   - Log errors instead of crashing

### Development Mode

Enable RimWorld's development mode for additional debugging:
1. In-game: Options → Development mode
2. Provides debug actions, inspector, and more logging

## Critical Knowledge

### XML Definitions

#### StorytellerDef Requirements

**CRITICAL**: Custom storytellers MUST inherit from `BaseStoryteller`:

```xml
<StorytellerDef ParentName="BaseStoryteller">
```

This provides required curves:
- `populationIntentFactorFromPopulationCurve`
- `populationIntentFactorFromPopAdaptDaysCurve`

Without this, world generation crashes with `NullReferenceException` in `CalculatePopulationIntent`.

#### Essential Storyteller Components

A functional storyteller needs these components:
- `StorytellerCompProperties_ClassicIntro` - Game intro events
- `StorytellerCompProperties_RandomMain` - Random event generation
- `StorytellerCompProperties_Disease` - Disease events
- `StorytellerCompProperties_ShipChunkDrop` - Ship chunks
- `StorytellerCompProperties_FactionInteraction` - Traders, visitors
- `StorytellerCompProperties_JourneyOffer` - World journey
- `StorytellerCompProperties_Triggered` - Rescue events

### Harmony Patching

#### Safe Patch Pattern

Always wrap Harmony patches in try-catch:

```csharp
[HarmonyPostfix]
public static void Postfix(World __instance)
{
    try
    {
        // Patch logic here
    }
    catch (Exception ex)
    {
        Log.Error($"[AI Narrator] Patch error: {ex.Message}");
    }
}
```

#### Null Safety

Game state may not be initialized during:
- World generation
- Game loading
- Mod initialization

Always check:
```csharp
if (Find.TickManager == null) return;
if (Find.CurrentMap == null) return;
if (__instance?.components == null) return;
```

### WorldComponent Registration

The mod uses a Harmony patch on `World.FinalizeInit` to add `StoryContext`:

```csharp
[HarmonyPatch(typeof(World), "FinalizeInit")]
public static class World_FinalizeInit_Patch
{
    [HarmonyPostfix]
    public static void Postfix(World __instance)
    {
        // Add StoryContext WorldComponent
    }
}
```

### Save/Load (ExposeData)

Classes persisted in saves must implement `IExposable`:

```csharp
public class JournalEntry : IExposable
{
    public void ExposeData()
    {
        Scribe_Values.Look(ref field, "field", defaultValue);
    }
}
```

Collections use `Scribe_Collections.Look()`.

### API Communication

- Uses Unity coroutines for async HTTP (non-blocking)
- Always use `LongEventHandler.ExecuteWhenFinished()` for UI updates
- Rate limiting tracked in `StoryContext`

## Code Conventions

### Namespacing

All code is in the `AINarrator` namespace.

### Logging

```csharp
Log.Message("[AI Narrator] Info message");
Log.Warning("[AI Narrator] Warning message");
Log.Error("[AI Narrator] Error message");
```

### Settings Access

```csharp
AINarratorMod.Settings.ApiKey
AINarratorMod.Settings.IsConfigured()
```

## Testing Checklist

Before considering changes complete:

1. **Build succeeds** without warnings
2. **RimWorld launches** with mod enabled
3. **New world generates** without crashing
4. **Settings page** opens without errors
5. **Check logs** for any `[AI Narrator]` errors

## Common Tasks

### Adding a New Setting

1. Add field to `ModSettings.cs`
2. Add UI control in `DoSettingsWindowContents()`
3. Add to `ExposeData()` for persistence

### Adding a New Harmony Patch

1. Create patch class with `[HarmonyPatch]` attribute
2. Use `[HarmonyPrefix]` or `[HarmonyPostfix]`
3. Wrap in try-catch
4. Patches auto-apply via `harmony.PatchAll()` in `AINarratorMod`

### Modifying Storyteller Behavior

1. Edit `Defs/Storyteller/LLMStoryteller.xml`
2. No rebuild needed for XML-only changes
3. Restart RimWorld to test

## Reference Resources

- [RimWorld Wiki - Modding](https://rimworldwiki.com/wiki/Modding)
- [Harmony Documentation](https://harmony.pardeike.net/articles/intro.html)
- [Storyteller-Enhanced (reference mod)](https://github.com/Lanilor/Storyteller-Enhanced)
- [OpenRouter API](https://openrouter.ai/docs)

