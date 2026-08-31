#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class MultiplayerPrefabBuilder
{
    private const string ViewFolder = "Assets/Prefabs/view";
    private const string TemplateFolder = "Assets/Prefabs/template";
    private const string FontFolder = "Assets/Fonts";
    private const string UiFontPath = "Assets/Fonts/WN-Sans-SC-Medium.ttf";
    private const string UiFontAssetPath = "Assets/Fonts/WN-Sans-SC-Medium SDF.asset";

    private static readonly Color BackgroundColor = new Color32(32, 39, 48, 255);
    private static readonly Color PanelColor = new Color32(47, 57, 70, 255);
    private static readonly Color PanelDarkColor = new Color32(38, 46, 58, 255);
    private static readonly Color PanelLightColor = new Color32(58, 70, 86, 255);
    private static readonly Color ButtonColor = new Color32(61, 123, 171, 255);
    private static readonly Color ButtonMutedColor = new Color32(84, 92, 104, 255);
    private static readonly Color DangerColor = new Color32(169, 72, 72, 255);
    private static readonly Color AccentColor = new Color32(232, 171, 84, 255);
    private static readonly Color TextColor = new Color32(238, 242, 246, 255);
    private static readonly Color MutedTextColor = new Color32(174, 184, 196, 255);
    private static readonly Color InputColor = new Color32(25, 31, 39, 255);

    private static TMP_FontAsset _uiFontAsset;

    [MenuItem("Card Game/Build Multiplayer UI Prefabs")]
    public static void BuildAll()
    {
        EnsureFolders();
        EnsureUIFontAsset();
        BuildLobbyPrefab();
        BuildRoomPrefab();
        BuildGamePrefab();
        BuildCardPrefab();
        BuildRoomItemPrefab();
        BuildBlockingOverlayPrefab();
        ApplyUIFontToTMPSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Multiplayer UI prefabs rebuilt.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "view");
        EnsureFolder("Assets/Prefabs", "template");
        EnsureFolder("Assets", "Fonts");
    }

    private static void EnsureUIFontAsset()
    {
        _uiFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
        if (_uiFontAsset != null && IsValidFontAsset(_uiFontAsset))
        {
            _uiFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            EditorUtility.SetDirty(_uiFontAsset);
            return;
        }

        if (_uiFontAsset != null)
        {
            AssetDatabase.DeleteAsset(UiFontAssetPath);
            _uiFontAsset = null;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"UI font was not found at {UiFontPath}; TextMeshPro will keep using its project default font.");
            return;
        }

        _uiFontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);
        _uiFontAsset.name = "WN-Sans-SC-Medium SDF";
        _uiFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        _uiFontAsset.TryAddCharacters("房间列表刷新暂无创建加入个人信息快速匹配空闲通过房间号名称准备添加移除开始游戏等待玩家离开手牌出牌区取消中已连接未连接房主座位");
        AssetDatabase.CreateAsset(_uiFontAsset, UiFontAssetPath);
        AddFontSubAssets(_uiFontAsset);
    }

    private static void AddFontSubAssets(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return;
        }

        Material material = fontAsset.material;
        if (material != null && !AssetDatabase.Contains(material))
        {
            material.name = $"{fontAsset.name} Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
        }

        Texture2D[] atlasTextures = fontAsset.atlasTextures;
        if (atlasTextures == null)
        {
            return;
        }

        for (int index = 0; index < atlasTextures.Length; index += 1)
        {
            Texture2D atlasTexture = atlasTextures[index];
            if (atlasTexture == null || AssetDatabase.Contains(atlasTexture))
            {
                continue;
            }

            atlasTexture.name = $"{fontAsset.name} Atlas {index}";
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }
    }

    private static bool IsValidFontAsset(TMP_FontAsset fontAsset)
    {
        return fontAsset != null
            && fontAsset.atlasTextures != null
            && fontAsset.atlasTextures.Length > 0
            && fontAsset.atlasTextures[0] != null;
    }

    private static void ApplyUIFontToTMPSettings()
    {
        TMP_FontAsset uiFontAsset = GetUIFontAsset();
        if (uiFontAsset == null)
        {
            return;
        }

        TMP_Settings tmpSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        if (tmpSettings == null)
        {
            return;
        }

        SerializedObject serializedSettings = new SerializedObject(tmpSettings);
        serializedSettings.FindProperty("m_defaultFontAsset").objectReferenceValue = uiFontAsset;

        SerializedProperty fallbackFonts = serializedSettings.FindProperty("m_fallbackFontAssets");
        fallbackFonts.arraySize = 1;
        fallbackFonts.GetArrayElementAtIndex(0).objectReferenceValue = uiFontAsset;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tmpSettings);
    }

    private static TMP_FontAsset GetUIFontAsset()
    {
        if (_uiFontAsset == null)
        {
            _uiFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
        }

        return _uiFontAsset;
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private static void BuildLobbyPrefab()
    {
        GameObject lobby = CreateViewRoot("Lobby");
        Transform root = lobby.transform.Find("root");

        CreateImage(root, "bg", BackgroundColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject roomList = CreatePanel(root, "panel_room_list", new Color32(43, 53, 66, 255));
        SetRect(roomList, new Vector2(0.03f, 0.08f), new Vector2(0.67f, 0.92f), Vector2.zero, Vector2.zero);
        AddVerticalLayout(roomList, 12, new RectOffset(20, 20, 18, 20), TextAnchor.UpperLeft, false, false);

        GameObject roomHeader = CreatePanel(roomList.transform, "panel_room_list_header", PanelDarkColor);
        SetFixedHeight(roomHeader, 70);
        AddHorizontalLayout(roomHeader, 12, new RectOffset(14, 14, 10, 10), TextAnchor.MiddleCenter);
        CreateText(roomHeader.transform, "txt_title", "房间列表", 30, TextAlignmentOptions.MidlineLeft, TextColor);
        CreateButton(roomHeader.transform, "btn_refresh", "刷新", ButtonMutedColor, 130, 50);

        GameObject scroll = CreateScrollView(roomList.transform, "scroll_room_list");
        SetFlexible(scroll, 1, 1);
        CreateText(roomList.transform, "txt_empty_hint", "暂无房间，创建一个或点击刷新", 22, TextAlignmentOptions.Center, MutedTextColor);

        GameObject actions = CreatePanel(root, "panel_actions", new Color32(40, 49, 61, 255));
        SetRect(actions, new Vector2(0.70f, 0.08f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);
        AddVerticalLayout(actions, 16, new RectOffset(18, 18, 18, 18), TextAnchor.UpperLeft, false, false);

        GameObject playerInfo = CreatePanel(actions.transform, "panel_player_info", PanelDarkColor);
        SetFixedHeight(playerInfo, 150);
        AddVerticalLayout(playerInfo, 8, new RectOffset(14, 14, 12, 12), TextAnchor.UpperLeft, false, false);
        CreateText(playerInfo.transform, "txt_player_name", "player_A7K2", 28, TextAlignmentOptions.Left, TextColor);
        CreateText(playerInfo.transform, "txt_player_id", "ID: ------", 18, TextAlignmentOptions.Left, MutedTextColor);
        CreateText(playerInfo.transform, "txt_connection_status", "未连接", 18, TextAlignmentOptions.Left, MutedTextColor);
        CreateButton(playerInfo.transform, "btn_profile", "个人信息", ButtonMutedColor, -1, 42);

        GameObject matchmaking = CreatePanel(actions.transform, "panel_matchmaking", PanelDarkColor);
        SetFixedHeight(matchmaking, 130);
        AddVerticalLayout(matchmaking, 10, new RectOffset(14, 14, 12, 12), TextAnchor.UpperLeft, false, false);
        CreateButton(matchmaking.transform, "btn_matchmaking", "快速匹配", ButtonColor, -1, 52);
        CreateText(matchmaking.transform, "txt_matchmaking_status", "空闲", 18, TextAlignmentOptions.Left, MutedTextColor);

        GameObject joinRoom = CreatePanel(actions.transform, "panel_join_room", PanelDarkColor);
        SetFixedHeight(joinRoom, 138);
        AddVerticalLayout(joinRoom, 10, new RectOffset(14, 14, 12, 12), TextAnchor.UpperLeft, false, false);
        CreateText(joinRoom.transform, "txt_join_title", "通过房间号加入", 20, TextAlignmentOptions.Left, TextColor);
        GameObject joinRow = CreateHorizontalGroup(joinRoom.transform, "panel_join_row", 10);
        CreateInput(joinRow.transform, "input_room_id", "4位房间号", 150, 48);
        CreateButton(joinRow.transform, "btn_join_room", "加入", ButtonColor, 100, 48);

        GameObject createRoom = CreatePanel(actions.transform, "panel_create_room", PanelDarkColor);
        SetFixedHeight(createRoom, 150);
        AddVerticalLayout(createRoom, 10, new RectOffset(14, 14, 12, 12), TextAnchor.UpperLeft, false, false);
        CreateText(createRoom.transform, "txt_create_title", "创建房间", 20, TextAlignmentOptions.Left, TextColor);
        CreateInput(createRoom.transform, "input_room_name", "房间名称", -1, 46);
        CreateButton(createRoom.transform, "btn_create_room", "创建", ButtonColor, -1, 48);

        SavePrefab(lobby, $"{ViewFolder}/Lobby.prefab");
    }

    private static void BuildRoomPrefab()
    {
        GameObject room = CreateViewRoot("Room");
        Transform root = room.transform.Find("root");

        CreateImage(root, "bg", BackgroundColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject header = CreatePanel(root, "panel_header", PanelDarkColor);
        SetRect(header, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
        AddHorizontalLayout(header, 14, new RectOffset(18, 18, 12, 12), TextAnchor.MiddleCenter);
        CreateText(header.transform, "txt_room_name", "房间名称", 28, TextAlignmentOptions.MidlineLeft, TextColor);
        CreateText(header.transform, "txt_room_id", "房间号 1234", 22, TextAlignmentOptions.MidlineLeft, MutedTextColor, 220, -1);
        CreateText(header.transform, "txt_owner_name", "房主 -", 22, TextAlignmentOptions.MidlineLeft, MutedTextColor, 220, -1);
        CreateButton(header.transform, "btn_leave", "离开", DangerColor, 120, 52);

        GameObject players = CreatePanel(root, "panel_players", PanelColor);
        SetRect(players, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
        AddHorizontalLayout(players, 18, new RectOffset(18, 18, 18, 18), TextAnchor.MiddleCenter);
        for (int index = 0; index < 3; index += 1)
        {
            GameObject slot = CreatePlayerSlot(players.transform, $"slot_player{index}", index);
            SetFlexible(slot, 1, 1);
        }

        GameObject controls = CreatePanel(root, "panel_controls", PanelDarkColor);
        SetRect(controls, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.22f), Vector2.zero, Vector2.zero);
        AddHorizontalLayout(controls, 18, new RectOffset(18, 18, 14, 14), TextAnchor.MiddleCenter);
        CreateButton(controls.transform, "btn_ready", "准备", ButtonColor, 180, 56);
        CreateButton(controls.transform, "btn_add_ai", "添加AI", ButtonMutedColor, 180, 56);
        CreateButton(controls.transform, "btn_start_game", "开始游戏", ButtonColor, 220, 56);

        CreateText(root, "txt_room_status", "等待玩家准备", 22, TextAlignmentOptions.Center, MutedTextColor);
        SetRect(root.Find("txt_room_status").gameObject, new Vector2(0.20f, 0.04f), new Vector2(0.80f, 0.09f), Vector2.zero, Vector2.zero);

        SavePrefab(room, $"{ViewFolder}/Room.prefab");
    }

    private static void BuildRoomItemPrefab()
    {
        GameObject item = CreatePanel(null, "room_item", new Color32(52, 63, 78, 255));
        SetRect(item, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(0, -43));
        SetSize(item, 0, 86);
        AddHorizontalLayout(item, 14, new RectOffset(16, 16, 10, 10), TextAnchor.MiddleCenter);
        CreateText(item.transform, "txt_room_name", "房间名称", 24, TextAlignmentOptions.MidlineLeft, TextColor);
        CreateText(item.transform, "txt_room_id", "1234", 22, TextAlignmentOptions.MidlineLeft, MutedTextColor, 110, -1);
        CreateText(item.transform, "txt_owner_name", "房主", 20, TextAlignmentOptions.MidlineLeft, MutedTextColor, 180, -1);
        CreateText(item.transform, "txt_player_count", "1/3", 22, TextAlignmentOptions.Center, MutedTextColor, 80, -1);
        CreateButton(item.transform, "btn_join", "加入", ButtonColor, 110, 50);

        SavePrefab(item, $"{TemplateFolder}/room_item.prefab");
    }

    private static void BuildBlockingOverlayPrefab()
    {
        GameObject overlay = CreatePanel(null, "blocking_overlay", new Color(0, 0, 0, 0));
        SetRect(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateImage(overlay.transform, "bg_dim", new Color(0, 0, 0, 0.58f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateButton(overlay.transform, "btn_blocker", "", new Color(1, 1, 1, 0), -1, -1);
        SetRect(overlay.transform.Find("btn_blocker").gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject panel = CreatePanel(overlay.transform, "panel_message", new Color32(42, 51, 64, 245));
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        SetSize(panel, 440, 230);
        AddVerticalLayout(panel, 16, new RectOffset(26, 26, 22, 22), TextAnchor.MiddleCenter, false, false);
        CreateText(panel.transform, "txt_title", "匹配中", 30, TextAlignmentOptions.Center, TextColor);
        CreateText(panel.transform, "txt_message", "等待玩家 1/3", 22, TextAlignmentOptions.Center, MutedTextColor);
        CreateButton(panel.transform, "btn_cancel", "取消", DangerColor, 180, 50);

        SavePrefab(overlay, $"{TemplateFolder}/blocking_overlay.prefab");
    }

    private static void BuildGamePrefab()
    {
        GameObject game = CreateViewRoot("Game");
        Transform root = game.transform.Find("root");

        CreateImage(root, "bg", new Color32(26, 32, 39, 255), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateImage(root, "bg_table_glow", new Color32(40, 66, 74, 120), new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.82f), Vector2.zero, Vector2.zero);

        GameObject topBar = CreatePanel(root, "panel_top_bar", new Color32(30, 39, 49, 245));
        SetRect(topBar, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        AddHorizontalLayout(topBar, 18, new RectOffset(22, 22, 10, 10), TextAnchor.MiddleCenter);
        CreateText(topBar.transform, "txt_title", "对局进行中", 28, TextAlignmentOptions.MidlineLeft, TextColor);
        CreateText(topBar.transform, "txt_hint", "轮到你时点击手牌出牌", 20, TextAlignmentOptions.MidlineRight, MutedTextColor, 420, -1);
        CreateButton(topBar.transform, "btn_close", "离开", DangerColor, 120, 52);

        GameObject player2 = CreateOpponentPanel(root, "card_player2", "玩家 3");
        SetRect(player2, new Vector2(0.04f, 0.40f), new Vector2(0.25f, 0.80f), Vector2.zero, Vector2.zero);
        CreateText(player2.transform, "txt_timer2", "10", 24, TextAlignmentOptions.Center, AccentColor);
        SetRect(player2.transform.Find("txt_timer2").gameObject, new Vector2(0.72f, 0.72f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);

        GameObject player1 = CreateOpponentPanel(root, "card_player1", "玩家 2");
        SetRect(player1, new Vector2(0.75f, 0.40f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
        CreateText(player1.transform, "txt_timer1", "10", 24, TextAlignmentOptions.Center, AccentColor);
        SetRect(player1.transform.Find("txt_timer1").gameObject, new Vector2(0.72f, 0.72f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);

        GameObject cardArea = CreatePanel(root, "card_area", new Color32(35, 47, 55, 230));
        SetRect(cardArea, new Vector2(0.34f, 0.39f), new Vector2(0.66f, 0.70f), Vector2.zero, Vector2.zero);
        AddVerticalLayout(cardArea, 10, new RectOffset(18, 18, 18, 18), TextAnchor.MiddleCenter, false, false);
        CreateText(cardArea.transform, "txt_area_hint", "出牌区", 24, TextAlignmentOptions.Center, MutedTextColor);

        GameObject localPanel = CreatePanel(root, "panel_local", new Color32(34, 43, 54, 245));
        SetRect(localPanel, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.31f), Vector2.zero, Vector2.zero);
        AddVerticalLayout(localPanel, 10, new RectOffset(22, 22, 14, 18), TextAnchor.UpperLeft, false, false);

        GameObject localHeader = CreateHorizontalGroup(localPanel.transform, "panel_local_header", 10);
        CreateText(localHeader.transform, "txt_player0_name", "玩家 1（你）", 24, TextAlignmentOptions.MidlineLeft, TextColor);
        CreateText(localHeader.transform, "txt_timer0", "10", 24, TextAlignmentOptions.MidlineRight, AccentColor, 90, -1);

        GameObject localCards = CreatePanel(localPanel.transform, "card_player0", new Color32(23, 30, 39, 255));
        SetFixedHeight(localCards, 170);
        AddHorizontalLayout(localCards, 12, new RectOffset(14, 14, 12, 12), TextAnchor.MiddleLeft);

        SavePrefab(game, $"{ViewFolder}/Game.prefab");
    }

    private static void BuildCardPrefab()
    {
        GameObject card = CreatePanel(null, "card", new Color32(238, 232, 212, 255));
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        SetSize(card, 118, 158);

        GameObject inner = CreatePanel(card.transform, "panel_inner", new Color32(248, 244, 230, 255));
        SetRect(inner, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

        CreateText(card.transform, "card_name", "AS", 34, TextAlignmentOptions.Center, new Color32(42, 50, 58, 255));
        SetRect(card.transform.Find("card_name").gameObject, new Vector2(0.14f, 0.34f), new Vector2(0.86f, 0.66f), Vector2.zero, Vector2.zero);

        GameObject button = CreateImage(card.transform, "btn", new Color(1, 1, 1, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button cardButton = button.AddComponent<Button>();
        cardButton.targetGraphic = button.GetComponent<Image>();

        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 118;
        layoutElement.preferredHeight = 158;

        SavePrefab(card, $"{TemplateFolder}/card.prefab");
    }

    private static GameObject CreateOpponentPanel(Transform parent, string name, string playerName)
    {
        GameObject panel = CreatePanel(parent, name, new Color32(41, 52, 65, 245));
        AddVerticalLayout(panel, 12, new RectOffset(16, 16, 16, 16), TextAnchor.UpperCenter, false, false);
        CreateText(panel.transform, name == "card_player1" ? "txt_player1_name" : "txt_player2_name", playerName, 24, TextAlignmentOptions.Center, TextColor);

        GameObject cardNum = CreatePanel(panel.transform, "card_num", new Color32(24, 31, 40, 255));
        SetFixedHeight(cardNum, 76);
        AddVerticalLayout(cardNum, 2, new RectOffset(10, 10, 8, 8), TextAnchor.MiddleCenter, false, false);
        CreateText(cardNum.transform, "txt_label", "手牌", 18, TextAlignmentOptions.Center, MutedTextColor);
        CreateText(cardNum.transform, "txt_card_num", "0", 30, TextAlignmentOptions.Center, AccentColor);
        return panel;
    }

    private static GameObject CreateViewRoot(string viewName)
    {
        GameObject view = new GameObject(viewName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform viewRect = view.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.zero;
        viewRect.pivot = Vector2.zero;
        viewRect.anchoredPosition = Vector2.zero;
        viewRect.sizeDelta = Vector2.zero;

        Canvas canvas = view.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = view.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject root = new GameObject("root", typeof(RectTransform));
        root.transform.SetParent(view.transform, false);
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return view;
    }

    private static GameObject CreatePlayerSlot(Transform parent, string name, int seatIndex)
    {
        GameObject slot = CreatePanel(parent, name, PanelDarkColor);
        AddVerticalLayout(slot, 10, new RectOffset(16, 16, 16, 16), TextAnchor.UpperLeft, false, false);
        CreateText(slot.transform, "txt_seat", $"座位 {seatIndex + 1}", 24, TextAlignmentOptions.Center, TextColor);
        CreateText(slot.transform, "txt_name", "空位", 28, TextAlignmentOptions.Center, TextColor);
        CreateText(slot.transform, "txt_player_type", "-", 20, TextAlignmentOptions.Center, MutedTextColor);
        CreateText(slot.transform, "txt_ready", "未准备", 20, TextAlignmentOptions.Center, MutedTextColor);
        CreateText(slot.transform, "txt_owner", "", 20, TextAlignmentOptions.Center, MutedTextColor);
        CreateButton(slot.transform, "btn_remove_ai", "移除AI", DangerColor, -1, 46);
        return slot;
    }

    private static void AddTextIfMissing(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (parent.Find(name) != null)
        {
            return;
        }

        GameObject textObject = CreateText(parent, name, text, 22, TextAlignmentOptions.Center, TextColor);
        SetRect(textObject, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = CreateImage(parent, name, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return panel;
    }

    private static GameObject CreateImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
        {
            imageObject.transform.SetParent(parent, false);
        }

        SetRect(imageObject, anchorMin, anchorMax, pivot, anchoredPosition);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color color, float width, float height)
    {
        GameObject buttonObject = CreateImage(parent, name, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        if (width > 0 || height > 0)
        {
            SetSize(buttonObject, Mathf.Max(width, 0), Mathf.Max(height, 0));
        }

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        if (width > 0)
        {
            layoutElement.preferredWidth = width;
        }
        else
        {
            layoutElement.flexibleWidth = 1;
        }

        if (height > 0)
        {
            layoutElement.preferredHeight = height;
        }

        CreateText(buttonObject.transform, "txt_label", label, 22, TextAlignmentOptions.Center, TextColor);
        return buttonObject;
    }

    private static GameObject CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color,
        float preferredWidth = -1,
        float preferredHeight = -1)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetRect(textObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        TMP_FontAsset uiFontAsset = GetUIFontAsset();
        if (uiFontAsset != null)
        {
            tmp.font = uiFontAsset;
        }

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        if (preferredWidth > 0)
        {
            layoutElement.preferredWidth = preferredWidth;
        }
        else
        {
            layoutElement.flexibleWidth = 1;
        }

        if (preferredHeight > 0)
        {
            layoutElement.preferredHeight = preferredHeight;
        }

        return textObject;
    }

    private static GameObject CreateInput(Transform parent, string name, string placeholder, float width, float height)
    {
        GameObject input = CreateImage(parent, name, InputColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        TMP_InputField inputField = input.AddComponent<TMP_InputField>();
        SetSize(input, Mathf.Max(width, 0), height);

        GameObject textArea = new GameObject("text_area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(input.transform, false);
        SetRect(textArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Offset(textArea, 12, 8, -12, -8);

        GameObject placeholderText = CreateText(textArea.transform, "txt_placeholder", placeholder, 20, TextAlignmentOptions.MidlineLeft, new Color32(120, 130, 142, 255));
        GameObject inputText = CreateText(textArea.transform, "txt_input", "", 20, TextAlignmentOptions.MidlineLeft, TextColor);
        inputField.textViewport = textArea.GetComponent<RectTransform>();
        inputField.placeholder = placeholderText.GetComponent<TextMeshProUGUI>();
        inputField.textComponent = inputText.GetComponent<TextMeshProUGUI>();
        inputField.targetGraphic = input.GetComponent<Image>();

        LayoutElement layoutElement = input.AddComponent<LayoutElement>();
        if (width > 0)
        {
            layoutElement.preferredWidth = width;
        }
        else
        {
            layoutElement.flexibleWidth = 1;
        }
        layoutElement.preferredHeight = height;
        return input;
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        GameObject scroll = CreateImage(parent, name, new Color32(31, 38, 48, 255), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreateImage(scroll.transform, "viewport", new Color(1, 1, 1, 0.02f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        SetRect(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        return scroll;
    }

    private static GameObject CreateHorizontalGroup(Transform parent, string name, float spacing)
    {
        GameObject group = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        group.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = group.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        LayoutElement layoutElement = group.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 52;
        return group;
    }

    private static void AddVerticalLayout(GameObject target, float spacing, RectOffset padding, TextAnchor alignment, bool expandWidth, bool expandHeight)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = expandWidth;
        layout.childForceExpandHeight = expandHeight;
    }

    private static void AddHorizontalLayout(GameObject target, float spacing, RectOffset padding, TextAnchor alignment)
    {
        HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void SetFlexible(GameObject target, float width, float height)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = width;
        layoutElement.flexibleHeight = height;
    }

    private static void SetFixedHeight(GameObject target, float height)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 1;
    }

    private static void SetSize(GameObject target, float width, float height)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void SetRect(GameObject target, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static void Offset(GameObject target, float left, float top, float right, float bottom)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(right, -top);
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
#endif
