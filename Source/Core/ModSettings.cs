using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Mod settings for Tales from the RimWorld.
    /// Handles API configuration, model selection, and preferences.
    /// </summary>
    public class ModSettings : Verse.ModSettings
    {
        // API Configuration
        public string ApiKey = "";
        public string SelectedModel = "anthropic/claude-3.5-sonnet";
        public float Temperature = 0.7f;
        
        // Feature Toggles
        public bool ShowNarrativeNotifications = true;
        public bool EnableChoiceEvents = true;
        public bool PauseOnNarrative = true;
        
        // Choice Event Frequency (per in-game season)
        public int ChoiceEventsPerSeasonMin = 1;
        public int ChoiceEventsPerSeasonMax = 2;
        
        // Rate Limiting
        public int MaxCallsPerDay = 50;
        
        // Connection Status
        public enum ConnectionStatus { NotConfigured, Testing, Connected, Error }
        public static ConnectionStatus CurrentStatus = ConnectionStatus.NotConfigured;
        public static string StatusMessage = "";
        
        // Available models (hardcoded for MVP)
        public static readonly List<string> AvailableModels = new List<string>
        {
            "anthropic/claude-sonnet-4.5",
            "openai/gpt-5.1",
            "google/gemini-3-pro-preview",
            "x-ai/grok-4.1-fast"
        };
        
        // UI State
        private bool showApiKey = false;
        private int selectedModelIndex = 0;
        private Vector2 scrollPosition = Vector2.zero;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ApiKey, "apiKey", "");
            Scribe_Values.Look(ref SelectedModel, "selectedModel", "anthropic/claude-sonnet-4.5");
            Scribe_Values.Look(ref Temperature, "temperature", 0.7f);
            Scribe_Values.Look(ref ShowNarrativeNotifications, "showNarrativeNotifications", true);
            Scribe_Values.Look(ref EnableChoiceEvents, "enableChoiceEvents", true);
            Scribe_Values.Look(ref PauseOnNarrative, "pauseOnNarrative", true);
            Scribe_Values.Look(ref ChoiceEventsPerSeasonMin, "choiceEventsPerSeasonMin", 1);
            Scribe_Values.Look(ref ChoiceEventsPerSeasonMax, "choiceEventsPerSeasonMax", 2);
            Scribe_Values.Look(ref MaxCallsPerDay, "maxCallsPerDay", 50);
            
            base.ExposeData();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            // Calculate view height
            float viewHeight = 800f; 
            if (Prefs.DevMode) viewHeight += 150f;

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);
            
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            
            // Header
            Text.Font = GameFont.Medium;
            listing.Label("Tales from the RimWorld Settings");
            Text.Font = GameFont.Small;
            listing.GapLine();
            listing.Gap(12f);
            
            // API Key Section
            listing.Label("API Key (OpenRouter):");
            
            Rect apiKeyRect = listing.GetRect(28f);
            Rect keyFieldRect = new Rect(apiKeyRect.x, apiKeyRect.y, apiKeyRect.width - 160f, 28f);
            Rect showButtonRect = new Rect(keyFieldRect.xMax + 5f, apiKeyRect.y, 50f, 28f);
            Rect testButtonRect = new Rect(showButtonRect.xMax + 5f, apiKeyRect.y, 95f, 28f);
            
            // Password or visible field
            if (showApiKey)
            {
                ApiKey = Widgets.TextField(keyFieldRect, ApiKey);
            }
            else
            {
                string masked = string.IsNullOrEmpty(ApiKey) ? "" : new string('•', Math.Min(ApiKey.Length, 20));
                Widgets.Label(keyFieldRect, masked);
                if (Widgets.ButtonInvisible(keyFieldRect))
                {
                    showApiKey = true;
                }
            }
            
            if (Widgets.ButtonText(showButtonRect, showApiKey ? "Hide" : "Show"))
            {
                showApiKey = !showApiKey;
            }
            
            if (Widgets.ButtonText(testButtonRect, "Test Connection"))
            {
                TestApiConnection();
            }
            
            listing.Gap(6f);
            
            // Status indicator
            DrawStatusIndicator(listing);
            
            listing.Gap(12f);
            listing.GapLine();
            listing.Gap(12f);
            
            // Model Selection
            listing.Label("LLM Model:");
            
            selectedModelIndex = AvailableModels.IndexOf(SelectedModel);
            if (selectedModelIndex < 0) selectedModelIndex = 0;
            
            Rect modelRect = listing.GetRect(28f);
            if (Widgets.ButtonText(modelRect, GetModelDisplayName(SelectedModel)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var model in AvailableModels)
                {
                    string modelName = model;
                    options.Add(new FloatMenuOption(GetModelDisplayName(model), () =>
                    {
                        SelectedModel = modelName;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            
            listing.Gap(12f);
            
            // Temperature Slider
            listing.Label($"Creativity (Temperature): {Temperature:F2}");
            listing.Gap(4f);
            Temperature = listing.Slider(Temperature, 0.3f, 1.0f);
            Text.Font = GameFont.Tiny;
            listing.Label("Lower = consistent responses, Higher = more creative/surprising");
            Text.Font = GameFont.Small;
            
            listing.Gap(12f);
            listing.GapLine();
            listing.Gap(12f);
            
            // Feature Toggles
            listing.Label("Features:");
            listing.Gap(6f);
            
            listing.CheckboxLabeled("Show narrative notifications for events", ref ShowNarrativeNotifications, 
                "Display AI-generated flavor text when events occur");
            
            listing.CheckboxLabeled("Enable choice events", ref EnableChoiceEvents,
                "Allow the narrator to present dilemmas with choices");
            
            if (EnableChoiceEvents)
            {
                listing.Gap(8f);
                
                // Choice events per season range
                listing.Label($"Choice events per season: {ChoiceEventsPerSeasonMin} - {ChoiceEventsPerSeasonMax}");
                listing.Gap(4f);
                
                // Min slider
                listing.Label($"  Minimum: {ChoiceEventsPerSeasonMin}");
                ChoiceEventsPerSeasonMin = (int)listing.Slider(ChoiceEventsPerSeasonMin, 0, 5);
                
                // Max slider (ensure it's >= min)
                listing.Label($"  Maximum: {ChoiceEventsPerSeasonMax}");
                ChoiceEventsPerSeasonMax = (int)listing.Slider(ChoiceEventsPerSeasonMax, ChoiceEventsPerSeasonMin, 10);
                
                // Ensure max is always >= min
                if (ChoiceEventsPerSeasonMax < ChoiceEventsPerSeasonMin)
                {
                    ChoiceEventsPerSeasonMax = ChoiceEventsPerSeasonMin;
                }
                
                Text.Font = GameFont.Tiny;
                listing.Label("Set both to 0 to disable choice events entirely");
                Text.Font = GameFont.Small;
                
                listing.Gap(4f);
            }
            
            listing.CheckboxLabeled("Pause game on narrative popup", ref PauseOnNarrative,
                "Automatically pause when a narrative event appears");
            
            listing.Gap(12f);
            
            // Rate Limiting
            listing.Label($"Max API calls per in-game day: {MaxCallsPerDay}");
            MaxCallsPerDay = (int)listing.Slider(MaxCallsPerDay, 10, 100);
            
            listing.Gap(12f);
            listing.GapLine();
            listing.Gap(6f);
            
            // Info text
            GUI.color = Color.gray;
            listing.Label("Get your API key at: https://openrouter.ai/keys");
            listing.Label("The API key is stored locally and never shared.");
            GUI.color = Color.white;
            
            // Debug Tools (only in Dev Mode)
            if (Prefs.DevMode)
            {
                listing.Gap(12f);
                listing.GapLine();
                listing.Gap(6f);
                
                GUI.color = new Color(1f, 0.8f, 0.4f);
                listing.Label("⚠ Debug Tools (Dev Mode)");
                GUI.color = Color.white;
                listing.Gap(6f);
                
                // Only show in-game debug tools when actually in a game
                if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null)
                {
                    Rect row1 = listing.GetRect(28f);
                    float buttonWidth = (row1.width - 10f) / 2f;
                    
                    if (Widgets.ButtonText(new Rect(row1.x, row1.y, buttonWidth, 28f), "Trigger Narrative"))
                    {
                        DebugActions.TriggerNarrative();
                    }
                    
                    if (Widgets.ButtonText(new Rect(row1.x + buttonWidth + 10f, row1.y, buttonWidth, 28f), "Trigger Choice Event"))
                    {
                        DebugActions.TriggerChoiceEvent();
                    }
                    
                    listing.Gap(6f);
                    
                    Rect row2 = listing.GetRect(28f);
                    if (Widgets.ButtonText(new Rect(row2.x, row2.y, buttonWidth, 28f), "Reset Rate Limit"))
                    {
                        DebugActions.ResetRateLimit();
                    }
                }
                else
                {
                    GUI.color = Color.gray;
                    listing.Label("Load a save to access debug tools");
                    GUI.color = Color.white;
                }
                
                listing.Gap(6f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                listing.Label("Keyboard shortcuts: Ctrl+Shift+N (narrative), Ctrl+Shift+C (choice event)");
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
            
            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawStatusIndicator(Listing_Standard listing)
        {
            Rect statusRect = listing.GetRect(24f);
            
            Color statusColor;
            string statusText;
            
            switch (CurrentStatus)
            {
                case ConnectionStatus.Connected:
                    statusColor = Color.green;
                    statusText = "● Connected";
                    break;
                case ConnectionStatus.Testing:
                    statusColor = Color.yellow;
                    statusText = "○ Testing...";
                    break;
                case ConnectionStatus.Error:
                    statusColor = Color.red;
                    statusText = "● Error";
                    break;
                default:
                    statusColor = Color.gray;
                    statusText = "○ Not Configured";
                    break;
            }
            
            GUI.color = statusColor;
            Widgets.Label(statusRect, $"Status: {statusText}");
            GUI.color = Color.white;
            
            if (!string.IsNullOrEmpty(StatusMessage))
            {
                listing.Gap(4f);
                GUI.color = CurrentStatus == ConnectionStatus.Error ? new Color(1f, 0.5f, 0.5f) : Color.gray;
                Text.Font = GameFont.Tiny;
                listing.Label(StatusMessage);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        private string GetModelDisplayName(string modelId)
        {
            if (string.IsNullOrEmpty(modelId)) return "Select Model";
            
            // Convert "anthropic/claude-3.5-sonnet" to "Claude 3.5 Sonnet"
            string[] parts = modelId.Split('/');
            if (parts.Length > 1)
            {
                return parts[1].Replace("-", " ").Replace(".", " ");
            }
            return modelId;
        }

        private void TestApiConnection()
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                CurrentStatus = ConnectionStatus.Error;
                StatusMessage = "Please enter an API key first";
                return;
            }
            
            CurrentStatus = ConnectionStatus.Testing;
            StatusMessage = "Testing connection...";
            
            OpenRouterClient.TestConnection(
                onSuccess: () =>
                {
                    CurrentStatus = ConnectionStatus.Connected;
                    StatusMessage = "Successfully connected to OpenRouter";
                },
                onError: (error) =>
                {
                    CurrentStatus = ConnectionStatus.Error;
                    StatusMessage = error;
                }
            );
        }

        public void OnSettingsChanged()
        {
            // Update connection status when API key changes
            if (string.IsNullOrEmpty(ApiKey))
            {
                CurrentStatus = ConnectionStatus.NotConfigured;
                StatusMessage = "";
            }
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(ApiKey);
        }
    }
}

