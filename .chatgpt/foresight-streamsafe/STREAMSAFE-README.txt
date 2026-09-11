Valheim Foresight 2.0.1 - StreamSafe LocalHUD r3

이번 수정의 원인
첨부 로그에서 Foresight 로드 및 공격 감지는 정상적으로 진행됐지만,
"Overlay disabled after rendering failure: Capture exclusion failed (Win32 8)."
메시지와 함께 표시가 영구 중단됐습니다.
r2는 첫 번째 캡처 제외 API 실패 시 바로 예외를 던져,
기존 보조 방식인 WCA_EXCLUDED_FROM_DDA를 시도하지 못했습니다.
첫 번째 API가 Win32 8을 반환한 Windows 내부 원인까지 로그로 확정할 수는 없습니다.

r3 변경 사항
- 첫 번째 API의 성공 여부와 관계없이 기존 보조 캡처 제외 API도 시도합니다.
- 둘 중 하나가 성공한 창은 표시를 진행합니다.
- 둘 다 실패하면 그 창만 숨기고 2초 후 재시도합니다. 이 오류로 전체 renderer를 영구 중단하지 않습니다.
- 각 API의 결과와 첫 overlay 프레임 전송 여부를 로그에 기록합니다.
- r2의 HUD 소유자/가시성 검사, bar 중복 방지, 삭제된 HUD 정리 기능을 유지합니다.
- 완성된 평캔 3.1.0의 코드, DLL, 설정은 변경하지 않았습니다.

설치
1. 게임을 종료합니다.
2. 현재 Foresight DLL을 plugins 바깥에 백업합니다. 원본 .bak도 보관합니다.
3. 기존 Foresight 폴더의 Valheim.Foresight.dll만 이 파일로 교체합니다.
4. 같은 플러그인의 DLL을 파일명만 바꿔 함께 설치하지 마세요.
5. 평캔 DLL/설정과 기존 Foresight 의존 파일, 설정, 학습 데이터는 그대로 둡니다.

로그 확인
- "LocalHUD r3: renderer ready" = r3 로드 완료. 아직 캡처 제외 성공을 뜻하지 않습니다.
- "DisplayAffinity=failed (Win32 8); DDA=applied" = 첫 방식은 실패했지만 보조 API는 성공했습니다.
- "First overlay frame published." = 첫 native overlay 프레임을 전송했습니다.
- "This window remains hidden; retrying in 2 seconds." = 두 API 모두 실패해 해당 창의 표시를 보류했습니다.
마지막 경우 로그 전체와 함께 현재 캡처 방식(OBS 게임/창/디스플레이 캡처)을 알려 주세요.

확인 범위
실제 수정 코드로 첫 API 실패/예외 후 보조 방식이 호출되는지,
두 방식 모두 실패한 경우 표시를 허용하지 않는지 자동 검증합니다.
이 검증과 DLL 빌드는 Windows/OBS 실게임 검증을 대신하지 않습니다.
로컬 화면에서 몹의 Foresight bar/icon이 보이는지, OBS 미리보기/녹화에는 빠지는지 확인해 주세요.
카메라 회전, 몹 처치, 화면 밖 이동 후 bar가 남거나 쌓이지 않는지도 확인합니다.
F7 타이밍 편집기는 기존처럼 게임 내부 UI입니다.

캡처 방식 참고
API 호출 성공만으로 모든 캡처 방식의 제외 결과를 보장할 수는 없습니다.
기존 별도 native overlay 방식은 유지하며 게임 HUD 표시로 자동 전환하지 않습니다.
위험도 계산, 아이콘, 공격 타이밍 학습 및 일반 castbar renderer는 원본 v2.0.1을 사용합니다.
Microsoft 문서: SetWindowDisplayAffinity의 지원 범위와 한계
https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity
Microsoft 문서: SetWindowCompositionAttribute의 반환값
https://learn.microsoft.com/en-us/windows/win32/dwm/setwindowcompositionattribute
