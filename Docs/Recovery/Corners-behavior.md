# Original Orgrimmar corner interactions

The playable scene now uses a narrow compatibility implementation of six recovered
PlayMaker state machines, alongside the previously restored chimney. It executes
no original client assemblies. Original models, animation clips and skinning are
already in RecoveredOrgrimmar; native colliders/particles are copied onto that same
hierarchy. The current checkpoint preserves all enabled actions in these machines.

## Provenance

`Assets/_Project/Art/NativeCorners/SourceManifest.json` records source-prefab hash,
component IDs, paths, layers, materials and texture hashes. The wider Interface
export resolves Splinter1 paint absent from the narrow board export; source native
components match between exports after GUID normalization. Eight selected emitters
have complete original material/texture mappings. Unreferenced generic ORG/FX dust
emitters with null materials are excluded. Existing chimney is not duplicated.

`Corners-audio-source.json` follows original UnityFS PPtr references to SoundDef
m_AudioClip, retaining action weights and hashes of48 unchanged desktop WAVs.
SoundDef random pitch/volume multipliers are all1 and random-subclip lists empty;
source FSM action-level pitch/volume ranges therefore govern playback. There are
18 actions, including seven possible catapult flight/impact sounds.

`Tools/AuditNativeCornerFsms.py` uses YAML BaseLoader to preserve binary hex arrays
and original action-enabled flags. `PrepareNativeCornerLogic.py` translates enabled
source parameters into committed `CornerLogic.json`; installer bakes the runtime
ScriptableObject and resolves source audio asset paths. Disabled actions remain
excluded. Regeneration needs the external exports; gameplay/reinstallation uses
committed selected assets/data.

Action behavior was checked by read-only Mono.Cecil metadata/CIL inspection:
Assembly-CSharp MousePickRandomAction/AnimationPlaythroughAction,
Blizzard.T5.PlayMaker.Runtime PlayAnimation/SetPosition/GetMouseX/Y/ScreenToWorldPoint,
and Blizzard.T5.iTween RotateAdd/easeOutBounce. Captures remain under external
recovery/corner-*-il.txt. Original assemblies were not executed.

## State semantics retained

- PlayAnimation waits for its legacy AnimationState to disable (or ClampForever
  time to exceed length), then finishes and sends its configured event.
- AnimationPlaythrough starts/cross-fades the clip and immediately finishes its
  action. Separate source Wait actions control state timing.
- Mouse gate counts persist per action over state re-entry. A new random threshold
  is drawn after an open; reset counts only where source ResetOnOpen is enabled.
  Integer max is exclusive: gate/spire/thorns5–9, zeppelin6–11, catapult winch7.
- Catapult obsolete X/Y.5 FloatCompare actions, several spire stop actions and
  thorns State2 Wait are disabled in source and excluded.
- SetPosition space0 is world,1 local; optional None values do not overwrite axes.
  Mouse uses pixels, ScreenToWorldPoint depth18 and the native board camera.
  ScreenPick uses source default raycast mask; no invented trigger exclusion.
- Winch is source RotateAdd(6.25,0,0), .25sec, Self space, ease24= easeOutBounce.
  The original piecewise bounce formula drives incremental rotation deltas.
- Gate dust starts after original .2sec delay; tests wait for actual emission.
  Flag and zeppelin resume source idle clips. Original source clip events were
  checked absent from the relevant animation components before integration.

## Compatibility limits

The custom FSM adapter, UI-click guard and Ubuntu particle rendering are integration
code. Source particle paint/tints/modules/blends remain (RGB5/10), while the existing
Source Board Particles shader approximates unsupported original compiled shaders;
original lighting/soft-particle behavior is not claimed identical. Moving click audio
keeps its original3D parent but adjusts the full-volume radius to the elevated camera.
The existing chimney ember-material fallback remains separately documented.

Live verification covers source screen hits, model/bone movement, source particles,
audio playback, full catapult loading/firing/winding and menu/reset. Catapult input
uses a disposable synthetic Mouse; the fixture samples Dynamic update state because
Editor continuations may read a separate Editor buffer. Temporary routing changes
are restored without swapping InputSettings: its setter destroys transient defaults.
