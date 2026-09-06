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
    private const string GameBackgroundPath = "Assets/Arts/game/bg_1.png";
    private const string CharacterOnePath = "Assets/Arts/character_1.png";
    private const string CharacterTwoPath = "Assets/Arts/chrarcter_2.png";
    private const string CardArtFolder = "Assets/Arts/cards/1x";
    private const string CardBackPath = "Assets/Arts/cards/back.png";
    private static readonly string[] CardSuits = { "spade", "heart", "club", "diamond" };

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
        EnsureGameSpriteImportSettings();
        EnsureUIFontAsset();
        BuildLx3GamePrefab();
        BuildCardPrefab();
        BuildGamePhasePrefabs();
        LobbyRoomPrefabBuilder.BuildLobbyRoomPrefabs();
        ApplyUIFontToTMPSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Multiplayer UI prefabs rebuilt.");
    }

    private static void EnsureGameSpriteImportSettings()
    {
        EnsureSpriteImportSettings(GameBackgroundPath, 4096);
        EnsureSpriteImportSettings(CharacterOnePath, 4096);
        EnsureSpriteImportSettings(CharacterTwoPath, 4096);
        EnsureSpriteImportSettings(CardBackPath, 2048);
        foreach (string suit in CardSuits)
        {
            for (int rank = 2; rank <= 7; rank += 1)
            {
                EnsureSpriteImportSettings($"{CardArtFolder}/{suit}_{rank}.png", 2048);
            }
        }
    }

    private static void EnsureSpriteImportSettings(string path, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Cannot find texture importer at {path}.");
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.maxTextureSize < maxTextureSize)
        {
            importer.maxTextureSize = maxTextureSize;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
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
        _uiFontAsset.TryAddCharacters("房间列表刷新暂无创建加入个人信息快速匹配空闲通过房间号名称准备添加移除开始游戏等待玩家离开手牌出牌区取消中已连接未连接房主座位福童沪上阿姨");
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

    private static void BuildLx3GamePrefab()
    {
        GameObject game = CreateViewRoot("Game");
        Transform root = game.transform.Find("root");

        CreateImage(root, "bg", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Sprite(GameBackgroundPath));
        CreateImage(root, "bg_table_dim", new Color32(68, 8, 8, 72), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject topBar = CreatePanel(root, "panel_top_bar", new Color32(92, 24, 20, 210));
        SetRect(topBar, new Vector2(0.04f, 0.885f), new Vector2(0.96f, 0.965f), Vector2.zero, Vector2.zero);
        CreateText(topBar.transform, "txt_phase_title", "出牌阶段", 28, TextAlignmentOptions.MidlineLeft, AccentColor);
        SetRect(topBar.transform.Find("txt_phase_title").gameObject, new Vector2(0.02f, 0.12f), new Vector2(0.16f, 0.88f), Vector2.zero, Vector2.zero);
        CreateText(topBar.transform, "txt_phase_hint", "选择手牌，指定明牌后确认出牌", 20, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(topBar.transform.Find("txt_phase_hint").gameObject, new Vector2(0.18f, 0.12f), new Vector2(0.78f, 0.88f), Vector2.zero, Vector2.zero);
        CreateButton(topBar.transform, "btn_menu", "菜单", DangerColor, 120, 52);
        SetRect(topBar.transform.Find("btn_menu").gameObject, new Vector2(0.88f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);

        GameObject seatLocal = CreateGameSeatPanel(root, "seat_local", "你");
        SetAnchoredRect(seatLocal, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-64f, 64f), new Vector2(300f, 280f));

        GameObject seatLeft = CreateGameSeatPanel(root, "seat_left", "左家");
        SetAnchoredRect(seatLeft, new Vector2(0f, 0.56f), new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(300f, 280f));

        GameObject seatTop = CreateGameSeatPanel(root, "seat_top", "上家");
        SetAnchoredRect(seatTop, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(300f, 280f));

        GameObject seatRight = CreateGameSeatPanel(root, "seat_right", "右家");
        SetAnchoredRect(seatRight, new Vector2(1f, 0.56f), new Vector2(1f, 0.5f), new Vector2(-64f, 0f), new Vector2(300f, 280f));

        GameObject phaseHost = new GameObject("phase_host", typeof(RectTransform));
        phaseHost.transform.SetParent(root, false);
        SetRect(phaseHost, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject localHand = CreatePanel(root, "panel_local_hand", new Color32(87, 24, 20, 228));
        SetAnchoredRect(localHand, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(108f, 56f), new Vector2(770f, 170f));
        AddVerticalLayout(localHand, 5, new RectOffset(14, 14, 8, 10), TextAnchor.UpperLeft, false, false);
        CreateText(localHand.transform, "txt_hand_title", "你的手牌", 20, TextAlignmentOptions.Left, AccentColor);
        GameObject localCards = CreatePanel(localHand.transform, "card_player0", new Color32(45, 12, 10, 120));
        SetFixedHeight(localCards, 106);
        AddHorizontalLayout(localCards, 6, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleLeft);

        GameObject chat = CreatePanel(root, "panel_chat", new Color32(64, 17, 15, 220));
        SetAnchoredRect(chat, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 56f), new Vector2(500f, 300f));
        CreateText(chat.transform, "txt_chat_title", "房间聊天", 22, TextAlignmentOptions.Left, AccentColor);
        SetRect(chat.transform.Find("txt_chat_title").gameObject, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);
        GameObject chatPreview = CreateText(chat.transform, "txt_chat_preview", "", 17, TextAlignmentOptions.TopLeft, MutedTextColor);
        SetRect(chatPreview, new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.75f), Vector2.zero, Vector2.zero);
        GameObject chatInputRow = CreateHorizontalGroup(chat.transform, "panel_chat_input", 10);
        SetRect(chatInputRow, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.23f), Vector2.zero, Vector2.zero);
        CreateInput(chatInputRow.transform, "input_chat", "输入消息", 0, 42);
        CreateButton(chatInputRow.transform, "btn_send_chat", "发送", ButtonColor, 86, 42);

        AddGameArtLibraries(game);
        SavePrefab(game, $"{ViewFolder}/Game.prefab");
    }

    private static void BuildGamePhasePrefabs()
    {
        BuildGamePlayPanelPrefab();
        BuildGameChallengePanelPrefab();
        BuildGameShowdownPanelPrefab();
        BuildGameFortunePanelPrefab();
        BuildGameResultPanelPrefab();
    }

    private static void BuildGamePlayPanelPrefab()
    {
        GameObject panel = CreatePhaseRoot("game_play_panel", false);
        GameObject cardPreview = CreatePanel(panel.transform, "panel_card_preview", new Color32(96, 26, 20, 230));
        SetAnchoredRect(cardPreview, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-250f, 240f), new Vector2(360f, 140f));
        AddVerticalLayout(cardPreview, 6, new RectOffset(16, 16, 10, 10), TextAnchor.UpperCenter, true, false);
        CreateText(cardPreview.transform, "txt_title", "出牌预览", 20, TextAlignmentOptions.Center, AccentColor);
        GameObject previewCards = CreatePanel(cardPreview.transform, "preview_cards", new Color32(50, 12, 10, 95));
        SetFixedHeight(previewCards, 86);
        AddHorizontalLayout(previewCards, 6, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleCenter);
        CreateText(cardPreview.transform, "txt_declaration", "声明：等待选择", 20, TextAlignmentOptions.Center, TextColor);

        GameObject timer = CreatePanel(panel.transform, "panel_timer", new Color32(92, 24, 20, 232));
        SetAnchoredRect(timer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(36f, 240f), new Vector2(104f, 140f));
        CreateText(timer.transform, "txt_timer", "--", 38, TextAlignmentOptions.Center, AccentColor);

        GameObject actions = CreatePanel(panel.transform, "panel_actions", new Color32(80, 21, 18, 235));
        SetAnchoredRect(actions, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(248f, 240f), new Vector2(286f, 140f));
        AddVerticalLayout(actions, 7, new RectOffset(14, 14, 10, 10), TextAnchor.UpperCenter, true, false);
        CreateButton(actions.transform, "btn_confirm_play", "确认出牌", AccentColor, -1, 42, new Color32(39, 20, 15, 255));
        CreateButton(actions.transform, "btn_clear_selection", "清空选择", ButtonMutedColor, -1, 38);
        CreateText(actions.transform, "txt_error", "请选择至少一张手牌", 16, TextAlignmentOptions.Center, MutedTextColor);

        SavePrefab(panel, $"{TemplateFolder}/game_play_panel.prefab");
    }

    private static void BuildGameChallengePanelPrefab()
    {
        GameObject panel = CreatePhaseRoot("game_challenge_panel", true);
        GameObject content = CreateMaskedContent(panel.transform, "panel_challenge", 1040, 560);
        AddVerticalLayout(content, 12, new RectOffset(26, 26, 22, 22), TextAnchor.UpperCenter, true, false);
        GameObject timer = CreatePanel(content.transform, "panel_timer", new Color32(58, 15, 13, 205));
        SetAnchoredRect(timer, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(108f, 58f));
        IgnoreLayout(timer);
        CreateText(timer.transform, "txt_timer", "--", 28, TextAlignmentOptions.Center, AccentColor);
        CreateText(content.transform, "txt_title", "选择质疑对象", 32, TextAlignmentOptions.Center, AccentColor);
        CreateText(content.transform, "txt_hint", "查看每名玩家的公开声明和明牌，再决定是否质疑", 19, TextAlignmentOptions.Center, MutedTextColor);
        CreateChallengeInfoRow(content.transform, "row_left", "左家");
        CreateChallengeInfoRow(content.transform, "row_top", "上家");
        CreateChallengeInfoRow(content.transform, "row_right", "右家");

        GameObject noChallengeRow = CreateHorizontalGroup(content.transform, "panel_no_challenge", 12);
        SetFixedHeight(noChallengeRow, 52);
        CreateButton(noChallengeRow.transform, "btn_no_challenge", "不质疑", ButtonMutedColor, 190, 48);
        CreateButton(noChallengeRow.transform, "btn_confirm_challenge", "确认选择", AccentColor, 220, 48, new Color32(39, 20, 15, 255));
        CreateText(content.transform, "txt_status", "请选择一个质疑结果", 19, TextAlignmentOptions.Center, TextColor);

        SavePrefab(panel, $"{TemplateFolder}/game_challenge_panel.prefab");
    }

    private static void CreateChallengeInfoRow(Transform parent, string name, string fallbackTitle)
    {
        GameObject row = CreatePanel(parent, name, new Color32(58, 15, 13, 185));
        SetFixedHeight(row, 108);

        CreateText(row.transform, "txt_player_name", fallbackTitle, 22, TextAlignmentOptions.MidlineLeft, TextColor);
        SetAnchoredRect(row.transform.Find("txt_player_name").gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 20f), new Vector2(160f, 36f));

        CreateText(row.transform, "txt_declaration", "声明牌：等待公开", 22, TextAlignmentOptions.MidlineLeft, AccentColor);
        SetAnchoredRect(row.transform.Find("txt_declaration").gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, -20f), new Vector2(220f, 36f));

        GameObject previewCards = CreatePanel(row.transform, "preview_cards", new Color32(45, 12, 10, 90));
        SetAnchoredRect(previewCards, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 0f), new Vector2(520f, 88f));
        AddHorizontalLayout(previewCards, 6, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleLeft);

        CreateButton(row.transform, "btn_challenge", "质疑", DangerColor, 120, 54);
        SetAnchoredRect(row.transform.Find("btn_challenge").gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(120f, 54f));
    }

    private static void BuildGameShowdownPanelPrefab()
    {
        GameObject panel = CreatePhaseRoot("game_showdown_panel", true);
        GameObject content = CreateMaskedContent(panel.transform, "panel_showdown", 1080, 610);
        content.GetComponent<Image>().color = new Color32(92, 24, 18, 190);

        GameObject glow = CreateImage(content.transform, "bg_center_glow", new Color32(232, 171, 84, 18), new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.84f), Vector2.zero, Vector2.zero);
        glow.GetComponent<Image>().raycastTarget = false;
        AddModalTimer(content.transform);

        CreateText(content.transform, "txt_title", "公示审判", 38, TextAlignmentOptions.Center, AccentColor);
        SetAnchoredRect(content.transform.Find("txt_title").gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(420f, 52f));

        GameObject targetCard = CreatePanel(content.transform, "panel_target_card", new Color32(78, 19, 16, 0));
        SetAnchoredRect(targetCard, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 10f), new Vector2(250f, 410f));
        GameObject targetPortrait = CreatePanel(targetCard.transform, "panel_target_portrait", new Color32(255, 236, 188, 0));
        SetAnchoredRect(targetPortrait, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(194f, 238f));
        GameObject targetCharacter = CreateImage(targetPortrait.transform, "img_target_character", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        targetCharacter.GetComponent<Image>().raycastTarget = false;
        CreateText(targetCard.transform, "txt_target_label", "被质疑", 20, TextAlignmentOptions.Center, MutedTextColor);
        SetAnchoredRect(targetCard.transform.Find("txt_target_label").gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(190f, 30f));
        CreateText(targetCard.transform, "txt_target_name", "-", 28, TextAlignmentOptions.Center, TextColor);
        SetAnchoredRect(targetCard.transform.Find("txt_target_name").gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(230f, 42f));
        CreateText(targetCard.transform, "txt_challengers", "质疑者：-", 20, TextAlignmentOptions.Center, MutedTextColor);
        SetAnchoredRect(targetCard.transform.Find("txt_challengers").gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(240f, 36f));

        GameObject revealStage = CreatePanel(content.transform, "panel_reveal_stage", new Color32(48, 12, 10, 0));
        SetAnchoredRect(revealStage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 50f), new Vector2(470f, 330f));
        CreateText(revealStage.transform, "txt_stage_label", "翻开的暗牌", 22, TextAlignmentOptions.Center, MutedTextColor);
        SetAnchoredRect(revealStage.transform.Find("txt_stage_label").gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(240f, 36f));
        GameObject cards = CreatePanel(revealStage.transform, "panel_revealed_cards", new Color32(30, 6, 5, 0));
        SetAnchoredRect(cards, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(420f, 188f));
        AddHorizontalLayout(cards, 16, new RectOffset(16, 16, 14, 14), TextAnchor.MiddleCenter);
        GameObject resultStamp = CreatePanel(revealStage.transform, "panel_result_stamp", new Color32(232, 171, 84, 235));
        SetAnchoredRect(resultStamp, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(170f, 58f));
        CreateText(resultStamp.transform, "txt_result", "等待翻牌", 30, TextAlignmentOptions.Center, new Color32(68, 17, 12, 255));

        GameObject eventList = CreatePanel(content.transform, "panel_event_list", new Color32(60, 15, 13, 0));
        SetAnchoredRect(eventList, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-54f, 76f), new Vector2(260f, 270f));
        AddVerticalLayout(eventList, 16, new RectOffset(10, 10, 20, 10), TextAnchor.UpperCenter, true, false);
        CreateText(eventList.transform, "txt_event_title", "本轮事件", 22, TextAlignmentOptions.Center, AccentColor);
        CreateText(eventList.transform, "txt_event_rows", "等待公示", 19, TextAlignmentOptions.TopLeft, TextColor);

        GameObject deltaGrid = CreatePanel(content.transform, "panel_delta_grid", new Color32(36, 9, 8, 0));
        SetAnchoredRect(deltaGrid, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 42f), new Vector2(760f, 96f));
        AddHorizontalLayout(deltaGrid, 14, new RectOffset(8, 8, 8, 8), TextAnchor.MiddleCenter);
        for (int index = 0; index < 4; index += 1)
        {
            CreateShowdownDeltaSlot(deltaGrid.transform, $"delta_slot_{index}");
        }

        SavePrefab(panel, $"{TemplateFolder}/game_showdown_panel.prefab");
    }

    private static void CreateShowdownDeltaSlot(Transform parent, string name)
    {
        GameObject slot = CreatePanel(parent, name, new Color32(80, 21, 18, 72));
        LayoutElement layoutElement = slot.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 176;
        layoutElement.preferredHeight = 78;
        CreateText(slot.transform, "txt_player", "-", 20, TextAlignmentOptions.Center, TextColor);
        SetAnchoredRect(slot.transform.Find("txt_player").gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(160f, 28f));
        CreateText(slot.transform, "txt_delta", "0", 28, TextAlignmentOptions.Center, MutedTextColor);
        SetAnchoredRect(slot.transform.Find("txt_delta").gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(160f, 34f));
    }

    private static void BuildGameFortunePanelPrefab()
    {
        GameObject panel = CreatePhaseRoot("game_fortune_panel", true);
        GameObject content = CreateMaskedContent(panel.transform, "panel_fortune", 780, 460);
        AddVerticalLayout(content, 14, new RectOffset(28, 28, 24, 24), TextAnchor.UpperCenter, true, false);
        AddModalTimer(content.transform);
        CreateText(content.transform, "txt_title", "气运抽取", 34, TextAlignmentOptions.Center, AccentColor);
        CreateText(content.transform, "txt_hint", "所见即所得：从自己的气运池中选择抽取数量", 20, TextAlignmentOptions.Center, MutedTextColor);
        GameObject pool = CreatePanel(content.transform, "panel_fortune_pool", new Color32(48, 12, 10, 130));
        SetFixedHeight(pool, 130);
        AddHorizontalLayout(pool, 10, new RectOffset(12, 12, 10, 10), TextAnchor.MiddleCenter);
        CreateText(pool.transform, "txt_pool_hint", "好运 / 霉运 token 区", 22, TextAlignmentOptions.Center, TextColor);
        GameObject controls = CreateHorizontalGroup(content.transform, "panel_draw_controls", 12);
        CreateButton(controls.transform, "btn_minus", "-", ButtonMutedColor, 64, 54);
        CreateText(controls.transform, "txt_draw_count", "2", 30, TextAlignmentOptions.Center, TextColor, 90, -1);
        CreateButton(controls.transform, "btn_plus", "+", ButtonMutedColor, 64, 54);
        CreateButton(content.transform, "btn_submit_draw", "开始抽取", AccentColor, 240, 56, new Color32(39, 20, 15, 255));
        CreateText(content.transform, "txt_result", "等待抽取", 20, TextAlignmentOptions.Center, MutedTextColor);

        SavePrefab(panel, $"{TemplateFolder}/game_fortune_panel.prefab");
    }

    private static void BuildGameResultPanelPrefab()
    {
        GameObject panel = CreatePhaseRoot("game_result_panel", true);
        GameObject content = CreateMaskedContent(panel.transform, "panel_result", 900, 560);
        AddVerticalLayout(content, 14, new RectOffset(28, 28, 24, 24), TextAnchor.UpperCenter, true, false);
        CreateText(content.transform, "txt_title", "最终结算", 36, TextAlignmentOptions.Center, AccentColor);
        GameObject ranking = CreatePanel(content.transform, "panel_ranking", new Color32(48, 12, 10, 130));
        SetFixedHeight(ranking, 330);
        AddVerticalLayout(ranking, 8, new RectOffset(12, 12, 10, 10), TextAnchor.UpperCenter, true, false);
        CreateText(ranking.transform, "txt_header", "名次  玩家  有效霉运  最终福运  溜走的3  称号", 22, TextAlignmentOptions.Center, TextColor);
        CreateText(ranking.transform, "txt_rows", "等待结算数据", 22, TextAlignmentOptions.Center, MutedTextColor);
        GameObject actions = CreateHorizontalGroup(content.transform, "panel_actions", 12);
        CreateButton(actions.transform, "btn_view_log", "本局记录", ButtonMutedColor, 180, 54);
        CreateButton(actions.transform, "btn_rematch", "再来一局", AccentColor, 180, 54, new Color32(39, 20, 15, 255));
        CreateButton(actions.transform, "btn_back_lobby", "返回大厅", DangerColor, 180, 54);

        SavePrefab(panel, $"{TemplateFolder}/game_result_panel.prefab");
    }

    private static void AddModalTimer(Transform parent)
    {
        GameObject timer = CreatePanel(parent, "panel_timer", new Color32(58, 15, 13, 205));
        SetAnchoredRect(timer, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(108f, 58f));
        IgnoreLayout(timer);
        CreateText(timer.transform, "txt_timer", "--", 28, TextAlignmentOptions.Center, AccentColor);
    }

    private static GameObject CreateGameSeatPanel(Transform parent, string name, string title)
    {
        GameObject panel = CreatePanel(parent, name, new Color32(75, 18, 14, 138));
        GameObject portrait = CreatePanel(panel.transform, "panel_portrait", new Color32(255, 236, 188, 0));
        SetAnchoredRect(portrait, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(158f, 158f));
        GameObject character = CreateImage(portrait.transform, "img_character", Color.white, new Vector2(0.00f, 0.00f), new Vector2(1.00f, 1.00f), Vector2.zero, Vector2.zero);
        character.GetComponent<Image>().raycastTarget = false;
        CreateText(panel.transform, "txt_name", title, 21, TextAlignmentOptions.MidlineRight, TextColor);
        SetAnchoredRect(panel.transform.Find("txt_name").gameObject, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(32f, -18f), new Vector2(190f, 36f));
        GameObject cardBack = CreateImage(panel.transform, "img_card_count_back", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Sprite(CardBackPath));
        SetAnchoredRect(cardBack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(42f, -18f), new Vector2(24f, 32f));
        cardBack.GetComponent<Image>().raycastTarget = false;
        CreateText(panel.transform, "txt_card_count", "0", 21, TextAlignmentOptions.MidlineLeft, TextColor);
        SetAnchoredRect(panel.transform.Find("txt_card_count").gameObject, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(60f, -18f), new Vector2(56f, 36f));
        CreateText(panel.transform, "txt_status", "等待中", 18, TextAlignmentOptions.Center, MutedTextColor);
        SetAnchoredRect(panel.transform.Find("txt_status").gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(220f, 32f));
        return panel;
    }

    private static void AddGameArtLibraries(GameObject root)
    {
        AddCharacterLibrary(root);
        AddCardLibrary(root);
    }

    private static void AddCharacterLibrary(GameObject root)
    {
        CharacterArtLibrary library = root.AddComponent<CharacterArtLibrary>();
        SerializedObject serializedObject = new SerializedObject(library);
        SerializedProperty entries = serializedObject.FindProperty("_entries");
        entries.arraySize = 2;

        for (int index = 0; index < 2; index += 1)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_characterId").stringValue = $"character_{index + 1}";
            entry.FindPropertyRelative("_sprite").objectReferenceValue = index == 0
                ? Sprite(CharacterOnePath)
                : Sprite(CharacterTwoPath);
            entry.FindPropertyRelative("_roomOffset").vector2Value = index == 0 ? new Vector2(0f, -10f) : new Vector2(-6f, -14f);
            entry.FindPropertyRelative("_roomScale").floatValue = index == 0 ? 1.14f : 1.18f;
            entry.FindPropertyRelative("_gameOffset").vector2Value = index == 0 ? new Vector2(12f, -4f) : new Vector2(0f, -4f);
            entry.FindPropertyRelative("_gameScale").floatValue = index == 0 ? 1.05f : 0.90f;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddCardLibrary(GameObject root)
    {
        CardArtLibrary library = root.AddComponent<CardArtLibrary>();
        SerializedObject serializedObject = new SerializedObject(library);
        serializedObject.FindProperty("_backSprite").objectReferenceValue = Sprite(CardBackPath);

        SerializedProperty entries = serializedObject.FindProperty("_entries");
        entries.arraySize = CardSuits.Length * 6;

        int entryIndex = 0;
        foreach (string suit in CardSuits)
        {
            for (int rank = 2; rank <= 7; rank += 1)
            {
                string cardName = $"{suit}_{rank}";
                SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
                entry.FindPropertyRelative("_cardName").stringValue = cardName;
                entry.FindPropertyRelative("_sprite").objectReferenceValue = Sprite($"{CardArtFolder}/{cardName}.png");
                entryIndex += 1;
            }
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePhaseRoot(string name, bool masked)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        if (masked)
        {
            CreateImage(root.transform, "bg_dim", new Color(0, 0, 0, 0.48f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject blocker = CreateButton(root.transform, "btn_blocker", "", new Color(1, 1, 1, 0), -1, -1);
            SetRect(blocker, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        return root;
    }

    private static GameObject CreateMaskedContent(Transform parent, string name, float width, float height)
    {
        GameObject content = CreatePanel(parent, name, new Color32(96, 26, 20, 245));
        SetRect(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        SetSize(content, width, height);
        return content;
    }

    private static void BuildCardPrefab()
    {
        GameObject card = CreatePanel(null, "card", new Color32(110, 43, 26, 255));
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        SetSize(card, 96, 142);

        GameObject shadow = CreateImage(card.transform, "bg_shadow", new Color32(35, 8, 4, 90), new Vector2(0.05f, -0.03f), new Vector2(1.05f, 0.97f), Vector2.zero, Vector2.zero);
        shadow.transform.SetAsFirstSibling();
        shadow.GetComponent<Image>().raycastTarget = false;

        GameObject face = CreateImage(card.transform, "img_card_face", Color.white, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f), Vector2.zero, Vector2.zero, Sprite($"{CardArtFolder}/spade_3.png"));
        face.GetComponent<Image>().raycastTarget = false;

        GameObject marker = CreatePanel(card.transform, "txt_face_up_marker", new Color32(244, 190, 86, 230));
        SetRect(marker, new Vector2(0.58f, 0.78f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
        CreateText(marker.transform, "txt_label", "明", 18, TextAlignmentOptions.Center, new Color32(39, 20, 15, 255));
        marker.transform.SetAsLastSibling();
        marker.SetActive(false);

        GameObject cardName = CreateText(card.transform, "card_name", "3", 22, TextAlignmentOptions.Center, new Color32(42, 50, 58, 255));
        SetRect(card.transform.Find("card_name").gameObject, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f), Vector2.zero, Vector2.zero);
        cardName.SetActive(false);

        GameObject button = CreateImage(card.transform, "btn", new Color(1, 1, 1, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button cardButton = button.AddComponent<Button>();
        cardButton.targetGraphic = button.GetComponent<Image>();
        button.transform.SetAsLastSibling();

        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 96;
        layoutElement.preferredHeight = 142;

        SavePrefab(card, $"{TemplateFolder}/card.prefab");
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

    private static Sprite Sprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreateImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Sprite sprite = null)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
        {
            imageObject.transform.SetParent(parent, false);
        }

        SetRect(imageObject, anchorMin, anchorMax, pivot, anchoredPosition);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.preserveAspect = sprite != null && name != "bg";
        return imageObject;
    }

    private static GameObject CreateButton(
        Transform parent,
        string name,
        string label,
        Color color,
        float width,
        float height,
        Color? textColor = null)
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

        CreateText(buttonObject.transform, "txt_label", label, 22, TextAlignmentOptions.Center, textColor ?? TextColor);
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
        Offset(textArea, 14, 0, -14, 0);

        GameObject placeholderText = CreateText(textArea.transform, "txt_placeholder", placeholder, 20, TextAlignmentOptions.Left, new Color32(184, 130, 102, 255));
        GameObject inputText = CreateText(textArea.transform, "txt_input", "", 20, TextAlignmentOptions.Left, TextColor);
        ConfigureInputText(placeholderText);
        ConfigureInputText(inputText);
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.caretWidth = 2;
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

    private static void ConfigureInputText(GameObject textObject)
    {
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.horizontalAlignment = HorizontalAlignmentOptions.Left;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.textWrappingMode = TextWrappingModes.NoWrap;
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

    private static void IgnoreLayout(GameObject target)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
    }

    private static void SetSize(GameObject target, float width, float height)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void SetAnchoredRect(GameObject target, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
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
