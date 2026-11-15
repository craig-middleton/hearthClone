using System.Collections;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // Isolated visual study only. The live match never creates or uses this component.
    public class NativeInterfaceProofMotion : MonoBehaviour
    {
        public Animation buttonAnimation;
        public Renderer buttonRenderer;
        public Renderer greenHighlight;
        public string Phase { get; private set; }
        Material paint;
        Material glowPaint;

        IEnumerator Start()
        {
            paint = buttonRenderer.material;
            if (greenHighlight != null) glowPaint = greenHighlight.material;
            while (true)
            {
                yield return State("Your turn", "ENDTURN_YOUR_TURN", 2f, false);
                yield return State("Press", "ENDTURN_PRESSED_DOWN", .13f, false);
                yield return State("Release", "ENDTURN_PRESSED_UP", .7f, false);
                yield return State("No more plays", "ENDTURN_NO_MORE_PLAYS", 2f, true);
                yield return State("Enemy turn", "ENDTURN_WAITING", 2.5f, false);
            }
        }

        IEnumerator State(string phase, string clip, float duration, bool green)
        {
            Phase = phase;
            var state = buttonAnimation[clip];
            if (state == null) { Debug.LogError("Missing native button clip: " + clip); yield break; }
            state.wrapMode = WrapMode.ClampForever;
            buttonAnimation.Play(clip, PlayMode.StopAll);
            // Preserve the recovered atlas state while replacing its unavailable client shader.
            paint.mainTextureOffset = green ? new Vector2(0, .5f) : Vector2.zero;
            if (greenHighlight != null) greenHighlight.enabled = green;
            yield return new WaitForSeconds(duration);
        }

        void OnDestroy()
        {
            if (paint != null) Destroy(paint);
            if (glowPaint != null) Destroy(glowPaint);
        }
    }
}
