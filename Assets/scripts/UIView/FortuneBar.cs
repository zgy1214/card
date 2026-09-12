using UnityEngine;
using UnityEngine.UI;

public sealed class FortuneBar : MonoBehaviour
{
    [SerializeField] private Sprite _luckySprite;
    [SerializeField] private Sprite _unluckySprite;
    [SerializeField] private Image[] _icons;

    public void ShowCounts(int lucky, int unlucky, int previousUnlucky = -1)
    {
        if (lucky < 0 || unlucky < 0 || lucky + unlucky != _icons.Length)
            throw new System.ArgumentException("Fortune counts must form the complete eight-token pool.");
        int total = _icons.Length;
        for (int i = 0; i < _icons.Length; i++)
        {
            Image icon = _icons[i];
            icon.transform.parent.gameObject.SetActive(i < total);
            icon.sprite = i < unlucky ? _unluckySprite : _luckySprite;
            icon.SetNativeSize();
            Outline outline = icon.GetComponent<Outline>();
            if (outline != null) outline.enabled = previousUnlucky >= 0
                && i >= Mathf.Min(unlucky, previousUnlucky) && i < Mathf.Max(unlucky, previousUnlucky);
        }
    }
}
