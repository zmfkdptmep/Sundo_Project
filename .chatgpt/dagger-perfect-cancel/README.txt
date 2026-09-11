Goni State Driven Block Cancel 3.0.0
====================================

Client-side BepInEx plugin for Valheim 1.0.x.

Purpose
-------
Reproduce the proven AutoHotkey sequence without using fixed animation timing:
  PRIMARY click
  -> wait until Valheim reports the first attack actually ended
  -> BLOCK
  -> wait until Valheim reports blocking is actually active
  -> immediately release BLOCK
  -> PRIMARY hold for 1200 ms

Why this version exists
-----------------------
The AHK version works until hit-stop / impact slowdown changes the real animation duration. This plugin does not guess the first-attack-to-block delay in milliseconds. It follows Player.InAttack() and Player.IsBlocking() instead, while injecting only the same queued-click / attack-hold / block fields that normal controls use.

It deliberately does NOT call Attack.Stop(), directly start custom Attack objects, change damage, change stamina cost, or synthesize five hits. Valheim's own normal combo logic decides the result of the post-block LMB hold.

Install
-------
Replace every older Goni.DaggerPerfectCancel DLL with this DLL in:
  Valheim\BepInEx\plugins\

Client side only.

Config
------
Valheim\BepInEx\config\goni.valheim.daggerperfectcancel.cfg

Important settings:
- Enabled = true
- TriggerVirtualKey = 6               (XBUTTON2 / normally Mouse5)
- AcceptEitherSideButtonFallback = false
- SecondAttackHoldMs = 1200           (same role as AHK Attack2_Hold)
- AttackQueueSeconds = 0.50
- FirstAttackStartTimeoutSeconds = 1.5
- FirstAttackEndTimeoutSeconds = 4.0
- BlockStartTimeoutSeconds = 1.5
- VerboseLogging = false

Behavior
--------
One Mouse5 press starts one sequence. Releasing Mouse5 re-arms the next press after the sequence finishes.

The critical transition is state-driven:
- hit-stop can extend the first swing without breaking the cancel timing;
- block is released on the first control frame after IsBlocking() becomes true;
- the second primary input is then held for SecondAttackHoldMs, exactly like the working AHK macro.

Weapon coverage
---------------
There is no knife-only restriction. The currently equipped primary attack is used. Weapons with unusual draw/reload/looping behavior may not match a melee block-cancel sequence and will fall back to the safety timeouts.
