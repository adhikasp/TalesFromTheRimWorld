using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Registers the StoryContext WorldComponent on game start.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class WorldComponentRegistration
    {
        static WorldComponentRegistration()
        {
            Log.Message("[AI Narrator] Mod loaded successfully.");
        }
    }
    
    /// <summary>
    /// Harmony patch to add StoryContext WorldComponent to worlds.
    /// </summary>
    [HarmonyPatch(typeof(World), "FinalizeInit")]
    public static class World_FinalizeInit_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(World __instance)
        {
            try
            {
                // Safety check: ensure components list exists
                if (__instance == null || __instance.components == null)
                {
                    Log.Warning("[AI Narrator] World or components list is null, skipping StoryContext initialization.");
                    return;
                }
                
                // Check if StoryContext already exists
                var existingComponent = __instance.components.OfType<StoryContext>().FirstOrDefault();
                
                if (existingComponent == null)
                {
                    Log.Message("[AI Narrator] Creating StoryContext WorldComponent...");
                    var storyContext = new StoryContext(__instance);
                    __instance.components.Add(storyContext);
                    // Let the component initialize via its own tick method
                }
                else
                {
                    // Ensure static instance is set
                    StoryContext.Instance = existingComponent;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[AI Narrator] Error during StoryContext initialization: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}

