Goni Sword Triple Slash 3.3.1 — No Auto Guard

3.3.0 반복 오류 수정
- 로그에 1,423번 반복된 Character.Message(..., UnityEngine.Sprite) 호출을 제거했습니다.
- 검막의 매 프레임 처리에서 발생한 API 불일치가 삼연참 업데이트를 막고 있었습니다.
- 검막 자동 방어, 적 탐색, F9 토글, 화면 메시지 호출은 코드에서 삭제했습니다.
- 예상하지 못한 실행 오류가 생기면 해당 세션의 삼연참을 중단하고 한 번만 기록합니다.
  같은 실패를 매 프레임 재시도하지 않습니다.
- 게임 버전별로 유무가 달라질 수 있는 선택적 피해 보정 필드는 실행 중 존재할 때만 복사합니다.

설치
1. 게임을 종료합니다.
2. 기존 Goni.DaggerPerfectCancel.dll을 plugins 바깥에 백업합니다.
3. BepInEx/plugins 안의 기존 DLL을 이 ZIP의 Goni.DaggerPerfectCancel.dll로 교체합니다.
   같은 이름/기능의 구버전 DLL을 plugins 하위 폴더에 중복으로 두지 마세요.
4. 게임을 실행하고 로딩 로그의 버전 3.3.1을 확인합니다.
- 설정 파일을 삭제할 필요는 없습니다.
- 예전 [SwordGuard] 설정이 남아 있어도 이 DLL에는 검막 실행 코드가 없습니다.

Mouse5 기술
검을 들고 Mouse5(XBUTTON2)를 한 번 누르면:
평타 1회 -> 실제 막기 자세 -> 보조공격 피해 배율의 검 1·2·3타 -> 종료.
막기 이후의 세 참격이 바로 삼연참이며, 그 앞에 준비용 평타를 추가하지 않습니다.

- 세 참격은 검 평타의 1·2·3타 모션을 사용합니다.
- 각 참격에 해당 검의 보조공격 피해 배율을 적용합니다. 첨부 로그의 청동검은 배율 3입니다.
- 원래 평타 마지막 타격의 추가 배율을 중복 적용하지 않습니다.
- 세 참격의 공격 스태미나 비용은 0입니다. 첫 평타/실제 피격 방어/달리기 비용은 원래대로입니다.
- 최종 피해는 숙련도, 난수, 적의 방어력·저항과 상태 효과 등에 따라 달라집니다.
- 실제 타격 이벤트와 연계 가능 시점을 확인합니다. 적 명중/허공의 시간 차이를 고정 Sleep으로 맞추지 않습니다.
- Mouse5를 누르고 있어도 반복하지 않습니다. 손을 뗐다가 다시 누르면 다음 기술을 실행합니다.
- F12: 기술/평캔 중단 및 입력 해제.
- 회피/점프, 사망, 경직, 장비 변경, UI 열기, 창 전환 시 기술을 중단합니다.
- 검 이외 무기의 Mouse5는 기존 3.1 평캔 입력 흐름을 사용합니다.

막기 구간
자동 검막은 제거했지만 기술 순서에 필요한 짧은 막기 모션은 유지합니다.
Animator의 실제 block 클립과 게임의 방어 상태를 확인한 뒤 세 참격으로 넘어갑니다.
모션을 찾지 못하면 watchdog at Block으로 중단하고 관측한 클립 이름을 기록합니다.

설정: BepInEx/config/goni.valheim.daggerperfectcancel.cfg
[SwordSkill]
Enabled = true
VisibleBlockSeconds = 0.18
StageTimeoutSeconds = 8

[Debug]
VerboseLogging = true

확인할 로그
- [SwordSkill] Ready: ... Auto guard removed.
- weapon=$item_sword_bronze; secondaryDamageMultiplier=3; slashAnimations=swing_longsword0,swing_longsword1,swing_longsword2
- melee: opening=1; slashes=1/3 -> 2/3 -> 3/3
- complete: opening=1, slashes=3

검증 범위
- Windows Release 빌드, 연계 상태 전환/입력 회귀 테스트.
- 실제 배포 DLL의 메타데이터를 검사하여 Character.Message, MessageHud, 자동 검막 코드가 없음을 확인합니다.
- 실제 Valheim 클라이언트를 실행할 수 없는 환경에서 제작되어, 화면 모션과 실전 피해 수치 및 모드 호환성은 실게임 검증 전입니다.
