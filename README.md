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

## 포함 내용 (데모 씬·모델 미포함)
이 패키지는 **도구 + 스크립트 + 필수 에셋**만 담습니다. 데모 씬(Step_09/10)과 모델(라이플 등)은 라이선스/용량 때문에 **포함하지 않습니다.**

- `Runtime/` — Hand Pose 적용, Rifle, FireSafetyVR 런타임 스크립트
- `Editor/` — Hand Pose Recorder 윈도우 등 에디터 도구
- `Content/` — 항상 임포트되는 필수 에셋
  - `Animations/XRHand/` — 컨트롤러 손 그립/오픈 애니메이션 + Animator 컨트롤러(`XRHand_L/R`)
  - `Materials/` — `GhostHand`(고스트핸드), `HandGlow`(손 글로우)
  - `Shaders/` — `GhostHand`, `HandGlow` 셰이더

### 쓰는 법
- 본인 XR 리그(컨트롤러 손)에 `XRHand_L/R` 컨트롤러를 Animator에 물리고, 잡을 오브젝트에 `Rifle`(또는 그립 스크립트) + `HandPoseData`를 할당하면 그립 포즈가 동작합니다.
- 손 모델/리그는 XR Hands·XRI Starter Assets 샘플 또는 본인 모델을 사용하세요(이 패키지는 모델 미포함).

## 구성
- `Runtime/` — Hand Pose 적용, Rifle, FireSafetyVR 등 런타임 스크립트 (asmdef로 XR 패키지 참조)
- `Editor/` — Hand Pose Recorder 윈도우 등 에디터 도구
- `Samples~/Demo/` — 데모 씬 + GhostHand + 포즈
- `Documentation~/` — 가이드 문서

## 라이선스 / 주의
- 핸드트래킹 포즈 캡처는 실제 기기(Quest/Link)에서만 동작
- `.unitypackage`가 아니라 UPM 패키지이므로 의존성이 자동 해결됩니다
