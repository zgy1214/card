using System;
using Spine.Unity;
using UnityEngine;

public sealed class CharacterArtLibrary : MonoBehaviour
{
    [SerializeField] private CharacterArtEntry[] _entries;

    public Sprite GetSprite(string characterId)
    {
        return GetEntry(characterId)?.Sprite;
    }

    public SkeletonDataAsset GetSkeletonDataAsset(string characterId)
    {
        return GetEntry(characterId)?.SkeletonDataAsset;
    }

    public Vector2 GetRoomOffset(string characterId)
    {
        return GetEntry(characterId)?.RoomOffset ?? Vector2.zero;
    }

    public float GetRoomScale(string characterId)
    {
        return GetEntry(characterId)?.RoomScale ?? 1f;
    }

    public Vector2 GetGameOffset(string characterId)
    {
        return GetEntry(characterId)?.GameOffset ?? Vector2.zero;
    }

    public float GetGameScale(string characterId)
    {
        return GetEntry(characterId)?.GameScale ?? 1f;
    }

    private CharacterArtEntry GetEntry(string characterId)
    {
        if (_entries == null || _entries.Length == 0)
        {
            return null;
        }

        foreach (CharacterArtEntry entry in _entries)
        {
            if (entry != null && string.Equals(entry.CharacterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return _entries[0];
    }
}

[Serializable]
public sealed class CharacterArtEntry
{
    [SerializeField] private string _characterId;
    [SerializeField] private Sprite _sprite;
    [SerializeField] private SkeletonDataAsset _skeletonDataAsset;
    [SerializeField] private Vector2 _roomOffset;
    [SerializeField] private float _roomScale = 1f;
    [SerializeField] private Vector2 _gameOffset;
    [SerializeField] private float _gameScale = 1f;

    public string CharacterId => _characterId;
    public Sprite Sprite => _sprite;
    public SkeletonDataAsset SkeletonDataAsset => _skeletonDataAsset;
    public Vector2 RoomOffset => _roomOffset;
    public float RoomScale => _roomScale > 0f ? _roomScale : 1f;
    public Vector2 GameOffset => _gameOffset;
    public float GameScale => _gameScale > 0f ? _gameScale : 1f;

    public CharacterArtEntry(string characterId, Sprite sprite)
    {
        _characterId = characterId;
        _sprite = sprite;
        _roomOffset = Vector2.zero;
        _roomScale = 1f;
        _gameOffset = Vector2.zero;
        _gameScale = 1f;
    }
}
