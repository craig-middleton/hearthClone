using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using HearthstoneClone.UI;
public static class VerifyMatchMusic
{
 public static async Task<string[]> Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
  var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
  if(e.musicTracks.Count!=10||e.musicTracks.Distinct().Count()!=10||e.musicTracks.Any(c=>c==null||c.length<90))throw new Exception("Invalid ten-song playlist");
  var method=typeof(EffectTester).GetMethod("SetRandomMusic",BindingFlags.NonPublic|BindingFlags.Instance);
  var seen=new System.Collections.Generic.HashSet<AudioClip>();var previous=e.musicSource.clip;
  for(int i=0;i<128;i++){method.Invoke(e,null);var next=e.musicSource.clip;if(next==previous)throw new Exception("Immediate music repeat");seen.Add(next);previous=next;}
  if(seen.Count!=10)throw new Exception("Not all tracks reachable");
  e.frontEnd.Play();var first=e.musicSource.clip;e.frontEnd.ShowMenu();e.frontEnd.Play();var second=e.musicSource.clip;
  if(first==second)throw new Exception("Menu Play did not change music");
  e.playAgainButton.onClick.Invoke();if(e.musicSource.clip==second)throw new Exception("Play Again did not change music");
  await Task.Delay(500);
  if(!e.musicSource.loop||e.musicSource.spatialBlend!=0||!e.musicSource.isPlaying)throw new Exception("Music source is not playing/looping in 2D");
  return new[]{"Ten unique streaming songs, all over 90 seconds","All ten selected across 128 picks, with no consecutive repeat","Actual Menu Play and Play Again both choose a different song; looping 2D playback active"};
 }
}
