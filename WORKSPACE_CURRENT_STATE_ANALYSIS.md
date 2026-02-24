# Workspace Current State Analysis

## Document Purpose

This document is a high-context handoff for continued human or LLM work in this workspace.
It describes what this workspace originally was, what it has become, how work is actually executed day-to-day, and what standards/methods are currently in force.

It is intentionally detailed to reduce relearning time and enable seamless continuation.

---

## 1) Workspace Mission: Original Scope vs Current Scope

### Original mission (still active)
- Primary codebase is an Android foreground-service app that bridges BLE and HTTP device data to MQTT.
- Main goal: provide Home Assistant-compatible telemetry/control for RV-related hardware using MQTT discovery.
- Core repository: android_ble_plugin_bridge.

### Current mission (expanded)
- The workspace now also supports direct Home Assistant custom integration development (HACS-style repos) for selected plugins/protocols previously implemented in the Android bridge.
- This is no longer only an Android app maintenance workspace; it is now a multi-repo protocol porting and operations workspace.

### Practical implication
- The Android app remains the protocol reference and “known-good behavior” source.
- HACS repos are now parallel products derived from Android plugin behavior, adapted to native HA architecture (coordinators/entities/config flows).

---

## 2) Workspace Topology and Active Repositories

## In-workspace roots
- android_ble_plugin_bridge
- .gradle (shared Gradle cache/work tooling)

## Related sibling repos actively used during current phase
- ha-onecontrol
- ha-gopower
- ha-mopeka

Even if not all sibling repos are mounted as formal workspace roots at all times, active development workflow includes hopping between these repos for protocol parity and production fixes.

---

## 3) Current Repository States (Snapshot)

Snapshot based on latest local git status/log checks during this session:

### android_ble_plugin_bridge
- Branch: main tracking origin/main
- HEAD/tag: v2.7.3
- Recent release cadence indicates active maintenance and fast iteration.
- Working tree note: ble-mqtt-plugin-app.code-workspace is modified (both staged/unstaged indicators observed as MM).

### ha-onecontrol
- Branch: main tracking origin/main
- Recent commits include BLE connection robustness and icon/logo asset commits.
- Representative recent commits:
  - Add integration icon and logo assets
  - Use bleak-retry-connector for OneControl BLE connect
  - Pairing diagnostics and adapter fallback fixes

### ha-gopower
- Branch: main tracking origin/main
- Recent commits include startup/discovery hardening and icon/logo asset commit.

### ha-mopeka
- Branch: main tracking origin/main
- Repository has scaffolded integration plus debugging alignment and icon/logo asset commit.

---

## 4) Architecture Baseline: Android App (System of Record)

## Core architecture
- Android app is a foreground-service system.
- BLE and MQTT were historically central; service independence refactors now decouple BLE, MQTT, and web concerns.
- Web UI/API is embedded (NanoHTTPD) for runtime configuration and instance management.

## Plugin model
- Multi-instance plugin architecture is mature and central.
- Plugins encapsulate device/protocol-specific behavior.
- Host framework manages lifecycle, persistence, MQTT publication, and status reporting.

## Device/plugin coverage (current known set)
- OneControl (BLE)
- EasyTouch (BLE)
- GoPower (BLE)
- Mopeka (BLE)
- Hughes (BLE)
- BLE scanner plugin
- Peplink (HTTP polling)

## Why Android code remains critical
- Protocol details (pairing quirks, authentication sequencing, event mappings, command semantics) are fully exercised here.
- Porting to HA integrations uses Android plugin behavior as parity reference.

---

## 5) Expansion Pattern: Android Plugin -> Native HA Integration

## Strategic shift
The current workflow explicitly treats each protocol/domain as an independent HA integration candidate, instead of trying to replicate Android’s multi-plugin host in HA.

## Current status by domain
- Mopeka: custom integration scaffolded and functioning; domain matches core mopeka domain behavior.
- OneControl: active custom integration with ongoing BLE connection/pairing hardening.
- GoPower: custom integration in active maintenance.

## Porting principles being followed
- Preserve protocol behavior and edge-case handling from Android reference.
- Conform to HA patterns:
  - Config flow
  - Coordinator/entity model
  - Diagnostics
  - Bluetooth matcher usage in manifest
- Prefer robust connection setup using bleak-retry-connector patterns rather than direct raw connect calls.

## Known domain/branding behavior learned
- HA/HACS visual brand icons resolve by integration domain and Brands pipeline, not by arbitrary local icon files alone.
- Domain collisions with core integrations favor core branding assets.

---

## 6) Operational Environment and Infrastructure

## Test/deploy environment patterns
- Android deployment commonly targets a remote test device over wireless ADB.
- HA integration deployment is performed to remote HAOS instance over SSH.
- MQTT test infrastructure and LAN/Tailscale routing are part of normal development operations.

## Current test environment specifics (as of 2026-02-22)

### Host and network inventory
- Development workstation: macOS host running this workspace.
- Android test device (wireless ADB): `10.115.19.214:5555`.
- MQTT broker (test/home lab): `10.115.19.131:1883`.
  - Current known test credentials: username `mqtt`, password `mqtt`.
- Home Assistant OS host (remote over Tailscale): `100.127.141.49` (SSH as `root`).
- Primary subnet router node for LAN reachability: `shamrock` at Tailscale `100.127.238.102`, LAN interface `10.115.19.2`.
- Routed LAN currently relied upon for device access: `10.115.19.0/24`.

### Android deploy scripts and commands
- Preferred script: `scripts/install-dev.sh` (build + install-replace + restart/launch flow).
- Manual build: `./gradlew assembleDebug`.
- Manual install preserving app data: `adb -s 10.115.19.214:5555 install -r app/build/outputs/apk/debug/app-debug.apk`.
- Manual launch: `adb -s 10.115.19.214:5555 shell am start -n com.blemqttbridge/.MainActivity`.
- ADB connect sequence (if disconnected):
  - `adb connect 10.115.19.214:5555`
  - `adb devices -l`

### Android test/validation commands
- Run all unit tests: `./gradlew test`.
- Run multi-instance test class: `./gradlew test --tests "com.blemqttbridge.web.MultiInstanceWebApiTest"`.
- Open test report: `open app/build/reports/tests/testDebugUnitTest/index.html`.

### MQTT sanity checks
- Subscribe all HA topics: `mosquitto_sub -h 10.115.19.131 -p 1883 -u mqtt -P mqtt -t "homeassistant/#"`.
- Publish test message: `mosquitto_pub -h 10.115.19.131 -p 1883 -u mqtt -P mqtt -t "test/topic" -m "message"`.

### HA custom integration deploy/ops pattern
- Primary access path: SSH to `root@100.127.141.49`.
- Typical deploy target path on HAOS: `/homeassistant/custom_components/<domain>/`.
- Typical operational actions:
  - Copy integration files into `/homeassistant/custom_components/`.
  - Restart HA core or reload integration/config entries as appropriate.
  - Follow logs to verify discovery/setup and runtime connection health.

### Routing dependency notes
- Tailscale subnet routing to `10.115.19.0/24` is an active dependency for lab access.
- If LAN targets become unreachable after reboot events, validate forwarding on subnet router first (`net.ipv4.ip_forward`) before assuming app/integration regressions.

## Network operations reality
- This workspace now includes practical infrastructure troubleshooting as part of delivery (not just coding).
- Example class of issue already handled in current cycle: subnet route breakage after reboot due to forwarding reset on subnet router.
- Development flow assumes comfort with:
  - Tailscale subnet routing
  - sysctl persistence
  - reachability verification from macOS client to LAN targets

---

## 7) Workflow Standards and Methods Actually in Use

## 7.1 Development method (observed)
1. Implement or modify protocol behavior in appropriate repo.
2. Build/deploy quickly to real target environment (Android device or HAOS).
3. Validate with real hardware and logs, not just static/unit tests.
4. Iterate immediately on runtime findings.
5. Commit small, focused fixes with explicit intent in message.

This is a high-feedback, hardware-in-the-loop workflow.

## 7.2 Android deployment standard
- Never uninstall app unless explicitly requested.
- Reinstall with preserve-data flow (install with replace) to keep plugin config, permissions, and state.
- Use scripts/install-dev.sh as preferred path for build+install+launch.

## 7.3 Testing strategy
- Use layered confidence:
  - Unit tests where present
  - API/integration tests
  - Real device runtime validation (critical)
- For multi-instance and plugin behavior, web API checks and live plugin health indicators are part of acceptance.

## 7.4 Observability standard
- Heavy use of diagnostic logging and runtime health indicators.
- BLE trace capture and export are first-class debugging tools.
- Diagnostic entities/states are treated as product features, not only debug artifacts.

## 7.5 Git and release behavior
- Mainline development with frequent releases/tags in Android repo.
- Focused commits in HACS repos for protocol fixes and deployment readiness.
- Release process in Android repo remains formalized (version bump, build, release artifact).

---

## 8) Documentation Quality and Drift Notes

## Strength
- docs/INTERNALS.md is extensive and currently the deepest technical source of truth.
- Multiple planning/feasibility docs exist for protocol-specific evolution.

## Drift observed
- README/development snippets in some places reflect older version numbers compared to current git tags.
- Handoff work should trust git history and INTERNALS-level docs over high-level README version text when conflicting.

---

## 9) Current Engineering Standards (Implicit + Explicit)

## Design/implementation standards
- Fix root causes, not only symptoms.
- Keep changes focused and avoid unrelated refactors during bug fixes.
- Preserve behavior parity when porting protocols from Android to HA.
- Prefer smallest change that resolves runtime issue.

## Reliability standards
- Treat BLE connection robustness as a first-class concern.
- Build for retries, reconnects, and stale/zombie connection recovery.
- Surface operational health through diagnostics.

## Safety and UX constraints
- Conservative handling around potentially hazardous controls (example pattern documented for cover/awning safety in OneControl domain).
- Keep user-facing semantics stable while improving internals.

## Product positioning standards
- Integrations are community/unofficial unless explicitly vendor-endorsed.
- Avoid implying official vendor endorsement in naming/metadata.

---

## 10) Active Working Conventions for LLM/Human Collaboration

## How work is expected to proceed
- Read existing docs first (especially INTERNALS and plugin-specific docs).
- Reuse established patterns from sibling integrations before inventing new ones.
- Validate changes in live environment when protocol/connection behavior is involved.
- Keep commit scope narrow and descriptive.

## What to check before modifying behavior
- Existing Android plugin implementation for the same protocol.
- Existing HACS repo issue/fix history for equivalent symptoms.
- Whether behavior is transport-level, parser-level, or entity/state-projection-level.

## Preferred debug flow
- Reproduce with targeted logging.
- Identify stage of failure:
  - Discovery/matcher
  - Pairing/bonding
  - GATT connect
  - Auth handshake
  - Notification stream
  - Parsing/state projection
  - Command round-trip
- Patch at the narrowest failing layer.

---

## 11) Integration Porting Method (Recommended Repeatable Template)

Use this sequence for future plugin-to-HACS migrations:

1. Scope protocol boundaries
- Extract BLE services/chars, pairing needs, auth steps, frame format, command map, entity map.

2. Build minimal HA skeleton
- manifest, const, config_flow, coordinator, parser/model, one or two entities, diagnostics.

3. Implement read-path first
- Discovery -> connect -> auth -> notifications -> parse -> sensor entities.

4. Add write-path conservatively
- Introduce only validated command classes first.
- Add retries/debouncing/verification where protocol needs it.

5. Add diagnostics and health exposure
- Connection/auth/data-health and key protocol state.

6. Validate on live hardware
- Confirm both happy path and reconnect/error behavior.

7. Harden deployability
- Ensure proper dependencies, logging guidance, and recovery behavior.

---

## 12) Known Risk Areas and Mitigations

## Risk: BLE pairing differences across gateway generations
- Mitigation: detect pairing method from advertisement/protocol hints; branch behavior explicitly.

## Risk: transport-layer fragility in HA BLE environment
- Mitigation: use HA bluetooth helpers and bleak-retry-connector patterns.

## Risk: stale or ignored discovery states in HA
- Mitigation: inspect HA storage/config entry states; redeploy complete component when loader issues arise.

## Risk: branding/icon confusion
- Mitigation: treat icon rendering as domain/brands pipeline concern, not only repo-local asset concern.

## Risk: environment drift after host reboot/network changes
- Mitigation: keep routing/forwarding checks and persistence as standard runbook items.

---

## 13) Immediate Next-Step Backlog (State-Oriented)

### Android bridge repo
- Resolve whether ble-mqtt-plugin-app.code-workspace local modifications should be committed, reverted, or ignored.
- Continue release hygiene and doc/version alignment.

### HACS repos
- Continue connection robustness and pairing edge-case hardening where needed.
- Optionally formalize brand asset publication strategy via Brands pipeline for domains without core coverage.

### Cross-repo
- Keep Android protocol implementation and HACS ports behaviorally aligned.
- Use one fix pattern across repos where transport failure modes match.

---

## 14) Fast Resume Checklist for Next LLM Session

Start with this order:

1. Confirm repo statuses and recent commits:
- android_ble_plugin_bridge
- ha-onecontrol
- ha-gopower
- ha-mopeka

2. Read these docs first:
- docs/INTERNALS.md
- docs/DEVELOPMENT.md
- docs/HACS_REVISIT_FEASIBILITY.md
- docs/ONECONTROL_HACS_INVENTORY.md
- TEST_PLAN.md

3. Identify current task stream category:
- Android protocol/runtime bug
- HACS port feature/fix
- Deployment/runtime environment issue
- Branding/metadata/discovery issue

4. Execute with live validation path:
- Build/deploy target
- Capture logs/diagnostics
- Patch narrow layer
- Revalidate in real environment

5. Keep commit granularity small and message intent explicit.

---

## 15) Bottom Line

This workspace is now a hybrid platform:
- Product 1: Android BLE/HTTP -> MQTT bridge app (still active and evolving).
- Product 2+: Native HA custom integrations ported from Android plugin knowledge.

The strongest continuity strategy is to treat Android plugin logic as protocol truth, while implementing HA-native coordinator/entity architecture per integration domain with robust BLE connection patterns and runtime-first validation.

---

Document generated: 2026-02-22
