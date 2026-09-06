#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class LobbyRoomPrefabBuilder
{
    private const int ActiveCharacterCount = 2;
    private const string ViewFolder = "Assets/Prefabs/view";
    private const string TemplateFolder = "Assets/Prefabs/template";
    private const string UiFontAssetPath = "Assets/Fonts/WN-Sans-SC-Medium SDF.asset";
    private const string LobbyBackgroundPath = "Assets/Arts/lobby/bg_1.png";
    private const string CharacterOnePath = "Assets/Arts/character_1.png";
    private const string CharacterTwoPath = "Assets/Arts/chrarcter_2.png";

    private static readonly Color CardRed = new Color32(120, 28, 24, 236);
    private static readonly Color CardRedDark = new Color32(78, 21, 20, 242);
    private static readonly Color CardRedLight = new Color32(163, 45, 34, 230);
    private static readonly Color Gold = new Color32(244, 190, 86, 255);
    private static readonly Color GoldMuted = new Color32(211, 154, 68, 255);
    private static readonly Color Cream = new Color32(255, 239, 205, 255);
    private static readonly Color MutedCream = new Color32(235, 199, 151, 255);
    private static readonly Color Ink = new Color32(53, 24, 21, 255);
    private static readonly Color Danger = new Color32(143, 37, 42, 255);

    private static TMP_FontAsset _uiFontAsset;

    [MenuItem("Card Game/Build Lobby Room UI Prefabs")]
    public static void BuildLobbyRoomPrefabs()
    {
        EnsureFolders();
        EnsureSpriteImportSettings();
        _uiFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);

        BuildLobbyPrefab();
        BuildRoomPrefab();
        BuildRoomItemPrefab();
        BuildBlockingOverlayPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Lobby and room UI prefabs rebuilt.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "view");
        EnsureFolder("Assets/Prefabs", "template");
    }

    private static void EnsureSpriteImportSettings()
    {
        EnsureSpriteImportSettings(LobbyBackgroundPath, 4096);
        EnsureSpriteImportSettings(CharacterOnePath, 2048);
        EnsureSpriteImportSettings(CharacterTwoPath, 2048);
    }

    private static void EnsureSpriteImportSettings(string assetPath, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Cannot find texture importer at {assetPath}.");
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

    private static void BuildLobbyPrefab()
    {
        GameObject lobby = CreateViewRoot("Lobby");
        AddCharacterLibrary(lobby);
        Transform root = lobby.transform.Find("root");

        CreateImage(root, "bg", Color.white, Vector2.zero, Vector2.one, Sprite(LobbyBackgroundPath));
        CreateImage(root, "bg_warm_vignette", new Color32(74, 12, 9, 88), Vector2.zero, Vector2.one);

        GameObject titleBand = CreatePanel(root, "panel_title_band", new Color32(86, 20, 18, 170));
        SetRect(titleBand, new Vector2(0, 0.86f), new Vector2(1, 1));
        CreateText(titleBand.transform, "txt_title", "溜小3", 58, TextAlignmentOptions.Center, Gold);
        SetRect(titleBand.transform.Find("txt_title").gameObject, Vector2.zero, Vector2.one);

        GameObject profile = CreatePanel(root, "panel_profile", new Color32(96, 27, 24, 218));
        SetRect(profile, new Vector2(0.045f, 0.10f), new Vector2(0.34f, 0.84f));
        AddVerticalLayout(profile, 14, new RectOffset(22, 22, 20, 20), TextAnchor.UpperCenter, true, false);

        GameObject showcase = CreatePanel(profile.transform, "panel_character_showcase", new Color32(255, 232, 184, 52));
        SetFixedHeight(showcase, 445);
        CreateImage(showcase.transform, "img_character", Color.white, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.94f), Sprite(CharacterOnePath));
        CreateText(showcase.transform, "txt_character_name", "福童", 30, TextAlignmentOptions.Center, Cream);
        SetRect(showcase.transform.Find("txt_character_name").gameObject, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.13f));

        GameObject picker = CreatePanel(profile.transform, "panel_character_picker", new Color(1, 1, 1, 0));
        SetFixedHeight(picker, 160);
        AddHorizontalLayout(picker, 10, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
        for (int index = 0; index < ActiveCharacterCount; index += 1)
        {
            GameObject option = CreateButton(picker.transform, $"btn_character{index}", "", CardRed, 0, 140);
            SetFlexible(option, 1, 0);
            Sprite sprite = index % 2 == 0 ? Sprite(CharacterOnePath) : Sprite(CharacterTwoPath);
            CreateImage(option.transform, "img_portrait", Color.white, new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.94f), sprite);
            TMP_Text optionLabel = option.transform.Find("txt_label").GetComponent<TMP_Text>();
            optionLabel.text = CharacterDisplayName(index);
            optionLabel.fontSize = 18;
            SetRect(option.transform.Find("txt_label").gameObject, new Vector2(0, 0.02f), new Vector2(1, 0.25f));
        }

        GameObject actions = CreatePanel(root, "panel_actions", new Color32(85, 22, 20, 224));
        SetRect(actions, new Vector2(0.365f, 0.10f), new Vector2(0.62f, 0.84f));
        AddVerticalLayout(actions, 18, new RectOffset(22, 22, 20, 20), TextAnchor.UpperCenter, true, false);

        GameObject playerInfo = CreatePanel(actions.transform, "panel_player_info", CardRedDark);
        SetFixedHeight(playerInfo, 150);
        AddVerticalLayout(playerInfo, 8, new RectOffset(18, 18, 14, 14), TextAnchor.UpperLeft, true, false);
        CreateText(playerInfo.transform, "txt_player_name", "player_A7K2", 30, TextAlignmentOptions.Left, Cream);
        CreateText(playerInfo.transform, "txt_player_id", "ID: ------", 18, TextAlignmentOptions.Left, MutedCream);
        CreateText(playerInfo.transform, "txt_connection_status", "连接中...", 18, TextAlignmentOptions.Left, MutedCream);
        CreateButton(playerInfo.transform, "btn_profile", "个人信息", CardRedLight, -1, 42);

        GameObject matchmaking = CreatePanel(actions.transform, "panel_matchmaking", CardRedDark);
        SetFixedHeight(matchmaking, 145);
        AddVerticalLayout(matchmaking, 10, new RectOffset(18, 18, 16, 16), TextAnchor.UpperCenter, true, false);
        CreateButton(matchmaking.transform, "btn_matchmaking", "快速匹配", GoldMuted, -1, 58, Ink);
        CreateText(matchmaking.transform, "txt_matchmaking_status", "空闲", 20, TextAlignmentOptions.Center, MutedCream);

        GameObject joinRoom = CreatePanel(actions.transform, "panel_join_room", CardRedDark);
        SetFixedHeight(joinRoom, 145);
        AddVerticalLayout(joinRoom, 10, new RectOffset(18, 18, 16, 16), TextAnchor.UpperLeft, true, false);
        CreateText(joinRoom.transform, "txt_join_title", "加入房间", 22, TextAlignmentOptions.Left, Cream);
        GameObject joinRow = CreateHorizontalGroup(joinRoom.transform, "panel_join_row", 10, 52);
        CreateInput(joinRow.transform, "input_room_id", "输入房间号", 0, 50);
        CreateButton(joinRow.transform, "btn_join_room", "加入", GoldMuted, 92, 50, Ink);

        GameObject createRoom = CreatePanel(actions.transform, "panel_create_room", CardRedDark);
        SetFixedHeight(createRoom, 165);
        AddVerticalLayout(createRoom, 10, new RectOffset(18, 18, 16, 16), TextAnchor.UpperLeft, true, false);
        CreateText(createRoom.transform, "txt_create_title", "创建房间", 22, TextAlignmentOptions.Left, Cream);
        CreateInput(createRoom.transform, "input_room_name", "房间名称", -1, 46);
        CreateButton(createRoom.transform, "btn_create_room", "开一桌", GoldMuted, -1, 50, Ink);

        GameObject roomList = CreatePanel(root, "panel_room_list", new Color32(76, 22, 20, 226));
        SetRect(roomList, new Vector2(0.645f, 0.10f), new Vector2(0.955f, 0.84f));
        AddVerticalLayout(roomList, 12, new RectOffset(20, 20, 18, 20), TextAnchor.UpperLeft, true, false);

        GameObject roomHeader = CreatePanel(roomList.transform, "panel_room_list_header", CardRedDark);
        SetFixedHeight(roomHeader, 66);
        AddHorizontalLayout(roomHeader, 12, new RectOffset(14, 14, 8, 8), TextAnchor.MiddleCenter);
        CreateText(roomHeader.transform, "txt_title", "房间列表", 28, TextAlignmentOptions.MidlineLeft, Cream);
        CreateButton(roomHeader.transform, "btn_refresh", "刷新", CardRedLight, 110, 48);

        GameObject scroll = CreateScrollView(roomList.transform, "scroll_room_list", new Color32(59, 16, 14, 180));
        SetFlexible(scroll, 1, 1);
        CreateText(roomList.transform, "txt_empty_hint", "暂无可加入房间", 21, TextAlignmentOptions.Center, MutedCream);

        SavePrefab(lobby, $"{ViewFolder}/Lobby.prefab");
    }

    private static void BuildRoomPrefab()
    {
        GameObject room = CreateViewRoot("Room");
        AddCharacterLibrary(room);
        Transform root = room.transform.Find("root");

        CreateImage(root, "bg", Color.white, Vector2.zero, Vector2.one, Sprite(LobbyBackgroundPath));
        CreateImage(root, "bg_dim", new Color32(58, 8, 7, 118), Vector2.zero, Vector2.one);

        GameObject header = CreatePanel(root, "panel_header", new Color32(82, 20, 18, 225));
        SetRect(header, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.95f));
        AddHorizontalLayout(header, 14, new RectOffset(20, 20, 10, 10), TextAnchor.MiddleCenter);
        CreateText(header.transform, "txt_room_name", "房间名称", 30, TextAlignmentOptions.MidlineLeft, Cream);
        CreateText(header.transform, "txt_room_id", "房间号 1234", 22, TextAlignmentOptions.MidlineLeft, MutedCream, 210, -1);
        CreateButton(header.transform, "btn_leave", "离开", Danger, 110, 52);

        GameObject players = CreatePanel(root, "panel_players", new Color32(105, 28, 23, 190));
        SetRect(players, new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.80f));
        AddHorizontalLayout(players, 18, new RectOffset(20, 20, 18, 18), TextAnchor.MiddleCenter, true);
        for (int index = 0; index < 4; index += 1)
        {
            GameObject slot = CreateRoomSlot(players.transform, $"slot_player{index}", index);
            SetEqualSlotLayout(slot);
        }

        GameObject chat = CreatePanel(root, "panel_chat", new Color32(66, 18, 16, 222));
        SetRect(chat, new Vector2(0.04f, 0.08f), new Vector2(0.54f, 0.31f));
        AddVerticalLayout(chat, 8, new RectOffset(16, 16, 12, 12), TextAnchor.UpperLeft, true, false);
        CreateText(chat.transform, "txt_chat_title", "房间聊天", 22, TextAlignmentOptions.Left, Gold);
        GameObject scroll = CreateScrollView(chat.transform, "scroll_chat", new Color32(45, 12, 11, 135));
        SetFlexible(scroll, 1, 1);
        GameObject inputRow = CreateHorizontalGroup(chat.transform, "panel_chat_input", 10, 50);
        CreateInput(inputRow.transform, "input_chat", "说点什么...", 0, 48);
        CreateButton(inputRow.transform, "btn_send_chat", "发送", GoldMuted, 92, 48, Ink);

        GameObject controls = CreatePanel(root, "panel_controls", new Color32(82, 20, 18, 225));
        SetRect(controls, new Vector2(0.58f, 0.08f), new Vector2(0.96f, 0.22f));
        AddHorizontalLayout(controls, 14, new RectOffset(18, 18, 14, 14), TextAnchor.MiddleCenter);
        CreateButton(controls.transform, "btn_ready", "准备", GoldMuted, 170, 56, Ink);
        CreateButton(controls.transform, "btn_add_ai", "添加AI", CardRedLight, 160, 56);
        CreateButton(controls.transform, "btn_start_game", "开始游戏", GoldMuted, 190, 56, Ink);

        CreateText(root, "txt_room_status", "等待玩家加入", 24, TextAlignmentOptions.Center, Cream);
        SetRect(root.Find("txt_room_status").gameObject, new Vector2(0.58f, 0.24f), new Vector2(0.96f, 0.31f));

        SavePrefab(room, $"{ViewFolder}/Room.prefab");
    }

    private static GameObject CreateRoomSlot(Transform parent, string name, int seatIndex)
    {
        GameObject slot = CreatePanel(parent, name, new Color32(255, 234, 196, 52));
        CreateText(slot.transform, "txt_seat", $"座位 {seatIndex + 1}", 22, TextAlignmentOptions.Center, Gold);
        SetRect(slot.transform.Find("txt_seat").gameObject, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.98f));
        CreateImage(slot.transform, "img_character", new Color32(88, 34, 30, 180), new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.84f));
        CreateText(slot.transform, "txt_name", "等待玩家加入", 22, TextAlignmentOptions.Center, Cream);
        SetRect(slot.transform.Find("txt_name").gameObject, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.28f));
        CreateText(slot.transform, "txt_player_type", "-", 18, TextAlignmentOptions.Center, MutedCream);
        SetRect(slot.transform.Find("txt_player_type").gameObject, new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.17f));
        CreateText(slot.transform, "txt_ready", "-", 20, TextAlignmentOptions.Center, Gold);
        SetRect(slot.transform.Find("txt_ready").gameObject, new Vector2(0.50f, 0.08f), new Vector2(0.92f, 0.17f));
        return slot;
    }

    private static void BuildRoomItemPrefab()
    {
        GameObject item = CreatePanel(null, "room_item", new Color32(113, 31, 25, 232));
        SetRect(item, new Vector2(0, 1), new Vector2(1, 1));
        SetSize(item, 0, 88);
        AddHorizontalLayout(item, 12, new RectOffset(14, 14, 10, 10), TextAnchor.MiddleCenter);
        CreateText(item.transform, "txt_room_name", "房间名称", 23, TextAlignmentOptions.MidlineLeft, Cream);
        CreateText(item.transform, "txt_room_id", "1234", 21, TextAlignmentOptions.MidlineLeft, MutedCream, 90, -1);
        CreateText(item.transform, "txt_owner_name", "owner", 19, TextAlignmentOptions.MidlineLeft, MutedCream, 140, -1);
        CreateText(item.transform, "txt_player_count", "1/4", 22, TextAlignmentOptions.Center, Gold, 70, -1);
        CreateButton(item.transform, "btn_join", "加入", GoldMuted, 90, 48, Ink);

        SavePrefab(item, $"{TemplateFolder}/room_item.prefab");
    }

    private static void BuildBlockingOverlayPrefab()
    {
        GameObject overlay = CreatePanel(null, "blocking_overlay", new Color(0, 0, 0, 0));
        SetRect(overlay, Vector2.zero, Vector2.one);
        CreateImage(overlay.transform, "bg_dim", new Color(0, 0, 0, 0.58f), Vector2.zero, Vector2.one);
        GameObject blocker = CreateButton(overlay.transform, "btn_blocker", "", new Color(1, 1, 1, 0), -1, -1);
        SetRect(blocker, Vector2.zero, Vector2.one);

        GameObject panel = CreatePanel(overlay.transform, "panel_message", new Color32(92, 24, 22, 248));
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        SetSize(panel, 460, 240);
        AddVerticalLayout(panel, 16, new RectOffset(28, 28, 24, 24), TextAnchor.MiddleCenter, true, false);
        CreateText(panel.transform, "txt_title", "匹配中", 32, TextAlignmentOptions.Center, Gold);
        CreateText(panel.transform, "txt_message", "等待玩家 1/4", 22, TextAlignmentOptions.Center, Cream);
        CreateButton(panel.transform, "btn_cancel", "取消匹配", Danger, 190, 52);

        SavePrefab(overlay, $"{TemplateFolder}/blocking_overlay.prefab");
    }

    private static void AddCharacterLibrary(GameObject root)
    {
        CharacterArtLibrary library = root.AddComponent<CharacterArtLibrary>();
        SerializedObject serializedObject = new SerializedObject(library);
        SerializedProperty entries = serializedObject.FindProperty("_entries");
        entries.arraySize = ActiveCharacterCount;

        for (int index = 0; index < ActiveCharacterCount; index += 1)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_characterId").stringValue = $"character_{index + 1}";
            entry.FindPropertyRelative("_sprite").objectReferenceValue = index % 2 == 0
                ? Sprite(CharacterOnePath)
                : Sprite(CharacterTwoPath);
            entry.FindPropertyRelative("_roomOffset").vector2Value = index == 1 ? new Vector2(12f, 0f) : Vector2.zero;
            entry.FindPropertyRelative("_roomScale").floatValue = 1f;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string CharacterDisplayName(int index)
    {
        switch (index)
        {
            case 0:
                return "福童";
            case 1:
                return "沪上阿姨";
            default:
                return $"角色{index + 1}";
        }
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
        SetRect(root, Vector2.zero, Vector2.one);
        return view;
    }

    private static Sprite Sprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        return CreateImage(parent, name, color, Vector2.zero, Vector2.one);
    }

    private static GameObject CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite = null)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
        {
            imageObject.transform.SetParent(parent, false);
        }

        SetRect(imageObject, anchorMin, anchorMax);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.preserveAspect = sprite != null && name != "bg";
        return imageObject;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color color, float width, float height, Color? textColor = null)
    {
        GameObject buttonObject = CreateImage(parent, name, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
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

        CreateText(buttonObject.transform, "txt_label", label, 22, TextAlignmentOptions.Center, textColor ?? Cream);
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
        SetRect(textObject, Vector2.zero, Vector2.one);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        if (_uiFontAsset != null)
        {
            tmp.font = _uiFontAsset;
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
        GameObject input = CreateImage(parent, name, new Color32(55, 15, 14, 230), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        TMP_InputField inputField = input.AddComponent<TMP_InputField>();
        SetSize(input, Mathf.Max(width, 0), height);

        GameObject textArea = new GameObject("text_area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(input.transform, false);
        SetRect(textArea, Vector2.zero, Vector2.one);
        Offset(textArea, 14, 0, -14, 0);

        GameObject placeholderText = CreateText(textArea.transform, "txt_placeholder", placeholder, 20, TextAlignmentOptions.Left, new Color32(184, 130, 102, 255));
        GameObject inputText = CreateText(textArea.transform, "txt_input", "", 20, TextAlignmentOptions.Left, Cream);
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

    private static GameObject CreateScrollView(Transform parent, string name, Color color)
    {
        GameObject scroll = CreateImage(parent, name, color, Vector2.zero, Vector2.one);
        ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreateImage(scroll.transform, "viewport", new Color(1, 1, 1, 0.02f), Vector2.zero, Vector2.one);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        SetRect(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
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

    private static GameObject CreateHorizontalGroup(Transform parent, string name, float spacing, float height)
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
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 1;
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
        AddHorizontalLayout(target, spacing, padding, alignment, false);
    }

    private static void AddHorizontalLayout(GameObject target, float spacing, RectOffset padding, TextAnchor alignment, bool forceExpandWidth)
    {
        HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = forceExpandWidth;
        layout.childForceExpandHeight = false;
    }

    private static void SetFlexible(GameObject target, float width, float height)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = width;
        layoutElement.flexibleHeight = height;
    }

    private static void SetEqualSlotLayout(GameObject target)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 0;
        layoutElement.flexibleWidth = 1;
        layoutElement.flexibleHeight = 1;
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

    private static void SetRect(GameObject target, Vector2 anchorMin, Vector2 anchorMax)
    {
        SetRect(target, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
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

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
#endif
