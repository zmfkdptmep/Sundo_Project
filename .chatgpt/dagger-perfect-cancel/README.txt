Goni Sword Block Cancel 3.5.0 - Native Input

요청한 원래 입력 순서
Mouse5 한 번: 평 -> 방어 -> 평 1회 -> 평+보조공격 함께 누르고 있기.
F12: 입력 해제. Mouse5를 계속 누른 상태에서는 재발동하지 않습니다.

설치
1. 발헤임을 종료합니다.
2. 기존 Goni.DaggerPerfectCancel.dll을 이 ZIP의 같은 이름 DLL로 교체합니다.
   BepInEx/plugins 안에 구버전과 신버전 DLL이 동시에 남지 않게 합니다.
3. 기존 cfg를 삭제할 필요 없습니다. 이번 버전은 [SwordInput] 설정을 사용합니다.
   이전 [SwordSkill], [SwordTempo] 설정은 이번 DLL에서 사용하지 않습니다.

3.4.0에서 달라진 점
- 직접 공격을 시작하거나 3타 기술을 생성하는 코드를 제거했습니다.
- 모션 속도, 회수 속도, 명중 정지, 피해 배율, 스태미나, 연계 규칙 변경을 제거했습니다.
- 방어는 기존 평캔처럼 게임 UpdateBlock의 실제 방어 적용을 확인합니다.
  추가로 정해진 시간 동안 방어 자세를 유지하지 않습니다.
- 방어 뒤 평타 한 번의 StartAttack 성공을 관찰한 뒤, 해당 입력 처리가
  끝나는 즉시 좌클릭과 보조공격 입력을 함께 적용합니다.
  타격 판정이나 평타 종료까지 기다리지 않습니다.
- 다음 게임 PlayerAttackInput이 두 버튼을 함께 처리합니다. 입력 처리는
  게임 원본을 그대로 실행합니다. 별도로 StartAttack을 호출하지 않습니다.
- 한 프레임 안에 물리 갱신이 여러 번 돌아도 첫 클릭이 중복되지 않도록
  실제 입력 소비 직전에 갱신하고 소비 직후 입력을 정리합니다.
- 마지막 입력은 실제 보조공격 프로필의 근접 공격 이벤트 3회가 관찰되면 해제합니다.
  같은 Attack 객체가 여러 애니메이션 이벤트를 처리해도 이벤트를 누락하거나 억제하지 않습니다.
- 스태미나를 0으로 덮어쓰지 않습니다. 무소모 여부는 원래 평캔의 게임 내 결과를 따릅니다.
- 검막/자동 방어는 없습니다. 검 이외 무기의 기존 3.1 평캔 경로는 유지합니다.

설정
[SwordInput]
Enabled = true
DualHoldTimeoutSeconds = 8

DualHoldTimeoutSeconds는 무한 입력을 막는 중단 한계입니다. 공격 시점을 정하는
지연 시간이 아닙니다. 시간 초과 시 부족한 공격을 만들어내지 않고 입력을 해제합니다.
F12, 포커스 상실, UI, 회피/점프, 피격 경직, 무기/플레이어 변경 시에도 해제합니다.
입력 시작/해제 때 입력 큐 2개만 비웁니다. 공격/애니메이션 상태 필드는 수정하지 않습니다.

확인할 로그
[Info : Goni State Driven Block Cancel] ... 3.5.0 loaded
[SwordInput] Ready ...
PostBlockAccepted: vanilla accepted ONE post-block primary; no melee wait
DualHold: primary consumer returned; press primary+secondary TOGETHER now
nativeMelee: secondary=True; secondaryEvents=.../3; multiplier=...;
             profileStamina=...; attackAnimation=...; clips=...
result=...; openingStarts=1; postBlockPrimaryStarts=1; ...

postBlockPrimaryStarts=1은 동시 입력 이전의 단독 평타 시작 횟수입니다.
dualPrimaryStarts/dualSecondaryStarts는 동시 입력 중 원본 게임이 승인한 공격 횟수입니다.
secondaryEvents는 대상 수나 명중 수가 아닌 근접 공격 이벤트 수이며, 허공에서도 나옵니다.
multiplier는 해당 이벤트를 처리한 원래 공격 객체의 배율입니다. 몹 방어력 적용 후의
실제 HP 감소는 이 로그만으로 확인하지 않습니다. clips는 실제 재생 모션 진단용입니다.
profileStamina는 프로필 값이고, staminaSpentDuringDual은 실제 UseStamina 관찰값입니다.
후자는 달리기 등 다른 행동의 소모도 포함합니다.

검증 범위
빌드, 입력 상태 전이, 명중 지연/여러 물리 갱신/큐 지연/중단 시나리오와 DLL의
공격·피해·모션 변경 코드 제거 여부를 자동 검사합니다. 발헤임 실행 환경에서 원래
보조공격 평캔이 100% 재현되는지는 아직 실측하지 않았습니다. 보조공격 이벤트 3회가
나왔다고 해서 반드시 원하는 평캔 모션/무소모가 재현됐다고 판정하지 않습니다.
다른 게임 버전이나 모드가 연계 규칙을 바꾸면 같은 입력의 결과도 달라질 수 있습니다.
