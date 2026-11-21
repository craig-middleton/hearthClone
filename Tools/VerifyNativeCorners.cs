using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Run in Play mode with auto-tick enabled. Main, Zeppelin and Catapult are separate
// entry points so no single verification request needs an unbounded wait.
public static class VerifyNativeCorners
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> predicate,int milliseconds,string message)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();
        while(!predicate()){if(timer.ElapsedMilliseconds>milliseconds)throw new Exception(message);await Task.Delay(20);}
    }
    static Transform At(NativeCornerInteractions c,string relative)
    {
        var t=c.sourceRoot.Find(relative);Check(t!=null,"Source object missing: "+relative);return t;
    }
    static Vector2 Point(NativeCornerInteractions c,string path,bool checkUi=true)
    {
        var collider=At(c,path).GetComponent<Collider>();Check(collider!=null&&collider.enabled,"Source collider disabled/missing: "+path);
        Vector2 point=c.boardCamera.WorldToScreenPoint(collider.bounds.center);
        Check(point.x>0&&point.x<Screen.width&&point.y>0&&point.y<Screen.height,"Source collider outside game view: "+path+" @"+point);
        Check(collider.Raycast(c.boardCamera.ScreenPointToRay(point),out _,100),"Projected point misses source collider: "+path);
        if(checkUi)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Check(hits.Count==0,"Corner click intercepted by UI: "+path+" / "+string.Join(",",hits.Select(h=>h.gameObject.name)));
        }
        return point;
    }
    static void Click(NativeCornerInteractions c,string corner,string colliderPath)
    {
        int before=c.ClicksOf(corner);Check(c.HandlePointer(Point(c,colliderPath),true,false),corner+" did not consume projected source click");
        Check(c.ClicksOf(corner)==before+1,corner+" did not count exactly one click");
    }
    static void Sound(NativeCornerInteractions c,string parent)
    {
        var sources=At(c,parent).GetComponentsInChildren<AudioSource>(true).Where(s=>s.isPlaying).ToArray();
        Check(sources.Length>0,"Source click sound did not start: "+parent);
        foreach(var s in sources)Check(s.minDistance>=Vector3.Distance(c.boardCamera.transform.position,s.transform.position),"Corner click attenuated before reaching camera listener");
    }
    sealed class Motion
    {
        readonly Transform[] targets;readonly Vector3[] positions,scales;readonly Quaternion[] rotations;
        public Motion(Transform root)
        {
            targets=root.GetComponentsInChildren<Transform>(true);positions=targets.Select(t=>t.localPosition).ToArray();rotations=targets.Select(t=>t.localRotation).ToArray();scales=targets.Select(t=>t.localScale).ToArray();
        }
        public bool Changed()=>targets.Where((t,i)=>(t.localPosition-positions[i]).sqrMagnitude>1e-8f||Quaternion.Angle(t.localRotation,rotations[i])>.01f||(t.localScale-scales[i]).sqrMagnitude>1e-8f).Any();
    }
    static async Task<(EffectTester e,NativeCornerInteractions c)> Start()
    {
        await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();Check(e!=null,"Match fixture missing");
        e.frontEnd.ShowMenu();await Task.Delay(50);e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
        await Until(()=>!e.ActionEffectsBusy&&!e.handDisplay.drawPresentation.IsBusy,10000,"Opening presentation did not finish");
        var c=UnityEngine.Object.FindAnyObjectByType<NativeCornerInteractions>();Check(c!=null&&c.library!=null&&c.library.machines.Length==6,"Six original corner machines not installed");
        Check(c.TotalClicks==0,"New match retained old corner click counts");
        foreach(var m in c.library.machines)foreach(var state in m.states)foreach(var a in state.actions)
        {
            if(a.type.StartsWith("Audio"))Check(a.sounds!=null&&a.sounds.Length>0&&a.sounds.All(s=>s!=null)&&a.weights.Length==a.sounds.Length,"Source audio binding incomplete: "+m.path+"/"+state.name);
        }
        var systems=c.resetPoses.SelectMany(p=>p.target.GetComponents<ParticleSystem>()).ToArray();Check(systems.Length==8,"Expected eight original corner emitters");
        foreach(var p in systems)
        {
            var r=p.GetComponent<ParticleSystemRenderer>();Check(r!=null&&r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported&&r.sharedMaterial.mainTexture!=null,"Corner particle shader or original paint missing: "+p.name);
        }
        return(e,c);
    }
    static async Task Finish(EffectTester e,NativeCornerInteractions c)
    {
        e.frontEnd.ShowMenu();Check(!c.isActiveAndEnabled,"Menu left original corner runtime active");
        foreach(var p in c.resetPoses)
        {
            Check((p.target.localPosition-p.position).sqrMagnitude<1e-8f&&Quaternion.Angle(p.target.localRotation,p.rotation)<.01f&&(p.target.localScale-p.scale).sqrMagnitude<1e-8f,"Menu did not restore original corner pose: "+p.target.name);
            foreach(var ps in p.target.GetComponents<ParticleSystem>())Check(!ps.IsAlive(false),"Menu left source particles alive: "+ps.name);
        }
        Check(c.sourceRoot.GetComponentsInChildren<AudioSource>(true).All(s=>!s.isPlaying),"Menu left corner audio playing");
        e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();await Task.Delay(30);
        Check(c.TotalClicks==0&&c.library.machines.All(m=>c.StateOf(m.path.Split('/').Last())==m.start),"New match did not restart every source state/click gate");
    }
    public static async Task<string> Main()
    {
        var(e,c)=await Start();
        const string gate="Clickables/TowerGate/Gate_Collision";
        var gateMotion=new Motion(At(c,"Clickables/TowerGate/GateTowers_GateRoot_mesh"));
        Click(c,"TowerGate",gate);Check(c.StateOf("TowerGate")=="State 2","First gate click should play source wiggle");Sound(c,"Clickables/TowerGate");
        await Until(gateMotion.Changed,500,"Original gate wiggle did not move its model");
        await Until(()=>c.StateOf("TowerGate")=="State 1",1000,"Gate wiggle never returned to click state");
        int count=1;
        while(count<9)
        {
            Click(c,"TowerGate",gate);count++;
            if(c.StateOf("TowerGate")=="State 3")break;
            await Until(()=>c.StateOf("TowerGate")=="State 1",1000,"Gate poke got stuck");
        }
        Check(count>=5&&count<=9&&c.StateOf("TowerGate")=="State 3","Gate did not use source repeated-click threshold5–9");
        await Until(()=>At(c,"Clickables/TowerGate/FX_Dust1").GetComponentsInChildren<ParticleSystem>().Any(p=>p.particleCount>0),1000,"Original closing gate dust/pebbles did not emit after source start delay");
        await Until(()=>c.StateOf("TowerGate")=="State 4",1000,"Gate did not wait for reopening click");
        var reopen=new Motion(At(c,"Clickables/TowerGate/GateTowers_GateRoot_mesh"));Click(c,"TowerGate",gate);
        Check(c.StateOf("TowerGate")=="State 5","Next gate click did not start original open clip");
        await Until(reopen.Changed,500,"Original gate open did not move model");await Until(()=>c.StateOf("TowerGate")=="State 1",1000,"Gate open failed to return");

        var spireMotion=new Motion(At(c,"ORG_Spire"));Click(c,"RockSpire","Clickables/RockSpire");
        Check(c.StateOf("RockSpire")=="Close State","First spire click did not use original poke state");Sound(c,"Clickables/RockSpire");
        await Until(spireMotion.Changed,500,"Original spire poke did not move model");
        await Until(()=>c.StateOf("RockSpire")=="State 1",1000,"Spire .1-second source wait did not release input");
        bool rocked=false;int rocks=1;
        while(rocks<9)
        {
            Click(c,"RockSpire","Clickables/RockSpire");rocks++;
            if(c.StateOf("RockSpire")=="Open State"){rocked=true;break;}
            await Until(()=>c.StateOf("RockSpire")=="State 1",1000,"Spire poke got stuck");
        }
        Check(rocked&&rocks>=5&&rocks<=9,"Spire did not trigger original rocking branch after5–9 clicks");
        await Until(()=>At(c,"Clickables/RockSpire/FX_Pebbles1").GetComponent<ParticleSystem>().particleCount>0,1000,"Original spire pebbles missing after source start delay");

        var flag=At(c,"ORG_Hut_Flag").GetComponent<Animation>();var flagMotion=new Motion(flag.transform);
        Click(c,"Hut_Flag","Clickables/Hut_Flag");Check(c.StateOf("Hut_Flag")=="State 2"&&flag.IsPlaying("ORG_HutFlagClicked1"),"Flag click did not play source clip");Sound(c,"Clickables/Hut_Flag");
        Check(!c.HandlePointer(Point(c,"Clickables/Hut_Flag"),true,false),"Flag accepted another click during its source reaction");
        await Until(flagMotion.Changed,1000,"Original flag bones did not move");
        await Until(()=>c.StateOf("Hut_Flag")=="State 1",5500,"Flag reaction never returned to idle");Check(flag.IsPlaying("ORG_HutFlagIdle"),"Flag idle loop did not resume");

        var thorns=At(c,"ORG_GateTower/ORG_GateTowerThornsSkinned").GetComponent<Animation>();
        var thornMotion=new Motion(thorns.transform);Click(c,"Thorns","Clickables/Thorns");
        Check(thorns.IsPlaying("ORG_ThornsClickedClose"),"Thorns poke did not play source clip");Sound(c,"Clickables/Thorns");
        await Until(thornMotion.Changed,500,"Original thorn bones did not move");
        await Until(()=>c.StateOf("Thorns")=="State 1",1000,"Thorns source .01 wait did not return");
        int thornClicks=1;bool reacted=false;
        while(thornClicks<9)
        {
            Click(c,"Thorns","Clickables/Thorns");thornClicks++;
            if(c.StateOf("Thorns")=="State 2"){reacted=true;break;}
            await Until(()=>c.StateOf("Thorns")=="State 1",1000,"Thorns poke got stuck");
        }
        Check(reacted&&thornClicks>=5&&thornClicks<=9&&thorns.IsPlaying("ORG_ThornsClicked1"),"Thorns original repeated-click reaction missing");
        await Until(()=>At(c,"Clickables/Thorns/FX_WoodChip1").GetComponent<ParticleSystem>().particleCount>0,1000,"Original wood chips did not emit at click after source start delay");
        await Until(()=>c.StateOf("Thorns")=="State 1",3000,"Thorn reaction failed to finish");
        await Finish(e,c);
        return "Source screen colliders/UI clearance, gate wiggle/close/open+dust, spire poke/rock+pebbles, flag click/idle bones, thorn poke/reaction+wood chips, original audio/near radius and menu/reset passed; gate="+count+", spire="+rocks+", thorns="+thornClicks+" clicks.";
    }
    public static async Task<string> Zeppelin()
    {
        var(e,c)=await Start();var animation=At(c,"ORG_Zepp").GetComponent<Animation>();Check(animation.IsPlaying("ORG_ZepIdle"),"Original zeppelin idle missing");
        int clicks=0;
        while(clicks<11)
        {
            var motion=new Motion(animation.transform);Click(c,"Zepp","ORG_Zepp");clicks++;
            Sound(c,"Clickables/Zepp/Zeppelin Sound");
            if(c.StateOf("Zepp")=="State 2")
            {
                Check(clicks>=6&&animation.IsPlaying("ORG_ZepClicked1"),"Major zeppelin source reaction occurred at wrong count/clip");
                await Until(motion.Changed,1000,"Original major zeppelin reaction did not move bones");break;
            }
            Check(c.StateOf("Zepp")=="State 3"&&animation.IsPlaying("ORG_ZepClickedClosed"),"Zeppelin poke branch does not use original two-second clip");
            await Until(motion.Changed,1000,"Original zeppelin poke did not move bones");
            await Until(()=>c.StateOf("Zepp")=="State 1",3000,"Zeppelin poke source wait did not finish");
        }
        Check(clicks>=6&&clicks<=11&&c.StateOf("Zepp")=="State 2","Zeppelin did not react after source6–11 clicks");
        await Until(()=>c.StateOf("Zepp")=="State 1",8000,"Six-second source zeppelin reaction did not finish");Check(animation.IsPlaying("ORG_ZepIdle"),"Zeppelin idle failed to resume");
        await Finish(e,c);return "Original zeppelin screen hit, idle/poke/major animation bones, sound groups, source6–11-click gate (observed "+clicks+"), animation completion and menu/reset passed.";
    }
    sealed class TestMouse:IDisposable
    {
        readonly Mouse previous,device;readonly InputSettings settings;
        readonly InputSettings.EditorInputBehaviorInPlayMode originalBehavior;bool pressed;
        Vector2 observedPosition;bool observedPressed;int dynamicUpdates;
        public TestMouse()
        {
            // Replacing transient default settings destroys the previous instance in
            // this InputSystem version. Change one property in place and restore it.
            previous=Mouse.current;settings=InputSystem.settings;originalBehavior=settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            device=InputSystem.AddDevice<Mouse>("NativeCornerVerificationMouse");InputSystem.onAfterUpdate+=AfterUpdate;device.MakeCurrent();
        }
        void AfterUpdate()
        {
            device.MakeCurrent();
            // Async editor-script continuations can read the separate Editor
            // buffer. Observe the same Dynamic buffer used by MonoBehaviour.Update.
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
            observedPosition=device.position.ReadValue();observedPressed=device.leftButton.isPressed;dynamicUpdates++;
        }
        public async Task Move(Vector2 point)
        {
            int before=dynamicUpdates;bool expectedPressed=pressed;
            InputSystem.QueueStateEvent(device,new MouseState{position=point}.WithButton(MouseButton.Left,pressed));
            await Until(()=>dynamicUpdates>before&&(observedPosition-point).sqrMagnitude<1&&observedPressed==expectedPressed,1200,"Synthetic pointer was not consumed by a Dynamic input update");
            await Task.Delay(60);
        }
        public async Task Down(Vector2 point){pressed=true;await Move(point);}
        public async Task Up(Vector2 point){pressed=false;await Move(point);}
        public void Dispose()
        {
            InputSystem.onAfterUpdate-=AfterUpdate;
            try{if(device.added)InputSystem.RemoveDevice(device);}
            finally
            {
                if(settings!=null)settings.editorInputBehaviorInPlayMode=originalBehavior;
                if(previous!=null&&previous.added)previous.MakeCurrent();
            }
        }
    }
    public static async Task<string> Catapult()
    {
        var(e,c)=await Start();const string pile="Clickables/Catapult/Rockpile_Collision",basket="Catapult_Root/ORG_Catapult/CatapultArmRoot_mesh/polySurface68/Basket_Collision";
        var rock=At(c,"Clickables/Catapult/RockPickup");var model=At(c,"Catapult_Root/ORG_Catapult").GetComponent<Animation>();
        using var mouse=new TestMouse();Vector2 pickup=Point(c,pile);await mouse.Move(pickup);await mouse.Down(pickup);Check(c.StateOf("Catapult")=="State 2","Catapult pickup immediately cancelled/stuck");
        Check(rock.GetComponent<Renderer>().enabled,"Source dragged rock was not shown");Sound(c,"Clickables/Catapult");
        await mouse.Move(new Vector2(10,10));await Until(()=>c.StateOf("Catapult")=="State 1",1200,"Leaving source catapult bounds did not cancel/rearm pickup");Check(!rock.GetComponent<Renderer>().enabled,"Cancelled source rock remained visible");await mouse.Up(pickup);

        await mouse.Move(pickup);await mouse.Down(pickup);Check(c.StateOf("Catapult")=="State 2","Second pickup failed after cancellation");
        Vector2 loading=Point(c,basket);await mouse.Move(loading);Check(c.StateOf("Catapult")=="State 2","Dragging to source basket left catapult bounds");
        Check((rock.position-c.boardCamera.ScreenToWorldPoint(new Vector3(loading.x,loading.y,18))).sqrMagnitude<.01f,"Dragged rock did not follow source screen depth18");
        await mouse.Up(loading);Check(c.StateOf("Catapult") is "State 4" or "State 5","Source basket did not accept mouse release");await Until(()=>c.StateOf("Catapult")=="State 5",1500,"Original rock-fall loading animation did not finish");
        Check(!rock.GetComponent<Renderer>().enabled&&At(c,"Catapult_Root/ORG_Catapult/CatapultArmRoot_mesh/Sphere").GetComponent<Renderer>().enabled,"Loaded/dragged rock visibility wrong");
        var launchMotion=new Motion(model.transform);Vector2 launch=Point(c,"Catapult_Root");await mouse.Move(launch);await mouse.Down(launch);Check(model.IsPlaying("ORG_CattapultLaunch"),"Catapult did not play original launch clip");Sound(c,"Clickables/Catapult");await mouse.Up(launch);
        await Until(launchMotion.Changed,500,"Original catapult launch did not move arm");await Until(()=>c.StateOf("Catapult")=="State 7",1500,"Original launch .5 wait did not reach winch state");
        await Task.Delay(300);var arm=At(c,"Catapult_Root/ORG_Catapult/CatapultArmRoot_mesh");
        for(int i=0;i<6;i++)
        {
            var rotation=arm.localRotation;await mouse.Down(launch);Check(c.StateOf("Catapult")=="State 8","Source winch opened before seventh click");await mouse.Up(launch);
            await Until(()=>c.StateOf("Catapult")=="State 7",1000,"Original winch .25-second easeOutBounce never finished");
            Check(Mathf.Abs(Quaternion.Angle(rotation,arm.localRotation)-6.25f)<.15f,"Original winch step was not6.25 degrees");
        }
        await mouse.Down(launch);await mouse.Up(launch);await Until(()=>c.StateOf("Catapult")=="State 1",1000,"Seventh winch click did not lock/rearm catapult");
        Check(!At(c,"Catapult_Root/ORG_Catapult/CatapultArmRoot_mesh/Sphere").GetComponent<Renderer>().enabled,"Source launch left loaded rock visible");
        await Finish(e,c);return "Original catapult screen pickup/drag depth18, out-of-bounds cancel, basket mouse release, rock-fall/load, launch/audio, six6.25-degree native bounce winch steps plus seventh lock, menu/reset passed.";
    }
}
