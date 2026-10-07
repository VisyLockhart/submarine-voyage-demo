# Submarine Voyage

A small idle game made in Unity: send submarines on timed voyages, collect gold and materials, then upgrade and unlock more boats until the whole fleet is maxed out.

Built as a learning project to get started with Unity. Original design inspired by the submarine voyages in FINAL FANTASY XIV; unofficial, not affiliated with SQUARE ENIX CO., LTD., and no official assets are used.

- Design document (Traditional Chinese): [docs/GDD.md](docs/GDD.md)
- Download: Windows build on the [Releases](https://github.com/VisyLockhart/submarine-voyage-demo/releases) page

## How to play

1. Download `SubmarineVoyage-v1.0.0-windows.zip` from Releases, unzip it, and run `Submarine Voyage.exe` (Windows 10/11, 64-bit).
2. Click **Depart** on an idle submarine and pick a route:

   | Route | Game time | At 60x | Gold | Materials |
   |---|---|---|---|---|
   | Near Sea | 10 min | 10 s | 50–80 | 0–1 |
   | Deep Sea | 60 min | 1 min | 300–450 | 1–3 |
   | Far Sea | 6 h | 6 min | 2000–3000 | 3–6 |

3. When it returns, click **Collect**.
4. Spend gold and materials on **Upgrade** (cargo: more reward, speed: shorter voyages, Lv1–5 each), or unlock the next submarine (500 / 1500 / 4000 gold).
5. Goal: unlock all 4 submarines and max both upgrades on each (progress shown as `Lv x/40`).

**Settings** lets you pick the time scale: `60x` (default, 1 real second = 1 game minute), `1x` (real time) or `600x` (to see the whole game in a few minutes). It also has a two-step save reset.

Progress is saved automatically and voyages keep running while the game is closed. The save file is at `%USERPROFILE%\AppData\LocalLow\VisyLockhart\Submarine Voyage\save.json`.

## Technical highlights

- **Game rules in plain C#.** `SubmarineVoyage.Core` has `noEngineReferences` enabled: submarines, fleet, wallet, upgrade rules and save mapping never touch `UnityEngine`. MonoBehaviours in `SubmarineVoyage.UI` only display state and forward button clicks through C# events.
- **State from timestamps, not frame ticks.** A submarine stores only its departure and return times (UTC); idle / voyaging / ready is derived from the current time. Offline progress needs no extra code, and changing the time scale or speed level never affects a voyage already at sea.
- **Data-driven routes.** Route values live in ScriptableObject assets (`Assets/_Project/Data/Routes/`), so they can be tuned in the Inspector without code changes.
- **Defensive saving.** JSON via `JsonUtility`, written to a temp file and then swapped in; an unreadable save is backed up as `.bak`; out-of-range values from old or hand-edited saves are clamped instead of failing.
- **Testable by design.** Time and randomness are injected (`IClock`, `IRandomSource`), and 32 EditMode tests cover voyages, rewards, upgrades, unlocks, the fleet goal and save round-trips.

## Project structure

```
Assets/_Project/
  Scripts/Core/    SubmarineVoyage.Core   game rules (pure C#)
  Scripts/Data/    SubmarineVoyage.Data   ScriptableObjects, JSON save file
  Scripts/UI/      SubmarineVoyage.UI     HarborController + views
  Tests/EditMode/  SubmarineVoyage.Core.Tests
  Data/Routes/     route assets
  Prefabs/         submarine card, route button
  Scenes/Harbor.unity
docs/GDD.md        game design document
```

## Running from source

1. Install Unity **6000.5.1f1** (Unity 6.5) with Windows Build Support through Unity Hub.
2. Clone this repo and open the folder in Unity Hub (**Add > Add project from disk**).
3. Open `Assets/_Project/Scenes/Harbor.unity` and press Play.

**Tests:** **Window > General > Test Runner**, select **EditMode**, then **Run All**.

**Build:** **File > Build Profiles**, select the **Windows** profile and click **Build**. Output to `Builds/` (ignored by git).

## Built with

Unity 6.5 (Universal 2D template), C#, Unity UI (uGUI), TextMeshPro, Unity Test Framework (NUnit).

Developed with Claude Code as a pair programmer.
