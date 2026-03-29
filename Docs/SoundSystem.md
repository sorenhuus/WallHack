# Sound System Design

## Core Principle
Sound events are server-authoritative but feel instant for the local player,
mirroring how movement works (client-side prediction + server authority).

## Architecture

### Local Player (client-side)
- Plays own sounds **immediately** on action (footstep, gunshot, etc.)
- No network round-trip — feels snappy

### Remote Players (server-authoritative)
- Server detects actions based on its simulation
- Server fires `RpcPlaySound(soundType, position)` to all **other** clients
- Clients spawn a temporary AudioSource at the given world position and play the clip
- Position is the server's authoritative position — correct even when not visually synced

### Why server-authoritative for remote players?
- Cheaters cannot suppress their own footsteps or fake sounds
- Sound position is always the true server position, not client-reported

---

## Sound Event Types

| Event        | Trigger                                      | Notes                          |
|--------------|----------------------------------------------|--------------------------------|
| Footstep     | Server detects horizontal movement > threshold each tick | Frequency tied to move speed |
| Jump         | Server detects jump input processed          |                                |
| Land         | Server detects grounded transition           |                                |
| Gunshot      | Server validates and processes shot          | Future — when shooting added   |

---

## Implementation Plan

### 1. SoundEvent enum
```csharp
public enum SoundEvent { Footstep, Jump, Land, Gunshot }
```

### 2. SoundManager (MonoBehaviour, scene singleton)
- Holds `AudioClip[]` per SoundEvent type
- `PlayAt(SoundEvent type, Vector3 position)` — spawns temporary AudioSource at position
- Local player calls this directly for own sounds
- Remote clients receive it via RPC

### 3. PlayerMovement changes (server-side)
- Track distance travelled each tick for footstep cadence
- Detect jump and land transitions
- Call `RpcPlaySoundRemote(target, soundEvent, position)` on relevant events
  - `target` = all connections except the observed player's own connection

### 4. RpcPlaySoundRemote (TargetRpc on PlayerMovement)
```
Server → specific client: "play this sound at this position"
```
- Client receives it and calls `SoundManager.PlayAt(type, position)`

### 5. Local sound (PlayerMovement, client-side)
- In `Update()`, local player detects own footstep/jump/land
- Calls `SoundManager.PlayAt(type, transform.position)` directly — no network

---

## Steam Audio Compatibility

Yes — fully compatible. Steam Audio replaces Unity's built-in spatializer but
AudioSources work exactly the same way. To upgrade:

1. Import Steam Audio package
2. Add `Steam Audio Source` component to the temporary AudioSource prefab
3. Enable occlusion on the Steam Audio Source — sounds through walls will be
   automatically muffled based on geometry
4. Add `Steam Audio Listener` to the camera

The event system (Cmd/Rpc) is completely independent of the spatializer,
so switching to Steam Audio requires no changes to networking code.

### Steam Audio benefits for this project
- **Occlusion** — footsteps behind walls sound muffled, not full volume
- **HRTF** — true 3D including height (headphones)
- **Reverb** — indoor/outdoor environments sound different automatically
