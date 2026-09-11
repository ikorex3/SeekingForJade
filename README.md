# SeekingForJade (赌石 - Stone Gambling Simulator)

> **A 3D Multiplayer Lapidary Simulation & High-Stakes Stone Gambling Game**  
> Built with **Unity 6 (6000.6.0f1)**, **Universal Render Pipeline (URP 17.7.0)**, and **Unity Netcode for GameObjects (NGO 2.13.2)**.

---

## 💎 Game Overview & Core Concept

**SeekingForJade** is inspired by the legendary real-world phenomenon of **Dǔshí** (赌石 - *"Stone Gambling"*), centered around the gemstone markets of Myanmar (Hpakant) and China (Ruili, Tengchong, Guangdong).

In stone gambling, raw jadeite boulders extracted from ancient alluvial riverbeds are encrusted with an opaque, weathered outer crust (*pí*, 皮). Looking from the outside, a multi-million-dollar imperial green boulder looks identical to an ordinary gravel river rock. Buyers must rely on geological clues, weight, skin texture, and specialized gemological flashlights to peer into the crust before placing their bets. 

Once purchased, the moment of truth arrives: the rock is cut open. One cut can make you an overnight millionaire, or leave you holding worthless gravel (*"One knife poor, one knife rich, one knife wearing linen cloth, one knife wearing silk"*).

### Core Pillars
1. **Authentic Stone Gambling Loop**: Forage or buy rough stones, inspect internal translucency with optical flashlights, decide whether to risk cutting, and appraise the exposed jade slabs.
2. **Physical Mesh Slicing**: Every cut is a true dynamic 3D plane slice. Rocks physically cleave in two, exposing unique, procedurally generated internal crystal structures, veins, and flaws.
3. **Multiplayer Bazaar & Social Tension**: Players gather in a shared workshop and open-air market, watching each other cut stones live, reacting to epic drops, trading slabs, and placing bets.
4. **Graduated Equipment Progression**: From desperate "throw-and-smash" starter cuts that damage stones, to manual hand saws, precision workshop drop saws, and industrial diamond wire rigs.

---

## 🎮 Gameplay Mechanics & Systems

### 1. Rock Sourcing
- **Mining Quarry (Free Foraging)**:
  - An alluvial rock mound located near the workshop.
  - Players can mine free raw boulders on a short cooldown (15s prototype).
  - Ensures players can never go bankrupt and can always rebuild their bankroll through foraging.
- **Trader Stall & Bazaar NPC (Master Chen)**:
  - Sells graded mystery boulders categorized by weight, skin type, and origin.
  - Tiers: Budget River Cobbles ($50), Medium Mountain Stones ($250), High-Grade Black Waxy Boulders ($1,000), and Masterclass Roughs ($5,000+).
- **Player Trading (Multiplayer)**:
  - Players can buy, sell, or swap uncut boulders or cut slabs directly with other players in the bazaar.

### 2. Gemological Inspection (Flashlight System)
Specialized lapidary torches shine through the rock crust to reveal optical clues before cutting:
- **Warm Yellow Beam (3000K)**:
  - Penetrates deep into the stone body.
  - Highlights internal translucency (*water level* / 水头), color saturation, and light absorption.
- **Cool White Beam (6500K)**:
  - High-contrast inspection for surface crystal grain, sand texture (*shā lí*, 沙粒), and outer crust fissures.
- **UV Purple Beam (365nm)**:
  - Fluorescent detection for synthetic resin fills, glue-injection scams (*B+C goods*), and fracture dyes.
- **Window Grinding (*Kāi Chuāng*, 开窗 - Planned)**:
  - Use a small diamond burr tool to polish off a coin-sized patch of crust, creating a "peeking window" to increase the rock's resale value without a full cut.

### 3. Slicing & Cutting Progression
Cutting determines how much market value is preserved from the raw gemstone:

| Tier | Cutting Method | Description | Value Retention | Risk / Flaw Penalty |
| :--- | :--- | :--- | :--- | :--- |
| **Tier 1** | **Throw & Smash** | Pick up and hurl stone at hard floor/walls | **50% Value** | **High**: Severe crude fractures (+0.35 crack severity) |
| **Tier 2** | **Hand Saw / Portable** | Slow manual saw for small cobbles | **80% Value** | **Moderate**: Rough cut surface, slight unevenness |
| **Tier 3** | **Workshop Drop Saw** | Heavy motorized circular diamond blade with coolant | **100% Value** | **None**: Perfect clean plane slice |
| **Tier 4** | **Wire / Gang Saw (Future)** | Industrial diamond wire for massive boulders | **110% Value** | **Bonus**: Multi-slab yield with mirror polish |

### 4. Procedural Jade Generation & Valuation Formula
Every rock has unique internal attributes generated from its seed:
- **Rarity Categories**:
  1. **Brick Stone (砖头料)**: Worthless limestone/calcite chalk, ash grey or dirty white ($5/kg).
  2. **Bean Green (豆青)**: Common, slightly opaque pale pea green ($80/kg).
  3. **Apple Green (苹果绿)**: Rare, vibrant, fresh emerald green ($450/kg).
  4. **Lavender (春色 / 紫罗兰)**: Epic, soft violet-purple hues ($900/kg).
  5. **Imperial Glass (玻璃种帝王绿)**: Legendary, hyper-translucent glowing deep emerald ($3,500/kg).
- **Quality Attributes**:
  - `Translucency` (0.0 to 1.0): How deep light travels through the stone (Chao/Water level).
  - `Purity` (0.0 to 1.0): Freedom from dark impurities, cotton spots, and mineral flecks.
  - `Crack Severity` (0.0 to 1.0): Natural fissures and impact fractures that dramatically discount value.
- **Valuation Formula**:
  $$\text{Value} = \text{Weight}_{\text{kg}} \times \text{BasePrice} \times \text{PurityMult} \times \text{TranslucencyMult} \times (1 - \text{CrackPenalty}) \times \text{CutEfficiency}$$

---

## 🛠 Technical Architecture

- **Engine**: Unity 6 (`6000.6.0f1`)
- **Render Pipeline**: Universal Render Pipeline (URP `17.7.0`)
- **Multiplayer**: Unity Netcode for GameObjects (`2.13.2`)
- **Input System**: New Input System (`com.unity.inputsystem 1.20.0`)
- **Custom Shaders**:
  - `SeekingForJade/JadeInternalURP`: Subsurface light wrap, procedural FBM vein noise, cotton mask, crack lines, vitreous specular polish, and `Cull Off` double-sided cap rendering.
- **Mesh Slicing Engine**:
  - `MeshSlicer.cs`: Real-time geometric plane-mesh split, angular vertex sorting around centroid, outwards-oriented normal winding, convex mesh collider generation, and dynamic material array management.
- **Editor Automation**:
  - `SceneSetupHelper.cs`: Full one-click prototype workbench, quarry, trader stall, and rock generation via menu item `SeekingForJade > Setup Prototype Scene`.

---

## 🗺 Implementation Plan & Roadmap

### Milestone 1: Core Physics & Cutting Prototype ✅
- [x] Procedural icosphere deformed rock generator with non-uniform silhouettes.
- [x] Dynamic runtime 3D mesh plane slicer (`MeshSlicer.cs`).
- [x] Custom URP Jade SSS internal shader with procedural grain and veins.
- [x] Multi-spectrum inspection flashlight (`InspectionFlashlight.cs`).
- [x] Tier 1 Throw & Smash mechanic with impact detection and crude break penalty (`RockImpactBreaker.cs`).
- [x] Tier 3 Workshop cutting drop saw station with blade animation and audio (`CuttingSawStation.cs`).
- [x] Free rock foraging quarry with cooldown (`MiningPile.cs`).
- [x] NPC trader with buy catalog and appraisal scale selling (`JadeTraderNPC.cs`, `PlayerWallet.cs`).
- [x] Visual overhaul: dark slate workshop floor, industrial blade guard/hub, stable rock placement.

### Milestone 2: Multiplayer Synchronization & Bazaar Atmosphere 🔄
- [ ] Network synchronization for procedural rocks (seed, weight, quality attributes replicated via NGO).
- [ ] Networked slicing: Server-authoritative mesh split replicated to all observing clients.
- [ ] Player-to-player rock handoff / trade interaction.
- [ ] Proximity voice chat (Vivox integration).
- [ ] Workshop soundscapes: diamond blade grinding, water coolant spray, rock impact thuds.

### Milestone 3: Lapidary Crafting & Processing Mini-Games ⏳
- [ ] Window grinding station (*Kāi Chuāng*): mini-game to expose coin-sized jade patches.
- [ ] Core drilling: drilling cylindrical jade cores out of cut slabs.
- [ ] Bangle & pendant carving lathe: turn rough slabs into finished jewelry pieces for 3x - 10x value multipliers.
- [ ] Diamond buffing wheel: polish cut faces to mirror glassy finishes.

### Milestone 4: Economy, Progression & Metagame ⏳
- [ ] Player progression: Apprentice -> Cutter -> Master Appraiser -> Jade Tycoon.
- [ ] High-stakes VIP auction room with live bidding wars.
- [ ] Specialized boulder origins (e.g., Old Mine *Lao Keng*, Mo Sha, River Gravel vs. Mountain Quarry).
- [ ] Tool upgrades: High-power UV torches, laser-guided wire saws, automated hydraulic clamps.

---

## 📜 Changelog & Update History

All changes made to the codebase are tracked here in chronological order:

### [2026-09-11] - Visual Polish, Rock Physics & Slice Rendering Fixes
**Branch**: `feature/rock-cutting-improvements`
- **Saw Blade Orientation**: Corrected saw blade cylinder rotation (`Quaternion.Euler(0, 0, 90)`) to align the disc vertically in the Y-Z plane. Corrected spin axis to `Vector3.up` (cylinder axle) in local space. Added industrial `BladeGuard` hood and central `BladeHub` collar.
- **Solid Jade Cross-Section**: Fixed transparent cut faces by assigning `JadeInternalURP.shader` to `M_Jade_Internal.mat` with `Cull Off`, verifying outward cap triangle winding in `MeshSlicer.cs`, and isolating property blocks so outer crust remains natural stone while the cut cap face renders vibrant jade.
- **Dark Floor Material**: Created `M_Dark_Floor.mat` with dark slate charcoal tone (`#1E2129`), replacing the blinding white floor plane.
- **Rock Physics & Natural Resting**: Added linear (0.8) and angular (2.5) damping to rock Rigidbodies. Adjusted spawn heights to `Y = 1.15m` and clamped rock mount to `Y = 1.22m` so boulders rest naturally without clipping or rolling endlessly.
- **Multiplayer Starter Cube**: Relocated starter test cube away from the cutting workbench.
- **Audio Listener**: Ensured player camera prefab includes an `AudioListener` to eliminate missing listener warnings.

### [2026-09-11] - Economy, Mining & Slicing Systems
**Branch**: `feature/rock-cutting-improvements`
- **Tier 1 Throw & Smash**: Implemented `RockImpactBreaker.cs` and `PlayerInteraction.cs` left-click throwing with high-velocity collision slicing and crude break penalty (+0.35 crack severity, 50% price reduction).
- **Free Foraging Quarry**: Added `MiningPile.cs` with 15s cooldown to mine free boulders.
- **Trader & Appraisal Scale**: Added `JadeTraderNPC.cs` for scale selling and boulder purchasing, paired with `PlayerWallet.cs` HUD money tracker.
- **Precision Saw Station**: Built `CuttingSawStation.cs` with vice clamping, blade descent routine, and dynamic slicing.
- **Inspection Flashlight**: Built `InspectionFlashlight.cs` with Warm Yellow, Cool White, and UV Purple modes.

---

## 🕹 Quick Start & Controls

1. Open Unity 6 and load scene `Assets/Scenes/SampleScene.unity`.
2. Press **Play** and click **Host** on the NetworkUI overlay.
3. **Controls**:
   - **`[W] [A] [S] [D]`**: Move
   - **`[Space]`**: Jump / **`[Shift]`**: Sprint
   - **`[E]`**: Interact (Pick up rock / Place on clamp / Start saw / Mine quarry / Sell on scale)
   - **`[Left Click]`**: Throw held rock (smash cut against hard surface)
   - **`[Q]`** or **`[Right Click]`**: Drop held rock gently
   - **`[F]`**: Toggle Inspection Flashlight on/off
   - **`[T]`**: Cycle flashlight mode (Warm Yellow -> Cool White -> UV Purple)
   - **`[B]`**: Buy mystery boulder when looking at Trader NPC
