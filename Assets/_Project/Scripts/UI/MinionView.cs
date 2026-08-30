using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class MinionView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text nameText;
        public TMP_Text statsText;
        public Button button;
        public Image minionBackground;
        public Image artworkImage;

        [Header("Visuals")]
        public Color normalColor = Color.white;
        public Color selectedColor = new Color(0.6f, 0.9f, 0.6f);
        public Color cannotAttackColor = new Color(0.45f, 0.45f, 0.45f);

        [Header("Spell Reaction")]
        public float reactionDuration = 0.25f;
        public Color damageFlashColor = new Color(1f, 0.3f, 0.3f);

        private Minion minion;
        private Action<Minion> onClicked;
        private Coroutine reactionRoutine;

        public Minion Minion => minion;

        private int holdCount = 0;
        private float holdExpiresAt = 0f;
        private LayoutElement layoutElement;

        // True while an in-flight animation (e.g. SpellAnimationSequencer on a lethal hit)
        // still needs this GameObject to survive the next BoardDisplay.RenderBoard refresh,
        // even though the Minion it represents has already been removed from the model by
        // then. Self-expires via holdExpiresAt so a caller that forgets EndHold() can't leak
        // the view forever - the next refresh after the TTL lapses destroys it anyway, logged
        // since that indicates a caller bug rather than expected behavior.
        public bool IsHeld
        {
            get
            {
                if (holdCount <= 0) return false;
                if (Time.time >= holdExpiresAt)
                {
                    Debug.LogWarning($"MinionView: hold on '{(minion != null ? minion.MinionName : "(unknown)")}' expired via TTL sweep (holdCount was {holdCount}) - an animation likely didn't call EndHold(). Treating as unheld.", this);
                    holdCount = 0;
                    if (layoutElement != null)
                    {
                        layoutElement.ignoreLayout = false;
                    }
                    return false;
                }
                return true;
            }
        }

        // Counter-based (not a bool) so two overlapping holds on the same view - e.g. two
        // quick spells targeting the same dying minion before the first animation finishes -
        // don't get cleared early by the first one's EndHold(). maxSeconds bounds the hold via
        // IsHeld's TTL check above.
        public void BeginHold(float maxSeconds)
        {
            // First hold on this view: pull it out of the parent HorizontalLayoutGroup's
            // control before RenderBoard can rebuild siblings around it. A held view keeps
            // its GameObject (BoardDisplay.RenderBoard skips destroying it) but its Minion is
            // already gone from the model, so the surviving minions get re-instantiated as
            // new children appended after it - which shifts this view's sibling index and
            // lets the layout group snap it to a recalculated (wrong) slot mid-animation.
            // ignoreLayout freezes it exactly where it visually sits right now, decoupled
            // from whatever the group does to its rebuilt siblings afterward.
            if (holdCount == 0)
            {
                if (layoutElement == null)
                {
                    layoutElement = GetComponent<LayoutElement>();
                }
                if (layoutElement != null)
                {
                    layoutElement.ignoreLayout = true;
                }
            }

            holdCount++;
            holdExpiresAt = Mathf.Max(holdExpiresAt, Time.time + maxSeconds);

            // A held-but-already-dead minion must not stay clickable - the model no longer
            // contains it, so a click during the hold window would invoke onClicked with a
            // stale Minion reference. Mirrors the exact bug class Constraint 16 fixed for
            // SpellAnimationSequencer's destroyed-Transform read, just triggered by input
            // instead of an animation frame.
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        public void EndHold()
        {
            holdCount = Mathf.Max(0, holdCount - 1);

            if (holdCount == 0 && layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
            }
        }

        public void SetMinion(Minion minionData, Action<Minion> clickCallback = null, bool isSelected = false, bool showAttackEligibility = false)
        {
            if (minionData == null)
            {
                Debug.LogWarning("MinionView.SetMinion called with a null Minion — skipping.", this);
                return;
            }

            minion = minionData;
            onClicked = clickCallback;

            if (nameText != null)
            {
                nameText.text = minion.HasTaunt ? $"{minion.MinionName} (Taunt)" : minion.MinionName;
            }
            else
            {
                Debug.LogWarning("MinionView: 'nameText' is not assigned in the Inspector — minion name will not render.", this);
            }

            if (statsText != null)
            {
                statsText.text = $"{minion.CurrentAttack} / {minion.CurrentHealth}";
            }
            else
            {
                Debug.LogWarning("MinionView: 'statsText' is not assigned in the Inspector — minion attack/health will not render.", this);
            }

            if (artworkImage != null)
            {
                if (minion.Artwork != null)
                {
                    artworkImage.sprite = minion.Artwork;
                    artworkImage.enabled = true;
                }
                else
                {
                    artworkImage.enabled = false;
                }
            }

            UpdateVisual(isSelected, showAttackEligibility);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(minion));
            }
            else
            {
                Debug.LogWarning("MinionView: 'button' is not assigned in the Inspector — this minion cannot be clicked or selected to attack.", this);
            }
        }

        // Called by SpellAnimationSequencer when a damage spell's travel effect lands on
        // this minion. Captures whatever color minionBackground currently shows (selected /
        // cannot-attack / normal) as the flash baseline and restores it afterward, since
        // unlike FaceView there's no continuous per-frame recompute to just let overwrite it.
        public void PlayDamageReaction()
        {
            if (minionBackground == null) return;

            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }
            reactionRoutine = StartCoroutine(FlashRoutine(damageFlashColor));
        }

        private IEnumerator FlashRoutine(Color flashColor)
        {
            Color baseColor = minionBackground.color;
            float elapsed = 0f;
            while (elapsed < reactionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reactionDuration);
                float intensity = 1f - Mathf.Abs((t * 2f) - 1f);
                minionBackground.color = Color.Lerp(baseColor, flashColor, intensity);
                yield return null;
            }

            minionBackground.color = baseColor;
            reactionRoutine = null;
        }

        private void UpdateVisual(bool isSelected, bool showAttackEligibility)
        {
            if (minionBackground == null) return;

            if (isSelected)
            {
                minionBackground.color = selectedColor;
            }
            else if (showAttackEligibility && !minion.CanAttack)
            {
                minionBackground.color = cannotAttackColor;
            }
            else
            {
                minionBackground.color = normalColor;
            }
        }
    }
}