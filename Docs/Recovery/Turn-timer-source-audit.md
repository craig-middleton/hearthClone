# Original turn timer: source audit

26 November 2026. Metadata/serialized-asset inspection only; no original client
assembly or game method was loaded or executed. The original managed DLL was read
through Mono.Cecil using reflection in an ephemeral Unity Editor script.

## Original source locations

External source root:
`/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/Box-complete-source/ExportedProject/Assets`.
Paths below are relative to that root.

- Actor: `AssetBundles/essential_base_global-prefab-0.unity3d/TurnTimer.prefab`.
- Wick mesh: `AssetBundles/essential_base_global-mesh-0.unity3d/TurnTimer_Wick_Skinnedmesh.asset`.
- Spiral mesh: `AssetBundles/essential_base_global-mesh-0.unity3d/TurnTimerSpiral_Mesh.asset`.
- Materials: `AssetBundles/essential_base_global-material-0.unity3d/TurnTimerWick.mat`,
  `TurnTimerWickShadow.mat`, `TurnTimerWickSpiral.mat`.
- Textures: `AssetBundles/essential_base_global-texture-2.unity3d/WickRope.png`,
  `WickRopeSpiral.png`; additional mask/particle textures are referenced by materials.
- Original managed metadata:
  `/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone/Hearthstone_Data/Managed/Assembly-CSharp.dll`.
  Relevant types: TurnTimer, its iterator types, TurnTimerGameModeSettings.

Temporary complete metadata evidence is `/tmp/TurnTimerIL.txt`; normalized FSM and
source transform evidence is `/tmp/rope-source-audit.json`. These are external,
not automatically supplied by a GitHub clone. Source exported script stubs are
empty and do not establish behavior; managed metadata supplies the evidence below.

## Appearance, placement and source bindings

Original actor root is identity: position(0,0,0), rotation identity, scale1. The
TurnTimer methods do not move this root. Preserve the board's native coordinate
mapping; do not independently center/scale the rope to an arbitrary screen line.
No source parent board placement was recovered in this audit. Source
`AssetBundles/essential_base_qualityhigh_global-content-0.unity3d/BoardCameras.prefab`
is identity-rooted with Main Camera position(-7.795346,19.597504,-5.965445) and
quaternion(.68707883,0,0,.7265829). The original board is centered near negative X;
rope coordinates below must use the existing native board's source-world mapping,
not an independent origin-centered screen line. Bone positions in the original
actor's local coordinates:

- `Bones/SparksStartBone`: (-14.243834,.19836211,-4.463869).
- `Bones/SparksFinishBone`: (-2.4254975,0,-4.46).
- `Sparks` starts at the start-bone position.
- `AfterSmoke`: (-1.0353276,-.23229814,-4.341159).

The actor contains a ten-joint skinned wick mesh, separate spiral and shadow, plus
original particles AfterSmoke, Sparks/FX_Sparks, FX_Sparks2, FX_FlareGlow and
FX_Smoke. Sparks and AfterSmoke are separate groups with different lifecycles.
Retain source meshes, bone weights/bind poses, UV1 and UV2, particle modules,
transforms, sizes, lifetimes and material inputs.

Wick uses original shader family `Hero/MaskUV2PanUV1Alpha`. Active declarations are
_Color, _Diffuse, _Mask, _Amount, _Xamount and _LightingBlend. Source diffuse uses
ST scale(10,2), offset(0,.2). The exported shader body is a dummy constant-color
fallback, NOT the original GPU program. Alpha-mask cutoff/UV interpretation and
lighting need disclosed compatibility rendering; names/old unused saved fields
are not evidence for exact shader kernels.

The timer updates BOTH wick and shadow `_Xamount`: start .42, finish -1.5. The
original code uses linear movement from start to finish over remaining rope time,
and linear interpolation of this material value. With fraction = remaining /
countdownDuration, position = Lerp(finishBone,startBone,fraction), and material
value = Lerp(-1.5,.42,fraction). Original iTween EaseType21 is linear. These two
tweens explicitly ignore time scale; remaining time comes from realtimeSinceStartup.

## Original rollout clips, controllers and timing

All controller/modern clips below are in
`AssetBundles/essential_base_global-prefab-0.unity3d/`.

- `TurnTimer_Wick.controller`, state **TurnTimer**, selects **TurnTimer_edit.anim**
  (GUID3f836424f7f3ad44ab8344dfd224185b). Clip duration1s, source state speed2:
  actual wick unroll .5s. Ten original joint position/quaternion tracks.
- Do not substitute legacy `essential_base_global-mesh-0.unity3d/TurnTimer.anim`:
  its endpoints are similar but tangents differ, and the source controller does
  not select it.
- `TurnTimer_Spiral.controller`, state **Unwrap**, speed1, selects
  `TurnTimer_SpiralUnwrap.anim`: duration .71666664s; spiral _Xamount5→-14.49,
  renderer m_Enabled1→0, plus original position curve.
- `TurnTimer_Wick_mat.controller`, state **TurnTimer_Wick_mat**, speed2, selects
  `TurnTimer_Wick_mat.anim`: duration1.5s, actual .75s. The exported float binding
  `script_0x4270A720_vKnpVPJ` is the original TurnTimer.m_FuseXamountAnimation field,
  animated -1.5→.37. Safely bake this as data; do not execute the original script.
- Original TimerBirthAnimateMaterialValues coroutine copies that animated field
  to both wick/shadow materials for1 scaled second after Birth begins. Preserve
  its source curve and clock; do not invent a replacement rope-grow tween.

Never AnimationClip.SampleAnimation the exported mixed bindings. Existing project
history records a native Unity crash from this. Bake and safely evaluate the
specific original channels instead; compose all quaternion components before
normalizing.

## When the rope appears and ends

Serialized `m_RopeCapSeconds` =20. TurnTimer waits until remaining≤20 before Birth;
when a turn update arrives earlier it schedules `remaining−20` using
WaitForSecondsRealtime. It also waits for the original turn-start manager/current
player to be ready. The rollout is NOT played at the start of every full turn.

The original client reads the whole turn timeout from the player's server TIMEOUT
tag and end timestamp from TurnTimerUpdate. It does not hardcode75seconds here.
Serialized debug30 / debugStart20 are debug values, not normal turn rules. A local
single-player turn policy must be documented separately from these asset facts.

FSM flow:

- ready: hide renderer hierarchy.
- Birth→State5: restart Unwrap/TurnTimer/TurnTimer_Wick_mat at0, hide hierarchy,
  wait .01s (scaled), transition to State1.
- State1: reveal hierarchy, restart the same three clips at0; play original rollout
  sounds; wait .87s realtime, transition to State2.
- State2/Idle: begin Sparks group particles and fuse loop, fade fuse in .3s. On
  countdown entry, duration is remaining time AFTER birth. Linear burn continues
  until the true end timestamp.
- Timeout/Death→State3: stop Sparks with children, hide wick with children and
  shadow, stop stinger, fade fuse to0 over .1s and stop it. Play both original
  explosion sounds. Optional timeout FX starts AfterSmoke (includeChildren=false)
  and camera shake: source amount(.6,.6,.6), duration .7s, delay0.
- Cancel→State4: stop Sparks; hide spiral, wick and shadow; stop stinger; fade fuse
  to0 over .4s and stop it. Manual End Turn, current-player changes and game-over
  cancel an active Start/Countdown timer. Cancellation is not an explosion.

Timeout also calls original EndTurnButton.OnTurnTimerEnded and game-entity hook.
Our local game must use its own authoritative end-turn path exactly once; original
client code is not executed or imported as runtime logic.

## Original sounds and default standard settings

Embedded SoundDef strings:

- `Pegasus_Stinger_Fuse_Timer.wav:645aa9a24fb51fe4da19922952dd5af0`.
- `turn_timer_fuse_burns.wav:2e2ea4b8495030e40a5f06761176259e`.
- `Countdown_Tick.wav:d4cbcbff6b2c84843839a2787776a2b8`.
- `Countdown_Tick_Final.wav:e99b87e5dc68bcc4dbe04e8e05190b6a`.

FSM rollout plays original clip GUID33aa7811545222441b7855ff58fda326 with SoundDef
73cbe473817deeb44bbffef6e683ae5c, plus SoundDef3dd0cb2d5492b9f4e9e667a5189bab50.
Both are scaled by RopeRolloutVolume. Timeout plays these two independent layers:

- clip GUIDb42fca3e29553704796713691c2c3fa9 /
  SoundDef96de56233687da14ba85cd62e00f3869;
- clip GUIDcc51c48172cd9214b92aec69fe658d1c /
  SoundDef0ab9303caf48d1548944c391f36c830e.

Their volume variable is EndTurnButtonExplosionVolume. Exact filenames and clips
must be resolved from these source dependencies, not replaced with similar audio.

TurnTimerGameModeSettings constructor defaults, recovered from managed metadata:
PlayMusicStinger=true; RopeFuseVolume=.4; RopeRolloutVolume=1;
EndTurnButtonExplosionVolume=1; PlayTimeoutFx=true; **PlayTickSound=false** (field
not assigned, so CLR defaultfalse). Although tick assets exist, ordinary standard
settings do not play a per-second tick. If another mode enables ticks, each ceil
remaining-second decrement triggers normal tick, with final tick at0.

## End Turn button timeout behavior

Additional managed-metadata evidence is `/tmp/RopeButtonIL.txt`, read through the
same reflection-only Cecil workflow. Original complete EndTurnButton prefab is
`AssetBundles/essential_base_global-prefab-0.unity3d/EndTurnButton.prefab`.

`OnTurnTimerEnded(friendlySide)` immediately returns for the opponent. For the
friendly player it sets the cursor busy and selects actor state23,
ENDTURN_WAITING_TIMER. That source state plays **ENDTURN_YOUR_TIMER_DONE.anim**
(layer0, crossfade0) and **ENDTURN_CollisionDisable.anim** (layer0). Both clips have
source duration1second. The source blocks button input until the actor manager's
maximum active animation time has elapsed (scaled WaitForSeconds).

YourTimerDone is already the expiry-to-waiting motion: its root begins ready and
ends at approximately180degrees with localY=-.33; the final rotation key is at
.8333333s, with the clip held to1s. Original IsInWaitingState includes state23.
Consequently the subsequent opponent-turn UpdateState→SetStateToWaiting returns
without replaying ENDTURN_WAITING. An adapter must preserve this timeout motion
rather than let ordinary turn refresh/hover or its manual End Turn flip interrupt
it, and should not queue a second ordinary waiting flip afterward. On the next
friendly turn, the source normal ENDTURN_YOUR_TURN motion returns the button.

**ENDTURN_TIMER_RELEASE.anim is not referenced by the current EndTurnButton CIL
or prefab actor states.** Its presence among exports does not justify appending
it as a timeout-release phase. Opponent expiry does not play a friendly timeout
clip. Local debug control of the opponent should preserve that presentation rule
unless a deliberate separate manual-control convention is documented.

## Verification limits

This is a read-only source audit, not an installed or visually verified timer. It
does not establish full original GPU shader parity, global camera-shake kernels,
server timer/slush rules, or a recovered source parent board placement. Native
particles/audio/curves should be integrated and checked against actual Game-view
captures and existing end-turn/game-over/restart mechanics before completion is
claimed. No invented explosion/spark motion is needed: the original AfterSmoke,
spark systems, paired end sounds and timeout shake are present.


## Integration measurement and limits

Unity6000.6.9f1 reports ENDTURN_YOUR_TIMER_DONE.length=.85s and CollisionDisable
.length=.8333333s, despite exported stop markers1s. The adapter uses the actual
imported timeout clip length. It safely evaluates source transform/atlas/highlight
curves; it does not use mixed AnimationClip.SampleAnimation. Rope uses original
world origin against the already recovered board camera; inspected screen placement
spans the board toward the button. Mask UV2.x doubling is a compatibility inference
from normalized source UVs, gradient edge~.42 and authored1.92 offset range. Shader
bodies are dummy exports, so this is not a recovered original GPU kernel. Vector tint
avoids introducing an unintended Color-property gamma conversion. Original shadow
geometry is retained but source shadow alignment is not certified. Camera shake and
original mixer/compositor kernels remain unported. Local75-second policy omits
server-driven opening/AFK/card variants.
