Valheim Foresight 2.0.1 - StreamSafe LocalHUD r2

변경 사항
- 원래 HUD가 삭제된 후 overlay에 남던 bar/icon을 즉시 비활성화하고 삭제합니다.
- EnemyHud가 원래 HUD를 삭제하기 전에 임시 이동했던 UI를 부모에게 반환합니다.
- 실제 HUD 소유자, 활성 상태, 이름 UI, 로컬 카메라/화면 위치를 먼저 검사합니다.
- 한 HUD에서 bar/icon이 프레임당 여러 번 생성/이동되지 않도록 중복을 차단합니다.
- 현재 프레임에 갱신하지 못한 창을 숨기고, 더 이상 보이지 않는 대상의 창/복사본을 해제합니다.
- 창 전환/게임 이탈 시 overlay 창을 숨깁니다.

캡처 제외
기존 Windows 별도 overlay 창과 WDA_EXCLUDEFROMCAPTURE 방식은 유지했습니다.
캡처 제외 API 호출이 실패하면 Foresight 표시를 숨깁니다.
원본 표시로 자동 전환해서 방송에 표시하는 fallback은 사용하지 않습니다.
위험도 계산, 아이콘, 공격 타이밍 학습 및 일반 castbar renderer는 원본 v2.0.1을 사용합니다.
캡처 제외는 Windows/캡처 방식에 의존하며 실전 확인이 필요합니다.
F7 타이밍 편집기는 여전히 게임 내부 UI입니다.

설치
1. 게임을 종료합니다.
2. 현재 Foresight DLL을 plugins 바깥에 백업합니다. 제공한 .bak 원본도 보관합니다.
3. 기존 Foresight 폴더의 Valheim.Foresight.dll을 이 파일로 교체합니다.
4. 같은 플러그인의 DLL을 파일명만 바꿔 함께 설치하지 마세요.
5. 기존 BepInEx/YamlDotNet 등 의존 파일, 설정, 학습 데이터는 유지합니다.
로그에 '[StreamSafeOverlay] LocalHUD r2:'가 표시되는지 확인해 주세요.

확인
싱글/멀티에서 카메라 회전, 몹 처치, 대상이 화면 밖으로 이동, 다른 플레이어 근처 이동 후
bar가 쌓이지 않는지 확인합니다. 같은 장면을 OBS 미리보기/녹화와 비교합니다.
게임 로컬 화면에는 보이고 방송에는 보이지 않아야 합니다.
이 빌드는 코드 및 빌드 검증용 수정본이며, 사용자의 Windows/OBS 실게임 검증은 아직입니다.
문제가 남으면 로컬 화면 스크린샷과 BepInEx/LogOutput.log를 첨부해 주세요.
