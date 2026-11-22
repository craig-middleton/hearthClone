using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

public static class VerifyNativeHandOutlines
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static async Task Until(Func<bool> condition, int timeout, string message)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.ElapsedMilliseconds > timeout) throw new Exception(message);
            await Task.Delay(20);
        }
    }

    static void Refresh(EffectTester game)
    {
        typeof(EffectTester).GetMethod("RefreshAll", Private).Invoke(game, null);
    }

    static NativeHandCardPresentation HandCard(Transform hand, CardData card)
    {
        return hand.GetComponentsInChildren<CardView>()
            .Single(view => view.Card.Data == card).GetComponent<NativeHandCardPresentation>();
    }

    static bool Hidden(NativeHandCardPresentation style)
    {
        return !style.minionOutline.enabled && !style.spellOutline.enabled;
    }

    static void OriginalGraphic(Graphic outline, string textureName)
    {
        var image = outline as RawImage;
        Check(image != null && image.texture != null && image.texture.name == textureName,
            "Ready outline is missing its original card silhouette");
        Check(!outline.raycastTarget && image.maskable,
            "Ready outline intercepts input or bypasses UI clipping");
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/NativeMinions/HighlightGreenInPlay.mat");
        Check(image.material.shader.isSupported &&
            image.material.GetColor("_Color") == SourceMaterialProperties.Color(original, "_Color") &&
            image.material.GetColor("_HiColor") == SourceMaterialProperties.Color(original, "_HiColor") &&
            Mathf.Approximately(image.material.GetFloat("_Intensity"), SourceMaterialProperties.Float(original, "_Intensity")),
            "Source highlight material is missing its original green parameters");
    }

    public static async Task<string> Main()
    {
        await Task.Delay(150);
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        bool previousManual = game.manualControlMode;
        try
        {
            game.manualControlMode = true;
            game.frontEnd.ShowMenu();
            game.frontEnd.Play();
            game.confirmMulliganButton.onClick.Invoke();
            await Until(() => !game.ActionEffectsBusy && !game.handDisplay.drawPresentation.IsBusy,
                10000, "Opening presentation did not finish");

            var owner = game.faceView.Player;
            var hand = (PlayerHand)typeof(EffectTester).GetField("playerOneHand", Private).GetValue(game);
            var minion = game.cardPool.First(card => card.cardType == CardType.Minion && card.manaCost > 0);
            var spell = game.cardPool.First(card => card.cardType == CardType.Spell && card.manaCost > 0);
            hand.Hand.Clear();
            hand.AddCardToHand(minion);
            hand.AddCardToHand(spell);
            owner.CurrentMana = 0;
            Refresh(game);
            await Task.Delay(100);

            var minionStyle = HandCard(game.handDisplay.handPanel, minion);
            var spellStyle = HandCard(game.handDisplay.handPanel, spell);
            Check(minionStyle != null && spellStyle != null, "Hand outline presenters were not installed");
            OriginalGraphic(minionStyle.minionOutline, "Card_InHand_Ally_Silhouette");
            OriginalGraphic(spellStyle.spellOutline, "Card_Inhand_Ability_Silhouette");
            Check(Hidden(minionStyle) && Hidden(spellStyle), "Unaffordable cards have a ready outline");

            owner.CurrentMana = 10;
            Refresh(game);
            await Task.Delay(100);
            minionStyle = HandCard(game.handDisplay.handPanel, minion);
            spellStyle = HandCard(game.handDisplay.handPanel, spell);
            Check(minionStyle.minionOutline.enabled && !minionStyle.spellOutline.enabled,
                "Affordable minion does not use its original silhouette");
            Check(spellStyle.spellOutline.enabled && !spellStyle.minionOutline.enabled,
                "Affordable spell does not use its original silhouette");

            owner.BoardMinions.Clear();
            for (int i = 0; i < 7; i++)
                owner.BoardMinions.Add(new Minion("Full-board outline fixture " + i, 1, 1, false, minion.artwork));
            Refresh(game);
            await Task.Delay(100);
            minionStyle = HandCard(game.handDisplay.handPanel, minion);
            spellStyle = HandCard(game.handDisplay.handPanel, spell);
            Check(Hidden(minionStyle) && spellStyle.spellOutline.enabled,
                "Full board did not suppress only the unplayable minion outline");

            var spellView = spellStyle.GetComponent<CardView>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = spellView.transform.position
            };
            spellView.OnBeginDrag(pointer);
            await Task.Delay(60);
            var ghosts = spellView.GetComponentInParent<Canvas>().rootCanvas
                .GetComponentsInChildren<NativeHandCardPresentation>()
                .Where(style => style.name.EndsWith("(DragGhost)")).ToArray();
            Check(ghosts.Length == 1 && ghosts.All(Hidden), "Drag ghost retained a hand-ready outline");
            spellView.OnEndDrag(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = Vector2.zero
            });
            await Task.Delay(100);

            game.endTurnButton.onClick.Invoke();
            await Task.Delay(100);
            Check(game.handDisplay.handPanel.GetComponentsInChildren<NativeHandCardPresentation>().All(Hidden),
                "Inactive player's cards retained ready outlines");
            game.manualControlMode = false;
            Refresh(game);
            await Task.Delay(100);
            Check(game.opponentHandDisplay.handPanel.GetComponentsInChildren<NativeHandCardPresentation>().All(Hidden),
                "Concealed opponent cards retained ready outlines");

            game.frontEnd.ShowMenu();
            game.frontEnd.Play();
            await Task.Delay(100);
            Check(game.mulliganPanel.GetComponentsInChildren<NativeHandCardPresentation>().All(Hidden),
                "Mulligan choices have hand-ready outlines");
            game.frontEnd.ShowMenu();
            game.frontEnd.chooseDeckButton.onClick.Invoke();
            await Task.Delay(100);
            Check(game.frontEnd.collectionGrid.GetComponentsInChildren<NativeHandCardPresentation>().All(Hidden),
                "Deck collection cards have hand-ready outlines");
            game.frontEnd.ShowMenu();
            return "Original minion/spell hand silhouettes and green source parameters, affordability, board-space eligibility, input/clipping, inactive turn, concealed opponent, drag ghost, mulligan and collection suppression passed.";
        }
        finally
        {
            game.manualControlMode = previousManual;
            game.frontEnd.ShowMenu();
        }
    }
}
