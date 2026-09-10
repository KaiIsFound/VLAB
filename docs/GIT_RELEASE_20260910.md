# Chemistry Lab — Git release increment 2026-09-10

## Included

- Existing Chemistry Lab work: hands-on titration, mouse grab/use/pour, finite volumes, reset amounts preserving recorded results, pour guide, collision recovery, package XR Device Simulator, floor-only safe teleport and snap 30 degrees.
- New persistent Desktop settings panel: four render presets, sensitivity, master volume and pour-guide visibility. Invalid persisted values fall back or clamp safely.
- Two-voice click feedback pool; runtime source count stays bounded under repeated input.
- Original repository Home/Physics assets retained. Default build scene is ChemistryLab, not the original Home scene. Original README retained with Chemistry instructions prepended in the publishing checkout.

## Verification

- EditMode: 65 discovered/executed/passed, 0 failed, `20260910T125154Z-settings-quality-EditMode/results.xml`.
- PlayMode: 22 discovered/executed/passed, 0 failed, `20260910T125409Z-settings-audio-PlayMode/results.xml`.
- Local test artifacts are under `Artifacts/VLABUpgrade/plan-1.4/DemoDay/`; generated logs and builds are intentionally not tracked.
- Four quality presets tested against actual Unity render settings; save/load and invalid settings tested; audio pool and unchanged lesson state tested.
- Final Windows build: `Builds/DemoDay-20260910-settings-final`, Succeeded, 0 errors/1 known TMP shader warning. Player smoke/fallback and settings-open smoke exit 0 with one camera/listener; startup screenshot captured. Separate hidden-player settings screenshot failed to capture, so visual acceptance of that panel is not claimed from this run.
- Tests run in the working Chemistry project. Original Physics files retained from upstream are not represented as newly tested Physics features.

## Remaining limits

- Hardware VR, haptic, comfort and performance measurements not tested.
- Physical Daniell/electrolysis, world-space SDF/settings, continuous locomotion/vignette and seated calibration remain incomplete.
- One known Built-in/URP TMP shader build warning remains; no renderer migration performed.
- Settings are local to the device; experiment progress is not persisted by this feature.

Use `DEMO_QUICK_START.md` to play. This commit is a tested increment, not completion of the entire P2–P7 roadmap.
