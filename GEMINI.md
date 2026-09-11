# SeekingForJade - Project Guidelines & Rules

## Project Context
- **Engine Version**: Unity 6 (6000.6.0f1)
- **Render Pipeline**: Universal Render Pipeline (URP 17.7.0)
- **Multiplayer / Networking**: Unity Netcode for GameObjects (NGO 2.13.2)
- **Input Framework**: Unity New Input System (`com.unity.inputsystem` 1.20.0)
- **Pathfinding & AI**: Unity AI Navigation (`com.unity.ai.navigation` 2.0.14)
- **UI Systems**: uGUI + TextMeshPro / UI Toolkit
- **Agent Skills**: 31 specialized Unity skills located in `.agents/skills/`

---

## Coding Standards & Conventions

### 1. Architecture & General Practices
- Target **C# / .NET Standard 2.1** features supported by Unity 6.
- Organize scripts cleanly into feature-specific folders within `Assets/`.
- Use explicit access modifiers on all classes, structs, methods, and fields.
- Use `PascalCase` for classes, structs, enums, public properties, and methods.
- Use `camelCase` or `_camelCase` for private fields and local variables.
- Prefer `[SerializeField] private` for fields that need Inspector exposure; avoid naked `public` fields.
- Prefer `TryGetComponent<T>(out var component)` over `GetComponent<T>()`.
- Cache component lookups in `Awake()` or `OnNetworkSpawn()`; never query components inside `Update()`.
- Avoid GC allocations in per-frame loops (no `new`, no string concatenations or boxing in `Update()` / `FixedUpdate()`).

### 2. Netcode for GameObjects (NGO) Guidelines
- Scripts managing networked state must inherit from `NetworkBehaviour` rather than `MonoBehaviour`.
- Network synchronization initialization belongs in `OnNetworkSpawn()`, not `Awake()` or `Start()`.
- Network cleanup and unsubscriptions must be handled in `OnNetworkDespawn()`.
- Use `IsOwner`, `IsServer`, and `IsClient` guards to enforce authoritative execution.
- Use `NetworkVariable<T>` for state synchronization with appropriate read/write permissions.
- Use `[ServerRpc]` (sent from client to server) and `[ClientRpc]` (sent from server to clients) for events and actions.

### 3. Input System Guidelines
- Always use the **New Input System** (`UnityEngine.InputSystem`). Do not use legacy `Input.GetKeyDown` or `Input.GetAxis`.
- Reference and bind actions through the project asset `InputSystem_Actions.inputactions`.

### 4. Rendering & URP Guidelines
- Always design shaders and materials for URP.
- In Unity 6, custom render passes and renderer features must use the **Render Graph API** (`ScriptableRenderPass` with `AddRasterRenderPass` / `RecordRenderGraph`).
- Use the Volume framework (`UnityEngine.Rendering.Volume`) for post-processing effects (Bloom, Tone Mapping, Color Adjustments).

---

## Unity MCP (Model Context Protocol) Integration

Antigravity is connected to the live Unity Editor via the `unityMCP` server. Follow these guidelines when interacting with Unity:

1. **Verify State First**:
   - Query scene hierarchy using `manage_scene(action="get_hierarchy")` before mutating GameObjects.
   - Use `find_gameobjects` or `manage_asset` to inspect components and assets.
2. **Monitor Compilation & Errors**:
   - After creating or editing any C# script, inspect compilation logs using `read_console(action="get", count=5)` to verify that domain reload succeeds without compiler errors.
3. **Payload Management**:
   - For large scenes or asset searches, always use pagination (`page_size=25` or `50`) to prevent excessively large payloads.

---

## Workspace Skills
Specialized procedures are located in `.agents/skills/`. When performing tasks in the following domains, consult the corresponding skill's `SKILL.md`:
- **URP Post-Processing**: `.agents/skills/urp-postprocessing/`
- **Render Graph Validation**: `.agents/skills/validate-urp-render-graph-renderer-feature/`
- **AI Navigation & NavMesh**: `.agents/skills/initialize-ai-navigation/`
- **Multiplayer & Networking**: `.agents/skills/setup-multiplayer-services/`
- **UI (uGUI / UI Toolkit)**: `.agents/skills/ui/`, `ui-ugui/`, `ui-uitk/`
- **Audio Optimization**: `.agents/skills/optimize-audio/`, `audio-setup-mixers/`
- **TextMeshPro & Fonts**: `.agents/skills/optimize-text-mesh-pro/`
- **3D Physics**: `.agents/skills/physics-3d-collision/`
