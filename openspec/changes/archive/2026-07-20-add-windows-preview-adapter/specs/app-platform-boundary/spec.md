## ADDED Requirements

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
