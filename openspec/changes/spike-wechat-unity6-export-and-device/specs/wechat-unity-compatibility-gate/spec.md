## ADDED Requirements

### Requirement: Reproducible desktop preflight
The project SHALL provide a non-destructive preflight that reports Unity and WebGL support, Node, WXSDK/conversion plugin, WeChat Developer Tools, platform configuration, generated artifacts, package sizes, and credential-presence flags without exposing secret values.

#### Scenario: Missing platform tools
- **WHEN** the preflight runs on a machine without WXSDK or WeChat Developer Tools
- **THEN** it completes with structured Yellow rows identifying the missing prerequisites

#### Scenario: Secret-bearing environment
- **WHEN** WeChat AppID, session, or upload-key environment variables are configured
- **THEN** the report records only boolean presence and never their values

### Requirement: Immutable platform version evidence
The compatibility report MUST record the exact Unity version and SHALL identify an installed WXSDK by immutable source commit or package lock together with its observed metadata version, and SHALL record the exact Stable WeChat Developer Tools, client, and base-library versions before conversion evidence can become Green.

#### Scenario: Mutable SDK reference
- **WHEN** the SDK is referenced only by a moving branch or assets without an immutable revision
- **THEN** the SDK row remains Yellow with an action to pin the successfully tested commit

#### Scenario: Developer Tools edition is unsuitable
- **WHEN** only the Minigame Build edition or an unversioned Developer Tools candidate is detected
- **THEN** the tool row remains Yellow and does not satisfy the Stable Developer Tools requirement

### Requirement: Evidence-based status classification
Every compatibility row MUST be classified Green, Yellow, or Red and MUST include whether it blocks release, an evidence summary, observation timestamp, and next action.

#### Scenario: Unverified device capability
- **WHEN** a capability is documented or works in ordinary WebGL but has not run on the required physical device
- **THEN** the row remains Yellow rather than Green

#### Scenario: Reproducible incompatibility
- **WHEN** the pinned toolchain fails the same compile, conversion, or runtime check on a clean retry
- **THEN** the row is Red with the failing command or artifact recorded

### Requirement: WeChat integration gate
The project MUST NOT merge or activate the production WeChat adapter until Unity export, conversion, simulator, Android, iOS, lifecycle, input, audio, HTTPS/cache, code update, content delivery, package-splitting, and stability rows are Green.

#### Scenario: Gate is incomplete
- **WHEN** one or more release-blocking rows are Yellow or Red
- **THEN** `WeChatMiniGame` remains explicitly unavailable and the Web adapter is not selected as a silent fallback

#### Scenario: Gate is complete
- **WHEN** every release-blocking row is Green and the Douyin-first release path is stable
- **THEN** the compatibility report authorizes work on `add-wechat-runtime-adapter`

### Requirement: Platform mechanisms remain separated
The compatibility report SHALL evaluate code-package UpdateManager, remote-content delivery, ordinary subpackages, and Wasm splitting as distinct mechanisms.

#### Scenario: Code update evidence
- **WHEN** UpdateManager reports a downloaded code package
- **THEN** the evidence demonstrates restart-based application and does not describe it as in-process code hot replacement

#### Scenario: Content update evidence
- **WHEN** a remote catalog or bundle changes without a code-package change
- **THEN** the evidence demonstrates version validation, cache behavior, and fallback using only types supported by the shipped code

### Requirement: Android and iOS device matrix
The spike MUST exercise cold and warm launch, touch, audio, hide/show, HTTPS, cache, UpdateManager callbacks, one complete battle, and a 30-minute repeated-play run on both Android and iOS.

#### Scenario: Partial device coverage
- **WHEN** only a simulator or one mobile operating system has completed the matrix
- **THEN** the overall device gate remains Yellow

#### Scenario: Completed device coverage
- **WHEN** both operating systems complete the matrix without crash or out-of-memory termination
- **THEN** the device and stability rows may be marked Green with logs and exact environment metadata
