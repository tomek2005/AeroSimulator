# AeroSim – Full Developer Specification

> **Course:** Paradygmaty Programowania | **Language:** C# (.NET 8)  
> **Target size:** ~9,000–10,000 LOC | **Type:** Real-time console game / flight simulator  
> **Design patterns used:** State, Strategy, Observer, Command, MVC, Factory, Singleton

---

## Table of Contents

1. [Project Overview & Gameplay](#1-project-overview--gameplay)
2. [Directory Structure](#2-directory-structure)
3. [Module: Core/Aircraft](#3-module-coreaircraftcore)
4. [Module: Core/States (Pattern: State)](#4-module-corestates--pattern-state)
5. [Module: Core/Strategies (Pattern: Strategy)](#5-module-corestrategies--pattern-strategy)
6. [Module: Core/Events (Pattern: Observer)](#6-module-coreevents--pattern-observer)
7. [Module: Core/Commands (Pattern: Command)](#7-module-corecommands--pattern-command)
8. [Module: Controllers (MVC – Controller)](#8-module-controllers--mvc-controller)
9. [Module: Views (MVC – View)](#9-module-views--mvc-view)
10. [Module: Infrastructure](#10-module-infrastructure)
11. [Gameplay Loop & Input Map](#11-gameplay-loop--input-map)
12. [Black Box & Flight Logging](#12-black-box--flight-logging)
13. [Implementation Roadmap](#13-implementation-roadmap)
14. [Team Task Split](#14-team-task-split)
15. [LOC Estimate per Module](#15-loc-estimate-per-module)
16. [Grading Checklist](#16-grading-checklist)

---

## 1. Project Overview & Gameplay

AeroSim is a **real-time console flight simulator** controlled entirely via keyboard. The player pilots an aircraft through a full flight cycle — engine startup, taxi, takeoff, climb, cruise, descent, approach, and landing — while reacting to random in-flight events such as engine failures, severe turbulence, and electrical outages.

### Gameplay summary

```
[STARTUP SCREEN]
  Select aircraft → Boeing 737-800 / Airbus A320 / Cessna 172
  Select difficulty → Easy (no anomalies) / Normal / Hard (all anomalies)

[MAIN LOOP – runs at 10 Hz]
  ┌─────────────────────────────────────────────────────────┐
  │  aircraft.Update(deltaT)          ← physics simulation  │
  │  anomalyEngine.Tick(deltaT)       ← random events       │
  │  view.Render(aircraft)            ← redraws console UI  │
  │  input = inputHandler.Poll()      ← reads keyboard      │
  │  controller.Execute(input)        ← dispatches command  │
  └─────────────────────────────────────────────────────────┘

[EVENTS]
  Random anomalies pop up mid-flight with a WARNING banner.
  Player must press the correct key to respond (e.g. [R] Restart Engine).
  Unresolved critical anomalies → crash (game over).

[END OF FLIGHT]
  Flight Report printed: duration, fuel used, anomalies handled,
  landing score, full command history (black box).
```

### Console dashboard layout (ASCII)

```
+==================================================================+
| ✈ AeroSim  |  SP-LRA  Boeing 737-800  |  FLT TIME: 01:14:32     |
| STATE: [CRUISE]                        |  AUTOPILOT: ON          |
+==============+==============+===========+========================+
| ALT: 35000ft | SPD: 461 kts | HDG: 087° | V/S:    0 ft/min       |
| THR:  78%    | RPM:  92%    | GFR: 1.0g | TEMP: 745°C            |
+==============+==============+===========+========================+
| FUEL [████████████░░░░] 68%  12,400 kg         RANGE: ~2100 km  |
+------------------------------------------------------------------+
| SYSTEMS:  ENG1 [OK]  ENG2 [OK]  HYD [OK]  ELEC [98%]  NAV [OK] |
+------------------------------------------------------------------+
| WEATHER: THUNDERSTORM  |  WIND: 270°/45 kts  |  TURBULENCE: HIGH|
+------------------------------------------------------------------+
| !! ALERT: ENGINE 1 TEMPERATURE HIGH (845°C) – reduce throttle !!|
+------------------------------------------------------------------+
| CONTROLS: [W/S] Throttle  [A/D] Heading  [Q] Autopilot          |
|           [SPACE] Next Phase  [E] Emergency  [R] Resolve  [ESC] |
+==================================================================+
```

---

## 2. Directory Structure

```
AeroSim/
├── Core/
│   ├── Aircraft/
│   │   ├── Aircraft.cs
│   │   ├── FlightData.cs
│   │   ├── FlightDataSnapshot.cs
│   │   ├── AircraftConfig.cs
│   │   ├── Systems/
│   │   │   ├── IAvionicSystem.cs
│   │   │   ├── EngineSystem.cs
│   │   │   ├── FuelSystem.cs
│   │   │   ├── NavigationSystem.cs
│   │   │   ├── HydraulicSystem.cs
│   │   │   ├── ElectricalSystem.cs
│   │   │   ├── WeatherSystem.cs
│   │   │   └── AutopilotSystem.cs
│   │   └── Enums/
│   │       ├── SystemType.cs
│   │       ├── SystemStatus.cs
│   │       └── Severity.cs
│   ├── States/
│   │   ├── IAircraftState.cs
│   │   ├── GroundState.cs
│   │   ├── TaxiState.cs
│   │   ├── TakeOffState.cs
│   │   ├── ClimbState.cs
│   │   ├── CruiseState.cs
│   │   ├── DescentState.cs
│   │   ├── LandingState.cs
│   │   ├── HoldingState.cs
│   │   ├── EmergencyState.cs
│   │   └── CriticalState.cs
│   ├── Strategies/
│   │   ├── Anomalies/
│   │   │   ├── IAnomaly.cs
│   │   │   ├── AbstractAnomaly.cs
│   │   │   ├── EngineFailureAnomaly.cs
│   │   │   ├── BirdStrikeAnomaly.cs
│   │   │   ├── HydraulicFailureAnomaly.cs
│   │   │   ├── FuelLeakAnomaly.cs
│   │   │   ├── ElectricalFailureAnomaly.cs
│   │   │   ├── DecompressionAnomaly.cs
│   │   │   ├── TurbulenceAnomaly.cs
│   │   │   ├── IcingAnomaly.cs
│   │   │   ├── RunwayIncursionAnomaly.cs
│   │   │   └── MicroburstAnomaly.cs
│   │   └── Weather/
│   │       ├── IWeatherStrategy.cs
│   │       ├── ClearSkiesStrategy.cs
│   │       ├── ThunderstormStrategy.cs
│   │       ├── FogStrategy.cs
│   │       ├── CrosswindStrategy.cs
│   │       ├── IcingConditionsStrategy.cs
│   │       └── WindShearStrategy.cs
│   ├── Events/
│   │   ├── FlightEvent.cs
│   │   ├── EventBus.cs
│   │   ├── IFlightEventHandler.cs
│   │   └── Handlers/
│   │       ├── FlightLoggerHandler.cs
│   │       ├── AlertSystemHandler.cs
│   │       ├── BlackBoxHandler.cs
│   │       └── StatisticsHandler.cs
│   └── Commands/
│       ├── IFlightCommand.cs
│       ├── CommandHistory.cs
│       ├── SetThrottleCommand.cs
│       ├── SetHeadingCommand.cs
│       ├── SetAltitudeCommand.cs
│       ├── ToggleAutopilotCommand.cs
│       ├── ActivateSystemCommand.cs
│       ├── ResolveAnomalyCommand.cs
│       └── EmergencyDeclareCommand.cs
├── Controllers/
│   ├── FlightController.cs
│   ├── InputHandler.cs
│   └── AnomalyEngine.cs
├── Views/
│   ├── IFlightView.cs
│   ├── ConsoleDashboardView.cs
│   ├── StartupScreen.cs
│   ├── FlightReportView.cs
│   └── Components/
│       ├── AltimeterWidget.cs
│       ├── AirspeedWidget.cs
│       ├── FuelGaugeWidget.cs
│       ├── SystemsPanelWidget.cs
│       ├── AlertsBarWidget.cs
│       └── ActionMenuWidget.cs
├── Infrastructure/
│   ├── SimulationConfig.cs
│   ├── FlightLogger.cs
│   ├── FlightReport.cs
│   ├── AircraftFactory.cs
│   ├── AnomalyFactory.cs
│   └── WeatherFactory.cs
└── Program.cs
```

---

## 3. Module: Core/Aircraft

---

### 3.1 `Aircraft.cs`

Central domain class. Aggregates all avionics systems and the current state machine.  
**Namespace:** `AeroSim.Core.Aircraft`

#### Fields (private)

| Field | Type | Description |
|---|---|---|
| `_currentState` | `IAircraftState` | Active state (State pattern) |
| `_flightData` | `FlightData` | Live telemetry data |
| `_engine` | `EngineSystem` | Engine system |
| `_navigation` | `NavigationSystem` | GPS / autopilot |
| `_fuel` | `FuelSystem` | Fuel tanks and flow |
| `_hydraulics` | `HydraulicSystem` | Gear, flaps, brakes |
| `_electrical` | `ElectricalSystem` | Power buses |
| `_weather` | `WeatherSystem` | Atmospheric data |
| `_eventBus` | `EventBus` | Observer event bus |
| `_config` | `AircraftConfig` | Static aircraft specs |

#### Properties (public)

| Property | Type | get/set | Description |
|---|---|---|---|
| `TailNumber` | `string` | get | Registration (e.g. "SP-LRA") |
| `Model` | `string` | get | Aircraft model name |
| `FlightData` | `FlightData` | get | Live telemetry (read-only ref) |
| `CurrentState` | `IAircraftState` | get | Active state object |
| `Config` | `AircraftConfig` | get | Static aircraft config |
| `AllSystems` | `IReadOnlyList<IAvionicSystem>` | get | All avionics systems |

#### Constructor

| Signature | Parameters | Description |
|---|---|---|
| `Aircraft(string tailNumber, string model, AircraftConfig config)` | `tailNumber` – registration string; `model` – display name; `config` – aircraft specs | Initializes all systems, sets initial state to `GroundState`, subscribes all default event handlers |

#### Methods

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `TakeOff()` | — | `void` | Delegates to `_currentState.TakeOff(this)` |
| `Cruise()` | — | `void` | Delegates to `_currentState.Cruise(this)` |
| `Descend()` | — | `void` | Delegates to `_currentState.Descend(this)` |
| `Land()` | — | `void` | Delegates to `_currentState.Land(this)` |
| `DeclareEmergency()` | — | `void` | Delegates to `_currentState.HandleEmergency(this)` |
| `Abort()` | — | `void` | Delegates to `_currentState.Abort(this)` |
| `Update(double deltaT)` | `deltaT` – elapsed seconds since last frame | `void` | Main simulation tick. Calls state update, then updates all systems |
| `TransitionTo(IAircraftState newState)` | `newState` – the next state to enter | `void` | Exits old state, enters new state, publishes `StateChangedEvent` |
| `ApplyDamage(SystemType system, double severity)` | `system` – which system; `severity` – 0.0–1.0 | `void` | Calls `IAvionicSystem.ApplyDamage()` on the specified system |
| `GetSystemStatus(SystemType system)` | `system` – enum value | `SystemStatus` | Returns current `SystemStatus` of specified system |
| `GetSystemHealth(SystemType system)` | `system` – enum value | `double` | Returns health 0.0–1.0 of specified system |
| `Subscribe(IFlightEventHandler handler)` | `handler` – event listener | `void` | Registers handler on `EventBus` |
| `Publish(FlightEvent evt)` | `evt` – event to broadcast | `void` | Forwards event to `EventBus.Publish()` |

---

### 3.2 `FlightData.cs`

Mutable data bag holding all live telemetry values. Updated every simulation tick.  
**Namespace:** `AeroSim.Core.Aircraft`

#### Properties

| Property | Type | Unit | Description |
|---|---|---|---|
| `Altitude` | `double` | feet | Current altitude MSL |
| `Speed` | `double` | knots (IAS) | Indicated airspeed |
| `TrueAirspeed` | `double` | knots | Computed from IAS + altitude |
| `VerticalSpeed` | `double` | ft/min | Rate of climb/descent |
| `Heading` | `double` | degrees 0–360 | Magnetic heading |
| `Latitude` | `double` | decimal degrees | GPS latitude |
| `Longitude` | `double` | decimal degrees | GPS longitude |
| `Throttle` | `double` | 0.0–1.0 | Engine throttle lever position |
| `EngineRPM` | `double` | % | Engine RPM as percentage of max |
| `EngineTempC` | `double` | Celsius | Engine exhaust gas temperature |
| `FuelLevelKg` | `double` | kg | Current fuel mass |
| `FuelFlowKgPerH` | `double` | kg/h | Current fuel burn rate |
| `FuelCapacityKg` | `double` | kg | Maximum fuel capacity (from config) |
| `WindSpeedKnots` | `double` | knots | Current wind speed |
| `WindDirectionDeg` | `double` | degrees | Wind direction (from) |
| `AirPressureHPa` | `double` | hPa | Barometric pressure |
| `TemperatureC` | `double` | Celsius | Outside air temperature |
| `FlightTime` | `TimeSpan` | — | Elapsed time since wheels-up |
| `GForce` | `double` | g | Current G-loading |
| `PitchAngleDeg` | `double` | degrees | Aircraft pitch attitude |
| `RollAngleDeg` | `double` | degrees | Aircraft roll attitude |

#### Methods

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `FuelRemainingPercent()` | — | `double` | `FuelLevelKg / FuelCapacityKg * 100` |
| `EstimatedRangeKm()` | — | `double` | Calculates remaining range at current burn rate |
| `IsStalling()` | — | `bool` | Returns `true` if speed below stall speed for current config |
| `IsOverspeed()` | — | `bool` | Returns `true` if speed exceeds VMO/MMO |
| `Snapshot()` | — | `FlightDataSnapshot` | Returns an immutable copy of all fields at this moment |
| `ToTelemetryString()` | — | `string` | One-line CSV-style string for logging |
| `Reset()` | — | `void` | Resets all values to ground defaults |

---

### 3.3 `FlightDataSnapshot.cs`

Immutable record capturing a moment-in-time copy of `FlightData`. Used by `CommandHistory` and `BlackBoxHandler`.

```csharp
public record FlightDataSnapshot(
    double Altitude, double Speed, double VerticalSpeed, double Heading,
    double Throttle, double EngineRPM, double EngineTempC,
    double FuelLevelKg, double GForce, double PitchAngleDeg, double RollAngleDeg,
    TimeSpan FlightTime, DateTime CapturedAt
);
```

---

### 3.4 `AircraftConfig.cs`

Immutable record holding static aircraft performance parameters.

```csharp
public record AircraftConfig
{
    public double MaxFuelKg        { get; init; }
    public double MaxAltitudeFt    { get; init; }
    public double CruiseSpeedKts   { get; init; }
    public double MaxSpeedKts      { get; init; }  // VMO
    public double StallSpeedKts    { get; init; }  // clean config
    public double StallSpeedFlaps  { get; init; }  // flaps extended
    public int    EngineCount      { get; init; }
    public double MaxThrustKN      { get; init; }  // total
    public double MaxAltitudeClimbRateFtMin { get; init; }
    public double NormalDescentRateFtMin    { get; init; }
    public double V1SpeedKts       { get; init; }  // decision speed
    public double VRSpeedKts       { get; init; }  // rotation speed
    public double V2SpeedKts       { get; init; }  // takeoff safety speed
    public double MaxCrosswindKts  { get; init; }
}
```

---

### 3.5 `IAvionicSystem.cs`

Interface that every avionics system must implement.

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `Name` (property) | — | `string` | Display name (e.g. "ENGINE 1") |
| `Status` (property) | — | `SystemStatus` | `OK`, `Degraded`, `Failed` |
| `Health` (property) | — | `double` | 0.0 (destroyed) to 1.0 (perfect) |
| `Update(double deltaT, FlightData data)` | `deltaT` – seconds; `data` – current telemetry | `void` | Simulate one tick of this system |
| `ApplyDamage(double severity)` | `severity` – 0.0–1.0 | `void` | Reduces `Health` by `severity`, may change `Status` |
| `Repair(double amount)` | `amount` – 0.0–1.0 | `void` | Restores `Health` by `amount`, clamps to 1.0 |
| `GenerateReport()` | — | `SystemReport` | Returns structured report with health, status, recent errors |

---

### 3.6 Avionics System Classes

All implement `IAvionicSystem`. Only non-obvious methods listed.

#### `EngineSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Start()` | — | `bool` | Attempts engine start. Returns `false` if health < 0.2 or fuel empty |
| `Stop()` | — | `void` | Cuts fuel to engine, RPM decays to 0 |
| `Restart()` | — | `bool` | In-flight restart attempt. Success depends on health and altitude |
| `CoolDown(double deltaT)` | `deltaT` – seconds | `void` | Decreases `EngineTempC` when throttle is reduced |
| `CalculateThrust(double throttle)` | `throttle` – 0.0–1.0 | `double` | Returns thrust in kN based on throttle, health, and altitude |
| `IsOverheating` (property) | — | `bool` | `true` if `EngineTempC > 900` |
| `ThrustKN` (property) | — | `double` | Current actual thrust output in kN |

#### `FuelSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Refuel(double kg)` | `kg` – amount to add | `void` | Adds fuel, clamps to `MaxFuelKg` |
| `Burn(double kgPerH, double deltaT)` | `kgPerH` – burn rate; `deltaT` – seconds | `void` | Reduces `FuelLevelKg`, publishes `FuelLowEvent` at 15% |
| `StartLeak(double rateKgPerH)` | `rateKgPerH` – leak rate | `void` | Activates a fuel leak with given rate |
| `SealLeak()` | — | `bool` | Attempts to seal active leak. Returns `false` if system health < 0.3 |
| `EmergencyDump()` | — | `void` | Rapidly dumps fuel to reduce landing weight |
| `LeakRate` (property) | — | `double` | Current kg/h being lost to leak (0 if none) |
| `IsLeaking` (property) | — | `bool` | `true` if active leak exists |

#### `NavigationSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `SetWaypoint(double lat, double lon)` | GPS coordinates | `void` | Sets next navigation waypoint |
| `CalculateBearing(double toLat, double toLon)` | target coordinates | `double` | Returns bearing in degrees to target |
| `DistanceToWaypointKm()` | — | `double` | Returns km to current waypoint |
| `EngageAutopilot()` | — | `bool` | Engages autopilot if health OK. Returns success |
| `DisengageAutopilot()` | — | `void` | Disengages autopilot |
| `IsAutopilotEngaged` (property) | — | `bool` | Autopilot status |

#### `HydraulicSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `DeployGear()` | — | `bool` | Extends landing gear. Returns `false` if hydraulics failed |
| `RetractGear()` | — | `bool` | Retracts landing gear |
| `EmergencyGearExtension()` | — | `void` | Gravity-drops gear without hydraulics (always succeeds) |
| `SetFlaps(int position)` | `position` – 0 to 5 | `void` | Sets flap position (0=up, 5=full) |
| `IsGearDown` (property) | — | `bool` | Landing gear position |
| `FlapsPosition` (property) | — | `int` | Current flap setting 0–5 |
| `Pressure` (property) | — | `double` | Hydraulic pressure in PSI |

#### `ElectricalSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `SwitchToBackupBattery()` | — | `void` | Routes power from backup batteries |
| `SwitchToAPU()` | — | `bool` | Starts APU. Returns `false` if APU unavailable |
| `GetBusPower(BusType bus)` | `bus` – Main, Secondary, Emergency | `double` | Returns voltage on given bus |
| `IsSystemPowered(SystemType sys)` | `sys` – system to check | `bool` | Returns whether given system has power |
| `PowerLevel` (property) | — | `double` | Overall power level 0.0–1.0 |

#### `AutopilotSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Engage(AutopilotMode mode)` | `mode` – ALT_HOLD, HDG, LNAV, VNAV, APPROACH | `bool` | Engages specified autopilot mode |
| `Disengage()` | — | `void` | Fully disconnects autopilot |
| `SetTargetAltitude(double feet)` | `feet` – target altitude | `void` | Sets altitude hold target |
| `SetTargetHeading(double degrees)` | `degrees` – target heading | `void` | Sets heading bug |
| `SetTargetSpeed(double knots)` | `knots` – target IAS | `void` | Sets autothrottle target |
| `Update(double deltaT, FlightData data)` | — | `void` | Computes and applies control corrections |
| `ActiveMode` (property) | — | `AutopilotMode` | Currently active mode |

---

## 4. Module: Core/States – Pattern: State

The State pattern allows `Aircraft` to change its behavior completely based on the current flight phase without any `if/switch` chains. Each state is a self-contained class.

### 4.1 `IAircraftState.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `StateName` (property) | — | `string` | Display name (e.g. "CRUISE") |
| `StateDescription` (property) | — | `string` | One-line description for UI |
| `StateColor` (property) | — | `ConsoleColor` | Color for dashboard header |
| `AllowedActions` (property) | — | `IReadOnlyList<string>` | Actions available in this state (for ActionMenuWidget) |
| `TakeOff(Aircraft ctx)` | `ctx` – the aircraft | `void` | Handle takeoff request |
| `Cruise(Aircraft ctx)` | `ctx` – the aircraft | `void` | Handle cruise transition |
| `Descend(Aircraft ctx)` | `ctx` – the aircraft | `void` | Handle descent request |
| `Land(Aircraft ctx)` | `ctx` – the aircraft | `void` | Handle landing request |
| `HandleEmergency(Aircraft ctx)` | `ctx` – the aircraft | `void` | Handle emergency declaration |
| `Abort(Aircraft ctx)` | `ctx` – the aircraft | `void` | Handle abort request |
| `Update(Aircraft ctx, double deltaT)` | `ctx` – the aircraft; `deltaT` – seconds | `void` | Per-frame simulation logic for this state |
| `OnEnter(Aircraft ctx)` | `ctx` – the aircraft | `void` | Called once when transitioning into this state |
| `OnExit(Aircraft ctx)` | `ctx` – the aircraft | `void` | Called once when transitioning out of this state |

---

### 4.2 State Classes

#### `GroundState.cs`

Aircraft is parked or on the ground with engines stopped or at idle.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Sets speed=0, verticalSpeed=0, retracts gear flag |
| `TakeOff(ctx)` | Checks: fuel > 10%, all systems OK. If pass → `ctx.TransitionTo(new TakeOffState())`. If fail → publishes `AlertEvent("NOT READY FOR TAKEOFF")` |
| `Cruise(ctx)` | Publishes alert: "Cannot cruise on the ground" |
| `Descend(ctx)` | Publishes alert: "Already on the ground" |
| `Land(ctx)` | Publishes alert: "Already on the ground" |
| `HandleEmergency(ctx)` | Publishes alert: "Ground emergency – call fire services" |
| `Abort(ctx)` | No-op |
| `Update(ctx, deltaT)` | Simulates engine warm-up, fuel level if refueling, boarding countdown |

**Private fields:**
- `bool IsEngineRunning`
- `int GateNumber`
- `double RefuelRate` (kg/s)

---

#### `TaxiState.cs`

Aircraft moves along the taxiway to the runway.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Sets speed=15kts, engages tiller steering |
| `TakeOff(ctx)` | If at correct runway position → `TransitionTo(new TakeOffState())` |
| `Abort(ctx)` | Returns to nearest gate → `TransitionTo(new GroundState())` |
| `Update(ctx, deltaT)` | Advances taxi position, checks runway hold-short lines |

---

#### `TakeOffState.cs`

Acceleration on runway, rotation, and initial climb to 1,500 ft AGL.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Sets throttle=1.0, arms spoilers, arms auto-rotate at VR |
| `Update(ctx, deltaT)` | Increases `Speed` based on thrust vs drag; at VR → sets `PitchAngleDeg=+7.5`; at V2 → starts climbing; at 1500ft → `TransitionTo(new ClimbState())` |
| `Abort(ctx)` | If speed < V1 → rejected takeoff: cut throttle, apply max brakes, `TransitionTo(new GroundState())` |
| `HandleEmergency(ctx)` | If speed > V1 → continue and `TransitionTo(new EmergencyState())`. If speed < V1 → `Abort()` |
| `Cruise(ctx)` | Alert: "Cannot cruise during takeoff" |

**Private fields:**
- `double V1Speed`, `double VRSpeed`, `double V2Speed`
- `bool HasRotated`
- `double RotationAngle`

---

#### `ClimbState.cs`

Steady climb to cruise altitude (e.g. FL350 = 35,000 ft).

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Sets target altitude from config, retracts gear and flaps |
| `Update(ctx, deltaT)` | Increases altitude at `ClimbRate` ft/min; adjusts speed; when target altitude reached → auto-calls `Cruise(ctx)` |
| `Cruise(ctx)` | `TransitionTo(new CruiseState())` |
| `HandleEmergency(ctx)` | `TransitionTo(new EmergencyState())` |
| `Abort(ctx)` | Immediate descent → `TransitionTo(new DescentState())` |

**Private fields:**
- `double TargetAltitude`
- `double ClimbRate` (ft/min, varies with altitude)
- `ClimbPhase Phase` (Gear Up / Flaps Retract / Climb Power / Level Off)

---

#### `CruiseState.cs`

Main flight phase. Autopilot active. Anomalies can spawn here.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Engages autopilot ALT_HOLD + HDG, sets cruise throttle |
| `Update(ctx, deltaT)` | Simulates fuel burn, wind effects, waypoint tracking; if `FuelPercent < 5%` → `HandleEmergency()` |
| `Descend(ctx)` | `TransitionTo(new DescentState())` |
| `HandleEmergency(ctx)` | `TransitionTo(new EmergencyState())` |
| `TakeOff(ctx)` | Alert: "Already airborne" |
| `Land(ctx)` | Alert: "Must descend first" |

---

#### `DescentState.cs`

Descent from cruise altitude toward approach altitude (~3,000 ft).

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Sets throttle=0.3, begins extending flaps incrementally |
| `Update(ctx, deltaT)` | Reduces altitude at `DescentRate` ft/min; reduces speed; at 3000ft and speed < 180kts → auto-calls `Land(ctx)` |
| `Land(ctx)` | `TransitionTo(new LandingState())` |
| `HandleEmergency(ctx)` | `TransitionTo(new EmergencyState())` |
| `Abort(ctx)` | Go-around: increase throttle, `TransitionTo(new ClimbState())` |

**Private fields:**
- `double DescentRate` (ft/min)
- `int FlapsPosition` (0–5)
- `DescentPhase Phase` (Cruise Descent / Approach / Final)

---

#### `LandingState.cs`

Final approach ILS tracking, flare, touchdown, rollout.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Full flaps, gear down, ILS intercept |
| `Update(ctx, deltaT)` | Tracks ILS glideslope (3°); applies crosswind correction; at 50ft → flare (pitch up +2°); touchdown: records speed, applies brakes, decelerates to 0 → `TransitionTo(new GroundState())` |
| `Abort(ctx)` | Go-around: full throttle, gear up, `TransitionTo(new ClimbState())` |
| `HandleEmergency(ctx)` | Continues landing attempt but transitions to `EmergencyState` for logging |

**Private fields:**
- `double ILSDeviation` (dots, -2.5 to +2.5)
- `double GlideslopeAngle`
- `double TouchdownSpeed`
- `LandingPhase Phase` (Approach / Flare / Rollout)
- `bool IsGearDown`

---

#### `HoldingState.cs`

Aircraft circles in a holding pattern waiting for clearance.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Records holding fix position, sets bank angle 25° for turns |
| `Update(ctx, deltaT)` | Flies racetrack pattern (1-min legs, standard turns); counts fuel, increments `LoopCount` |
| `Land(ctx)` | `TransitionTo(new DescentState())` (given landing clearance) |
| `HandleEmergency(ctx)` | If fuel critical → immediate `TransitionTo(new DescentState())` |

**Private fields:**
- `double HoldFixLat`, `double HoldFixLon`
- `double HoldInboundCourse`
- `int LoopCount`
- `HoldingLeg CurrentLeg` (Inbound / Turn1 / Outbound / Turn2)

---

#### `EmergencyState.cs`

Activated from any state. Identifies failure type and manages emergency response.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Publishes `MAYDAYEvent`, logs emergency declaration, sets emergency frequency |
| `Update(ctx, deltaT)` | Degrades affected systems further; tracks time to nearest airport; if severity escalates → `TransitionTo(new CriticalState())` |
| `Land(ctx)` | Forced immediate approach → `TransitionTo(new LandingState())` |
| `HandleEmergency(ctx)` | Escalate → `TransitionTo(new CriticalState())` |
| `Abort(ctx)` | No-op (cannot abort an emergency) |

**Private fields:**
- `EmergencyType Type` (EngineFailure, Fire, Decompression, etc.)
- `double Severity` (0.0–1.0)
- `double TimeToNearestAirportMin`
- `bool MaydayDeclared`

---

#### `CriticalState.cs`

Aircraft is in unrecoverable or near-unrecoverable failure. Player has limited control.

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Disables autopilot, removes most actions from `AllowedActions`, rapid system degradation begins |
| `Update(ctx, deltaT)` | Each tick reduces control authority; tracks altitude; if altitude < 0 → crash (game over); if somehow landed → `TransitionTo(new GroundState())` with crash-landing report |
| `Land(ctx)` | Last-ditch crash landing attempt |

---

## 5. Module: Core/Strategies – Pattern: Strategy

---

### 5.1 `IAnomaly.cs`

Interface for all in-flight anomalies (the Strategy family for failures).

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `AnomalyName` (property) | — | `string` | Display name ("ENGINE FAILURE") |
| `Description` (property) | — | `string` | Short description for pilot |
| `Level` (property) | — | `Severity` | Low / Medium / High / Critical |
| `Probability` (property) | — | `double` | Chance per second of triggering (0.0–1.0) |
| `IsActive` (property) | — | `bool` | Whether this anomaly is currently active |
| `CanBeResolved` (property) | — | `bool` | Whether pilot action can fix it |
| `Trigger(Aircraft ctx, FlightData data)` | `ctx` – aircraft; `data` – current telemetry | `void` | Activates the anomaly, applies immediate effect |
| `Update(Aircraft ctx, FlightData data)` | same | `void` | Called each tick while anomaly is active |
| `Resolve(Aircraft ctx)` | `ctx` – aircraft | `bool` | Pilot attempts fix. Returns `true` if successful |
| `GetWarningMessage()` | — | `string` | Short warning for alerts bar |
| `GetPilotAction()` | — | `string` | Instruction text ("Press [R] to restart engine") |

---

### 5.2 `AbstractAnomaly.cs`

Abstract base implementing common logic (timer, active flag, event publishing).

```csharp
public abstract class AbstractAnomaly : IAnomaly
{
    protected bool   _isActive;
    protected double _activeDuration;   // seconds since triggered
    protected Random _rng;

    // Shared helpers
    protected void PublishAlert(Aircraft ctx, string message, Severity level);
    protected bool CheckProbability(double deltaT);   // true if should trigger this tick
}
```

---

### 5.3 Anomaly Implementations

#### `EngineFailureAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Sets `EngineSystem.Health = 0`, calls `Stop()`. RPM decays to 0. Publishes `SystemFailureEvent`. Sets aircraft into mild descent |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Monitors altitude loss, increases other engine temperature, checks if pilot is attempting restart |
| `Resolve(ctx)` | aircraft | `bool` | Calls `EngineSystem.Restart()`. Returns `false` if health < 0.2 or altitude < 5000ft |

**Properties:** `Level = High`, `CanBeResolved = true`, `Probability = 0.0002` per second

---

#### `BirdStrikeAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Applies `ApplyDamage(SystemType.Engine, 0.3)`. Publishes vibration effect (GForce spike +0.5g). Drops engine health by 30% |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Adds engine vibration to telemetry display (GForce oscillates ±0.15g) |
| `Resolve(ctx)` | aircraft | `bool` | `CanBeResolved = false` – requires maintenance |

**Properties:** `Level = Medium`, only triggers at `Altitude < 10000ft`

---

#### `HydraulicFailureAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Sets hydraulic pressure to 0. `HydraulicSystem.ApplyDamage(1.0)`. Flaps and gear now blocked |
| `Update(ctx, data)` | aircraft, telemetry | `void` | If gear needed (landing) → warns pilot to use emergency extension |
| `Resolve(ctx)` | aircraft | `bool` | Only partially: calls `HydraulicSystem.EmergencyGearExtension()`. Flaps remain stuck |

---

#### `FuelLeakAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Calls `FuelSystem.StartLeak(leakRate)` where `leakRate` = 80–250 kg/h random |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Monitors fuel level; if < 5% → publishes `FuelCriticalEvent`; updates leak display in fuel gauge |
| `Resolve(ctx)` | aircraft | `bool` | Calls `FuelSystem.SealLeak()`. Returns result |

---

#### `ElectricalFailureAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Drops main bus voltage to 0. Navigation and autopilot go offline |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Increments degradation of systems that lack power; after 30s → secondary bus fails too |
| `Resolve(ctx)` | aircraft | `bool` | Calls `ElectricalSystem.SwitchToBackupBattery()`. Returns `true` |

---

#### `DecompressionAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Only valid if `data.Altitude > 25000`. Publishes MAYDAY. Sets target altitude to 10,000ft immediately |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Forces rapid descent; if pilot doesn't descend within 60s → pilot incapacitation (game over) |
| `Resolve(ctx)` | aircraft | `bool` | `CanBeResolved = false` – must descend below 10,000ft (automatic on resolve) |

---

#### `TurbulenceAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Sets turbulence active flag; `Level` randomly chosen Low–High |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Each tick: `Altitude += Random(-200, 200)` ft, `Speed += Random(-15, 15)` kts, `GForce = 1.0 + Random(-0.8, 0.8)` |
| `Resolve(ctx)` | aircraft | `bool` | `CanBeResolved = true` – pilot changes altitude ±2000ft. Auto-resolves after 3–8 minutes |

---

#### `IcingAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Only if `TempC < 0` and humidity > 80%. Increases effective aircraft weight, changes stall speed |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Gradually increases ice accretion; stall speed rises by 1 kt/min; if >15 kts above normal stall → Level escalates |
| `Resolve(ctx)` | aircraft | `bool` | Calls `ElectricalSystem.ActivateDeIcing()`. Returns `true` if electrical health > 0.4 |

---

#### `RunwayIncursionAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Only in `LandingState` when altitude < 500ft. Vehicle on runway. ATC warning |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Counts down to collision; if no go-around within 15s → forced collision |
| `Resolve(ctx)` | aircraft | `bool` | Player must press go-around key → calls `LandingState.Abort()` |

---

#### `MicroburstAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Only during approach/landing. Sudden headwind of +40kts followed by tailwind -40kts within 10 seconds |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Applies wind shear vector; causes rapid altitude loss (−1000 ft/min extra); if pilot doesn't apply full thrust within 5s → crash |
| `Resolve(ctx)` | aircraft | `bool` | Full throttle + pitch up → survives. Pilot must act within window |

---

### 5.4 `IWeatherStrategy.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `WeatherName` (property) | — | `string` | Display name |
| `Description` (property) | — | `string` | Short description |
| `IsHazardous` (property) | — | `bool` | Whether it can trigger anomalies |
| `Apply(FlightData data, double deltaT)` | current telemetry, elapsed seconds | `void` | Modifies wind, temperature, pressure in `FlightData` |
| `GetTurbulenceLevel()` | — | `double` | 0.0 (calm) to 1.0 (extreme) |
| `GetVisibilityMeters()` | — | `double` | Horizontal visibility in meters |
| `GetWindData()` | — | `WindData` | Returns `(double SpeedKts, double DirectionDeg, double GustKts)` |
| `GetCompatibleAnomalies()` | — | `IReadOnlyList<AnomalyType>` | Anomalies this weather can spawn |

#### Weather Strategy Classes

| Class | `WeatherName` | Key behavior in `Apply()` |
|---|---|---|
| `ClearSkiesStrategy` | "CLEAR" | Wind < 10kts, visibility > 10km, temp standard ISA |
| `ThunderstormStrategy` | "THUNDERSTORM" | Wind 30–60kts gusting, turbulence 0.8–1.0, visibility 200–500m, spawns TurbulenceAnomaly |
| `FogStrategy` | "FOG" | Visibility < 200m, no wind, forces ILS Cat III approach |
| `CrosswindStrategy` | "CROSSWIND" | Wind 90° offset from runway, 20–40kts, causes drift on approach |
| `IcingConditionsStrategy` | "ICING" | Temp < -5°C, spawns IcingAnomaly above FL180 |
| `WindShearStrategy` | "WIND SHEAR" | Rapid wind direction/speed change on approach, spawns MicroburstAnomaly |

---

## 6. Module: Core/Events – Pattern: Observer

---

### 6.1 `FlightEvent.cs` (abstract base)

```csharp
public abstract class FlightEvent
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string   Source    { get; init; }    // e.g. "EngineSystem", "CruiseState"
    public Severity Level     { get; init; }
    public string   Message   { get; init; }
}
```

#### Concrete Event Classes

| Class | Extra Fields | Triggered When |
|---|---|---|
| `AltitudeChangedEvent` | `double NewAltitude, double OldAltitude` | Altitude changes by > 100ft |
| `StateChangedEvent` | `string OldState, string NewState` | `Aircraft.TransitionTo()` called |
| `AnomalyTriggeredEvent` | `IAnomaly Anomaly` | `IAnomaly.Trigger()` called |
| `AnomalyResolvedEvent` | `IAnomaly Anomaly, bool Success` | `IAnomaly.Resolve()` called |
| `SystemFailureEvent` | `SystemType System, double Health` | System health drops below 0.3 |
| `FuelLowEvent` | `double RemainingPercent` | Fuel drops below 15% |
| `FuelCriticalEvent` | `double RemainingPercent` | Fuel drops below 5% |
| `WeatherChangedEvent` | `IWeatherStrategy NewWeather` | Weather strategy changes |
| `LandingCompletedEvent` | `double TouchdownSpeedKts, bool Successful` | Wheels touch runway |
| `MaydayEvent` | `string Reason, EmergencyType Type` | Emergency declared |
| `CommandExecutedEvent` | `string CommandName, string Details` | Any `IFlightCommand.Execute()` |
| `PlayerInputEvent` | `PlayerAction Action, ConsoleKey Key` | Player presses a key |
| `TelemetryTickEvent` | `FlightDataSnapshot Snapshot` | Every 1 second of sim time |

---

### 6.2 `EventBus.cs` (Singleton)

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Instance` (static property) | — | `EventBus` | Lazy singleton accessor |
| `Subscribe<T>(IFlightEventHandler handler)` | `T` – event type; `handler` – listener | `void` | Registers handler for event type `T` |
| `Unsubscribe<T>(IFlightEventHandler handler)` | same | `void` | Removes handler for event type `T` |
| `Publish<T>(T evt)` | `evt` – event instance | `void` | Dispatches event to all registered handlers for type `T` |
| `History` (property) | — | `IReadOnlyList<FlightEvent>` | Full ordered log of all published events |
| `ClearHistory()` | — | `void` | Clears the event history (called on new flight) |

---

### 6.3 `IFlightEventHandler.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `HandledEventTypes` (property) | — | `IEnumerable<Type>` | Types this handler responds to |
| `Handle(FlightEvent evt)` | `evt` – incoming event | `void` | Process the event |

---

### 6.4 Handler Classes

#### `BlackBoxHandler.cs`

Records **every** event and every `TelemetryTickEvent` (every second) to an in-memory list. On flight end, serializes to file.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | `evt` | `void` | Appends to internal `List<FlightEvent>` |
| `SaveToFile(string path)` | `path` – file path | `void` | Writes all events as JSON or structured text |
| `GetFullHistory()` | — | `IReadOnlyList<FlightEvent>` | Returns complete event list |

#### `AlertSystemHandler.cs`

Maintains a queue of active alert messages for display in the alerts bar.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | `evt` | `void` | Adds formatted alert to `_alertQueue` |
| `GetActiveAlerts()` | — | `IReadOnlyList<string>` | Returns alerts to display (max 3, oldest first) |
| `DismissAlert(int index)` | `index` – alert index | `void` | Removes specific alert |

#### `FlightLoggerHandler.cs`

Writes human-readable log lines to a `.log` file.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | `evt` | `void` | Formats and appends line to log file |
| `SetLogPath(string path)` | `path` | `void` | Sets output file path |

#### `StatisticsHandler.cs`

Collects flight statistics for the post-flight report.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | `evt` | `void` | Updates counters (anomaly count, max altitude, etc.) |
| `GetStatistics()` | — | `FlightStatistics` | Returns aggregated stats record |

---

## 7. Module: Core/Commands – Pattern: Command

---

### 7.1 `IFlightCommand.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `CommandName` (property) | — | `string` | Human-readable name ("Set Throttle 80%") |
| `ExecutedAt` (property) | — | `DateTime` | Timestamp of execution |
| `CanUndo` (property) | — | `bool` | Whether this command can be reversed |
| `Execute()` | — | `void` | Applies the command to the aircraft |
| `Undo()` | — | `void` | Reverses the command (if `CanUndo = true`) |
| `GetDescription()` | — | `string` | One-line description for black box log |

---

### 7.2 Command Implementations

| Class | Constructor Params | Execute | Undo | CanUndo |
|---|---|---|---|---|
| `SetThrottleCommand` | `Aircraft aircraft, double newThrottle` | Sets `FlightData.Throttle = newThrottle` | Restores previous throttle value | `true` |
| `SetHeadingCommand` | `Aircraft aircraft, double newHeading` | Sets `FlightData.Heading = newHeading` | Restores previous heading | `true` |
| `SetAltitudeCommand` | `Aircraft aircraft, double newAltitudeFt` | Sets autopilot target altitude | Restores previous target | `true` |
| `ToggleAutopilotCommand` | `Aircraft aircraft` | Calls `AutopilotSystem.Engage()` or `Disengage()` | Reverses autopilot state | `true` |
| `ActivateSystemCommand` | `Aircraft aircraft, SystemType system` | Calls appropriate system activation | Deactivates system | `true` |
| `ResolveAnomalyCommand` | `Aircraft aircraft, IAnomaly anomaly` | Calls `anomaly.Resolve(aircraft)` | `CanUndo = false` (cannot un-resolve) | `false` |
| `EmergencyDeclareCommand` | `Aircraft aircraft` | Calls `aircraft.DeclareEmergency()` | `CanUndo = false` | `false` |
| `GoAroundCommand` | `Aircraft aircraft` | Calls `aircraft.Abort()` from landing state | `CanUndo = false` | `false` |

---

### 7.3 `CommandHistory.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Execute(IFlightCommand cmd)` | `cmd` – command to run | `void` | Calls `cmd.Execute()`, pushes to undo stack, clears redo stack, publishes `CommandExecutedEvent` |
| `Undo()` | — | `bool` | Pops from undo stack, calls `Undo()`, pushes to redo stack. Returns `false` if stack empty or `CanUndo = false` |
| `Redo()` | — | `bool` | Pops from redo stack, re-executes. Returns `false` if stack empty |
| `GetAll()` | — | `IReadOnlyList<IFlightCommand>` | All commands ever executed (for black box) |
| `GetUndoable()` | — | `IReadOnlyList<IFlightCommand>` | Current undo stack |
| `SaveToFile(string path)` | `path` – output file | `void` | Exports full command history as text (black box export) |
| `Clear()` | — | `void` | Resets all stacks (new flight) |

---

## 8. Module: Controllers – MVC Controller

---

### 8.1 `FlightController.cs`

Orchestrates the entire simulation loop. Sits between Model and View.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `FlightController(Aircraft aircraft, IFlightView view, SimulationConfig config)` | constructor params | — | Wires up all components |
| `RunAsync(CancellationToken ct)` | `ct` – cancellation token | `Task` | Main async game loop at 10 Hz. Each iteration: update model → tick anomalies → render view → read input → execute command |
| `ExecuteCommand(IFlightCommand cmd)` | `cmd` – command to execute | `void` | Passes command to `CommandHistory.Execute()` |
| `SetThrottle(double value)` | `value` – 0.0–1.0 | `void` | Creates and executes `SetThrottleCommand` |
| `AdjustThrottle(double delta)` | `delta` – change amount | `void` | Adjusts throttle by delta, clamps 0.0–1.0 |
| `SetHeading(double degrees)` | `degrees` – 0–360 | `void` | Creates and executes `SetHeadingCommand` |
| `AdjustHeading(double delta)` | `delta` – degrees to turn | `void` | Adjusts heading by delta |
| `SetTargetAltitude(double feet)` | `feet` – target alt | `void` | Creates and executes `SetAltitudeCommand` |
| `ToggleAutopilot()` | — | `void` | Creates and executes `ToggleAutopilotCommand` |
| `ExecuteTakeOff()` | — | `void` | Wraps `aircraft.TakeOff()` in a command |
| `ExecuteLand()` | — | `void` | Wraps `aircraft.Land()` |
| `ExecuteEmergency()` | — | `void` | Creates `EmergencyDeclareCommand` |
| `ExecuteGoAround()` | — | `void` | Creates `GoAroundCommand` |
| `ResolveTopAnomaly()` | — | `void` | Gets `AnomalyEngine.MostCritical`, creates `ResolveAnomalyCommand` |
| `UndoLastCommand()` | — | `void` | Calls `CommandHistory.Undo()` |
| `GetFlightReport()` | — | `FlightReport` | Builds and returns the post-flight report |

---

### 8.2 `InputHandler.cs`

Reads keyboard input without blocking the render loop.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Poll()` | — | `PlayerAction?` | Non-blocking. Returns `null` if no key pressed. Reads `Console.KeyAvailable` |
| `SetKeyMap(Dictionary<ConsoleKey, PlayerAction> map)` | `map` – key to action mapping | `void` | Replaces default key bindings |
| `GetKeyMap()` | — | `IReadOnlyDictionary<ConsoleKey, PlayerAction>` | Returns current bindings |
| `IsKeyDown(ConsoleKey key)` | `key` – key to check | `bool` | Returns whether key is currently held (for analog controls) |

**Default key map:**

| Key | PlayerAction |
|---|---|
| `W` | `IncreaseThrottle` |
| `S` | `DecreaseThrottle` |
| `A` | `TurnLeft` |
| `D` | `TurnRight` |
| `Space` | `NextPhase` (TakeOff / Descend / Land depending on state) |
| `Q` | `ToggleAutopilot` |
| `E` | `DeclareEmergency` |
| `R` | `ResolveAnomaly` |
| `U` | `UndoLastCommand` |
| `F` | `FlapsUp` |
| `G` | `FlapsDown` |
| `L` | `DeployGear` |
| `H` | `GoAround` |
| `Tab` | `ViewReport` |
| `Escape` | `Quit` |

---

### 8.3 `AnomalyEngine.cs`

Manages the pool of possible anomalies and randomly triggers them.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `AnomalyEngine(Aircraft aircraft, SimulationConfig config)` | constructor params | — | Initializes anomaly pool from `AnomalyFactory` |
| `Tick(double deltaT)` | `deltaT` – seconds | `void` | Called each frame. Calls `TrySpawnAnomaly()` and `UpdateActiveAnomalies()` |
| `TrySpawnAnomaly(double deltaT)` | `deltaT` – seconds | `void` | For each pooled anomaly, rolls probability vs spawn chance. Spawns at most 1 per tick. Probability weighted by state, weather, and elapsed flight time |
| `UpdateActiveAnomalies(double deltaT)` | `deltaT` – seconds | `void` | Calls `Update()` on each active anomaly |
| `ResolveAnomaly(string anomalyName)` | `anomalyName` – name match | `bool` | Finds active anomaly by name, calls `Resolve()` |
| `AddToPool(IAnomaly anomaly)` | `anomaly` – to add | `void` | Adds custom anomaly to pool |
| `ActiveAnomalies` (property) | — | `IReadOnlyList<IAnomaly>` | Currently active anomalies |
| `MostCritical` (property) | — | `IAnomaly?` | Active anomaly with highest `Severity` |
| `SetDifficulty(Difficulty d)` | `d` – Easy/Normal/Hard | `void` | Adjusts global probability multiplier |

**Spawn weighting logic (inside `TrySpawnAnomaly`):**
- Base probability from `IAnomaly.Probability`
- Multiplied by `config.AnomalyFrequency`
- +50% during `TakeOffState` and `LandingState`
- +20% per hour of flight elapsed
- +30% if weather `IsHazardous`
- Max 1 new anomaly per 30 seconds

---

## 9. Module: Views – MVC View

---

### 9.1 `IFlightView.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Render(Aircraft aircraft)` | `aircraft` – current state | `void` | Full redraw of the dashboard |
| `ShowAlert(string message, AlertLevel level)` | message + severity | `void` | Pushes an alert to the alerts bar |
| `ShowMenu(IReadOnlyList<string> options)` | `options` – action list | `void` | Renders action menu |
| `ShowGameOver(string reason)` | `reason` – crash reason | `void` | Shows crash screen |
| `ShowFlightReport(FlightReport report)` | `report` – end of flight data | `void` | Renders post-flight summary |
| `Clear()` | — | `void` | Clears the console |

---

### 9.2 `ConsoleDashboardView.cs`

**IMPORTANT:** Never modify model data. Read all data through `Aircraft` public properties.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Render(Aircraft aircraft)` | aircraft | `void` | Calls all widget render methods in order, uses `Console.SetCursorPosition()` for in-place redraw (no flicker) |
| `RenderHeader(Aircraft a)` | aircraft | `void` | Top bar: tail number, model, flight time, state name (colored by `StateColor`) |
| `RenderPFD(FlightData fd)` | telemetry | `void` | Primary Flight Display: altitude, speed, heading, vertical speed, G-force, pitch, roll |
| `RenderFuelGauge(FuelSystem fuel)` | fuel system | `void` | ASCII progress bar `[████████░░░░]` with % and kg |
| `RenderSystemsPanel(Aircraft a)` | aircraft | `void` | Grid of all systems with health indicator: `[OK]` green / `[WARN]` yellow / `[FAIL]` red |
| `RenderWeatherPanel(WeatherSystem ws)` | weather system | `void` | Weather name, wind speed/direction, turbulence level, visibility |
| `RenderAlertsBar(IReadOnlyList<string> alerts)` | active alert strings | `void` | Up to 3 scrolling alert lines with blinking `!!` for Critical |
| `RenderActionMenu(IAircraftState state)` | current state | `void` | Shows `AllowedActions` as `[KEY] Action` pairs |
| `RenderFlightMap(FlightData fd)` | telemetry | `void` | 20×10 ASCII grid showing aircraft position symbol (`✈`) and waypoint |
| `RenderAttitudeIndicator(double pitch, double roll)` | pitch/roll degrees | `void` | ASCII artificial horizon with `---` horizon line and `✈` symbol |

**Rendering strategy:**
- Use `Console.SetCursorPosition(0, 0)` at start of each `Render()` call to redraw in-place
- Use `Console.ForegroundColor` / `Console.BackgroundColor` for colors
- Use `Console.CursorVisible = false` at startup
- Render to a `string[]` buffer first, then flush all at once to minimize flicker

---

### 9.3 View Widget Classes (in `Views/Components/`)

Each widget renders one section of the dashboard as a string array.

| Class | Method | Returns | Description |
|---|---|---|---|
| `AltimeterWidget` | `Render(double altitude, double verticalSpeed)` | `string[]` | Vertical tape with current altitude and trend arrow |
| `AirspeedWidget` | `Render(double ias, double tas, double vmo)` | `string[]` | Speed tape with color zones (green/yellow/red) |
| `FuelGaugeWidget` | `Render(double pct, double kg, double rangeKm)` | `string[]` | Bar + numbers + range estimate |
| `SystemsPanelWidget` | `Render(IReadOnlyList<IAvionicSystem> systems)` | `string[]` | 2-column system status grid |
| `AlertsBarWidget` | `Render(IReadOnlyList<string> alerts)` | `string[]` | Alert list with severity colors |
| `ActionMenuWidget` | `Render(IReadOnlyList<string> actions, Dictionary<string,ConsoleKey> keyMap)` | `string[]` | Key–action pairs grid |

---

### 9.4 `StartupScreen.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Show()` | — | `StartupSelection` | Renders ASCII logo, aircraft selector, difficulty picker. Blocks until player confirms |
| `SelectAircraft()` | — | `AircraftConfig` | Shows numbered list of aircraft (Boeing 737, A320, Cessna). Returns selected config |
| `SelectDifficulty()` | — | `Difficulty` | Easy / Normal / Hard selection |

---

## 10. Module: Infrastructure

---

### 10.1 `SimulationConfig.cs` (Singleton)

| Method / Property | Params | Returns | Description |
|---|---|---|---|
| `Instance` (static) | — | `SimulationConfig` | Lazy singleton |
| `SimulationSpeed` | — | `double` | Time multiplier (1.0 = real-time, 5.0 = 5× faster) |
| `AnomaliesEnabled` | — | `bool` | Master toggle for anomaly spawning |
| `AnomalyFrequency` | — | `double` | Probability multiplier (1.0 = normal, 2.0 = double) |
| `BlackBoxEnabled` | — | `bool` | Whether to write black box files |
| `LogDirectory` | — | `string` | Output path for logs and reports |
| `LoadFromFile(string path)` | `path` – JSON config file | `void` | Deserializes config from JSON |
| `SaveToFile(string path)` | `path` – output path | `void` | Serializes current config to JSON |

---

### 10.2 `FlightLogger.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `FlightLogger(string logDirectory)` | `logDirectory` – path | — | Creates log file named `flight_YYYYMMDD_HHmmss.log` |
| `Log(LogLevel level, string message, string source)` | level, message, source | `void` | Appends formatted line: `[HH:mm:ss][LEVEL][source] message` |
| `LogEvent(FlightEvent evt)` | `evt` | `void` | Formats and logs any `FlightEvent` |
| `LogTelemetry(FlightData data)` | `data` | `void` | Writes one CSV telemetry row (called every second) |
| `SaveFlightReport(FlightReport report)` | `report` | `void` | Writes formatted text report to file |
| `Flush()` | — | `void` | Forces write of buffered output |
| `Close()` | — | `void` | Closes log file handle |

---

### 10.3 `FlightReport.cs`

Data class (record) representing the completed flight summary.

```csharp
public record FlightReport
{
    public string    TailNumber        { get; init; }
    public string    Model             { get; init; }
    public DateTime  DepartureTime     { get; init; }
    public TimeSpan  FlightDuration    { get; init; }
    public double    DistanceKm        { get; init; }
    public double    FuelUsedKg        { get; init; }
    public double    MaxAltitudeFt     { get; init; }
    public double    MaxSpeedKts       { get; init; }
    public int       AnomaliesTotal    { get; init; }
    public int       AnomaliesResolved { get; init; }
    public bool      LandedSafely      { get; init; }
    public double    TouchdownSpeedKts { get; init; }
    public double    LandingScore      { get; init; }    // 0–100
    public IReadOnlyList<FlightEvent>      EventLog  { get; init; }
    public IReadOnlyList<IFlightCommand>   CommandLog { get; init; }

    public void PrintToConsole();
    public void SaveAsText(string path);
}
```

**Landing score formula:**  
`score = 100 - (speedPenalty) - (verticalSpeedPenalty) - (anomalyPenalty) - (crosswindPenalty)`

---

### 10.4 `AircraftFactory.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `CreateBoeing737()` | — | `Aircraft` | Boeing 737-800 with full config |
| `CreateAirbusA320()` | — | `Aircraft` | Airbus A320 with full config |
| `CreateCessna172()` | — | `Aircraft` | Small GA aircraft, lower speeds and altitude |
| `Create(AircraftConfig config, string tail, string model)` | full params | `Aircraft` | Generic factory for custom aircraft |

**Boeing 737-800 config:**
```
MaxFuelKg=26000, MaxAltitudeFt=41000, CruiseSpeedKts=460, MaxSpeedKts=544,
StallSpeedKts=130, StallSpeedFlaps=105, EngineCount=2, MaxThrustKN=242.8,
V1=148kts, VR=152kts, V2=158kts
```

---

### 10.5 `AnomalyFactory.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Create(AnomalyType type)` | `type` – enum | `IAnomaly` | Returns new instance of requested anomaly |
| `CreateRandom(FlightData ctx)` | `ctx` – current telemetry | `IAnomaly` | Weighted random selection based on altitude, speed, state |
| `GetAllFor(IAircraftState state)` | `state` – current state | `IReadOnlyList<IAnomaly>` | Returns anomalies valid for given state |
| `GetPool()` | — | `IReadOnlyList<IAnomaly>` | Returns default full anomaly pool |

---

## 11. Gameplay Loop & Input Map

### Complete Game Flow

```
Program.cs
  ↓
StartupScreen.Show()           → player picks aircraft + difficulty
  ↓
AircraftFactory.Create()       → creates Aircraft
  ↓
FlightController.RunAsync()    → 10 Hz game loop
  ├── aircraft.Update(deltaT)
  ├── anomalyEngine.Tick(deltaT)
  ├── view.Render(aircraft)
  ├── input = inputHandler.Poll()
  └── controller.ExecuteCommand(...)
  ↓
[flight ends: LandingCompletedEvent or CriticalState crash]
  ↓
FlightReport generated + shown
  ↓
BlackBox saved to file
```

### State Transition Diagram

```
                    +----------+
             -----> | GROUND   | <--------------------------+
             |      +----+-----+                           |
             |           | TakeOff() OK                    |
             |           v                                 |
             |      +----+------+                          |
             |      |  TAXI    |                           |
             |      +----+------+                          |
             |           | at runway                       |
             |           v                                 |
             |      +----+-------+  Abort(<V1)             |
             |      | TAKE OFF   | ------> [brake] --------+
             |      +----+-------+                         
             |           | 1500ft + V2                     
             |           v                                 
             |      +----+----+                           
             |      |  CLIMB  |                           
             |      +----+----+                           
             |           | target alt                      
             |           v                                 
             |      +----+-----+                          
             |      | CRUISE   | <---+                    
             |      +----+-----+     |                    
             |           |Descend()  | (holding cleared)  
             |           v           |                    
             |      +----+------+    +-- +----------+     
             |      | DESCENT  |        | HOLDING  |     
             |      +----+------+        +----------+     
             |           | <3000ft                        
             |           v                                 
             |      +----+------+  Abort()                
             +------| LANDING  | -------> [go-around] --> CLIMB
                    +----+------+                          
                         | touchdown + full stop           
                         v                                 
                      [GROUND] (loop complete)             

 ANY STATE --[HandleEmergency()]--> EMERGENCY ---> CRITICAL (if escalated)
```

---

## 12. Black Box & Flight Logging

The black box records everything automatically via `BlackBoxHandler` (Observer).

### What gets recorded

| Event Type | Details stored |
|---|---|
| Every `TelemetryTickEvent` (1/sec) | Altitude, speed, heading, throttle, RPM, fuel, G-force |
| Every state transition | Old state, new state, timestamp |
| Every anomaly trigger | Anomaly name, severity, aircraft params at moment of trigger |
| Every anomaly resolve attempt | Success/fail, aircraft params |
| Every player input | Key pressed, resulting action, timestamp |
| Every command executed | Command name, parameter values, pre/post state snapshot |
| Every system failure | System name, health at failure |
| Landing | Touch-down speed, vertical speed, runway offset |

### Output files (saved to `LogDirectory`)

| File | Format | Content |
|---|---|---|
| `flight_YYYYMMDD_HHmmss.log` | Plain text | Human-readable event log |
| `blackbox_YYYYMMDD_HHmmss.txt` | Structured text | Full raw event dump |
| `telemetry_YYYYMMDD_HHmmss.csv` | CSV | One row per second: all `FlightData` fields |
| `report_YYYYMMDD_HHmmss.txt` | Plain text | Formatted flight summary |

---

## 13. Implementation Roadmap

> Follow this order strictly. Each stage builds on the previous one.

### Stage 1 – Foundation (~500 LOC)
- [ ] Create directory structure
- [ ] Implement all enums (`SystemType`, `SystemStatus`, `Severity`, `PlayerAction`, `AnomalyType`)
- [ ] Implement `AircraftConfig` (record)
- [ ] Implement `FlightData` with all properties and methods
- [ ] Implement `FlightDataSnapshot` (record)
- [ ] Implement `IAvionicSystem` interface
- [ ] Stub all system classes (empty `Update`, `ApplyDamage`, `Repair`)
- [ ] Write unit tests for `FlightData` calculated properties

### Stage 2 – State Pattern (~1,500 LOC)
- [ ] Implement `IAircraftState`
- [ ] Implement `Aircraft` class with `TransitionTo()` and all delegate methods
- [ ] Implement `GroundState` (full)
- [ ] Implement `TaxiState` (full)
- [ ] Implement `TakeOffState` with V1/VR/V2 logic
- [ ] Implement `ClimbState`
- [ ] Implement `CruiseState` (without anomalies yet)
- [ ] Implement `DescentState`
- [ ] Implement `LandingState` with ILS tracking
- [ ] Implement `HoldingState`
- [ ] Implement `EmergencyState` and `CriticalState`
- [ ] Test full flight: Ground → Taxi → TakeOff → Climb → Cruise → Descent → Landing → Ground

### Stage 3 – Observer Pattern (~600 LOC)
- [ ] Implement all `FlightEvent` subclasses
- [ ] Implement `EventBus` (Singleton, thread-safe with lock)
- [ ] Implement `IFlightEventHandler`
- [ ] Implement `BlackBoxHandler`
- [ ] Implement `AlertSystemHandler`
- [ ] Implement `FlightLoggerHandler`
- [ ] Implement `StatisticsHandler`
- [ ] Wire all events into state `OnEnter()`, `OnExit()`, `Update()`

### Stage 4 – Strategy Pattern: Anomalies (~2,000 LOC)
- [ ] Implement `IAnomaly` and `AbstractAnomaly`
- [ ] Implement all 10 anomaly classes (one at a time, test each)
- [ ] Implement `IWeatherStrategy`
- [ ] Implement all 6 weather strategy classes
- [ ] Implement `AnomalyFactory` and `WeatherFactory`
- [ ] Implement `AnomalyEngine` with full spawn logic
- [ ] Integrate `AnomalyEngine.Tick()` into `CruiseState.Update()`

### Stage 5 – Command Pattern (~700 LOC)
- [ ] Implement `IFlightCommand`
- [ ] Implement all 8 command classes
- [ ] Implement `CommandHistory` with undo/redo stacks
- [ ] Add `CommandHistory.SaveToFile()` (black box export)

### Stage 6 – Avionics Systems – Full Implementation (~800 LOC)
- [ ] Fully implement `EngineSystem` (RPM curves, temperature, restart logic)
- [ ] Fully implement `FuelSystem` (burn rates, leak, emergency dump)
- [ ] Fully implement `NavigationSystem` (waypoints, autopilot coupling)
- [ ] Fully implement `HydraulicSystem` (gear, flaps, emergency extension)
- [ ] Fully implement `ElectricalSystem` (buses, backup battery, APU)
- [ ] Fully implement `AutopilotSystem` (all 5 modes)

### Stage 7 – View Layer (~1,500 LOC)
- [ ] Implement `IFlightView`
- [ ] Implement all 6 widget classes in `Views/Components/`
- [ ] Implement `ConsoleDashboardView` with full in-place redraw
- [ ] Implement `StartupScreen` with aircraft + difficulty selection
- [ ] Implement `FlightReportView`

### Stage 8 – Controller + Input (~800 LOC)
- [ ] Implement `InputHandler` with non-blocking `Poll()`
- [ ] Implement `FlightController` with 10 Hz `RunAsync()` loop
- [ ] Map all `PlayerAction` values to `FlightController` methods
- [ ] Implement `AircraftFactory` (all 3 aircraft configs)
- [ ] Implement `SimulationConfig` (Singleton with JSON load/save)

### Stage 9 – Infrastructure + Polish (~900 LOC)
- [ ] Implement `FlightLogger` with file output
- [ ] Implement `FlightReport` with `SaveAsText()` and `PrintToConsole()`
- [ ] Add landing score calculation
- [ ] Add color coding throughout dashboard (state-dependent colors)
- [ ] Add blinking `!!` for Critical alerts
- [ ] Add demo mode (auto-pilot all phases with no input needed)
- [ ] Write XML doc comments on all public APIs
- [ ] Write at least 25 unit tests
- [ ] End-to-end test: full flight with every anomaly triggered and resolved

---

## 14. Team Task Split

| Area | Stages | Person |
|---|---|---|
| `FlightData`, `AircraftConfig`, all enums, `IAvionicSystem` | 1 | Person 1 |
| State pattern: all 10 state classes + `Aircraft.TransitionTo()` | 2 | Person 1 + Person 2 |
| Observer pattern: `EventBus`, all events, all handlers | 3 | Person 2 |
| Strategy: 10 anomaly classes + `AbstractAnomaly` + `AnomalyFactory` | 4a | Person 3 |
| Strategy: 6 weather strategies + `AnomalyEngine` | 4b | Person 2 |
| Command pattern: all commands + `CommandHistory` | 5 | Person 4 |
| Avionics systems full implementation | 6 | Person 1 + Person 3 |
| View layer: all widgets + dashboard + startup screen | 7 | Person 3 + Person 4 |
| Controller + InputHandler + main game loop | 8 | Person 1 |
| Factory classes + SimulationConfig + FlightLogger + FlightReport | 8–9 | Person 4 |
| Unit tests, integration tests, XML docs, polish | 9 | All |

---

## 15. LOC Estimate per Module

| Module | Est. LOC |
|---|---|
| Core/Aircraft (Aircraft + FlightData + config) | 600 |
| Core/Aircraft/Systems (7 system classes) | 900 |
| Core/States (10 state classes) | 1,500 |
| Core/Strategies/Anomalies (10 anomalies) | 1,200 |
| Core/Strategies/Weather (6 strategies) | 600 |
| Core/Events (EventBus + events + 4 handlers) | 600 |
| Core/Commands (8 commands + CommandHistory) | 700 |
| Controllers (FlightController + InputHandler + AnomalyEngine) | 900 |
| Views (dashboard + widgets + screens) | 1,500 |
| Infrastructure (config + logger + report + factories) | 800 |
| Unit tests | 900 |
| Program.cs + startup glue | 200 |
| **TOTAL** | **~10,400 LOC** |

---

## 16. Grading Checklist

### Required (must have for passing)

- [ ] **State pattern** – minimum 6 states with correct transitions, no if/switch in Aircraft
- [ ] **Strategy pattern** – minimum 6 anomalies + 3 weather strategies, all interchangeable
- [ ] **MVC** – Model does not import View. Controller mediates all interactions
- [ ] **Observer** – EventBus with minimum 3 handlers, events published from model
- [ ] **Command** – CommandHistory with undo for minimum 5 distinct command types
- [ ] **Factory** – AircraftFactory + AnomalyFactory
- [ ] **Singleton** – SimulationConfig or EventBus with lazy init
- [ ] **Polymorphism** – visible in `Update()`, `Trigger()`, `Handle()` calls
- [ ] **Encapsulation** – no public fields anywhere, only properties with controlled access
- [ ] **Inheritance** – minimum 2 clear hierarchies (e.g. `AbstractAnomaly`, `FlightEvent`)

### Bonus (for grade 5.0)

- [ ] Unit test suite (25+ tests using xUnit)
- [ ] JSON config file for `SimulationConfig`
- [ ] Black box export to file after each flight
- [ ] Landing score calculation with formula
- [ ] Demo / auto-pilot mode (no keyboard needed)
- [ ] XML doc comments (`/// <summary>`) on all public APIs
- [ ] Flight telemetry CSV export
- [ ] All 10 anomalies implemented and testable
- [ ] Smooth in-place console redraw (no flicker)
