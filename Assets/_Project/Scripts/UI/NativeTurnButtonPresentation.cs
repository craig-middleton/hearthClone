using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HearthstoneClone.UI
{
    public class NativeTurnButtonPresentation : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public EffectTester game;
        public GameObject model;
        public Animation animator;
        public Renderer paintRenderer,greenHighlight;
        Material paint,glow;
        bool flipping,green,hover,timeoutAnimating,timeoutPoseWaiting,turnEndedDuringTimeout;
        public bool IsTransitioning => flipping;
        void Awake(){paint=paintRenderer.material;glow=greenHighlight.material;}
        void OnEnable()
        {
            // A menu/mulligan can hide the button halfway through its native flip.
            // A fresh match must restore the ready pose rather than freeze that frame.
            animator.Stop();animator.transform.localPosition=Vector3.zero;animator.transform.localRotation=Quaternion.identity;
            timeoutAnimating=false;timeoutPoseWaiting=false;turnEndedDuringTimeout=false;green=false;hover=false;paint.mainTextureOffset=Vector2.zero;greenHighlight.enabled=false;model.SetActive(true);
        }
        void OnDisable(){StopAllCoroutines();flipping=false;hover=false;if(model!=null)model.SetActive(false);}
        void OnDestroy(){if(paint!=null)Destroy(paint);if(glow!=null)Destroy(glow);}
        void LateUpdate()
        {
            if(flipping||game.faceView.Player==null)return;
            bool next=!game.IsOpponentTurn&&!game.HasVisibleActions;
            if(next!=green){green=next;Play(green?"ENDTURN_NO_MORE_PLAYS":"ENDTURN_YOUR_TURN");paint.mainTextureOffset=green?new Vector2(0,.5f):Vector2.zero;}
            greenHighlight.enabled=green||hover;
        }
        public void TimerElapsed(NativeTurnTimerLibrary source)
        {
            StopAllCoroutines();animator.Stop();timeoutAnimating=true;timeoutPoseWaiting=true;
            turnEndedDuringTimeout=false;hover=false;green=false;flipping=true;
            StartCoroutine(Timeout(source));
        }
        IEnumerator Timeout(NativeTurnTimerLibrary source)
        {
            float time=0;
            while(time<source.buttonTimeoutDuration)
            {
                time=Mathf.Min(source.buttonTimeoutDuration,time+Time.unscaledDeltaTime);
                NativeTurnTimerLibrary.Apply(source.timeoutButton,animator.transform,time);
                paint.mainTextureOffset=new Vector2(source.buttonAtlasX.Evaluate(time),source.buttonAtlasY.Evaluate(time));
                greenHighlight.enabled=source.buttonHighlight.Evaluate(time)>.5f;
                yield return null;
            }
            timeoutAnimating=false;
            if(turnEndedDuringTimeout){timeoutPoseWaiting=false;yield return Flip(true);}
        }
        public void TurnEnded()
        {
            if(timeoutAnimating){turnEndedDuringTimeout=true;return;}
            StopAllCoroutines();hover=false;bool skipWaiting=timeoutPoseWaiting;timeoutPoseWaiting=false;
            StartCoroutine(Flip(skipWaiting));
        }
        IEnumerator Flip(bool skipWaiting=false)
        {
            flipping=true;green=false;greenHighlight.enabled=false;paint.mainTextureOffset=Vector2.zero;
            if(!skipWaiting&&(!game.manualControlMode||game.IsOpponentTurn))
            {
                Play("ENDTURN_WAITING");yield return new WaitForSeconds(.65f);
                while(game.ActionEffectsBusy)yield return null;
            }
            if(!game.IsOpponentTurn)
            {
                Play("ENDTURN_YOUR_TURN");
                // Hover/green clips have no root flip channel. Letting them stop this
                // return clip can strand its last sampled root pose halfway side-on.
                var returning=animator["ENDTURN_YOUR_TURN"];
                if(returning!=null)yield return new WaitForSeconds(returning.length);
            }
            flipping=false;
        }
        void Play(string clip){var state=animator[clip];if(state==null)return;state.wrapMode=WrapMode.ClampForever;animator.Play(clip,PlayMode.StopAll);}
        bool Ready=>!flipping&&GetComponent<Button>().interactable;
        public void OnPointerEnter(PointerEventData e){if(Ready){hover=true;Play(green?"ENDTURN_NO_MORE_PLAYS_MOUSE_OVER":"ENDTURN_YOUR_TURN_MOUSE_OVER");}}
        public void OnPointerExit(PointerEventData e){hover=false;if(Ready)Play(green?"ENDTURN_NMP_MOUSE_OVER_2_NMP":"ENDTURN_YT_MOUSE_OVER_2_YT");}
        public void OnPointerDown(PointerEventData e){if(Ready&&e.button==PointerEventData.InputButton.Left)Play("ENDTURN_PRESSED_DOWN");}
        public void OnPointerUp(PointerEventData e){if(Ready&&e.button==PointerEventData.InputButton.Left)Play("ENDTURN_PRESSED_UP");}
    }
}
