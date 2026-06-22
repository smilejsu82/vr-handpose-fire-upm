# Hand Pose Attach 가이드 — 위치가 틀렸던 이유 & 수정 방법

Hand Pose Recorder의 **Attach to Target** 기능으로 XRHand 스냅샷을 큐브 등 타깃에 붙일 때, 손 위치가 어긋났던 원인과 최종 해결 방법을 정리한 문서입니다.

---

## 1. 기능 개요

| 항목 | 설명 |
|---|---|
| **도구** | `Tools → Hand Pose Recorder` |
| **사용 시점** | Play 모드 + 핸드 트래킹 중 |
| **Attach to Target** | 추적 중인 손을 복제 → 타깃 자식으로 부착 → (옵션) 프리팹 저장 |
| **Save Prefab on Attach** | 부착된 상태 그대로 `.prefab` 저장 (권장) |

손 **모양**은 `L_Wrist` 등 본(bone)의 로컬 회전/위치에, 손 **배치**는 타깃 기준 루트 `(0,0,0)` + 본 오프셋에 저장됩니다.

---

## 2. 증상

### 2.1 Attach vs Clone 위치 불일치

- **Attach** (`HandPose_Snapshot`): 큐브 안/주변에 손이 올바르게 보임
- **Clone** (`HandPose_Snapshot(Clone)`): 손이 큐브에서 **~1m 위·앞** 등 엉뚱한 곳에 표시

### 2.2 측정 예시 (Step_05 / Step_06)

| 객체 | root localPos | mesh bounds (대략) |
|---|---|---|
| Attach (정상) | `(0, 0, 0)` | `(-0.07, 1.13, 0.46)` |
| Clone (오류) | `(0.24, -1.10, 1.40)` | `(0.16, 0.02, 1.86)` |

루트·본 데이터는 비슷한데 **Clone만** root local이 `(0,0,0)`이 아니면 메시 전체가 밀립니다.

### 2.3 기타 증상

- 손이 **아예 안 보임** → `SkinnedMeshRenderer.enabled = false` (트래킹 lost 시 HandVisualizer가 끔)
- 프리팹 저장 실패 → `HideFlags.HideAndDontSave` 설정 후 `SaveAsPrefabAsset` 호출

---

## 3. 왜 위치가 틀렸는가

XR Hand 추적 손의 계층 구조는 대략 다음과 같습니다.

```
LeftHand(Clone)              ← 드라이버 루트 (월드 원점 근처)
├── L_Wrist                  ← 추적된 손목 (로컬/월드 오프셋 큼)
│   └── … (손가락 본)
└── LeftHand                 ← SkinnedMeshRenderer (메시 컨테이너, local ≈ 0,0,0)
```

손 **위치 정보**가 한곳이 아니라 **여러 Transform에 분산**되어 있습니다.

### 원인 1: 추적 좌표계 오프셋이 루트·손목에 분산

- 라이브 손 루트는 **Hand Visualizer / XR Origin 원점** `(0,0,0)` 근처
- 실제 손목 위치는 **`L_Wrist` 본**의 로컬/월드 값에 있음
- Attach 시 `worldPositionStays = true`로 부모만 바꾸면, 루트 local이 **`(0, -1.1, -0.5)`** 같은 “큐브 위치 보정값”이 됨

프리팹을 큐브 자식으로 넣을 때 루트가 `(0,0,0)`으로 들어가면, 손목에만 남은 오프셋으로 **손이 큐브에서 떠 보입니다.**

### 원인 2: 메시 컨테이너(LeftHand)에 월드 위치를 보존

초기 베이크가 **모든 자식의 월드 위치**를 유지했습니다.

- `LeftHand` 메시 컨테이너의 월드 위치 = **추적 원점** `(0,0,0)`
- 큐브 `(0, 1.1, 0.5)`의 자식으로 두면 → `LeftHand.localPosition = (0, -1.1, -0.5)`
- SkinnedMeshRenderer는 이 Transform도 곱하므로 **메시가 ~1m 어긋남**

**본은 월드 유지, 메시 컨테이너는 local `(0,0,0)`** 이어야 합니다.

### 원인 3: 프리팹 Instantiate / 드래그 순서

프리팹을 씬에 놓을 때:

1. 월드 위치 `(0.62, 0, 1.90)` 등에 생성
2. Cube 자식으로 붙임 (`worldPositionStays`)
3. root local → `(0.62, -1.10, 1.40)` (틀림)

`Awake` / `OnTransformParentChanged`에서 스냅해도, **Instantiate 직후 transform이 다시 덮어씌워질 수 있습니다.**

### 원인 4: 별도 Prefab 저장 + Reset Root

Attach와 분리된 Prefab 저장 + “Reset Root”로 루트/손목을 `(0,0,0)`만 맞추면 **추적 위치 정보가 사라집니다.**

---

## 4. 어떻게 수정했는가

### 4.1 BakePoseRelativeToTarget (핵심 베이크)

**파일:** `Assets/Scripts/HandPose/Editor/HandPoseRecorderWindow.cs`

```
1. 타깃에 SetParent(target, false)
2. handRoot.localPosition / localRotation = (0,0,0) / identity
3. 본(L_Wrist 등): 캡처 시점 월드 pos/rot 유지 (SetPositionAndRotation)
4. LeftHand / RightHand / SMR: local (0,0,0) 강제 (IsMeshContainer)
```

→ 타깃 기준 **루트 원점 + 본 오프셋** 구조로 굳힙니다.

### 4.2 HandPoseSnapshotRoot (Clone 보정)

**파일:** `Assets/Scripts/HandPose/HandPoseSnapshotRoot.cs`

- 부모가 있을 때 root local을 `(0,0,0)`으로 유지
- `[ExecuteAlways]` + `Awake` / `OnEnable` / `OnTransformParentChanged` / **`LateUpdate`**
- Instantiate·드래그 후 transform이 다시 쓰여도 매 프레임 보정

### 4.3 HandPoseSnapshotValidator (자동 검증)

**파일:** `Assets/Scripts/HandPose/HandPoseSnapshotValidator.cs`

Attach 직후·프리팹 저장 전 검사:

- `HandPoseSnapshotRoot` 존재
- root `localPosition ≈ (0,0,0)`, `localRotation ≈ identity`
- 메시 컨테이너 `localPosition ≈ (0,0,0)`
- XR Hands 추적 컴포넌트 없음
- `SkinnedMeshRenderer` 존재

**검증 실패 시** → 콘솔 Error + **프리팹 저장 중단**

### 4.4 HandPoseSnapshotPrefabGuard (프리팹 가드)

**파일:** `Assets/Scripts/HandPose/Editor/HandPoseSnapshotPrefabGuard.cs`

- `Assets/HandPoses/Prefabs/*.prefab` 임포트 시 자동 검증
- Inspector에 검증 상태 + **Snap To Parent Origin** 버튼

### 4.5 워크플로 단순화

- **Attach to Target** 버튼 하나로 부착 + (옵션) 프리팹 저장
- **Save Prefab on Attach** 권장
- 별도 “Save Prefab” / “Reset Root” 경로 제거

### 4.6 기타

- 클론 시 모든 `Renderer.enabled = true`
- 홀로그램 머티리얼: `Assets/Materials/HologramHand.mat`
- `VelocityPrefab` 등 디버그 오브젝트 제거
- AI/개발 규칙: `.cursor/rules/hand-pose-recorder.mdc`

---

## 5. 올바른 사용법

1. **Step 씬 Play** → 손 트래킹 확인
2. `Tools → Hand Pose Recorder` 열기
3. **Attach Target** = Cube (또는 대상 오브젝트)
4. **Save Prefab on Attach** ✓
5. 원하는 손 모양에서 **`➜ Attach to Target`** 클릭
6. 콘솔 확인: **`[HandPoseRecorder] 검증 OK`**

프리팹 재사용:

- `Assets/HandPoses/Prefabs/` 에서 프리팹을 **타깃의 자식**으로 배치
- root local이 `(0,0,0)`이면 Attach와 동일 위치
- 어긋나면 Inspector → `HandPoseSnapshotRoot` → **Snap To Parent Origin**

---

## 6. 검증 체크리스트

Attach 또는 Clone 후 Hierarchy / Scene에서 확인:

| 항목 | 기대값 |
|---|---|
| `HandPose_Snapshot` parent | Attach Target (예: Cube) |
| root `localPosition` | `(0, 0, 0)` |
| `LeftHand` `localPosition` | `(0, 0, 0)` |
| `L_Wrist` `localPosition` | 타깃 기준 오프셋 (예: `(-0.10, -0.01, -0.12)`) |
| Attach vs Clone mesh bounds | 거의 동일 |
| 콘솔 | `[HandPoseRecorder] 검증 OK` |

`hera-agent-unity`로 씬 프로브 예시:

```powershell
# Play 중 HandPose 객체 transform / mesh bounds 확인용 exec
```

---

## 7. 금지 패턴 (재발 방지)

| 하지 말 것 | 이유 |
|---|---|
| 메시 컨테이너 월드 위치 보존 | `(0,-1.1,-0.5)` 오프셋 baked → Clone 시 ~1m 어긋남 |
| Attach 없이 프리팹만 저장 후 수동 배치 | 타깃 기준 베이크 누락 |
| 루트/손목만 0으로 리셋 | 손 위치 정보 손실 |
| `HideAndDontSave` 후 프리팹 저장 | SaveAsPrefabAsset 실패 |
| Play 중 구 코드로 Attach 후 재컴파일 전 테스트 | 이전 버그 있는 DLL 실행 |

---

## 8. 관련 파일

```
Assets/Scripts/HandPose/
├── Editor/
│   ├── HandPoseRecorderWindow.cs      # Attach, Bake, UI
│   └── HandPoseSnapshotPrefabGuard.cs # 프리팹 임포트 검증
├── HandPoseSnapshotRoot.cs            # Clone root (0,0,0) 보정
├── HandPoseSnapshotValidator.cs       # 검증 로직
├── HandPoseData.cs
└── HandPose-Attach-Guide.md           # 이 문서

Assets/HandPoses/Prefabs/              # 저장된 스냅샷 프리팹
Assets/Materials/HologramHand.mat      # 홀로그램 머티리얼
Assets/Shaders/Hologram.shader

.cursor/rules/hand-pose-recorder.mdc   # AI/개발용 규칙
```

---

## 9.4 핵심 버그 — Instantiate가 부모(Hand Visualizer) 월드 프레임을 잃음 (해결됨)

### 증상
드리프트가 전혀 없는 상태(코드로 캡처+Attach를 한 프레임에 실행)에서도
Attach된 스냅샷이 라이브 손과 **일정한 거리만큼 평행이동**해 어긋났다.
- 손목·손가락끝 오차의 **크기·방향이 완전히 동일** → 회전/스케일 오류 아님, 순수 평행이동.
- 측정: wrist/tip delta = `(-0.0231, 0, 0.0525)` = **5.7cm** (Y는 정확히 0).

### 원인
`BuildPosedHandClone`의 `Instantiate(driver.gameObject)`는 원본의 **로컬** transform만 복제한다.
드라이버(`LeftHand(Clone)`)는 **`Hand Visualizer`**(월드 `(0.023, 0, -0.052)`)의 자식이므로,
부모 없이 복제된 클론은 부모의 월드 오프셋을 잃고 그만큼 통째로 어긋난다.
→ §3 "원인 1"이 말한 "추적 좌표계 오프셋"의 **실제 코드 위치**가 바로 이 클론 단계였다.

### 해결
클론 직후, 원본 드라이버 루트의 **월드 pose/scale**로 클론 루트를 맞춘다:

```csharp
var clone = Instantiate(driver.gameObject);
clone.name = handName;
var src = driver.gameObject.transform;
clone.transform.SetParent(null, false);
clone.transform.SetPositionAndRotation(src.position, src.rotation); // 원본 월드 pose
clone.transform.localScale = src.lossyScale;                        // 원본 월드 scale
```

검증(드리프트 0, 한 프레임 캡처+Attach): **좌/우 양손 wrist·tip delta = 0.00cm.**

---

## 9.5 헤드셋 사용 시 위치 어긋남 — "마우스 드리프트" & 지연 캡처

### 증상
헤드셋(Quest)으로 실제 손을 추적해 타깃을 잡고 **Attach to Target (즉시)** 를 눌렀는데,
고스트 핸드가 타깃에서 **수십 cm 떨어져** 굳어진다.

### 원인 (베이크 버그 아님)
`BakePoseRelativeToTarget`는 손을 **타깃 기준 상대 위치**로 정확히 보존한다(수학적으로 올바름).
문제는 **캡처 시점**이다:
- 헤드셋에서 손으로 타깃을 잡은 뒤, **Attach 버튼은 데스크톱 모니터에서 마우스로 클릭**해야 한다.
- 마우스를 누르는 순간 추적된 손은 이미 타깃에서 빠져 기본/휴식 자세로 이동해 있다.
- 레코더는 *클릭 순간*의 손 포즈를 캡처하므로, "타깃을 잡은 포즈"가 아니라 "빠진 손"이 저장된다.
- 그 결과 = (빠진 손 위치 − 타깃 위치) 만큼 어긋남.

진단 팁: 저장된 프리팹에서 `메시 컨테이너 localPos + baked mesh bounds.center` 의 합이
타깃 피벗에서 얼마나 떨어졌는지 보면, 그게 곧 캡처 순간 손–타깃 거리다.

### 해결: 지연 캡처 (Capture Delay)
`HandPoseRecorderWindow`에 **지연 캡처** 추가:
1. **Capture Delay (s)** 를 설정(기본 3s).
2. **⏱ Attach to Target (N초 후)** 클릭 → 카운트다운 시작.
3. 헤드셋에서 손을 타깃에 잡고 **그대로 유지**.
4. 카운트다운 종료 순간의 손 포즈로 자동 캡처 → 손을 마우스로 옮길 필요 없음.

> Play 모드 + 손 추적 중에만 동작. 카운트다운 중 Play를 벗어나면 자동 취소.

### 추가 점검: 타깃 피벗
지연 캡처를 써도 어긋난다면 **Attach Target의 피벗 위치**를 확인한다.
손은 "타깃 피벗 기준"으로 베이크되므로, 피벗이 실제 그립 지점에서 멀면 그만큼 떠 보인다.
→ 그립 지점 근처에 피벗이 있는 오브젝트(또는 빈 자식 앵커)를 타깃으로 지정.

---

## 9. 요약

**위치가 틀린 이유:** XR Hand의 위치가 루트·손목·메시 컨테이너에 나뉘어 있고, 추적 원점 `(0,0,0)` 오프셋이 프리팹/Clone에 그대로 baked 되었기 때문입니다.

**수정 방법:** 타깃 기준으로 베이크(루트 `(0,0,0)` + 본 월드 유지 + 메시 `(0,0,0)`)하고, Clone은 `HandPoseSnapshotRoot`로 보정하며, Attach/프리팹 저장 전 `HandPoseSnapshotValidator`로 검증합니다.

**한 줄 워크플로:** Play → Attach Target 지정 → **Attach to Target** → 콘솔 **검증 OK** 확인.
