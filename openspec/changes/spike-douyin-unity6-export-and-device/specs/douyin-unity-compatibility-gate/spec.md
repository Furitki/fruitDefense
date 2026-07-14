## ADDED Requirements

### Requirement: Reproducible desktop preflight
The project SHALL provide a non-destructive preflight that reports Unity and WebGL support, Node, TTSDK, Douyin developer tools, platform configuration, build artifacts, and credential-presence flags without exposing secret values.

#### Scenario: Missing platform tools
- **WHEN** the preflight runs on a machine without TTSDK or Douyin developer tools
- **THEN** it completes with structured Yellow rows identifying the missing prerequisites

#### Scenario: Secret-bearing environment
- **WHEN** platform credentials are configured
- **THEN** the report records only their presence and never their values

### Requirement: Evidence-based status classification
Every compatibility row MUST be classified Green, Yellow, or Red and MUST include an evidence source, observed version or environment, timestamp, and next action.

#### Scenario: Unverified device capability
- **WHEN** a capability is documented but has not run on the required physical device
- **THEN** the row remains Yellow rather than Green

#### Scenario: Reproducible incompatibility
- **WHEN** the pinned toolchain fails the same compile, conversion, or runtime check on a clean retry
- **THEN** the row is Red with the failing command or artifact recorded

### Requirement: Douyin integration gate
The project MUST NOT merge or activate the production Douyin adapter until Unity export, conversion, simulator, Android, iOS, lifecycle, input, audio, HTTPS/cache, UpdateManager, content delivery, and stability rows are Green.

#### Scenario: Gate is incomplete
- **WHEN** one or more release-blocking rows are Yellow or Red
- **THEN** `DouyinMiniGame` remains explicitly unavailable and the Web adapter is not selected as a silent fallback

#### Scenario: Gate is complete
- **WHEN** every release-blocking row is Green
- **THEN** the compatibility report authorizes work on `add-douyin-runtime-adapter`

### Requirement: Platform mechanisms remain separated
The compatibility report SHALL evaluate code-package update, remote-content delivery, ordinary subpackages, and Wasm splitting as four distinct platform mechanisms.

#### Scenario: Code update evidence
- **WHEN** UpdateManager reports a downloaded code package
- **THEN** the evidence demonstrates restart-based application and does not describe it as in-process code hot replacement

#### Scenario: Content update evidence
- **WHEN** a remote catalog or bundle changes without a code-package change
- **THEN** the evidence demonstrates version validation, cache behavior, and fallback using only types supported by the shipped code

### Requirement: Android and iOS device matrix
The spike MUST exercise cold and warm launch, touch, audio, hide/show, HTTPS, cache, UpdateManager callbacks, one complete battle, and a 30-minute repeated-play run on both Android and iOS.

#### Scenario: Partial device coverage
- **WHEN** only one mobile operating system has completed the matrix
- **THEN** the overall device gate remains Yellow

#### Scenario: Completed device coverage
- **WHEN** both operating systems complete the matrix without crash or out-of-memory termination
- **THEN** the device and stability rows may be marked Green with attached logs and version metadata
