using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AINarrator
{
    /// <summary>
    /// Dialog window for choice events with multiple options.
    /// Player selects one option and consequences are executed.
    /// </summary>
    public class Dialog_StoryChoice : Window
    {
        private ChoiceEvent choiceEvent;
        private int selectedOption = -1;
        private Action<int> onChoiceMade;
        
        // Animation
        private float openTime;
        private const float FADE_DURATION = 0.3f;
        
        // Styling
        private static readonly Color HeaderColor = new Color(0.95f, 0.8f, 0.5f);
        private static readonly Color TextColor = new Color(0.95f, 0.93f, 0.88f);
        private static readonly Color OptionColor = new Color(0.9f, 0.88f, 0.82f);
        private static readonly Color HintColor = new Color(0.7f, 0.68f, 0.6f);
        private static readonly Color SelectedColor = new Color(0.3f, 0.5f, 0.3f, 0.4f);
        private static readonly Color HoverColor = new Color(0.4f, 0.4f, 0.35f, 0.3f);
        private static readonly Color DividerColor = new Color(0.6f, 0.55f, 0.45f);
        
        public override Vector2 InitialSize
        {
            get
            {
                // Dynamic height based on number of options
                float baseHeight = 280f;
                float optionHeight = choiceEvent?.Options?.Count * 70f ?? 140f;
                return new Vector2(550f, Mathf.Min(baseHeight + optionHeight, 550f));
            }
        }
        
        public Dialog_StoryChoice(ChoiceEvent choice, Action<int> onChoice)
        {
            choiceEvent = choice;
            onChoiceMade = onChoice;
            
            // Window settings
            doCloseButton = false;
            doCloseX = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = true;  // Always pause for choices
            
            openTime = Time.realtimeSinceStartup;
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
            Widgets.Label(headerRect, "★ A CHOICE AWAITS ★");
            Text.Anchor = TextAnchor.UpperLeft;
            
            y += 45f;
            
            // Decorative line
            GUI.color = new Color(DividerColor.r, DividerColor.g, DividerColor.b, alpha * 0.8f);
            Widgets.DrawLineHorizontal(inRect.width * 0.1f, y, inRect.width * 0.8f);
            y += 15f;
            
            // Narrative text
            Text.Font = GameFont.Small;
            GUI.color = new Color(TextColor.r, TextColor.g, TextColor.b, alpha);
            
            float narrativeHeight = Text.CalcHeight(choiceEvent.NarrativeText, inRect.width - 40f);
            narrativeHeight = Mathf.Max(narrativeHeight, 60f);
            Rect narrativeRect = new Rect(20f, y, inRect.width - 40f, narrativeHeight);
            Widgets.Label(narrativeRect, choiceEvent.NarrativeText);
            
            y += narrativeHeight + 15f;
            
            // Divider before options
            GUI.color = new Color(DividerColor.r, DividerColor.g, DividerColor.b, alpha * 0.6f);
            Widgets.DrawLineHorizontal(20f, y, inRect.width - 40f);
            y += 15f;
            
            // Options
            if (choiceEvent.Options != null)
            {
                for (int i = 0; i < choiceEvent.Options.Count; i++)
                {
                    var option = choiceEvent.Options[i];
                    y = DrawOption(inRect, y, i, option, alpha);
                }
            }
            
            y += 10f;
            
            // Confirm button
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect buttonRect = new Rect(inRect.width - 140f, inRect.height - 45f, 120f, 35f);
            
            bool canConfirm = selectedOption >= 0;
            
            if (canConfirm)
            {
                if (Widgets.ButtonText(buttonRect, "Confirm"))
                {
                    OnConfirmClicked();
                }
            }
            else
            {
                // Disabled button
                GUI.color = new Color(0.5f, 0.5f, 0.5f, alpha);
                Widgets.DrawHighlight(buttonRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(buttonRect, "Select an option");
                Text.Anchor = TextAnchor.UpperLeft;
            }
            
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private float DrawOption(Rect inRect, float y, int index, ChoiceOption option, float alpha)
        {
            float optionHeight = 60f;
            Rect optionRect = new Rect(25f, y, inRect.width - 50f, optionHeight);
            
            // Background
            bool isSelected = selectedOption == index;
            bool isHovered = Mouse.IsOver(optionRect);
            
            if (isSelected)
            {
                GUI.color = new Color(SelectedColor.r, SelectedColor.g, SelectedColor.b, SelectedColor.a * alpha);
                Widgets.DrawBoxSolid(optionRect, GUI.color);
            }
            else if (isHovered)
            {
                GUI.color = new Color(HoverColor.r, HoverColor.g, HoverColor.b, HoverColor.a * alpha);
                Widgets.DrawBoxSolid(optionRect, GUI.color);
            }
            
            // Border
            GUI.color = new Color(0.5f, 0.5f, 0.45f, alpha * (isSelected ? 0.8f : 0.4f));
            Widgets.DrawBox(optionRect);
            
            // Radio button indicator
            Rect radioRect = new Rect(optionRect.x + 10f, optionRect.y + 20f, 20f, 20f);
            GUI.color = new Color(OptionColor.r, OptionColor.g, OptionColor.b, alpha);
            
            if (isSelected)
            {
                Widgets.Label(radioRect, "●");
            }
            else
            {
                Widgets.Label(radioRect, "○");
            }
            
            // Option label
            Rect labelRect = new Rect(optionRect.x + 35f, optionRect.y + 8f, optionRect.width - 45f, 24f);
            GUI.color = new Color(OptionColor.r, OptionColor.g, OptionColor.b, alpha);
            Text.Font = GameFont.Small;
            Widgets.Label(labelRect, option.Label);
            
            // Hint text
            if (!string.IsNullOrEmpty(option.HintText))
            {
                Rect hintRect = new Rect(optionRect.x + 35f, optionRect.y + 32f, optionRect.width - 45f, 22f);
                GUI.color = new Color(HintColor.r, HintColor.g, HintColor.b, alpha);
                Text.Font = GameFont.Tiny;
                Widgets.Label(hintRect, $"→ {option.HintText}");
                Text.Font = GameFont.Small;
            }
            
            // Click handler
            if (Widgets.ButtonInvisible(optionRect))
            {
                selectedOption = index;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            
            return y + optionHeight + 5f;
        }
        
        private void OnConfirmClicked()
        {
            if (selectedOption < 0) return;
            
            SoundDefOf.Click.PlayOneShotOnCamera();
            
            // Log choice to story context
            if (StoryContext.Instance != null && choiceEvent.Options != null && 
                selectedOption < choiceEvent.Options.Count)
            {
                var option = choiceEvent.Options[selectedOption];
                string choiceId = $"choice_{Find.TickManager.TicksGame}";
                
                StoryContext.Instance.RecordChoice(choiceId, option.Label);
                StoryContext.Instance.AddJournalEntry(
                    choiceEvent.NarrativeText,
                    JournalEntryType.Choice,
                    option.Label
                );
            }
            
            Close();
            onChoiceMade?.Invoke(selectedOption);
        }
        
        public override void PreOpen()
        {
            base.PreOpen();
            SoundDefOf.Quest_Accepted.PlayOneShotOnCamera();
        }
    }
}

