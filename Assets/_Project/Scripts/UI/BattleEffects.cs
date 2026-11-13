using System;
using System.Collections;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Presentation observes resolved actions; it never applies damage or spends mana.
    public class BattleEffects : MonoBehaviour
    {
        public RectTransform overlay;
        public Sprite fire, frost, arcane, impact, slash;
        public Material lightMaterial;
        public TMP_FontAsset font;
        public Material numberMaterial;
        public AudioClip fireSound,arcaneSound,frostSound,hitSound;
        public bool IsBusy {get;private set;}
        public event Action Completed;
        Board board;BoardDisplay oneBoard,twoBoard;FaceView oneFace,twoFace;
        readonly Queue<IEnumerator> queue=new Queue<IEnumerator>();
        readonly List<GameObject> temporary=new List<GameObject>();
        readonly List<CanvasGroup> hidden=new List<CanvasGroup>();
        AudioSource audioSource;
        public void Bind(Board model,BoardDisplay one,BoardDisplay two,FaceView a,FaceView b)
        {ResetEffects();board=model;oneBoard=one;twoBoard=two;oneFace=a;twoFace=b;}
        public void ResetEffects()
        {
            StopAllCoroutines();queue.Clear();IsBusy=false;
            if(audioSource!=null)audioSource.Stop();
            foreach(var group in hidden)if(group!=null)group.alpha=1;hidden.Clear();
            foreach(var go in temporary)if(go!=null){go.SetActive(false);Destroy(go);}temporary.Clear();
        }
        Transform View(Target target)
        {
            if(target==null)return null;
            if(target.TargetPlayer!=null)return target.TargetPlayer==board.PlayerOne?oneFace.transform:twoFace.transform;
            var view=oneBoard.GetViewTransform(target.TargetMinion);
            if(view==null)view=twoBoard.GetViewTransform(target.TargetMinion);return view;
        }
        GameObject Ghost(Transform source)
        {
            if(source==null)return null;
            var copy=Instantiate(source.gameObject,overlay,true);copy.name="CombatSnapshot";
            foreach(var view in copy.GetComponentsInChildren<MinionView>())view.enabled=false;
            foreach(var button in copy.GetComponentsInChildren<Button>())button.enabled=false;
            foreach(var graphic in copy.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
            foreach(var canvas in copy.GetComponentsInChildren<Canvas>())canvas.overrideSorting=false;
            var group=copy.GetComponent<CanvasGroup>();if(group==null)group=copy.AddComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=false;
            temporary.Add(copy);return copy;
        }
        void Enqueue(IEnumerator routine){queue.Enqueue(routine);if(!IsBusy){IsBusy=true;StartCoroutine(Run());}}
        IEnumerator Run()
        {yield return null;while(queue.Count>0)yield return queue.Dequeue();IsBusy=false;Completed?.Invoke();}
        void Remove(GameObject go){if(go==null)return;temporary.Remove(go);Destroy(go);}
        Image Image(string name,Sprite sprite,Vector3 position,float size,Color tint)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(overlay,false);
            var image=go.GetComponent<Image>();image.sprite=sprite;image.material=lightMaterial;image.color=tint;image.raycastTarget=false;
            image.rectTransform.sizeDelta=Vector2.one*size;image.rectTransform.position=position;temporary.Add(go);return image;
        }
        void Sound(AudioClip clip)
        {if(clip==null)return;if(audioSource==null)audioSource=gameObject.AddComponent<AudioSource>();audioSource.PlayOneShot(clip,.65f);}
        Sprite SpriteFor(SpellSchool school)=>school==SpellSchool.Fire?fire:school==SpellSchool.Frost?frost:school==SpellSchool.Arcane?arcane:impact;
        Color Tint(SpellSchool school)=>school==SpellSchool.Nature?new Color(.35f,1,.4f):school==SpellSchool.None?new Color(1,.85f,.35f):Color.white;
        void React(Target target)
        {var live=View(target);if(live==null)return;var face=live.GetComponent<FaceView>();if(face!=null)face.PlayDamageReaction();else live.GetComponent<MinionView>()?.PlayDamageReaction();}
        public void PlayAttack(Minion attacker,Target target)
        {
            var source=View(new Target(attacker));var destination=View(target);if(destination==null)return;
            var ghost=Ghost(source);var victim=target.TargetMinion!=null&&target.TargetMinion.IsDead?Ghost(destination):null;
            if(ghost!=null)ghost.SetActive(false);
            Vector3 from=source!=null?source.position:(board.GetOwnerOf(attacker)==board.PlayerOne?oneFace:twoFace).transform.position;
            Enqueue(Attack(attacker,target,ghost,victim,from,destination.position));
        }
        IEnumerator Attack(Minion attacker,Target target,GameObject ghost,GameObject victim,Vector3 from,Vector3 to)
        {
            var live=View(new Target(attacker));CanvasGroup group=null;
            if(live!=null){group=live.GetComponent<CanvasGroup>();if(group==null)group=live.gameObject.AddComponent<CanvasGroup>();group.alpha=0;hidden.Add(group);}
            if(ghost!=null)ghost.SetActive(true);
            float t=0;Vector3 hit=Vector3.Lerp(from,to,.82f);
            while(t<.19f){t+=Time.deltaTime;if(ghost!=null)ghost.transform.position=Vector3.Lerp(from,hit,Mathf.SmoothStep(0,1,t/.19f));yield return null;}
            Sound(hitSound);React(target);StartCoroutine(Burst(to,slash,new Color(1,.85f,.5f),attacker.CurrentAttack,.45f));
            if(target.TargetMinion!=null&&target.TargetMinion.CurrentAttack>0)StartCoroutine(Burst(from,impact,Color.white,target.TargetMinion.CurrentAttack,.4f));
            Remove(victim);t=0;
            while(t<.22f){t+=Time.deltaTime;if(ghost!=null)ghost.transform.position=Vector3.Lerp(hit,from,Mathf.SmoothStep(0,1,t/.22f));yield return null;}
            Remove(ghost);if(group!=null){group.alpha=1;hidden.Remove(group);}yield return new WaitForSeconds(.22f);
        }
        public void PlaySpell(CardData card,Player caster,Target target)
        {
            if(card.cardType!=CardType.Spell||card.onPlayEffect is IAreaDamageEffect)return;
            if(card.visualShape==SpellVisualShape.BoardSweep)
            {
                var enemy=board.GetOpponent(caster);var targets=new List<Target>();foreach(var minion in enemy.BoardMinions)if(!minion.IsDead)targets.Add(new Target(minion));
                Enqueue(Sweep(targets,card.spellSchool));return;
            }
            int damage=card.onPlayEffect is IDamageEffect effect?effect.DamageAmount:0;
            PlayProjectile((caster==board.PlayerOne?oneFace:twoFace).transform.position,target,card.spellSchool,damage);
        }
        public void PlayPower(Player caster,HeroPowerKind kind,Target target,Transform button)
        {
            if(kind==HeroPowerKind.ArmorUp)Enqueue(Burst((caster==board.PlayerOne?oneFace:twoFace).transform.position,arcane,new Color(.65f,.8f,1),0,.7f));
            else PlayProjectile(button.position,target,SpellSchool.Fire,1);
        }
        void PlayProjectile(Vector3 source,Target target,SpellSchool school,int damage)
        {
            var view=View(target);if(view==null)return;
            var victim=target.TargetMinion!=null&&target.TargetMinion.IsDead?Ghost(view):null;
            Enqueue(Projectile(source,target,view.position,school,damage,victim));
        }
        IEnumerator Projectile(Vector3 source,Target target,Vector3 destination,SpellSchool school,int damage,GameObject victim)
        {
            var projectile=Image("NativeSpellProjectile",SpriteFor(school),source,95,Tint(school));
            float t=0;while(t<.38f)
            {
                t+=Time.deltaTime;float p=Mathf.Clamp01(t/.38f);
                var live=View(target);if(live!=null)destination=live.position;
                projectile.rectTransform.position=Vector3.Lerp(source,destination,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*45);
                projectile.rectTransform.localRotation=Quaternion.Euler(0,0,p*140);yield return null;
            }
            Remove(projectile.gameObject);Remove(victim);if(damage>0)React(target);
            Sound(school==SpellSchool.Fire?fireSound:school==SpellSchool.Frost?frostSound:arcaneSound);
            yield return Burst(destination,SpriteFor(school),Tint(school),damage,.65f);
        }
        IEnumerator Sweep(List<Target> targets,SpellSchool school)
        {foreach(var target in targets){var view=View(target);if(view!=null)StartCoroutine(Burst(view.position,SpriteFor(school),Tint(school),0,.8f));yield return new WaitForSeconds(.08f);}yield return new WaitForSeconds(.8f);}
        IEnumerator Burst(Vector3 position,Sprite sprite,Color tint,int amount,float duration)
        {
            var image=Image("NativeImpact",sprite,position,155,tint);TMP_Text number=null;
            if(amount>0)
            {
                var go=new GameObject("ImpactDamage",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(overlay,false);temporary.Add(go);
                number=go.GetComponent<TMP_Text>();number.font=font;number.fontSharedMaterial=numberMaterial;number.text="-"+amount;number.fontSize=43;number.alignment=TextAlignmentOptions.Center;number.raycastTarget=false;
                number.rectTransform.sizeDelta=new Vector2(130,65);number.rectTransform.position=position;
            }
            float t=0;while(t<duration)
            {
                t+=Time.deltaTime;float p=Mathf.Clamp01(t/duration);image.rectTransform.localScale=Vector3.one*Mathf.Lerp(.45f,1.35f,p);
                image.color=new Color(tint.r,tint.g,tint.b,Mathf.Sin(Mathf.PI*p));
                if(number!=null){number.rectTransform.position=position+Vector3.up*p*30;number.color=new Color(1,1,1,1-p*p);}yield return null;
            }
            Remove(image.gameObject);if(number!=null)Remove(number.gameObject);
        }
        void OnDestroy(){ResetEffects();}
    }
}
