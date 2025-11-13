# Data Model – Diagnostics Framework

## DiagnosticSession
- **Fields**: `SessionId` (GUID), `Name` (string), `StartedAt` (DateTime), `Configuration` (sink list + options), `IsEnabled` (bool), `Metadata` (key/value bag).
- **Responsibilities**: Tracks opt-in configuration, forwards incoming events/contexts to the dispatcher, and exposes helpers for creating contexts. It does **not** store events or dumps.
- **Validation**: `Sinks` list can be empty (diagnostics allowed off), but when enabled, configuration must specify at least one sink or observer.

## DiagnosticEvent
- **Fields**: `EventId` (GUID), `SessionId`, `Category` (string identifier such as `style.compute`), `Kind` (string: `start`, `stop`, `error`, `dump`, etc.), `Timestamp` (long ticks), `Payload` (arbitrary object or dictionary with metrics, reasoning, warnings), `Dump` (embedded `StructuredDumpMetadata` when applicable).
- **Flow**: Constructed by stages and immediately dispatched to the sink pipeline; sessions do not retain event collections.
- **Notes**: Categories replace rigid enums; teams can add custom identifiers without contract churn.

## StructuredDumpMetadata
- **Fields**: `Format` (string, e.g., `json`), `Summary` (string), `NodeCount` (int), `Body` (string/byte[]).
- **Purpose**: Carries dump details inline within events so sinks can persist or ignore them without separate lookups.

## DiagnosticContext
- **Fields**: `ContextId` (GUID), `SessionId`, `Name` (string), `Values` (key/value bag), `OpenedAt`/`ClosedAt` timestamps.
- **Behavior**: Created via `session.Context("Scenario")`, collects `Set(key,value)` calls, and on disposal produces its own `DiagnosticEvent` (e.g., category `context`, kind `detail`) whose payload contains the captured values.

## DiagnosticsModel (Ephemeral)
- **Definition**: Transient aggregation per sink invocation composed of the current event plus its associated contexts and dumps.
- **Role**: Provides a convenient object for sinks to consume without requiring them to traverse session state. Not persisted or stored between events.

## DiagnosticSink
- **Fields**: `SinkId` (string), `Type` (JSON, Console, InMemory, Custom), `Configuration` (dictionary), `Status` (Active/Disabled/CircuitBroken).
- **Behavior**: Receives each event synchronously; may store or emit data according to its type. Circuit-broken sinks stop receiving events until reset.

## Relationships Overview
- Sessions own configuration and context helpers only.
- Events flow from stages → dispatcher → sinks; they are not stored on the session.
- Contexts and dumps attach to events but are not modeled as persistent collections.
