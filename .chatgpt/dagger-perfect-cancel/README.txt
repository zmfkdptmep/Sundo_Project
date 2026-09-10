Goni Dagger Perfect Cancel 1.0.0
=================================

Client-side BepInEx plugin for Valheim.

Goal
----
Mouse5 (XBUTTON2) automates the same knife block-cancel sequence a player can perform manually, but it watches Valheim's internal action state instead of using a fixed millisecond delay.

Default behavior
----------------
1. Hold Mouse5.
2. If idle, the plugin requests the first primary knife attack.
   - You may alternatively start the first attack manually and press Mouse5 while that attack is active.
3. It waits until Player.InAttack() actually becomes false.
4. It requests block.
5. It waits until Player.IsBlocking() becomes true.
6. When available, it additionally waits until the underlying Animator blocking bool is true.
7. It releases block.
8. It waits until IsBlocking() is actually false.
9. It requests primary attack on every control tick until Valheim accepts the first legal post-block attack frame.
10. It then holds primary attack for as long as Mouse5 remains held.

This is not a fixed-timing macro. Millisecond values in the config are fail-safe timeouts only.

Install
-------
Copy Goni.DaggerPerfectCancel.dll into:
  Valheim\BepInEx\plugins\

Requirements
------------
- BepInExPack for Valheim
- Windows (Mouse5 is read with Win32 GetAsyncKeyState / XBUTTON2)
- Client side only; do not install on the dedicated server.

Config
------
Created after first launch at:
  Valheim\BepInEx\config\goni.valheim.daggerperfectcancel.cfg

Useful settings:
- Enabled = true
- RequireKnife = true
- ConfirmAnimatorBlock = true
- VerboseLogging = false
- TriggerVirtualKey = 6  (0x06 = XBUTTON2 / usually Mouse5)

Notes
-----
- The plugin does not change attack animation speed, stamina cost, damage, or combo values.
- It only overrides primary-attack and block input while Mouse5 is held; movement/camera/other controls remain vanilla.
- A server-side anti-animation-cancel mod can intentionally prevent the combo carry and therefore defeat this plugin.
- Stamina, stagger, dodge, death, equipment changes, or another mod changing Player.SetControls can still prevent an attack from being legal; no client automation can make an engine-rejected action valid without changing game rules.
