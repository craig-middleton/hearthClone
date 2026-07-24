# hearthClone Source Snapshot — Session 10 (2026-07-24)

This folder contains full source for every file Claude actually saw the contents
of during this session. Copy these over your real project files at matching
paths under `Assets/_Project/Scripts/`.

## Included (full current source)
- `AI/AIController.cs` — new this session
- `UI/EffectTester.cs` — updated this session (End Turn button + AI wiring)
- `Cards/PlayerHand.cs`
- `Core/Player.cs`
- `Core/TurnManager.cs`
- `Core/Target.cs`

## NOT included (Claude has not seen full current source this session)
These files are referenced/described in PROJECT_STATUS.md but their exact
current contents were not pasted into this chat, so they could not be
snapshotted:
- `CardData.cs`, `CardEffect.cs`, `DealDamageEffect.cs`
- `Minion.cs`, `Board.cs`, `GameContext.cs`
- `CardView.cs`, `HandDisplay.cs`, `MinionView.cs`, `BoardDisplay.cs`

If you want a fully complete snapshot next time, paste/upload these files
during the session (or at the start of the next one) and ask Claude to
include them in the next handoff package.

See `PROJECT_STATUS.md` (in this same folder) for full project context,
architecture, gotchas, and next steps.
