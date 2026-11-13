using System;
using System.IO;
using System.Linq;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallKeywordMinions
{
    const string Art="Assets/_Project/Art/KeywordMinions";
    const string Cards="Assets/_Project/ScriptableObjects/KeywordMinions";
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before installing minions.");
        Directory.CreateDirectory(Art);Directory.CreateDirectory(Cards);AssetDatabase.Refresh();
        // Retain GUIDs when upgrading the previous atlas prototype.
        foreach(string n in new[]{"HS18-128_Wolf.png","GenFX_ally_taunt_glow.png","AttackGlow.mat"})
            if(AssetDatabase.LoadMainAssetAtPath("Assets/_Project/Art/BoardThemes/"+n)!=null&&!File.Exists(Art+"/"+n))
                AssetDatabase.MoveAsset("Assets/_Project/Art/BoardThemes/"+n,Art+"/"+n);
        string source=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Hearthstone Export","Texture2D");
        foreach(string n in new[]{"HS18-128_Wolf.png","GenFX_ally_taunt_glow.png"})
            if(!File.Exists(Art+"/"+n))File.Copy(Path.Combine(source,n),Art+"/"+n);
        AssetDatabase.Refresh();
        foreach(string n in new[]{"HS18-128_Wolf.png","GenFX_ally_taunt_glow.png"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"/"+n);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.alphaSource=TextureImporterAlphaSource.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        foreach(var card in new[]{
            Card("Ironfur Grizzly",3,3,3,true,false,"Assets/_Project/Art/HearthstoneTable/HS12-223_S_Bear.png"),
            Card("Shielded Guardian",4,3,5,true,false,"Assets/_Project/Art/HearthstoneTable/HS14-233.png"),
            Card("Wolf Rider",3,3,1,false,true,Art+"/HS18-128_Wolf.png"),
            Card("Stormwind Rider",5,5,2,false,true,"Assets/_Project/Art/HearthstoneTable/HS23-054_M_Horse.png")
        }){e.cardPool.RemoveAll(c=>c!=null&&c.cardName==card.cardName);e.cardPool.Add(card);}
        var rhino=e.cardPool.FirstOrDefault(c=>c.cardName=="Charging Rhino");if(rhino!=null){rhino.hasCharge=true;rhino.description="<b>Charge</b>";EditorUtility.SetDirty(rhino);}
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/AttackGlow.mat");
        if(material==null){material=new Material(Shader.Find("HearthClone/UI/Atlas Cutout"));AssetDatabase.CreateAsset(material,Art+"/AttackGlow.mat");}
        material.SetColor("_KeyColor",Color.black);material.SetFloat("_Threshold",.02f);EditorUtility.SetDirty(material);
        string path="Assets/_Project/Prefabs/HearthstoneTable/TableMinion.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
        var glow=prefab.transform.Find("ReadyGlow").GetComponent<Image>();glow.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/GenFX_ally_taunt_glow.png");glow.material=material;glow.color=new Color(.18f,1,.12f);glow.rectTransform.sizeDelta=new Vector2(172,211);
        PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
        EditorUtility.SetDirty(e);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Installed 4 Taunt/Charge cards and Charge on the existing Rhino. Pool="+e.cardPool.Count;
    }
    static CardData Card(string name,int cost,int attack,int health,bool taunt,bool charge,string art)
    {
        string path=Cards+"/"+name.Replace(" ","")+".asset";var c=AssetDatabase.LoadAssetAtPath<CardData>(path);
        if(c==null){c=ScriptableObject.CreateInstance<CardData>();AssetDatabase.CreateAsset(c,path);}
        c.cardName=name;c.manaCost=cost;c.cardType=CardType.Minion;c.attack=attack;c.health=health;c.hasTaunt=taunt;c.hasCharge=charge;
        c.description=taunt?"<b>Taunt</b>":"<b>Charge</b>";c.artwork=AssetDatabase.LoadAssetAtPath<Sprite>(art);c.onPlayEffect=null;c.targetRequirement=TargetRequirement.None;c.spellSchool=SpellSchool.None;c.visualShape=SpellVisualShape.SingleTarget;EditorUtility.SetDirty(c);return c;
    }
}
