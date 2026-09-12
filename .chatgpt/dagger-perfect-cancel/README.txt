Goni Sword Triple Slash 3.4.0 — Fast Slash

현재 동작과 피해
- Mouse5: 평타 1회 -> 실제 막기 모션 -> 검의 1·2·3타 모션으로 삼연참.
- 삼연참 각 타격은 검 보조공격의 피해 배율을 사용합니다. 청동검은 배율 3입니다.
- 평타 모션을 쓰는 것이 맞습니다. 보조공격 찌르기 모션을 세 번 반복하는 기술은 아닙니다.
- 기존 3.3.1은 재생 속도가 원래대로여서 일반 검 콤보처럼 느리게 보일 수 있었습니다.
- 실제 게임에서 받은 로그에는 네 차례 opening=1, slashes=3 완료가 있었습니다.

3.4.0 속도 변경
- 삼연참의 타격 전 모션: 기본 2.4배 재생.
- 첫 평타와 삼연참의 타격 이벤트 이후 회수 동작: 기본 4배 재생.
- 기술 사이의 실제 막기 모션 확인 시간: 0.10초.
- 이 기술의 명중 정지(hit-stop): 최대 0.025초.
- 타격이 확인된 뒤 남은 모션을 가속합니다. 타격 이벤트를 건너뛰거나 피해만 따로 넣지 않습니다.
- 다음 참격은 실제 타격 이벤트와 게임의 연계 가능 시점을 확인한 뒤 시작합니다.
- 재생 배율은 전체 기술 소요 시간/DPS 배율과 같지 않습니다. 실제 전환 시간과 기존 애니메이션 속도 설정이 영향을 줍니다.
- 세계 시간(Time.timeScale)을 바꾸지 않습니다. 해당 플레이어가 이 기술을 쓰는 동안만 Animator 속도에 배율을 적용합니다.
- 종료, F12, 회피, 점프, UI/장비/플레이어 변경 시 속도를 복구합니다.
- 명중 정지 중 취소할 때는 정지 해제 뒤 복원될 속도에서도 기술 배율을 제거합니다.
- 자동 검막은 계속 제거된 상태이며 F9는 사용하지 않습니다.

설치
1. 게임을 종료합니다.
2. 기존 Goni.DaggerPerfectCancel.dll을 plugins 바깥에 백업합니다.
3. BepInEx/plugins 안의 기존 DLL을 ZIP 안의 DLL로 교체합니다. 중복 버전을 함께 두지 마세요.
4. 로딩 로그의 버전 3.4.0을 확인합니다. 설정 파일은 삭제할 필요가 없습니다.

설정: BepInEx/config/goni.valheim.daggerperfectcancel.cfg
[SwordTempo]
Enabled = true
SlashSpeedMultiplier = 2.4
RecoverySpeedMultiplier = 4
BlockPoseSeconds = 0.1
MaxHitStopSeconds = 0.025

- SlashSpeedMultiplier: 1~3.5, 삼연참 타격 전 모션 재생 배율.
- RecoverySpeedMultiplier: 1~6, 실제 타격 이벤트 후 모션 재생 배율.
- Enabled=false: 3.3.1의 원래 재생 속도와 기존 [SwordSkill] VisibleBlockSeconds 설정 사용.
- Enabled=true이면 새 BlockPoseSeconds를 사용하므로, 예전 설정 파일의 0.18초 때문에 새 속도가 적용되지 않는 문제를 피합니다.

피해·스태미나
- 세 참격의 공격 스태미나 비용은 0입니다. 첫 평타/달리기/실제 피격 방어 비용은 원래대로입니다.
- 평타 마지막 타격의 추가 피해 배율은 보조공격 배율 위에 중복 적용하지 않습니다.
- 숙련도, 난수, 대상 방어력/저항, 상태 효과 등으로 실제 HP 감소량은 달라집니다.
- 다른 무기, 다른 플레이어, 평소 수동 공격의 피해·스태미나·속도는 변경하지 않습니다.

새 요약 로그 (VerboseLogging=false여도 출력)
summary: durationSeconds=...; tripleSeconds=...;
slashDamageMultipliers=[3.00,3.00,3.00]; slashAttackStamina=[0.00,0.00,0.00];
outgoingDamageSamples=...; outgoingPreDefenseDamage=...

- slashDamageMultipliers는 각 실제 참격 이벤트에서 관측한 배율입니다.
- outgoingPreDefenseDamage는 타격 생성 과정에서 관측한 방어 적용 전 피해 범위입니다.
  이후 상태 효과, 상대 방어력/저항, 네트워크 처리에 따른 최종 HP 감소량과는 다를 수 있습니다.
- 허공을 치면 outgoingDamageSamples=0, outgoingPreDefenseDamage=none일 수 있습니다.
- outgoingDamageSamples는 타격 대상별 표본 수이며, 참격 횟수가 아닙니다.
- 시간 항목은 기술 완료/취소까지의 실제 경과 시간입니다.

검증 범위
- Windows Release 빌드 및 기존 연계/입력 회귀 검증.
- 반복 Update/FixedUpdate의 속도 배율 누적 방지, 애니메이션 Speed 이벤트, hit-stop 유지와 취소 시 속도 복구 검증.
- 배포 DLL의 Character.Message/MessageHud/자동 검막 코드 제거 상태 검증.
- 이 환경에서는 Valheim을 실행할 수 없어 새 속도의 실제 화면 모습과 최종 피해 수치는 실게임 검증 전입니다.

문서 참고
Unity Animator.speed: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator-speed.html
Harmony parameter injection: https://harmony.pardeike.net/articles/patching-injections.html
