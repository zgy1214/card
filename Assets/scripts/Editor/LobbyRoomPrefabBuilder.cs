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
    private const string LobbyBackgroundPath = "Assets/Arts/Lobby/background.png";
    private const string LobbyTitlePath = "Assets/Arts/Start/title.png";
    private const string LobbyPreviousPath = "Assets/Arts/Lobby/character_previous.png";
    private const string LobbyNextPath = "Assets/Arts/Lobby/character_next.png";
    private const string LobbyEditNamePath = "Assets/Arts/Lobby/edit_name.png";
    private const string RoomBackgroundPath = "Assets/Arts/Room/States/Ready/layer_hash_071039.png";
    private const string RoomPanelPath = "Assets/Arts/Room/States/Ready/panel_room.png";
    private const string RoomChatPanelPath = "Assets/Arts/Room/States/Ready/panel_chat.png";
    private const string RoomSlotPanelPath = "Assets/Arts/Room/States/Ready/panel_slot.png";
    private const string RoomBackButtonPath = "Assets/Arts/Room/States/Ready/button_back.png";
    private const string RoomHomeButtonPath = "Assets/Arts/Room/States/Ready/button_home.png";
    private const string RoomSendButtonPath = "Assets/Arts/Room/States/Ready/button_send.png";
    private const string RoomCharacterOnePath = "Assets/Arts/Room/States/Ready/character_art_1.png";
    private const string RoomCharacterTwoPath = "Assets/Arts/Room/States/Ready/character_art_2.png";
    private const string RoomSlotShadowOnePath = "Assets/Arts/Room/States/Ready/character_slot_shadow_1.png";
    private const string RoomSlotShadowTwoPath = "Assets/Arts/Room/States/Ready/character_slot_shadow_2.png";
    private const string RoomSlotShadowThreePath = "Assets/Arts/Room/States/Ready/character_slot_shadow_3.png";
    private const string RoomSlotShadowFourPath = "Assets/Arts/Room/States/Ready/character_slot_shadow_4.png";

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
        EnsureSpriteImportSettings(LobbyTitlePath, 2048);
        EnsureSpriteImportSettings(LobbyPreviousPath, 256);
        EnsureSpriteImportSettings(LobbyNextPath, 256);
        EnsureSpriteImportSettings(LobbyEditNamePath, 256);
        EnsureSpriteImportSettings(RoomBackgroundPath, 4096);
        EnsureSpriteImportSettings(RoomPanelPath, 256);
        EnsureSpriteImportSettings(RoomChatPanelPath, 512);
        EnsureSpriteImportSettings(RoomSlotPanelPath, 256);
        EnsureSpriteImportSettings(RoomBackButtonPath, 256);
        EnsureSpriteImportSettings(RoomHomeButtonPath, 128);
        EnsureSpriteImportSettings(RoomSendButtonPath, 128);
        EnsureSpriteImportSettings(RoomCharacterOnePath, 1024);
        EnsureSpriteImportSettings(RoomCharacterTwoPath, 1024);
        EnsureSpriteImportSettings(RoomSlotShadowOnePath, 512);
        EnsureSpriteImportSettings(RoomSlotShadowTwoPath, 512);
        EnsureSpriteImportSettings(RoomSlotShadowThreePath, 512);
        EnsureSpriteImportSettings(RoomSlotShadowFourPath, 512);
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

        CreateImage(root, "image_background", Color.white, Vector2.zero, Vector2.one, Sprite(LobbyBackgroundPath));

        GameObject characterStage = CreateGroup(root, "group_character_stage");
        SetRect(characterStage, new Vector2(0.02f, 0.05f), new Vector2(0.53f, 0.95f));
        CreateImage(characterStage.transform, "image_character", Color.white, new Vector2(0.10f, 0.02f), new Vector2(0.92f, 0.90f), Sprite(RoomCharacterOnePath));
        GameObject previous = CreateButton(characterStage.transform, "btn_character_previous", "", Color.white, 72, 88, Color.white);
        previous.GetComponent<Image>().sprite = Sprite(LobbyPreviousPath);
        previous.GetComponent<Image>().preserveAspect = true;
        previous.transform.Find("txt_label").gameObject.SetActive(false);
        SetRect(previous, new Vector2(0.02f, 0.42f), new Vector2(0.02f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero);
        GameObject next = CreateButton(characterStage.transform, "btn_character_next", "", Color.white, 72, 88, Color.white);
        next.GetComponent<Image>().sprite = Sprite(LobbyNextPath);
        next.GetComponent<Image>().preserveAspect = true;
        next.transform.Find("txt_label").gameObject.SetActive(false);
        SetRect(next, new Vector2(0.96f, 0.42f), new Vector2(0.96f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero);

        GameObject identity = CreateGroup(characterStage.transform, "group_player_identity");
        SetRect(identity, new Vector2(0.28f, 0.91f), new Vector2(0.72f, 0.99f));
        CreateText(identity.transform, "txt_player_name", "player_A7K2", 30, TextAlignmentOptions.MidlineRight, CardRedDark);
        SetRect(identity.transform.Find("txt_player_name").gameObject, new Vector2(0.02f, 0), new Vector2(0.78f, 1));
        GameObject editName = CreateButton(identity.transform, "btn_edit_name", "", Color.white, 48, 48, CardRedDark);
        editName.GetComponent<Image>().sprite = Sprite(LobbyEditNamePath);
        editName.GetComponent<Image>().preserveAspect = true;
        editName.transform.Find("txt_label").gameObject.SetActive(false);
        SetRect(identity.transform.Find("btn_edit_name").gameObject, new Vector2(0.82f, 0.5f), new Vector2(0.82f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        GameObject nameInput = CreateInput(identity.transform, "input_player_name", "输入昵称", 280, 48);
        SetRect(nameInput, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        nameInput.SetActive(false);

        GameObject logo = CreateImage(root, "image_logo", Color.white, new Vector2(0.51f, 0.62f), new Vector2(0.94f, 0.94f), Sprite(LobbyTitlePath));
        SetRaycastTarget(logo, false);

        GameObject topActions = CreateGroup(root, "group_top_actions");
        SetRect(topActions, new Vector2(0.87f, 0.88f), new Vector2(0.98f, 0.98f));
        CreateButton(topActions.transform, "btn_help", "?", new Color(1, 1, 1, 0), 64, 64, CardRedDark);
        GameObject settings = CreateButton(topActions.transform, "btn_settings", "⚙", new Color(1, 1, 1, 0), 64, 64, CardRedDark);
        SetRect(settings, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(66, 0));

        GameObject actions = CreateGroup(root, "group_main_actions");
        SetRect(actions, new Vector2(0.51f, 0.18f), new Vector2(0.95f, 0.58f));
        GameObject join = CreateButton(actions.transform, "btn_join_room", "加入房间", new Color32(247, 190, 45, 255), 300, 105, CardRedDark);
        SetRect(join, new Vector2(0.02f, 0.56f), new Vector2(0.46f, 0.90f));
        GameObject create = CreateButton(actions.transform, "btn_create_room", "创建房间", new Color32(52, 155, 229, 255), 300, 105, Color.white);
        SetRect(create, new Vector2(0.54f, 0.56f), new Vector2(0.98f, 0.90f));
        GameObject matchmaking = CreateButton(actions.transform, "btn_matchmaking", "随机匹配", new Color32(150, 65, 211, 255), 300, 105, Color.white);
        SetRect(matchmaking, new Vector2(0.02f, 0.08f), new Vector2(0.46f, 0.42f));
        GameObject rules = CreateButton(actions.transform, "btn_rules", "规则说明", new Color32(245, 90, 74, 255), 300, 105, Color.white);
        SetRect(rules, new Vector2(0.54f, 0.08f), new Vector2(0.98f, 0.42f));

        CreateText(root, "txt_matchmaking_status", "", 18, TextAlignmentOptions.Center, CardRedDark);
        root.Find("txt_matchmaking_status").gameObject.SetActive(false);
        SavePrefab(lobby, $"{ViewFolder}/Lobby.prefab");
    }
    private static void BuildRoomPrefab()
    {
        GameObject room = CreateViewRoot("Room");
        AddCharacterLibrary(room);
        Transform root = room.transform.Find("root");

        CreateImage(root, "image_background", Color.white, Vector2.zero, Vector2.one, Sprite(RoomBackgroundPath));

        GameObject header = CreateGroup(root, "group_header");
        SetRect(header, new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.97f));
        CreateText(header.transform, "txt_room_name", "房间名称", 32, TextAlignmentOptions.MidlineLeft, Cream);
        SetRect(header.transform.Find("txt_room_name").gameObject, new Vector2(0.02f, 0), new Vector2(0.34f, 1));
        CreateText(header.transform, "txt_room_id", "房间号 1234", 22, TextAlignmentOptions.Center, MutedCream);
        SetRect(header.transform.Find("txt_room_id").gameObject, new Vector2(0.35f, 0), new Vector2(0.65f, 1));
        CreateButton(header.transform, "btn_leave", "离开", new Color32(222, 76, 63, 255), 120, 54);
        SetRect(header.transform.Find("btn_leave").gameObject, new Vector2(0.86f, 0.5f), new Vector2(0.86f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        SetButtonSprite(header.transform.Find("btn_leave").gameObject, Sprite(RoomBackButtonPath));

        GameObject roomPanel = CreateSlicedImage(root, "panel_room", Color.white, new Vector2(0.045f, 0.17f), new Vector2(0.74f, 0.85f), Sprite(RoomPanelPath));
        SetRaycastTarget(roomPanel, false);
        GameObject seats = CreateGroup(root, "group_player_seats");
        SetRect(seats, new Vector2(0.09f, 0.25f), new Vector2(0.70f, 0.80f));
        AddHorizontalLayout(seats, 18, new RectOffset(12, 12, 12, 12), TextAnchor.MiddleCenter, true);
        for (int index = 0; index < 4; index += 1)
        {
            GameObject slot = CreateRoomSlot(seats.transform, $"slot_player{index}", index);
            SetEqualSlotLayout(slot);
        }

        CreateText(root, "txt_room_status", "等待玩家加入", 24, TextAlignmentOptions.Center, Cream);
        SetRect(root.Find("txt_room_status").gameObject, new Vector2(0.16f, 0.18f), new Vector2(0.63f, 0.24f));

        GameObject bottom = CreateGroup(root, "group_bottom");
        SetRect(bottom, new Vector2(0.045f, 0.04f), new Vector2(0.955f, 0.15f));

        GameObject chat = CreateSlicedImage(bottom.transform, "group_chat", Color.white, new Vector2(0, 0), new Vector2(0.68f, 1), Sprite(RoomChatPanelPath));
        AddVerticalLayout(chat, 6, new RectOffset(18, 18, 10, 10), TextAnchor.UpperLeft, true, false);
        CreateText(chat.transform, "txt_chat_title", "房间聊天", 22, TextAlignmentOptions.Left, Gold);
        GameObject scroll = CreateScrollView(chat.transform, "scroll_chat", new Color32(45, 12, 11, 135));
        SetFlexible(scroll, 1, 1);
        GameObject inputRow = CreateHorizontalGroup(chat.transform, "group_chat_input", 8, 42);
        CreateInput(inputRow.transform, "input_chat", "说点什么...", 0, 48);
        CreateButton(inputRow.transform, "btn_send_chat", "", Color.white, 48, 48);

        SetButtonSprite(inputRow.transform.Find("btn_send_chat").gameObject, Sprite(RoomSendButtonPath));
        GameObject controls = CreateSlicedImage(bottom.transform, "group_room_actions", Color.white, new Vector2(0.71f, 0), new Vector2(1, 1), Sprite(RoomPanelPath));
        AddHorizontalLayout(controls, 10, new RectOffset(14, 14, 10, 10), TextAnchor.MiddleCenter);
        CreateButton(controls.transform, "btn_ready", "准备", GoldMuted, 170, 56, Ink);
        CreateButton(controls.transform, "btn_start_game", "开始游戏", GoldMuted, 190, 56, Ink);

        SavePrefab(room, $"{ViewFolder}/Room.prefab");
    }
    private static GameObject CreateRoomSlot(Transform parent, string name, int seatIndex)
    {
        GameObject slot = CreateSlicedImage(parent, name, Color.white, Vector2.zero, Vector2.one, Sprite(RoomSlotPanelPath));
        CreateText(slot.transform, "txt_seat", $"座位 {seatIndex + 1}", 22, TextAlignmentOptions.Center, Gold);
        SetRect(slot.transform.Find("txt_seat").gameObject, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.98f));
        CreateImage(slot.transform, "img_shadow", Color.white, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.32f), Sprite(RoomSlotShadowPath(seatIndex)));
        CreateImage(slot.transform, "img_character", Color.white, new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.86f));
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
                ? Sprite(RoomCharacterOnePath)
                : Sprite(RoomCharacterTwoPath);
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

    private static string RoomSlotShadowPath(int index)
    {
        switch (index)
        {
            case 0:
                return RoomSlotShadowOnePath;
            case 1:
                return RoomSlotShadowTwoPath;
            case 2:
                return RoomSlotShadowThreePath;
            default:
                return RoomSlotShadowFourPath;
        }
    }

    private static GameObject CreateSlicedImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite)
    {
        GameObject imageObject = CreateImage(parent, name, color, anchorMin, anchorMax, sprite);
        Image image = imageObject.GetComponent<Image>();
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        return imageObject;
    }

    private static void SetButtonSprite(GameObject buttonObject, Sprite sprite)
    {
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        Transform label = buttonObject.transform.Find("txt_label");
        if (label != null)
        {
            label.gameObject.SetActive(false);
        }
    }

    private static GameObject CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name, typeof(RectTransform));
        group.transform.SetParent(parent, false);
        return group;
    }

    private static void SetRaycastTarget(GameObject target, bool raycastTarget)
    {
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = raycastTarget;
        }
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
