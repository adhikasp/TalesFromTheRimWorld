using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AINarrator
{
    /// <summary>
    /// Dialog window that displays AI-generated narrative flavor text for events.
    /// Styled with a storyteller theme.
    /// </summary>
    public class Dialog_NarrativePopup : Window
    {
        private string narrativeText;
        private string eventSummary;
        private Action onContinue;
        private bool loading;
        private bool wasLoadingDialog; // Track if this started as a loading dialog
        
        // Animation
        private float openTime;
        private const float FADE_DURATION = 0.3f;
        
        // Scrolling
        private Vector2 scrollPosition = Vector2.zero;
        
        // Styling
        private static readonly Color HeaderColor = new Color(0.9f, 0.85f, 0.7f);
        private static readonly Color TextColor = new Color(0.95f, 0.93f, 0.88f);
        private static readonly Color DividerColor = new Color(0.6f, 0.55f, 0.45f);
        private static readonly Color BackgroundColor = new Color(0.12f, 0.11f, 0.1f, 0.97f);
        
        public override Vector2 InitialSize => new Vector2(560f, 450f);
        
        public Dialog_NarrativePopup(string narrative, string eventInfo, Action onContinueCallback)
        {
            narrativeText = narrative;
            eventSummary = eventInfo;
            onContinue = onContinueCallback;
            loading = false;
            
            // Window settings
            doCloseButton = false;
            doCloseX = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = AINarratorMod.Settings.PauseOnNarrative;
            
            openTime = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// Create a loading state dialog.
        /// </summary>
        public static Dialog_NarrativePopup CreateLoading(string eventInfo)
        {
            var dialog = new Dialog_NarrativePopup("...", eventInfo, null);
            dialog.loading = true;
            dialog.wasLoadingDialog = true;
            dialog.narrativeText = "The Narrator ponders...";
            return dialog;
        }
        
        /// <summary>
        /// Update the narrative text (used when async loading completes).
        /// </summary>
        public void SetNarrative(string narrative, Action onContinueCallback)
        {
            narrativeText = narrative;
            onContinue = onContinueCallback;
            loading = false;
            
            // Clear the placeholder event summary if this was a loading dialog
            if (wasLoadingDialog)
            {
                eventSummary = null;
            }
            
            // Reset scroll position when new text arrives
            scrollPosition = Vector2.zero;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            // Calculate fade-in alpha
            float elapsed = Time.realtimeSinceStartup - openTime;
            float alpha = Mathf.Clamp01(elapsed / FADE_DURATION);
            
            GUI.color = new Color(1f, 1f, 1f, alpha);
            
            float y = 0f;
            
            // Header
            Text.Font = GameFont.Medium;
            GUI.color = new Color(HeaderColor.r, HeaderColor.g, HeaderColor.b, alpha);
            
            Rect headerRect = new Rect(0f, y, inRect.width, 40f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, "❝ THE NARRATOR SPEAKS ❞");
            Text.Anchor = TextAnchor.UpperLeft;
            
            y += 45f;
            
            // Decorative line
            GUI.color = new Color(DividerColor.r, DividerColor.g, DividerColor.b, alpha * 0.8f);
            Widgets.DrawLineHorizontal(inRect.width * 0.1f, y, inRect.width * 0.8f);
            y += 15f;
            
            // Narrative text
            Text.Font = GameFont.Small;
            GUI.color = new Color(TextColor.r, TextColor.g, TextColor.b, alpha);
            
            float narrativeAreaHeight = 200f;
            Rect narrativeOuterRect = new Rect(20f, y, inRect.width - 40f, narrativeAreaHeight);
            
            if (loading)
            {
                // Animated loading dots
                int dots = (int)(Time.realtimeSinceStartup * 2) % 4;
                string loadingText = narrativeText + new string('.', dots);
                
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f, alpha);
                Widgets.Label(narrativeOuterRect, loadingText);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                // Calculate actual text height for scrolling
                float textWidth = narrativeOuterRect.width - 16f; // Account for scrollbar
                float textHeight = Text.CalcHeight(narrativeText, textWidth);
                
                // Create scrollable area if text is taller than visible area
                Rect viewRect = new Rect(0f, 0f, textWidth, Mathf.Max(textHeight + 10f, narrativeAreaHeight));
                
                Widgets.BeginScrollView(narrativeOuterRect, ref scrollPosition, viewRect);
                
                Rect narrativeTextRect = new Rect(0f, 0f, textWidth, textHeight);
                Widgets.Label(narrativeTextRect, narrativeText);
                
                Widgets.EndScrollView();
            }
            
            y += narrativeAreaHeight + 10f;
            
            // Divider
            GUI.color = new Color(DividerColor.r, DividerColor.g, DividerColor.b, alpha * 0.6f);
            Widgets.DrawLineHorizontal(20f, y, inRect.width - 40f);
            y += 15f;
            
            // Event summary
            if (!string.IsNullOrEmpty(eventSummary))
            {
                Rect eventRect = new Rect(20f, y, inRect.width - 40f, 50f);
                GUI.color = new Color(0.8f, 0.8f, 0.8f, alpha);
                Text.Font = GameFont.Small;
                Widgets.Label(eventRect, eventSummary);
                y += 55f;
            }
            
            // Continue button
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect buttonRect = new Rect(inRect.width - 140f, inRect.height - 45f, 120f, 35f);
            
            if (!loading)
            {
                if (Widgets.ButtonText(buttonRect, "Continue →"))
                {
                    OnContinueClicked();
                }
                
                // Also allow Enter key
                if (Event.current.type == EventType.KeyDown && 
                    (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    OnContinueClicked();
                    Event.current.Use();
                }
            }
            else
            {
                // Disabled button during loading
                GUI.color = new Color(0.5f, 0.5f, 0.5f, alpha);
                Widgets.DrawHighlight(buttonRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(buttonRect, "Loading...");
                Text.Anchor = TextAnchor.UpperLeft;
            }
            
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void OnContinueClicked()
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            
            Close();
            onContinue?.Invoke();
        }
        
        public override void PreOpen()
        {
            base.PreOpen();
            
            // Play a subtle sound
            SoundDefOf.LetterArrive.PlayOneShotOnCamera();
        }
        
        public override void OnAcceptKeyPressed()
        {
            if (!loading)
            {
                OnContinueClicked();
            }
        }
    }
}

