using System;
using System.IO;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
public static class RefreshNativeCardNames
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var library=AssetDatabase.LoadAssetAtPath<NativeCardLibrary>("Assets/_Project/Art/NativeCards/NativeCards.asset");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/RecoveredInterface/Proof/Belwe.asset");
        foreach(var item in library.names)
        {
            string path=AssetDatabase.GetAssetPath(item.texture);
            if(item.card==null||string.IsNullOrEmpty(path))throw new Exception("Unbound source name");
            item.texture=NativeCardTextBaker.Bake(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path),item.card.cardName,font,Color.white,true);
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab");
        try
        {
            var face=root.GetComponent<NativeCardFace>();
            foreach(var label in face.labels)
            {
                if(label.role!=NativeCardRole.Description||!label.text.transform.IsChildOf(face.spellFace.transform))continue;
                var rect=label.text.rectTransform;
                if(Mathf.Abs(rect.sizeDelta.x-112.812f)<.2f)
                {
                    // Recover unused upper parchment space, leaving the lower
                    // edge and separate school label untouched. Idempotent.
                    rect.anchoredPosition+=Vector2.up*4;rect.sizeDelta+=new Vector2(6,8);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        return "Rebaked "+library.names.Length+" original Belwe titles at consistent ink height, with a thinner compatibility outline; source mesh/UVs unchanged.";
    }
}
