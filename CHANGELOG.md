# Hand Pose Recorder — 변경 내역 (Changelog)

도구: `Tools → Hand Pose Recorder`
버전 상수: `HandPoseRecorderWindow.Version` (한 곳만 수정하면 창 헤더·로그·프리팹 각인에 모두 반영)

저장되는 스냅샷/프리팹에는 `HandPoseSnapshotRoot.BakedVersion`으로 만든 버전이 각인되며,
프리팹 인스펙터에서 현재 도구 버전과 다르면 경고가 표시됩니다.

---

## v1.0.5
- **창 안 변경 내역(Changelog) 표시.** 헤더 밑 "변경 내역" 접기 메뉴로 버전 이력 확인.
- **전체 스크롤뷰.** 창을 줄여도 아래 섹션(Export/Posed Hand/지연 캡처)까지 스크롤 가능.

## v1.0.4
- **Ghost Hand Model 옵션 추가.** 지정한 손 모델(예: 컨트롤러 손)로 고스트를 생성한다.
  - 그립에 쓰는 손과 **동일 모델**이 되어 손목뿐 아니라 **손가락 끝까지 완전히 일치**한다.
  - 비워두면 기존처럼 추적 손(XRHandSkeletonDriver)으로 생성.
  - 손가락 포즈 접두사는 Handedness로 결정(Left→`L_`, Right→`R_`). 모델의 본 이름과 일치해야 함.
- 콘솔 로그에 `모델기반: True/False` 표기.

## v1.0.3
- **Attach to Target 시 손목의 '타깃 기준 포즈'를 SO에 저장** (`hasAttachPose`, `attachLocalPosition`, `attachLocalRotation`).
  - 그립 시 손목을 타깃(예: 라이플) 기준으로 고정하는 데 사용(`Rifle.cs` 등).

## v1.0.2
- **Attach to Target 시 HandPoseData(ScriptableObject)도 프리팹과 함께 저장.**
  - 프리팹과 같은 이름으로 떨어져 짝이 맞음. 같은 프레임의 라이브 손 기준으로 캡처.
  - `Save Pose Data on Attach` 토글 추가.

## v1.0.1
- **핵심 버그 수정 — Instantiate가 부모(Hand Visualizer/XR Origin) 월드 프레임을 잃던 문제.**
  - `Instantiate(driver.gameObject)`가 로컬 transform만 복제해 스냅샷이 라이브 손에서 평행이동(예: 5.7cm)하던 것을, 클론 직후 원본 루트의 월드 pose/scale로 맞춰 해결. 검증: 좌·우 양손 0.00cm.
- **지연 캡처(Capture Delay) 추가.** 버튼을 누른 뒤 손을 타깃에 잡고 유지하면 카운트다운 종료 시점에 캡처 → 마우스로 손이 빠지는 "드리프트" 문제 방지.
- **Record Root Pose 한글 설명** 추가.
- **버전 관리 도입.** `Version` 상수 + 창 헤더 표시 + 로그 + 프리팹 각인 + 인스펙터 경고.

## v1.0.0 (이전)
- 초기 기능: Record(HandPoseData 저장), Export(AnimationClip), Attach to Target(고스트 스냅샷 부착/프리팹 저장), 좌표 베이크·검증·가드.
- 자세한 좌표계 원인/해결은 [HandPose-Attach-Guide.md](HandPose-Attach-Guide.md) 참고.

---

## 버전 올리는 법
1. `Assets/Scripts/HandPose/Editor/HandPoseRecorderWindow.cs`의 `public const string Version = "x.y.z";` 수정
2. 이 파일(CHANGELOG.md) 맨 위에 새 항목 추가
3. 변경 요약을 `Version` 상수 위 주석에도 한 줄 남기기
