# ProjectBS 성능 분석 인계 — 2026-10-22

이 파일을 다른 컴퓨터의 Codex에 첨부하고 아래 요청으로 이어가면 됨.

> 이 인계 문서를 읽고 현재 프로젝트 코드를 확인해서 Unity 성능 개선 설계를 이어가줘. 직접 소스 파일을 수정하지 말고 방법과 수정 대상 파일·줄 번호를 알려줘. 최신 관심사는 길쭉한 유닛의 회전을 반영하면서 사거리 Trigger를 거리 계산으로 대체하는 방법이야.

## 사용자 작업 규칙

- Unity 프로그래밍을 공부 중. 기본적으로 직접 파일을 수정하지 말고 방향·방법을 설명할 것.
- 사용자가 직접 파일 수정을 요청했을 때만 수정 가능. 이번 요청은 이 인계 파일 작성에 대한 허용임.
- 참고하거나 수정할 파일과 줄 번호를 명시할 것.
- 한국어 반말, 불필요한 설명과 토큰 사용 최소화.

## 프로젝트와 원래 문제

- 원래 경로: `C:/JJH/ProjectBS`, Unity `6000.3.5f2`.
- 아래 소스 경로는 저장소 기준 상대 경로. 다른 컴퓨터에서는 그 컴퓨터의 실제 프로젝트 경로에 맞춰 읽을 것.
- 일반 유닛 약 100마리와 궁수 약 100마리로 전투하면 심한 프레임 저하. 전투 종료 후 부하가 급감.
- 처음에는 화살의 Rigidbody 이동을 의심했지만, 후속 실험에서 사거리 감지 Trigger와 관련된 큰 비용을 확인함.
- 사용자 초기 제안: 화살 이동 거리 `speed * deltaTime`을 계산하고, 현재 위치에서 다음 위치까지 Raycast한 뒤 이동/명중 처리.
- 이 화살 설계는 아직 구현하지 않음. 사거리 Trigger 분석으로 우선순위가 이동함.

## 현재 화살 구조

`Assets/3.Scripts/Objects/Characters/Attack/ArrowProjectile.cs`

- 7줄: 기본 속도 `20`.
- 46~47줄: `GameManager.OnPhysicsObject` 구독.
- 82~87줄: 물리 갱신마다 `Rigidbody.MovePosition`으로 직선 이동.
- 103줄 이후: `OnTriggerEnter`에서 캐릭터 판별, 자기 자신/아군 제외, 데미지 처리.
- 89줄 이후: 명중한 타깃 따라가기.
- 128줄 이후: 데미지 적용. 수명과 명중 후 풀 반환 처리도 있음.

`Assets/1.Datas/Origin/Prefabs/Globals/GameObjects/ArrowBasic.prefab`

- 259줄: Kinematic 켜짐.
- 260줄: 보간 꺼짐.
- 264줄 이후: BoxCollider, 278줄: Trigger 켜짐, 280줄: Enabled 설정.
- 마지막 파일 확인 시 소스는 기존 Rigidbody 이동 상태였음. 런타임/Inspector의 현재 Collider 활성 상태는 별도 확인 필요.

## 실험 및 측정 결과

수치는 사용자가 보낸 서로 다른 단일 프레임 스크린샷에서 읽음. 동일한 진행 시점·생존 수의 통제된 평균값은 아님. 부모/자식 샘플 시간은 중복되므로 더하지 말 것.

| 단계 | 물리 갱신 Calls | Physics.FixedUpdate 총 ms | SendTriggerEvents 총 ms | SendTriggerEvents Self ms | OverlapSphereNonAlloc |
|---|---:|---:|---:|---:|---|
| 초기 | 17 | 266.14 | 195.79 | 171.08 | 2,386회 / 30.38ms |
| 충돌 매트릭스 조정 후, 검색 주기 미변경 | 17 | 266.35 | 182.10 | 174.55 | 2,440회 / 30.55ms |
| 다음 캡처 ※ | 16 | 275.87 | 198.86 | 144.96 | 2,704회 / 26.25ms |
| TryGetComponent 적용 후로 해석되는 캡처 | 15 | 미제공 | 140.04 | 139.57 | 미제공 |
| 두 종류 유닛의 AttackRangeRoot Collider 모두 끔 — 사용자 명시 | 3 | 미제공 | 3.83 | 3.83 | 미제공 |

※ 이 시점에 화살 BoxCollider를 끄고 비교하라고 제안했고 사용자는 캡처를 보냈으나, 실제로 껐다는 설명은 없었음. 이전 응답도 조건부로 해석함. 화살 Collider 해제 실험이 확정적으로 수행됐다고 단정하지 말 것.

### 충돌 매트릭스 변경

- 사용자는 원래 AttackRange가 여러 Layer와 충돌하도록 돼 있었고, 분석 이후 Character만 충돌하도록 바꿨다고 명시함.
- 처음부터 Character만 감지하던 것이 아님. 이전 대화에서 이를 잘못 이해했다가 정정함.
- 매트릭스 조정만으로는 큰 병목이 없어지지 않음.

### GetComponent 검색 실패 비용 확인 및 수정

`SendTriggerEvents` 하위 콜백을 펼친 캡처:

- `AttackRangeModule.OnTriggerEnter`: 1,388회, 총 39.76ms.
- 그 아래 `EditorOnly [GetComponentNullErrorMessage]`: 1,194회, 38.17ms.
- `AttackRangeModule.OnTriggerExit`: 1,188회, 총 14.14ms.
- 그 아래 `EditorOnly [GetComponentNullErrorMessage]`: 1,122회, 13.24ms.
- 검색 실패 관련 에디터 처리만 합계 약 51ms.
- Character Layer에 속해도 같은 GameObject에 CharacterBase가 있다고 보장되지 않음. 자식/뼈 Collider 등 가능. 정확히 어떤 Collider가 실패를 일으켰는지는 아직 추적 안 함.

사용자가 `AttackRangeModule`의 GetComponent를 TryGetComponent로 변경했고, 파일에서도 확인함.

`Assets/3.Scripts/Objects/Characters/CharacterModules/AttackRangeModule.cs`

- 22줄: OnTriggerEnter.
- 26~27줄:

```csharp
if (!other.TryGetComponent<CharacterBase>(out var target)
    || target == Owner) return;
```

- 31줄: OnTriggerExit.
- 33줄:

```csharp
if (!other.TryGetComponent<CharacterBase>(out var target)) return;
```

다음 캡처에서 Enter 216회 / 0.18ms, Exit 248회 / 0.27ms, SendTriggerEvents GC 0B로 확인됨. 프레임별 이벤트 수가 달라 정확한 순수 개선율은 산출 불가하지만, 해당 에디터 검색 실패 샘플은 사라짐. Trigger 자체 비용 약 140ms는 여전히 남았음.

### 사거리 Collider 해제 실험

- 사용자가 소환한 두 종류 유닛 모두 AttackRangeRoot의 Collider를 껐다고 명시함.
- Trigger 자체 비용/물리 갱신 1회: 약 `139.57 / 15 = 9.30ms` → `3.83 / 3 = 1.28ms`.
- 물리 갱신 반복도 15회 → 3회로 감소.
- 사거리 Trigger 관련 부하가 주요 병목이라는 강한 근거.
- 단, 사거리 감지를 끄면 공격 진입도 막히므로 공격 애니메이션·화살 생성 감소 영향도 섞일 수 있음. 정상 공격을 유지하는 대체 구현 후 재측정해야 최종 효과를 판단할 수 있음.
- 물리 Calls 15~17은 해당 프레임에서 물리 갱신을 반복한 횟수이며, 충돌/Trigger 쌍의 개수가 아님.

## 사거리 Trigger 대체 설계 — 아직 미구현

1. 현재 타깃 한 명의 사거리 확인은 매 AI 갱신마다 거리/몸통 외곽 거리 계산으로 처리.
2. 현재 타깃이 범위 밖일 때 다른 사거리 내 적을 찾는 검색은 약 0.1~0.2초마다 실행하고 유닛별 시점 분산.
3. 기존 TargetingModule의 적 목록 캐시를 활용. 새 OverlapSphere 검색을 무조건 추가하지 말 것.
4. 기존의 '현재 타깃이 범위 밖이어도 가까운 다른 적이 사거리 안이면 공격' 동작을 유지할지 명확히 고려.
5. TargetingModule.ScanClosestTarget에는 공격 슬롯 예약 가능 여부 필터가 있으므로, 기존 사거리 내 타깃 선택과 완전히 같은 함수라고 가정하지 말 것. 적 캐시 공유와 선택 정책 재사용은 구분할 것.

참고 파일:

- `Assets/3.Scripts/Objects/Characters/CharacterModules/AttackRangeModule.cs:37` — Contains, 현재 HashSet 포함 여부 사용.
- 같은 파일 `:42` — TryGetClosestTarget, 현재 Trigger로 모은 목록에서 중심 거리 비교.
- `Assets/3.Scripts/Objects/Controllers/HostileAIController.cs:167` — 현재 타깃 사거리 확인.
- 같은 파일 `:173` — 사거리 내 다른 적으로 타깃 전환.
- `Assets/3.Scripts/Objects/Characters/CharacterModules/TargetingModule.cs:42` — 적 CharacterBase 목록 캐싱.
- 같은 파일 `:92` — 가장 가까운 타깃 검색.
- 같은 파일 `:14`, `:154` — scanInterval 0.2초, 실제 대기 시간은 Random.Range(0.05f, scanInterval).
- `Assets/1.Datas/Origin/Prefabs/Globals/3DUnits/Humans/M_Archer.prefab:1743` — 사거리 SphereCollider Enabled.
- 같은 파일 `:1745` — 사거리 SphereCollider 반경 8.

## 최신 질문: 긴 유닛의 방향에 따른 거리 판정

사용자 설명:

- 보통 유닛은 원형이라 UnitStatus.colliderRadius에 기본 Collider 반지름을 저장해 거리 계산에 사용 중.
- 가로로 긴 유닛은 반지름 하나로 처리하면 바라보는 방향에 따라 거리 판정이 어긋남.
- 예시 그림: 원형 유닛 둘과 기울어진 긴 타원형 유닛 하나. 중심 간 거리만으로는 실제 몸통 사이 거리를 표현하기 어려움.

제안한 선택지 두 가지. 아직 사용자가 선택하거나 구현을 요청하지 않음.

### A. 기존 몸통 Collider의 ClosestPoint 활용

- 원형 공격자 중심에서 대상 몸통 Collider의 가장 가까운 점을 구해 거리 비교.
- 대상의 회전과 실제 Collider 모양 반영.
- 사거리 감지용 Trigger는 제거/해제해도 되지만, 조회할 몸통 Collider는 활성 상태여야 함.
- 비활성 Collider의 ClosestPoint는 입력점을 반환하므로 그대로 사용하면 잘못된 0거리 판정 위험.
- Collider.ClosestPoint는 bounds.ClosestPoint와 다름. 후자는 회전한 실제 몸통의 정확한 외곽을 표현하지 않음.
- 이 방식도 물리 API를 조회하지만, 전체 Trigger 쌍을 유지하고 이벤트를 받는 설계와는 다른 비용 구조. 성능 수치는 측정 필요.
- 원형 공격자 예시: `(nearestPoint - attackerCenter).sqrMagnitude <= (weaponReach + attackerRadius)^2`.
- 위 weaponReach는 '공격자 몸통 밖으로 닿는 거리'로 정의했을 때의 식. 이미 몸통 크기를 포함한 기존 사거리라면 반지름을 중복해서 더하면 안 됨.
- 위 식만으로 양쪽 모두 긴 유닛의 몸통 간 최단 거리가 해결되는 것은 아님.
- ClosestPoint는 3D 거리 조회. XZ 평면 판정으로 옮길 때 기준 높이를 포함한 처리 검토 필요. 결과의 y만 지운다고 임의의 3D 형상의 정확한 투영 거리가 되는 것은 아님.

### B. XZ 평면의 논리적 캡슐 몸통으로 근사

- 긴 유닛 몸통을 '중심 선분 A–B + 두께 반지름'으로 표현.
- 원형 유닛은 중심 선분 길이가 0인 경우로 통일 가능.
- 원 ↔ 원: 중심 거리 − 양쪽 반지름.
- 원 ↔ 캡슐: 점과 상대 중심 선분 사이 최단 거리 − 양쪽 반지름.
- 캡슐 ↔ 캡슐: 두 중심 선분 사이 최단 거리 − 양쪽 반지름.
- 몸통 사이 거리는 필요하면 0 이상으로 제한하고, 공격 사거리와 비교.
- A/B를 로컬 좌표로 저장하고 각 인스턴스의 현재 Transform으로 월드 좌표 변환하면 회전 반영.
- 원형 스칼라 반지름으로 타원 자체를 정확히 표현하는 설계가 아니라, 길쭉한 몸통을 캡슐로 근사하는 설계임.
- 원형/긴 유닛 모두에 사거리, 겹침 해소, 접근 슬롯 계산이 같은 몸통 정의를 사용하도록 맞추는 것이 목표.
- 동일 위치/0길이 선분, 겹치는 선분, Transform 스케일 등의 경계 조건은 구현 시 처리할 것.

관련 기존 코드:

- `Assets/3.Scripts/ScriptableObjects/UnitStatus.cs:24` — colliderRadius.
- 같은 파일 `:28` — UpdateRadiusFromPrefab.
- 같은 파일 `:46` — Character Layer의 CapsuleCollider 검색.
- 같은 파일 `:64` — CapsuleCollider.radius에 XZ 최대 스케일을 곱해 colliderRadius 저장. 캡슐 길이 정보는 저장하지 않음.
- `Assets/3.Scripts/Objects/Characters/CharacterBase.cs:72` — 유닛 앵커 영역. 몸통 Collider 참조를 추가한다면 후보 위치. 현재 BodyCollider 같은 속성이 존재한다고 가정하지 말 것.
- `Assets/3.Scripts/Objects/Characters/CharacterModules/MovementModule.cs:189` — 두 반지름 합으로 겹침 보정 거리 계산.
- `Assets/3.Scripts/Objects/Characters/CharacterModules/AttackSlotModule.cs:125` — 두 반지름 합과 margin으로 공격 슬롯 거리 계산.
- `Assets/3.Scripts/Objects/Controllers/HostileAIController.cs:34`, `:72` — 자기/대상 반지름 캐싱. 영향 범위 함께 확인.

## OverlapSphereNonAlloc 최적화 제안 — 아직 미적용

- 위치: `Assets/3.Scripts/Objects/Characters/CharacterModules/MovementModule.cs:152`.
- 아군 겹침 보정을 위해 Character Layer 검색, Trigger 제외.
- `:95`에서 물리 이동 갱신마다 ApplyFriendlySeparation 호출.
- NonAlloc은 결과 배열 할당을 줄이는 기능이지 검색 계산이 무료라는 뜻은 아님.
- 기본 물리 주기 0.02초에서 검색을 0.1초로 늘리면 시간당 호출량은 이론상 약 80% 감소. 전체 프레임 비용이 80% 줄어든다는 뜻은 아님.
- 권장: 후보 목록 검색만 0.1초마다, 저장된 후보의 현재 위치를 통한 겹침 계산과 실제 밀어내기는 매 물리 갱신 유지.
- `:230`의 navAgent.Move까지 0.1초마다 호출하면 보정 속도가 줄거나 누적 시간 사용 시 움직임이 튈 수 있음.
- 초기 검색 타이머를 0~0.1초로 분산해 동시에 검색하지 않도록 제안.
- 0.1초에서 반응이 늦으면 0.06초 등을 시험. 사거리 감지 개선 후 필요성을 다시 판단.

## 화살 Raycast 설계 시 놓치지 말 것

- 현재 화살촉 위치 → 다음 화살촉 위치의 전체 이동 구간 검사. 도착점만 검사하지 말 것.
- 처음에는 매 프레임 검사/이동으로 구현하고, 낮은 검사 주기와 렌더 보간은 필요할 때 별도 도입.
- 단일 Raycast가 아군을 맞힌 뒤 결과만 버리면 같은 구간 뒤의 적을 놓칠 수 있음. 팀 LayerMask 또는 재사용 버퍼의 다중 결과에서 가장 가까운 유효 충돌 선택 필요.
- 화살 두께가 필요하면 작은 SphereCast 고려.
- 타깃 몸통 Collider는 유지할 수 있지만 화살 자체 Rigidbody/Collider는 전환 의도에 맞게 정리.
- 명중 처리, 타깃에 박힌 뒤 따라가기, 풀 반환 등 기존 동작 재사용.
- 초기 성능 문제를 '화살 물리만의 문제'로 확정하지 말 것. 현재 가장 뚜렷한 증거는 사거리 Trigger 해제 시 부하 감소임.

## 자료와 분석 한계

- 원본 캡처: `ProfilerCaptures/ProjectBS_2026-10-22_13-48-56.data`, 2,146,209,252 bytes.
- Unity 바이너리 캡처 내부는 직접 분석하지 못함. 당시 Unity 창 자동화가 capture timeout으로 두 번 실패함.
- 위 수치는 사용자 제공 스크린샷에서 확인한 값임. 다른 컴퓨터에서 원본 .data를 활용하려면 파일도 별도 복사해야 함.
- 첨부 이미지들은 원래 컴퓨터의 Temp 폴더에 있었음. 이 Markdown에 이미지를 포함하지 않았지만 핵심 수치와 설계를 텍스트로 정리함.
- 현재 Inspector/런타임 Collider 상태와 로컬 파일의 저장 상태는 재확인 필요.
- 줄 번호는 인계 시점 기준이며 후속 변경 시 달라질 수 있음.
- 이 대화에서 Codex가 프로젝트 소스를 직접 수정하지는 않았음. 사용자가 TryGetComponent 수정과 Inspector 실험을 수행함. 이번에 생성한 것은 이 인계 Markdown뿐임.

## 확인한 Unity 공식 문서

- [물리 갱신이 밀릴 때 반복 실행되는 현상](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-performance-issues.html)
- [Profiler Total / Self / Calls 의미](https://docs.unity3d.com/6000.3/Documentation/Manual/ProfilerCPU.html)
- [TryGetComponent: 검색 실패 시 에디터 할당 회피](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Component.TryGetComponent.html)
- [Collider.ClosestPoint](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider.ClosestPoint.html)
- [OverlapSphereNonAlloc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapSphereNonAlloc.html)
- [Physics.Raycast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.Raycast.html)
