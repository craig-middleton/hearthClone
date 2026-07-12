using System.Collections.Generic;
using UnityEngine;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    public class HandDisplay : MonoBehaviour
    {
        public GameObject cardViewPrefab;
        public Transform handPanel;

        public void RenderHand(List<CardData> hand)
        {
            foreach (Transform child in handPanel)
            {
                Destroy(child.gameObject);
            }

            foreach (CardData card in hand)
            {
                GameObject cardObj = Instantiate(cardViewPrefab, handPanel);
                CardView view = cardObj.GetComponent<CardView>();
                view.SetCard(card);
            }
        }
    }
}