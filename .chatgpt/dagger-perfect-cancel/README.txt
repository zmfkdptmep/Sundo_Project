Goni State Driven Block Cancel 3.2.0 - Sword Follow-up

검 전용 추가 동작
Mouse5 한 번으로:
평타 -> 실제 공격 종료 확인 -> 방어 처리 확인 -> 평타 1회
-> 그 평타의 실제 근접 공격 판정 확인 -> 일반 공격 + 보조공격 동시에 누름/유지
-> 보조 근접 공격 판정 3회 관찰 -> 두 입력 해제.

방어 뒤 평타는 클릭 1회만 보냅니다. 그 판정을 기다리는 동안 좌클릭을
계속 유지해 평타가 추가로 반복되지 않도록 했습니다.
이후 일반/보조 입력은 같은 SetControls 호출에서 동시에 켭니다.
세 번째 보조 근접 판정 직후 정상 SetControls로 두 입력을 해제합니다.
고정 ms로 두 번째 타격이나 후속 3타 완료를 추정하지 않습니다.

적용 범위
- 게임의 Swords 스킬을 사용하는 무기이며 일반/보조공격이 근접 공격인 경우 적용합니다.
- 지원하는 보조공격 종류는 Horizontal / Vertical입니다.
- 단검 등 다른 무기는 기존 3.1의 1200ms 좌클릭 유지 흐름을 사용합니다.
- 검 옵션을 끄면 검도 기존 3.1 입력 흐름을 사용합니다.
- 기존 평캔의 첫 평타/방어 처리 및 Mouse5/F12 입력은 유지합니다.

설치
1. 게임과 기존 AHK 평캔을 종료합니다.
2. 기존 Goni.DaggerPerfectCancel.dll을 BepInEx/plugins 바깥에 백업합니다.
3. 이번 Goni.DaggerPerfectCancel.dll 하나로 교체합니다. 파일명만 바꿔 중복 설치하지 마세요.
4. 기존 설정 파일과 Foresight DLL은 그대로 둡니다.
5. 검을 든 평상 상태에서 Mouse5를 한 번 누릅니다. F12는 입력 해제입니다.
기존 클라이언트 BepInEx 환경을 사용하며 서버에는 설치하지 않습니다.

설정
BepInEx/config/goni.valheim.daggerperfectcancel.cfg
새로 추가되는 [Sword] 항목:
Enabled = true
DualHoldTimeoutSeconds = 6
Enabled=false는 다음 실행부터 기존 3.1 흐름을 선택합니다.
DualHoldTimeoutSeconds는 실패 시 입력을 해제하는 한도입니다.
6초 동안 공격하라는 뜻이 아니며, 판정 3회를 관찰하면 그 전에 해제합니다.
기존 SecondAttackHoldMs=1200은 검 확장 동작 이외의 기존 흐름에 적용됩니다.
자세한 단계 로그가 필요하면 [Debug] VerboseLogging=true로 설정하세요.

관찰 로그
시작: Sword mode: primary -> block -> ONE primary -> primary+secondary hold.
종료: Sword observations: secondaryMeleeEvents=3/3;
      lastSecondaryDamageMultiplier=...;
      staminaSpentDuringDualHold=...; staminaUseCalls=...
- secondaryMeleeEvents: 현재 로컬 플레이어의 보조공격 객체가 수행한 근접 판정 횟수.
  허공에도 판정은 발생합니다. 여러 적을 맞혔다고 여러 타로 세지 않습니다.
  같은 공격 객체가 여러 애니메이션 판정에 재사용돼도 판정마다 셉니다.
- lastSecondaryDamageMultiplier: 관찰한 보조공격 객체의 실제 damage multiplier 필드.
  적의 최종 HP 감소량을 의미하지는 않습니다.
- staminaSpentDuringDualHold: 동시 유지 구간의 Player.UseStamina 호출 전후 감소량 합계.
  달리기 등 다른 원인의 소모도 포함합니다. 스태미나를 변경하거나 환급하지 않습니다.
- ABORT / 0/3, 1/3, 2/3: 관찰한 판정이 부족하거나 입력이 중단된 것입니다.
  이 경우 없는 타격을 추가로 만들거나 성공으로 기록하지 않습니다.

구현 및 확인 범위
기존 요구대로 정상 입력만 주입합니다. 공격 객체 생성, StartAttack 직접 호출,
Attack.Stop, 공격/애니메이션/콤보/데미지/스태미나 값 수정은 하지 않습니다.
읽기 관찰용 후크로 두 번째 평타 및 후속 보조 판정을 확인합니다.
검 관찰 API가 맞지 않으면 확장만 비활성화하고 기존 평캔은 유지합니다.

코드의 이벤트 순서와 기존 3.1과의 입력 비교를 자동 검증합니다.
이 검증은 Windows Valheim 실게임/멀티플레이 검증을 대신하지 않습니다.
3/3 로그만으로 보조공격 애니메이션 3연타, 실제 보조 데미지 및 무소모가
모든 환경에서 재현됐다고 단정하지 않습니다. 실제 화면과 로그를 함께 확인해야 합니다.
처음 확인할 때는 이동 없이 검을 들고 허공/몹 명중을 각각 비교해 주세요.
결과가 다르면 검 이름, 3연타 여부, 스태미나 변화와 BepInEx/LogOutput.log가 필요합니다.

빌드 대상: .NET Framework 4.8 / CoffeeNova.Valheim.ManagedReferences 1.2214.6
