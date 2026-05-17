# AeroSim – Full Developer Specification v2

> **Course:** Paradygmaty Programowania | **Language:** C# (.NET 8)
> **Target size:** ~10,000–12,000 LOC | **Type:** Real-time console game / flight simulator
> **Design patterns used:** State, Strategy, Observer, Command, MVC, Factory, Singleton

---

## Table of Contents

1. [Project Overview & Gameplay](#1-project-overview--gameplay)
2. [Directory Structure](#2-directory-structure)
3. [Cascade Damage System](#3-cascade-damage-system)
4. [Module: Core/Aircraft](#4-module-coreaircraftcore)
5. [Module: Core/Sensors](#5-module-coresensors)
6. [Module: Core/States (Pattern: State)](#6-module-corestates--pattern-state)
7. [Module: Core/Strategies (Pattern: Strategy)](#7-module-corestrategies--pattern-strategy)
8. [Module: Core/Events (Pattern: Observer)](#8-module-coreevents--pattern-observer)
9. [Module: Core/Commands (Pattern: Command)](#9-module-corecommands--pattern-command)
10. [Module: Controllers (MVC – Controller)](#10-module-controllers--mvc-controller)
11. [Module: Views (MVC – View)](#11-module-views--mvc-view)
12. [Module: Infrastructure](#12-module-infrastructure)
13. [Gameplay Loop & Input Map](#13-gameplay-loop--input-map)
14. [Black Box & Flight Logging](#14-black-box--flight-logging)
15. [Implementation Roadmap](#15-implementation-roadmap)
16. [Team Task Split](#16-team-task-split)
17. [LOC Estimate per Module](#17-loc-estimate-per-module)
18. [Grading Checklist](#18-grading-checklist)

---

## 1. Project Overview & Gameplay

AeroSim is a **real-time console flight simulator** controlled entirely via keyboard. The player selects an aircraft, pilots it from startup through a full flight cycle, and reacts to cascading real-time failures. Systems affect each other — a bird strike can cause an engine explosion, which starts a fire, which spreads and melts the wing, which asymmetrically drags the aircraft, which eventually causes loss of control. Nothing happens in isolation.

### Gameplay summary

```
[STARTUP SCREEN]
  1. Select aircraft  → Boeing 737-800 / Airbus A320 / Cessna 172
  2. Select route     → short (30 min) / medium (1 hr) / long (2 hr)
  3. Select difficulty → Easy / Normal / Hard

[MAIN LOOP – 10 Hz]
  aircraft.Update(deltaT)       <- physics, fuel burn, cascade damage
  sensorSystem.Update(deltaT)   <- sensors read real values (may be wrong)
  anomalyEngine.Tick(deltaT)    <- random events, cascade triggers
  view.Render(aircraft)         <- full dashboard redraw
  input = inputHandler.Poll()   <- non-blocking keyboard check
  controller.Execute(input)     <- dispatch command

[EVENTS — cascade chain examples]
  Bird strike  ->  engine damage  ->  engine fire  ->  wing fire
               ->  wing melting   ->  asymmetric drag -> loss of control
               ->  black box GAME OVER

  Turbulence   ->  altitude oscillations  ->  sensor noise on altimeter
               ->  autopilot confusion    ->  wrong altitude held
               ->  CFIT (ground collision if not corrected)

  Fuel leak    ->  fuel critical          ->  both engines flame out
               ->  emergency descent      ->  forced landing

[END OF FLIGHT]
  Full flight report + black box printout
```

### Console dashboard layout

```
+====================================================================+
| AeroSim  SP-LRA  Boeing 737-800          FLT TIME: 01:14:32       |
| STATE: [CRUISE]               AUTOPILOT: ON    DIFFICULTY: NORMAL  |
+===============+===============+===============+====================+
| ALT:  35000ft | SPD:  461 kts | HDG:   087 deg| V/S:    0 ft/min  |
| THR:   78%    | RPM:   92%    | GFR:   1.0 g  | TEMP:  745 C      |
+===============+===============+===============+====================+
| FUEL  [############........]  68%   12400 kg      RANGE: ~2100 km |
+--------------------------------------------------------------------+
| SYSTEMS:                                                           |
|  ENG1 [OK  100%]  ENG2 [OK  100%]  FUEL [OK]   HYD [OK]          |
|  ELEC [OK   98%]  NAV  [OK]        AP   [ON]    WING [OK]         |
+--------------------------------------------------------------------+
|  SENSORS:                                                          |
|  ALT-SNS [OK]  SPD-SNS [OK]  ENG-SNS [FAULT - reading: 0 RPM]    |
+--------------------------------------------------------------------+
| MAP:                    WEATHER: THUNDERSTORM                      |
| ..........*.....        WIND: 270 / 45 kts   TURB: HIGH           |
| .....A..........        GUST: 62 kts         VIS: 400 m           |
| .................                                                   |
+--------------------------------------------------------------------+
| VIEW: pitch -2.0  roll +1.5                                        |
|  sky sky sky sky sky sky sky sky sky sky sky sky                  |
|  ------- horizon ------- A ------- horizon -------                 |
|  gnd gnd gnd gnd gnd gnd gnd gnd gnd gnd gnd gnd                  |
+--------------------------------------------------------------------+
| !! ALERT: ENGINE 1 BIRD STRIKE -- check RPM sensor               !!|
| !! WARNING: SENSOR FAULT on ENG-SNS1 -- readings unreliable      !!|
+--------------------------------------------------------------------+
| [W/S] Throttle  [A/D] Heading  [Q] Autopilot  [R] Resolve anomaly |
| [SPACE] Next phase  [E] Emergency  [L] Gear  [F/G] Flaps  [ESC]   |
+====================================================================+
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
│   │   ├── DamageModel.cs              <- tracks cascade state for whole aircraft
│   │   ├── Systems/
│   │   │   ├── IAvionicSystem.cs
│   │   │   ├── EngineSystem.cs
│   │   │   ├── FuelSystem.cs
│   │   │   ├── NavigationSystem.cs
│   │   │   ├── HydraulicSystem.cs
│   │   │   ├── ElectricalSystem.cs
│   │   │   ├── WeatherSystem.cs
│   │   │   ├── AutopilotSystem.cs
│   │   │   └── WingSystem.cs           <- NEW: wing integrity, fire spread
│   │   ├── Sensors/
│   │   │   ├── ISensor.cs              <- NEW
│   │   │   ├── Sensor.cs               <- NEW: base sensor with noise/fault
│   │   │   ├── SensorSystem.cs         <- NEW: holds all sensors
│   │   │   ├── AltitudeSensor.cs       <- NEW
│   │   │   ├── AirspeedSensor.cs       <- NEW
│   │   │   ├── EngineSensor.cs         <- NEW (RPM + temp per engine)
│   │   │   ├── FuelSensor.cs           <- NEW
│   │   │   └── HydraulicSensor.cs      <- NEW
│   │   └── Enums/
│   │       ├── SystemType.cs
│   │       ├── SystemStatus.cs
│   │       ├── Severity.cs
│   │       ├── FireState.cs            <- NEW: None / Burning / Spreading / Melting
│   │       └── SensorState.cs          <- NEW: OK / Noisy / Fault / Dead
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
│   │   │   ├── BirdStrikeAnomaly.cs    <- UPDATED: triggers engine explosion chain
│   │   │   ├── EngineFireAnomaly.cs    <- NEW: fire that spreads
│   │   │   ├── WingFireAnomaly.cs      <- NEW: wing melts, asymmetric drag
│   │   │   ├── HydraulicFailureAnomaly.cs
│   │   │   ├── FuelLeakAnomaly.cs
│   │   │   ├── ElectricalFailureAnomaly.cs
│   │   │   ├── DecompressionAnomaly.cs
│   │   │   ├── TurbulenceAnomaly.cs    <- UPDATED: causes sensor noise
│   │   │   ├── IcingAnomaly.cs
│   │   │   ├── RunwayIncursionAnomaly.cs
│   │   │   ├── MicroburstAnomaly.cs
│   │   │   └── SensorFailureAnomaly.cs <- NEW
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
│   │       ├── CascadeHandler.cs       <- NEW: listens for events, triggers cascades
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
│       ├── EmergencyDeclareCommand.cs
│       └── GoAroundCommand.cs
├── Controllers/
│   ├── FlightController.cs
│   ├── InputHandler.cs
│   └── AnomalyEngine.cs
├── Views/
│   ├── IFlightView.cs
│   ├── ConsoleDashboardView.cs
│   ├── StartupScreen.cs              <- UPDATED: aircraft select + route + difficulty
│   ├── FlightReportView.cs
│   ├── BlackBoxReadoutView.cs        <- NEW: prints black box after game over
│   └── Components/
│       ├── AltimeterWidget.cs
│       ├── AirspeedWidget.cs
│       ├── FuelGaugeWidget.cs
│       ├── SystemsPanelWidget.cs
│       ├── SensorsPanelWidget.cs     <- NEW
│       ├── AlertsBarWidget.cs
│       ├── ActionMenuWidget.cs
│       ├── FlightMapWidget.cs        <- NEW: ASCII grid with plane + birds/weather
│       └── CockpitWindowWidget.cs    <- NEW: ASCII horizon / sky / ground view
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

## 3. Cascade Damage System

This is the core design decision that makes the simulation interesting. **Systems affect each other.** Every major failure can trigger secondary and tertiary consequences.

### 3.1 Cascade rules (implemented in `CascadeHandler.cs`)

`CascadeHandler` subscribes to all `SystemFailureEvent` and `AnomalyTriggeredEvent` on the EventBus. When it receives one, it checks the following table and may trigger further events or anomalies.

| Trigger event | Condition | Cascade effect |
|---|---|---|
| `BirdStrikeAnomaly` triggered | always | Engine health drops -30%. Roll dice: 40% chance → `EngineFireAnomaly` triggered |
| `EngineFireAnomaly` triggered | always | Engine health decays -2%/sec. Roll dice every 10s: 30% chance fire spreads → `WingFireAnomaly` |
| `WingFireAnomaly` triggered | always | `WingSystem` starts melting. At 50% wing health → `ElectricalSystem` loses power (wiring burns). At 20% wing health → `AsymmetricDragActive = true` on `FlightData`. At 0% wing health → `CriticalState` forced, black box GAME OVER |
| `EngineSystem.Health` reaches 0 | engine was on fire | Explosion: `GForce` spike +3g, `WingSystem.ApplyDamage(0.4)`, sensor for that engine → `Dead` |
| `EngineSystem.Health` reaches 0 | engine was NOT on fire | Flame-out only: RPM = 0, no explosion |
| `AsymmetricDragActive = true` | wing health < 20% | Each tick: aircraft `Heading` drifts toward damaged side by `drift = (1 - wingHealth) * 5 deg/sec`. Player must constantly counter with opposite rudder |
| `TurbulenceAnomaly` triggered | severity >= Medium | All sensors get `AddNoise(0.15)` for duration of turbulence. Autopilot may lock onto wrong altitude |
| `TurbulenceAnomaly` triggered | severity == Critical | Random sensor(s) enter `Fault` state |
| `FuelLeakAnomaly` triggered | leak rate > 150 kg/h | After 60s of unresolved leak → fuel ignition risk: 20% chance/min → `EngineFireAnomaly` |
| `HydraulicFailureAnomaly` triggered | landing gear was mid-retract | Gear jams in half position → `HydraulicSystem.GearJammed = true`. Emergency extension still available |
| `ElectricalFailureAnomaly` triggered | always | All sensors lose accuracy by -40%. `AutopilotSystem` goes offline. After 30s → secondary bus fails → `NavigationSystem` goes offline |
| `DecompressionAnomaly` triggered | altitude > 25000ft | `SensorSystem` all oxygen-related sensors → `Fault`. Player sees garbled readings |
| `SensorFailureAnomaly` triggered | sensor was altitude sensor | Autopilot reads wrong altitude → may climb into airspace limit or descend toward terrain |

### 3.2 `DamageModel.cs`

Central record of the aircraft's current damage state. Read by all systems and the view.

| Property | Type | Description |
|---|---|---|
| `Engine1FireState` | `FireState` | None / Burning / Spreading / Melting |
| `Engine2FireState` | `FireState` | same |
| `WingFireState` | `FireState` | None / Burning / Spreading / Melting |
| `WingHealth` | `double` | 0.0–1.0 |
| `AsymmetricDragActive` | `bool` | true when wing health < 20% |
| `AsymmetricDragSide` | `string` | "LEFT" or "RIGHT" |
| `DriftDegPerSec` | `double` | heading drift rate when asymmetric drag active |
| `IsExploded` | `bool` | true after engine explodes |
| `IsGameOver` | `bool` | true when wing reaches 0 or other fatal condition |
| `GameOverReason` | `string` | e.g. "Wing structural failure", "Both engines failed" |

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Update(double dt)` | `dt` – seconds | `void` | Advances fire spread, wing melt, drift calculations |
| `ApplyFire(FireLocation loc, double dt)` | location + time | `void` | Advances `FireState` for given location |
| `CheckGameOver()` | — | `bool` | Returns true and sets `GameOverReason` if fatal condition met |

---

## 4. Module: Core/Aircraft

### 4.1 `Aircraft.cs`

Central domain class. Aggregates all avionics systems, sensor system, damage model, and current state.
**Namespace:** `AeroSim.Core.Aircraft`

#### Fields (private)

| Field | Type | Description |
|---|---|---|
| `_currentState` | `IAircraftState` | Active state (State pattern) |
| `_flightData` | `FlightData` | Live telemetry data |
| `_engine1` | `EngineSystem` | Left/primary engine |
| `_engine2` | `EngineSystem` | Right engine |
| `_navigation` | `NavigationSystem` | GPS / autopilot |
| `_fuel` | `FuelSystem` | Fuel tanks and flow |
| `_hydraulics` | `HydraulicSystem` | Gear, flaps, brakes |
| `_electrical` | `ElectricalSystem` | Power buses |
| `_weather` | `WeatherSystem` | Atmospheric data |
| `_wing` | `WingSystem` | Wing integrity and fire |
| `_sensors` | `SensorSystem` | All sensor readings |
| `_damageModel` | `DamageModel` | Cascade damage state |
| `_eventBus` | `EventBus` | Observer event bus |
| `_config` | `AircraftConfig` | Static aircraft specs |

#### Properties (public)

| Property | Type | get/set | Description |
|---|---|---|---|
| `TailNumber` | `string` | get | Registration (e.g. "SP-LRA") |
| `Model` | `string` | get | Aircraft model name |
| `FlightData` | `FlightData` | get | Live telemetry |
| `CurrentState` | `IAircraftState` | get | Active state object |
| `Config` | `AircraftConfig` | get | Static aircraft config |
| `AllSystems` | `IReadOnlyList<IAvionicSystem>` | get | All avionics systems |
| `Sensors` | `SensorSystem` | get | Sensor system (for view) |
| `DamageModel` | `DamageModel` | get | Current cascade damage state |

#### Constructor

| Signature | Parameters | Description |
|---|---|---|
| `Aircraft(string tailNumber, string model, AircraftConfig config)` | registration, name, specs | Initializes all systems and sensors, sets initial state to `GroundState`, wires up `CascadeHandler` on EventBus |

#### Methods

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `TakeOff()` | — | `void` | Delegates to `_currentState.TakeOff(this)` |
| `Cruise()` | — | `void` | Delegates to `_currentState.Cruise(this)` |
| `Descend()` | — | `void` | Delegates to `_currentState.Descend(this)` |
| `Land()` | — | `void` | Delegates to `_currentState.Land(this)` |
| `DeclareEmergency()` | — | `void` | Delegates to `_currentState.HandleEmergency(this)` |
| `Abort()` | — | `void` | Delegates to `_currentState.Abort(this)` |
| `Update(double deltaT)` | `deltaT` – elapsed seconds | `void` | Ticks all systems, sensors, damage model, applies asymmetric drag if active |
| `TransitionTo(IAircraftState newState)` | `newState` – next state | `void` | Exits old state, enters new state, publishes `StateChangedEvent` |
| `ApplyDamage(SystemType system, double severity)` | system + severity 0–1 | `void` | Calls `ApplyDamage()` on the correct system |
| `GetSystemStatus(SystemType system)` | system enum | `SystemStatus` | Returns OK / Degraded / Failed |
| `GetSystemHealth(SystemType system)` | system enum | `double` | Returns health 0.0–1.0 |
| `Subscribe(IFlightEventHandler handler)` | handler | `void` | Registers on EventBus |
| `Publish(FlightEvent evt)` | event | `void` | Forwards to EventBus |

---

### 4.2 `FlightData.cs`

Mutable data bag holding all live telemetry. Sensors read these real values and may return noisy/wrong versions to the player.

#### Properties

| Property | Type | Unit | Description |
|---|---|---|---|
| `Altitude` | `double` | feet | True altitude MSL |
| `Speed` | `double` | knots IAS | True indicated airspeed |
| `VerticalSpeed` | `double` | ft/min | Rate of climb/descent |
| `Heading` | `double` | degrees 0–360 | Magnetic heading |
| `TargetHeading` | `double` | degrees | Player-set heading target |
| `TargetAltitude` | `double` | feet | Player-set altitude target |
| `TargetSpeed` | `double` | knots | Player-set speed target |
| `MapX` | `double` | units | Position on 2D map grid |
| `MapY` | `double` | units | Position on 2D map grid |
| `Throttle` | `double` | 0.0–1.0 | Throttle lever position |
| `Engine1RPM` | `double` | % | Engine 1 true RPM |
| `Engine2RPM` | `double` | % | Engine 2 true RPM |
| `Engine1TempC` | `double` | Celsius | Engine 1 true EGT |
| `Engine2TempC` | `double` | Celsius | Engine 2 true EGT |
| `FuelLevelKg` | `double` | kg | True fuel level |
| `FuelFlowKgPerH` | `double` | kg/h | Current burn rate |
| `FuelCapacityKg` | `double` | kg | Max capacity from config |
| `WindSpeedKnots` | `double` | knots | True wind speed |
| `WindDirectionDeg` | `double` | degrees | True wind direction |
| `AirPressureHPa` | `double` | hPa | Barometric pressure |
| `TemperatureC` | `double` | Celsius | Outside air temperature |
| `FlightTime` | `TimeSpan` | — | Elapsed since wheels-up |
| `GForce` | `double` | g | Current G-loading |
| `PitchAngleDeg` | `double` | degrees | Pitch attitude |
| `RollAngleDeg` | `double` | degrees | Roll attitude |
| `AsymmetricDrag` | `double` | 0.0–1.0 | How hard aircraft pulls sideways |

#### Methods

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `FuelRemainingPercent()` | — | `double` | `FuelLevelKg / FuelCapacityKg * 100` |
| `EstimatedRangeKm()` | — | `double` | Range at current burn rate |
| `IsStalling()` | — | `bool` | Speed below stall speed for current config |
| `IsOverspeed()` | — | `bool` | Speed above VMO/MMO |
| `Snapshot()` | — | `FlightDataSnapshot` | Immutable copy of all fields right now |
| `ToTelemetryString()` | — | `string` | CSV line for black box |
| `Reset()` | — | `void` | Resets to ground defaults |
| `ApplyAsymmetricDrift(double driftDeg, double dt)` | drift rate + time | `void` | Pushes `Heading` toward damaged wing side |

---

### 4.3 `AircraftConfig.cs`

```csharp
public record AircraftConfig
{
    public string DisplayName       { get; init; }
    public string TailNumber        { get; init; }
    public double MaxFuelKg         { get; init; }
    public double MaxAltitudeFt     { get; init; }
    public double CruiseSpeedKts    { get; init; }
    public double MaxSpeedKts       { get; init; }   // VMO
    public double StallSpeedKts     { get; init; }   // clean
    public double StallSpeedFlaps   { get; init; }   // flaps full
    public int    EngineCount       { get; init; }
    public double MaxThrustKN       { get; init; }
    public double MaxClimbRateFtMin { get; init; }
    public double NormalDescentFtMin{ get; init; }
    public double V1SpeedKts        { get; init; }
    public double VRSpeedKts        { get; init; }
    public double V2SpeedKts        { get; init; }
    public double MaxCrosswindKts   { get; init; }
    public double FuelBurnKgPerH    { get; init; }   // at cruise
    public double WingStrength      { get; init; }   // how fast wing melts (0.5–1.0)
}
```

---

### 4.4 `IAvionicSystem.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `Name` | — | `string` | Display name |
| `Status` | — | `SystemStatus` | OK / Degraded / Failed |
| `Health` | — | `double` | 0.0–1.0 |
| `Update(double deltaT, FlightData data)` | dt + telemetry | `void` | One simulation tick |
| `ApplyDamage(double severity)` | 0.0–1.0 | `void` | Reduces Health |
| `Repair(double amount)` | 0.0–1.0 | `void` | Restores Health, clamps to 1.0 |
| `GenerateReport()` | — | `SystemReport` | Health, status, recent errors |

---

### 4.5 `WingSystem.cs` (NEW)

Tracks wing structural integrity and fire spread.

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `Health` | — | `double` | 1.0 = intact, 0.0 = gone |
| `FireState` | — | `FireState` | None / Burning / Spreading / Melting |
| `StartFire()` | — | `void` | Sets `FireState = Burning`, starts decay timer |
| `Update(double dt, DamageModel dm)` | dt + damage model | `void` | Advances fire spread, reduces health. At Health=0.5 → sets `dm.AsymmetricDragActive=true`. At Health=0 → sets `dm.IsGameOver=true` |
| `ApplyDamage(double severity)` | 0.0–1.0 | `void` | Direct structural damage (e.g. explosion shockwave) |
| `ExtinguishFire()` | — | `bool` | Attempts fire suppression. Returns false if Health < 0.4 |
| `IsOnFire` | — | `bool` | `FireState != None` |

---

### 4.6 `EngineSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Start()` | — | `bool` | Engine start attempt. Returns false if Health < 0.2 or no fuel |
| `Stop()` | — | `void` | Cuts fuel, RPM decays to 0 |
| `Restart()` | — | `bool` | In-flight restart. Returns false if Health < 0.2 or alt > 30000ft |
| `StartFire()` | — | `void` | Sets engine on fire. Begins health decay + publishes `EngineFireEvent` |
| `ExtinguishFire()` | — | `bool` | Fire suppression. Returns false if Health < 0.3 |
| `Explode()` | — | `void` | Called when Health reaches 0 while on fire. Sets `IsExploded=true`, spikes GForce, damages wing |
| `Update(double dt, FlightData data)` | dt + telemetry | `void` | Advances RPM, temp, fire if active |
| `CalculateThrust(double throttle)` | 0.0–1.0 | `double` | Thrust in kN, scaled by Health |
| `IsOverheating` | — | `bool` | `TempC > DangerTempC` |
| `IsOnFire` | — | `bool` | Fire state flag |
| `IsExploded` | — | `bool` | Post-explosion flag |
| `ThrustKN` | — | `double` | Current thrust output |

---

### 4.7 `FuelSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Refuel(double kg)` | kg to add | `void` | Adds fuel, clamps to MaxFuelKg |
| `Burn(double kgPerH, double dt)` | burn rate + time | `void` | Reduces FuelLevelKg. Publishes `FuelLowEvent` at 15%, `FuelCriticalEvent` at 5% |
| `StartLeak(double rateKgPerH)` | leak rate | `void` | Activates leak |
| `SealLeak()` | — | `bool` | Attempts seal. False if Health < 0.3 |
| `EmergencyDump()` | — | `void` | Dumps fuel rapidly |
| `CheckIgnitionRisk()` | — | `bool` | Returns true if leak + ignition conditions met (used by CascadeHandler) |
| `LeakRate` | — | `double` | Current kg/h leak (0 if none) |
| `IsLeaking` | — | `bool` | Active leak flag |

---

## 5. Module: Core/Sensors

### 5.1 `ISensor.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `SensorName` | — | `string` | e.g. "ALT-SNS", "ENG1-RPM" |
| `State` | — | `SensorState` | OK / Noisy / Fault / Dead |
| `Accuracy` | — | `double` | 1.0 = perfect, 0.0 = dead |
| `Read(double realValue)` | true value | `double` | Returns (possibly wrong) reading |
| `ApplyDamage(double severity)` | 0.0–1.0 | `void` | Reduces accuracy, may change state |
| `AddNoise(double amount)` | noise factor | `void` | Temporarily adds noise (turbulence effect) |
| `Kill()` | — | `void` | Sets State = Dead, returns -1 always |
| `Repair()` | — | `void` | Resets to OK state |

---

### 5.2 `Sensor.cs` (base implementation)

```csharp
public class Sensor : ISensor
{
    private readonly Random _rng = new();
    private double _noiseBoost = 0;       // temporary extra noise from turbulence

    public string      SensorName { get; }
    public SensorState State      { get; private set; } = SensorState.OK;
    public double      Accuracy   { get; private set; } = 1.0;

    public double Read(double realValue)
    {
        if (State == SensorState.Dead)   return -1;     // -1 = "---" on dashboard
        if (State == SensorState.Fault)
        {
            // stuck at last value, or totally wrong reading
            return _lastReading;
        }
        double totalNoise = (1.0 - Accuracy) + _noiseBoost;
        double noise = (_rng.NextDouble() - 0.5) * 2 * totalNoise * realValue * 0.15;
        _lastReading = realValue + noise;
        return _lastReading;
    }

    public void ApplyDamage(double severity)
    {
        Accuracy = Math.Max(0, Accuracy - severity);
        if (Accuracy < 0.3) State = SensorState.Fault;
        if (Accuracy <= 0)  State = SensorState.Dead;
    }

    public void AddNoise(double amount)   => _noiseBoost = Math.Min(1.0, _noiseBoost + amount);
    public void ClearNoise()              => _noiseBoost = 0;
    public void Kill()                    { Accuracy = 0; State = SensorState.Dead; }
    public void Repair()                  { Accuracy = 1.0; State = SensorState.OK; _noiseBoost = 0; }

    private double _lastReading;
}
```

---

### 5.3 `SensorSystem.cs`

Holds all sensor instances. The **view reads from sensors, not from FlightData directly.** This means the player sees potentially wrong values.

| Property / Method | Parameters | Returns | Description |
|---|---|---|---|
| `Altitude` | — | `Sensor` | Reads from `FlightData.Altitude` |
| `Airspeed` | — | `Sensor` | Reads from `FlightData.Speed` |
| `Engine1RPM` | — | `Sensor` | Reads from `FlightData.Engine1RPM` |
| `Engine2RPM` | — | `Sensor` | Reads from `FlightData.Engine2RPM` |
| `Engine1Temp` | — | `Sensor` | Reads from `FlightData.Engine1TempC` |
| `Engine2Temp` | — | `Sensor` | Reads from `FlightData.Engine2TempC` |
| `FuelLevel` | — | `Sensor` | Reads from `FlightData.FuelLevelKg` |
| `HydraulicPressure` | — | `Sensor` | Reads from `HydraulicSystem.Pressure` |
| `GetAllSensors()` | — | `IReadOnlyList<ISensor>` | All sensors for display |
| `Update(double dt, FlightData data)` | dt + real data | `void` | Calls `Read()` on each, caches results |
| `GetReading(string sensorName)` | name | `double` | Returns cached sensor reading (-1 if dead) |
| `AddNoiseToAll(double amount)` | noise factor | `void` | Used by turbulence cascade |
| `DamageRandomSensor()` | — | `ISensor` | Picks a random sensor, applies damage 0.5–0.8 |
| `GetFaultySensors()` | — | `IReadOnlyList<ISensor>` | Sensors in Fault or Dead state |

---

### 5.4 Sensor state display (for SensorsPanelWidget)

| State | Display color | Reading shown |
|---|---|---|
| OK | Green | Normal value |
| Noisy | Yellow | Value with visible jitter |
| Fault | Red | "FAULT" label + last stuck value |
| Dead | Dark Red | "---" |

---

## 6. Module: Core/States – Pattern: State

State pattern: `Aircraft` holds one `IAircraftState`. All behavior delegates to it. No `if/switch` in `Aircraft`. Each state decides valid transitions.

### 6.1 `IAircraftState.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `StateName` | — | `string` | Display name |
| `StateDescription` | — | `string` | One-line description |
| `StateColor` | — | `ConsoleColor` | Dashboard header color |
| `AllowedActions` | — | `IReadOnlyList<string>` | Available actions for menu |
| `TakeOff(Aircraft ctx)` | aircraft | `void` | Handle takeoff request |
| `Cruise(Aircraft ctx)` | aircraft | `void` | Handle cruise request |
| `Descend(Aircraft ctx)` | aircraft | `void` | Handle descent request |
| `Land(Aircraft ctx)` | aircraft | `void` | Handle landing request |
| `HandleEmergency(Aircraft ctx)` | aircraft | `void` | Handle emergency |
| `Abort(Aircraft ctx)` | aircraft | `void` | Handle abort |
| `Update(Aircraft ctx, double deltaT)` | aircraft + dt | `void` | Per-frame simulation |
| `OnEnter(Aircraft ctx)` | aircraft | `void` | Called once on entry |
| `OnExit(Aircraft ctx)` | aircraft | `void` | Called once on exit |

---

### 6.2 State Classes

#### `GroundState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Speed=0, VerticalSpeed=0, gear down, engines idle |
| `TakeOff(ctx)` | Check: fuel>10%, Engine1.Health>0.3, Engine2.Health>0.3. Pass → `TransitionTo(TaxiState)`. Fail → alert |
| `Cruise(ctx)` | Alert: "Cannot cruise on ground" |
| `Land(ctx)` | Alert: "Already on ground" |
| `HandleEmergency(ctx)` | Alert: "Ground emergency" |
| `Update(ctx, dt)` | Simulate engine warm-up, fuel if refueling |

Private fields: `bool IsEngineRunning`, `int GateNumber`, `double RefuelRate`

---

#### `TaxiState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Speed=15kts |
| `TakeOff(ctx)` | At runway position → `TransitionTo(TakeOffState)` |
| `Abort(ctx)` | Return to gate → `TransitionTo(GroundState)` |
| `Update(ctx, dt)` | Advance taxi position on map |

---

#### `TakeOffState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Throttle=1.0, arm auto-rotate at VR |
| `Update(ctx, dt)` | Increase Speed from thrust; at VR → Pitch +7.5°; at V2 → climb; at 1500ft → `TransitionTo(ClimbState)` |
| `Abort(ctx)` | If Speed < V1 → cut throttle, brake, `TransitionTo(GroundState)` |
| `HandleEmergency(ctx)` | If Speed > V1 → continue to `EmergencyState`. If < V1 → `Abort()` |

Private fields: `double V1Speed`, `double VRSpeed`, `bool HasRotated`

---

#### `ClimbState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Retract gear + flaps, set target altitude from config |
| `Update(ctx, dt)` | Increase altitude at ClimbRate; when target reached → `TransitionTo(CruiseState)` |
| `Cruise(ctx)` | `TransitionTo(CruiseState)` |
| `HandleEmergency(ctx)` | `TransitionTo(EmergencyState)` |

Private fields: `double TargetAltitude`, `double ClimbRate`

---

#### `CruiseState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Engage autopilot ALT_HOLD + HDG |
| `Update(ctx, dt)` | Burn fuel, wind drift, apply asymmetric drag if `DamageModel.AsymmetricDragActive`. Check fuel < 5% → `HandleEmergency()` |
| `Descend(ctx)` | `TransitionTo(DescentState)` |
| `HandleEmergency(ctx)` | `TransitionTo(EmergencyState)` |

---

#### `DescentState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Throttle=0.3, extend flaps incrementally |
| `Update(ctx, dt)` | Reduce altitude and speed; at 3000ft and speed<180kts → `Land(ctx)` |
| `Land(ctx)` | `TransitionTo(LandingState)` |
| `Abort(ctx)` | Go-around → `TransitionTo(ClimbState)` |

---

#### `LandingState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Full flaps, gear down, ILS intercept |
| `Update(ctx, dt)` | ILS glideslope tracking; crosswind correction; flare at 50ft; touchdown → `TransitionTo(GroundState)`. If `AsymmetricDragActive` → harder correction needed, drifts on approach |
| `Abort(ctx)` | Go-around: full thrust, gear up, `TransitionTo(ClimbState)` |

Private fields: `double ILSDeviation`, `double TouchdownSpeed`, `LandingPhase Phase`

---

#### `HoldingState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Record hold fix, bank angle 25° |
| `Update(ctx, dt)` | Racetrack pattern; burn fuel; count loops |
| `Land(ctx)` | `TransitionTo(DescentState)` |
| `HandleEmergency(ctx)` | Fuel critical → immediate `TransitionTo(DescentState)` |

---

#### `EmergencyState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Publish `MaydayEvent`, log MAYDAY |
| `Update(ctx, dt)` | Track most critical active anomaly; escalate if severity increases → `TransitionTo(CriticalState)` |
| `Land(ctx)` | Immediate approach → `TransitionTo(LandingState)` |
| `HandleEmergency(ctx)` | Escalate → `TransitionTo(CriticalState)` |

Private fields: `double Severity`, `bool MaydayDeclared`

---

#### `CriticalState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Disable autopilot, clear most allowed actions, max damage decay begins |
| `Update(ctx, dt)` | Check `DamageModel.CheckGameOver()` each tick. If true → trigger black box GAME OVER sequence. Otherwise limited control |
| `Land(ctx)` | Last-ditch crash landing |

---

## 7. Module: Core/Strategies – Pattern: Strategy

### 7.1 `IAnomaly.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `AnomalyName` | — | `string` | Display name |
| `Description` | — | `string` | Short description |
| `Level` | — | `Severity` | Low / Medium / High / Critical |
| `Probability` | — | `double` | Chance per second of triggering |
| `IsActive` | — | `bool` | Currently active flag |
| `CanBeResolved` | — | `bool` | Whether player can fix it |
| `Trigger(Aircraft ctx, FlightData data)` | aircraft + telemetry | `void` | Activate anomaly, immediate effect |
| `Update(Aircraft ctx, FlightData data)` | aircraft + telemetry | `void` | Per-tick while active |
| `Resolve(Aircraft ctx)` | aircraft | `bool` | Player attempts fix. True = success |
| `GetWarningMessage()` | — | `string` | Alert bar text |
| `GetPilotAction()` | — | `string` | Instruction for player |

---

### 7.2 `AbstractAnomaly.cs`

```csharp
public abstract class AbstractAnomaly : IAnomaly
{
    protected bool   _isActive;
    protected double _activeDuration;
    protected Random _rng = new();

    protected void PublishAlert(Aircraft ctx, string msg, Severity level);
    protected bool CheckProbability(double deltaT);
    protected void TriggerCascade(Aircraft ctx, IAnomaly cascade);  // chains next anomaly
}
```

---

### 7.3 Anomaly Implementations

#### `BirdStrikeAnomaly.cs` (UPDATED with cascade)

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Picks random engine. `ApplyDamage(SystemType.Engine, 0.3)`. GForce spike +0.5g. Publishes `SystemFailureEvent`. **Cascade roll: 40% → calls `TriggerCascade(ctx, new EngineFireAnomaly())`** |
| `Update(ctx, data)` | aircraft, telemetry | `void` | GForce oscillates ±0.15g while active (vibration). Checks if engine RPM sensor should be damaged: if engine health < 0.5 → `ctx.Sensors.Engine1RPM.ApplyDamage(0.4)` |
| `Resolve(ctx)` | aircraft | `bool` | `CanBeResolved = false`. Bird strike is a one-time event. Sets `IsActive = false` automatically after 10 seconds |

**Spawn condition:** Only at altitude < 10,000ft

---

#### `EngineFireAnomaly.cs` (NEW)

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Calls `EngineSystem.StartFire()`. Publishes `EngineFireEvent`. Sets fire decay: engine health -3%/sec |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Every 10 seconds: roll 30% chance → `TriggerCascade(ctx, new WingFireAnomaly())`. Engine health decays. If engine health reaches 0 → calls `EngineSystem.Explode()` which damages wing. Temperature sensor becomes noisy |
| `Resolve(ctx)` | aircraft | `bool` | Calls `EngineSystem.ExtinguishFire()`. Returns result. On success: `IsActive = false`, fire damage stops (but engine health stays where it is) |

**Properties:** `Level = Critical`, `CanBeResolved = true`, `GetPilotAction() = "Press [R] to activate fire suppression"`

---

#### `WingFireAnomaly.cs` (NEW)

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Calls `WingSystem.StartFire()`. Publishes `WingFireEvent`. Begins wing health decay -1%/sec base rate |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Wing health decays. At 50% health → `DamageModel.AsymmetricDragActive = true`, aircraft starts drifting. At 20% health → `ElectricalSystem.ApplyDamage(0.6)` (wiring burns). At 0% health → `DamageModel.IsGameOver = true`, `DamageModel.GameOverReason = "Wing structural failure"` |
| `Resolve(ctx)` | aircraft | `bool` | Calls `WingSystem.ExtinguishFire()`. Returns false if wing health < 40%. Player must act fast |

**Properties:** `Level = Critical`, `CanBeResolved = true (barely)`, `GetPilotAction() = "Press [R] for wing fire suppression -- ACT FAST"`

---

#### `EngineFailureAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Engine health → 0, calls `Stop()`. RPM decays to 0. Publishes `SystemFailureEvent`. Engine RPM sensor → `Fault` (reads 0 even if restarted until sensor repaired) |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Checks if restart attempted. If only one engine left: aircraft slowly loses speed |
| `Resolve(ctx)` | aircraft | `bool` | Calls `EngineSystem.Restart()`. False if health < 0.2 or alt < 5000ft |

---

#### `TurbulenceAnomaly.cs` (UPDATED with sensor cascade)

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Sets turbulence level random Low–High. **If Medium or higher → `ctx.Sensors.AddNoiseToAll(0.15)`**. If Critical → `ctx.Sensors.DamageRandomSensor()` |
| `Update(ctx, data)` | aircraft, telemetry | `void` | Each tick: altitude ±200ft random, speed ±15kts, GForce oscillates. Sensor noise persists while active |
| `Resolve(ctx)` | aircraft | `bool` | Change altitude ±2000ft. Auto-resolves after 3–8 min. On resolve: `ctx.Sensors.ClearNoise()` (noise goes back to just accuracy-based) |

---

#### `SensorFailureAnomaly.cs` (NEW)

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | aircraft, telemetry | `void` | Picks a random critical sensor (altitude or airspeed preferred). Calls `sensor.ApplyDamage(0.7)`. If altitude sensor faulted → autopilot gets wrong target → may climb or descend incorrectly |
| `Update(ctx, data)` | aircraft, telemetry | `void` | If altitude sensor is Fault and autopilot is on: autopilot drifts the real altitude by ±50ft/sec (it thinks it's somewhere else). Player must disengage autopilot |
| `Resolve(ctx)` | aircraft | `bool` | Player presses [R]. Calls `sensor.Repair()`. Autopilot re-syncs to true altitude |

---

#### `HydraulicFailureAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | `HydraulicSystem.Pressure = 0`. Gear jams if mid-transit. Flaps stuck. Hydraulic sensor → Fault |
| `Update(ctx, data)` | — | `void` | If in landing and gear still up → escalate warning |
| `Resolve(ctx)` | — | `bool` | `HydraulicSystem.EmergencyGearExtension()`. Flaps remain stuck |

---

#### `FuelLeakAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | `FuelSystem.StartLeak(80–250 kg/h random)`. Fuel sensor becomes noisy (reads slightly higher than real) |
| `Update(ctx, data)` | — | `void` | Monitor fuel. At <5% → `FuelCriticalEvent`. **Every 60s unresolved: `FuelSystem.CheckIgnitionRisk()` → 20% chance → `TriggerCascade(EngineFireAnomaly)`** |
| `Resolve(ctx)` | — | `bool` | `FuelSystem.SealLeak()`. Fuel sensor noise cleared on success |

---

#### `ElectricalFailureAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | Main bus voltage = 0. Autopilot goes offline. All sensors lose -40% accuracy |
| `Update(ctx, data)` | — | `void` | After 30s → secondary bus fails → nav offline. Systems without power decay slowly |
| `Resolve(ctx)` | — | `bool` | `ElectricalSystem.SwitchToBackupBattery()`. Sensors recover -20% noise (battery is weaker) |

---

#### `DecompressionAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | Only valid if Altitude > 25,000ft. MAYDAY. Target altitude forced to 10,000ft. Altitude sensor and speed sensor both get `AddNoise(0.3)` |
| `Update(ctx, data)` | — | `void` | If not descending within 60s → game over (pilot incapacitation) |
| `Resolve(ctx)` | — | `bool` | `CanBeResolved = false`. Must descend below 10,000ft |

---

#### `IcingAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | Only if TempC < 0 and humidity high. Stall speed rises +1kt/min. Airspeed sensor gets noise (ice on pitot tube) |
| `Update(ctx, data)` | — | `void` | Stall speed rises. If >15kts above normal → Level escalates. Airspeed sensor noise increases over time |
| `Resolve(ctx)` | — | `bool` | `ElectricalSystem.ActivateDeIcing()`. Clears airspeed sensor noise on success |

---

#### `MicroburstAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | During approach only. Sudden +40kt headwind then -40kt tailwind within 10 seconds |
| `Update(ctx, data)` | — | `void` | Rapid altitude loss -1000ft/min extra. If no full thrust within 5s → game over |
| `Resolve(ctx)` | — | `bool` | Full throttle + pitch up within time window |

---

#### `RunwayIncursionAnomaly.cs`

| Method | Parameters | Returns | Behavior |
|---|---|---|---|
| `Trigger(ctx, data)` | — | `void` | LandingState only, alt < 500ft. ATC warning |
| `Update(ctx, data)` | — | `void` | Countdown. No go-around in 15s → forced collision → game over |
| `Resolve(ctx)` | — | `bool` | Player presses go-around key |

---

### 7.4 Weather Strategies

`IWeatherStrategy` — same interface as before, with one addition:

| Method | Parameters | Returns | Description |
|---|---|---|---|
| (all previous methods) | — | — | unchanged |
| `ApplySensorEffects(SensorSystem sensors)` | sensor system | `void` | NEW: weather-specific sensor interference |

| Class | Sensor effect |
|---|---|
| `ClearSkiesStrategy` | No sensor effects |
| `ThunderstormStrategy` | All sensors +0.1 noise. Lightning strike: 5% chance/min → random sensor `ApplyDamage(0.5)` |
| `FogStrategy` | No sensor effects (ground-level fog) |
| `CrosswindStrategy` | No sensor effects |
| `IcingConditionsStrategy` | Airspeed sensor +0.2 noise (ice on pitot tube) |
| `WindShearStrategy` | Altitude and airspeed sensors +0.15 noise during shear event |

---

## 8. Module: Core/Events – Pattern: Observer

### 8.1 `FlightEvent.cs` (abstract base)

```csharp
public abstract class FlightEvent
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string   Source    { get; init; }
    public Severity Level     { get; init; }
    public string   Message   { get; init; }
}
```

#### Concrete Event Classes

| Class | Extra Fields | Triggered When |
|---|---|---|
| `AltitudeChangedEvent` | `double NewAltitude, OldAltitude` | Altitude changes > 100ft |
| `StateChangedEvent` | `string OldState, NewState` | `Aircraft.TransitionTo()` called |
| `AnomalyTriggeredEvent` | `IAnomaly Anomaly` | `IAnomaly.Trigger()` called |
| `AnomalyResolvedEvent` | `IAnomaly Anomaly, bool Success` | `IAnomaly.Resolve()` called |
| `CascadeTriggeredEvent` | `string Source, string Target` | NEW: cascade chain link activated |
| `EngineFireEvent` | `int EngineNumber` | NEW: engine catches fire |
| `WingFireEvent` | `string Side` | NEW: wing catches fire |
| `EngineExplosionEvent` | `int EngineNumber` | NEW: engine explodes |
| `AsymmetricDragEvent` | `string DamagedSide, double DriftRate` | NEW: wing melted enough to drift |
| `GameOverEvent` | `string Reason` | NEW: fatal condition reached |
| `SystemFailureEvent` | `SystemType System, double Health` | System health < 0.3 |
| `SensorFaultEvent` | `string SensorName, SensorState State` | NEW: sensor changes state |
| `FuelLowEvent` | `double RemainingPercent` | Fuel < 15% |
| `FuelCriticalEvent` | `double RemainingPercent` | Fuel < 5% |
| `WeatherChangedEvent` | `IWeatherStrategy NewWeather` | Weather changes |
| `LandingCompletedEvent` | `double TouchdownSpeedKts, bool Successful` | Wheels touch runway |
| `MaydayEvent` | `string Reason, EmergencyType Type` | Emergency declared |
| `CommandExecutedEvent` | `string CommandName, string Details` | Any command executed |
| `PlayerInputEvent` | `PlayerAction Action, ConsoleKey Key` | Player key press |
| `TelemetryTickEvent` | `FlightDataSnapshot Snapshot` | Every 1 second |

---

### 8.2 `EventBus.cs` (Singleton)

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Instance` (static) | — | `EventBus` | Lazy singleton |
| `Subscribe<T>(IFlightEventHandler h)` | handler | `void` | Register for event type T |
| `Unsubscribe<T>(IFlightEventHandler h)` | handler | `void` | Unregister |
| `Publish<T>(T evt)` | event | `void` | Dispatch to all handlers |
| `History` | — | `IReadOnlyList<FlightEvent>` | Full event log |
| `ClearHistory()` | — | `void` | Reset log (new flight) |

---

### 8.3 Handler Classes

#### `CascadeHandler.cs` (NEW)

This is the brain of the cascade system. It listens for events and decides what triggers next.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `CascadeHandler(AnomalyEngine engine)` | constructor | — | Injects AnomalyEngine reference |
| `Handle(FlightEvent evt)` | event | `void` | Switch on event type. For each type, apply cascade table from section 3.1 |
| `HandledEventTypes` | — | `IEnumerable<Type>` | EngineFireEvent, WingFireEvent, EngineExplosionEvent, SystemFailureEvent, AnomalyTriggeredEvent, FuelCriticalEvent |

Internal logic example:
```
on EngineFireEvent received:
    schedule: after 10 seconds, roll 30% → trigger WingFireAnomaly

on EngineExplosionEvent received:
    aircraft.WingSystem.ApplyDamage(0.4)
    aircraft.Sensors.Engine1RPM.Kill()
    publish AsymmetricDragEvent if wing health now < 20%
```

#### `BlackBoxHandler.cs`

Records everything. On `GameOverEvent` → immediately saves to file and triggers `BlackBoxReadoutView`.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | event | `void` | Append to `List<FlightEvent>` |
| `SaveToFile(string path)` | path | `void` | Write full event list as structured text |
| `GetFullHistory()` | — | `IReadOnlyList<FlightEvent>` | All events |
| `PrintReadout()` | — | `void` | Formatted black box print to console (used after game over) |

#### `AlertSystemHandler.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | event | `void` | Add alert to queue |
| `GetActiveAlerts()` | — | `IReadOnlyList<string>` | Max 3 most recent alerts |
| `DismissAlert(int index)` | index | `void` | Remove one alert |

#### `FlightLoggerHandler.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | event | `void` | Write formatted line to `.log` file |
| `SetLogPath(string path)` | path | `void` | Set output file path |

#### `StatisticsHandler.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Handle(FlightEvent evt)` | event | `void` | Update counters |
| `GetStatistics()` | — | `FlightStatistics` | Aggregated stats for report |

---

## 9. Module: Core/Commands – Pattern: Command

### 9.1 `IFlightCommand.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `CommandName` | — | `string` | Human-readable name |
| `ExecutedAt` | — | `DateTime` | Timestamp |
| `CanUndo` | — | `bool` | Reversible flag |
| `Execute()` | — | `void` | Apply command |
| `Undo()` | — | `void` | Reverse command |
| `GetDescription()` | — | `string` | One-line description for black box |

### 9.2 Command Implementations

| Class | Constructor Params | Execute | Undo | CanUndo |
|---|---|---|---|---|
| `SetThrottleCommand` | `Aircraft a, double newVal` | `FlightData.Throttle = newVal` | Restore previous | `true` |
| `SetHeadingCommand` | `Aircraft a, double newVal` | `FlightData.TargetHeading = newVal` | Restore previous | `true` |
| `SetAltitudeCommand` | `Aircraft a, double newVal` | Autopilot target altitude | Restore previous | `true` |
| `ToggleAutopilotCommand` | `Aircraft a` | Engage or disengage | Reverse | `true` |
| `ActivateSystemCommand` | `Aircraft a, SystemType sys` | Activate system | Deactivate | `true` |
| `ResolveAnomalyCommand` | `Aircraft a, IAnomaly anomaly` | `anomaly.Resolve(a)` | Cannot un-resolve | `false` |
| `EmergencyDeclareCommand` | `Aircraft a` | `a.DeclareEmergency()` | N/A | `false` |
| `GoAroundCommand` | `Aircraft a` | `a.Abort()` | N/A | `false` |

### 9.3 `CommandHistory.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Execute(IFlightCommand cmd)` | command | `void` | Run, push to undo stack, publish `CommandExecutedEvent` |
| `Undo()` | — | `bool` | Pop from undo, reverse, push to redo |
| `Redo()` | — | `bool` | Pop from redo, re-execute |
| `GetAll()` | — | `IReadOnlyList<IFlightCommand>` | Full command list for black box |
| `SaveToFile(string path)` | path | `void` | Export history as text |
| `Clear()` | — | `void` | Reset all stacks |

---

## 10. Module: Controllers – MVC Controller

### 10.1 `FlightController.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `FlightController(Aircraft a, IFlightView v, SimulationConfig cfg)` | constructor | — | Wire all components |
| `RunAsync(CancellationToken ct)` | cancellation token | `Task` | Main 10 Hz loop |
| `ExecuteCommand(IFlightCommand cmd)` | command | `void` | Pass to `CommandHistory.Execute()` |
| `SetThrottle(double v)` | 0.0–1.0 | `void` | Create + execute `SetThrottleCommand` |
| `AdjustThrottle(double delta)` | change | `void` | Throttle ± delta, clamp 0–1 |
| `SetHeading(double deg)` | 0–360 | `void` | Create + execute `SetHeadingCommand` |
| `AdjustHeading(double delta)` | degrees | `void` | Heading ± delta |
| `SetTargetAltitude(double feet)` | feet | `void` | Create + execute `SetAltitudeCommand` |
| `ToggleAutopilot()` | — | `void` | Create + execute `ToggleAutopilotCommand` |
| `ExecuteTakeOff()` | — | `void` | `aircraft.TakeOff()` via command |
| `ExecuteLand()` | — | `void` | `aircraft.Land()` via command |
| `ExecuteEmergency()` | — | `void` | `EmergencyDeclareCommand` |
| `ExecuteGoAround()` | — | `void` | `GoAroundCommand` |
| `ResolveTopAnomaly()` | — | `void` | Get `AnomalyEngine.MostCritical`, create `ResolveAnomalyCommand` |
| `UndoLastCommand()` | — | `void` | `CommandHistory.Undo()` |
| `GetFlightReport()` | — | `FlightReport` | Build + return report |

---

### 10.2 `InputHandler.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Poll()` | — | `PlayerAction?` | Non-blocking. `Console.KeyAvailable` check. Returns null if nothing pressed |
| `SetKeyMap(Dictionary<ConsoleKey, PlayerAction> map)` | map | `void` | Override default bindings |
| `GetKeyMap()` | — | `IReadOnlyDictionary<ConsoleKey, PlayerAction>` | Current bindings |

**Default key map:**

| Key | Action |
|---|---|
| W | IncreaseThrottle |
| S | DecreaseThrottle |
| A | TurnLeft (adjust heading -5°) |
| D | TurnRight (adjust heading +5°) |
| Z | AltitudeUp (target +1000ft) |
| X | AltitudeDown (target -1000ft) |
| Space | NextPhase (context-sensitive: TakeOff / Descend / Land) |
| Q | ToggleAutopilot |
| E | DeclareEmergency |
| R | ResolveAnomaly (most critical active) |
| F | FlapsUp |
| G | FlapsDown |
| L | DeployGear |
| H | GoAround |
| U | UndoLastCommand |
| Tab | ViewReport |
| Escape | Quit |

---

### 10.3 `AnomalyEngine.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `AnomalyEngine(Aircraft a, SimulationConfig cfg)` | constructor | — | Initialize pool from `AnomalyFactory` |
| `Tick(double dt)` | dt | `void` | `TrySpawnAnomaly()` + `UpdateActiveAnomalies()` |
| `TrySpawnAnomaly(double dt)` | dt | `void` | Roll probability per anomaly. At most 1 new per 30 seconds |
| `UpdateActiveAnomalies(double dt)` | dt | `void` | Call `Update()` on each active anomaly |
| `ForceSpawn(IAnomaly anomaly)` | anomaly | `void` | Used by `CascadeHandler` to inject cascade anomalies |
| `ResolveAnomaly(string name)` | name | `bool` | Find + resolve by name |
| `ActiveAnomalies` | — | `IReadOnlyList<IAnomaly>` | Currently active |
| `MostCritical` | — | `IAnomaly?` | Highest severity active anomaly |
| `SetDifficulty(Difficulty d)` | difficulty | `void` | Adjust probability multiplier |

**Spawn weighting:**
- Base probability from `IAnomaly.Probability`
- × `config.AnomalyFrequency`
- +50% in TakeOffState and LandingState
- +20% per hour of flight
- +30% if weather `IsHazardous`
- Max 1 new anomaly per 30 seconds
- Fire anomalies can only be force-spawned by cascade (not random)

---

## 11. Module: Views – MVC View

**Rule: View NEVER modifies model data. It only reads through public properties.**

### 11.1 `IFlightView.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Render(Aircraft aircraft)` | aircraft | `void` | Full dashboard redraw |
| `ShowAlert(string msg, AlertLevel level)` | message + level | `void` | Push to alerts bar |
| `ShowMenu(IReadOnlyList<string> options)` | options | `void` | Render action menu |
| `ShowGameOver(string reason)` | reason | `void` | Crash screen |
| `ShowFlightReport(FlightReport report)` | report | `void` | End of flight summary |
| `Clear()` | — | `void` | Clear console |

---

### 11.2 `ConsoleDashboardView.cs`

**Rendering strategy:**
- `Console.SetCursorPosition(0, 0)` at start — no flicker (overwrites in place)
- `Console.CursorVisible = false` at startup
- Build full frame as `string[]` buffer, then write all at once

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Render(Aircraft a)` | aircraft | `void` | Calls all widgets in order |
| `RenderHeader(Aircraft a)` | aircraft | `void` | State name (state color), tail, model, flight time, difficulty |
| `RenderPFD(SensorSystem s)` | sensors | `void` | **Reads from SENSORS not FlightData.** Shows sensor readings with fault markers |
| `RenderFuelGauge(SensorSystem s)` | sensors | `void` | Fuel bar from sensor reading. If sensor fault → shows "FAULT" |
| `RenderSystemsPanel(Aircraft a)` | aircraft | `void` | Grid: ENG1/ENG2/WING/HYD/ELEC/NAV. Color by health. Shows fire state |
| `RenderSensorsPanel(SensorSystem s)` | sensors | `void` | NEW: Grid of all sensors with OK/NOISY/FAULT/DEAD |
| `RenderWeatherPanel(WeatherSystem ws)` | weather | `void` | Weather name, wind, turbulence, visibility |
| `RenderAlertsBar(IReadOnlyList<string> alerts)` | alerts | `void` | Up to 3 alert lines. Critical blinks |
| `RenderActionMenu(IAircraftState state)` | state | `void` | `AllowedActions` as key-action pairs |
| `RenderFlightMap(FlightData fd, List<MapObject> objects)` | telemetry + objects | `void` | 40x12 ASCII grid with plane (A), birds (*), waypoints (+) |
| `RenderCockpitWindow(double pitch, double roll, FireState wingFire)` | attitude + fire | `void` | ASCII horizon view. If wing fire → show fire chars on side |

---

### 11.3 View Widget Classes

| Class | Method signature | Returns | Description |
|---|---|---|---|
| `AltimeterWidget` | `Render(double sensorAlt, SensorState state)` | `string[]` | Altitude reading with FAULT marker if sensor bad |
| `AirspeedWidget` | `Render(double sensorSpd, double vmo, SensorState state)` | `string[]` | Speed tape, red if near VMO |
| `FuelGaugeWidget` | `Render(double sensorPct, double kg, double range)` | `string[]` | Bar + kg + range. "FAULT" if sensor dead |
| `SystemsPanelWidget` | `Render(IReadOnlyList<IAvionicSystem> systems, DamageModel dm)` | `string[]` | Shows fire state + health per system |
| `SensorsPanelWidget` | `Render(SensorSystem sensors)` | `string[]` | NEW: all sensors, color by state |
| `AlertsBarWidget` | `Render(IReadOnlyList<string> alerts)` | `string[]` | Alert list |
| `ActionMenuWidget` | `Render(IReadOnlyList<string> actions)` | `string[]` | Key-action pairs |
| `FlightMapWidget` | `Render(double mapX, double mapY, double heading, IReadOnlyList<MapObject> objects)` | `string[]` | 40x12 grid. A=plane, *=bird, +=waypoint, ~=weather zone |
| `CockpitWindowWidget` | `Render(double pitch, double roll, FireState wingFire, FireState engine1Fire)` | `string[]` | ASCII horizon. Roll tilts horizon line. Fire = "***FIRE***" |

---

### 11.4 `CockpitWindowWidget.cs` — how it works

```
pitch = -2.0  roll = +5.0
Wing fire on LEFT side:

***FIRE***  sky sky sky sky sky sky sky sky
sky sky sky sky sky sky sky sky sky sky sky
 ------- horizon --- A -- horizon -------
gnd gnd gnd gnd gnd gnd gnd gnd gnd gnd
gnd gnd gnd gnd gnd gnd gnd gnd gnd gnd
```

- `pitch` shifts horizon line up/down
- `roll` tilts horizon line left/right (slant using `/` `\` chars)
- `A` symbol is always centered (the plane)
- `***FIRE***` appears on correct side when wing/engine is on fire
- If engine explodes → one `EXPLOSION` flash frame then engine removed from view

---

### 11.5 `StartupScreen.cs` (UPDATED)

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Show()` | — | `StartupSelection` | Full startup sequence |
| `SelectAircraft()` | — | `AircraftConfig` | Numbered list with stats (speed, range, fuel, wing strength) |
| `SelectRoute()` | — | `RouteConfig` | Short (30min) / Medium (1hr) / Long (2hr). Sets waypoints A→B |
| `SelectDifficulty()` | — | `Difficulty` | Easy (no anomalies) / Normal / Hard (full cascades enabled) |
| `ShowAircraftStats(AircraftConfig cfg)` | config | `void` | ASCII stat bars for each aircraft |

---

### 11.6 `BlackBoxReadoutView.cs` (NEW)

Shown after game over. Prints the complete black box in a dramatic terminal-style readout.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Show(IReadOnlyList<FlightEvent> events, IReadOnlyList<IFlightCommand> commands)` | full history | `void` | Prints line by line, with brief pause between lines (typewriter effect) |
| `PrintHeader()` | — | `void` | "=== FLIGHT DATA RECORDER READOUT ===" |
| `PrintEventLine(FlightEvent evt)` | event | `void` | `[HH:mm:ss] [LEVEL] [SOURCE] Message` |
| `PrintCommandLine(IFlightCommand cmd)` | command | `void` | `[HH:mm:ss] PILOT ACTION: description` |
| `PrintSummary(DamageModel dm)` | damage model | `void` | Final state of all systems + game over reason |

---

## 12. Module: Infrastructure

### 12.1 `SimulationConfig.cs` (Singleton)

| Property / Method | Returns | Description |
|---|---|---|
| `Instance` (static) | `SimulationConfig` | Lazy singleton |
| `SimulationSpeed` | `double` | 1.0 = real-time |
| `AnomaliesEnabled` | `bool` | Master toggle |
| `AnomalyFrequency` | `double` | Probability multiplier |
| `CascadesEnabled` | `bool` | NEW: whether cascade chains fire |
| `BlackBoxEnabled` | `bool` | Write to file |
| `LogDirectory` | `string` | Output path |
| `LoadFromFile(string path)` | `void` | Load JSON |
| `SaveToFile(string path)` | `void` | Save JSON |

---

### 12.2 `FlightLogger.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `FlightLogger(string logDir)` | directory | — | Creates timestamped log file |
| `Log(LogLevel level, string msg, string src)` | — | `void` | `[HH:mm:ss][LEVEL][src] msg` |
| `LogEvent(FlightEvent evt)` | event | `void` | Formatted event line |
| `LogTelemetry(FlightData data)` | data | `void` | CSV row every second |
| `SaveFlightReport(FlightReport report)` | report | `void` | Write formatted report |
| `Flush()` | — | `void` | Force write buffer |
| `Close()` | — | `void` | Close file handle |

---

### 12.3 `FlightReport.cs`

```csharp
public record FlightReport
{
    public string    TailNumber         { get; init; }
    public string    Model              { get; init; }
    public DateTime  DepartureTime      { get; init; }
    public TimeSpan  FlightDuration     { get; init; }
    public double    DistanceKm         { get; init; }
    public double    FuelUsedKg         { get; init; }
    public double    MaxAltitudeFt      { get; init; }
    public double    MaxSpeedKts        { get; init; }
    public int       AnomaliesTotal     { get; init; }
    public int       AnomaliesResolved  { get; init; }
    public int       CascadesTriggered  { get; init; }  // NEW
    public bool      LandedSafely       { get; init; }
    public double    TouchdownSpeedKts  { get; init; }
    public double    LandingScore       { get; init; }   // 0–100
    public string    GameOverReason     { get; init; }   // null if landed safely
    public IReadOnlyList<FlightEvent>    EventLog    { get; init; }
    public IReadOnlyList<IFlightCommand> CommandLog  { get; init; }

    public void PrintToConsole();
    public void SaveAsText(string path);
}
```

**Landing score formula:**
`score = 100 - speedPenalty - verticalSpeedPenalty - anomalyPenalty - crosswindPenalty - cascadePenalty`

---

### 12.4 `AircraftFactory.cs`

| Method | Returns | Boeing 737-800 values |
|---|---|---|
| `CreateBoeing737()` | `Aircraft` | Fuel=26000kg, MaxAlt=41000ft, Cruise=460kts, V1=148, VR=152, V2=158, WingStrength=0.85 |
| `CreateAirbusA320()` | `Aircraft` | Fuel=18800kg, MaxAlt=39800ft, Cruise=450kts, V1=142, VR=146, V2=152, WingStrength=0.90 |
| `CreateCessna172()` | `Aircraft` | Fuel=212kg, MaxAlt=14000ft, Cruise=122kts, V1=55, VR=60, V2=65, WingStrength=0.70 |
| `Create(AircraftConfig cfg, string tail, string model)` | `Aircraft` | Generic |

---

### 12.5 `AnomalyFactory.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Create(AnomalyType type)` | type | `IAnomaly` | New instance |
| `CreateRandom(FlightData ctx)` | telemetry | `IAnomaly` | Weighted random based on alt/speed/state |
| `GetAllFor(IAircraftState state)` | state | `IReadOnlyList<IAnomaly>` | Valid anomalies for this state |
| `GetPool()` | — | `IReadOnlyList<IAnomaly>` | Full default pool |
| `CreateCascade(AnomalyType type)` | type | `IAnomaly` | For cascade injection (skips probability check) |

---

## 13. Gameplay Loop & Input Map

### Complete Game Flow

```
Program.cs
  |
  StartupScreen.Show()
    SelectAircraft()      -> AircraftFactory.Create()
    SelectRoute()         -> sets FlightData.TargetWaypoint
    SelectDifficulty()    -> SimulationConfig settings
  |
  FlightController.RunAsync()
    |
    [10 Hz loop]
    aircraft.Update(dt)         <- physics, all systems, damage model
    sensorSystem.Update(dt)     <- sensors read real values
    anomalyEngine.Tick(dt)      <- random events + cascade injections
    view.Render(aircraft)       <- full redraw from SENSOR readings
    input = inputHandler.Poll() <- non-blocking keyboard
    if (input != null)
        controller.Execute(input)
    if (damageModel.IsGameOver)
        -> BlackBoxReadoutView.Show()
        -> FlightReportView.Show()
        -> break loop
    Thread.Sleep(100)
  |
  [flight ends normally: LandingCompletedEvent]
    FlightReportView.Show()
    BlackBoxHandler.SaveToFile()
```

### State Transition Diagram

```
                 +----------+
          -----> |  GROUND  | <---------------------------+
          |      +----+-----+                             |
          |           | TakeOff() OK                      |
          |           v                                   |
          |      +----+------+                            |
          |      |   TAXI   |                             |
          |      +----+------+                            |
          |           | at runway                         |
          |           v                                   |
          |      +----+--------+  Abort (<V1)             |
          |      |  TAKE OFF  | ----------> [brake] ------+
          |      +----+--------+
          |           | 1500ft + V2
          |           v
          |      +----+-----+
          |      |  CLIMB   |
          |      +----+-----+
          |           | target alt
          |           v
          |      +----+------+
          |      |  CRUISE   | <----+
          |      +----+------+      |
          |           | Descend()   | (holding cleared)
          |           v             |
          |      +----+-------+  +--+---------+
          |      |  DESCENT  |  |  HOLDING   |
          |      +----+-------+  +------------+
          |           | <3000ft
          |           v
          |      +----+-------+  Abort()
          +------| LANDING   | ---------> [go-around] --> CLIMB
                 +----+-------+
                      | touchdown + stop
                      v
                  [GROUND]

  ANY STATE --[HandleEmergency()]--> EMERGENCY ---> CRITICAL
  CRITICAL --[DamageModel.IsGameOver]--> GAME OVER -> BlackBoxReadout
```

---

## 14. Black Box & Flight Logging

### What gets recorded

| Event | Details stored |
|---|---|
| `TelemetryTickEvent` (every 1 sec) | Altitude, speed, heading, throttle, RPM ×2, fuel, G-force, pitch, roll, all sensor readings |
| State transition | Old state, new state, timestamp |
| Anomaly triggered | Anomaly name, severity, snapshot of FlightData |
| Anomaly resolved (success/fail) | Anomaly name, result, snapshot |
| **Cascade triggered** | Source anomaly, target anomaly, timestamp |
| **Sensor fault** | Sensor name, new state, real value vs reported value |
| **Fire events** | Engine/wing fire start, spread, explosion |
| **Asymmetric drag activated** | Drift rate, damaged side |
| Player input | Key, action, timestamp |
| Command executed | Name, params, pre/post snapshot |
| System failure | System name, health |
| Landing | Touchdown speed, V/S, crosswind component |
| **Game over** | Reason, final state of all systems |

### Output files

| File | Format | Content |
|---|---|---|
| `flight_YYYYMMDD_HHmmss.log` | Plain text | Human-readable event log |
| `blackbox_YYYYMMDD_HHmmss.txt` | Structured text | Full raw event + sensor dump |
| `telemetry_YYYYMMDD_HHmmss.csv` | CSV | One row per second: all FlightData + sensor readings |
| `report_YYYYMMDD_HHmmss.txt` | Plain text | Formatted flight summary |

### Black Box readout after game over (example output)

```
=== FLIGHT DATA RECORDER — AeroSim ===
AIRCRAFT: SP-LRA Boeing 737-800
FLIGHT DURATION: 00:47:23
=== EVENT LOG ===
[00:12:44] [INFO]     [WeatherSystem]   Weather changed: THUNDERSTORM
[00:18:02] [HIGH]     [AnomalyEngine]   TURBULENCE triggered -- severity: HIGH
[00:18:02] [WARN]     [SensorSystem]    SENSOR NOISE added to all sensors
[00:18:45] [HIGH]     [AnomalyEngine]   BIRD STRIKE triggered -- ENG1
[00:18:45] [CRIT]     [CascadeHandler]  CASCADE: BirdStrike -> EngineFireAnomaly
[00:18:45] [INFO]     [PlayerInput]     PILOT ACTION: ResolveAnomaly (EngineFireAnomaly)
[00:18:46] [INFO]     [EngineSystem]    Fire suppression: FAILED (health too low)
[00:18:50] [CRIT]     [CascadeHandler]  CASCADE: EngineFire -> WingFireAnomaly
[00:18:50] [CRIT]     [WingSystem]      WING FIRE started
[00:19:14] [CRIT]     [WingSystem]      Wing health 50% -- ASYMMETRIC DRAG ACTIVE
[00:19:14] [INFO]     [PlayerInput]     PILOT ACTION: DeclareEmergency
[00:19:14] [CRIT]     [Aircraft]        STATE: Cruise -> EmergencyState
[00:19:44] [CRIT]     [WingSystem]      Wing health 20% -- ELECTRICAL FAILURE (wiring)
[00:20:02] [CRIT]     [WingSystem]      Wing health 0% -- STRUCTURAL FAILURE
[00:20:02] [FATAL]    [DamageModel]     GAME OVER: Wing structural failure
=== FINAL SYSTEM STATE ===
  ENGINE 1:  FAILED (exploded)      ENGINE 2:  OK (72%)
  WING:      DESTROYED              HYDRAULICS: OK
  ELECTRICAL: FAILED                FUEL: 38% remaining
  SENSORS:   ENG1-RPM: DEAD | ALT-SNS: NOISY | others: OK
=== END OF RECORDING ===
```

---

## 15. Implementation Roadmap

> Follow stages in order. Each builds on the previous.

### Stage 1 – Foundation (~600 LOC)
- [ ] Create full directory structure
- [ ] Implement all enums: `SystemType`, `SystemStatus`, `Severity`, `PlayerAction`, `AnomalyType`, `FireState`, `SensorState`
- [ ] Implement `AircraftConfig` (record with all fields including WingStrength)
- [ ] Implement `FlightData` (all properties + methods + `ApplyAsymmetricDrift()`)
- [ ] Implement `FlightDataSnapshot` (record)
- [ ] Implement `DamageModel` (all properties + `Update()` + `CheckGameOver()`)
- [ ] Implement `IAvionicSystem` interface
- [ ] Stub all system classes with empty methods
- [ ] Unit tests: `FlightData` calculations, `DamageModel.CheckGameOver()`

### Stage 2 – Sensor System (~500 LOC)
- [ ] Implement `ISensor` interface
- [ ] Implement `Sensor` base class (Read with noise, ApplyDamage, Kill, Repair)
- [ ] Implement `SensorSystem` (all sensors, `Update()`, `GetReading()`, `AddNoiseToAll()`, `DamageRandomSensor()`)
- [ ] Add `Sensors` property to `Aircraft`
- [ ] Unit tests: sensor noise, sensor fault returns stuck value, sensor dead returns -1

### Stage 3 – State Pattern (~1,500 LOC)
- [ ] Implement `IAircraftState`
- [ ] Implement `Aircraft` with `TransitionTo()` and all delegates
- [ ] Implement all 10 states (Ground, Taxi, TakeOff, Climb, Cruise, Descent, Landing, Holding, Emergency, Critical)
- [ ] Wire `DamageModel.CheckGameOver()` into `CriticalState.Update()`
- [ ] Wire asymmetric drag into `CruiseState.Update()` and `LandingState.Update()`
- [ ] Test full flight: Ground → Taxi → TakeOff → Climb → Cruise → Descent → Landing → Ground

### Stage 4 – Observer Pattern (~700 LOC)
- [ ] Implement all `FlightEvent` subclasses (include new: CascadeTriggered, EngineFire, WingFire, EngineExplosion, AsymmetricDrag, GameOver, SensorFault)
- [ ] Implement `EventBus` (Singleton, thread-safe lock)
- [ ] Implement `IFlightEventHandler`
- [ ] Implement `BlackBoxHandler` (with `PrintReadout()` for game over)
- [ ] Implement `AlertSystemHandler`
- [ ] Implement `FlightLoggerHandler`
- [ ] Implement `StatisticsHandler`
- [ ] Wire events into all states

### Stage 5 – Avionics Systems Full (~900 LOC)
- [ ] Fully implement `EngineSystem` (RPM curves, temp, fire, explode, restart)
- [ ] Fully implement `FuelSystem` (burn, leak, ignition risk, emergency dump)
- [ ] Fully implement `WingSystem` (fire spread, melt, asymmetric drag trigger)
- [ ] Fully implement `HydraulicSystem` (gear, flaps, jam, emergency extension)
- [ ] Fully implement `ElectricalSystem` (buses, backup, APU, de-icing)
- [ ] Fully implement `NavigationSystem` (waypoints, autopilot coupling)
- [ ] Fully implement `AutopilotSystem` (all modes, sensor-coupled altitude hold)

### Stage 6 – Strategy: Anomalies + Cascades (~2,200 LOC)
- [ ] Implement `IAnomaly` and `AbstractAnomaly` (with `TriggerCascade()`)
- [ ] Implement all 12 anomaly classes one by one, test each:
  - `BirdStrikeAnomaly` (with 40% cascade to EngineFire)
  - `EngineFireAnomaly` (with 30% cascade to WingFire, explode at 0 health)
  - `WingFireAnomaly` (melt → asymmetric drag → game over)
  - `EngineFailureAnomaly`
  - `TurbulenceAnomaly` (with sensor noise cascade)
  - `SensorFailureAnomaly` (with autopilot altitude drift)
  - `FuelLeakAnomaly` (with 20% ignition cascade)
  - `HydraulicFailureAnomaly`
  - `ElectricalFailureAnomaly`
  - `DecompressionAnomaly`
  - `IcingAnomaly`
  - `MicroburstAnomaly`
  - `RunwayIncursionAnomaly`
- [ ] Implement `IWeatherStrategy` + all 6 weather classes (with `ApplySensorEffects()`)
- [ ] Implement `AnomalyFactory` + `WeatherFactory`
- [ ] Implement `AnomalyEngine` (with `ForceSpawn()` for cascades)

### Stage 7 – Cascade Handler (~300 LOC)
- [ ] Implement `CascadeHandler` subscribing to all fire/explosion/failure events
- [ ] Implement full cascade table (section 3.1) in `Handle()`
- [ ] Wire `CascadeHandler` into `Aircraft` constructor (auto-subscribe to EventBus)
- [ ] Test cascade chain: BirdStrike → EngineFire → WingFire → AsymmetricDrag → GameOver

### Stage 8 – Command Pattern (~700 LOC)
- [ ] Implement `IFlightCommand`
- [ ] Implement all 8 command classes
- [ ] Implement `CommandHistory` (undo/redo + `SaveToFile()`)

### Stage 9 – View Layer (~1,800 LOC)
- [ ] Implement `IFlightView`
- [ ] Implement all widget classes (including `SensorsPanelWidget`, `FlightMapWidget`, `CockpitWindowWidget`)
- [ ] Implement `ConsoleDashboardView` — **PFD reads from SensorSystem not FlightData**
- [ ] Implement `StartupScreen` (aircraft select with stats, route select, difficulty)
- [ ] Implement `FlightReportView`
- [ ] Implement `BlackBoxReadoutView` (typewriter-style print, game over sequence)

### Stage 10 – Controller + Infrastructure (~1,200 LOC)
- [ ] Implement `InputHandler` with non-blocking `Poll()`
- [ ] Implement `FlightController` with 10 Hz `RunAsync()` loop
- [ ] Implement `AircraftFactory` (all 3 aircraft)
- [ ] Implement `SimulationConfig` (Singleton, JSON load/save with `CascadesEnabled`)
- [ ] Implement `FlightLogger` (4 output files)
- [ ] Implement `FlightReport` with cascade count and game over reason

### Stage 11 – Polish + Tests (~1,000 LOC)
- [ ] Add color coding: fire = red flashing, sensor fault = yellow, game over = full red screen
- [ ] Add blinking `!!` for Critical alerts
- [ ] Add asymmetric drag visible feedback (heading slowly drifts, player must counter)
- [ ] Demo mode (auto-pilot all phases, scripted anomaly sequence for presentation)
- [ ] XML doc comments on all public APIs
- [ ] Minimum 30 unit tests covering: sensor noise, cascade chain, state transitions, damage model
- [ ] Full end-to-end test: complete flight with every cascade triggered

---

## 16. Team Task Split

| Area | Stages | Person |
|---|---|---|
| `FlightData`, `AircraftConfig`, `DamageModel`, all enums | 1 | Person 1 |
| `SensorSystem` + all sensor classes + `ISensor` | 2 | Person 2 |
| State pattern: all 10 states + `Aircraft.TransitionTo()` | 3 | Person 1 + Person 2 |
| Observer: `EventBus`, all events (incl. new fire/cascade), all handlers | 4 | Person 3 |
| Avionics systems: Engine, Fuel, Wing, Hydraulic, Electrical, Nav, Autopilot | 5 | Person 1 + Person 4 |
| Strategy: all 13 anomaly classes + `AbstractAnomaly` + cascade logic | 6a | Person 3 |
| Strategy: 6 weather strategies + `AnomalyEngine` (with `ForceSpawn`) | 6b | Person 2 |
| `CascadeHandler` — full cascade table | 7 | Person 3 |
| Command pattern: all commands + `CommandHistory` | 8 | Person 4 |
| View: all 9 widgets + dashboard + cockpit window + map + startup + blackbox readout | 9 | Person 3 + Person 4 |
| Controller + InputHandler + main loop | 10a | Person 1 |
| Factories + SimulationConfig + FlightLogger + FlightReport | 10b | Person 4 |
| Unit tests + integration + XML docs + demo mode | 11 | All |

---

## 17. LOC Estimate per Module

| Module | Est. LOC |
|---|---|
| Core/Aircraft (Aircraft + FlightData + DamageModel + config) | 700 |
| Core/Aircraft/Systems (8 system classes incl. WingSystem) | 1,100 |
| Core/Sensors (ISensor + Sensor + SensorSystem + 5 typed sensors) | 600 |
| Core/States (10 state classes) | 1,500 |
| Core/Strategies/Anomalies (13 anomalies + AbstractAnomaly) | 1,500 |
| Core/Strategies/Weather (6 strategies) | 600 |
| Core/Events (EventBus + 18 event classes + 5 handlers incl. CascadeHandler) | 900 |
| Core/Commands (8 commands + CommandHistory) | 700 |
| Controllers (FlightController + InputHandler + AnomalyEngine) | 900 |
| Views (9 widgets + dashboard + startup + report + blackbox readout) | 1,800 |
| Infrastructure (config + logger + report + 3 factories) | 900 |
| Unit tests | 1,000 |
| Program.cs + startup glue | 200 |
| **TOTAL** | **~12,400 LOC** |

---

## 18. Grading Checklist

### Required (must have for passing)

- [ ] **State pattern** — min 6 states, correct transitions, zero if/switch in `Aircraft`
- [ ] **Strategy pattern** — min 6 anomalies + 3 weather strategies, all interchangeable
- [ ] **MVC** — Model never imports View. Controller mediates all
- [ ] **Observer** — EventBus with min 3 handlers, events from model
- [ ] **Command** — CommandHistory with undo for min 5 command types
- [ ] **Factory** — AircraftFactory + AnomalyFactory
- [ ] **Singleton** — SimulationConfig or EventBus with lazy init
- [ ] **Polymorphism** — visible in `Update()`, `Trigger()`, `Handle()`, `Read()`
- [ ] **Encapsulation** — no public fields, only controlled properties
- [ ] **Inheritance** — min 2 hierarchies (AbstractAnomaly, FlightEvent)

### Bonus (for grade 5.0)

- [ ] Sensor system — player sees sensor readings not raw data; faults show "---" or stuck value
- [ ] Cascade system — at least 3 working cascade chains (bird→fire→wing→drift→gameover)
- [ ] Wing fire + asymmetric drag — aircraft drifts, player must compensate
- [ ] Cockpit window widget — ASCII horizon with fire, attitude
- [ ] Flight map widget — 2D grid with plane symbol moving
- [ ] Black box readout after game over — timestamped events, dramatic printout
- [ ] Unit test suite (30+ tests)
- [ ] JSON config file
- [ ] CSV telemetry export
- [ ] Demo mode
- [ ] XML doc comments on all public APIs
