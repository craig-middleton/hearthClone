using System;
using System.IO;
using System.Linq;
using HearthstoneClone.Cards;
using HearthstoneClone.Effects;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run after the table builder; edits the existing styled scene without regenerating it.
public static class InstallMageSpells
{
    const string Dir = "Assets/_Project/ScriptableObjects/MageSpells";
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before installing cards.");
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/HearthstoneTable.unity")
            throw new InvalidOperationException("Open HearthstoneTable before installing cards.");
        Directory.CreateDirectory(Dir);
        string audioDir="Assets/_Project/Art/HearthstoneTable/SpellAudio";
        Directory.CreateDirectory(audioDir);
        string supplied=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Hearthstone Export","AudioClip");
        foreach(string n in new[]{"Shared_Fire_Impact.wav","Mage_ArcaneExplosion_AE_1.wav","Mage_ConeOfCold_Cast_1.wav"})
            if(!File.Exists(audioDir+"/"+n))File.Copy(Path.Combine(supplied,n),audioDir+"/"+n);
        AssetDatabase.Refresh();
        var fire=Asset<EnemyBoardDamageEffect>("FlamestrikeEffect");fire.damageAmount=4;EditorUtility.SetDirty(fire);
        var arcane=Asset<EnemyBoardDamageEffect>("ArcaneExplosionEffect");arcane.damageAmount=1;EditorUtility.SetDirty(arcane);
        var frost=Asset<ConeOfColdEffect>("ConeOfColdEffect");frost.damageAmount=1;EditorUtility.SetDirty(frost);
        var cards=new[]{
            Card("Flamestrike",7,"Deal 4 damage to all enemy minions.",fire,SpellSchool.Fire,TargetRequirement.Self,"HS14-321.png"),
            Card("Arcane Explosion",2,"Deal 1 damage to all enemy minions.",arcane,SpellSchool.Arcane,TargetRequirement.Self,"HS6-049.png"),
            Card("Cone of Cold",4,"Deal 1 damage to a minion and the minions next to it, and <b>Freeze</b> them.",frost,SpellSchool.Frost,TargetRequirement.AnyMinion,"HS11-046.png")
        };
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        foreach(var card in cards)
        {
            e.cardPool.RemoveAll(c=>c!=null && c.cardName==card.cardName);
            e.cardPool.Add(card);
        }
        var animator=e.GetComponent<AreaSpellAnimator>();if(animator==null)animator=e.gameObject.AddComponent<AreaSpellAnimator>();
        animator.rendererTemplate=e.spellAnimationSequencer.burstRenderer;
        animator.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Art/HearthstoneTable/TableSerif.asset");
        animator.numberMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HearthstoneTable/OutlinedNumbers.mat");
        const string compositePath="Assets/_Project/Art/HearthstoneTable/SpellLightComposite.mat";
        var composite=AssetDatabase.LoadAssetAtPath<Material>(compositePath);
        if(composite==null){composite=new Material(Shader.Find("HearthClone/UI/Spell Light Composite"));AssetDatabase.CreateAsset(composite,compositePath);}
        animator.compositeMaterial=composite;
        animator.fireSound=AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir+"/Shared_Fire_Impact.wav");
        animator.arcaneSound=AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir+"/Mage_ArcaneExplosion_AE_1.wav");
        animator.frostSound=AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir+"/Mage_ConeOfCold_Cast_1.wav");
        e.areaSpellAnimator=animator;
        EditorUtility.SetDirty(e);EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Added three spell cards, their rule assets, independent VFX stages, and supplied sound effects to HearthstoneTable. Pool="+e.cardPool.Count;
    }
    static T Asset<T>(string name) where T:ScriptableObject
    {
        string path=Dir+"/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;
    }
    static CardData Card(string name,int cost,string text,CardEffect effect,SpellSchool school,TargetRequirement target,string art)
    {
        var c=Asset<CardData>(name.Replace(" ",""));c.cardName=name;c.manaCost=cost;c.cardType=CardType.Spell;c.description=text;
        c.attack=0;c.health=0;c.hasTaunt=false;c.onPlayEffect=effect;c.targetRequirement=target;c.spellSchool=school;
        c.visualShape=SpellVisualShape.BoardSweep;
        c.artwork=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/HearthstoneTable/"+art);
        EditorUtility.SetDirty(c);return c;
    }
}
