using MediatorPattern;

// ============================================================
// MEDIATOR PATTERN — DEMO
// ============================================================
// A smart home hub coordinates 6 devices: motion sensor, two
// lights, security camera, thermostat, and door lock.
// Devices never reference each other — all coordination logic
// lives in SmartHomeHub (the Mediator).
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

// ── Setup ─────────────────────────────────────────────────────
var hub = new SmartHomeHub();

var motion      = new MotionSensor("FrontDoor-Motion", hub);
var livingLight = new SmartLight("LivingRoom-Light");
var hallLight   = new SmartLight("Hallway-Light");
var camera      = new SecurityCamera("FrontDoor-Cam");
var thermostat  = new Thermostat("Main-Thermostat", hub);
var frontLock   = new DoorLock("FrontDoor-Lock", hub);

hub.Register(motion);
hub.Register(livingLight);
hub.Register(hallLight);
hub.Register(camera);
hub.Register(thermostat);
hub.Register(frontLock);

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       MEDIATOR PATTERN — Smart Home Hub              ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("6 devices registered — none know about each other:");
Console.WriteLine("  • FrontDoor-Motion  (MotionSensor)");
Console.WriteLine("  • LivingRoom-Light  (SmartLight)");
Console.WriteLine("  • Hallway-Light     (SmartLight)");
Console.WriteLine("  • FrontDoor-Cam     (SecurityCamera)");
Console.WriteLine("  • Main-Thermostat   (Thermostat)");
Console.WriteLine("  • FrontDoor-Lock    (DoorLock)");
Console.WriteLine();
Console.WriteLine("All coordination lives in SmartHomeHub (the Mediator).");

Pause();

// ── DEMO 1: Motion detected ───────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Motion detected");
Console.WriteLine("         Hub routes: lights ON + camera starts recording");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

motion.DetectMotion();

Console.WriteLine();
Console.WriteLine($"  LivingRoom-Light state : {livingLight.State}");
Console.WriteLine($"  Hallway-Light state    : {hallLight.State}");
Console.WriteLine($"  Camera recording       : {camera.IsRecording}  (trigger: {camera.RecordingSource})");

Pause();

// ── DEMO 2: Motion cleared ────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Motion cleared");
Console.WriteLine("         Hub routes: lights OFF + camera stops recording");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

motion.ClearMotion();

Console.WriteLine();
Console.WriteLine($"  LivingRoom-Light state : {livingLight.State}");
Console.WriteLine($"  Hallway-Light state    : {hallLight.State}");
Console.WriteLine($"  Camera recording       : {camera.IsRecording}");

Pause();

// ── DEMO 3: Temperature change ────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Temperature changes");
Console.WriteLine("         Hub routes: thermostat mode adjusts automatically");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

thermostat.ReportTemperature(30.5);
Console.WriteLine($"  Thermostat mode: {thermostat.Mode}");
Console.WriteLine();

thermostat.ReportTemperature(22.0);
Console.WriteLine($"  Thermostat mode: {thermostat.Mode}");
Console.WriteLine();

thermostat.ReportTemperature(14.0);
Console.WriteLine($"  Thermostat mode: {thermostat.Mode}");

Pause();

// ── DEMO 4: Alarm triggered ───────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Alarm triggered");
Console.WriteLine("         Hub routes: all lights FLASH, camera records,");
Console.WriteLine("                     front door LOCKED");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

frontLock.Unlock(); // start in an unlocked state
Console.WriteLine();
hub.Send(Events.AlarmTriggered, "SecuritySystem");

Console.WriteLine();
Console.WriteLine($"  LivingRoom-Light state : {livingLight.State}");
Console.WriteLine($"  Hallway-Light state    : {hallLight.State}");
Console.WriteLine($"  Camera recording       : {camera.IsRecording}  (trigger: {camera.RecordingSource})");
Console.WriteLine($"  FrontDoor-Lock locked  : {frontLock.IsLocked}");

Pause();

// ── DEMO 5: Alarm cleared ─────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — Alarm cleared");
Console.WriteLine("         Hub routes: lights OFF, camera stops, door unlocks");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

hub.Send(Events.AlarmCleared, "SecuritySystem");

Console.WriteLine();
Console.WriteLine($"  LivingRoom-Light state : {livingLight.State}");
Console.WriteLine($"  Camera recording       : {camera.IsRecording}");
Console.WriteLine($"  FrontDoor-Lock locked  : {frontLock.IsLocked}");

Pause();

// ── DEMO 6: Door opened ───────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 6 — Door interaction");
Console.WriteLine("         Locked door blocks open; unlocked door triggers camera");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

Console.WriteLine("Attempting to open while locked:");
frontLock.Open();

Console.WriteLine();
Console.WriteLine("Unlocking, then opening:");
frontLock.Unlock();
frontLock.Open();

Console.WriteLine();
Console.WriteLine($"  Camera recording: {camera.IsRecording}  (trigger: {camera.RecordingSource})");

Pause();

// ── DEMO 7: Event log ─────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 7 — Full event log");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

for (int i = 0; i < hub.EventLog.Count; i++)
    Console.WriteLine($"  {i + 1,2}. {hub.EventLog[i]}");

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • ISmartHomeHub   — mediator interface (Register + Send)");
Console.WriteLine("  • SmartHomeHub    — coordination logic; routes events to the right devices");
Console.WriteLine("  • ISmartDevice    — common device interface (Name only)");
Console.WriteLine("  • MotionSensor    — publishes motion events via hub");
Console.WriteLine("  • SmartLight      — reacts to motion / alarm events");
Console.WriteLine("  • SecurityCamera  — reacts to motion / door / alarm events");
Console.WriteLine("  • Thermostat      — publishes temperature events; reacts to them");
Console.WriteLine("  • DoorLock        — publishes door events; reacts to alarm events");
Console.WriteLine("  Devices never reference each other — only the hub.");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
