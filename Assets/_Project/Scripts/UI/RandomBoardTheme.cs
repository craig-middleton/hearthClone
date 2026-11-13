using System.Collections.Generic;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // Decorative only. All boards share the same hand, target panels, and hero positions.
    public class RandomBoardTheme : MonoBehaviour
    {
        public List<GameObject> boards = new List<GameObject>();
        public int CurrentIndex { get; private set; } = -1;
        public string CurrentName => CurrentIndex >= 0 ? boards[CurrentIndex].name : "";

        public void ChooseNextBoard()
        {
            if (boards.Count == 0) return;
            int next = Random.Range(0, boards.Count > 1 && CurrentIndex >= 0 ? boards.Count - 1 : boards.Count);
            if (boards.Count > 1 && CurrentIndex >= 0 && next >= CurrentIndex) next++;
            ShowBoard(next);
        }

        public void ShowBoard(int index)
        {
            if (index < 0 || index >= boards.Count) return;
            for (int i = 0; i < boards.Count; i++) if (boards[i] != null) boards[i].SetActive(i == index);
            CurrentIndex = index;
        }
    }
}
