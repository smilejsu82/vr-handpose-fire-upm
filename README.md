# VR Hand Pose + Fire (All-in-One) — UPM 패키지

Hand Pose Recorder 도구 · 잡기 그립 포즈 적용(Rifle) · 두 손 VR 소화기 데모.
**UPM 패키지라, Git URL 하나만 추가하면 의존성(XR Hands 등)이 자동 설치됩니다.**

---

## 설치 (Package Manager)
1. Unity ▸ **Window ▸ Package Manager**
2. 좌상단 **`+` ▸ Add package from git URL...**
3. 입력:
   ```
   https://github.com/smilejsu82/vr-handpose-fire-upm.git
   ```
4. Add → **의존성 자동 설치**:
   XR Interaction Toolkit 3.4.1 · XR Hands 1.7.3 · Input System · URP · OpenXR · XR Management

> 특정 버전 고정: URL 뒤에 `#v1.0.5` 처럼 태그를 붙일 수 있습니다.

## 설치 후 1회 설정 (패키지가 못 하는 부분)
- **XR Plug-in Management ▸ (Android/PC 탭) OpenXR** 체크 + **Hand Tracking** + Meta Quest/Oculus Touch 프로파일
- **URP**를 활성 렌더 파이프라인으로 지정 (URP 템플릿이면 자동)
- **Player ▸ Active Input Handling = New(또는 Both)**

## 도구
- 메뉴 **Tools ▸ Hand Pose Recorder** (v1.0.5)
- 그립 포즈 캡처 → Attach to Target → 프리팹/포즈 SO 자동 저장
- 손목까지 일치: `Ghost Hand Model`에 그립용 손 모델 지정
- 문서: `Documentation~/HandPose-Attach-Guide.md`, 변경 내역: `CHANGELOG.md`

## 데모 씬 (Samples)
Package Manager에서 이 패키지 선택 ▸ **Samples ▸ "Demo Scenes" Import**
- `Step_09` 라이플 그립 · `Step_10` 소화기
- ※ 데모 씬은 **XRI Starter Assets 리그/손 모델**을 추가로 임포트해야 완전히 동작합니다
  (Package Manager ▸ XR Interaction Toolkit ▸ Samples ▸ Starter Assets)

## 구성
- `Runtime/` — Hand Pose 적용, Rifle, FireSafetyVR 등 런타임 스크립트 (asmdef로 XR 패키지 참조)
- `Editor/` — Hand Pose Recorder 윈도우 등 에디터 도구
- `Samples~/Demo/` — 데모 씬 + GhostHand + 포즈
- `Documentation~/` — 가이드 문서

## 라이선스 / 주의
- 핸드트래킹 포즈 캡처는 실제 기기(Quest/Link)에서만 동작
- `.unitypackage`가 아니라 UPM 패키지이므로 의존성이 자동 해결됩니다
