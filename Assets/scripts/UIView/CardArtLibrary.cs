using System;
using UnityEngine;

public sealed class CardArtLibrary : MonoBehaviour
{
    [SerializeField] private Sprite _backSprite;
    [SerializeField] private CardArtEntry[] _entries;

    public Sprite BackSprite => _backSprite;

    public Sprite GetFaceSprite(string cardName)
    {
        if (_entries == null || string.IsNullOrEmpty(cardName))
        {
            return null;
        }

        foreach (CardArtEntry entry in _entries)
        {
            if (entry != null && string.Equals(entry.CardName, cardName, StringComparison.Ordinal))
            {
                return entry.Sprite;
            }
        }

        return null;
    }
}

[Serializable]
public sealed class CardArtEntry
{
    [SerializeField] private string _cardName;
    [SerializeField] private Sprite _sprite;

    public string CardName => _cardName;
    public Sprite Sprite => _sprite;
}
