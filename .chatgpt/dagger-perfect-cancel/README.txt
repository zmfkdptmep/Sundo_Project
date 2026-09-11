Goni Five-Hit Block Cancel 2.2.0
================================

Client-side BepInEx plugin for Valheim 1.0.x.

Behavior
--------
One Mouse5 press executes one five-hit PRIMARY-attack burst:
  Hit 1 (normal stamina)
  -> one very short block flash
  -> Hit 2 (normal stamina)
  -> Hit 3 (0 attack stamina)
  -> Hit 4 (0 attack stamina)
  -> Hit 5 (0 attack stamina)

Hits are linked from Valheim's real Attack.OnAttackTrigger events. After each real trigger, the plugin removes recovery and starts the next primary attack. Damage, hit detection and the weapon's normal attack animation/chain selection still come from Valheim.

Important
---------
- Exactly five attacks per Mouse5 press.
- Release Mouse5 before pressing again for another five-hit burst.
- Only attack stamina is suppressed on hits 3-5. Damage is not multiplied or directly injected.
- The block visual is inserted only after hit 1.
- First two attacks keep their normal attack stamina cost.
- Client side only.

Install
-------
Replace every older Goni.DaggerPerfectCancel DLL with this DLL in:
  Valheim\BepInEx\plugins\

Do not keep 1.x / 2.0 / 2.1 copies elsewhere under plugins.

Config
------
Valheim\BepInEx\config\goni.valheim.daggerperfectcancel.cfg

Important settings:
- Enabled = true
- TriggerVirtualKey = 6
- BlockVisualFrames = 1
- RecoveryCutNormalizedTime = 0.985
- StartRetrySeconds = 0.01
- HitTimeoutSeconds = 3.0
- VerboseLogging = false

If the block flash is too hard to see, set BlockVisualFrames=2.

Weapon coverage
---------------
The plugin uses the currently equipped weapon's PRIMARY attack. Normal melee weapons and tools using Valheim's standard Attack.OnAttackTrigger path are the main target. Unusual draw/reload/looping attacks may behave differently and are protected by a timeout instead of being forced indefinitely.
