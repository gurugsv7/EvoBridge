# GPT Image design prompts

Tool: built-in `image_gen.imagegen` (no API-key/CLI fallback).

## Shared screen specification

Create one complete front-on high-fidelity desktop application screenshot for EVOBRIDGE. Landscape canvas approximately 1200 × 720. A bespoke industrial controller workbench: graphite `#101315`, charcoal `#1B1F21`, warm off-white `#F2EEE6`, muted gray `#8C9392`, vermilion orange `#FF744A`, and a restrained lime connection indicator. Thin rules, small uppercase monospace labels, strong editorial titles, compact practical spacing. No surrounding desktop, perspective, neon cyberpunk, purple gradients, rounded card grids, stock SaaS layouts or templates. Keep every control visible.

Use a 160–172 px left rail with linked-bars icon, EVOBRIDGE wordmark, Controller / Profiles / Map controls navigation, and LOCAL. OPEN. YOURS. / v0.4 footer. The first screen serves as the style reference for the following screens.

## Controller workspace

Header CONTROLLER / LIVE and Controller desk, with connected status at the upper right. Selected-device row with PS4 Controller and Change device. Dominant central dark, handsomely shaded controller top view showing D-pad, face buttons, shoulders, triggers and integrated thumbstick markers; avoid two standalone circle charts as the main visual. INPUT LIVE label. Compact LEFT STICK / RIGHT STICK numeric readouts and L2/R2 meters beneath it. Narrow right SESSION SETTINGS column with Stick deadzone 8% slider, Forward rumble and Prevent double input switches, the note Only the selected physical controller is hidden, Test rumble and Save profile. Fixed bottom dock: Start session, Stop, PS, Touchpad; helper Back + Start: PS / Back + R3: touchpad click. Require exact readable text and native-implementable layout.

Output: `controller-workspace.png`.

## Profiles

Use the workspace as shell/style reference. Profiles navigation selected. LIBRARY / LOCAL, Your controllers., SAVED ON THIS PC. A full-width table with PROFILE / INPUT MODE / DEADZONE / RUMBLE columns. Three illustrative rows: EvoFox Elite X2 / XInput / 8% / ON; Ant Esports GP400 / XInput / 8% / ON; GP400 · HID mode / PS4 / HID / 8% / ON. Selected HID row has an orange left stripe and subtly lighter fill. Below, thin divider and native-looking PROFILE DETAILS editor: profile name, deadzone slider, rumble/hiding switches, Save changes. Left lower area shows delicate outline gamepad and Each connection mode keeps its own settings. Bottom Back to controller action. Use an editorial table rather than cards.

Output: `profiles.png`.

## Mapping capture

Use the workspace as shell/style reference. Map controls navigation selected. MAPPING / 01 OF 20, Make it yours., connected-controller label. Large Press the bottom face button instruction; Release other controls, then begin. Same dark gamepad below with only the bottom face button orange, leader label A / CROSS. Right column MAPPING SEQUENCE: 01–04 FACE BUTTONS; 05–08 SYSTEM + STICKS; 09–14 SHOULDERS + D-PAD; 15–20 ANALOG INPUTS. CURRENT BINDING / Waiting to begin. Fixed action dock Begin step, Skip control, Cancel. Footer Release and centre controls before each step. All data and wording must be clearly readable.

Output: `mapping-capture.png`.

## Mapping review

Use the workspace as shell/style reference. Map controls selected. MAPPING / REVIEW and Mapping ready. Small lime 20 CONTROLS badge. Four binding sections in an editorial two-column table with thin dividers, uppercase labels and monospace outlined binding tags. FACE BUTTONS: A b2, B b3, X b0, Y b1. SYSTEM + STICKS: Back b4, Start b5, L3 b6, R3 b7. SHOULDERS + D-PAD: L1 b8, R1 b9, D-pad mapped. ANALOG INPUTS: Left stick a0/a1, Right stick a2/a3, L2 a4, R2 a5. Note Test your new mapping in live preview before starting a session. Bottom Save mapping and Cancel. No performance claims or fake statistics.

Output: `mapping-review.png`. Implemented values come from actual captured/current SDL bindings, so they can differ from these illustrative values.

## Production controller artwork

Use the generated controller-workspace screen as a visual extraction/style reference. Isolate only the complete central dark charcoal PS4-style controller, same straight-on top/front view, shape, matte shading and readable controls. Remove all interface, labels, callout lines, coordinate markers, orange active illumination, surrounding background and external shadow. Genuine transparent background with narrow equal padding. All buttons neutral. Clean unmarked dark thumbstick tops for dynamic movement overlays. Replace central PlayStation trademark with a small plain dot; no brand logos. Thin unlit orange touchpad strip is allowed. Preserve the sophisticated charcoal material rendering for compositing into the native UI.

Output: `../assets/controller.png`.

A generated outline extraction was rejected because it introduced white-fill artifacts. It is not shipped or referenced by the app; the Profiles illustration was implemented as native line art following the Profiles reference.
