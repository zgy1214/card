using TMPro;
using UnityEngine;

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

        gameCardNameText.text = gameCardViewArg.CardName;
    }
}
