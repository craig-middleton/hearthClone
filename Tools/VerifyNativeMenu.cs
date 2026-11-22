using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class VerifyNativeMenu
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Click(Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    static async Task Until(Func<bool> condition,int milliseconds,string message)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();
        while(!condition()){if(timer.ElapsedMilliseconds>milliseconds)throw new Exception(message);await Task.Delay(25);}
    }
    static void FirstHit(Button button)
    {
        var hits=new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=button.transform.position},hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Original menu button covered: "+button.name);
    }
    public static async Task<string> Main()
    {
        Check(EditorApplication.isPlaying,"Play mode required");
        var runtimeMeshErrors=new List<string>();
        Application.LogCallback observer=(message,trace,type)=>
        {
            if(type==LogType.Error||type==LogType.Exception)
                if(message.IndexOf("mesh",StringComparison.OrdinalIgnoreCase)>=0&&
                  (message.IndexOf("isReadable",StringComparison.OrdinalIgnoreCase)>=0||message.IndexOf("not readable",StringComparison.OrdinalIgnoreCase)>=0))runtimeMeshErrors.Add(message);
        };
        Application.logMessageReceived+=observer;
        try
        {
        await Task.Delay(150);
        var game=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var front=game.frontEnd;
        front.ShowMenu();var menu=front.mainMenu.GetComponent<NativeMenuPresentation>();
        Check(menu!=null&&menu.menuRoot!=null&&menu.menuCamera!=null,"Native menu not installed");
        Check(menu.slots!=null&&menu.slots.Length==2&&menu.slots.All(slot=>slot.sourcePoints!=null&&slot.sourcePoints.Length>0&&slot.sourcePoints.Length==slot.source.GetComponent<MeshFilter>().sharedMesh.vertexCount),"Original menu slot points were not saved by the Edit-mode installer");
        Check(menu.originalCameraClip!=null&&menu.cameraChannels.Length==7,"Complete original camera clip channels missing");
        Check(menu.menuCamera.cullingMask==(1<<30)&&menu.menuRoot.GetComponentsInChildren<AudioListener>(true).Length==0,"Native menu camera/audio isolation failed");
        Check(menu.menuRoot.GetComponentsInChildren<Transform>(true).Length>=242,"Original TheBox transform hierarchy incomplete");
        Check(menu.menuRoot.GetComponentsInChildren<MonoBehaviour>(true).All(b=>b is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData),"Original client behaviour imported into menu root");
        await Until(()=>menu.IntroStarted,1500,"Original intro startup callback did not run");
        if(menu.IntroPlaying&&menu.IntroTime<menu.originalCameraClip.length-.2f)
        {
            var before=menu.menuCamera.transform.localPosition;float beforeTime=menu.IntroTime;await Task.Delay(150);
            Check(menu.IntroTime>beforeTime&&Vector3.Distance(before,menu.menuCamera.transform.localPosition)>.001f,"Original intro camera did not advance in real frames");
        }
        await Until(()=>!menu.IntroPlaying,7000,"Original menu camera intro did not complete");await Task.Delay(50);
        Check(Vector3.Distance(menu.menuCamera.transform.localPosition,menu.restingCameraPosition)<.001f,"Source resting camera pose not restored");
        Check(menu.menuRoot.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.isSupported)),"Unsupported original menu material still visible");
        var disk=menu.menuRoot.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="TheBox_CenterDisk_mesh");
        Check(disk.enabled&&disk.sharedMaterials.Any(m=>m.HasProperty("_Color")&&m.GetColor("_Color").a>.9f),"Opaque original centre disk was disabled with its glow submesh");
        FirstHit(front.playButton);FirstHit(front.chooseDeckButton);
        foreach(var slot in menu.slots)
        {
            var screen=menu.menuCamera.WorldToScreenPoint(slot.source.bounds.center);
            Check(Vector2.Distance(screen,slot.hitbox.position)<12,"Original source slot/input alignment failed");
        }
        Click(front.chooseDeckButton);await Task.Delay(50);
        Check(front.deckScreen.activeInHierarchy&&!menu.menuRoot.activeInHierarchy,"Choose Deck did not hide source menu model/camera");
        Check(!menu.menuCamera.isActiveAndEnabled,"Deck builder retained an active menu camera");
        Click(front.backButton);await Task.Delay(50);
        Check(front.mainMenu.activeInHierarchy&&menu.menuRoot.activeInHierarchy&&!menu.IntroPlaying,"Menu return replayed intro or failed to show source model");
        Click(front.playButton);await Task.Delay(50);
        Check(front.tableSurface.activeInHierarchy&&game.mulliganPanel.gameObject.activeInHierarchy&&!menu.menuRoot.activeInHierarchy,"Play did not start real fresh match and hide source menu");
        Check(!menu.menuCamera.isActiveAndEnabled,"Match retained active menu camera");
        front.ShowMenu();await Task.Delay(50);FirstHit(front.playButton);
        Check(runtimeMeshErrors.Count==0,"Native menu accessed non-readable runtime mesh data: "+string.Join("; ",runtimeMeshErrors));
        return "Original TheBox geometry/camera clip, isolated rendering/audio, source resting pose, source button projection/raycast, Choose Deck/back, fresh-match mulligan and repeated menu lifecycle passed. Shader bloom/gloss and unsupported scrolling FX remain compatibility gaps.";
        }
        finally{Application.logMessageReceived-=observer;}
    }
}
