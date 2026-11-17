using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace HearthstoneClone.UI
{
    public class NativeChimneyInteraction : MonoBehaviour
    {
        public Camera boardCamera;
        public SphereCollider clickArea;
        public ParticleSystem puff, ambientSmoke;
        public AudioSource voice;
        public AudioClip[] sounds;
        bool smokeStopped;
        float nextClick;
        public int ClickCount {get;private set;}
        public bool SmokeStopped => smokeStopped;
        void OnEnable()
        {
            smokeStopped=false;nextClick=0;ClickCount=0;
            // Our listener is on the elevated board camera. A one-unit near radius
            // reduces this board click by ~25 dB before it reaches that listener.
            // Keep its 3D placement/pan, but put the board inside the full-volume radius.
            if(voice!=null&&boardCamera!=null)voice.minDistance=Mathf.Max(1,Vector3.Distance(boardCamera.transform.position,voice.transform.position)+.01f);
            if(ambientSmoke!=null)ambientSmoke.Play(false);
        }
        void Update()
        {
            var mouse=Mouse.current;
            if(mouse==null||!mouse.leftButton.wasPressedThisFrame)return;
            if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
            TryClick(mouse.position.ReadValue());
        }
        public bool TryClick(Vector2 screenPosition)
        {
            if(!isActiveAndEnabled||Time.time<nextClick||boardCamera==null||clickArea==null)return false;
            if(!clickArea.Raycast(boardCamera.ScreenPointToRay(screenPosition),out _,100))return false;
            // Original Hut FSM: alternate puff+stop stack / puff+play stack, with a
            // .2-second Wait gate and the source state's sound pitch range.
            puff.Play(true);
            if(!smokeStopped)ambientSmoke.Stop(false,ParticleSystemStopBehavior.StopEmitting);
            else ambientSmoke.Play(false);
            if(voice!=null&&sounds!=null&&sounds.Length>0)
            {
                voice.pitch=smokeStopped?Random.Range(.8f,1.1f):Random.Range(1f,1.2f);
                voice.PlayOneShot(sounds[Random.Range(0,sounds.Length)]);
            }
            smokeStopped=!smokeStopped;nextClick=Time.time+.2f;ClickCount++;return true;
        }
        void OnDisable()
        {
            if(puff!=null)puff.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(ambientSmoke!=null)ambientSmoke.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(voice!=null)voice.Stop();
        }
    }
}
