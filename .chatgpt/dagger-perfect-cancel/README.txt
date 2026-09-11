Goni Universal Perfect Block Cancel 2.0.0
=========================================

Client-side BepInEx plugin for Valheim 1.0.x.

What it does
------------
Hold Mouse5.

For every eligible primary attack cycle the plugin does:
  NORMAL PRIMARY ATTACK
  -> waits for the REAL Attack.OnAttackTrigger event (the actual hit/projectile trigger)
  -> removes only the post-hit recovery portion
  -> shows a very short block pose
  -> directly starts the next NORMAL PRIMARY ATTACK
  -> repeats while Mouse5 is held

This is not a millisecond macro and it does not attempt to hit the normal human block-cancel timing window.
The recovery transition is driven from Valheim's combat objects after the legitimate hit event has already fired.

Preserved vanilla behavior
--------------------------
- Real attack hit event still fires normally.
- Normal damage calculation is untouched.
- Normal attack stamina/eitr/health cost is untouched.
- Normal ammo/projectile trigger work is untouched.
- Vanilla combo-chain state is preserved by keeping the finished attack as previousAttack.
- Stagger, dodge, no-stamina, menus, death, etc. can still make StartAttack illegal. The plugin retries instead of corrupting those states.

Visual sequence
---------------
The default BlockVisualFrames=1 keeps the inserted block very short so it resembles a perfect manual block cancel rather than an artificial attack-speed animation.

Install
-------
Replace the old Goni.DaggerPerfectCancel.dll with this DLL in:
  Valheim\BepInEx\plugins\

Do not keep old 1.x copies in another plugins subfolder.
Client side only. Do not install on the dedicated server.

Config
------
Valheim\BepInEx\config\goni.valheim.daggerperfectcancel.cfg

Important settings:
- Enabled = true
- TriggerVirtualKey = 6       (0x06 = XBUTTON2 / normally Mouse5)
- BlockVisualFrames = 1       (increase to 2 if you want the shield/block pose more obvious)
- FastForwardNormalizedTime = 0.97
- RestartRetrySeconds = 0.05
- VerboseLogging = false

Weapon coverage
---------------
The mod no longer checks the Knives skill. It drives the currently equipped weapon's PRIMARY attack.
Normal melee weapons/tools that fire Attack.OnAttackTrigger are the main target. Unusual draw/reload/looping attacks are allowed to use the same hook, but the safety timeout releases/restarts the cycle if that weapon does not use the normal attack-trigger path.
