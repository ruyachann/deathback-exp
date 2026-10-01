# Sol independent review — task 002

Reviewer: GPT-5.6 Sol. Read AGENTS.md, task 002, original snapshots, current sources, and installed package implementation. The implementer supplied only a stable-version notification and hashes; no implementation report or other review was read before this conclusion.

## Version

- `Assets/LoopRoom/Scripts/DemoRig.cs`: SHA256 `EB753E6B0685B92A771918BD7756086A486EA140ECFB3532DD164F666F421474`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: SHA256 `449B19BD9694AFA8CB5EAB145A70B46DADBAB41A362404900923E893F172D39F`

Hashes independently confirmed after the implementer declared edits stopped.

## Conclusion

Approved for source review, with no actionable findings in the bounded task. Runtime acceptance remains pending.

- `StartXR` explicitly yields a frame before accessing/initializing the loader, satisfying the installed XR Management `InitializeLoader` requirement to call after Start completes. Input actions are enabled before coroutine launch.
- Origin reference is retained. `XROrigin` is disabled immediately after addition; this does not suppress its initial Awake or OnEnable, but installed Core Utils Awake only establishes references/trackables and OnEnable only registers before-render bookkeeping. Its Start performs camera setup and is deferred while disabled. The requested Floor setter itself invokes camera setup even while disabled; here that setter is deliberately called only after running input subsystems have confirmed Floor. Enabling then permits normal Start setup within preparation.
- Preparation checks running displays/input, supported modes, one direct Floor request per selected input, and actual Floor mode before marking prepared. Readiness rechecks running Floor inputs and rejects other running non-Floor inputs. HeadTracked additionally requires both position and rotation bits; the installed Input System pose driver exposes the action property and uses the same integer mask convention.
- `CanStart` and the guard inside `Begin` block Enter and controller start during initialization or failed Floor preparation. Display activation can enable pose drivers while still preparing, allowing delayed tracking to become available without permitting a session early. `--desktop` retains Enter and the existing Space/E paths; missing settings/failed loader preserve desktop fallback.
- Preparation waits by yielding and has a ten-second bound, with a single diagnostic warning. No per-frame origin request/recenter was introduced. Core Utils may perform its ordinary setup again at first Start, but only after the preparation enables it; that is not a per-loop operation.
- Loop/death handlers remain unchanged. No new origin or offset writes occur in Playing/Blackout. Existing desktop positioning and idle mode switching remain intact.
- Existing active loaders are not started, stopped, or deinitialized by this path. The original `ownsXR` shutdown behavior is retained for loaders this rig initializes and starts.

The preparation window is finite: if input/Floor confirmation arrives only after the ten-second deadline, a fresh Play run is required; if Floor was already prepared and only head tracking was late, readiness can recover through the getter. This is consistent with blocking an unprepared session and reporting the failure, but delayed-device recovery UX could be designed separately.

## Verification limits

Read-only package checks used Core Utils `com.unity.xr.core-utils@a8b900321199/Runtime/XROrigin.cs`, XR Management `com.unity.xr.management@e3a3882b360a/Runtime/XRManagerSettings.cs`, and Input System `com.unity.inputsystem@02433b2481ab/InputSystem/Plugins/XR/TrackedPoseDriver.cs`. These checks establish API/source compatibility evidence, not a successful Unity compile. No Unity Editor execution, PCVR/Floor-height test, controller reach test, three-death test, or three-Play lifecycle test was performed by this reviewer.

## Review exchange

Pending independent Sonnet review availability; no review exchange or Unity runtime acceptance is claimed.
