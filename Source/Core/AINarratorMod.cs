using HarmonyLib;
using UnityEngine;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Main mod entry point for Tales from the RimWorld.
    /// Handles Harmony patching and settings management.
    /// </summary>
    public class AINarratorMod : Mod
    {
        public static AINarratorMod Instance { get; private set; }
        public static ModSettings Settings { get; private set; }
        
        private static Harmony harmony;

        public AINarratorMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<ModSettings>();
            
            // Apply Harmony patches
            harmony = new Harmony("com.yourname.talesfromtherimworld");
            harmony.PatchAll();
            
            Log.Message("[Tales from the RimWorld] Mod initialized successfully.");
        }

        public override string SettingsCategory()
        {
            return "Tales from the RimWorld";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.OnSettingsChanged();
        }
    }
}

