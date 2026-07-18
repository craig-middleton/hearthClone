using System.Collections.Generic;
using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class BoardDisplay : MonoBehaviour
    {
        public GameObject minionViewPrefab;
        public Transform boardPanel;

        public void RenderBoard(List<Minion> minions)
        {
            foreach (Transform child in boardPanel)
            {
                Destroy(child.gameObject);
            }

            foreach (Minion minion in minions)
            {
                GameObject minionObj = Instantiate(minionViewPrefab, boardPanel);
                MinionView view = minionObj.GetComponent<MinionView>();
                view.SetMinion(minion);
            }
        }
    }
}