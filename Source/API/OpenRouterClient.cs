using System;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Static API facade for OpenRouter API communication in RimWorld.
    /// Uses Unity's coroutine system to avoid blocking the game thread.
    /// 
    /// This is a thin wrapper around OpenRouterClientCore that:
    /// - Provides static methods for easy access
    /// - Creates the Unity HTTP transport
    /// - Pulls configuration from ModSettings
    /// </summary>
    public static class OpenRouterClient
    {
        private static OpenRouterClientCore _core;
        private static IHttpTransport _transport;
        
        /// <summary>
        /// Get or create the client core instance.
        /// </summary>
        private static OpenRouterClientCore GetCore()
        {
            // Recreate if settings changed (API key, etc.)
            if (_core == null || _transport == null)
            {
                _transport = new UnityHttpTransport();
                _core = new OpenRouterClientCore(
                    _transport,
                    CreateConfig(),
                    msg => Log.Message(msg),
                    msg => Log.Warning(msg),
                    msg => Log.Error(msg)
                );
            }
            return _core;
        }
        
        /// <summary>
        /// Create transport configuration from mod settings.
        /// </summary>
        private static HttpTransportConfig CreateConfig()
        {
            return new HttpTransportConfig
            {
                ApiKey = AINarratorMod.Settings.ApiKey,
                ApiUrl = "https://openrouter.ai/api/v1/chat/completions",
                TimeoutSeconds = 60,
                Referer = "https://rimworld-ainarrator.local",
                Title = "Tales from the RimWorld"
            };
        }
        
        /// <summary>
        /// Force recreation of the client (e.g., after settings change).
        /// </summary>
        public static void ResetClient()
        {
            _core = null;
            _transport = null;
        }
        
        /// <summary>
        /// Request a narrative response from the LLM.
        /// </summary>
        public static void RequestNarration(string systemPrompt, string userPrompt, 
            Action<string> onSuccess, Action<string> onError, int maxTokens = 200)
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            var core = GetCore();
            var request = core.BuildNarrationRequest(
                AINarratorMod.Settings.SelectedModel,
                AINarratorMod.Settings.Temperature,
                systemPrompt,
                userPrompt,
                maxTokens
            );
            
            core.RequestNarration(request, onSuccess, onError);
        }
        
        /// <summary>
        /// Request a choice event from the LLM.
        /// </summary>
        public static void RequestChoiceEvent(string systemPrompt, string userPrompt,
            Action<ChoiceEvent> onSuccess, Action<string> onError)
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            var core = GetCore();
            var request = core.BuildChoiceRequest(
                AINarratorMod.Settings.SelectedModel,
                AINarratorMod.Settings.Temperature,
                systemPrompt,
                userPrompt,
                2000  // Multiple choice events need more tokens
            );
            
            core.RequestChoiceEvent(request, 
                result =>
                {
                    // Randomly select one event from the result
                    var selectedEvent = result.GetRandomEvent(UnityEngine.Random.Range);
                    if (selectedEvent != null)
                    {
                        Log.Message($"[AI Narrator] Randomly selected choice event 1 of {result.Events.Count}");
                        onSuccess?.Invoke(selectedEvent);
                    }
                    else
                    {
                        onError?.Invoke("No valid choice events in response");
                    }
                },
                onError
            );
        }
        
        /// <summary>
        /// Test the API connection with a minimal request.
        /// </summary>
        public static void TestConnection(Action onSuccess, Action<string> onError)
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            var core = GetCore();
            var request = core.BuildTestRequest(AINarratorMod.Settings.SelectedModel, 10);
            
            core.TestConnection(request, onSuccess, onError);
        }
    }
}
