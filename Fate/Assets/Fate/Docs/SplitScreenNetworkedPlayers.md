# Split-Screen + Networked Player Architecture

Context: `FatePlayerNetworked Variant.prefab` mixes local split-screen (up to
4 players on one machine) with FishNet networked multiplayer. The Online
FPS/TPS Kit this prefab is built on assumes exactly one player exists in the
whole scene (one MainCamera, one CinemachineBrain, global `Input.*` reads),
so multiple instances immediately fight over cameras and input. This doc
tracks the fix across phases. **Phase 1 and the zero-scaffolding local-slot
auto-assignment are implemented.** Phase 2 (per-slot input isolation) was
prototyped and then deliberately pulled back out for a rearchitecture pass -
see its section below for what was tried and why. Phases 3-4 are architecture
only, written down here so the design survives between sessions - do not
start implementing them without revisiting/confirming scope first.

Target constraints driving every phase below:
- Max 4 players total, split across any mix of local slots and network
  connections. Ultra-performant: zero ongoing per-frame cost for anything
  that isn't an active local slot.
- Console-first UX: full controller support, 4-quadrant local join screen,
  mouse/keyboard usable for local player 1, must also work untouched in
  "big picture" / couch mode with zero keyboard-mouse interaction required.

## Phase 1 - Camera/input ownership isolation (DONE)

Problem: `CinemachineBrain.TopCameraFromPriorityQueue()` picks the
highest-priority `CinemachineVirtualCamera` globally, filtered only by
`Camera.cullingMask` vs. the vcam GameObject's `layer`. With N player
instances all using the default layers, every brain locks onto the same
winning vcam.

Shipped:
- `Assets/Fate/Scripts/Systems/Game/PlayerCameraRig.cs` - a `NetworkBehaviour`
  added to `FatePlayerNetworked Variant.prefab`.
  - Non-owned (remote network) instances: `OnStartClient` disables the whole
    camera GameObject (Camera + AudioListener + CinemachineBrain) and the
    owner-only input/camera scripts (`Input_Handler`, `CameraController`,
    `CameraSwitcher`) once, at spawn. No per-frame cost afterward.
  - Owned instances: `ApplyLocalSlot(slotIndex, playerCount)` moves that
    instance's vcams onto a reserved `VCamSlot{0-3}` layer, restricts its
    own `Camera.cullingMask` to only its own reserved layer (so its brain
    can never see another local/remote instance's vcams), sets
    `Camera.rect` for the given local player count (full/halves/quadrants),
    and enables the `AudioListener` only on slot 0 (avoids multiple
    simultaneous listeners).
  - `localSlotIndex` / `totalLocalPlayers` are serialized fields. Originally
    hand-set (inspector / prefab-instance overrides); superseded by the
    zero-scaffolding auto-assignment note below - `autoAssignLocalSlot`
    defaults to `true`, so these two fields are now only consulted when an
    instance explicitly opts out (the future join lobby will do that).
- `ProjectSettings/TagManager.asset` - added layers `VCamSlot0..VCamSlot3`
  (indices 9-12), reserved purely as a Cinemachine visibility filter; nothing
  else renders or collides on them.
- `FatePlayerNetworked Variant.prefab` - added the `PlayerCameraRig`
  component.
- `Assets/Fate/Assets/Scenes/Dev/Player.unity` - the two dev-scene instances
  originally overrode `localSlotIndex` (0 and 1) and `totalLocalPlayers` (2)
  by hand to exercise the split-screen path before auto-assignment existed.
  Those overrides are harmless now (auto-assignment ignores them) but stale
  - fine to leave or clear whenever the scene is next opened in Editor.

Known test caveat: neither dev-scene instance currently has explicit network
ownership assigned. Depending on FishNet's default scene-object ownership,
both may evaluate `IsOwner == false` (both disabled) instead of exactly one
being owned. To exercise the owner code path, host from one client and give
that client ownership of one instance (or spawn instances at runtime via
`NetworkObject.GiveOwnership`), rather than relying on the two pre-placed
scene instances alone.

## Phase 2 - Per-slot input isolation (Input System) (PULLED BACK - rearchitecting)

Goal (unchanged): each local slot reads input only from the device(s) paired
to it, so slot 0's keyboard/mouse or gamepad never drives slot 1-3's
camera/character, and vice versa. Today `Input_Handler`/`CameraController`/
the move states still read the global `UnityEngine.Input` class with no
device concept - this is a known, accepted gap until this phase is
re-attempted.

History: a first pass was implemented and then explicitly reverted at the
user's request ("undo the input work, I want to architect that better")
before it was ever compiled/verified in-Editor. What was tried, for context
before the next attempt:
- Flipped on `InputSystem_Actions.inputactions`'s `generateWrapperCode` and
  added `Pause`/`AimView`/`AimSight`/`ViewChange`/`Lean`/`WeaponSlot1-4`
  actions, generating a `GameInputActions` wrapper.
- A `PlayerInputRig` component per owned local slot wrapping that generated
  action asset, restricting `InputActionAsset.devices` to a slot's device(s)
  via a **positional guess** (slot 0 = keyboard+mouse+first gamepad, slots
  1-3 = one gamepad each) - this positional guess was the weakest part of the
  design and a likely target for the rearchitecture (Phase 3's join-lobby
  device pairing was always meant to replace it, but doing that guess as an
  interim step added complexity without a real payoff since Phase 3 doesn't
  exist yet).
- An `InputContext` enum + `LocalInputCoordinator` scene component for
  per-player-scoped pause (only the pausing slot's rig switches context;
  every other local slot keeps playing - this specific requirement/behavior
  is still correct and worth keeping in whatever design replaces this).
- Migrating every raw `Input.*` call site in `Input_Handler.cs`,
  `CameraController.cs`, `move/StandState.cs`, `move/CrouchState.cs`,
  `ViewingResistance.cs`, `BodyScripts/BodySlope_Handler.cs` to read through
  `PlayerInputRig`, plus widening `CharacterMove`/move-state scope to depend
  on it.

All of the above has been reverted: the kit scripts are back to raw
`Input.*`, `InputSystem_Actions.inputactions` is back to its unmodified
template state (`generateWrapperCode: 0`), and `PlayerInputRig.cs` /
`InputContext.cs` / `LocalInputCoordinator.cs` / the generated
`GameInputActions.cs` wrapper have been deleted. `PlayerCameraRig.cs` no
longer references any of them - it only handles camera/viewport isolation
again (see Phase 1). The zero-scaffolding auto-slot-assignment feature below
was kept since it's independent of input and still valuable on its own.

## Zero-scaffolding local slot auto-assignment (DONE)

Goal: drop N `FatePlayerNetworked Variant` instances into any scene, press
Play, get isolated cameras/viewports per instance with no per-instance
inspector setup - so ad-hoc local split-screen testing never needs
`localSlotIndex`/`totalLocalPlayers` hand-tuning. (This originally also
auto-wired per-slot input/pause scaffolding; that part was reverted along
with the rest of Phase 2 above and will come back once Phase 2 is
rearchitected.)

Shipped:
- `Assets/Fate/Scripts/Systems/Game/LocalSlotRegistry.cs` - a client-local,
  non-networked static registry. `Claim(rig)` hands out the lowest free slot
  index (growing up to `PlayerCameraRig.MaxLocalSlots`, logging an error and
  returning -1 if full) purely from how many owned `PlayerCameraRig`
  instances currently exist on this machine; `Release(rig)` frees a slot on
  teardown and trims trailing empty slots so the next `Claim()` reuses the
  lowest index instead of growing forever. Both call an internal
  `RefreshAll()` that recomputes the current total local-player count and
  calls `ApplyLocalSlot(i, total)` on every still-claimed rig, so viewport
  splits stay correct as players join/leave.
- `PlayerCameraRig` - new `autoAssignLocalSlot` bool (default `true`).
  `OnStartClient` calls `LocalSlotRegistry.Claim(this)` when true instead of
  reading the manual `localSlotIndex`/`totalLocalPlayers` fields; a new
  `OnStopClient` override calls `LocalSlotRegistry.Release(this)` to free the
  slot. Set `autoAssignLocalSlot` false to keep the old explicit path (the
  future join lobby will do exactly that and call `ApplyLocalSlot` directly
  with an explicit device/quadrant assignment).

## Phase 3 - 4-quadrant local join/lobby screen

Goal: a pre-game screen with 4 screen quadrants (one per potential local
slot). Each connected input device shows an icon (controller glyph, or a
mouse/keyboard icon) that the player can move between quadrants using that
device; landing a device's icon in a quadrant assigns that device to that
local slot. Must support both "console default" (controllers only, nobody
touches mouse/keyboard, works fine in big-picture mode) and "PC-friendly"
(mouse/keyboard controls local player 1's icon like a cursor) without either
mode blocking the other.

Design:
- `PlayerInputManager.onDeviceLost` / `onDeviceRegained` plus polling
  `InputSystem.devices` for unpaired devices drives "device detected, not
  yet assigned to a quadrant" state.
- Each unpaired device gets a lightweight `DeviceJoinCursor` (UI element)
  rendered on the lobby canvas:
  - Gamepad: `DeviceJoinCursor` position driven by that gamepad's left
    stick/d-pad (accumulated, clamped to screen bounds) - each gamepad
    drives only its own cursor, never the shared mouse cursor.
  - Keyboard+mouse: a single shared `DeviceJoinCursor` driven by the actual
    OS mouse position (or arrow keys as a fallback), representing "the
    keyboard+mouse pairing" as one movable icon like any gamepad's.
  - Cursor entering a quadrant's screen-space rect for a short dwell time
    (or an explicit confirm button press, e.g. South/A or Enter/Click)
    locks that device to that quadrant's local slot and shows a "ready"
    state; pressing a cancel button un-assigns it.
- Big-picture / zero-keyboard-mouse support: the lobby must be fully
  completable via any single connected gamepad alone (navigate quadrants,
  confirm, start) - the keyboard/mouse cursor is additive, never required.
  Concretely: quadrant confirm/cancel and "start game" all need controller
  bindings, and the screen must render/operate correctly with zero mouse
  input ever received.
- Result of this screen: a `LocalSlotAssignment` list (deviceId(s) -> slot
  index, up to `PlayerCameraRig.MaxLocalSlots`) and a `totalLocalPlayers`
  count, handed to Phase 4's spawn step. This is the same
  `(slotIndex, totalLocalPlayers)` pair `PlayerCameraRig.ApplyLocalSlot`
  already accepts today by hand - Phase 3 is what will finally compute it
  live instead of the current inspector defaults.
- Where it lives relative to networking: this is a purely local/offline
  screen (no relation to `LobbySession`'s host/join network lobby) that runs
  before `LobbySession.StartGame()` on whichever machine has multiple local
  players; a single-local-player machine can skip straight past it.
- Identity integration (see `Assets/Fate/Scripts/Systems/Identity/Runtime/`,
  built this pass): each quadrant, once a device lands in it, should let that
  local slot pick from the machine's saved `PlayerProfileStore.LoadAll()`
  list (console-style "who's playing" picker) rather than defaulting to
  "every saved profile joins" the way `LobbySession.SendLocalIdentity` does
  today. The resulting per-slot `(deviceId, PlayerId)` pairing is what
  eventually replaces `LobbySession`'s current "announce every local profile"
  behavior with "announce only the profiles actually assigned to a quadrant
  this session".

## Phase 4 - Multi-object-per-connection network spawn

Goal: one FishNet client connection can own and control multiple
`FatePlayerNetworked Variant` instances at once (one per local slot decided
in Phase 3), up to the 4-player total across all connections combined.

Design:
- Extend `LobbySession`/spawn flow: instead of the implicit
  "one NetworkObject per connection" assumption, the server spawns one
  `FatePlayerNetworked Variant` per entry in that connection's
  `LocalSlotAssignment` list (from Phase 3), and calls
  `NetworkObject.GiveOwnership(connection)` for each, then RPCs down the
  resolved `(slotIndex, totalLocalPlayers)` for that specific instance so
  the owning client's `PlayerCameraRig.ApplyLocalSlot` gets called with the
  right values instead of reading serialized inspector defaults.
- Global player cap: server tracks total spawned player instances across
  all connections and rejects/queues additional local-slot spawn requests
  once 4 is reached (matches the "4 max player" ultra-performant target -
  no headroom is reserved for a 5th+).
- `PlayerController.cs` (currently an empty stub) is the natural place to
  eventually own per-player gameplay state once this lands, but is out of
  scope for the camera/input work itself.
- Sequencing dependency: Phase 4 needs Phase 3's `LocalSlotAssignment`
  output to know how many instances/slots a given connection should get;
  it does not strictly need Phase 2 to be done first, but shipping Phase 4
  without Phase 2 means multiple locally-spawned instances on one machine
  would still fight over raw `Input.*` the same way cameras used to fight
  over Cinemachine priority - so Phase 2 should land no later than Phase 4.
  Phase 2 is currently pulled back for a rearchitecture pass (see above), so
  this constraint is **not yet satisfied** - revisit before starting Phase 4.
- Identity integration (see `Assets/Fate/Scripts/Systems/Identity/Runtime/`,
  built this pass): `PartyRosterService.TryGetRoster(connection, out roster)`
  is exactly the per-connection input this spawner needs - `roster.LocalPlayers`
  (each a `PlayerId` + `DisplayName`, already stashed on
  `NetworkConnection.CustomData` when the connection's
  `PlayerIdentityBroadcast` arrived) tells the server how many instances to
  spawn for that connection and which saved identity to associate with each
  one, once Phase 3 exists to produce a roster with one entry per assigned
  quadrant instead of today's "every saved local profile" broadcast.

## Identity & party scope note

`PlayerId`/`PlayerProfile`/`PlayerProfileStore` (local-only, GUID-backed,
console-style multi-profile picker) and the `PlayerIdentityBroadcast` ->
`PartyRosterService` pipeline are built and working today, independent of
Phases 3-4 above. What's intentionally **not** built, and stays out of scope
until a real backend is chosen: any matchmaking or party system beyond
today's direct-connect `LobbySession.StartHost()` / `JoinHost(address)` model
- no server browser, no invite links, no friends list, no relay/NAT
traversal. "Join a friend's party" remains "get their IP/address out of band
and enter it". The identity layer was deliberately kept provider-agnostic
(raw string IDs on the wire, no auth handshake) so a real backend can slot in
underneath it later without changing `PlayerId`/`PlayerProfile` shapes.
