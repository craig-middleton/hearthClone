using System;
using System.IO;
using System.Linq;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallBattlePresentation
{
    const string Art="Assets/_Project/Art/ActionFX",Table="Assets/_Project/Art/HearthstoneTable";
    static TMP_FontAsset font;static Material number;
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        foreach(var name in new[]{"Warrior_power2_D.png","Armor_D.png","ArrowTargetA_D.png","TargetCursor64.png","GenFX_Fireball_1Hot.png","GenFX_Fireball_1Hot_Blue.png","GenFX_Fireball_12.png","GenFX_ArcaneImpactRunes.png","GenFX_Slash_03.png","GenFX_Slash_01.png","bullet_hit_flash_burst_explosion_pop_star.png"})Import(name);
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var canvas=GameObject.Find("GameCanvas").transform;var surface=canvas.Find("TableSurface");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Table+"/TableSerif.asset");number=AssetDatabase.LoadAssetAtPath<Material>(Table+"/OutlinedNumbers.mat");
        var old=surface.Find("OpponentHeroPowerButton");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var power=UnityEngine.Object.Instantiate(e.heroPowerButton.gameObject,surface,false);power.name="OpponentHeroPowerButton";
        var rect=(RectTransform)power.transform;rect.anchoredPosition=new Vector2(202,370);
        e.opponentHeroPowerButton=power.GetComponent<Button>();e.opponentHeroPowerButton.interactable=false;
        var display=power.GetComponent<HeroPowerPresentation>();display.artwork.sprite=Load("Warrior_power2_D.png");
        display.usedBack.enabled=false;display.artwork.enabled=true;display.readyGlow.SetActive(false);
        foreach(var button in new[]{e.heroPowerButton,e.opponentHeroPowerButton}){var colors=button.colors;colors.disabledColor=new Color(.8f,.8f,.8f,1);button.colors=colors;}
        var armor=Slice("Armor_D.png","ArmorShield",4,5,106,119);
        foreach(var face in new[]{e.faceView,e.opponentFaceView})
        {
            string name=face.name+" Armor";var parent=surface.Find("HeroHealthOverlay");var prior=parent.Find(name);if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
            var icon=Image(name,parent,armor,((RectTransform)face.transform).anchoredPosition+new Vector2(83,-21),new Vector2(45,50));
            icon.gameObject.AddComponent<AtlasContour>().outline=new[]{new Vector2(.5f,.99f),new Vector2(.96f,.83f),new Vector2(.96f,.52f),new Vector2(.84f,.24f),new Vector2(.5f,.01f),new Vector2(.17f,.24f),new Vector2(.02f,.52f),new Vector2(.02f,.83f)};
            var text=Text("Armor",icon.transform,"0",30,Vector2.zero,new Vector2(55,50));text.rectTransform.anchoredPosition=new Vector2(0,-3);
            var serialized=new SerializedObject(face);serialized.FindProperty("armorDisplay").objectReferenceValue=icon.gameObject;serialized.FindProperty("armorNumberText").objectReferenceValue=text;serialized.ApplyModifiedPropertiesWithoutUndo();icon.gameObject.SetActive(false);
        }
        var mana=(RectTransform)surface.Find("OpponentMana");mana.anchoredPosition=new Vector2(420,500);mana.sizeDelta=new Vector2(90,40);
        var priorBadge=surface.Find("OpponentManaBadge");if(priorBadge!=null)UnityEngine.Object.DestroyImmediate(priorBadge.gameObject);
        var badge=Image("OpponentManaBadge",surface,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/FrontEnd/CollectionHeader.asset"),new Vector2(420,500),new Vector2(105,44));badge.type=UnityEngine.UI.Image.Type.Sliced;badge.pixelsPerUnitMultiplier=4;badge.color=new Color(.55f,.7f,.9f);badge.transform.SetSiblingIndex(mana.GetSiblingIndex());
        var previous=canvas.Find("ActionOverlay");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var overlay=Rect("ActionOverlay",canvas,Vector2.zero,new Vector2(1920,1080));var layer=overlay.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=12;
        var battle=e.GetComponent<BattleEffects>();if(battle==null)battle=e.gameObject.AddComponent<BattleEffects>();e.battleEffects=battle;battle.overlay=overlay;
        battle.fire=Load("GenFX_Fireball_1Hot.png");battle.frost=Load("GenFX_Fireball_1Hot_Blue.png");battle.arcane=Load("GenFX_ArcaneImpactRunes.png");
        battle.impact=Load("bullet_hit_flash_burst_explosion_pop_star.png");battle.slash=Load("GenFX_Slash_03.png");battle.font=font;battle.numberMaterial=number;
        battle.lightMaterial=e.areaSpellAnimator.compositeMaterial;battle.fireSound=e.areaSpellAnimator.fireSound;battle.arcaneSound=e.areaSpellAnimator.arcaneSound;battle.frostSound=e.areaSpellAnimator.frostSound;
        string clipPath=Art+"/RammingSpeed_Attack_Impact_Sound.wav";
        if(!File.Exists(clipPath))File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Hearthstone Export","AudioClip","RammingSpeed_Attack_Impact_Sound.wav"),clipPath);
        AssetDatabase.ImportAsset(clipPath,ImportAssetOptions.ForceSynchronousImport);battle.hitSound=AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        e.spellAnimationSequencer.nativeEffects=battle;
        e.areaSpellAnimator.suppliedGlow=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/GenFX_Fireball_12.png");e.areaSpellAnimator.suppliedShard=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/GenFX_Slash_01.png");
        var arrowRect=Rect("PowerTargetingArrow",overlay,Vector2.zero,new Vector2(1920,1080));var arrow=arrowRect.gameObject.AddComponent<TargetingRibbon>();arrow.raycastTarget=false;
        arrow.ribbon=Slice("ArrowTargetA_D.png","RedTargetRibbon",235,58,75,38);e.targetingRibbon=arrow;
        arrow.cursor=Image("TargetCursor",overlay,Load("TargetCursor64.png"),Vector2.zero,new Vector2(62,62));
        arrow.hint=Text("AimHint",overlay,"Choose a target. Right-click or Esc to cancel.",23,new Vector2(0,230),new Vector2(900,45)).gameObject;arrow.Hide();
        e.heroPowerButton.gameObject.SetActive(false);power.SetActive(false);
        foreach(var component in e.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorUtility.SetDirty(e);EditorUtility.SetDirty(battle);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Installed native targeting ribbon, both hero powers/armor badges and native combat/spell presentation.";
    }
    static void Import(string name)
    {
        string path=Art+"/"+name;if(!File.Exists(path))File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Hearthstone Export","Texture2D",name),path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaSource=name=="TargetCursor64.png"||name=="GenFX_Slash_01.png"?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    }
    static Sprite Load(string file)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+file);
    static Sprite Slice(string file,string name,float x,float top,float w,float h)
    {
        string path=Art+"/"+name+".asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+file);var s=Sprite.Create(texture,new Rect(x,texture.height-top-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);s.name=name;
        var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(old==null){AssetDatabase.CreateAsset(s,path);return s;}EditorUtility.CopySerialized(s,old);UnityEngine.Object.DestroyImmediate(s);return old;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;}
    static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size)
    {var image=Rect(name,parent,position,size).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;}
    static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 position,Vector2 dimensions)
    {var text=Rect(name,parent,position,dimensions).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=number;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
}
