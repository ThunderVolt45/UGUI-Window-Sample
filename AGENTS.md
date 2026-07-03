# AGENTS.md

This repository is a Unity sample project for building a desktop-OS-style window UI using only UGUI.

## Project Basics

- Unity version: `6000.2.6f2` or newer.
- Main sample scene: `Assets/UGUIWindowSample/Scenes/UGUIWindowSampleScene.unity`.
- Core code lives under `Assets/UGUIWindowSample/Scripts/`.
- Window prefabs are loaded by convention from `Assets/Resources/Windows/{ClassName}.prefab`.
- Public-facing documentation starts at `README.md` and `docs/Manual.md`.

## Important Docs

Read these before changing behavior:

- `docs/manual/02-concepts.md` for architecture, pooling, z-order, and DPI.
- `docs/manual/03-creating-windows.md` for creating custom windows and prefab naming.
- `docs/manual/06-events-lifecycle.md` for open, close, focus, and minimize events.
- `docs/manual/07-samples.md` for the desktop sample scene.
- `docs/manual/08-api-reference.md` for public APIs.
- `docs/ClassDiagram.md` and `docs/class-diagram/*.md` for class relationships.

## Architecture Notes

- `UGUIWindowManager` is the singleton entry point for window creation, pooling, z-order, DPI scaling, and ESC handling.
- `UGUIWindow` is the controller for each window. It owns the window mode and exposes:
  - `OnOpenWindow`
  - `OnCloseWindow`
  - `OnFocusWindow`
  - `OnMinimizeWindow`
- `UGUIWindowView` owns visual state such as header, border, buttons, fade, and maximized/restored layout.
- `UGUIWindowState` stores position, size, anchors, and flags for restore behavior.
- The sample desktop layer is in `Assets/UGUIWindowSample/Scripts/Sample/`.

## Implementation Guidelines

- Prefer existing patterns over new abstractions.
- Keep base window-system changes small and reusable.
- Put demo-only behavior in the `Sample` folder unless the feature belongs to the framework.
- When overriding `UGUIWindow.Awake` or `UGUIWindow.OnEnable`, call `base` first.
- Use `UGUIWindowLog` for project logs instead of raw `Debug.Log` in framework code.
- Preserve object pooling behavior unless the task explicitly changes it.
- For features that observe window state, prefer subscribing to existing window events instead of polling.
- Avoid editing Unity prefab or scene YAML by hand unless the change is small, deliberate, and easy to verify.

## Unity Asset Guidelines

- Keep `.meta` files with their assets.
- Use forward slashes in Unity asset paths.
- Do not rename prefab/class pairs casually; window loading depends on class-name-to-prefab-name matching.
- If creating a new window class, add the matching prefab under `Assets/Resources/Windows/`.
- If changing serialized fields, consider whether existing prefabs need their references assigned in Unity.

## Testing And Verification

- If Unity MCP/editor tools are available, use them to check compilation after C# changes.
- At minimum, inspect changed C# files and run repository searches for broken references.
- For UI work, verify the sample scene still covers:
  - create/open
  - focus/z-order
  - minimize/restore
  - close/pooling
  - DPI changes

## Current Sample Flow

- `UGUIDesktop` gathers child `UGUIIcon` components and spawns a few demo windows on `Start`.
- `UGUIIcon` opens a window by resolving `UGUIWindow.{targetClassName}` on double click.
- `UGUIMenu` opens the settings window and can quit the app.
- `UGUIApplicationSetting` applies resolution, fullscreen mode, framerate, and DPI changes.

