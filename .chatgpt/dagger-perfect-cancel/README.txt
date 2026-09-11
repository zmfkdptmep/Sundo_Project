Goni State Driven Block Cancel 3.1.0

목적
Mouse5 한 번으로 정상 입력만 순서대로 주입합니다.
첫 좌클릭 -> 실제 공격 시작 확인 -> InAttack 종료 확인 -> 막기 입력
-> 게임의 UpdateBlock 처리 확인 -> 막기 해제 + 좌클릭 1200ms 유지.
첫 공격 종료 시각과 막기 완료 시각을 고정 ms로 추정하지 않습니다.

3.0과의 차이
- Player.SetControls Prefix에서 정상 입력 인자만 변경합니다.
- 첫 클릭을 PlayerAttackInput이 실제 소비한 뒤 해제합니다.
- IsBlocking만 확인하지 않고, UpdateBlock 이후 게임이 설정한
  m_internalBlockingState도 읽어 실제 막기 처리를 확인합니다.
- m_queuedAttackTimer, m_attackHold, m_blocking 등 게임 필드를 직접 쓰지 않습니다.
- Attack.Stop, 직접 StartAttack, 애니메이션 변경, 데미지 생성, 스태미나 변경,
  인위적인 5타 생성은 없습니다. 후속 공격 수는 게임이 결정합니다.
- 공격/막기 실패는 중단으로 기록합니다. 상태 전이 완료를 평캔 성공으로 단정하지 않습니다.

설치
1. 게임과 AHK 스크립트를 종료합니다.
2. 기존 Goni.DaggerPerfectCancel 및 StateDrivenBlockCancel DLL을
   BepInEx/plugins 바깥으로 옮깁니다. 다른 파일명으로 중복 설치하지 마세요.
3. 새 Goni.DaggerPerfectCancel.dll 하나만 BepInEx/plugins에 넣습니다.
4. 게임을 실행하고 근접 무기를 든 평상 상태에서 Mouse5를 한 번 누릅니다.
   단검이 기준 무기입니다. F12는 입력 해제입니다.
서버에는 설치하지 않습니다. 기존 BepInEx 환경을 사용합니다.

설정
BepInEx/config/goni.valheim.daggerperfectcancel.cfg
Enabled = true
TriggerVirtualKey = 6
AcceptEitherSideButtonFallback = false
SecondAttackHoldMs = 1200
VerboseLogging = true
기존 설정에서 VerboseLogging=false였다면 진단할 때 true로 변경해 주세요.
AttackQueueSeconds는 3.1에서 사용하지 않습니다.
Safety의 timeout은 실패 시 중단하는 한도이며, 공격/막기 타이밍을 정하는 값이 아닙니다.

게임 검증 필요
빌드 및 순수 상태머신 테스트는 Windows Valheim 실전 검증을 대신하지 않습니다.
적 타격 시 hit-stop을 포함한 공격 태그 종료를 기다리도록 설계했지만,
실제 extended combo 발생 및 '첫 평타 후 5타/일부 무소모'는 아직 확인 전입니다.
FirstPress -> WaitFirstStart -> WaitFirstEnd -> WaitBlockApplied -> SecondHold
로그가 모두 나와도 5타 성공이 증명되지는 않습니다.
스태미나 부족, 경직, 회피, 메뉴, 창 전환, 무기 교체 등으로 입력이 거부/중단될 수 있습니다.
이미 게임이 소비한 클릭이나 정상 공격 큐는 강제로 취소하지 않습니다.
첫 클릭 때 마우스 버튼을 직접 누르거나 다른 자동입력 모드를 동시에 실행하지 마세요.

확인할 결과
- 단검: 허공, 몹 명중, 여러 몹 명중에서 같은 평캔 동작이 발생하는지.
- 후속 공격 횟수와 스태미나 사용 양상이 수동 성공 동작과 일치하는지.
- Mouse5를 길게 눌러도 한 번만 실행되는지. F12/창 전환 시 유지 입력이 풀리는지.
문제가 남으면 무기명, 허공/명중 각각의 결과와 BepInEx/LogOutput.log를 첨부해 주세요.

빌드
기존 프로젝트와 같은 CoffeeNova.Valheim.ManagedReferences 1.2214.6 참조,
.NET Framework 4.8 대상입니다. 시작 시 필요한 입력 API/필드를 확인하며,
다르면 모드를 비활성화하고 Unsupported game control API 로그를 남깁니다.
