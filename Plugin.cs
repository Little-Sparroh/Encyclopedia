using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using Pigeon;

namespace SparrohEncyclopedia
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [MycoMod(null, ModFlags.IsClientSide)]
    public class EncyclopediaPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "sparroh.encyclopedia";
        public const string PluginName = "Encyclopedia";
        public const string PluginVersion = "1.1.0";

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
                float requiredHeight =
                    tooltipStyle.CalcHeight(new GUIContent(MenuPatches.CurrentTooltipText), tooltipWidth);
                requiredHeight = Mathf.Clamp(requiredHeight, 80f, TOOLTIP_MAX_HEIGHT);

                float tooltipHeight = requiredHeight + 12;

                bool canFitAbove = (mousePos.y - tooltipHeight - 20f) >= 0;
                float yPosition = canFitAbove ? mousePos.y - tooltipHeight - 10f : mousePos.y + 20f;

                if (yPosition < TOOLTIP_MARGIN)
                {
                    yPosition = TOOLTIP_MARGIN;
                }
                else if (yPosition + tooltipHeight > Screen.height - TOOLTIP_MARGIN)
                {
                    yPosition = Screen.height - tooltipHeight - TOOLTIP_MARGIN;
                }

                bool canFitRight = (mousePos.x + 15f + tooltipWidth) <= Screen.width - TOOLTIP_MARGIN;
                float xPosition = canFitRight ? mousePos.x + 15f : mousePos.x - tooltipWidth - 15f;

                if (xPosition < TOOLTIP_MARGIN)
                {
                    xPosition = TOOLTIP_MARGIN;
                }
                else if (xPosition + tooltipWidth > Screen.width - TOOLTIP_MARGIN)
                {
                    xPosition = Screen.width - tooltipWidth - TOOLTIP_MARGIN;
                }

                Rect tooltipRect = new Rect(xPosition, yPosition, tooltipWidth, tooltipHeight);

                GUI.Window(999, tooltipRect, (windowId) =>
                {
                    Color prevColor = GUI.color;
                    GUI.color = Color.white;
                    GUI.Label(new Rect(0, 0, tooltipRect.width, tooltipRect.height),
                        MenuPatches.CurrentTooltipText, tooltipStyle);
                    GUI.color = prevColor;
                }, "", GUIStyle.none);
            }

            if (MenuPatches.IsMenuOpen && GUI.Button(new Rect(Screen.width - 110, 10, 100, 30), "Encyclopedia"))
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
                if (currentEvent.isMouse && (currentEvent.type == EventType.MouseDown ||
                                             currentEvent.type == EventType.MouseUp ||
                                             currentEvent.type == EventType.MouseDrag))
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

            GUI.Box(new Rect(leftPanelWidth, 0, 2, windowHeight), "",
                new GUIStyle(GUI.skin.box)
                    { normal = { background = Texture2D.whiteTexture }, margin = new RectOffset(0, 0, 0, 0) });

            GUI.Label(new Rect(10, 30, leftPanelWidth - 20, 30), "📚 Categories", GUI.skin.label);

            var universalUpgradesList = MenuPatches.GetUniversalUpgrades();
            int universalCount = universalUpgradesList.Count;

            var weapons = Global.Instance.AllGear.Where(g => g.GearType == GearType.Primary && g is IWeapon).ToList();
            int weaponCount = weapons.Count;

            var resources = Global.Instance.PlayerResources;
            int resourcesCount = resources.Length;

            int missionsCount = 0;
            int enemiesCount = 0;

            try
            {
                var missions = Global.Instance.Missions.Where(m => m.MissionFlags.HasFlag(MissionFlags.NormalMission) && m.CanBeSelected()).ToList();
                missionsCount = missions.Count;
            }
            catch (Exception ex)
            {
                missionsCount = 0;
            }

            try
            {
                enemiesCount = GetTotalEnemyCount();
            }
            catch (Exception ex)
            {
                enemiesCount = 0;
            }

            int totalCategories = 0;
            foreach (Character character in Global.Instance.Characters)
            {
                if (character.Info.HasVisibleUpgrades() && character.Info.Upgrades.Length > 0) totalCategories++;
            }

            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades.Length > 0) totalCategories++;
            }

            if (universalCount > 0) totalCategories++;
            if (weaponCount > 0) totalCategories++;
            if (resourcesCount > 0) totalCategories++;
            if (missionsCount > 0) totalCategories++;
            if (enemiesCount > 0) totalCategories++;

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
                if (character.Info != null && character.Info.HasVisibleUpgrades() && character.Info.Upgrades != null && character.Info.Upgrades.Length > 0)
                {
                    const string debugPattern =
                        @"(_test_|_dev_|_wip|debug|temp|placeholder|todo|_old|_backup|_copy|_staging|_exp|_alpha|_beta|_proto|_mock|_fake|_stub|_wingsuit|\.skinasset$|^test_|^wingsuit|^experimental|^dev_|^exp_|^proto_|^Test$|^Debug$|^Temp$|^Placeholder$|^Todo$|^Old$|^Backup$|^Copy$|^Staging$|^Exp$|^Alpha$|^Beta$|^Proto$|^Mock$|^Fake$|^Stub$|^Wingsuit$|^Experimental$)";
                    var fullUpgrades = character.Info.Upgrades
                        .Where(u => !Regex.IsMatch(u.Name, debugPattern, RegexOptions.IgnoreCase)).ToList();
                    bool isSelected = (MenuPatches.SelectedCategoryIndex >= 0 &&
                                       MenuPatches.SelectedCategoryGear == character);

                    string buttonText = isSelected
                        ? $"▶ {character.ClassName}\n({fullUpgrades.Count} upgrades)"
                        : $"{character.ClassName}\n({fullUpgrades.Count} upgrades)";

                    if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                    {
                        int currentIndex = 0;
                        foreach (Character c in Global.Instance.Characters)
                        {
                            if (c.Info.HasVisibleUpgrades() && c.Info.Upgrades.Length > 0)
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

            if (universalCount > 0)
            {
                bool isSelected = MenuPatches.IsUniversalSelected;
                string buttonText = isSelected
                    ? $"▶ Universal Upgrades\n({universalCount} upgrades)"
                    : $"Universal Upgrades\n({universalCount} upgrades)";

                if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                {
                    MenuPatches.SelectUniversal();
                    MenuPatches.ClearTooltip();
                }

                buttonY += buttonHeight + buttonSpacing;
            }

            if (weaponCount > 0)
            {
                bool isSelected = MenuPatches.IsWeaponSelected;
                string buttonText = isSelected
                    ? $"▶ Weapon Stats\n({weaponCount} weapons)"
                    : $"Weapon Stats\n({weaponCount} weapons)";

                if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                {
                    MenuPatches.SelectWeapons();
                    MenuPatches.ClearTooltip();
                }

                buttonY += buttonHeight + buttonSpacing;
            }

            if (resourcesCount > 0)
            {
                bool isSelected = MenuPatches.IsResourcesSelected;
                string buttonText = isSelected
                    ? $"▶ Resources\n({resourcesCount} resources)"
                    : $"Resources\n({resourcesCount} resources)";

                if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                {
                    MenuPatches.SelectResources();
                    MenuPatches.ClearTooltip();
                }

                buttonY += buttonHeight + buttonSpacing;
            }

            if (missionsCount > 0)
            {
                bool isSelected = MenuPatches.IsMissionsSelected;
                string buttonText = isSelected
                    ? $"▶ Missions\n({missionsCount} missions)"
                    : $"Missions\n({missionsCount} missions)";

                if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                {
                    MenuPatches.SelectMissions();
                    MenuPatches.ClearTooltip();
                }

                buttonY += buttonHeight + buttonSpacing;
            }

            if (enemiesCount > 0)
            {
                bool isSelected = MenuPatches.IsEnemiesSelected;
                string buttonText = isSelected
                    ? $"▶ Enemies\n({enemiesCount} enemies)"
                    : $"Enemies\n({enemiesCount} enemies)";

                if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                {
                    MenuPatches.SelectEnemies();
                    MenuPatches.ClearTooltip();
                }

                buttonY += buttonHeight + buttonSpacing;
            }

            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info != null && gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades != null && gear.Info.Upgrades.Length > 0 && Global.Instance.AllGear.Contains(gear))
                {
                    bool isSelected = (MenuPatches.SelectedCategoryIndex >= 0 &&
                                       MenuPatches.SelectedCategoryGear == gear);

                    string buttonText = isSelected
                        ? $"▶ {gear.Info.Name}\n({gear.Info.Upgrades.Length} upgrades)"
                        : $"{gear.Info.Name}\n({gear.Info.Upgrades.Length} upgrades)";

                    if (GUI.Button(new Rect(0, buttonY, leftPanelWidth - 40, buttonHeight), buttonText))
                    {
                        int currentIndex = 0;
                        foreach (Character c in Global.Instance.Characters)
                        {
                            if (c.Info.HasVisibleUpgrades() && c.Info.Upgrades.Length > 0)
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

            if ((MenuPatches.SelectedCategoryIndex >= 0 && MenuPatches.SelectedCategoryGear != null) || MenuPatches.IsUniversalSelected || MenuPatches.IsWeaponSelected || MenuPatches.IsResourcesSelected || MenuPatches.IsMissionsSelected || MenuPatches.IsEnemiesSelected)
            {
                if (MenuPatches.IsUniversalSelected)
                {
                    string header = $"Universal Upgrades";
                    GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30), header, GUI.skin.label);

                    System.Collections.Generic.List<Upgrade> upgrades = universalUpgradesList;

                    upgrades.Sort((a, b) =>
                    {
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

                    int totalUpgrades = upgrades.Count;
                    int numRowsUpgrades = totalUpgrades > 0 ? ((totalUpgrades + numCardsPerRow - 1) / numCardsPerRow) : 0;
                    int totalContentWidth = numCardsPerRow > 0 ? ((numCardsPerRow - 1) * (cardWidth + cardSpacing)) + cardWidth : 0;
                    int totalContentHeight = numRowsUpgrades > 0 ? ((numRowsUpgrades - 1) * (cardHeight + cardSpacing)) + cardHeight : 0;

                    Rect scrollViewRect = new Rect(0, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120);
                    Rect scrollContentRect = new Rect(0, 0, totalContentWidth, totalContentHeight);

                    rightScrollPos = GUI.BeginScrollView(scrollViewRect, rightScrollPos, scrollContentRect, false, true);

                    for (int i = 0; i < upgrades.Count; i++)
                    {
                        Upgrade upgrade = upgrades[i];
                        bool isUnlocked = true;

                        int rowInt = i / numCardsPerRow;
                        int colInt = i % numCardsPerRow;

                        float cardX = colInt * (cardWidth + cardSpacing);
                        float cardY = rowInt * (cardHeight + cardSpacing);

                        GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
                        cardStyle.normal.background = Texture2D.whiteTexture;
                        Color backgroundColor = Global.GetRarity(upgrade.Rarity).backgroundColor;

                        Color prevColor = GUI.color;
                        GUI.color = backgroundColor;
                        GUI.Box(new Rect(cardX, cardY, cardWidth, cardHeight), "", cardStyle);
                        GUI.color = prevColor;

                        ref RarityData rarity = ref Global.GetRarity(upgrade.Rarity);
                        GUIStyle rarityStyle = new GUIStyle(GUI.skin.label);
                        rarityStyle.normal.textColor = rarity.color;
                        rarityStyle.fontSize = 10;
                        GUI.Label(new Rect(cardX + 10, cardY + 10, cardWidth - 20, 15), rarity.Name, rarityStyle);

                        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                        nameStyle.fontSize = 12;
                        nameStyle.wordWrap = true;

                        GUI.Label(new Rect(cardX + 10, cardY + 25, cardWidth - 20, 60), StripRichTextTags(upgrade.Name), nameStyle);

                        Vector2 mousePos = Event.current.mousePosition;
                        Rect cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);

                        if (cardRect.Contains(mousePos))
                        {
                            IUpgradable dummyGear = Global.Instance.AllGear?.FirstOrDefault();
                            string tooltipText = GetUpgradeTooltipText(upgrade, dummyGear, true);

                            Vector2 screenMousePos = new Vector2(mousePos.x + windowX, mousePos.y + windowY);
                            MenuPatches.SetTooltip(tooltipText, screenMousePos);
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
                else if (MenuPatches.IsWeaponSelected)
                {
                    string header = "Weapon Stats";
                    GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30), header, GUI.skin.label);

                    int spacing = 15;
                    const int cardHeight = 50;
                    const int cardWidth = 200;
                    float totalHeight = weapons.Count * (cardHeight + spacing);
                    float panelWidth = (windowWidth - rightPanelX - 10) - 20;

                    Rect scrollViewRect = new Rect(0, 40, panelWidth, windowHeight - 120);
                    Rect scrollContentRect = new Rect(0, 0, scrollViewRect.width, totalHeight);

                    rightScrollPos = GUI.BeginScrollView(scrollViewRect, rightScrollPos, scrollContentRect, false, false);

                    float currentY = 0;
                    foreach (var weapon in weapons)
                    {
                        Color backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                        Color prevColor = GUI.color;
                        GUI.color = backgroundColor;
                        GUI.Box(new Rect(10, currentY, cardWidth, cardHeight), "", new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } });
                        GUI.color = prevColor;

                        string name = StripRichTextTags(weapon.Info.Name);

                        Sprite iconSprite = null;
                        try
                        {
                            var weaponType = weapon.GetType();
                            var infoProperty = weaponType.GetProperty("Info", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (infoProperty != null)
                            {
                                var gearInfo = infoProperty.GetValue(weapon);
                                if (gearInfo != null)
                                {
                                    var iconProperty = gearInfo.GetType().GetProperty("Icon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                    if (iconProperty != null)
                                    {
                                        var iconValue = iconProperty.GetValue(gearInfo);
                                        iconSprite = iconValue as Sprite;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }

                        if (iconSprite != null && iconSprite.texture != null)
                        {
                            float scale = Mathf.Min(cardWidth / iconSprite.rect.width, cardHeight / iconSprite.rect.height);
                            float iconWidth = iconSprite.rect.width * scale;
                            float iconHeight = iconSprite.rect.height * scale;
                            float iconX = 10 + (cardWidth - 10 - iconWidth) / 2f;
                            float iconY = currentY + (cardHeight - iconHeight) / 2f;

                            var drawRect = new Rect(iconX, iconY, iconWidth, iconHeight);

                            Rect texCoords = new Rect(iconSprite.textureRect.xMin / iconSprite.texture.width,
                                                     iconSprite.textureRect.yMin / iconSprite.texture.height,
                                                     iconSprite.textureRect.width / iconSprite.texture.width,
                                                     iconSprite.textureRect.height / iconSprite.texture.height);

                            GUI.DrawTexture(drawRect, iconSprite.texture);

                        }

                        Rect cardRect = new Rect(10, currentY, cardWidth, cardHeight);

                        if (cardRect.Contains(Event.current.mousePosition))
                        {
                            string tooltip = GetWeaponTooltipText((IWeapon)weapon);
                            var screenMousePos = new Vector2(Event.current.mousePosition.x + windowX, Event.current.mousePosition.y + windowY);
                            MenuPatches.SetTooltip(tooltip, screenMousePos);
                        }

                        currentY += cardHeight + spacing;
                    }

                    GUI.EndScrollView();

                    if (e.type == EventType.ScrollWheel)
                    {
                        Vector2 mousePos = e.mousePosition;
                        if (scrollViewRect.Contains(mousePos))
                        {
                            float maxScroll = Mathf.Max(0, totalHeight - scrollViewRect.height);
                            rightScrollPos.y = Mathf.Clamp(rightScrollPos.y + e.delta.y * 20f, 0, maxScroll);
                            e.Use();
                        }
                    }
                }
                else if (MenuPatches.IsResourcesSelected)
                {
                    string header = $"Resources";
                    GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30), header, GUI.skin.label);

                    int cardWidth = 90;
                    int cardHeight = 70;
                    const int cardSpacing = 8;
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

                    int totalResources = resources.Length;
                    int numRowsResources = totalResources > 0 ? ((totalResources + numCardsPerRow - 1) / numCardsPerRow) : 0;
                    int totalContentWidth = numCardsPerRow > 0 ? ((numCardsPerRow - 1) * (cardWidth + cardSpacing)) + cardWidth : 0;
                    int totalContentHeight = numRowsResources > 0 ? ((numRowsResources - 1) * (cardHeight + cardSpacing)) + cardHeight : 0;

                    Rect scrollViewRect = new Rect(0, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120);
                    Rect scrollContentRect = new Rect(0, 0, totalContentWidth, totalContentHeight);

                    rightScrollPos = GUI.BeginScrollView(scrollViewRect, rightScrollPos, scrollContentRect, false, true);

                    for (int i = 0; i < resources.Length; i++)
                    {
                        PlayerResource resource = resources[i];

                        if (resource.Visibility == PlayerResource.VisibilityOptions.DontShow)
                            continue;

                        int rowInt = i / numCardsPerRow;
                        int colInt = i % numCardsPerRow;

                        float cardX = colInt * (cardWidth + cardSpacing);
                        float cardY = rowInt * (cardHeight + cardSpacing);

                        GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
                        cardStyle.normal.background = Texture2D.whiteTexture;
                        Color backgroundColor = resource.Color;

                        Color prevColor = GUI.color;
                        GUI.color = backgroundColor;
                        GUI.Box(new Rect(cardX, cardY, cardWidth, cardHeight), "", cardStyle);
                        GUI.color = prevColor;

                        ref RarityData rarity = ref Global.GetRarity(resource.Rarity);
                        GUIStyle rarityStyle = new GUIStyle(GUI.skin.label);
                        rarityStyle.normal.textColor = rarity.color;
                        rarityStyle.fontSize = 8;
                        GUI.Label(new Rect(cardX + 5, cardY + 5, 50, 12), rarity.Name, rarityStyle);

                        if (resource.Icon != null)
                        {
                            const float iconSize = 20f;
                            float iconX = cardX + (cardWidth - iconSize) / 2f;
                            float iconY = cardY + 20f;

                            Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);
                            GUI.DrawTexture(iconRect, resource.Icon.texture);
                        }

                        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                        nameStyle.fontSize = 9;
                        nameStyle.wordWrap = true;
                        nameStyle.alignment = TextAnchor.UpperCenter;

                        string resourceName = StripRichTextTags(resource.Name);
                        if (resourceName.Length > 12)
                            resourceName = resourceName.Substring(0, 10) + "...";

                        GUI.Label(new Rect(cardX + 3, cardY + 42, cardWidth - 6, 20), resourceName, nameStyle);

                        GUIStyle countStyle = new GUIStyle(GUI.skin.label);
                        countStyle.fontSize = 8;
                        countStyle.alignment = TextAnchor.LowerCenter;

                        string countText = resource.ItemCount.ToString();
                        if (resource.Max > 0 && resource.Max < 9999)
                        {
                            countText += $"/{resource.Max}";
                        }

                        GUI.Label(new Rect(cardX + 3, cardY + 50, cardWidth - 6, 15), countText, countStyle);

                        Vector2 mousePos = Event.current.mousePosition;
                        Rect cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);

                        if (cardRect.Contains(mousePos))
                        {
                            string tooltipText = GetResourceTooltipText(resource);
                            Vector2 screenMousePos = new Vector2(mousePos.x + windowX, mousePos.y + windowY);
                            MenuPatches.SetTooltip(tooltipText, screenMousePos);
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
                else if (MenuPatches.IsMissionsSelected)
                {
                    string header = $"Missions";
                    GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30), header, GUI.skin.label);

                    GUI.Label(new Rect(10, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120),
                        "Missions system currently unavailable due to compatibility issues.\n" +
                        "This feature will be added in a future update.");
                }
                else if (MenuPatches.IsEnemiesSelected)
                {
                    string header = $"Enemies";
                    GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30), header, GUI.skin.label);

                    try
                    {
                        DisplayEnemiesPanel(windowWidth, rightPanelX, windowHeight, rightScrollPos);
                    }
                    catch (Exception ex)
                    {
                        GUI.Label(new Rect(10, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120),
                            "Enemies database currently unavailable due to compatibility issues.\n" +
                            "This feature will be added in a future update.");
                    }
                }
                else
                {
                    IUpgradable selectedGear = MenuPatches.SelectedCategoryGear;
                    string header = $"{selectedGear.Info.Name}";
                    GUI.Label(new Rect(0, 0, (windowWidth - rightPanelX - 10) - 20, 30), header, GUI.skin.label);

                    var scrollPosition = Vector2.zero;

                    System.Collections.Generic.List<Upgrade> upgrades = new System.Collections.Generic.List<Upgrade>();
                    if (selectedGear is Character character)
                    {
                        const string debugPattern = @"(_test_|_dev_|_wip|debug|temp|placeholder|todo|_old|_backup|_copy|_staging|_exp|_alpha|_beta|_proto|_mock|_fake|_stub|_wingsuit|\.skinasset$|^test_|^wingsuit|^experimental|^dev_|^exp_|^proto_|^Test$|^Debug$|^Temp$|^Placeholder$|^Todo$|^Old$|^Backup$|^Copy$|^Staging$|^Exp$|^Alpha$|^Beta$|^Proto$|^Mock$|^Fake$|^Stub$|^Wingsuit$|^Experimental$)";
                        var allUpgrades = character.Info.Upgrades.Where(u => !Regex.IsMatch(u.Name, debugPattern, RegexOptions.IgnoreCase)).ToList();
                        upgrades = allUpgrades;
                    }
                    else
                    {
                        upgrades.AddRange(selectedGear.Info.Upgrades);
                    }

                    upgrades.Sort((a, b) =>
                    {
                        int rarityA = GetRarityOrder(a);
                        int rarityB = GetRarityOrder(b);
                        if (rarityA != rarityB) return rarityA.CompareTo(rarityB);
                        return a.Name.CompareTo(b.Name);
                    });

                    var normalUpgrades = upgrades.Where(u => u.UpgradeType != Upgrade.Type.Cosmetic).ToList();
                    var cosmeticUpgrades = upgrades.Where(u => u.UpgradeType == Upgrade.Type.Cosmetic).ToList();

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

                    int numNormal = normalUpgrades.Count;
                    int numCosmetic = cosmeticUpgrades.Count;
                    int totalUpgrades = numNormal + numCosmetic;
                    int numRowsUpgrades = totalUpgrades > 0 ? ((totalUpgrades + numCardsPerRow - 1) / numCardsPerRow) : 0;
                    int totalContentWidth = numCardsPerRow > 0 ? ((numCardsPerRow - 1) * (cardWidth + cardSpacing)) + cardWidth : 0;
                    int totalContentHeight = numRowsUpgrades > 0 ? ((numRowsUpgrades - 1) * (cardHeight + cardSpacing)) + cardHeight : 0;

                    Rect scrollViewRect = new Rect(0, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120);
                    Rect scrollContentRect = new Rect(0, 0, totalContentWidth, totalContentHeight);

                    rightScrollPos = GUI.BeginScrollView(scrollViewRect, rightScrollPos, scrollContentRect, false, true);

                    upgrades.Clear();
                    upgrades.AddRange(normalUpgrades);
                    upgrades.AddRange(cosmeticUpgrades);

                    for (int i = 0; i < upgrades.Count; i++)
                    {
                        Upgrade upgrade = upgrades[i];
                        var upgradeInfo = PlayerData.GetUpgradeInfo(selectedGear, upgrade);
                        bool isUnlocked = upgradeInfo.TotalInstancesCollected > 0;

                        int rowInt = i / numCardsPerRow;
                        int colInt = i % numCardsPerRow;

                        float cardX = colInt * (cardWidth + cardSpacing);
                        float cardY = rowInt * (cardHeight + cardSpacing);

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

                        ref RarityData rarity = ref Global.GetRarity(upgrade.Rarity);
                        GUIStyle rarityStyle = new GUIStyle(GUI.skin.label);
                        rarityStyle.normal.textColor = rarity.color;
                        rarityStyle.fontSize = 10;
                        GUI.Label(new Rect(cardX + 10, cardY + 10, cardWidth - 20, 15), rarity.Name, rarityStyle);

                        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                        nameStyle.fontSize = 12;
                        nameStyle.wordWrap = true;
                        if (!isUnlocked)
                        {
                            nameStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                        }

                        GUI.Label(new Rect(cardX + 10, cardY + 25, cardWidth - 20, 60), StripRichTextTags(upgrade.Name), nameStyle);

                        Vector2 mousePos = Event.current.mousePosition;
                        Rect cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);

                        if (cardRect.Contains(mousePos))
                        {
                            string tooltipText = GetUpgradeTooltipText(upgrade, selectedGear, isUnlocked);

                            Vector2 screenMousePos = new Vector2(mousePos.x + windowX, mousePos.y + windowY);
                            MenuPatches.SetTooltip(tooltipText, screenMousePos);
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
                string statListWithRanges = upgrade.GetStatList(0, new UpgradeInstance(upgrade, gear));
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

            string tooltipText = $"-- {StripRichTextTags(upgrade.Name)} --\n";

            if (!isUnlocked)
            {
                tooltipText += "[Not Collected Yet]\n";
            }

            tooltipText += $"{rarity.Name} Upgrade\n\n";
            tooltipText += $"{StripRichTextTags(upgrade.Description)}\n\n";

            tooltipText += "Properties:\n";

            string statRanges = GetUpgradeStatRanges(upgrade, gear);
            if (!string.IsNullOrEmpty(statRanges))
            {
                tooltipText += statRanges;
            }
            else
            {
                string cleanStatList = StripRichTextTags(upgrade.GetStatList(0, new UpgradeInstance(upgrade, gear)));
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

        private static string GetWeaponTooltipText(IWeapon weapon)
        {
            string tooltip = $"-- {StripRichTextTags(weapon.Info.Name)} --\n";

            string description = StripRichTextTags(weapon.Info.Description);
            tooltip += $"{description}\n\n";

            tooltip += "Base Stats:\n";

            try
            {
                Dictionary<string, StatInfo> primaryStats = new Dictionary<string, StatInfo>();
                Dictionary<string, StatInfo> secondaryStats = new Dictionary<string, StatInfo>();

                UpgradeStatChanges statChanges = new UpgradeStatChanges();

                var primaryEnum = weapon.EnumeratePrimaryStats(statChanges);
                while (primaryEnum.MoveNext())
                {
                    primaryStats[primaryEnum.Current.name] = primaryEnum.Current;
                }

                var secondaryEnum = weapon.EnumerateSecondaryStats(statChanges);
                while (secondaryEnum.MoveNext())
                {
                    if (secondaryEnum.Current.name != "Aim Zoom")
                    {
                        secondaryStats[secondaryEnum.Current.name] = secondaryEnum.Current;
                    }
                }

                AddWeaponStatToTooltip(ref tooltip, secondaryStats, "Damage");
                AddWeaponStatToTooltip(ref tooltip, primaryStats, "Damage Type");
                AddWeaponStatToTooltip(ref tooltip, secondaryStats, "Fire Rate");
                AddWeaponStatToTooltip(ref tooltip, secondaryStats, "Ammo Capacity");
                AddWeaponStatToTooltip(ref tooltip, secondaryStats, "Reload Duration");
                AddWeaponStatToTooltip(ref tooltip, secondaryStats, "Range");

                ref GunData gunData = ref weapon.GunData;
                tooltip += $"\n• Burst Size: {gunData.burstSize}\n";
                tooltip += $"• Fire Mode: {(gunData.automatic == 1 ? "Automatic" : "Semi Automatic")}\n";
            }
            catch (Exception ex)
            {
                tooltip += "Base Stats: [Error loading weapon data]\n";
                tooltip += $"• Type: Primary Weapon\n";
            }

            return tooltip;
        }

        private static void AddWeaponStatToTooltip(ref string tooltip, Dictionary<string, StatInfo> stats, string statName)
        {
            if (stats.TryGetValue(statName, out StatInfo stat))
            {
                string cleanValue = StripColors(stat.value);

                if (statName == "Fire Rate")
                {
                    try
                    {
                        if (float.TryParse(cleanValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float rpm) && rpm > 0)
                        {
                            float shotsPerSec = rpm / 60f;
                            tooltip += $"• {stat.name}: {shotsPerSec:F1} shots/sec\n";
                            return;
                        }
                    }
                    catch (Exception) { }
                }

                tooltip += $"• {stat.name}: {cleanValue}\n";
            }
        }

        private static string StripColors(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, "<color#[0-9A-Fa-f]{8}>|</color>", "");
        }

        private static string GetResourceTooltipText(PlayerResource resource)
        {
            ref RarityData rarity = ref Global.GetRarity(resource.Rarity);

            string tooltip = $"-- {StripRichTextTags(resource.Name)} --\n";

            string resourceType = resource.IsItem ? "Item" : "Resource";
            tooltip += $"{rarity.Name} {resourceType}\n\n";

            string description = StripRichTextTags(resource.Description);
            if (!string.IsNullOrEmpty(description))
            {
                tooltip += $"{description}\n\n";
            }

            tooltip += $"Owned: {resource.ItemCount}";
            if (resource.Max > 0 && resource.Max < 9999)
            {
                tooltip += $"/{resource.Max}";
            }
            tooltip += "\n\n";

            if (resource.Max > 0 && resource.Max < 9999)
            {
                tooltip += $"• Max capacity: {resource.Max}\n";
            }
            else
            {
                tooltip += $"• Unlimited capacity\n";
            }

            if (resource.IsItem)
            {
                tooltip += $"• Can be used as an item\n";
                if (resource.UnlockUseCount > 0)
                {
                    tooltip += $"• Usable after collecting {resource.UnlockUseCount} samples\n";
                }
            }
            else
            {
                tooltip += $"• Resource can be gathered repeatedly\n";
            }

            return tooltip;
        }

        private static string GetMissionTooltipText(Mission mission)
        {
            string tooltip = $"-- Mission Information --\n\n";

            tooltip += $"Missions provide objectives and challenges throughout the game.\n";
            tooltip += $"Each mission has specific goals to complete.\n\n";

            tooltip += $"• Log in regularly to get new missions\n";
            tooltip += $"• Different types of challenges available\n";
            tooltip += $"• Rewards for mission completion\n";

            return tooltip;
        }



        private static void DisplayEnemiesPanel(float windowWidth, float rightPanelX, float windowHeight, Vector2 rightScrollPos)
        {
            try
            {
                Type enemyManagerType = typeof(EnemyManager);
                PropertyInfo instanceProp = enemyManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null) return;

                object instance = instanceProp.GetValue(null);
                if (instance == null) return;

                PropertyInfo enemyGroupsProp = enemyManagerType.GetProperty("EnemyClassGroups", BindingFlags.Public | BindingFlags.Instance);
                if (enemyGroupsProp == null) return;

                object enemyGroups = enemyGroupsProp.GetValue(instance);
                if (enemyGroups == null) return;

                var groups = GetItemsFromWeightedArray<EnemyClassGroup>(enemyGroups);
                if (groups == null) return;

                List<EnemyClass> allEnemies = new List<EnemyClass>();
                foreach (var group in groups)
                {
                    if (group != null && group.enemyClasses != null)
                    {
                        var classes = GetItemsFromWeightedArray<EnemyClass>(group.enemyClasses);
                        if (classes != null)
                        {
                            allEnemies.AddRange(classes);
                        }
                    }
                }

                int cardWidth = 100;
                int cardHeight = 80;
                const int cardSpacing = 8;
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

                int totalEnemies = allEnemies.Count;
                int numRowsEnemies = totalEnemies > 0 ? ((totalEnemies + numCardsPerRow - 1) / numCardsPerRow) : 0;
                int totalContentWidth = numCardsPerRow > 0 ? ((numCardsPerRow - 1) * (cardWidth + cardSpacing)) + cardWidth : 0;
                int totalContentHeight = numRowsEnemies > 0 ? ((numRowsEnemies - 1) * (cardHeight + cardSpacing)) + cardHeight : 0;

                Rect scrollViewRect = new Rect(0, 40, (windowWidth - rightPanelX - 10) - 20, windowHeight - 120);
                Rect scrollContentRect = new Rect(0, 0, totalContentWidth, totalContentHeight);

                rightScrollPos = GUI.BeginScrollView(scrollViewRect, rightScrollPos, scrollContentRect, false, true);

                for (int i = 0; i < allEnemies.Count; i++)
                {
                    EnemyClass enemy = allEnemies[i];

                    int rowInt = i / numCardsPerRow;
                    int colInt = i % numCardsPerRow;

                    float cardX = colInt * (cardWidth + cardSpacing);
                    float cardY = rowInt * (cardHeight + cardSpacing);

                    GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
                    cardStyle.normal.background = Texture2D.whiteTexture;
                    Color backgroundColor = new Color(0.3f, 0.3f, 0.4f, 1f);

                    Color prevColor = GUI.color;
                    GUI.color = backgroundColor;
                    GUI.Box(new Rect(cardX, cardY, cardWidth, cardHeight), "", cardStyle);
                    GUI.color = prevColor;

                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.fontSize = 10;
                    nameStyle.wordWrap = true;
                    nameStyle.alignment = TextAnchor.UpperCenter;

                    string enemyName = StripRichTextTags(enemy.Name);
                    if (enemyName.Length > 14)
                        enemyName = enemyName.Substring(0, 12) + "...";

                    GUI.Label(new Rect(cardX + 5, cardY + 8, cardWidth - 10, 20), enemyName, nameStyle);

                    GUIStyle typeStyle = new GUIStyle(GUI.skin.label);
                    typeStyle.fontSize = 8;
                    typeStyle.alignment = TextAnchor.MiddleCenter;

                    string enemyTypeText = "";
                    Color enemyTypeColor = Color.white;

                    try
                    {
                        switch (enemy.config.enemyType)
                        {
                            case EnemyType.Grunt:
                                enemyTypeText = "Grunt";
                                enemyTypeColor = new Color(0.7f, 0.7f, 0.7f);
                                break;
                            case EnemyType.Brute:
                                enemyTypeText = "Brute";
                                enemyTypeColor = new Color(0.8f, 0.6f, 0.4f);
                                break;
                            case EnemyType.Abomination:
                                enemyTypeText = "Abom.";
                                enemyTypeColor = Color.red;
                                break;
                            case EnemyType.Boss:
                                enemyTypeText = "BOSS";
                                enemyTypeColor = Color.yellow;
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        enemyTypeText = "Unknown";
                        enemyTypeColor = Color.gray;
                    }

                    if (!string.IsNullOrEmpty(enemyTypeText))
                    {
                        Color prevColor2 = GUI.color;
                        GUI.color = enemyTypeColor;
                        GUI.Label(new Rect(cardX + 5, cardY + 30, cardWidth - 10, 15), enemyTypeText, typeStyle);
                        GUI.color = prevColor2;
                    }

                    Vector2 mousePos = Event.current.mousePosition;
                    Rect cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);

                    if (cardRect.Contains(mousePos))
                    {
                        string tooltipText = GetEnemyTooltipText(enemy);
                        Vector2 screenMousePos = new Vector2(mousePos.x + windowX, mousePos.y + windowY);
                        MenuPatches.SetTooltip(tooltipText, screenMousePos);
                    }
                }

                GUI.EndScrollView();

                Event e = Event.current;
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
            catch (Exception ex)
            {
                throw;
            }
        }

        private static string GetEnemyTooltipText(EnemyClass enemy)
        {
            string tooltip = $"-- {StripRichTextTags(enemy.Name)} --\n\n";

            try
            {
                string typeText = "";
                switch (enemy.config.enemyType)
                {
                    case EnemyType.Grunt:
                        typeText = "Grunt (Basic enemy)";
                        break;
                    case EnemyType.Brute:
                        typeText = "Brute (Heavy combatant)";
                        break;
                    case EnemyType.Abomination:
                        typeText = "Abomination (Powerful mutated enemy)";
                        break;
                    case EnemyType.Boss:
                        typeText = "Boss (End-game threat)";
                        break;
                }
                if (!string.IsNullOrEmpty(typeText))
                {
                    tooltip += $"{typeText}\n\n";
                }
            }
            catch (Exception)
            {
                tooltip += "Enemy type information unavailable\n\n";
            }

            try
            {
                var configType = enemy.config.GetType();
                var descriptionProperty = configType.GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (descriptionProperty != null)
                {
                    var descriptionValue = descriptionProperty.GetValue(enemy.config) as string;
                    if (!string.IsNullOrEmpty(descriptionValue))
                    {
                        tooltip += $"Description:\n{StripRichTextTags(descriptionValue)}\n\n";
                    }
                }
            }
            catch (Exception)
            {
            }

            tooltip += "Combat Information:\n";

            if (enemy.legChance > 0)
            {
                tooltip += $"• {enemy.legChance * 100:F0}% chance to have legs\n";
            }

            if (enemy.armChance > 0)
            {
                tooltip += $"• {enemy.armChance * 100:F0}% chance to have arms\n";
            }

            try
            {
                var attachmentsProperty = enemy.GetType().GetField("attachmentsGroups", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (attachmentsProperty != null)
                {
                    var attachments = attachmentsProperty.GetValue(enemy) as System.Array;
                    if (attachments != null && attachments.Length > 0)
                    {
                        int totalAttachmentTypes = 0;
                        foreach (var group in attachments)
                        {
                            if (group != null)
                            {
                                var chanceField = group.GetType().GetField("chance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                var maxField = group.GetType().GetField("max", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (chanceField != null && maxField != null)
                                {
                                    float chance = (float)chanceField.GetValue(group);
                                    int max = (int)maxField.GetValue(group);
                                    if (chance > 0 && max > 0)
                                        totalAttachmentTypes++;
                                }
                            }
                        }
                        if (totalAttachmentTypes > 0)
                        {
                            tooltip += $"• Can spawn with up to {totalAttachmentTypes} types of attachments\n";
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            if (enemy.overclockChance > 0)
            {
                tooltip += $"• {enemy.overclockChance * 100:F0}% chance to be overclocked\n";
            }

            return tooltip;
        }

        private static int GetTotalEnemyCount()
        {
            try
            {
                Type enemyManagerType = typeof(EnemyManager);

                PropertyInfo instanceProp = enemyManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null) return 0;

                object instance = instanceProp.GetValue(null);
                if (instance == null) return 0;

                PropertyInfo enemyGroupsProp = enemyManagerType.GetProperty("EnemyClassGroups", BindingFlags.Public | BindingFlags.Instance);
                if (enemyGroupsProp == null) return 0;

                object enemyGroups = enemyGroupsProp.GetValue(instance);
                if (enemyGroups == null) return 0;

                var groups = GetItemsFromWeightedArray<EnemyClassGroup>(enemyGroups);
                if (groups == null) return 0;

                int total = 0;
                foreach (var group in groups)
                {
                    if (group == null || group.enemyClasses == null) continue;

                    var classes = GetItemsFromWeightedArray<EnemyClass>(group.enemyClasses);
                    if (classes != null)
                    {
                        total += classes.Count;
                    }
                }

                return total;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private static List<T> GetItemsFromWeightedArray<T>(object weightedArray)
        {
            if (weightedArray == null) return null;

            Type type = weightedArray.GetType();

            if (!type.IsGenericType || type.GetGenericTypeDefinition().Name != "WeightedArray`1")
                throw new ArgumentException("Expected instance of WeightedArray<T>");

            FieldInfo itemsField = type.GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
            if (itemsField == null)
                throw new MissingFieldException("Could not find 'items' field in WeightedArray");

            Array items = itemsField.GetValue(weightedArray) as Array;
            if (items == null) return null;

            List<T> result = new List<T>();

            foreach (var node in items)
            {
                if (node == null) continue;

                var nodeType = node.GetType();
                FieldInfo weightField = nodeType.GetField("weight");
                FieldInfo valueField = nodeType.GetField("value");

                if (weightField == null || valueField == null) continue;

                int weight = (int)weightField.GetValue(node);
                if (weight > 0)
                {
                    T value = (T)valueField.GetValue(node);
                    result.Add(value);
                }
            }

            return result;
        }


    }

    public static class MenuPatches
    {
        private const int SELECT_WEAPONS = -10;
        public static bool IsMenuOpen { get; private set; }
        public static bool ShowEncyclopediaGUI { get; set; }
        public static int SelectedCategoryIndex { get; set; }
        public static IUpgradable SelectedCategoryGear { get; set; }

        public static string CurrentTooltipText { get; set; } = "";
        public static Vector2 TooltipPosition { get; set; } = Vector2.zero;
        public static bool ShowTooltip { get; set; } = false;
        public static bool IsUniversalSelected { get; set; } = false;
        public static bool IsWeaponSelected { get; set; } = false;
        public static bool IsResourcesSelected { get; set; } = false;
        public static bool IsMissionsSelected { get; set; } = false;
        public static bool IsEnemiesSelected { get; set; } = false;

        [HarmonyPatch(typeof(Menu), "Open")]
        public static void Prefix(Menu __instance)
        {
            IsMenuOpen = true;
            ShowEncyclopediaGUI = false;
            SelectedCategoryIndex = -1;
            SelectedCategoryGear = null;
            IsUniversalSelected = false;
            IsWeaponSelected = false;
            IsResourcesSelected = false;
            IsMissionsSelected = false;
            IsEnemiesSelected = false;
        }

        [HarmonyPatch(typeof(Menu), "Close")]
        public static void Prefix()
        {
            IsMenuOpen = false;
            ShowEncyclopediaGUI = false;
            SelectedCategoryIndex = -1;
            SelectedCategoryGear = null;
            IsUniversalSelected = false;
            IsWeaponSelected = false;
            IsResourcesSelected = false;
            IsMissionsSelected = false;
            IsEnemiesSelected = false;
        }

        public static void ToggleEncyclopedia()
        {
            ShowEncyclopediaGUI = !ShowEncyclopediaGUI;
            if (!ShowEncyclopediaGUI)
            {
                SelectedCategoryIndex = -1;
                SelectedCategoryGear = null;
                IsUniversalSelected = false;
                IsWeaponSelected = false;
                IsResourcesSelected = false;
                IsMissionsSelected = false;
                IsEnemiesSelected = false;
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

        public static void SelectUniversal()
        {
            SelectedCategoryIndex = -1;
            SelectedCategoryGear = null;
            SelectedCategoryIndex = -2;
            IsUniversalSelected = true;
            SelectedCategoryGear = null;
            IsWeaponSelected = false;
        }

        public static void SelectWeapons()
        {
            SelectedCategoryIndex = SELECT_WEAPONS;
            SelectedCategoryGear = null;
            IsUniversalSelected = false;
            IsWeaponSelected = true;
            IsResourcesSelected = false;
            IsMissionsSelected = false;
            IsEnemiesSelected = false;
        }

        public static void SelectResources()
        {
            SelectedCategoryIndex = -3;
            SelectedCategoryGear = null;
            IsUniversalSelected = false;
            IsWeaponSelected = false;
            IsResourcesSelected = true;
            IsMissionsSelected = false;
            IsEnemiesSelected = false;
        }

        public static void SelectMissions()
        {
            SelectedCategoryIndex = -4;
            SelectedCategoryGear = null;
            IsUniversalSelected = false;
            IsWeaponSelected = false;
            IsResourcesSelected = false;
            IsMissionsSelected = true;
            IsEnemiesSelected = false;
        }

        public static void SelectEnemies()
        {
            SelectedCategoryIndex = -5;
            SelectedCategoryGear = null;
            IsUniversalSelected = false;
            IsWeaponSelected = false;
            IsResourcesSelected = false;
            IsMissionsSelected = false;
            IsEnemiesSelected = true;
        }


        public static System.Collections.Generic.List<Upgrade> GetUniversalUpgrades()
        {
            var allGenericUpgrades = Resources.FindObjectsOfTypeAll<GenericPlayerUpgrade>();
            System.Collections.Generic.HashSet<Upgrade> skillTreeUpgrades = new System.Collections.Generic.HashSet<Upgrade>();
            foreach (var character in Global.Instance.Characters)
            {
                var skillTree = character.SkillTree;
                if (skillTree != null)
                {
                    SkillTreeUpgradeUI[] treeUpgradesUI = skillTree.GetComponentsInChildren<SkillTreeUpgradeUI>();
                    skillTreeUpgrades.UnionWith(treeUpgradesUI.Select(ui => ui.Upgrade));
                }
            }
            System.Collections.Generic.HashSet<Upgrade> characterSpecificUpgrades = new System.Collections.Generic.HashSet<Upgrade>();
            foreach (var character in Global.Instance.Characters)
            {
                characterSpecificUpgrades.UnionWith(character.Info.Upgrades);
            }
            const string debugPattern = @"(_test_|_dev_|_wip|debug|temp|placeholder|todo|_old|_backup|_copy|_staging|_exp|_alpha|_beta|_proto|_mock|_fake|_stub|_wingsuit|\.skinasset$|^test_|^wingsuit|^experimental|^dev_|^exp_|^proto_|^Test$|^Debug$|^Temp$|^Placeholder$|^Todo$|^Old$|^Backup$|^Copy$|^Staging$|^Exp$|^Alpha$|^Beta$|^Proto$|^Mock$|^Fake$|^Stub$|^Wingsuit$|^Experimental$)";
            var genericUpgrades = allGenericUpgrades
                .Where(u => !Regex.IsMatch(u.Name, debugPattern, RegexOptions.IgnoreCase) &&
                            !skillTreeUpgrades.Contains(u) &&
                            !characterSpecificUpgrades.Contains(u))
                .Cast<Upgrade>()
                .ToList();
            return genericUpgrades;
        }

        public static void SelectCategory(int categoryIndex)
        {
            IsUniversalSelected = false;
            IsWeaponSelected = false;
            SelectedCategoryIndex = categoryIndex;

            int currentIndex = 0;
            foreach (Character character in Global.Instance.Characters)
            {
                if (character.Info.HasVisibleUpgrades() && character.Info.Upgrades.Length > 0)
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
            var content = "MYCOPUNK ENCYCLOPEDIA\n\nAvailable Categories:\n\n";

            int totalUpgrades = 0;

            foreach (Character character in Global.Instance.Characters)
            {
                if (character.SkillTree != null && character.SkillTree.upgrades.Count > 0)
                {
                    var upgrades = character.SkillTree.upgrades.ConvertAll(ui => ui.Upgrade);
                    content += $"{character.ClassName}: {upgrades.Count} upgrades\n";
                    totalUpgrades += upgrades.Count;
                }
            }

            foreach (IUpgradable gear in Global.Instance.AllGear)
            {
                if (gear.Info.HasVisibleUpgrades() && gear.Info.Upgrades.Length > 0)
                {
                    content += $"{gear.Info.Name}: {gear.Info.Upgrades.Length} upgrades\n";
                    totalUpgrades += gear.Info.Upgrades.Length;
                }
            }

            content += $"\n Total: {totalUpgrades} upgrades across all categories";
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
