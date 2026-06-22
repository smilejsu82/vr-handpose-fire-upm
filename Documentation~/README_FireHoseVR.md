# VR 소화전 / 소방 호스 체험 — 셋업 가이드

특별한 모델링 메시 없이 Unity 기본 도형 + LineRenderer + XR Interaction Toolkit으로
"노즐 잡기 → 밸브 열기 → 물 분사 → 화재 진압" 흐름을 구현하는 프로토타입.

- 네임스페이스: `FireSafetyVR`
- 검증 환경: Unity 6, URP 17.4, XR Interaction Toolkit 3.4.1
- 스크립트 위치: `Assets/Scripts/FireHose/`

> 코드는 모두 준비돼 있습니다. 아래는 **에디터에서 씬을 조립하는 절차**입니다.
> 순서대로 따라하면 5~10분 안에 1차 프로토타입이 동작합니다.

---

## 0. 스크립트 목록

| 파일 | 단계 | 역할 |
|---|---|---|
| `SimpleFireHose.cs` | 1차 | LineRenderer 호스 (시작점↔노즐, sin 처짐) |
| `FireHoseNozzle.cs` | 1차 | 물 분사 제어 + Raycast 소화 판정 |
| `FireTarget.cs` | 1차 | 불 HP 관리, 0이면 꺼짐 |
| `FireHydrantSystem.cs` | 1차 | 밸브 상태 + 트리거 입력 → 노즐 전달 |
| `HoseBuilder.cs` | 2차 | Rigidbody 세그먼트 체인 호스 |
| `HosePointRenderer.cs` | 2차 | 물리 세그먼트를 LineRenderer로 표시 |

---

## 1. 씬 하이어라키 구성

빈 GameObject들로 아래 구조를 만든다. (Hierarchy 우클릭 → Create Empty / 3D Object)

```
FireHydrantSystem        (빈 오브젝트, FireHydrantSystem.cs)
├── FireHydrantBox       (빈 오브젝트)
│   ├── Cube_Back        (Cube)        소화전 뒷판
│   ├── Cube_Door        (Cube)        소화전 문
│   ├── Cylinder_Reel    (Cylinder)    호스 릴
│   ├── Cylinder_Valve   (Cylinder)    밸브 손잡이
│   └── HoseStartPoint   (빈 오브젝트) 호스 시작 위치
├── HoseRenderer         (빈 오브젝트, LineRenderer + SimpleFireHose.cs)
├── Nozzle               (빈 오브젝트, Rigidbody + XR Grab + FireHoseNozzle.cs)
│   ├── Cylinder_Body    (Cylinder)
│   ├── Cylinder_Grip    (Cylinder)
│   ├── Cone_Tip         (Cylinder을 가늘게 / Capsule)
│   ├── WaterParticle    (Particle System)
│   └── ShootPoint       (빈 오브젝트, +Z가 분사 방향)
└── FireTarget           (빈 오브젝트, FireTarget.cs)
    └── FireVisual        (Particle System 또는 발광 도형)
```

### 1-1. FireHydrantBox (소화전 박스)
- `Cube_Back`: Scale 약 (0.4, 0.8, 0.2), 빨간 Material.
- `Cube_Door`: Cube_Back 앞면에 얇게 (0.38, 0.78, 0.02).
- `Cylinder_Reel`: 옆으로 눕혀 릴처럼 (Rotation X=90, Scale 0.3,0.05,0.3).
- `Cylinder_Valve`: 밸브 손잡이. 작게.
- 콜라이더는 체험에 불필요하면 제거해도 됨(노즐 물리 간섭 방지).

### 1-2. HoseStartPoint
- `FireHydrantBox` 자식 빈 오브젝트.
- 릴/밸브 근처, 호스가 나오는 위치에 배치.

---

## 2. Nozzle (노즐) 설정 — 가장 중요

### 2-1. 외형
- `Nozzle` (빈 오브젝트) 아래에 Cylinder_Body / Cylinder_Grip / Cone_Tip 배치.
- **자식 도형들의 Collider는 모두 제거**하고, 잡기용 Collider는 `Nozzle` 루트에 하나만 둔다(아래).

### 2-2. Nozzle 루트 컴포넌트
1. **Rigidbody**
   - Mass `0.5`
   - Linear Damping(Drag) `0.2`
   - Angular Damping `0.5`
   - Interpolate `Interpolate`
   - Collision Detection `Continuous Dynamic`
   - Use Gravity: 체험 중 바닥에 떨어뜨리고 싶지 않으면 끔.
2. **Collider** (BoxCollider 또는 CapsuleCollider) — 손으로 잡을 영역.
3. **XR Grab Interactable**
   - Movement Type: `Velocity Tracking`
   - Track Position: ✅
   - Track Rotation: ✅
   - Throw On Detach: ❌ (false)
4. **FireHoseNozzle.cs**
   - `Shoot Point` → 자식 `ShootPoint` 드래그
   - `Water Particle` → 자식 `WaterParticle` 드래그
   - `Range` 8, `Extinguish Power` 10
   - `Hit Mask` → 불에 사용할 레이어 선택(예: Default 또는 "Fire" 레이어)

### 2-3. ShootPoint
- 노즐 끝에 빈 오브젝트. **파란 Z축(forward)이 물이 나갈 방향**을 향하게 회전.
- Raycast와 파티클 방향의 기준.

---

## 3. WaterParticle (물 파티클) 설정
- `Nozzle/ShootPoint` 근처에 Particle System 생성.
- 권장값:
  - Start Lifetime `0.4`, Start Speed `8~12`, Start Size `0.03~0.06`
  - Shape: Cone, Angle 작게(5~10°) → 물줄기 느낌
  - Color: 하늘색/흰색, 끝부분 투명
  - **Looping ✅ / Play On Awake ❌** (스크립트가 Play/Stop 제어)
  - Max Particles 적당히(200 이하, Quest 성능)
- 이 Particle System을 `FireHoseNozzle.Water Particle`에 연결.
- 파티클이 없어도 코드는 에러 없이 판정만 동작한다.

---

## 4. HoseRenderer (LineRenderer 호스) 설정
1. `HoseRenderer` 빈 오브젝트에 **LineRenderer** 추가.
   - Width `0.06 ~ 0.1`
   - Corner Vertices `6~8`, End Cap Vertices `6~8`
   - Texture Mode `Tile`
   - Material: 빨강/검정 고무 느낌의 단순 Material (URP/Lit 또는 Unlit)
   - Use World Space ✅ (스크립트가 월드 좌표를 넣음)
2. **SimpleFireHose.cs** 추가
   - `Hose Start` → `HoseStartPoint`
   - `Nozzle` → `Nozzle`
   - `Line Renderer` → 자동 연결됨(같은 오브젝트)
   - `Segment Count` 24, `Sag Amount` 0.3

---

## 5. FireTarget (불) 설정
1. `FireTarget` 빈 오브젝트에 **FireTarget.cs** 추가.
2. **Collider 필요** — Raycast가 맞아야 함. (예: 발광 Sphere에 SphereCollider)
   - 콜라이더가 있는 오브젝트가 `FireTarget` 자신이거나 그 자식이면 됨
     (스크립트는 `GetComponentInParent<FireTarget>()`로 찾음).
   - 콜라이더 레이어를 노즐의 `Hit Mask`와 일치시킬 것.
3. 필드 연결
   - `Max Hp` 100
   - `Fire Particle` → 불 Particle System (선택)
   - `Fire Visual Root` → 불 비주얼 루트 GameObject (선택)
   - 둘 다 비워도 HP 로직은 동작(시각효과만 없음).
4. `On Extinguished` UnityEvent에 사운드/점수 등 연결 가능(선택).

---

## 6. FireHydrantSystem (밸브 컨트롤러) 설정
1. 최상위 `FireHydrantSystem` 오브젝트에 **FireHydrantSystem.cs** 추가.
2. `Nozzle` 필드 → `Nozzle` 오브젝트(FireHoseNozzle) 드래그.
3. 초기 `Valve Opened`는 ❌(닫힘)로 두면 테스트 시나리오대로 동작.

---

## 7. VR 입력 연결 (UnityEvent 방식)

코드에 입력을 하드코딩하지 않았으므로 Inspector에서 이벤트로 연결한다.

### 7-1. 노즐 트리거 → 분사
`Nozzle`의 **XR Grab Interactable** 컴포넌트에서:
- **Activated** 이벤트 (+) → `FireHydrantSystem` 드래그
  → 함수: `FireHydrantSystem.SetTriggerPressed`, 체크박스 **✅ true**
- **Deactivated** 이벤트 (+) → `FireHydrantSystem` 드래그
  → 함수: `FireHydrantSystem.SetTriggerPressed`, 체크박스 **❌ false**

> Activate/Deactivate는 노즐을 잡은 손의 트리거를 당겼다 뗄 때 발생.
> 밸브가 닫혀 있으면 SetTriggerPressed(true)가 와도 물은 안 나온다(코드에서 보장).

### 7-2. 밸브 조작 → 열기/닫기
밸브를 어떻게 조작하느냐에 따라:
- **밸브에 XR Grab/Interactable 또는 버튼을 둔 경우**: 그 이벤트(Select Entered 등)에서
  → `FireHydrantSystem.ToggleValve` 연결.
- **별도 버튼/포크 인터랙션**: 동일하게 `OpenValve` / `CloseValve` / `ToggleValve` 중 선택.
- 기존 프로젝트의 `PushButtonPokeLogger` 같은 버튼 이벤트가 있다면 거기에 `ToggleValve`를 추가해도 됨.

### 7-3. (테스트용) 키보드로 밸브 열기
VR 장비 없이 에디터에서 확인하려면, 임시로 아무 오브젝트에서
`FireHydrantSystem.ToggleValve`를 호출하는 작은 테스트 스크립트나
버튼 UI를 붙여 확인할 수 있다.

---

## 8. 1차 프로토타입 테스트 순서
1. Play 실행.
2. VR 컨트롤러로 **Nozzle**을 잡는다(XR Grab).
3. 노즐을 당기면 **LineRenderer 호스**가 따라오고 가운데가 처진다.
4. **밸브가 닫힌 상태**에서 트리거를 당겨도 물이 **안 나온다**.
5. 밸브를 연다(`ToggleValve`/`OpenValve`).
6. 트리거를 당기면 **물 파티클**이 분사된다.
7. **FireTarget**을 조준하면 HP가 감소한다(Inspector에서 `Current Hp` 확인).
8. HP 0 → 불 파티클 Stop 또는 비주얼 비활성화.
9. Console 에러 없음 확인.

### 빠른 점검 체크리스트
- [ ] ShootPoint의 Z축이 노즐 앞을 향한다.
- [ ] FireTarget에 Collider가 있고 레이어가 Hit Mask에 포함된다.
- [ ] WaterParticle은 Looping ✅ / Play On Awake ❌.
- [ ] HoseRenderer LineRenderer가 World Space ✅.
- [ ] Nozzle 자식 도형들의 불필요한 Collider 제거(잡기/물리 간섭 방지).

---

## 9. 2차 — 물리 호스 적용 방법 (선택)
1차가 안정적으로 동작한 뒤에만 진행.

1. `HoseRenderer`(또는 새 오브젝트)에서 `SimpleFireHose`를 끄고,
   대신 **HosePointRenderer.cs**를 사용한다(LineRenderer 필요).
2. 빈 오브젝트에 **HoseBuilder.cs** 추가:
   - `Start Anchor` → `HoseStartPoint`
   - `Nozzle Rigidbody` → `Nozzle`의 Rigidbody
   - `Segment Count` 25 (Quest 권장 20~30 이하)
   - `Segment Length` 0.15
   - `Build On Start` ✅ (또는 런타임에 `Build()` 호출)
   - `Segment Prefab`은 비워두면 Capsule Primitive를 자동 생성.
3. **HosePointRenderer**의 `Source Builder`에 위 HoseBuilder를 연결하면
   생성된 세그먼트를 따라 LineRenderer가 그려진다.
4. 동작/안정성:
   - 첫 세그먼트는 kinematic으로 앵커에 고정, 이후 CharacterJoint로 연결.
   - 마지막 세그먼트는 노즐 Rigidbody에 CharacterJoint로 연결.
   - 불안정하면 Segment Count↓, Mass↑, Solver Iteration↑(Project Settings > Physics)로 튜닝.

---

## 10. Quest 2 / 독립형 VR 성능 주의사항
- **LineRenderer 호스를 우선** 사용. 물리 호스는 선택.
- 물리 세그먼트는 **20~30개 이하**. 많을수록 Joint 계산 비용 급증.
- Raycast는 **분사 중일 때만** 실행됨(코드에 반영).
- 실시간 Tube Mesh 생성/유체 시뮬레이션은 사용하지 않음.
- Particle Max Particles를 낮게 유지, 충돌(Collision) 모듈은 끄는 것을 권장.
- 노즐 Rigidbody Collision Detection은 Continuous Dynamic이 비싸므로,
  성능이 부족하면 Continuous 또는 Discrete로 낮춰 테스트.

---

## 11. 동작 원리 요약
- 분사 조건: `밸브 열림(valveOpened) && 트리거 눌림(triggerPressed)`
  → `FireHydrantSystem`이 계산해 `FireHoseNozzle.SetSpray()`로 전달.
- 소화: 분사 중에만 `ShootPoint.forward`로 Raycast →
  `FireTarget` 히트 시 `extinguishPower * Time.deltaTime`만큼 HP 감소.
- HP ≤ 0 → `FireTarget.OnExtinguished()` 1회 실행(중복 방지 플래그).

모든 스크립트는 참조가 비어 있어도 에러 대신 조용히 return하거나
`Debug.LogWarning`으로 알린다.
