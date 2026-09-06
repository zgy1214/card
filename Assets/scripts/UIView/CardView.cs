using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardView : UIScript
{
    public override string GetPath()
    {
        return "prefabs/template/card";
    }

    public override void OnOpen()
    {
        CardViewArg gameCardViewArg = this.GameViewArg as CardViewArg;
        if (gameCardViewArg == null)
        {
            throw new System.ArgumentException("CardView requires CardViewArg.");
        }

        Transform cardNameTransform = ViewTransform.Find("card_name");
        if (cardNameTransform == null)
        {
            throw new System.InvalidOperationException("CardView cannot find card_name node.");
        }

        TMP_Text gameCardNameText = cardNameTransform.GetComponent<TMP_Text>();
        if (gameCardNameText == null)
        {
            throw new System.InvalidOperationException("CardView cannot find TMP_Text on card_name.");
        }

        Transform faceTransform = ViewTransform.Find("img_card_face");
        gameCardNameText.gameObject.SetActive(false);

        if (faceTransform != null)
        {
            Image faceImage = faceTransform.GetComponent<Image>();
            CardArtLibrary cardArtLibrary = ViewGameObject.GetComponentInParent<CardArtLibrary>();
            Sprite sprite = cardArtLibrary?.GetFaceSprite(gameCardViewArg.CardName);
            if (faceImage != null && sprite != null)
            {
                faceImage.sprite = sprite;
                faceImage.color = Color.white;
            }
        }
    }
}
