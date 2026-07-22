# app-platform-boundary Specification

## Purpose
TBD - created by archiving change introduce-app-bootstrap-and-platform-boundary. Update Purpose after archive.
## Requirements
### Requirement: Platform-neutral runtime contract
The application SHALL expose platform identity, launch context, initialization result, and foreground/background visibility through contracts that do not reference Editor, browser, Douyin, or WeChat SDK types.

#### Scenario: Platform initialization completes
- **WHEN** the composition root initializes the selected adapter
- **THEN** the adapter reports exactly one `PlatformInitResult` through its completion callback and exposes its platform identity and launch context

#### Scenario: Host visibility changes
- **WHEN** the active host moves between foreground and background
- **THEN** the adapter emits a deduplicated platform-neutral visibility event

### Requirement: Editor and Web adapters
The application SHALL provide initialized Editor and Web adapters using the same public contract.

#### Scenario: Runtime executes in the Unity editor
- **WHEN** the platform factory selects the current host in an Editor build
- **THEN** it returns an available Editor adapter with an empty platform-neutral launch context

#### Scenario: Runtime executes as normal WebGL
- **WHEN** the platform factory selects the current host in a WebGL build without a mini-game host symbol
- **THEN** it returns an available Web adapter whose launch context contains the parsed absolute URL query

### Requirement: Explicit unavailable mini-game slots
The platform factory SHALL reserve Douyin and WeChat identities and SHALL report them unavailable until SDK-backed adapters are installed, without substituting Web behavior.

#### Scenario: Douyin adapter is requested before installation
- **WHEN** the factory is asked to create a Douyin adapter
- **THEN** the result retains `DouyinMiniGame` identity and initialization fails with `adapter-not-installed`

#### Scenario: WeChat adapter is requested before installation
- **WHEN** the factory is asked to create a WeChat adapter
- **THEN** the result retains `WeChatMiniGame` identity and initialization fails with `adapter-not-installed`

### Requirement: Unique application composition root
`AppBootstrap` SHALL allow only one active composition-root instance, own the selected adapter and navigator, survive scene loads once activated, and expose platform initialization readiness or failure without loading a scene.

#### Scenario: First bootstrap awakens
- **WHEN** the first `AppBootstrap` component awakens
- **THEN** it persists, constructs one navigator and one current-host adapter, and begins adapter initialization

#### Scenario: Duplicate bootstrap awakens
- **WHEN** another `AppBootstrap` awakens while an active instance exists
- **THEN** the duplicate is destroyed without replacing or reinitializing the active instance

#### Scenario: Platform initialization fails
- **WHEN** the selected adapter reports an unsuccessful result
- **THEN** the bootstrap exposes the failure and does not navigate or silently replace the adapter

### Requirement: Current battle entry remains unchanged
This boundary increment SHALL NOT activate a second automatic runtime entry or modify the current Main-scene battle bootstrap.

#### Scenario: Existing project smoke runs
- **WHEN** the existing editor smoke validation executes after the new framework compiles
- **THEN** the current direct battle behavior and gameplay validation remain unchanged

### Requirement: Windows desktop preview adapter
The application SHALL provide an explicit Windows desktop-preview adapter through the existing platform-neutral runtime contract. The adapter MUST initialize successfully for Windows standalone players, retain a desktop-preview identity, and MUST NOT identify itself as Web, Douyin, or WeChat.

#### Scenario: Runtime executes as a Windows standalone player
- **WHEN** the platform factory selects the current host in a Windows standalone build without a mini-game host symbol
- **THEN** it returns an available Windows desktop-preview adapter with an empty platform-neutral launch context

#### Scenario: Windows preview adapter is created explicitly
- **WHEN** deterministic validation requests the Windows desktop-preview platform identity
- **THEN** the adapter completes initialization exactly once with a successful result and retains the requested identity

#### Scenario: Mini-game symbols remain authoritative
- **WHEN** a Douyin or WeChat host is selected before its SDK-backed adapter is installed
- **THEN** the factory retains that mini-game identity and reports it unavailable instead of returning the Windows desktop-preview or Web adapter

