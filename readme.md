# Ghost

Ghost is a small test tool. It is a borderless, semi-transparent window that covers another app, to test if that app pauses when covered or when it loses focus.

## What it tests

- **Occlusion:** Does the app pause because it is 100% covered?
- **Focus loss:** Does the app pause because it lost focus?

If the app keeps moving in Occlusion but pauses in Focus loss, it is a focus-loss type app.

## How to use

1. Start `Ghost.exe`. It starts small, in Focus loss mode.
2. Cover the app you want to test.
3. Maximize Ghost with `F11` or menu > Maximize.
4. Check if the app behind pauses.
5. Switch mode with right-click menu and compare with the same cover.

## Controls

- `F11` = Maximize / Restore. Works in Focus loss mode.
- Right-click = Menu. Works in both modes.
- Menu > Maximize / Restore = same as F11, but works in Occlusion too.
- `Esc` or Menu > Close = Close, instant, no confirmation.
- No double-click. Double-click does nothing on purpose.

## Modes

Menu order:
1. Mode: Focus loss test
2. Mode: Occlusion test
3. Maximize / Restore
4. Close

Default is Focus loss.

- **Focus loss:** Ghost is a normal window. It steals focus. App behind loses focus.
- **Occlusion:** Ghost uses NOACTIVATE. It never steals focus. App behind keeps focus, but is covered.

## Warning - Occlusion

In Occlusion mode, `F11` does NOT go to Ghost. It goes to the app behind, because Ghost never gets focus. That app can maximize instead of Ghost.

In Occlusion, use menu Maximize / Restore, not F11.

Small + Occlusion is allowed, but it is not a valid test. Test only counts when Ghost is maximized and covers the app 100%.

## Maximized window

When maximized, Ghost shows no text. Only background + border + dot.

- Dot is green in Focus loss mode
- Dot is orange in Occlusion mode
- Dot is top-right, 6px from edge. Ghost uses WorkingArea only, so it never covers the taskbar.

## Build
