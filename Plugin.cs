using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using Pigeon;

namespace SparrohEncyclopedia
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class EncyclopediaPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "sparroh.encyclopedia";
        public const string PluginName = "Encyclopedia";
        public const string PluginVersion = "1.0.0";

        private static float windowX = 0f;
        private static float windowY = 0f;
        private static float windowWidth = 400f;
        private static float windowHeight = 300f;
        private static bool isDragging = false;
        private static Vector2 dragOffset;
        private static bool isResizing = false;
        private static ResizeEdge currentResizeEdge = ResizeEdge.None;
        private static bool windowPositionInitialized = false;
        private static Vector2 leftScrollPos = Vector2.zero;
        private static Vector2 rightScrollPos = Vector2.zero;

        private enum ResizeEdge
        {
            None,
            Bottom,
            Right,
            BottomRight,
            Left,
            Top
        }

        private void Awake()
        {
            var harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(MenuPatches));
        }

        private void InitializeWindowPosition()
        {
            if (windowPositionInitialized) return;

            windowWidth = Screen.width * 0.1f;
            windowHeight = Screen.height * 0.2f;
            windowX = Screen.width - windowWidth;
            windowY = Screen.height * 0.1f;
            windowPositionInitialized = true;
        }

        private void OnGUI()
        {
            if (MenuPatches.ShowTooltip && !string.IsNullOrEmpty(MenuPatches.CurrentTooltipText))
            {
                const float TOOLTIP_MAX_WIDTH = 400f;
                const float TOOLTIP_MIN_WIDTH = 300f;
                float TOOLTIP_MAX_HEIGHT = Screen.height * 0.7f;
                const float TOOLTIP_MARGIN = 5f;

                GUIStyle tooltipStyle = new GUIStyle(GUI.skin.label);
                tooltipStyle.normal.background = Texture2D.whiteTexture;
                tooltipStyle.normal.textColor = Color.black;
                tooltipStyle.fontSize = 12;
                tooltipStyle.wordWrap = true;
                tooltipStyle.alignment = TextAnchor.MiddleLeft;
                tooltipStyle.padding = new RectOffset(8, 8, 6, 6);
                tooltipStyle.richText = false;

                Vector2 mousePos = MenuPatches.TooltipPosition;

                float tooltipWidth = Mathf.Min(TOOLTIP_MAX_WIDTH, TOOLTIP_MIN_WIDTH);
                float requiredHeight = tooltipStyle.CalcHeight(new GUIContent(MenuPatches.CurrentTooltipText), tooltipWidth);
                requiredHeight = Mathf.Clamp(requiredHeight, 80f, TOOLTIP_MAX_HEIGHT);

                float tooltipHeight = requiredHeight + 12;

                bool canFitAbove = (mousePos.y - tooltipHeight - 20f) >= 0;
                float yPosition = canFitAbove ?
                    mousePos.y - tooltipHeight - 10f :
                    mousePos.y + 20f;

                if (yPosition < TOOLTIP_MARGIN)
                {
                    yPosition = TOOLTIP_MARGIN;
                }
                else if (yPosition + tooltipHeight > Screen.height - TOOLTIP_MARGIN)
                {
                    yPosition = Screen.height - tooltipHeight - TOOLTIP_MARGIN;
                }

                bool canFitRight = (mousePos.x + 15f + tooltipWidth) <= Screen.width - TOOLTIP_MARGIN;
                float xPosition = canFitRight ?
                    mousePos.x + 15f :
                    mousePos.x - tooltipWidth - 15f;

                if (xPosition < TOOLTIP_MARGIN)
                {
                    xPosition = TOOLTIP_MARGIN;
                }
                else if (xPosition + tooltipWidth > Screen.width - TOOLTIP_MARGIN)
                {
                    xPosition = Screen.width - tooltipWidth - TOOLTIP_MARGIN;
                }

                Rect tooltipRect = new Rect(xPosition, yPosition, tooltipWidth, tooltipHeight);

                GUI.Window(999, tooltipRect, (windowId) => {
                    Color prevColor = GUI.color;
                    GUI.color = Color.white;
                    GUI.Label(new Rect(0, 0, tooltipRect.width, tooltipRect.height),
                           MenuPatches.CurrentTooltipText, tooltipStyle);
                    GUI.color = prevColor;
                }, "", GUIStyle.none);
            }

            if (MenuPatches.IsMenuOpen && GUI.Button(new Rect(Screen.width - 110, 10, 100, 30), "📚 Encyclopedia"))
            {
                MenuPatches.ToggleEncyclopedia();
            }

            if (MenuPatches.ShowEncyclopediaGUI)
            {
                InitializeWindowPosition();

                HandleDragAndResize();

                GUI.Window(0, new Rect(windowX, windowY, windowWidth, windowHeight),
                    EncyclopediaWindow, "Mycopunk Encyclopedia [Drag | Resize ↗]");

                Event currentEvent = Event.current;
                if (currentEvent.isMouse && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.MouseDrag))
                {
                    Vector2 mousePos = currentEvent.mousePosition;

                    if (mousePos.x >= windowX && mousePos.x <= windowX + windowWidth &&
                        mousePos.y >= windowY && mousePos.y <= windowY + windowHeight)
                    {
                        if (currentEvent.type != EventType.ScrollWheel)
                        {
                            currentEvent.Use();
                        }
                    }
                }
            }
        }

        private void HandleDragAndResize()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                Vector2 mousePos = e.mousePosition;

                if (mousePos.x >= windowX && mousePos.x <= windowX + windowWidth &&
                    mousePos.y >= windowY && mousePos.y <= windowY + 30)
                {
                    isDragging = true;
                    dragOffset = mousePos - new Vector2(windowX, windowY);
                    e.Use();
                }
                else
                {
                    if (mousePos.x >= windowX + windowWidth - 20 && mousePos.x <= windowX + windowWidth &&
                        mousePos.y >= windowY + windowHeight - 20 && mousePos.y <= windowY + windowHeight)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.BottomRight;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX && mousePos.x <= windowX + 20 &&
                             mousePos.y >= windowY + windowHeight - 20 && mousePos.y <= windowY + windowHeight)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.Bottom;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX + windowWidth - 20 && mousePos.x <= windowX + windowWidth &&
                             mousePos.y >= windowY && mousePos.y <= windowY + 20)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.Right;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX && mousePos.x <= windowX + 20 &&
                             mousePos.y >= windowY && mousePos.y <= windowY + 20)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.None;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX + 20 && mousePos.x <= windowX + windowWidth - 20 &&
                             mousePos.y >= windowY + windowHeight - 10 && mousePos.y <= windowY + windowHeight)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.Bottom;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX + windowWidth - 10 && mousePos.x <= windowX + windowWidth &&
                             mousePos.y >= windowY + 20 && mousePos.y <= windowY + windowHeight - 20)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.Right;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX && mousePos.x <= windowX + 10 &&
                             mousePos.y >= windowY + 20 && mousePos.y <= windowY + windowHeight - 20)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.Left;
                        e.Use();
                    }
                    else if (mousePos.x >= windowX + 20 && mousePos.x <= windowX + windowWidth - 20 &&
                             mousePos.y >= windowY && mousePos.y <= windowY + 10)
                    {
                        isResizing = true;
                        currentResizeEdge = ResizeEdge.Top;
                        e.Use();
                    }
                }
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (isDragging)
                {
                    Vector2 mousePos = e.mousePosition;
                    windowX = Mathf.Clamp(mousePos.x - dragOffset.x, 0, Screen.width - windowWidth);
                    windowY = Mathf.Clamp(mousePos.y - dragOffset.y, 0, Screen.height - windowHeight);
                    e.Use();
                }
                else if (isResizing)
                {
                    Vector2 mousePos = e.mousePosition;

                    switch (currentResizeEdge)
                    {
                        case ResizeEdge.BottomRight:
                            windowWidth = Mathf.Clamp(mousePos.x - windowX, 300, Screen.width - windowX);
                            windowHeight = Mathf.Clamp(mousePos.y - windowY, 200, Screen.height - windowY);
                            break;

                        case ResizeEdge.Bottom:
                            windowHeight = Mathf.Clamp(mousePos.y - windowY, 200, Screen.height - windowY);
                            break;

                        case ResizeEdge.Right:
                            windowWidth = Mathf.Clamp(mousePos.x - windowX, 300, Screen.width - windowX);
                            break;

                        case ResizeEdge.Left:
                            float newWidth = windowX + windowWidth - mousePos.x;
                            if (newWidth >= 300)
                            {
                                windowX = Mathf.Clamp(mousePos.x, 0, windowX + windowWidth - 300);
                                windowWidth = windowX + windowWidth - windowX;
                            }
                            break;

                        case ResizeEdge.Top:
                            // Top edge resize: adjust position and height
                            float newHeight = windowY + windowHeight - mousePos.y;
                            if (newHeight >= 200)
                            {
                                windowY = Mathf.Clamp(mousePos.y, 0, windowY + windowHeight - 200);
                                windowHeight = windowY + windowHeight - windowY;
                            }
                            break;
                    }

                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                isDragging = false;
                isResizing = false;
                currentResizeEdge = ResizeEdge.None;
                e.Use();
            }
        }

        private static void EncyclopediaWindow(int windowId)
        {
            float leftPanelWidth = 200f;
            float rightPanelX = leftPanelWidth + 10f;
            Event e = Event.current;

            MenuPatches.ClearTooltip();

            GUI.Box(new Rect(leftPanelWidth, 0, 2, windowHeight), "", new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture }, margin = new RectOffset(0,0,0,0) });

            GUI.Label(new Rect(10, 30, leftPanelWidth - 20, 30), "📚 Categories", GUI.skin.label);

            int totalCategories = 0;
            foreach (Character character in Global.Instance.Characters)
            {
                if (character.SkillTree != null && character.SkillTree.upgrades.Count > 0) totalCategories++;
            }
            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades.Length > 0) totalCategories++;
            }

            float buttonHeight = 40;
            float buttonSpacing = 5;
            float totalButtonHeight = totalCategories * (buttonHeight + buttonSpacing);

            leftScrollPos = GUI.BeginScrollView(
                new Rect(10, 60, leftPanelWidth - 20, windowHeight - 110),
                leftScrollPos,
                new Rect(0, 0, leftPanelWidth - 40, totalButtonHeight),
                false, true);

            float buttonY = 0;

            foreach (Character character in Global.Instance.Characters)
            {
                if (character.SkillTree != null && character.SkillTree.upgrades.Count > 0)
                {
                    var upgrades = character.SkillTree.upgrades.ConvertAll(ui => ui.Upgrade);
                    bool isSelected = (MenuPatches.SelectedCategoryIndex >= 0 &&
                                      MenuPatches.SelectedCategoryGear == character);

                    string buttonText = isSelected ?
                        $"▶ 👤 {character.ClassName}\n({upgrades.Count} upgrades)" :
                        $"👤 {character.ClassName}\n({upgrades.Count} upgrades)";

                    if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                    {
                        int currentIndex = 0;
                        foreach (Character c in Global.Instance.Characters)
                        {
                            if (c.SkillTree != null && c.SkillTree.upgrades.Count > 0)
                            {
                                if (c == character)
                                {
                                    MenuPatches.SelectCategory(currentIndex);
                                    break;
                                }
                                currentIndex++;
                            }
                        }
                        foreach (IUpgradable g in Global.Instance.AllGear)
                        {
                            if (g.Info.HasVisibleUpgrades() && g.Info.Upgrades.Length > 0)
                            {
                                currentIndex++;
                            }
                        }
                        MenuPatches.ClearTooltip();
                    }
                    buttonY += buttonHeight + buttonSpacing;
                }
            }

            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades.Length > 0)
                {
                    bool isSelected = (MenuPatches.SelectedCategoryIndex >= 0 &&
                                      MenuPatches.SelectedCategoryGear == gear);

                    string buttonText = isSelected ?
                        $"▶ ⚔️ {gear.Info.Name}\n({gear.Info.Upgrades.Length} upgrades)" :
                        $"⚔️ {gear.Info.Name}\n({gear.Info.Upgrades.Length} upgrades)";

                    if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                    {
                        int currentIndex = 0;
                        foreach (Character c in Global.Instance.Characters)
                        {
                            if (c.SkillTree != null && c.SkillTree.upgrades.Count > 0)
                            {
                                currentIndex++;
                            }
                        }
                        foreach (IUpgradable g in Global.Instance.AllGear)
                        {
                            if (g.Info.HasVisibleUpgrades() && g.Info.Upgrades.Length > 0)
                            {
                                if (g == gear)
                                {
                                    MenuPatches.SelectCategory(currentIndex);
                                    break;
                                }
                                currentIndex++;
                            }
                        }
                        MenuPatches.ClearTooltip();
                    }
                    buttonY += buttonHeight + buttonSpacing;
                }
            }

            GUI.EndScrollView();

            if (e.type == EventType.ScrollWheel)
            {
                Vector2 mousePos = e.mousePosition;

                Rect leftScrollRect = new Rect(10, 60, leftPanelWidth - 20, windowHeight - 110);
                if (leftScrollRect.Contains(mousePos))
                {
                    float maxScroll = Mathf.Max(0, totalButtonHeight - (windowHeight - 110));
                    leftScrollPos.y = Mathf.Clamp(leftScrollPos.y + e.delta.y * 20f, 0, maxScroll);
                    e.Use();
                }
            }

            GUI.BeginGroup(new Rect(rightPanelX, 30, windowWidth - rightPanelX - 10, windowHeight - 50));

            if (MenuPatches.SelectedCategoryIndex >= 0 && MenuPatches.SelectedCategoryGear != null)
            {
                IUpgradable selectedGear = MenuPatches.SelectedCategoryGear;
                string header = $"🎯 {selectedGear.Info.Name}";
                GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30),
                         header, GUI.skin.label);

                var scrollPosition = Vector2.zero;

                System.Collections.Generic.List<Upgrade> upgrades = new System.Collections.Generic.List<Upgrade>();
                if (selectedGear is Character character && character.SkillTree != null)
                {
                    upgrades = character.SkillTree.upgrades.ConvertAll(ui => ui.Upgrade);
                }
                else
                {
                    upgrades.AddRange(selectedGear.Info.Upgrades);
                }

                upgrades.Sort((a, b) => {
                    int rarityA = GetRarityOrder(a);
                    int rarityB = GetRarityOrder(b);

                    if (rarityA != rarityB) return rarityA.CompareTo(rarityB);

                    return a.Name.CompareTo(b.Name);
                });

                int cardWidth = 110;
                int cardHeight = 85;
                const int cardSpacing = 8;
                const int scrollBarWidth = 20;
                const int totalSideMargin = 25;

                float scrollViewWidth = (windowWidth - rightPanelX - 10f - 20f);
                float availableContentWidth = scrollViewWidth - 20f;

                int availableCardsSize = (int)(availableContentWidth - totalSideMargin);
                int cardTotalSize = cardWidth + cardSpacing;
                int numCardsPerRow = 1;
                if (cardTotalSize > 0)
                {
                    numCardsPerRow = (availableCardsSize / cardTotalSize);
                    numCardsPerRow = Mathf.Max(1, numCardsPerRow);
                }

                float calculatedTotalRows = 1;
                if (numCardsPerRow > 0)
                {
                    float rawTotalRows = upgrades.Count / numCardsPerRow;
                    if (upgrades.Count % numCardsPerRow != 0)
                    {
                        rawTotalRows++;
                    }
                    calculatedTotalRows = rawTotalRows > 0 ? rawTotalRows : 1;
                }
                float totalContentWidth = (numCardsPerRow > 0) ?
                    ((numCardsPerRow - 1) * (cardWidth + cardSpacing)) + cardWidth : 0;
                float totalContentHeight = (calculatedTotalRows > 0) ?
                    ((calculatedTotalRows - 1) * (cardHeight + cardSpacing)) + cardHeight : 0;

                Rect scrollViewRect = new Rect(0, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120);
                Rect scrollContentRect = new Rect(0, 0, totalContentWidth, totalContentHeight);

                rightScrollPos = GUI.BeginScrollView(scrollViewRect, rightScrollPos, scrollContentRect, false, true);

                for (int i = 0; i < upgrades.Count; i++)
                {
                    Upgrade upgrade = upgrades[i];
                    var upgradeInfo = PlayerData.GetUpgradeInfo(selectedGear, upgrade);
                    bool isUnlocked = upgradeInfo.TotalInstancesCollected > 0;

                    int rowInt = i / numCardsPerRow;
                    float row = rowInt;
                    int colInt = i % numCardsPerRow;
                    float col = colInt;

                    float cardX = col * (cardWidth + cardSpacing);
                    float cardY = row * (cardHeight + cardSpacing);

                    GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
                    cardStyle.normal.background = Texture2D.whiteTexture;
                    Color backgroundColor = Global.GetRarity(upgrade.Rarity).backgroundColor;
                    if (!isUnlocked)
                    {
                        backgroundColor = new Color(backgroundColor.r * 0.5f, backgroundColor.g * 0.5f, backgroundColor.b * 0.5f, backgroundColor.a);
                    }
                    Color prevColor = GUI.color;
                    GUI.color = backgroundColor;
                    GUI.Box(new Rect(cardX, cardY, cardWidth, cardHeight), "", cardStyle);
                    GUI.color = prevColor;

                    GUI.Label(new Rect(cardX + 10, cardY + 10, 40, 40), "🎯", GUI.skin.label);

                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.fontSize = 12;
                    nameStyle.wordWrap = true;
                    if (!isUnlocked)
                    {
                        nameStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                    }
                    GUI.Label(new Rect(cardX + 10, cardY + 50, cardWidth - 20, 35), upgrade.Name, nameStyle);

                    ref RarityData rarity = ref Global.GetRarity(upgrade.Rarity);
                    GUIStyle rarityStyle = new GUIStyle(GUI.skin.label);
                    rarityStyle.normal.textColor = rarity.color;
                    rarityStyle.fontSize = 10;
                    GUI.Label(new Rect(cardX + 10, cardY + 35, cardWidth - 20, 15), rarity.Name, rarityStyle);

                    {
                        Vector2 mousePos = Event.current.mousePosition;
                        Rect cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);

                        if (cardRect.Contains(mousePos))
                        {
                            string tooltipText = GetUpgradeTooltipText(upgrade, selectedGear, isUnlocked);

                            Vector2 screenMousePos = new Vector2(mousePos.x + windowX, mousePos.y + windowY);
                            MenuPatches.SetTooltip(tooltipText, screenMousePos);
                        }
                        else if (MenuPatches.ShowTooltip)
                        {
                            bool mouseOverAnyCard = false;
                            for (int j = 0; j < upgrades.Count; j++)
                            {
                                int testRowInt = j / numCardsPerRow;
                                int testColInt = j % numCardsPerRow;
                                float testX = testColInt * (cardWidth + cardSpacing);
                                float testY = testRowInt * (cardHeight + cardSpacing);
                                Rect testRect = new Rect(testX, testY, cardWidth, cardHeight);
                                if (testRect.Contains(mousePos))
                                {
                                    mouseOverAnyCard = true;
                                    break;
                                }
                            }
                            if (!mouseOverAnyCard)
                            {
                                MenuPatches.ClearTooltip();
                            }
                        }
                    }
                }

                GUI.EndScrollView();

                if (e.type == EventType.ScrollWheel)
                {
                    Vector2 mousePos = e.mousePosition;

                    Rect rightScrollRect = new Rect(rightPanelX, 30 + 40, windowWidth - rightPanelX - 10 - 20, windowHeight - 120);
                    if (rightScrollRect.Contains(mousePos))
                    {
                        float maxScroll = Mathf.Max(0, totalContentHeight - (windowHeight - 120));
                        rightScrollPos.y = Mathf.Clamp(rightScrollPos.y + e.delta.y * 20f, 0, maxScroll);
                        e.Use();
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 100),
                         "📖 Select a category from the left panel to view upgrades\n\n" +
                         "Upgrades will appear as cards with icons, names, and hover tooltips.\n" +
                         "Locked upgrades appear dimmed to show they are not yet collected.",
                         GUI.skin.label);
            }

            GUI.EndGroup();

            if (GUI.Button(new Rect(windowWidth - 70, windowHeight - 40, 60, 30), "Close"))
            {
                MenuPatches.ShowEncyclopediaGUI = false;
                MenuPatches.ClearTooltip();
            }
        }

        private static int GetRarityOrder(Upgrade upgrade)
        {
            ref RarityData rarity = ref Global.GetRarity(upgrade.Rarity);
            switch (rarity.Name)
            {
                case "Oddity": return 1;
                case "Exotic": return 2;
                case "Epic": return 3;
                case "Rare": return 4;
                case "Standard": return 5;
                default: return 6;
            }
        }

        private static string StripRichTextTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "");
        }

        private static string GetUpgradeStatRanges(Upgrade upgrade, IUpgradable gear)
        {
            var originalShowExtraInfo = AccessTools.Method(typeof(HoverInfo), "ShowExtraInfo");
            var prefix = AccessTools.Method(typeof(StatRangePatches), "ShowExtraInfoPrefix");

            var harmony = new Harmony("encyclopedia.statranges");
            try
            {
                StatRangePatches.ForceShowExtraInfo = true;
                harmony.Patch(originalShowExtraInfo, new HarmonyMethod(prefix));
                string statListWithRanges = upgrade.GetStatList(0);
                return StripRichTextTags(statListWithRanges);
            }
            finally
            {
                StatRangePatches.ForceShowExtraInfo = false;
                harmony.Unpatch(originalShowExtraInfo, prefix);
            }
        }

        private static string GetUpgradeTooltipText(Upgrade upgrade, IUpgradable gear, bool isUnlocked)
        {
            ref RarityData rarity = ref Global.GetRarity(upgrade.Rarity);

            string tooltipText = $"-- {upgrade.Name} --\n";

            if (!isUnlocked)
            {
                tooltipText += "[🔒 Not Collected Yet]\n";
            }

            tooltipText += $"{rarity.Name} Upgrade\n\n";
            tooltipText += $"{upgrade.Description}\n\n";

            tooltipText += "Properties:\n";

            string statRanges = GetUpgradeStatRanges(upgrade, gear);
            if (!string.IsNullOrEmpty(statRanges))
            {
                tooltipText += statRanges;
            }
            else
            {
                string cleanStatList = StripRichTextTags(upgrade.GetStatList(0));
                if (!string.IsNullOrEmpty(cleanStatList))
                {
                    string[] stats = cleanStatList.Split('\n');
                    foreach (string stat in stats)
                    {
                        if (!string.IsNullOrWhiteSpace(stat))
                        {
                            tooltipText += $"• {stat}\n";
                        }
                    }
                }
                else
                {
                    tooltipText += "• Special upgrade effect (see description above)\n";
                }
            }

            return tooltipText;
        }
    }

    public static class MenuPatches
    {
        public static bool IsMenuOpen { get; private set; }
        public static bool ShowEncyclopediaGUI { get; set; }
        public static int SelectedCategoryIndex { get; set; }
        public static IUpgradable SelectedCategoryGear { get; set; }

        public static string CurrentTooltipText { get; set; } = "";
        public static Vector2 TooltipPosition { get; set; } = Vector2.zero;
        public static bool ShowTooltip { get; set; } = false;

        [HarmonyPatch(typeof(Menu), "Open")]
        public static void Prefix(Menu __instance)
        {
            IsMenuOpen = true;
            ShowEncyclopediaGUI = false;
            SelectedCategoryIndex = -1;
            SelectedCategoryGear = null;
        }

        [HarmonyPatch(typeof(Menu), "Close")]
        public static void Prefix()
        {
            IsMenuOpen = false;
            ShowEncyclopediaGUI = false;
            SelectedCategoryIndex = -1;
            SelectedCategoryGear = null;
        }

        public static void ToggleEncyclopedia()
        {
            ShowEncyclopediaGUI = !ShowEncyclopediaGUI;
            if (!ShowEncyclopediaGUI)
            {
                SelectedCategoryIndex = -1;
                SelectedCategoryGear = null;
                ShowTooltip = false;
                CurrentTooltipText = "";
            }
        }

        public static void SetTooltip(string text, Vector2 mousePosition)
        {
            if (CurrentTooltipText != text)
            {
                CurrentTooltipText = text;
                ShowTooltip = !string.IsNullOrEmpty(text);
                TooltipPosition = mousePosition;
            }
        }

        public static void ClearTooltip()
        {
            ShowTooltip = false;
            CurrentTooltipText = "";
        }

        public static void SelectCategory(int categoryIndex)
        {
            SelectedCategoryIndex = categoryIndex;

            int currentIndex = 0;
            foreach (Character character in Global.Instance.Characters)
            {
                if (character.SkillTree != null && character.SkillTree.upgrades.Count > 0)
                {
                    if (currentIndex == categoryIndex)
                    {
                        SelectedCategoryGear = character;
                        return;
                    }
                    currentIndex++;
                }
            }

            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades.Length > 0)
                {
                    if (currentIndex == categoryIndex)
                    {
                        SelectedCategoryGear = gear;
                        return;
                    }
                    currentIndex++;
                }
            }
        }

        public static string GetEncyclopediaContent()
        {
            var content = "🗃️ MYCOPUNK ENCYCLOPEDIA\n\nAvailable Categories:\n\n";

            int totalUpgrades = 0;

            foreach (Character character in Global.Instance.Characters)
            {
                if (character.SkillTree != null && character.SkillTree.upgrades.Count > 0)
                {
                    var upgrades = character.SkillTree.upgrades.ConvertAll(ui => ui.Upgrade);
                    content += $"👤 {character.ClassName}: {upgrades.Count} upgrades\n";
                    totalUpgrades += upgrades.Count;
                }
            }

            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades.Length > 0)
                {
                    content += $"⚔️ {gear.Info.Name}: {gear.Info.Upgrades.Length} upgrades\n";
                    totalUpgrades += gear.Info.Upgrades.Length;
                }
            }

            content += $"\n📊 Total: {totalUpgrades} upgrades across all categories";
            return content;
        }
    }

    public static class StatRangePatches
    {
        public static bool ForceShowExtraInfo { get; set; } = false;

        public static bool ShowExtraInfoPrefix(ref bool __result)
        {
            if (ForceShowExtraInfo)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
