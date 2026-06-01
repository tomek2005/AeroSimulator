# AeroSim – Full Developer Specification v2 (z paradygmatem funkcyjnym)

> **Course:** Paradygmaty Programowania | **Language:** C# (.NET 8)
> **Target size:** ~10,000–12,000 LOC | **Type:** Real-time console game / flight simulator
> **Design patterns used:** State, Strategy, Observer, Command, MVC, Factory, Singleton
> **Paradigms:** Object-Oriented (primary) + Functional (integrated throughout)

---

## 📌 Nota o paradygmacie funkcyjnym w C#

Projekt łączy **OOP** (struktury klas, wzorce projektowe, hierarchia dziedziczenia) z **programowaniem funkcyjnym** dostępnym natywnie w C# 8–12. Paradygmat funkcyjny **nie zmienia funkcjonalności** — jest stosowany w miejscach, gdzie naturalne jest przetwarzanie danych, transformacje, filtrowanie i niezmienność stanu. Funkcyjność pojawia się głównie przez:

- **`record` / `record struct`** — niemutowalne typy danych (value semantics, `with`-expressions)
- **LINQ** — deklaratywne przetwarzanie kolekcji zamiast pętli imperatiywnych
- **`Func<T>` / `Action<T>` / `Predicate<T>`** — funkcje jako wartości pierwszoklasowe
- **Pipeline / method chaining** — kompozycja transformacji danych
- **Expression-bodied members** — zwięzłe, czyste funkcje
- **Pattern matching** (`switch expression`, `when`, `is`) — zamiast if/switch z efektami ubocznymi
- **`Option`-style** — `T?` nullable reference types jako alternatywa dla null-checków

Wszędzie gdzie zastosowany jest paradygmat funkcyjny, oznaczono sekcję tagiem **`[FP]`**.

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
│   │   ├── FlightDataSnapshot.cs          # [FP] record — niemutowalna kopia
│   │   ├── AircraftConfig.cs              # [FP] record — niemutowalna konfiguracja
│   │   ├── DamageModel.cs
│   │   ├── Systems/
│   │   │   ├── IAvionicSystem.cs
│   │   │   ├── EngineSystem.cs
│   │   │   ├── FuelSystem.cs
│   │   │   ├── NavigationSystem.cs
│   │   │   ├── HydraulicSystem.cs
│   │   │   ├── ElectricalSystem.cs
│   │   │   ├── WeatherSystem.cs
│   │   │   ├── AutopilotSystem.cs
│   │   │   └── WingSystem.cs
│   │   ├── Sensors/
│   │   │   ├── ISensor.cs
│   │   │   ├── Sensor.cs
│   │   │   ├── SensorSystem.cs
│   │   │   ├── AltitudeSensor.cs
│   │   │   ├── AirspeedSensor.cs
│   │   │   ├── EngineSensor.cs
│   │   │   ├── FuelSensor.cs
│   │   │   └── HydraulicSensor.cs
│   │   └── Enums/
│   │       ├── SystemType.cs
│   │       ├── SystemStatus.cs
│   │       ├── Severity.cs
│   │       ├── FireState.cs
│   │       └── SensorState.cs
│   ├── Functional/                        # [FP] dedykowany moduł FP
│   │   ├── Option.cs                      # [FP] Option<T> — bezpieczna alternatywa null
│   │   ├── Result.cs                      # [FP] Result<T,E> — błędy bez wyjątków
│   │   ├── FlightPipeline.cs              # [FP] pipeline transformacji FlightData
│   │   └── AnomalyPredicates.cs           # [FP] predykaty Func<FlightData,bool>
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
│   │   │   ├── EngineFireAnomaly.cs
│   │   │   ├── WingFireAnomaly.cs
│   │   │   ├── HydraulicFailureAnomaly.cs
│   │   │   ├── FuelLeakAnomaly.cs
│   │   │   ├── ElectricalFailureAnomaly.cs
│   │   │   ├── DecompressionAnomaly.cs
│   │   │   ├── TurbulenceAnomaly.cs
│   │   │   ├── IcingAnomaly.cs
│   │   │   ├── RunwayIncursionAnomaly.cs
│   │   │   ├── MicroburstAnomaly.cs
│   │   │   └── SensorFailureAnomaly.cs
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
│   │       ├── CascadeHandler.cs
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
│   ├── StartupScreen.cs
│   ├── FlightReportView.cs
│   ├── BlackBoxReadoutView.cs
│   └── Components/
│       ├── AltimeterWidget.cs
│       ├── AirspeedWidget.cs
│       ├── FuelGaugeWidget.cs
│       ├── SystemsPanelWidget.cs
│       ├── SensorsPanelWidget.cs
│       ├── AlertsBarWidget.cs
│       ├── ActionMenuWidget.cs
│       ├── FlightMapWidget.cs
│       └── CockpitWindowWidget.cs
├── Infrastructure/
│   ├── SimulationConfig.cs
│   ├── FlightLogger.cs
│   ├── FlightReport.cs                    # [FP] record
│   ├── AircraftFactory.cs
│   ├── AnomalyFactory.cs
│   └── WeatherFactory.cs
└── Program.cs
```

---

## 3. Cascade Damage System

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

> **`[FP]` Implementacja cascade table:** zamiast długiego `if/else if` lub `switch`, reguły kaskady są zdefiniowane jako `IReadOnlyList<CascadeRule>` gdzie każda reguła to niemutowalna struktura z predykatem i efektem:
>
> ```csharp
> // [FP] — cascade rules jako dane, nie jako kod proceduralny
> public sealed record CascadeRule(
>     Func<FlightEvent, bool>  Predicate,   // kiedy reguła odpala
>     Action<Aircraft, Random> Effect       // co robi
> );
>
> // Przykład definicji reguły (w CascadeHandler lub AnomalyFactory):
> new CascadeRule(
>     Predicate: evt => evt is EngineFireEvent,
>     Effect:    (aircraft, rng) => {
>         if (rng.NextDouble() < 0.30)
>             aircraft.AnomalyEngine.ForceSpawn(new WingFireAnomaly());
>     }
> )
>
> // Ewaluacja — czysta iteracja bez side-effectów w predykacie:
> _rules.Where(r => r.Predicate(evt))
>       .ToList()
>       .ForEach(r => r.Effect(aircraft, _rng));
> ```

---

### 3.2 `DamageModel.cs`

Central record of the aircraft's current damage state.

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

> **`[FP]` `CheckGameOver()`** — zamiast serii `if`, używa pattern matching jako wyrażenia (expression form):
>
> ```csharp
> // [FP] — switch expression zwraca krotkę (isOver, reason), brak mutacji wewnątrz
> public (bool IsOver, string? Reason) CheckGameOver() => (WingHealth, IsExploded) switch
> {
>     ({ } h, _) when h <= 0.0          => (true,  "Wing structural failure"),
>     (_, true)  when Engine1FireState
>                     == FireState.Melting => (true, "Engine explosion cascade"),
>     _                                  => (false, null)
> };
> ```

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

> **`[FP]` `AllSystems`** — kolekcja jest budowana raz w konstruktorze jako `IReadOnlyList<IAvionicSystem>`. Wszelkie operacje na systemach w `Update()` używają LINQ zamiast pętli `for`:
>
> ```csharp
> // [FP] — deklaratywne, brak pętli imperatywnej
> public void Update(double deltaT)
> {
>     AllSystems.ToList().ForEach(s => s.Update(deltaT, _flightData));
>     // ...
> }
>
> // [FP] — LINQ do filtrowania
> public SystemStatus GetWorstSystemStatus() =>
>     AllSystems.Select(s => s.Status)
>               .OrderByDescending(s => (int)s)
>               .FirstOrDefault();
> ```

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

> **`[FP]` `GetSystemStatus()` i `GetSystemHealth()`** — zamiast switch po enumie, używają `Dictionary<SystemType, IAvionicSystem>` + wyrażenia LINQ:
>
> ```csharp
> // [FP] — lookup jako dane, nie jako switch
> private readonly IReadOnlyDictionary<SystemType, IAvionicSystem> _systemMap;
>
> public SystemStatus GetSystemStatus(SystemType system) =>
>     _systemMap.TryGetValue(system, out var s) ? s.Status : SystemStatus.Failed;
>
> public double GetSystemHealth(SystemType system) =>
>     _systemMap.GetValueOrDefault(system)?.Health ?? 0.0;
> ```

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

> **`[FP]` Metody `IsStalling()`, `IsOverspeed()`, `FuelRemainingPercent()`** — wyrażenia expression-bodied, zero imperatywnych side-effectów:
>
> ```csharp
> // [FP] — expression-bodied, czyste funkcje
> public double FuelRemainingPercent() => FuelLevelKg / FuelCapacityKg * 100.0;
> public double EstimatedRangeKm()    => FuelFlowKgPerH > 0
>     ? (FuelLevelKg / FuelFlowKgPerH) * CruiseSpeedKmPerH
>     : double.PositiveInfinity;
> public bool IsStalling()  => Speed < _config.StallSpeedKts;
> public bool IsOverspeed() => Speed > _config.MaxSpeedKts;
> ```
>
> **`[FP]` `Snapshot()`** — produkuje niemutowalne `FlightDataSnapshot` (record):
>
> ```csharp
> // [FP] — record with-expression (wartościowa kopia)
> public FlightDataSnapshot Snapshot() => new(
>     Altitude, Speed, VerticalSpeed, Heading,
>     Engine1RPM, Engine2RPM, FuelLevelKg, GForce,
>     PitchAngleDeg, RollAngleDeg, FlightTime
> );
> ```

---

### 4.3 `AircraftConfig.cs`

```csharp
// [FP] — record: niemutowalna konfiguracja, value semantics, with-expression, init-only
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
// [FP] Użycie with-expression do testowania wariantów:
// var heavyConfig = baseConfig with { MaxFuelKg = 30000, WingStrength = 0.95 };
```

> **`[FP]` Uwaga:** `record` w C# zapewnia strukturalną równość, niemutowalność przez `init`, oraz `with`-expressions. To czysty wzorzec funkcyjny — dane bez zachowania.

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

### 4.5 `WingSystem.cs`

| Method / Property | Parameters | Returns | Description |
|---|---|---|---|
| `Health` | — | `double` | 1.0 = intact, 0.0 = gone |
| `FireState` | — | `FireState` | None / Burning / Spreading / Melting |
| `StartFire()` | — | `void` | Sets `FireState = Burning`, starts decay timer |
| `Update(double dt, DamageModel dm)` | dt + damage model | `void` | Advances fire spread, reduces health. At Health=0.5 → sets `dm.AsymmetricDragActive=true`. At Health=0 → sets `dm.IsGameOver=true` |
| `ApplyDamage(double severity)` | 0.0–1.0 | `void` | Direct structural damage |
| `ExtinguishFire()` | — | `bool` | Attempts fire suppression. Returns false if Health < 0.4 |
| `IsOnFire` | — | `bool` | `FireState != None` |

> **`[FP]` `Update()` progi zdrowia** — zamiast `if`-łańcucha, używa tablicy progów jako danych:
>
> ```csharp
> // [FP] — progi jako niemutowalna lista reguł (Func<double,bool>, Action)
> private static readonly IReadOnlyList<(Func<double, bool> When, Action<DamageModel> Apply)>
>     _healthThresholds = new[]
>     {
>         (When: (double h) => h <= 0.50, Apply: (DamageModel dm) => dm.AsymmetricDragActive = true),
>         (When: (double h) => h <= 0.20, Apply: (DamageModel dm) => { /* electrical damage */ }),
>         (When: (double h) => h <= 0.00, Apply: (DamageModel dm) => dm.IsGameOver = true),
>     };
>
> // W Update():
> _healthThresholds
>     .Where(t => t.When(Health))
>     .ToList()
>     .ForEach(t => t.Apply(dm));
> ```

---

### 4.6 `EngineSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Start()` | — | `bool` | Engine start attempt |
| `Stop()` | — | `void` | Cuts fuel, RPM decays to 0 |
| `Restart()` | — | `bool` | In-flight restart |
| `StartFire()` | — | `void` | Sets engine on fire |
| `ExtinguishFire()` | — | `bool` | Fire suppression |
| `Explode()` | — | `void` | Called when Health reaches 0 while on fire |
| `Update(double dt, FlightData data)` | dt + telemetry | `void` | Advances RPM, temp, fire if active |
| `CalculateThrust(double throttle)` | 0.0–1.0 | `double` | Thrust in kN, scaled by Health |
| `IsOverheating` | — | `bool` | `TempC > DangerTempC` |
| `IsOnFire` | — | `bool` | Fire state flag |
| `IsExploded` | — | `bool` | Post-explosion flag |
| `ThrustKN` | — | `double` | Current thrust output |

> **`[FP]` `Start()` / `Restart()`** — zamiast warunków rozrzuconych po metodach, warunki wstępne zebrane jako lista predykatów:
>
> ```csharp
> // [FP] — guard predicates jako lista Func<bool>
> private IEnumerable<(Func<bool> Guard, string Reason)> StartGuards(bool inFlight) =>
> [
>     (() => Health >= 0.2,             "Engine too damaged to start"),
>     (() => _fuel.FuelLevelKg > 0,     "No fuel"),
>     (() => !inFlight || Altitude < 30000, "Too high for restart"),
> ];
>
> public bool Start()
> {
>     var failed = StartGuards(inFlight: false).FirstOrDefault(g => !g.Guard());
>     if (failed != default) { PublishAlert(failed.Reason); return false; }
>     // ... uruchamianie silnika
>     return true;
> }
> ```
>
> **`[FP]` `CalculateThrust()`** — czysta funkcja bez side-effectów:
>
> ```csharp
> // [FP] — czysta funkcja (pure function)
> public double CalculateThrust(double throttle) =>
>     _config.MaxThrustKN * throttle * Health * (IsOnFire ? 0.6 : 1.0);
> ```

---

### 4.7 `FuelSystem.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Refuel(double kg)` | kg to add | `void` | Adds fuel, clamps to MaxFuelKg |
| `Burn(double kgPerH, double dt)` | burn rate + time | `void` | Reduces FuelLevelKg |
| `StartLeak(double rateKgPerH)` | leak rate | `void` | Activates leak |
| `SealLeak()` | — | `bool` | Attempts seal |
| `EmergencyDump()` | — | `void` | Dumps fuel rapidly |
| `CheckIgnitionRisk()` | — | `bool` | Returns true if leak + ignition conditions met |
| `LeakRate` | — | `double` | Current kg/h leak (0 if none) |
| `IsLeaking` | — | `bool` | Active leak flag |

> **`[FP]` `CheckIgnitionRisk()`** — czysta funkcja, wynik zależy tylko od stanu (bez side-effectów):
>
> ```csharp
> // [FP] — czysta funkcja sprawdzająca warunki; nie mutuje niczego
> public bool CheckIgnitionRisk() =>
>     IsLeaking && LeakRate > 150 && _flightData.Engine1TempC > 400;
> ```

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
| `AddNoise(double amount)` | noise factor | `void` | Temporarily adds noise |
| `Kill()` | — | `void` | Sets State = Dead |
| `Repair()` | — | `void` | Resets to OK state |

---

### 5.2 `Sensor.cs` (base implementation)

```csharp
public class Sensor : ISensor
{
    private readonly Random _rng = new();
    private double _noiseBoost = 0;

    public string      SensorName { get; }
    public SensorState State      { get; private set; } = SensorState.OK;
    public double      Accuracy   { get; private set; } = 1.0;

    // [FP] — Read() jako czysta transformacja: realValue -> reading
    // (jedyna "nieczystość" to _rng.NextDouble() i _lastReading cache)
    public double Read(double realValue)
    {
        if (State == SensorState.Dead)  return -1;
        if (State == SensorState.Fault) return _lastReading;

        double totalNoise = (1.0 - Accuracy) + _noiseBoost;
        double noise = (_rng.NextDouble() - 0.5) * 2 * totalNoise * realValue * 0.15;
        _lastReading = realValue + noise;
        return _lastReading;
    }

    // [FP] — ApplyDamage jako transformacja stanu przez pattern matching
    public void ApplyDamage(double severity)
    {
        Accuracy = Math.Max(0, Accuracy - severity);
        // [FP] — switch expression do wyznaczenia nowego State
        State = Accuracy switch
        {
            <= 0    => SensorState.Dead,
            < 0.3   => SensorState.Fault,
            < 0.7   => SensorState.Noisy,
            _       => SensorState.OK
        };
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

Holds all sensor instances. The **view reads from sensors, not from FlightData directly.**

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
| `GetReading(string sensorName)` | name | `double` | Returns cached sensor reading |
| `AddNoiseToAll(double amount)` | noise factor | `void` | Used by turbulence cascade |
| `DamageRandomSensor()` | — | `ISensor` | Picks a random sensor, applies damage |
| `GetFaultySensors()` | — | `IReadOnlyList<ISensor>` | Sensors in Fault or Dead state |

> **`[FP]` `GetFaultySensors()` i `AddNoiseToAll()`** — LINQ zamiast pętli:
>
> ```csharp
> // [FP] — filtrowanie deklaratywne
> public IReadOnlyList<ISensor> GetFaultySensors() =>
>     GetAllSensors()
>         .Where(s => s.State is SensorState.Fault or SensorState.Dead)
>         .ToList()
>         .AsReadOnly();
>
> // [FP] — imperatywny efekt uboczny, ale selekcja jest deklaratywna
> public void AddNoiseToAll(double amount) =>
>     GetAllSensors().ToList().ForEach(s => s.AddNoise(amount));
>
> // [FP] — losowy wybór przez LINQ
> public ISensor DamageRandomSensor()
> {
>     var target = GetAllSensors()
>         .Where(s => s.State != SensorState.Dead)
>         .OrderBy(_ => _rng.NextDouble())
>         .First();
>     target.ApplyDamage(_rng.NextDouble() * 0.3 + 0.5);
>     return target;
> }
> ```

---

### 5.4 Sensor state display (for SensorsPanelWidget)

| State | Display color | Reading shown |
|---|---|---|
| OK | Green | Normal value |
| Noisy | Yellow | Value with visible jitter |
| Fault | Red | "FAULT" label + last stuck value |
| Dead | Dark Red | "---" |

> **`[FP]` Mapowanie stanu na wyświetlanie** — czyste funkcje bez if/else:
>
> ```csharp
> // [FP] — switch expression jako czysta funkcja State -> (Color, Format)
> private static (ConsoleColor Color, string Format) GetSensorDisplay(SensorState state) =>
>     state switch
>     {
>         SensorState.OK    => (ConsoleColor.Green,   "{0:F0}"),
>         SensorState.Noisy => (ConsoleColor.Yellow,  "~{0:F0}"),
>         SensorState.Fault => (ConsoleColor.Red,     "FAULT"),
>         SensorState.Dead  => (ConsoleColor.DarkRed, "---"),
>         _                 => (ConsoleColor.Gray,    "???")
>     };
> ```

---

## 6. Module: Core/States – Pattern: State

State pattern: `Aircraft` holds one `IAircraftState`. All behavior delegates to it. No `if/switch` in `Aircraft`.

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

> **`[FP]` `AllowedActions`** — każdy stan eksponuje swoje akcje jako niemutowalna lista wyrażona inline:
>
> ```csharp
> // [FP] — init-only, niemutowalna lista zdefiniowana przy deklaracji
> public IReadOnlyList<string> AllowedActions { get; } =
>     ["[W/S] Throttle", "[A/D] Heading", "[Q] Autopilot", "[SPACE] Next phase"];
> ```

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

> **`[FP]` `TakeOff()` warunki startowe** — guard checks jako pipeline wyrażeń:
>
> ```csharp
> // [FP] — warunki startowe jako IEnumerable<(bool ok, string alert)>
> private static IEnumerable<(bool Ok, string Alert)> TakeOffChecks(Aircraft ctx) =>
> [
>     (ctx.FlightData.FuelRemainingPercent() > 10, "ABORT: Fuel below 10%"),
>     (ctx.GetSystemHealth(SystemType.Engine1) > 0.3, "ABORT: Engine 1 too damaged"),
>     (ctx.GetSystemHealth(SystemType.Engine2) > 0.3, "ABORT: Engine 2 too damaged"),
> ];
>
> public void TakeOff(Aircraft ctx)
> {
>     var failed = TakeOffChecks(ctx).FirstOrDefault(c => !c.Ok);
>     if (failed != default) { ctx.Publish(new AlertEvent(failed.Alert)); return; }
>     ctx.TransitionTo(new TaxiState());
> }
> ```

---

#### `TakeOffState.cs`

| Method | Behavior |
|---|---|
| `OnEnter(ctx)` | Throttle=1.0, arm auto-rotate at VR |
| `Update(ctx, dt)` | Increase Speed from thrust; at VR → Pitch +7.5°; at V2 → climb; at 1500ft → `TransitionTo(ClimbState)` |
| `Abort(ctx)` | If Speed < V1 → cut throttle, brake, `TransitionTo(GroundState)` |
| `HandleEmergency(ctx)` | If Speed > V1 → continue to `EmergencyState`. If < V1 → `Abort()` |

> **`[FP]` `Update()` fazy startu** — sekwencja milestones jako lista warunków + akcji:
>
> ```csharp
> // [FP] — fazy startu jako niemutowalna lista (predykat → akcja)
> private static readonly IReadOnlyList<(Func<TakeOffState, Aircraft, bool> When,
>                                        Action<TakeOffState, Aircraft> Then)>
>     _phases =
>     [
>         (s => !s.HasRotated && s._speed >= s.VRSpeed,
>          (s, ctx) => { s.HasRotated = true; ctx.FlightData.PitchAngleDeg = 7.5; }),
>
>         (s => s.HasRotated && s._speed >= s.V2Speed,
>          (s, ctx) => ctx.TransitionTo(new ClimbState())),
>     ];
> ```

---

#### `ClimbState.cs` / `CruiseState.cs` / `DescentState.cs` / `LandingState.cs` / `HoldingState.cs` / `EmergencyState.cs` / `CriticalState.cs`

Implementacja analogiczna do powyższych — metody `Update()` i warunki przejść używają wyrażeń i LINQ tam gdzie to naturalne. Szczegóły zachowań bez zmian względem oryginalnej specyfikacji.

> **`[FP]` `CruiseState.Update()`** — sprawdzenie paliwa przez wyrażenie zamiast if:
>
> ```csharp
> // [FP] — wyrażenie warunkowe zamiast if-block
> public void Update(Aircraft ctx, double dt)
> {
>     // ...
>     if (ctx.FlightData.FuelRemainingPercent() < 5)
>         ctx.DeclareEmergency();  // efekt uboczny — celowy
> }
> ```
>
> **`[FP]` `CriticalState.Update()`** — game over check przez wyrażenie:
>
> ```csharp
> // [FP] — destructuring tuple z CheckGameOver()
> var (isOver, reason) = ctx.DamageModel.CheckGameOver();
> if (isOver) ctx.Publish(new GameOverEvent(reason!));
> ```

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
| `Resolve(Aircraft ctx)` | aircraft | `bool` | Player attempts fix |
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

    // [FP] — pomocnicze czyste metody jako expression-bodied
    protected bool CheckProbability(double deltaT) =>
        _rng.NextDouble() < Probability * deltaT;

    protected void TriggerCascade(Aircraft ctx, IAnomaly cascade) =>
        ctx.AnomalyEngine.ForceSpawn(cascade);

    protected void PublishAlert(Aircraft ctx, string msg, Severity level) =>
        ctx.Publish(new AlertEvent(msg, level));
}
```

---

### 7.3 Anomaly Implementations

> **`[FP]` Ogólna zasada dla wszystkich anomalii:** spawn conditions i efekty zdefiniowane jako wyrażenia (`=>`) zamiast bloków `{}` tam gdzie nie ma efektów ubocznych.

#### `BirdStrikeAnomaly.cs`

| Method | Behavior |
|---|---|
| `Trigger(ctx, data)` | Picks random engine. `ApplyDamage(SystemType.Engine, 0.3)`. GForce spike +0.5g. Publishes `SystemFailureEvent`. **Cascade roll: 40% → calls `TriggerCascade(ctx, new EngineFireAnomaly())`** |
| `Update(ctx, data)` | GForce oscillates ±0.15g while active. If engine health < 0.5 → `ctx.Sensors.Engine1RPM.ApplyDamage(0.4)` |
| `Resolve(ctx)` | `CanBeResolved = false`. Auto-sets `IsActive = false` after 10 seconds |

> **`[FP]` losowy wybór silnika:**
>
> ```csharp
> // [FP] — wybór przez wyrażenie zamiast if/switch
> private SystemType PickEngine() =>
>     _rng.NextDouble() < 0.5 ? SystemType.Engine1 : SystemType.Engine2;
> ```

---

#### `EngineFireAnomaly.cs`

| Method | Behavior |
|---|---|
| `Trigger(ctx, data)` | Calls `EngineSystem.StartFire()`. Publishes `EngineFireEvent`. Sets fire decay: engine health -3%/sec |
| `Update(ctx, data)` | Every 10 seconds: roll 30% chance → `TriggerCascade(ctx, new WingFireAnomaly())`. Engine health decays. If engine health reaches 0 → calls `EngineSystem.Explode()` |
| `Resolve(ctx)` | Calls `EngineSystem.ExtinguishFire()`. Returns result |

---

#### `WingFireAnomaly.cs`

| Method | Behavior |
|---|---|
| `Trigger(ctx, data)` | Calls `WingSystem.StartFire()`. Publishes `WingFireEvent`. Begins wing health decay |
| `Update(ctx, data)` | Wing health decays. Thresholds trigger cascades (see WingSystem section) |
| `Resolve(ctx)` | Calls `WingSystem.ExtinguishFire()`. Returns false if wing health < 40% |

---

#### Pozostałe anomalie

`EngineFailureAnomaly`, `TurbulenceAnomaly`, `SensorFailureAnomaly`, `FuelLeakAnomaly`, `HydraulicFailureAnomaly`, `ElectricalFailureAnomaly`, `DecompressionAnomaly`, `IcingAnomaly`, `MicroburstAnomaly`, `RunwayIncursionAnomaly` — zachowania bez zmian względem oryginalnej specyfikacji.

> **`[FP]` `TurbulenceAnomaly.Resolve()`** — warunek auto-resolve jako wyrażenie:
>
> ```csharp
> // [FP] — auto-resolve przez wyrażenie, bez if
> public bool Resolve(Aircraft ctx)
> {
>     ctx.Sensors.ClearNoise();
>     _isActive = false;
>     return true;
> }
>
> // w Update() — auto-resolve po czasie:
> if (_activeDuration > _rng.NextDouble() * 300 + 180)
>     Resolve(ctx);
> ```

---

### 7.4 Weather Strategies

`IWeatherStrategy` z dodaną metodą:

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `ApplySensorEffects(SensorSystem sensors)` | sensor system | `void` | Weather-specific sensor interference |

| Class | Sensor effect |
|---|---|
| `ClearSkiesStrategy` | No sensor effects |
| `ThunderstormStrategy` | All sensors +0.1 noise. Lightning: 5% chance/min → random sensor `ApplyDamage(0.5)` |
| `FogStrategy` | No sensor effects |
| `CrosswindStrategy` | No sensor effects |
| `IcingConditionsStrategy` | Airspeed sensor +0.2 noise |
| `WindShearStrategy` | Altitude and airspeed sensors +0.15 noise during shear |

> **`[FP]` `ApplySensorEffects()`** — efekty pogodowe jako słownik `Dictionary<WeatherType, Action<SensorSystem>>` zamiast hierarchii klas (alternatywa dla pełnego OOP):
>
> ```csharp
> // [FP] — opcjonalna implementacja: efekty jako Func/Action zamiast override
> // (można zastosować zamiast lub obok dziedziczenia)
> private static readonly Dictionary<WeatherType, Action<SensorSystem, Random>> _sensorEffects =
>     new()
>     {
>         [WeatherType.Thunderstorm]    = (s, rng) => s.AddNoiseToAll(0.1),
>         [WeatherType.IcingConditions] = (s, rng) => s.Airspeed.AddNoise(0.2),
>         [WeatherType.WindShear]       = (s, rng) => { s.Altitude.AddNoise(0.15); s.Airspeed.AddNoise(0.15); },
>         [WeatherType.Clear]           = (s, rng) => { /* no-op */ },
>     };
> ```

---

## 8. Module: Core/Events – Pattern: Observer

### 8.1 `FlightEvent.cs` (abstract base)

```csharp
// [FP] — abstract record: hierarchia niemutowalnych eventów
public abstract record FlightEvent
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string   Source    { get; init; }
    public Severity Level     { get; init; }
    public string   Message   { get; init; }
}
```

> **`[FP]` Wszystkie klasy eventów jako `record`** — niemutowalne, value-equality, with-expression:
>
> ```csharp
> // [FP] — konkretne eventy jako record dziedziczące po FlightEvent (abstract record)
> public sealed record StateChangedEvent(string OldState, string NewState)
>     : FlightEvent;
>
> public sealed record EngineFireEvent(int EngineNumber)
>     : FlightEvent;
>
> public sealed record CascadeTriggeredEvent(string Source, string Target)
>     : FlightEvent;
>
> public sealed record GameOverEvent(string Reason)
>     : FlightEvent;
>
> public sealed record SensorFaultEvent(string SensorName, SensorState State)
>     : FlightEvent;
> ```

#### Wszystkie klasy eventów

| Class | Extra Fields |
|---|---|
| `AltitudeChangedEvent` | `double NewAltitude, OldAltitude` |
| `StateChangedEvent` | `string OldState, NewState` |
| `AnomalyTriggeredEvent` | `IAnomaly Anomaly` |
| `AnomalyResolvedEvent` | `IAnomaly Anomaly, bool Success` |
| `CascadeTriggeredEvent` | `string Source, string Target` |
| `EngineFireEvent` | `int EngineNumber` |
| `WingFireEvent` | `string Side` |
| `EngineExplosionEvent` | `int EngineNumber` |
| `AsymmetricDragEvent` | `string DamagedSide, double DriftRate` |
| `GameOverEvent` | `string Reason` |
| `SystemFailureEvent` | `SystemType System, double Health` |
| `SensorFaultEvent` | `string SensorName, SensorState State` |
| `FuelLowEvent` | `double RemainingPercent` |
| `FuelCriticalEvent` | `double RemainingPercent` |
| `WeatherChangedEvent` | `IWeatherStrategy NewWeather` |
| `LandingCompletedEvent` | `double TouchdownSpeedKts, bool Successful` |
| `MaydayEvent` | `string Reason, EmergencyType Type` |
| `CommandExecutedEvent` | `string CommandName, string Details` |
| `PlayerInputEvent` | `PlayerAction Action, ConsoleKey Key` |
| `TelemetryTickEvent` | `FlightDataSnapshot Snapshot` |

---

### 8.2 `EventBus.cs` (Singleton)

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Instance` (static) | — | `EventBus` | Lazy singleton |
| `Subscribe<T>(IFlightEventHandler h)` | handler | `void` | Register for event type T |
| `Unsubscribe<T>(IFlightEventHandler h)` | handler | `void` | Unregister |
| `Publish<T>(T evt)` | event | `void` | Dispatch to all handlers |
| `History` | — | `IReadOnlyList<FlightEvent>` | Full event log |
| `ClearHistory()` | — | `void` | Reset log |

> **`[FP]` `Publish<T>()`** — dispatch przez LINQ:
>
> ```csharp
> // [FP] — dispatch deklaratywny: filtruj + wywołaj
> public void Publish<T>(T evt) where T : FlightEvent
> {
>     _handlers
>         .Where(h => h.Key.IsAssignableFrom(typeof(T)))
>         .SelectMany(h => h.Value)
>         .ToList()
>         .ForEach(h => h.Handle(evt));
>
>     _history.Add(evt);   // efekt uboczny — celowy
> }
> ```

---

### 8.3 Handler Classes

#### `CascadeHandler.cs`

| Method | Description |
|---|---|
| `CascadeHandler(AnomalyEngine engine)` | Wstrzykuje AnomalyEngine, buduje listę reguł |
| `Handle(FlightEvent evt)` | Iteruje przez reguły, odpala pasujące efekty |

```csharp
// [FP] — reguły kaskady jako niemutowalna lista rekordów danych
private readonly IReadOnlyList<CascadeRule> _rules;

// [FP] — CascadeRule jako record z predykatem i efektem
public sealed record CascadeRule(
    Func<FlightEvent, bool>  Predicate,
    Action<Aircraft, Random> Effect
);

public void Handle(FlightEvent evt)
{
    // [FP] — pipeline: filtruj pasujące reguły → odpal każdą
    _rules.Where(r => r.Predicate(evt))
          .ToList()
          .ForEach(r => r.Effect(_aircraft, _rng));
}
```

#### `BlackBoxHandler.cs` / `AlertSystemHandler.cs` / `FlightLoggerHandler.cs` / `StatisticsHandler.cs`

Bez zmian funkcjonalnych.

> **`[FP]` `AlertSystemHandler.GetActiveAlerts()`** — LINQ do pobrania N ostatnich:
>
> ```csharp
> // [FP] — deklaratywne pobranie ostatnich 3 alertów
> public IReadOnlyList<string> GetActiveAlerts() =>
>     _alerts.TakeLast(3).ToList().AsReadOnly();
> ```

---

## 9. Module: Core/Commands – Pattern: Command

### 9.1 `IFlightCommand.cs`

| Method / Property | Returns | Description |
|---|---|---|
| `CommandName` | `string` | Human-readable name |
| `ExecutedAt` | `DateTime` | Timestamp |
| `CanUndo` | `bool` | Reversible flag |
| `Execute()` | `void` | Apply command |
| `Undo()` | `void` | Reverse command |
| `GetDescription()` | `string` | One-line description for black box |

---

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

> **`[FP]` Komendy przechowują poprzednią wartość jako niemutowalne pole** — czyste undo:
>
> ```csharp
> // [FP] — zapis poprzedniej wartości w konstruktorze (immutable capture)
> public sealed class SetThrottleCommand : IFlightCommand
> {
>     private readonly Aircraft _aircraft;
>     private readonly double   _newValue;
>     private readonly double   _previousValue;   // [FP] przechwycona przy tworzeniu
>
>     public SetThrottleCommand(Aircraft a, double newVal)
>     {
>         _aircraft      = a;
>         _newValue      = newVal;
>         _previousValue = a.FlightData.Throttle;   // capture current state
>     }
>
>     public void Execute() => _aircraft.FlightData.Throttle = _newValue;
>     public void Undo()    => _aircraft.FlightData.Throttle = _previousValue;
>     public bool CanUndo   => true;
>     public string GetDescription() =>
>         $"Throttle: {_previousValue:P0} → {_newValue:P0}";
> }
> ```

---

### 9.3 `CommandHistory.cs`

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Execute(IFlightCommand cmd)` | command | `void` | Run, push to undo stack, publish event |
| `Undo()` | — | `bool` | Pop from undo, reverse, push to redo |
| `Redo()` | — | `bool` | Pop from redo, re-execute |
| `GetAll()` | — | `IReadOnlyList<IFlightCommand>` | Full command list |
| `SaveToFile(string path)` | path | `void` | Export history |
| `Clear()` | — | `void` | Reset all stacks |

> **`[FP]` `GetAll()`** — łączy undo + redo stack przez LINQ:
>
> ```csharp
> // [FP] — konkatenacja kolekcji jako wyrażenie
> public IReadOnlyList<IFlightCommand> GetAll() =>
>     _executed.Concat(_redoStack).OrderBy(c => c.ExecutedAt).ToList().AsReadOnly();
> ```

---

## 10. Module: Controllers – MVC Controller

### 10.1 `FlightController.cs`

| Method | Description |
|---|---|
| `FlightController(Aircraft a, IFlightView v, SimulationConfig cfg)` | Wire all components |
| `RunAsync(CancellationToken ct)` | Main 10 Hz loop |
| `SetThrottle(double v)` | Create + execute `SetThrottleCommand` |
| `AdjustThrottle(double delta)` | Throttle ± delta, clamp 0–1 |
| `SetHeading(double deg)` | Create + execute `SetHeadingCommand` |
| `AdjustHeading(double delta)` | Heading ± delta |
| `SetTargetAltitude(double feet)` | Create + execute `SetAltitudeCommand` |
| `ToggleAutopilot()` | Create + execute `ToggleAutopilotCommand` |
| `ExecuteTakeOff()` | `aircraft.TakeOff()` via command |
| `ExecuteLand()` | `aircraft.Land()` via command |
| `ExecuteEmergency()` | `EmergencyDeclareCommand` |
| `ExecuteGoAround()` | `GoAroundCommand` |
| `ResolveTopAnomaly()` | Get `AnomalyEngine.MostCritical`, create `ResolveAnomalyCommand` |
| `UndoLastCommand()` | `CommandHistory.Undo()` |
| `GetFlightReport()` | Build + return report |

> **`[FP]` Mapowanie inputu na komendy jako `Dictionary<PlayerAction, Func<IFlightCommand>>`** — zamiast switch w `Execute()`:
>
> ```csharp
> // [FP] — dispatch tabela: PlayerAction → fabryka komendy
> private readonly IReadOnlyDictionary<PlayerAction, Func<IFlightCommand>> _commandMap;
>
> public FlightController(Aircraft a, IFlightView v, SimulationConfig cfg)
> {
>     // [FP] — inicjalizacja jako wyrażenie słownikowe
>     _commandMap = new Dictionary<PlayerAction, Func<IFlightCommand>>
>     {
>         [PlayerAction.IncreaseThrottle] = () => new SetThrottleCommand(a, a.FlightData.Throttle + 0.05),
>         [PlayerAction.DecreaseThrottle] = () => new SetThrottleCommand(a, a.FlightData.Throttle - 0.05),
>         [PlayerAction.TurnLeft]         = () => new SetHeadingCommand(a, a.FlightData.TargetHeading - 5),
>         [PlayerAction.TurnRight]        = () => new SetHeadingCommand(a, a.FlightData.TargetHeading + 5),
>         [PlayerAction.ToggleAutopilot]  = () => new ToggleAutopilotCommand(a),
>         [PlayerAction.DeclareEmergency] = () => new EmergencyDeclareCommand(a),
>         [PlayerAction.ResolveAnomaly]   = () => new ResolveAnomalyCommand(a, _anomalyEngine.MostCritical!),
>         [PlayerAction.GoAround]         = () => new GoAroundCommand(a),
>     };
> }
>
> public void ExecuteCommand(PlayerAction action)
> {
>     // [FP] — lookup + optional chaining zamiast switch
>     if (_commandMap.TryGetValue(action, out var factory))
>         _commandHistory.Execute(factory());
> }
> ```

---

### 10.2 `InputHandler.cs`

| Method | Description |
|---|---|
| `Poll()` | Non-blocking. Returns `PlayerAction?` |
| `SetKeyMap(Dictionary<ConsoleKey, PlayerAction> map)` | Override default bindings |
| `GetKeyMap()` | Current bindings |

**Default key map** — bez zmian względem oryginalnej specyfikacji.

> **`[FP]` `Poll()`** — tłumaczenie klawisza jako wyrażenie z nullable:
>
> ```csharp
> // [FP] — nullable return zamiast null-object pattern
> public PlayerAction? Poll() =>
>     Console.KeyAvailable
>         ? _keyMap.TryGetValue(Console.ReadKey(true).Key, out var action)
>             ? action
>             : null
>         : null;
> ```

---

### 10.3 `AnomalyEngine.cs`

| Method | Description |
|---|---|
| `AnomalyEngine(Aircraft a, SimulationConfig cfg)` | Initialize pool from `AnomalyFactory` |
| `Tick(double dt)` | `TrySpawnAnomaly()` + `UpdateActiveAnomalies()` |
| `TrySpawnAnomaly(double dt)` | Roll probability per anomaly |
| `UpdateActiveAnomalies(double dt)` | Call `Update()` on each active anomaly |
| `ForceSpawn(IAnomaly anomaly)` | Used by `CascadeHandler` |
| `ResolveAnomaly(string name)` | Find + resolve by name |
| `ActiveAnomalies` | Currently active anomalies |
| `MostCritical` | Highest severity active anomaly |
| `SetDifficulty(Difficulty d)` | Adjust probability multiplier |

> **`[FP]` `MostCritical`** — przez LINQ:
>
> ```csharp
> // [FP] — wyrażenie zamiast loop+variable
> public IAnomaly? MostCritical =>
>     _activeAnomalies
>         .OrderByDescending(a => (int)a.Level)
>         .FirstOrDefault();
> ```
>
> **`[FP]` `TrySpawnAnomaly()`** — warunki spawnowania jako pipeline:
>
> ```csharp
> // [FP] — filtruj kwalifikujące anomalie, wybierz przez prawdopodobieństwo
> private void TrySpawnAnomaly(double dt)
> {
>     if (_timeSinceLastSpawn < 30) return;
>
>     _pool.Where(a => !a.IsActive
>                   && !IsFireAnomaly(a)                         // fire tylko przez cascade
>                   && CheckProbability(a, dt))
>          .Take(1)
>          .ToList()
>          .ForEach(a => { a.Trigger(_aircraft, _aircraft.FlightData); _timeSinceLastSpawn = 0; });
> }
>
> private bool CheckProbability(IAnomaly a, double dt) =>
>     _rng.NextDouble() < a.Probability * dt * _difficultyMultiplier;
> ```

---

## 11. Module: Views – MVC View

**Rule: View NEVER modifies model data. It only reads through public properties.**

### 11.1 `IFlightView.cs`

Bez zmian względem oryginalnej specyfikacji.

---

### 11.2 `ConsoleDashboardView.cs`

| Method | Description |
|---|---|
| `Render(Aircraft a)` | Calls all widgets in order |
| `RenderPFD(SensorSystem s)` | **Reads from SENSORS not FlightData.** Shows sensor readings |
| `RenderSystemsPanel(Aircraft a)` | Grid: ENG1/ENG2/WING/HYD/ELEC/NAV |
| `RenderSensorsPanel(SensorSystem s)` | Grid of all sensors with OK/NOISY/FAULT/DEAD |
| (pozostałe bez zmian) | |

> **`[FP]` Budowanie bufora ramki** — pipeline transformacji linii:
>
> ```csharp
> // [FP] — widok jako pipeline: dane → linie tekstowe → bufor
> public void Render(Aircraft aircraft)
> {
>     var frame = new[]
>     {
>         RenderHeader(aircraft),
>         RenderPFD(aircraft.Sensors),
>         RenderFuelGauge(aircraft.Sensors),
>         RenderSystemsPanel(aircraft),
>         RenderSensorsPanel(aircraft.Sensors),
>         RenderWeatherPanel(aircraft.Weather),
>         RenderAlertsBar(_alertHandler.GetActiveAlerts()),
>         RenderActionMenu(aircraft.CurrentState),
>     }
>     .SelectMany(lines => lines)   // [FP] — flatten
>     .ToArray();
>
>     Console.SetCursorPosition(0, 0);
>     frame.ToList().ForEach(Console.WriteLine);
> }
> ```

---

### 11.3 View Widget Classes

Sygnatury bez zmian. Wszystkie metody `Render()` zwracają `string[]` — są to czyste funkcje (dane wejściowe → linie tekstowe, bez side-effectów).

> **`[FP]` Widgety jako czyste funkcje** — `string[] Render(...)` to transformacje danych. Brak dostępu do stanu globalnego:
>
> ```csharp
> // [FP] — czysta funkcja: sensor data → linie do wyświetlenia
> public static string[] Render(double sensorAlt, SensorState state) =>
>     state switch
>     {
>         SensorState.Dead  => ["| ALT:   --- ft  |"],
>         SensorState.Fault => [$"| ALT: FAULT ft  |"],
>         _                 => [$"| ALT: {sensorAlt,6:F0} ft  |"]
>     };
> ```

---

### 11.4 `CockpitWindowWidget.cs`

Zachowanie bez zmian (opis ASCII horizon, fire chars, tilt przez pitch/roll).

---

### 11.5 `StartupScreen.cs`

| Method | Description |
|---|---|
| `Show()` | Full startup sequence → `StartupSelection` |
| `SelectAircraft()` | Numbered list with stats |
| `SelectRoute()` | Short / Medium / Long |
| `SelectDifficulty()` | Easy / Normal / Hard |
| `ShowAircraftStats(AircraftConfig cfg)` | ASCII stat bars |

> **`[FP]` `ShowAircraftStats()`** — renderowanie pasków statystyk jako mapowanie danych:
>
> ```csharp
> // [FP] — pola config jako lista (nazwa, wartość, max) → paski ASCII
> private static IEnumerable<string> RenderStatBars(AircraftConfig cfg) =>
>     new (string Label, double Value, double Max)[]
>     {
>         ("Fuel", cfg.MaxFuelKg,         30000),
>         ("Speed", cfg.CruiseSpeedKts,   500),
>         ("Alt",  cfg.MaxAltitudeFt,     45000),
>         ("Wing", cfg.WingStrength,      1.0),
>     }
>     .Select(s => $"  {s.Label,-6} [{MakeBar(s.Value, s.Max)}] {s.Value}");
>
> private static string MakeBar(double value, double max, int width = 20) =>
>     new string('#', (int)(value / max * width)).PadRight(width, '.');
> ```

---

### 11.6 `BlackBoxReadoutView.cs`

| Method | Description |
|---|---|
| `Show(events, commands)` | Prints line by line z typewriter effect |
| `PrintHeader()` | Header readout |
| `PrintEventLine(FlightEvent evt)` | Formatowana linia zdarzenia |
| `PrintCommandLine(IFlightCommand cmd)` | Formatowana linia komendy |
| `PrintSummary(DamageModel dm)` | Final state |

> **`[FP]` `Show()` jako pipeline zdarzeń posortowanych czasowo:**
>
> ```csharp
> // [FP] — merge i sort przez LINQ, wyświetlanie jako pipeline
> public void Show(IReadOnlyList<FlightEvent> events, IReadOnlyList<IFlightCommand> commands)
> {
>     var eventLines = events.Select(e  => (e.Timestamp, Line: FormatEvent(e)));
>     var cmdLines   = commands.Select(c => (c.ExecutedAt, Line: FormatCommand(c)));
>
>     eventLines.Concat(cmdLines)        // [FP] — scalenie dwóch strumieni
>               .OrderBy(x => x.Timestamp)
>               .Select(x => x.Line)
>               .ToList()
>               .ForEach(line => { Console.WriteLine(line); Thread.Sleep(40); });
> }
> ```

---

## 12. Module: Infrastructure

### 12.1 `SimulationConfig.cs` (Singleton)

| Property / Method | Description |
|---|---|
| `Instance` (static) | Lazy singleton |
| `SimulationSpeed` | `double` |
| `AnomaliesEnabled` | `bool` |
| `AnomalyFrequency` | `double` |
| `CascadesEnabled` | `bool` |
| `BlackBoxEnabled` | `bool` |
| `LogDirectory` | `string` |
| `LoadFromFile(string path)` | Load JSON |
| `SaveToFile(string path)` | Save JSON |

---

### 12.2 `FlightLogger.cs`

Bez zmian funkcjonalnych.

> **`[FP]` `LogTelemetry()`** — CSV przez LINQ:
>
> ```csharp
> // [FP] — projekcja pól jako sekwencja wartości → join
> public void LogTelemetry(FlightData data)
> {
>     var row = new object[]
>     {
>         data.FlightTime, data.Altitude, data.Speed,
>         data.Heading, data.FuelLevelKg, data.GForce
>     }
>     .Select(v => v.ToString())
>     .Aggregate((a, b) => $"{a},{b}");
>
>     _writer.WriteLine(row);
> }
> ```

---

### 12.3 `FlightReport.cs`

```csharp
// [FP] — record: niemutowalne podsumowanie lotu, value semantics
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
    public int       CascadesTriggered  { get; init; }
    public bool      LandedSafely       { get; init; }
    public double    TouchdownSpeedKts  { get; init; }
    public double    LandingScore       { get; init; }
    public string?   GameOverReason     { get; init; }
    public IReadOnlyList<FlightEvent>    EventLog    { get; init; }
    public IReadOnlyList<IFlightCommand> CommandLog  { get; init; }

    // [FP] — czysta funkcja obliczająca score bez side-effectów
    public double ComputeLandingScore(double speedPenalty, double vsPenalty,
                                       double anomalyPenalty, double crosswindPenalty,
                                       double cascadePenalty) =>
        Math.Max(0, 100 - speedPenalty - vsPenalty - anomalyPenalty
                        - crosswindPenalty - cascadePenalty);

    public void PrintToConsole();
    public void SaveAsText(string path);
}
```

---

### 12.4 `AircraftFactory.cs`

| Method | Returns | Boeing 737-800 values |
|---|---|---|
| `CreateBoeing737()` | `Aircraft` | Fuel=26000kg, MaxAlt=41000ft, Cruise=460kts, V1=148, VR=152, V2=158, WingStrength=0.85 |
| `CreateAirbusA320()` | `Aircraft` | Fuel=18800kg, MaxAlt=39800ft, Cruise=450kts, V1=142, VR=146, V2=152, WingStrength=0.90 |
| `CreateCessna172()` | `Aircraft` | Fuel=212kg, MaxAlt=14000ft, Cruise=122kts, V1=55, VR=60, V2=65, WingStrength=0.70 |
| `Create(AircraftConfig cfg, string tail, string model)` | `Aircraft` | Generic |

> **`[FP]` Konfiguracje samolotów jako niemutowalne dane:**
>
> ```csharp
> // [FP] — config jako static readonly record instances
> private static readonly AircraftConfig Boeing737Config = new()
> {
>     DisplayName = "Boeing 737-800", TailNumber = "SP-LRA",
>     MaxFuelKg = 26000, MaxAltitudeFt = 41000, CruiseSpeedKts = 460,
>     V1SpeedKts = 148,  VRSpeedKts = 152, V2SpeedKts = 158,
>     WingStrength = 0.85,
>     // ...
> };
>
> public Aircraft CreateBoeing737() => new("SP-LRA", "Boeing 737-800", Boeing737Config);
> ```

---

### 12.5 `AnomalyFactory.cs`

| Method | Returns | Description |
|---|---|---|
| `Create(AnomalyType type)` | `IAnomaly` | New instance |
| `CreateRandom(FlightData ctx)` | `IAnomaly` | Weighted random |
| `GetAllFor(IAircraftState state)` | `IReadOnlyList<IAnomaly>` | Valid anomalies |
| `GetPool()` | `IReadOnlyList<IAnomaly>` | Full default pool |
| `CreateCascade(AnomalyType type)` | `IAnomaly` | For cascade injection |

> **`[FP]` `CreateRandom()`** — ważone losowanie przez LINQ:
>
> ```csharp
> // [FP] — filtruj, ważone sortowanie, wybierz pierwszy
> public IAnomaly CreateRandom(FlightData ctx) =>
>     GetPool()
>         .Where(a => IsValidFor(a, ctx))
>         .OrderBy(a => _rng.NextDouble() / a.Probability)
>         .First();
> ```

---

## 13. Gameplay Loop & Input Map

### Complete Game Flow

Bez zmian funkcjonalnych — loop 10 Hz, pełna sekwencja jak w oryginalnej specyfikacji.

> **`[FP]` Główna pętla jako pipeline kroków:**
>
> ```csharp
> // [FP] — główna pętla jako sekwencja kroków (Action) zamiast spaghetti
> private static readonly IReadOnlyList<Action<FlightController, double>> _loopSteps =
> [
>     (fc, dt) => fc.Aircraft.Update(dt),
>     (fc, dt) => fc.SensorSystem.Update(dt, fc.Aircraft.FlightData),
>     (fc, dt) => fc.AnomalyEngine.Tick(dt),
>     (fc, dt) => fc.View.Render(fc.Aircraft),
>     (fc, dt) => fc.HandleInput(),
> ];
>
> // W RunAsync():
> while (!ct.IsCancellationRequested && !aircraft.DamageModel.IsGameOver)
> {
>     _loopSteps.ToList().ForEach(step => step(this, deltaT));
>     Thread.Sleep(100);
> }
> ```

### State Transition Diagram

Bez zmian — pełny diagram jak w oryginalnej specyfikacji.

---

## 14. Black Box & Flight Logging

### Nagrywane zdarzenia — bez zmian

| Event | Details stored |
|---|---|
| `TelemetryTickEvent` (every 1 sec) | Altitude, speed, heading, throttle, RPM ×2, fuel, G-force, pitch, roll, all sensor readings |
| State transition | Old state, new state, timestamp |
| Anomaly triggered | Anomaly name, severity, snapshot |
| Anomaly resolved (success/fail) | Anomaly name, result, snapshot |
| **Cascade triggered** | Source anomaly, target anomaly, timestamp |
| **Sensor fault** | Sensor name, new state, real vs reported value |
| **Fire events** | Engine/wing fire start, spread, explosion |
| **Asymmetric drag activated** | Drift rate, damaged side |
| Player input | Key, action, timestamp |
| Command executed | Name, params, pre/post snapshot |
| System failure | System name, health |
| Landing | Touchdown speed, V/S, crosswind component |
| **Game over** | Reason, final state of all systems |

### Output files — bez zmian

| File | Format |
|---|---|
| `flight_YYYYMMDD_HHmmss.log` | Plain text |
| `blackbox_YYYYMMDD_HHmmss.txt` | Structured text |
| `telemetry_YYYYMMDD_HHmmss.csv` | CSV |
| `report_YYYYMMDD_HHmmss.txt` | Plain text |

---

## 15. Implementation Roadmap

### Stage 1 – Foundation (~600 LOC)
- [ ] Create full directory structure + `Core/Functional/` folder
- [ ] Implement all enums
- [ ] **`[FP]`** Implement `AircraftConfig` as `record` (init-only, with-expression)
- [ ] **`[FP]`** Implement `FlightDataSnapshot` as `record`
- [ ] Implement `FlightData` z expression-bodied methods (`IsStalling()`, `FuelRemainingPercent()` etc.)
- [ ] **`[FP]`** Implement `DamageModel.CheckGameOver()` jako switch expression zwracający tuple
- [ ] Implement `IAvionicSystem` interface
- [ ] Stub all system classes with empty methods
- [ ] Unit tests: `FlightData` calculations, `DamageModel.CheckGameOver()`

### Stage 2 – Sensor System (~500 LOC)
- [ ] Implement `ISensor` interface
- [ ] **`[FP]`** Implement `Sensor.ApplyDamage()` z switch expression na State
- [ ] **`[FP]`** Implement `SensorSystem.GetFaultySensors()`, `AddNoiseToAll()`, `DamageRandomSensor()` przez LINQ
- [ ] Unit tests: sensor noise, sensor fault returns stuck value, dead returns -1

### Stage 3 – State Pattern (~1,500 LOC)
- [ ] Implement `IAircraftState`
- [ ] **`[FP]`** Implement guard checks w stanach jako `IEnumerable<(Func<bool>, string)>` zamiast if-chain
- [ ] **`[FP]`** Implement `AllowedActions` jako init-only listy
- [ ] Implement all 10 states
- [ ] **`[FP]`** Wire `DamageModel.CheckGameOver()` z tuple destructuring w `CriticalState`
- [ ] Test full flight: Ground → ... → Ground

### Stage 4 – Observer Pattern (~700 LOC)
- [ ] **`[FP]`** Implement wszystkie `FlightEvent` jako `record` dziedziczące po `abstract record FlightEvent`
- [ ] Implement `EventBus` z LINQ dispatch
- [ ] Implement wszystkie handler classes

### Stage 5 – Avionics Systems Full (~900 LOC)
- [ ] **`[FP]`** Implement `WingSystem._healthThresholds` jako lista reguł
- [ ] **`[FP]`** Implement `EngineSystem.CalculateThrust()` jako pure function
- [ ] **`[FP]`** Implement `FuelSystem.CheckIgnitionRisk()` jako pure function
- [ ] Fully implement pozostałe systemy

### Stage 6 – Strategy: Anomalies + Cascades (~2,200 LOC)
- [ ] Implement `IAnomaly` i `AbstractAnomaly` z expression-bodied helpers
- [ ] **`[FP]`** Implement spawn conditions w `AnomalyEngine.TrySpawnAnomaly()` przez LINQ pipeline
- [ ] **`[FP]`** Implement `AnomalyEngine.MostCritical` jako LINQ expression
- [ ] Implement wszystkie 13 klas anomalii
- [ ] **`[FP]`** Implement `WeatherStrategy.ApplySensorEffects()` jako dictionary lookup
- [ ] **`[FP]`** Implement `AnomalyFactory.CreateRandom()` przez weighted LINQ

### Stage 7 – Cascade Handler (~300 LOC)
- [ ] **`[FP]`** Implement `CascadeRule` jako `sealed record`
- [ ] **`[FP]`** Implement `CascadeHandler._rules` jako `IReadOnlyList<CascadeRule>` (predykaty + efekty)
- [ ] **`[FP]`** Implement `Handle()` jako LINQ pipeline przez reguły
- [ ] Test cascade chain: BirdStrike → EngineFire → WingFire → AsymmetricDrag → GameOver

### Stage 8 – Command Pattern (~700 LOC)
- [ ] **`[FP]`** Implement komendy z immutable capture poprzedniej wartości w konstruktorze
- [ ] **`[FP]`** Implement `CommandHistory.GetAll()` przez LINQ concat+sort
- [ ] **`[FP]`** Implement dispatch table w `FlightController` jako `Dictionary<PlayerAction, Func<IFlightCommand>>`

### Stage 9 – View Layer (~1,800 LOC)
- [ ] **`[FP]`** Implement widgety jako static metody zwracające `string[]` (czyste funkcje)
- [ ] **`[FP]`** Implement sensor display przez switch expression na `SensorState`
- [ ] **`[FP]`** Implement `ConsoleDashboardView.Render()` jako pipeline przez `SelectMany`
- [ ] **`[FP]`** Implement `BlackBoxReadoutView.Show()` jako merge + sort dwóch strumieni przez LINQ
- [ ] **`[FP]`** Implement `StartupScreen.ShowAircraftStats()` jako projekcja pól config → paski ASCII

### Stage 10 – Controller + Infrastructure (~1,200 LOC)
- [ ] **`[FP]`** Implement `InputHandler.Poll()` jako nullable expression
- [ ] **`[FP]`** Implement pętlę główną jako pipeline kroków `IReadOnlyList<Action<...>>`
- [ ] **`[FP]`** Implement `AircraftFactory` configs jako `static readonly record` instances
- [ ] **`[FP]`** Implement `FlightReport` jako `record`
- [ ] **`[FP]`** Implement `FlightLogger.LogTelemetry()` z LINQ join

### Stage 11 – Polish + Tests (~1,000 LOC)
- [ ] **`[FP]`** Implement `Core/Functional/Option.cs` — `Option<T>` wrapper dla bezpiecznych null-returns
- [ ] **`[FP]`** Implement `Core/Functional/FlightPipeline.cs` — przykładowy pipeline transformacji `FlightData`
- [ ] Dodaj color coding, blinking alerts
- [ ] Demo mode
- [ ] Minimum 30 unit tests (w tym testy czystych funkcji — sensor read, cascade predykaty, landing score)
- [ ] XML doc comments

---

## 16. Team Task Split

| Area | Stages | Person |
|---|---|---|
| `FlightData` (expression-bodied), `AircraftConfig` (record), `DamageModel` (switch expr), enums | 1 | Person 1 |
| `SensorSystem` + sensory (LINQ, switch expr na State) + `ISensor` | 2 | Person 2 |
| State pattern: 10 stanów + guard predicates jako listy + `Aircraft.TransitionTo()` | 3 | Person 1 + Person 2 |
| Observer: `EventBus` (LINQ dispatch), eventy jako `record`, handlery | 4 | Person 3 |
| Avionics systems: Engine (pure thrust fn), Fuel (pure risk fn), Wing (threshold rules), Hyd, Elec, Nav, Autopilot | 5 | Person 1 + Person 4 |
| Strategy: 13 anomalie + `AbstractAnomaly` + cascade logic | 6a | Person 3 |
| Strategy: 6 weather (dict effects) + `AnomalyEngine` (LINQ spawn) | 6b | Person 2 |
| `CascadeHandler` — `CascadeRule` records + LINQ pipeline | 7 | Person 3 |
| Command pattern: komendy z immutable capture + dispatch table w Controller | 8 | Person 4 |
| View: widgety (pure fns) + dashboard pipeline + cockpit + map + startup + blackbox readout | 9 | Person 3 + Person 4 |
| Controller (pipeline loop, dispatch dict) + InputHandler (nullable) | 10a | Person 1 |
| Factories (static record configs) + SimulationConfig + FlightLogger (LINQ CSV) + FlightReport (record) | 10b | Person 4 |
| `Core/Functional/` module + unit tests (w tym testy pure functions) + docs | 11 | All |

---

## 17. LOC Estimate per Module

| Module | Est. LOC |
|---|---|
| Core/Aircraft (Aircraft + FlightData + DamageModel + config) | 700 |
| Core/Aircraft/Systems (8 system classes incl. WingSystem) | 1,100 |
| Core/Sensors (ISensor + Sensor + SensorSystem + 5 typed sensors) | 600 |
| Core/Functional (Option, Result, FlightPipeline, AnomalyPredicates) | **150** |
| Core/States (10 state classes) | 1,500 |
| Core/Strategies/Anomalies (13 anomalies + AbstractAnomaly) | 1,500 |
| Core/Strategies/Weather (6 strategies) | 600 |
| Core/Events (EventBus + 18 event records + 5 handlers incl. CascadeHandler) | 900 |
| Core/Commands (8 commands + CommandHistory) | 700 |
| Controllers (FlightController + InputHandler + AnomalyEngine) | 900 |
| Views (9 widgets + dashboard + startup + report + blackbox readout) | 1,800 |
| Infrastructure (config + logger + report record + 3 factories) | 900 |
| Unit tests | 1,000 |
| Program.cs + startup glue | 200 |
| **TOTAL** | **~12,550 LOC** |

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

### Functional Paradigm (nowe wymaganie prowadzącego)

- [ ] **`record` types** — `AircraftConfig`, `FlightDataSnapshot`, `FlightReport`, wszystkie `FlightEvent` jako `record`
- [ ] **LINQ** — widoczne w min. 5 miejscach: `GetFaultySensors()`, `MostCritical`, `Publish<T>()`, `TrySpawnAnomaly()`, `Show()` w BlackBox
- [ ] **Switch expressions** — `ApplyDamage()` w Sensor, `CheckGameOver()` w DamageModel, wyświetlanie sensorów w widgecie
- [ ] **Pure functions** — `CalculateThrust()`, `CheckIgnitionRisk()`, `FuelRemainingPercent()`, metody widgetów `Render()`
- [ ] **Higher-order functions** — `CascadeRule` z `Func<FlightEvent, bool>` i `Action<Aircraft, Random>`, dispatch table w Controller
- [ ] **Expression-bodied members** — widoczne w min. 10 metodach (czyste transformacje danych)
- [ ] **Immutability** — `IReadOnlyList<>` na publicznych kolekcjach, `init`-only na recordach
- [ ] **`Core/Functional/`** — przynajmniej `Option<T>` lub `CascadeRule` record z demonstracją użycia

### Bonus (for grade 5.0)

- [ ] Sensor system — player sees sensor readings not raw data; faults show "---" or stuck value
- [ ] Cascade system — at least 3 working cascade chains (bird→fire→wing→drift→gameover)
- [ ] Wing fire + asymmetric drag — aircraft drifts, player must compensate
- [ ] Cockpit window widget — ASCII horizon with fire, attitude
- [ ] Flight map widget — 2D grid with plane symbol moving
- [ ] Black box readout after game over — timestamped events, dramatic printout
- [ ] Unit test suite (30+ tests, w tym testy pure functions)
- [ ] JSON config file
- [ ] CSV telemetry export
- [ ] Demo mode
- [ ] XML doc comments on all public APIs
