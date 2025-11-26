using System.Collections.Generic;
using Newtonsoft.Json;

namespace AINarrator
{
    /// <summary>
    /// Data transfer objects for OpenRouter API communication.
    /// </summary>
    
    // Request DTOs
    public class ChatCompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; }
        
        [JsonProperty("temperature")]
        public float Temperature { get; set; }
        
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }
        
        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;
    }
    
    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }
        
        [JsonProperty("content")]
        public string Content { get; set; }
        
        /// <summary>
        /// Reasoning content from reasoning models (e.g., gemini-3-pro-preview, o1).
        /// These models put their "thinking" in this field and final answer in Content.
        /// </summary>
        [JsonProperty("reasoning")]
        public string Reasoning { get; set; }
        
        public ChatMessage() { }
        
        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
        
        /// <summary>
        /// Returns true if this is a reasoning model response with empty content.
        /// This typically means the model ran out of tokens during reasoning.
        /// </summary>
        public bool IsEmptyReasoningResponse => 
            string.IsNullOrEmpty(Content) && !string.IsNullOrEmpty(Reasoning);
    }
    
    // Response DTOs
    public class ChatCompletionResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("choices")]
        public List<ChatChoice> Choices { get; set; }
        
        [JsonProperty("error")]
        public ApiError Error { get; set; }
        
        [JsonProperty("usage")]
        public UsageInfo Usage { get; set; }
    }
    
    public class ChatChoice
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        
        [JsonProperty("message")]
        public ChatMessage Message { get; set; }
        
        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }
    
    public class ApiError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("code")]
        public string Code { get; set; }
    }
    
    public class UsageInfo
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }
        
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }
        
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
    
    // Choice Event DTOs
    
    /// <summary>
    /// Response containing multiple choice events from LLM.
    /// One event will be randomly selected for presentation.
    /// </summary>
    public class MultipleChoiceEventsResponse
    {
        public List<ChoiceEvent> Events { get; set; }
        
        public MultipleChoiceEventsResponse()
        {
            Events = new List<ChoiceEvent>();
        }
    }
    
    public class ChoiceEvent
    {
        public string NarrativeText { get; set; }
        public List<ChoiceOption> Options { get; set; }
        
        public ChoiceEvent()
        {
            Options = new List<ChoiceOption>();
        }
    }
    
    public class ChoiceOption
    {
        public string Label { get; set; }
        public string HintText { get; set; }
        [JsonProperty("Consequences")]
        public List<ChoiceConsequence> Consequences { get; set; }
        
        [JsonProperty("Consequence")]
        private ChoiceConsequence LegacyConsequence
        {
            set
            {
                if (value == null)
                {
                    return;
                }
                
                Consequences ??= new List<ChoiceConsequence>();
                Consequences.Add(value);
            }
        }
        
        public ChoiceOption()
        {
            Consequences = new List<ChoiceConsequence>();
        }
    }
    
    public class ChoiceConsequence
    {
        public string Type { get; set; }  // "spawn_pawn", "spawn_items", "mood_effect", "faction_relation", "trigger_raid"
        public Dictionary<string, object> Parameters { get; set; }
        
        public ChoiceConsequence()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
    
    // Journal Entry for persistence
    public class JournalEntry : Verse.IExposable
    {
        public int GameTick { get; set; }
        public string DateString { get; set; }
        public string Text { get; set; }
        public JournalEntryType EntryType { get; set; }
        public string ChoiceMade { get; set; }  // Only for choice entries
        
        public JournalEntry() { }
        
        public JournalEntry(int tick, string date, string text, JournalEntryType type)
        {
            GameTick = tick;
            DateString = date;
            Text = text;
            EntryType = type;
        }
        
        public void ExposeData()
        {
            // Use local fields for Scribe_Values
            int gameTick = GameTick;
            string dateStr = DateString ?? "";
            string txt = Text ?? "";
            JournalEntryType entryType = EntryType;
            string choice = ChoiceMade ?? "";
            
            Verse.Scribe_Values.Look(ref gameTick, "gameTick", 0);
            Verse.Scribe_Values.Look(ref dateStr, "dateString", "");
            Verse.Scribe_Values.Look(ref txt, "text", "");
            Verse.Scribe_Values.Look(ref entryType, "entryType", JournalEntryType.Event);
            Verse.Scribe_Values.Look(ref choice, "choiceMade", "");
            
            // Write back to properties
            GameTick = gameTick;
            DateString = dateStr;
            Text = txt;
            EntryType = entryType;
            ChoiceMade = choice;
        }
    }
    
    public enum JournalEntryType
    {
        Event,
        Choice,
        Milestone
    }
}

