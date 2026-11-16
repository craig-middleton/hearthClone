using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;

// Disposable paused Play-mode source-motion inspection. Never save the scene.
public static class PreviewOriginalCardDraw
{
    public static string Main(float elapsed = .9f)
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play mode only");
        EditorApplication.isPaused = false;
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        game.frontEnd.Play(); game.confirmMulliganButton.onClick.Invoke();
        EditorApplication.isPaused = true;
        var motion = game.handDisplay.drawPresentation;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var flights = (IDictionary)typeof(NativeCardDrawPresentation).GetField("flights", flags).GetValue(motion);
        if (flights.Count != 1) throw new Exception("Expected first-turn source draw, found " + flights.Count);
        var f = flights.Values.Cast<object>().Single();
        typeof(NativeCardDrawPresentation).GetMethod("Sample", flags).Invoke(motion, new[] { f, (object)elapsed });
        Canvas.ForceUpdateCanvases();
        var root = (GameObject)f.GetType().GetField("root").GetValue(f);
        return "Paused original draw at " + elapsed + "s; source position=" + root.transform.position + "; screen=" + Screen.width + "x" + Screen.height + "; graphic count=" + root.GetComponentsInChildren<UnityEngine.UI.Graphic>().Length;
    }
}
