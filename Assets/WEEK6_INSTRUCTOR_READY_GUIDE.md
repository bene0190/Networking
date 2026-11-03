# Week 6 Instructor Ready Guide
## Netcode for GameObjects – Multiplayer Course

---

> [!IMPORTANT]
> Weeks 1–5 are fully built by the one-click Editor tool.  
> **Do NOT write Week 6 scripts before class** — that's the live-coding session!

---

## ✅ Step 1 — Generate the Week 1–5 Project in ~5 Seconds

1. Open the Unity project in the Editor.
2. Click the menu: **Tools → Netcode Course → Build Weeks 1-5 Complete Project**
3. A dialog appears when it's done. Click **"Great, thanks!"**
4. Unity imports and compiles everything automatically.

That's it. The script creates:
- All materials, the `NetworkPlayer` prefab, the full scene hierarchy
- `NetworkManager` with Unity Transport (WebSockets enabled)
- The Multiplayer Menu UI wired to `MultiplayerMenu.cs`
- The red Damage Zone and the interactive Networked Button
- All spawn points tagged `SpawnPoint`

> [!NOTE]
> You can safely re-run the builder after a crash or PC issue — it deletes and recreates managed objects cleanly.

---

## ✅ Step 2 — Verify Weeks 1–5 in Play Mode

Use **two Editor instances** (or File → Build and Run a second copy) for the best demo.

### Week 1 — Connection UI
| What to test | Expected result |
|---|---|
| Click **Start Host** | Status text says `"Host started!"`. Join Code text updates. |
| Click **Start Client** in a second instance | Second player spawns in the scene. |
| Click **Start Server** | Dedicated server starts (no local player spawns). |

### Week 2 — Spawn Points & Movement
| What to test | Expected result |
|---|---|
| Host and Client connect | Each player spawns at a different `SpawnPoint` (not on top of each other). |
| Press **W A S D** on each machine | Only that machine's own player moves. Other players see movement via `NetworkTransform`. |
| Player faces movement direction | The capsule rotates to face where it's going (ready for Week 6 shooting!). |

### Week 3 — Camera & Colors
| What to test | Expected result |
|---|---|
| Camera in Play Mode | Camera smoothly follows only the LOCAL player. Never jumps to another player's position. |
| Player colors | Host player = Red, first Client = Blue, second Client = Green, etc. Colors are visible on all machines. |

### Week 4 — Health System & Damage Zone
| What to test | Expected result |
|---|---|
| Walk the local player into the **red flat zone** at `(-4, 0, 4)` | The floating health bar above that player shrinks by 20% on **all** connected screens. |
| Walk in and out 5 times | Health bar reaches 0 (dead — respawn hook is ready for Week 6). |
| Try to fake damage from a client | Server blocks it — only `DamageZone.cs` (server-side) can call `TakeDamage()`. |

### Week 5 — RPCs & Validation
| What to test | Expected result |
|---|---|
| Press **Space** | Yellow/orange particle burst plays simultaneously on all clients (look at both windows). |
| Spam **Space** rapidly | Console shows `"Action BLOCKED by Server: Cooldown active"` — cooldown is enforced server-side. |
| Walk onto the **grey cylinder** at `(4, 0, 4)` and press **E** | Red point light above cylinder turns **green** on every connected screen. |

> [!TIP]
> Open **Window → Analysis → Profiler** and enable **Network** to show students the RPC traffic in real time during the Space and E-key demos.

---

## 📋 Week 6 Live-Coding Checklist

The following is intentionally **not built yet** — you will write it live with students.

### 🔵 Part A — Projectile System (Core Week 6 Feature)

- [ ] **`Assets/Scripts/NetworkProjectile.cs`**  
  `NetworkBehaviour` on a projectile.  
  Server moves it forward each frame; `OnTriggerEnter` calls `PlayerHealth.TakeDamage()` then despawns via `NetworkObject.Despawn()`.

- [ ] **`Assets/Prefabs/MagicProjectile.prefab`**  
  Sphere primitive + `NetworkObject` + `NetworkTransform` + `NetworkProjectile`.  
  Must be registered in `NetworkManager → NetworkPrefabs` list.

- [ ] **`Assets/Scripts/PlayerShooting.cs`** (NetworkBehaviour on NetworkPlayer prefab)  
  Owner reads `Input.GetMouseButtonDown(0)`.  
  Sends `[Rpc(SendTo.Server)]` → server spawns `MagicProjectile` at `FirePoint` via `Instantiate` + `NetworkObject.Spawn()`.

- [ ] **`FirePoint`** — Empty child Transform on the NetworkPlayer prefab (tip of the capsule, at `(0, 1, 0.6)`).  
  `PlayerShooting.firePoint` SerializedField wired in the Inspector.

### 🟢 Part B — Health Pack Collectible (Bonus if time allows)

- [ ] **`Assets/Scripts/NetworkCollectible.cs`**  
  Server-side `OnTriggerEnter` → calls `PlayerHealth` to restore HP, then `NetworkObject.Despawn()`.

- [ ] **`Assets/Prefabs/HealthPack.prefab`**  
  Cube + `NetworkObject` + `NetworkCollectible`.  
  Place a few in the scene and add to `NetworkPrefabs`.

### 🟡 Discussion Points to Raise Live

- **Lag compensation** — why does the projectile appear to start at the server position, not the client's gun tip?
- **NetworkObject pooling** — `Instantiate` every shot is expensive; intro to `NetworkObjectPool`.
- **Client-side prediction** — show students what the movement looks like without `NetworkTransform` to motivate learning prediction.

---

> [!CAUTION]
> Do not pre-build the Week 6 scripts before class — the live-coding experience is the lesson.  
> Students should watch `NetworkObject.Spawn()` called for the first time in the session.

---

*Generated by `Weeks1To5ProjectBuilder.cs` — re-run the menu item any time to rebuild Weeks 1–5 from scratch.*
