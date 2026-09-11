using System;
using System.IO;
using UnityEngine;

public sealed class LocalPlayerProfile
{
    private const string SaveFileName = "local_player_profile.json";
    private const float AutoSaveIntervalSeconds = 5f;

    private float _saveElapsedSeconds;
    private bool _isDirty;

    public string PlayerId { get; private set; }
    public string Name { get; private set; }
    public string CharacterId { get; private set; }

    public void LoadOrCreate()
    {
        LocalPlayerProfileSaveData saveData = LoadSaveData();
        PlayerId = string.IsNullOrEmpty(saveData.player_id) ? Guid.NewGuid().ToString() : saveData.player_id;
        Name = string.IsNullOrEmpty(saveData.name) ? GenerateDefaultName() : saveData.name;
        CharacterId = NormalizeCharacterId(saveData.character_id);
        MarkDirty();
        SaveIfDirty(force: true);
    }

    public void Tick(float deltaTime)
    {
        if (!_isDirty)
        {
            return;
        }

        _saveElapsedSeconds += deltaTime;
        if (_saveElapsedSeconds >= AutoSaveIntervalSeconds)
        {
            SaveIfDirty(force: true);
        }
    }

    public void SetName(string name)
    {
        if (string.IsNullOrEmpty(name) || string.Equals(Name, name, StringComparison.Ordinal))
        {
            return;
        }

        Name = name;
        MarkDirty();
    }

    public void SetCharacterId(string characterId)
    {
        string normalizedCharacterId = NormalizeCharacterId(characterId);
        if (string.Equals(CharacterId, normalizedCharacterId, StringComparison.Ordinal))
        {
            return;
        }

        CharacterId = normalizedCharacterId;
        MarkDirty();
    }

    public void SaveIfDirty(bool force = false)
    {
        if (!_isDirty && !force)
        {
            return;
        }

        LocalPlayerProfileSaveData saveData = new LocalPlayerProfileSaveData
        {
            player_id = PlayerId,
            name = Name,
            character_id = CharacterId
        };

        File.WriteAllText(GetSavePath(), JsonUtility.ToJson(saveData, prettyPrint: true));
        _isDirty = false;
        _saveElapsedSeconds = 0f;
    }

    private void MarkDirty()
    {
        _isDirty = true;
    }

    private LocalPlayerProfileSaveData LoadSaveData()
    {
        string savePath = GetSavePath();
        if (!File.Exists(savePath))
        {
            return new LocalPlayerProfileSaveData();
        }

        try
        {
            return JsonUtility.FromJson<LocalPlayerProfileSaveData>(File.ReadAllText(savePath))
                ?? new LocalPlayerProfileSaveData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load local player profile, a new profile will be created. {exception.Message}");
            return new LocalPlayerProfileSaveData();
        }
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private string GenerateDefaultName()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        System.Random random = new System.Random();
        char[] suffix = new char[4];
        for (int index = 0; index < suffix.Length; index += 1)
        {
            suffix[index] = alphabet[random.Next(alphabet.Length)];
        }

        return $"player_{new string(suffix)}";
    }

    private string NormalizeCharacterId(string characterId)
    {
        switch (characterId)
        {
            case "mengshen":
            case "xiaomu":
            case "ayi":
            case "naibao":
                return characterId;
            default:
                return "mengshen";
        }
    }

    [Serializable]
    private sealed class LocalPlayerProfileSaveData
    {
        public string player_id;
        public string name;
        public string character_id;
    }
}
