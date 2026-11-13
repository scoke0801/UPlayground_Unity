# Unity Hot Reload — 설계 · 구현 · 검증

프로젝트 연결: [분석·작업 상태](../../work-status/unity-hot-reload.md) · [개발과 검증 위키](../../wiki/development.md). 실제 진행·검증 결과는 별도 상태 문서에서 관리한다.

**UPlayground 적용 결정(2026-11-12, 설계 보정):** 아래 본문의 제안 경로·방식 중 이 프로젝트에서 다음과 같이 확정했다. 근거는 ADR에 있다.
- 컴파일 컨텍스트는 Bee 응답 파일(rsp)을 Roslyn 명령줄 파서로 재현한다(어댑터 `Unity6000.3-BeeRsp`). 기준선 parity는 MVID가 아니라 W0↔로드된 A0의 전체 표면·본문 지문 일치로 판정한다 — [ADR 0001](../../work-status/unity-hot-reload/adr/0001-scope-and-backend.md).
- Harmony는 병합본이 Burst 해싱을 깨뜨려 `Lib.Harmony.Thin` 2.3.6 + MonoMod netstandard 빌드로 고정했다 — [ADR 0002](../../work-status/unity-hot-reload/adr/0002-backend-proof.md).
- Worker 요청 취소는 자신이 시작한 Worker 종료 + 늦은 결과 폐기로 구현한다. Unity Mono의 FileSystemWatcher는 폴링형이므로 실효 감지 지연이 debounce보다 길다.
- `Docs/HotReload/`·`progress.json`·`evidence/` 제안 경로는 `docs/work-status/unity-hot-reload/`로 대체한다.

문서 v1.0.0 · 2026-11-12 · Claude / Codex 공통 전달 문서.

13개 필수 단계 · 45개 필수 테스트 · 3개 실제 프로젝트 smoke. 실제 Unity 구현/검증은 이 문서 작성 과정에서 수행하지 않았다.


<a id="readme"></a>

## 이 문서로 작업을 시작하는 방법

Unity의 Play Mode를 유지하면서 **이미 존재하는 객체의 메서드 본문만 교체**하는 Editor 전용 개발 도구를 완성한다. 상용 Hot Reload의 공개 사용 경험을 참고하되, 비공개 구현을 복제하지 않는다.

목표 결과

### Project Hot Reload v1

소스 수정 → 별도 컴파일 → 검증 → 패치 → 상태 보존 → 정상 컴파일 복귀까지 연결된 도구.

작업 방식

### G0 → G12

매 단계에 구현 산출물, 실제 테스트, 통과 조건을 둔다. UI나 빈 클래스만 만들고 끝내지 않는다.

현재 상태

### 설계 / 미실행

이 문서는 구현 지시서다. Unity 저장소 변경·컴파일·Play Mode·성능 검증은 아직 수행하지 않았다.

### 권장 전달 순서

1. 이 HTML 또는 동봉된 `DESIGN_SPEC.md`를 프로젝트 작업 에이전트에게 제공한다.
2. [전체 수행 요청문](#master-request)을 붙여 넣고 G0부터 시작한다. 실제 프로젝트 경로는 에이전트의 현재 작업 디렉터리에서 확인한다.
3. 중단된 세션은 [재개 요청문](#resume-request)으로 이어 간다. `progress.json`과 증거 파일을 기준으로 재개하며, 대화 기억만 신뢰하지 않는다.
4. 최종적으로 다른 에이전트에 [독립 검수 요청문](#review-request)을 전달한다. 동일 파일을 두 에이전트가 동시에 수정하지 않는다.

**완료의 뜻**

**G0~G12의 필수 범위를 구현하고 실제 Unity에서 통과 증거를 남기는 것**이다. 실행 환경이 없으면 구현 가능 부분까지 끝내고 `IMPLEMENTED_UNVERIFIED`로 보고한다. 테스트를 실행하지 않고 “완료”라고 쓰지 않는다.

문서 기준일: 2026-11-12 (Asia/Seoul). 예시의 파일명·네임스페이스·수치 목표는 제안값이다. 실제 저장소에서 확인된 이름과 버전이 우선한다. 웹 사실은 [Sxx], 나머지 요구사항과 알고리즘은 이 문서의 설계 결정이다.

<a id="scope"></a>

## 목표·전제·종료 범위

### 1.1 프로젝트 전제

기존 대화의 Unity 6 기반 액션 RPG/TPS 프로젝트를 적용 대상으로 삼는다. Animancer 기반 애니메이션, 이동·전투·카메라 로직과 여러 asmdef가 존재할 가능성이 높지만, **실제 클래스 이름과 현재 패키지 구성을 이 문서가 확인한 것은 아니다.** 최근 작업 맥락에는 Unity 6000.3.21f1 / URP 17.3.0이 언급되었고 과거에는 6000.0.60f1을 사용했다. 어느 값도 하드코딩하지 말고 G0에서 저장소와 실행 중 Editor를 대조한다.

### 1.2 v1의 보장 경계

**실제로 확인한 Unity Editor + OS/CPU 조합**에서, 허용 목록에 등록하고 사전에 일반 컴파일한 메인 스레드용 비제네릭 클래스 메서드를 지원한다. 첫 플랫폼은 현재 개발 PC를 우선한다. 다른 OS·CPU는 해당 환경의 테스트를 통과하기 전까지 미지원이다. 단일 플레이 세션에서 기존 객체의 필드와 참조를 유지하는 것이 목표이며, 게임 상태의 과거 시점 복원은 목표가 아니다.

| 필수 완료 범위 | v1에서 제외할 범위 |
| --- | --- |
| 기존 인스턴스/static 메서드, private/internal/public 접근, 오버로드, 여러 asmdef, partial 파일의 본문 수정 | 필드·타입·시그니처·상속 관계 변경, 새로운 메서드/타입 추가, 생성자·static 초기화 변경 |
| 올바른 원본 타입 재연결, 일반 분기·루프·기존 API 호출, 지정된 예외 처리 지원, 코드 롤백 | async/iterator/람다/로컬 함수 등 컴파일러 생성 코드를 포함한 메서드의 변경 |
| 파일 감지, 외부 컴파일러, 검증기, 원자적 적용 시도, 실패 복구, UI, 에이전트 연결, 로그, 테스트 | Burst/Jobs/Entities 생성 코드, IL2CPP 또는 배포 Player 패치, Native 함수, 임의 Editor 내부 코드 패치 |
| 원본 소스 기반 정상 컴파일 및 Player 빌드에서 도구 의존성 제외 확인 | 상용 제품과 100% 기능 동등성, 완전한 디버거 소스 재매핑, 임의 멀티스레드 실행 중 패치 |

**속도를 위해 정확성을 희생하지 않는다**

v1은 **변경이 포함된 어셈블리 전체를 외부 프로세스에서 컴파일**할 수 있다. Unity 전체 스크립트 컴파일/Domain Reload를 피하는 것이 1차 가치이며, 처음부터 “변경 메서드만 수 ms 컴파일”을 약속하지 않는다.

### 1.3 금지 사항

상용 패키지의 비공개 DLL·서버·라이선스 검증 로직 추출/복제, 인증 우회, 출처 불명 바이너리 사용을 작업에 포함하지 않는다. 기존 사용자 변경을 reset/stash/delete하지 않으며, 엔진 업그레이드·대규모 asmdef 재편·전역 Editor 설정 변경을 조용히 수행하지 않는다. 기본 구현이 불가능한 경우 기능을 몰래 축소하지 말고 재현 사례와 대안을 기록한다.

### 1.4 최종 판정

`VERIFIED_V1`은 G0~G12 필수 항목이 모두 PASS일 때만 쓴다. 코드만 준비되면 `IMPLEMENTED_UNVERIFIED`, 실행했으나 실패하면 `FAILED_VALIDATION`, G1에서 핵심 패치 경로가 성립하지 않으면 `BACKEND_BLOCKED`다. “대부분 구현”이나 “컴파일될 것으로 보임”은 완료 판정이 아니다.

<a id="evidence"></a>

## 앞선 검토에서 구체화·보정한 내용

| 검토 항목 | 이 문서의 정확한 취급 |
| --- | --- |
| “컴파일 없이” | 제품 표현과 달리 변경 코드를 컴파일하는 단계는 존재한다. 본 설계는 별도 컴파일과 Unity의 일반 리로드를 구분한다. [[S02]](#source-02) |
| Roslyn + Harmony면 완성 | 두 라이브러리만 연결해서는 타입 정체성, private 접근, IL 토큰, 인라이닝, 롤백이 해결되지 않는다. 이 부분을 G1/G4/G5의 핵심 산출물로 지정한다. [[S15]](#source-15) [[S16]](#source-16) [[S17]](#source-17) |
| 변경한 .cs 파일 하나만 컴파일 | partial, 같은 어셈블리 내부 멤버, using 및 참조 설정을 놓칠 수 있다. v1은 전체 컴파일 컨텍스트를 복제하고 부분 캐시만 사용한다. [[S08]](#source-08) [[S09]](#source-09) [[S19]](#source-19) |
| 오픈소스 README의 MIT 표기 | 참고 저장소 README에는 MIT와 Roslyn/Harmony 구조가 명시되어 있다. 이는 파일별 저작권·동봉 DLL·의존성 감사를 대신하지 않는다. 검증 전 통째로 반입하지 않는다. [[S06]](#source-06) |
| 필드 추가도 후반에 쉽게 구현 | 상용 제품에는 별도 필드 지원 기능과 제약이 있다. 기존 객체 레이아웃 문제를 가진 다른 규모의 작업이므로 v1의 필수 범위에서 명시적으로 제외한다. [[S03]](#source-03) [[S04]](#source-04) |
| Auto Refresh를 끄면 안전 | 해당 API는 중첩 카운터이며 명시적 Refresh는 막지 못한다. 소유권 있는 lease, 생명주기 정리, 비상 복구가 필요하다. [[S11]](#source-11) [[S12]](#source-12) |
| 패치 DLL을 계속 로드한 뒤 삭제 | 파일 삭제가 실행 중 어셈블리 언로드를 뜻하지 않는다. CoreCLR의 collectible 로딩 개념을 Unity 런타임에 무조건 적용하지 않는다. [[S21]](#source-21) |
| 공식 MethodReload와 동일 방식 | 공식 소개는 태깅된 진입점과 인터프리터 방식이며 언어/메서드 제약이 있다. 독립 Editor 패치 백엔드와 구분하고, 대안 PoC로만 검토한다. [[S07]](#source-07) |

Harmony 문서는 이 설계에서 **v2 계약**을 기준으로 읽었다. 실제 의존성은 G0/G1에서 버전과 바이너리 해시를 고정한다. 다른 메이저 버전을 고르면 같은 검증을 다시 수행하고 ADR을 변경한다. 문서의 API 예시는 제품 내부 구현을 확인했다는 의미가 아니다.

<a id="capability"></a>

## 지원 매트릭스와 안전한 거부 정책

아래 “필수”는 **구현해야 할 목표**이지 이미 검증된 기능이 아니다. 모든 변경은 소스 분석과 출력 IL 검사 두 관문을 통과해야 한다. 검증기가 해석할 수 없는 경우 허용하지 않는다.

| 변경/상황 | v1 정책 | 판정 및 테스트 |
| --- | --- | --- |
| 기존 비제네릭 참조 타입 메서드 본문 | 필수 | 메인 스레드 + 사전 NoInlining + allowlist. 인스턴스/static, private/internal/public을 실제 테스트. |
| int/float/bool/string/참조/Vector3 반환 | 필수 | 원본 시그니처 불변. Vector3 등 값 타입 반환 ABI는 실제 Unity에서 확인. |
| 여러 overload, partial 타입, 다중 asmdef | 필수 | 완전한 MethodKey로 구분. 현재 .cs 파일만으로 타입을 복제하지 않음. |
| 지역 변수 추가·삭제·형식 변경 | 필수 | 객체 필드 변경과 구별. local signature와 모든 ldloc/stloc/ldloca를 다시 매핑. |
| 기존 private 필드/메서드와 static 상태 접근 | 필수 | 원본 FieldInfo/MethodInfo로 재연결. 생성자/초기화 재실행 금지. |
| 일반 if/switch/loop, 기존 API 호출 | 필수 | 모든 분기 목적지·스택·피연산자 검증. 알 수 없는 opcode는 거부. |
| 기존 닫힌 제네릭 타입/API 호출 | 필수 | 예: List 읽기. 대상 메서드 자체가 제네릭인 것과 구분. 제네릭 인자 재귀 매핑. |
| 기존 API의 out/ref 인자 사용 | 필수 | 예: Raycast의 out 결과. 호출 피연산자와 local address를 보존. 대상 메서드 자체의 ref ABI 지원과 구분. |
| 일반 try/catch/finally, using으로 생성된 finally | 필수 | 중첩 catch/finally 포함 G4의 EH 테스트를 통과해야 허용. filter/fault는 제외. |
| 기존의 명시적 property/indexer 접근자 본문 | 필수 | 실제 get\_/set\_ 메서드로 매칭. auto-property의 필드 구조 변경은 제외. |
| 새 local lambda/local function/async/yield 또는 그 내부 수정 | 거부 | 메서드에 이러한 생성 코드를 포함하면 기존 구조가 비슷해도 v1은 보수적으로 FullCompileRequired. |
| 이미 존재하는 코루틴/비동기 메서드를 단순 호출 | 조건부 필수 | 호출자 본문이 지원 범위이고 기존 MethodInfo를 참조하면 허용. 진행 중 상태머신 자체를 갱신하지 않음. |
| 필드 초기값·const·attribute·signature·타입 추가/삭제 | 거부 | 메서드 본문처럼 보여도 메타데이터/초기화 변화로 분류. 현재 세션 유지 + 명시적 정상 컴파일. |
| 새 메서드, 기존 메서드 삭제/rename | 거부 | 새 코드가 호출 가능한지와 별개로 런타임 표면 변화이므로 v1 전체 배치 거부. |
| 제네릭 메서드/제네릭 선언 타입의 대상 메서드 | 거부 | runtime specialization과 코드 공유를 v1에서 보장하지 않음. [[S17]](#source-17) |
| struct instance 메서드, ref-return, ref/out/in 대상 인자 | 거부 | this byref/ABI 등 추가 검증이 필요. 외부 API 호출의 ref/out 피연산자 처리는 별개. |
| 생성자/소멸자/.cctor, P/Invoke, abstract/extern | 거부 | 원본 객체 생명주기, native 또는 IL 없음 문제. 기존 생성자를 일반 호출하는 것은 검사 후 허용. |
| 허용되지 않은 IL·unsafe·calli·예외 filter/fault | 거부 | 정확한 메서드와 opcode/구문 위치를 함께 보고. 기본 구현으로 대체하거나 조용히 무시하지 않음. |
| Source Generator / IL 후처리 의존 어셈블리 | 조건부 거부 | 컴파일 컨텍스트와 결과 동등성을 재현할 수 없으면 거부. “Unity가 한 번 빌드했으니 된다”는 판단 금지. |
| Burst/Jobs 대상, ECS 생성 코드, worker-thread 호출 | 거부 | 본문에 보이는 일반 C#만으로 패치 가능하다고 판단하지 않음. |
| Editor 자체, 도구 자체, 패키지 캐시, 외부 SDK 수정 | 거부 | 처음부터 프로젝트 소유 게임플레이 어셈블리만 허용. self-hot-reload 금지. |
| 미등록 메서드 수정 또는 인라이닝 보장 없음 | 거부 | 수정 대상/호출 경로가 allowlist 보장 밖이면 FullCompileRequired. 성공 UI만 띄우는 무효 패치 금지. |

**엄격한 배치 정책**

한 배치에 지원되는 변경과 지원되지 않는 변경이 섞이면 **전체 배치를 거부**한다. v1 기본값은 strict session: 감시 범위에서 아직 적용되지 않은 비지원 C# 변경이 남아 있으면 새 패치를 멈춘다. 사용자는 변경을 되돌리거나 정상 컴파일로 기준선을 갱신한다.

<a id="architecture"></a>

## 전체 구조와 선택한 패치 방식

UNITY EDITOR

Compile Context Capture원본 모듈·소스·옵션

↓

Session / Refresh Lease단일 세션·안전한 종료

↓

Watcher + Revision Queue동결된 소스 스냅샷

LOCAL WORKER

Roslyn Compilation변경 어셈블리 전체

↓

PE Metadata ReaderCecil / 실행 로드 없음

↓

Normalized IL + Plan타입 키·분기·예외 영역

EDITOR MAIN THREAD

Runtime Resolver원본 Type / MemberInfo

↓

Harmony Body Transpiler기존 객체의 코드 교체

↓

Audit / Rollback / UI상태·증거·다음 단계

### 4.1 선택: IL 재연결 + 메서드 본문 전체 교체

외부 Worker는 현재 소스의 컴파일 결과를 *실행 대상*이 아니라 *IL 분석 자료*로 사용한다. 컴파일된 게임 어셈블리를 Unity에 `Assembly.Load`하지 않는다. 변경된 메서드의 IL 피연산자를 원본 런타임 타입과 멤버로 해석한 뒤, Harmony transpiler가 해당 본문을 반환한다. 기존 인스턴스의 `this`는 그대로 유지된다.

Harmony transpiler는 호출마다 Roslyn을 실행하는 장치가 아니라 패치 코드 생성 시점의 IL 변환이다. Cecil은 실행용 어셈블리 로드 없이 메타데이터와 IL을 읽을 수 있다. 두 기능을 이 설계에 조합하는 것이며, 이 조합의 Unity 호환성은 반드시 G1에서 입증한다. [[S15]](#source-15) [[S20]](#source-20)

### 4.2 의도적으로 선택하지 않는 방식

| 방식 | 선택하지 않는 이유 |
| --- | --- |
| 새 PlayerController 타입을 로드하고 기존 객체를 캐스팅 | 동일 이름이어도 별도 어셈블리의 타입은 기존 인스턴스와 일치하지 않는다. 상태 복사나 GameObject 재생성으로 감추지 않는다. |
| 메서드마다 Reflection.Invoke / DynamicInvoke 호출 | 호출 비용·박싱·예외 래핑·ref 의미 변화가 생기기 쉽다. 주 경로는 원본 타입을 사용하는 IL 실행으로 설계한다. |
| JIT 함수 포인터를 직접 덮어쓰기 | 플랫폼 ABI·메모리 보호·런타임 차이에 대한 책임을 크게 늘린다. 별도 검증 없이 unsafe 포인터 패치를 구현하지 않는다. |
| Editor에 Roslyn/새 .NET BCL 전부 반입 | 어셈블리 충돌과 재로드 비용을 키운다. 컴파일러·Cecil은 별도 프로세스에 격리한다. |
| Unity 공식 MethodReload를 무조건 기본 백엔드로 교체 | 공식 경로는 별도 동작·제약을 갖는다. 비교 실험은 가능하지만 본 설계의 private/반환형 등 요구를 만족하는지 확인 없이 대체하지 않는다. [[S07]](#source-07) |

### 4.3 필수 사전 장치: NoInlining

v1은 완전한 무수정 drop-in 도구가 아니다. 게임플레이 소유자가 선택한 메서드에 아래 형태의 Editor 한정 속성을 **처음 한 번 일반 컴파일**해 넣는다. 기존 MethodImpl 플래그가 있다면 덮어쓰지 말고 병합한다. 런타임 패치 후에 속성을 추가해 이미 인라인된 호출을 고쳤다고 주장하면 안 된다. Harmony 문서도 인라이닝의 한계를 명시한다. [[S17]](#source-17)

csharp

```
#if UNITY_EDITOR
[System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
#endif
private void UpdateCameraPose()
{
    // 기존 프로젝트의 메서드 본문. 패치 도구 타입을 참조하지 않는다.
}
```

**G1이 설계의 첫 관문**

실제 Unity에서 원본 private 필드 읽기/쓰기, static 상태, 기존 호출자, Vector3 반환, 원복을 먼저 증명한다. 이 관문 실패를 UI 개발로 우회하지 않는다. 지원 라이브러리를 바꾸면 G1부터 다시 검증한다.

<a id="layout"></a>

## 저장소 구조·의존성·설치 계약

text

```
Packages/com.project.hotreload/
  package.json
  Editor/
    Project.HotReload.Editor.asmdef
    Bootstrap/          # 세션, 수명 관리, Editor 이벤트
    Compilation/        # Unity compile context 캡처
    Watching/           # 파일 감시, 스냅샷, revision
    Protocol/           # Editor용 DTO / 검증
    Patching/           # Resolver, IL 변환, Harmony 어댑터
    Recovery/           # journal, lease, 정상 컴파일 전환
    UI/                 # UI Toolkit 창과 설정
    AgentBridge/        # 로컬 파일 명령 / 기존 MCP 어댑터
    ThirdParty/         # 검증된 Editor 전용 의존성 + .meta
  Tests/                # EditMode / Editor 내 PlayMode 테스트
  Samples~/             # 도구 의존성이 없는 샘플과 문서
  Documentation~/
  ThirdPartyNotices.md
Tools/HotReload.Worker/
  src/                  # Roslyn / Cecil / Plan 생성
  tests/
  global.json           # 실제 환경에서 확인한 SDK 고정
  packages.lock.json
Docs/HotReload/
  DESIGN_SPEC.md
  MASTER_REQUEST.md
  progress.json
  Inventory.md
  CompatibilityMatrix.md
  adr/
  evidence/
ProjectSettings/ProjectHotReload.json  # 팀 공유 정책; 기존 설정 형식 존중
UserSettings/ProjectHotReload.json     # 개인 설정; 보통 VCS 제외
Library/ProjectHotReload/              # 세션/스냅샷/프로세스 결과; VCS 제외
  <project-id>/<session-id>/
  bridge/inbox/
  bridge/outbox/
```

디렉터리와 네임스페이스는 제안이다. G0에서 기존 패키지·도구 규칙에 맞춰 매핑한다. 일반 게임플레이 어셈블리는 이 패키지를 참조하지 않는다. Worker의 산출물을 Assets 아래에 두어 Unity가 컴파일하거나 Player에 포함하게 만들지 않는다.

| 의존성 | 반입/고정 규칙 | 실패 시 |
| --- | --- | --- |
| Harmony 및 해당 빌드의 전이 의존성 | 호환 버전·원본 배포처·해시·라이선스 기록. Plugin Importer는 Editor만 허용, 자동 참조를 최소화. 기존 Harmony 사용 여부를 먼저 조사. | 중복 타입/메타데이터 오류가 나면 baseline과 분리해 재현. 새 Burst 오류를 무시하지 않음. |
| Microsoft.CodeAnalysis.CSharp | Worker에만 설치. Unity의 언어/참조/옵션과 맞는 compiler profile을 고정. “현재 최신” 자동 추종 금지. | 동일 입력의 baseline IL 동등성을 증명하지 못하면 해당 대상을 거부. |
| Mono.Cecil | Worker의 PE/IL 읽기에 사용. 이름 충돌을 피하기 위해 Editor로 불필요하게 복사하지 않음. | IL 토큰을 raw 바이트로 Editor에 전달하는 우회 금지. |
| .NET SDK / Worker 실행 파일 | G0에서 OS·CPU·설치 SDK 확인. 배포 방식은 framework-dependent 또는 검증된 self-contained 중 ADR로 고정. | 없는 SDK를 있는 것으로 가정하지 않음. 설치 권한이 없으면 빌드 준비까지 하고 명확한 blocker 기록. |
| 참고 오픈소스 | 코드를 활용할 경우 source URL·정확한 커밋·실제 라이선스·변경 목록을 기록. 읽은 내용만으로 “깨끗한 라이선스” 판정 금지. | 권리 관계/원본 파일을 확인할 수 없으면 독립 구현하고 미확인 코드는 반입하지 않음. |

### 테스트 코드와 Player 경계

실제 프로젝트의 Test Framework 규칙에 맞춰 테스트 어셈블리를 구성한다. Editor API를 참조하는 테스트가 런타임 어셈블리로 잘못 분류되지 않게 한다. 패치 대상 fixture는 도구에 의존하지 않는 일반 C# 코드로 두고, 테스트 실행자가 Editor에서 패치한다. 초기 PoC는 별도 fixture 프로젝트 또는 격리된 worktree를 사용한다. 샘플 scene을 사용자 작업 scene 위에 덮어쓰지 않는다.

최종 빌드는 프로젝트가 원래 사용하던 정상 빌드 경로로 실행한다. 도구 패키지, Harmony, Worker, fixture가 Player 어셈블리·플러그인·배포 파일에 포함되지 않는지 결과 파일과 빌드 로그로 확인한다. Editor-only 폴더 이름만으로 제외를 단정하지 않는다.

<a id="baseline"></a>

## 컴파일 컨텍스트와 세 종류의 기준선

### 6.1 Unity가 컴파일하는 것과 같은 입력을 구성한다

`CompilationPipeline.GetAssemblies`와 해당 어셈블리 정보에서 sourceFiles, defines, 참조, 출력 경로와 옵션을 수집한다. 설치 버전에서 API로 얻지 못하는 정보는 응답 파일·생성된 빌드 입력을 **버전별 어댑터**로 읽고 출처를 기록한다. IDE의 .csproj 하나가 모든 컴파일 설정을 대표한다고 가정하지 않는다. [[S08]](#source-08) [[S09]](#source-09) [[S10]](#source-10) [[S19]](#source-19)

| 캡처 항목 | 반드시 기록할 값 |
| --- | --- |
| RuntimeBaseline | sessionId, Editor PID/start time, domainEpoch, Unity 정확한 버전, OS/CPU, 원본 모듈 MVID·DLL SHA-256, package lock hash |
| AssemblyContext | asmdef GUID/이름, 출력 assembly identity, 모든 source path + content hash, compile defines, editor-only 여부, 직접/전이 참조 |
| CompilerProfile | Roslyn 버전, language version, optimization, nullable, unsafe, checked, deterministic, debug/PDB 모드, rsp 및 analyzer/additional-file 영향 |
| ReferenceIdentity | 참조 파일 canonical path, assembly name/version/culture/public key, 파일 hash. Worker 자체 .NET BCL을 참조 집합에 혼입하지 않음. |
| SourceSnapshot | 모든 관련 파일의 불변 복사본, 인코딩, 프로젝트 상대 경로, source revision, snapshot hash. 원본 파일을 컴파일 중 다시 읽지 않음. 참조 DLL도 해시 검증된 불변 바이트/스냅샷으로 읽음. |
| 후처리 영향 | source generator 출력 및 IL post-processor 존재 여부. 재현/비교 불가한 출력에 의존하는 대상은 거부. |

같은 어셈블리의 소스를 전부 컴파일할 때 **그 어셈블리의 기존 DLL을 자기 자신에 대한 참조로 추가하지 않는다.** 타 어셈블리의 올바른 참조만 사용한다. 여러 변경 어셈블리는 의존성 순서로 컴파일하며, 전체 배치의 메타데이터 호환성 검사가 끝나기 전에는 하나도 패치하지 않는다.

### 6.2 A0 / Eg / Sr를 분리한다

| 기호 | 의미 | 갱신 규칙 |
| --- | --- | --- |
| `A0` / RuntimeBaseline | Unity가 정상 컴파일하여 현재 로드한 원본 타입·메타데이터·코드 | 실제 정상 컴파일/재로드 후에만 변경. 패치 성공으로 A0를 덮어쓰지 않는다. |
| `Eg` / EffectiveGeneration | 현재 실행 코드. A0 위에 적용된 메서드별 본문 맵 | 패치 또는 코드 롤백의 성공이 확인된 후에만 원자적으로 승격. |
| `Sr` / SourceSnapshot | 현재 디스크 소스의 동결된 revision | 저장/재스캔 때 생성. 컴파일 실패나 비지원 변경도 Sr에는 존재할 수 있다. |
| `W0` / WorkerBaseline | A0와 연결된 소스를 동일 Worker profile로 컴파일한 기준 결과 | 초기 컨텍스트 준비 시 생성. 새로운 Worker 컴파일 결과와 비교하는 기준. |

비교는 두 축이다. **구조와 런타임 연결 가능성은 A0에 대해** 검증하고, **이번에 바꿔야 하는 실행 본문은 Eg에 대해** 계산한다. 소스를 원본으로 되돌린 메서드도 감지해야 하므로 “A0와 다른 메서드만 적용”으로 구현하면 안 된다.

text

```
예시
A0: A=original, B=original
E1: A=patch1,   B=original
E2: A=patch1,   B=patch2
S3: A를 원본으로 되돌림, B는 patch2 유지
필요한 결과 E3: A=original, B=patch2
→ A에 RemoveOverride 작업이 필요하다. 변경 없음으로 처리하면 오류다.
```

### 6.3 기준선을 신뢰할 수 있는 조건

세션 시작 전에 마지막 정상 컴파일의 소스 해시와 현재 소스가 대응하는지 확인한다. 추적 기록이 없거나 컴파일 중 파일이 바뀌었다면 Play Mode 안에서 임의의 baseline을 만들지 않는다. 일반 컴파일을 요청하고, 컴파일 시작/종료 동안 입력 해시가 안정적이며 모든 오류가 없고 새 모듈이 로드된 뒤 기준선을 확정한다.

W0와 새 결과는 동일 CompilerProfile로 비교한다. 또한 패치 대상은 W0의 정규화된 본문·시그니처를 실제 A0의 런타임 본문과 비교한다. 디버그 nop 같은 허용된 차이만 정규화하고, 그 외 차이를 “아마 같은 코드”로 넘기지 않는다. IL 후처리나 컴파일 옵션 차이가 남으면 `BaselineParityFailed`다. Unity와 Worker가 생성하는 바이너리 전체 SHA가 같아야 한다는 요구는 아니며, 패치 대상의 동작에 영향을 주는 IL/메타데이터 대응이 요구된다.

### 6.4 변경 범위 탐색

메서드 본문 텍스트만 비교하지 않는다. using 별칭 변경으로 같은 텍스트의 호출 대상이 달라질 수 있으므로 영향을 받은 어셈블리의 정규화된 출력과 symbol binding을 비교한다. const, attribute, 초기화, 상속, 인터페이스, 필드 레이아웃, 메서드 추가/삭제, preprocessor 조건의 변화는 별도 구조 변경으로 다룬다. .asmdef/.asmref/.rsp/manifest/참조 DLL 변경은 컨텍스트를 무효화한다.

<a id="il-backend"></a>

## IL 추출·재연결·본문 교체의 상세 설계

### 7.1 실행용 DLL 대신 의미 있는 IL 계획을 전달한다

Worker가 생성하는 `NormalizedMethodBody`는 기계어도, 기존 모듈에서 그대로 쓸 수 있는 raw metadata token도 아니다. 명령·지역 변수·분기 목적지·예외 영역과 **의미 기반 멤버 식별자**의 집합이다. Editor는 이를 현재 세션의 원본 런타임 객체로 해석한다.

| IR 항목 | 형식 / 불변식 |
| --- | --- |
| MethodKey | AssemblyKey + DeclaringTypeKey + 이름 + generic arity + static/instance + 반환 타입 + 인자 타입/순서/방향/필수 modifier. 단순 이름/인자 개수만 사용 금지. |
| AssemblyKey | 단순 이름만이 아닌 name/version/culture/public-key identity. 원본 모듈 MVID와 파일 해시는 세션 검증용으로 별도 보유. |
| TypeKey | Named, Array(rank), GenericInstance(arguments), ByRef, 필요한 modifier를 재귀 표현. 미지원 pointer/function-pointer/open generic은 거부. |
| MemberKey | 필드/메서드/생성자 구분, 실제 declaring type, signature, closed generic 인자. inherited 검색은 declaring type별로 수행하고 모호한 후보는 거부. |
| Instructions | stable instruction index, opcode, typed operand. 숫자형은 정확한 비트 폭 유지. long/float 특수값의 JSON 변환 손실 방지. |
| Locals | 타입·인덱스·pinned 여부·InitLocals. local slot 번호를 원본 메서드의 local slot과 동일하다고 가정하지 않음. |
| Branches | 대상 instruction id 또는 메서드 끝 sentinel. switch는 id 배열. 짧은 분기는 emit 시 긴 형태로 정규화. |
| ExceptionRegions | try/handler 경계와 catch type, finally, 중첩 관계. filter/fault는 v1 거부. 겹치지만 올바르게 중첩되지 않은 구간은 거부. |
| BodyFingerprint | 정규화된 opcode + 의미 기반 operand + local/EH 구조의 해시. 새 PE의 metadata token 번호나 MVID가 바뀌어도 의미가 같으면 같은 결과. |

### 7.2 반드시 지켜야 하는 재연결 규칙

1. **자기 타입:** 새 PE의 PlayerController TypeDef는 원본의 실제 `System.Type`으로 매핑한다. 새 타입 객체를 만들지 않는다.
2. **인스턴스/정적 멤버:** ldfld/stfld/ldsfld/stsfld/call/callvirt/newobj/ldtoken 등의 operand를 해당 원본 모듈의 실제 멤버로 변환한다. static 저장소는 A0의 저장소 하나만 사용한다.
3. **제네릭 피연산자:** List<PlayerController>의 PlayerController도 재귀적으로 원본 타입으로 바꾼다. 닫힌 기존 API 호출만 허용한다.
4. **호출 의미:** call과 callvirt를 임의 교환하지 않는다. base 호출을 MethodInfo.Invoke로 흉내 내지 않는다. virtual/interface dispatch, null 검사, boxing/unboxing 의미를 보존한다.
5. **자기/서로 호출:** 재귀와 다른 패치 대상 호출도 원본 MethodInfo를 가리킨다. 이번 배치의 새 PE 메서드를 가리키면 안 된다.
6. **컴파일러 생성 의존:** display class, state machine, 익명 타입, 새 정적 데이터 blob 등 A0에 안전하게 대응되지 않는 참조는 거부한다. 이름이 비슷하다고 토큰 번호를 맞추지 않는다.
7. **접근 제어:** private 접근이 가능한 실행 위치/라이브러리 동작을 G1에서 검증한다. 실패하면 모든 필드를 public으로 바꾸거나 reflection 호출로 몰래 대체하지 않는다.
8. **외부 참조:** 로드된 원본 assembly identity/hash에 정확히 대응해야 한다. 임의 AssemblyResolve 핸들러로 다른 버전의 DLL을 끼워 넣지 않는다.

### 7.3 Harmony transpiler에서의 변환 순서

G1에서 검증한 Harmony 버전의 `CodeInstruction` 계약을 사용한다. 분기는 Label, local은 LocalBuilder로 다루며 transpiler 내부에서 임의로 ILGenerator.Emit을 호출해 Harmony의 생성을 우회하지 않는다. [[S16]](#source-16)

text

```
Transpile(original, incomingInstructions, generator):       // 설계 의사코드
  body = registry.GetImmutablePreparedBody(original)
  if body is absent:
      return incomingInstructions

  validate original belongs to current RuntimeBaseline
  allocate fresh Label for each target and end sentinel
  allocate fresh LocalBuilder for each normalized local

  for every normalized instruction:
      resolve opcode and typed operand
      map ldloc/stloc/ldloca variants to the new LocalBuilder
      map branch/switch target IDs to the fresh Labels
      attach labels and exact exception-block boundaries
      append a fresh CodeInstruction

  validate all labels, local uses, EH edges, return paths
  return materializedInstructionList
```

동일 IR은 재사용할 수 있지만 **Label, LocalBuilder, CodeInstruction 인스턴스는 generator별로 다시 생성**한다. Harmony가 재적용 중 transpiler를 다시 호출할 수 있으므로 mutable 이전 결과를 캐시하지 않는다. transpiler는 파일 I/O·Worker 호출·소스 분석을 하지 않고 준비된 불변 데이터만 읽는다. [[S15]](#source-15)

### 7.4 예외 처리·검증기의 최소 요건

일반 catch/finally와 중첩 EH를 다루는 전용 변환기를 구현한다. leave, endfinally, try/handler 끝 경계를 단순 분기와 구별하고, 경계에서 필요한 block 종료 순서를 보존한다. using/foreach가 생성한 finally도 동일한 테스트를 통과해야 한다. emit 시 자동으로 생기는 구조와 원본 endfinally를 이중 삽입하지 않도록 실제 라이브러리 코드를 확인한다.

v1 검증기는 허용 opcode 집합에 대해 스택 높이·분기 합류·유효한 return·지역 변수/멤버 타입을 확인해야 한다. 이는 모든 CLI 코드에 대한 완전한 verifier를 만들라는 뜻이 아니다. 지원된 하위 집합만 허용하고, 분석할 수 없는 조합을 차단한다. JIT 성공만으로 전체 게임 코드가 안전하다고 판정하지 않는다.

### 7.5 기존 호출자의 관찰 가능성

NoInlining이 있는 실제 대상에서 직접 호출, 기존에 캐시한 delegate, Unity Update 콜백, private helper, base/virtual/interface 호출을 fixture로 검사한다. 특정 경로가 갱신되지 않으면 성공으로 표시하지 말고 해당 호출 경로를 지원 경계에서 제외하거나 백엔드를 수정한다. 소스 행 단위 breakpoint가 새 본문으로 이동한다는 보장은 v1에 포함하지 않는다.

<a id="planning"></a>

## 변경 분류·계획 생성·승인 조건

### 8.1 네 가지 결과를 명확히 구분한다

| 분석 결과 | 의미 | 사용자에게 보여줄 다음 행동 |
| --- | --- | --- |
| `NoSemanticChange` | 주석/공백 등 실행 의미 변화 없음. API 바인딩/생성 IL까지 확인. | “의미 변화 없음”으로 기록. 새 generation을 만들지 않음. |
| `Patchable` | allowlist, 구조 불변, IL 지원, baseline 일치, 현재 revision 조건 통과. | 검증된 전체 배치를 적용하거나 dry-run 결과 제공. |
| `CompileError` | 동결된 소스가 컴파일 실패. | 파일/행/열/오류 코드 표시. 현재 Eg 유지. 코드 수정 후 재시도. |
| `FullCompileRequired` | 구조 변경 또는 지원 범위 밖/확인 불가. | 사유와 해당 메서드 표시. 플레이를 임의 종료하지 않고 사용자 명령 대기. |

### 8.2 PatchPlan은 전체 세대의 원하는 상태를 표현한다

각 메서드는 `Install`, `Replace`, `RemoveOverride`, `Keep` 중 하나다. 최종 결과는 변경 메서드 목록만이 아니라 **원하는 전체 override map**으로 정의한다. batchHash, sourceRevision, expectedEffectiveGeneration, contextHash, runtimeModuleIds를 반드시 포함한다.

json

```
{
  "protocolVersion": 1,
  "projectId": "project-fingerprint",
  "sessionId": "current-session-guid",
  "planId": "immutable-plan-guid",
  "sourceRevision": 17,
  "snapshotHash": "sha256:...",
  "contextHash": "sha256:...",
  "expectedEffectiveGeneration": 4,
  "runtimeModules": [
    {"assembly": "Game.Runtime", "mvid": "original-guid", "sha256": "..."}
  ],
  "outcome": "Patchable",
  "operations": [
    {"methodKey": "canonical-method-key", "kind": "Replace", "bodyHash": "..."},
    {"methodKey": "another-method-key", "kind": "RemoveOverride"}
  ],
  "desiredGenerationHash": "sha256:...",
  "diagnostics": []
}
```

위 JSON의 해시/키는 형식 설명용 placeholder다. 실제 프로토콜에서는 구조화된 MethodKey를 사용하고 예제 문자열을 유효한 실행 데이터로 받아들이지 않는다.

### 8.3 apply 직전 다시 확인할 것

컴파일 성공 시점과 적용 시점 사이에 파일이 바뀔 수 있다. 적용 직전에 snapshot revision과 현재 파일 hash, 현재 generation, module MVID, contextHash, Play Mode 상태, 외부 Harmony owner, 도구 session id를 다시 비교한다. 하나라도 다르면 계획을 폐기하고 새 revision을 분석한다. 이미 한 번 적용된 requestId는 재실행하지 않는다.

처리 중 10개 저장 이벤트가 들어와도 과거 revision을 순서대로 모두 적용하지 않는다. 오래된 작업을 취소하고 최신 불변 스냅샷을 처리한다. 취소가 늦게 도착해 결과가 반환되더라도 stale check가 최종 방어선이어야 한다.

<a id="transaction"></a>

## 패치 트랜잭션·코드 롤백·격리

### 9.1 두 단계로 적용한다

#### Prepare · 게임 코드 실행 금지

전체 타입/멤버 해석, local/EH/스택 검증, owner 충돌 검사, 원본/이전 IR 확보, generation 계획과 journal을 준비한다. 임의의 게임 메서드를 호출하여 “동작 확인”하지 않는다.

#### Commit · Editor 메인 스레드

컴파일·import·종료 중이 아닌지 확인하고 패치를 적용한다. 전체 성공 후에만 active map과 generation을 갱신한다. 중간 실패는 앞서 적용된 대상까지 역순 복구한다.

보장하는 것은 **메인 스레드 전용 대상의 프레임 사이에서 수행하는 응용 수준의 배치 일관성**이다. 운영체제 수준의 원자성이나 모든 실행 스레드를 멈추는 전역 safepoint를 구현했다고 주장하지 않는다. 대상 코드가 백그라운드에서도 호출될 수 있다면 v1 대상에서 제외한다. paused Play Mode도 다른 스레드 정지를 보장하지 않는다.

text

```
Apply(plan):                                           // 설계 의사코드
  RejectIfStaleOrUnsafe(plan)
  oldGeneration = activeGeneration
  prepared = PrepareAll(plan)                          // 원본 상태 유지
  journal.Write("Prepared", plan, oldGeneration)

  try:
      for operation in DeterministicOrder(prepared):
          ApplyOwnTranspilerExactlyOnce(operation)
          verify backend ownership and registration count
      activeGeneration = prepared.DesiredGeneration    // 마지막에 승격
      journal.Write("Committed", activeGeneration)
      return Applied
  catch applyError:
      try:
          RestoreAllTouchedMethodsInReverse(oldGeneration)
          activeGeneration = oldGeneration
          journal.Write("RolledBackAfterFailure", applyError)
          return FailedButRestored
      catch restoreError:
          EnterQuarantinedState(applyError, restoreError)
          return FailedAndQuarantined
```

### 9.2 Harmony owner와 재적용

세션별 owner id와 단일 transpiler 메서드를 사용한다. 같은 대상에 같은 hook이 중복 누적되지 않는지 검사한다. 교체 시 기존 *자신의* transpiler만 제거·재설치하고, 실패하면 이전의 불변 IR로 다시 설치한다. “모든 패치 제거” API를 전체 프로젝트에 실행하지 않는다. 외부 owner가 같은 대상에 개입한 경우 v1은 합성하지 않고 거부한다. [[S18]](#source-18)

### 9.3 롤백은 코드에만 적용된다

**Rollback Last Batch**는 직전 Eg의 본문 맵으로 돌아간다. **Revert All Overrides**는 A0의 본문으로 돌아간다. 두 동작 모두 디스크 소스, HP, 이동 위치, 생성된 오브젝트, 저장 파일, 발생한 이벤트를 되돌리지 않는다. 패치된 게임 코드가 이미 변경한 월드 상태까지 복구한다고 설명하지 않는다.

롤백 후 디스크에는 새 코드가 남을 수 있다. UI는 `SourceDiverged`를 표시하며 동일 revision의 자동 재적용을 잠근다. 사용자가 새 저장을 하거나 명시적으로 Reapply Current Source를 요청할 때만 다시 적용한다. 게임 코드 예외가 났다는 이유만으로 자동 원복 후 원본 코드를 다시 실행하면 부작용이 두 번 발생할 수 있으므로 금지한다.

### 9.4 복구 실패

부분 패치의 원상복구를 증명하지 못하면 `Quarantined`다. 추가 패치를 금지하고 편집기 Play Mode를 가능한 범위에서 일시정지해 위험을 알린다. 진단을 저장하고, 명시적 정상 컴파일 또는 Editor 재시작이 필요하다고 표시한다. 소스 파일은 보존한다. “오류를 무시하고 계속” 버튼은 제공하지 않는다.

### 9.5 journal의 한계

journal은 진단/복구 계획의 기록이지 JIT 포인터를 복원할 수 있는 영속 상태가 아니다. Editor 재시작 후 이전 네이티브/managed patch handle을 재사용하지 않는다. 새로운 sessionId와 baseline을 만들고 소스로부터 다시 시작한다.

<a id="lifecycle"></a>

## Auto Refresh·Play Mode·정상 컴파일 생명주기

### 10.1 RefreshLease

소유권을 가진 객체 하나가 `DisallowAutoRefresh`를 한 번 획득하고, 같은 Editor 세션에서 정확히 한 번 해제한다. API는 on/off 스위치가 아닌 카운터이며 수동 Refresh를 막지 못한다. Unity Preferences의 전역 Auto Refresh 값을 덮어쓰는 방식은 기본 설계에서 사용하지 않는다. [[S11]](#source-11) [[S12]](#source-12)

text

```
Acquire():
  assert on Editor main thread
  if this lease is already held: return
  AssetDatabase.DisallowAutoRefresh()
  heldByThisInstance = true

Dispose():
  assert on Editor main thread
  if not heldByThisInstance: return
  heldByThisInstance = false
  AssetDatabase.AllowAutoRefresh()
```

**복구 버튼에서 AllowAutoRefresh를 반복 호출해 카운터를 0으로 “맞추지” 않는다.** 다른 도구의 lease를 침범할 수 있다. 프로세스가 바뀐 뒤 남은 journal만 보고 이전 lease가 아직 살아 있다고 추측하지 않는다. domain reload를 가로지르는 카운터 동작은 실제 버전에서 시험하고, 소유권을 추적할 수 없는 경우 blind decrement 대신 안전 중지와 Editor 재시작 안내를 사용한다.

### 10.2 명령의 의미를 분리한다

| 명령/상황 | 필수 동작 |
| --- | --- |
| Start Session | 유효 baseline, backend probe, Play Mode, allowlist를 확인한 뒤 lease 획득. 이미 활성 상태면 중복 획득하지 않음. |
| Pause Watching | 감시/자동 적용만 일시정지. 기존 패치와 lease 유지. 에셋 import도 보류될 수 있음을 표시. |
| Resume Watching | 누락 이벤트를 해시 재스캔하고 최신 revision부터 처리. 오래된 이벤트 큐를 그대로 실행하지 않음. |
| Rollback / Revert All | 코드만 원복. SourceDiverged 표시. 소스/게임 상태는 보존. |
| Stop and Recompile | 사용자 또는 명시적으로 승인된 에이전트 명령으로 Play 종료 → 패치 정리 → lease 해제 → Refresh/정상 컴파일 → 오류 검사 → baseline 갱신. |
| 사용자가 Play Mode 종료 | watch/worker 작업 취소 → 자신의 패치 정리 → lease 해제. 소스 변경이 있으면 정상 컴파일로 정합성 회복. 다음 Play 진입은 정합성 확인 후. |
| 예상치 못한 일반 컴파일/Refresh 시작 | 새 계획 폐기, 현재 세션 invalidation, 재로드 전 정리. LockReloadAssemblies로 장시간 저지하지 않음. |
| Editor 종료 / AssemblyReloadEvents | 구독·watcher·process·세션 정리. try/finally로 자신이 획득한 자원만 해제. [[S13]](#source-13) |
| 빌드 시작 | Play 중인 활성 세션에서 조용히 빌드하지 않음. 패치 세션 종료 및 원본 소스의 정상 컴파일을 요구. |
| 비상 종료 후 재시작 | 새 프로세스임을 확인하고 stale session을 진단용으로 보존. 자동으로 지난 패치를 복원하지 않음. |

### 10.3 Domain Reload 비활성화도 시험한다

Enter Play Mode 설정으로 static 상태·이벤트 구독이 유지될 수 있다. Bootstrap을 여러 번 실행해도 update/compilation/playMode 이벤트에 중복 등록하지 않아야 한다. 일반 게임플레이 static 상태를 도구가 임의로 초기화하지 않는다. 도구 자신의 session registry·watcher·bridge만 명시적으로 수명 관리한다. [[S14]](#source-14)

### 10.4 “Refresh 잠금”의 사용자 경험

감시 중에는 텍스처·프리팹·shader 등 다른 에셋의 import도 지연될 수 있다. 상단에 **Auto Refresh held by this session** 배지를 상시 표시한다. v1은 “C#만 막고 다른 자산은 항상 자동 import”를 보장하지 않는다. 수동 Ctrl/Cmd+R 또는 타 도구의 명시적 Refresh를 감지하면 세션을 안전하게 무효화한다. 파일 감시에는 Unity의 자동 import가 완료되어야만 호출되는 콜백을 유일한 수단으로 쓰지 않는다.

<a id="watching"></a>

## 파일 감시·배치·Worker 프로토콜

### 11.1 Watcher의 책임

파일 이벤트는 “변경했을 수 있음”의 신호다. FileSystemWatcher는 중복 이벤트나 오류를 낼 수 있으므로 실제 content hash로 중복을 제거하고, 오류·버퍼 overflow 시 해당 root를 다시 스캔한다. [[S22]](#source-22)

| 항목 | 설계 기본값 / 처리 |
| --- | --- |
| 감시 대상 | 프로젝트 소유 .cs와 컴파일 컨텍스트 파일. Library/Temp/obj/bin/.git/PackageCache/도구 자체 산출물은 제외. 경로 규칙은 G0에서 실제 프로젝트 기준 확정. |
| Debounce | 기본 300ms. 연속 저장은 하나의 revision으로 합침. 임계값은 설정 가능하며 정확성은 해시 검증이 담당. |
| 완전한 파일 쓰기 | 연속 두 번의 읽기에서 해시가 안정적인지 확인하고 짧은 공유 위반 재시도. 단순 시간 지연만으로 완성 저장을 확정하지 않음. |
| 폴링 | OS watcher 불가 시 폴링으로 전환. idle 프레임마다 전 파일 해시를 계산하지 않음. 기본 주기 1초, 주기적 전체 정합성 점검은 별도. |
| 경로 정규화 | 절대 경로 정규화 후 프로젝트 허용 root 안인지 검사. OS별 대소문자 규칙 적용. symlink/reparse point 우회에 대한 정책을 명시. |
| rename/delete/create | 안정된 스냅샷에서 최종 상태를 판정. 영구 파일 추가/삭제는 v1 컨텍스트 변경으로 정상 컴파일 요구. |
| 한글/공백 경로 | UTF-8 프로토콜과 ProcessStartInfo 인자 배열을 사용. shell 문자열 결합 및 따옴표 추정 금지. |
| 동시 변경 | Worker는 1개, 최신 revision 우선. 동시에 여러 patch commit 금지. 취소와 늦은 결과 폐기를 모두 구현. |

### 11.2 에이전트가 여러 파일을 수정할 때의 배치 장벽

Debounce는 논리적 트랜잭션이 아니다. Claude/Codex가 A를 수정한 뒤 몇 초 후 B를 저장하면 A만 먼저 적용될 수 있다. 에이전트 작업은 `begin_edit_batch` → 파일 수정 → `end_edit_batch` → dry-run/apply를 사용한다. 장벽 안에서는 변화만 수집하고 자동 적용하지 않는다. 에이전트가 중단되면 timeout을 이유로 미완성 배치를 적용하지 말고 Paused로 남겨 명시적 종료/취소를 받는다.

### 11.3 Worker 실행 계약

Editor가 `UseShellExecute=false`로 로컬 Worker를 시작한다. stdin/stdout은 길이 프레임 또는 명확한 NDJSON 프로토콜 중 하나로 고정하고, 로그는 stderr로 분리한다. protocolVersion/프로세스 세션 token/컴파일 context hash를 handshake한다. 프로토콜에 임의 shell command 또는 C# eval을 넣지 않는다.

Worker는 소스 읽기·Roslyn·Cecil·계획 생성만 수행한다. 게임 어셈블리의 user code·static constructor를 실행하지 않는다. source generator/analyzer를 임의 실행하는 것도 코드 실행이므로, v1 기본값은 신뢰·재현성이 확인되지 않은 generator를 거부하는 것이다. Worker 프로세스 분리는 의존성/장애 격리이지 악성 소스에 대한 완전한 보안 sandbox가 아니다.

### 11.4 중단·재시작

취소는 요청별 token으로 전달한다. timeout이면 해당 request를 폐기하고 필요 시 *자신이 시작한* Worker만 종료한다. 전체 dotnet/Unity 프로세스를 kill하지 않는다. 재시작 후 context를 다시 전송하고 세션·revision 일치를 확인한다. 이전 Worker 응답은 새 세션에서 적용하지 않는다. 디스크 snapshot은 atomic rename으로 확정한다.

<a id="contracts"></a>

## 모듈 경계·데이터 계약·설정

아래 인터페이스는 작업 분할과 책임을 고정하기 위한 **설계 계약**이다. 그대로 빈 클래스를 생성하는 것이 구현 완료가 아니다. 실제 언어 버전과 프로젝트 명명 규칙에 맞춰 구현하되 책임과 불변식은 유지한다.

csharp

```
public interface ICompilationContextProvider
{
    CompilationContext CaptureOnMainThread();
}

public interface IPatchWorkerClient
{
    // Worker 결과는 Unity 메인 스레드에 marshaling 후 사용한다.
    System.Threading.Tasks.Task<AnalysisResult> AnalyzeAsync(
        SnapshotRequest request,
        System.Threading.CancellationToken cancellationToken);
}

public interface IRuntimePatchBackend
{
    BackendProbeResult Probe(RuntimeBaseline baseline);
    PreparedBatch Prepare(PatchPlan plan, EffectiveGeneration current);
    ApplyResult CommitOnMainThread(PreparedBatch batch);
    ApplyResult RestoreOnMainThread(EffectiveGeneration desired);
}

public interface IHotReloadCoordinator
{
    HotReloadStatus GetStatus();
    CommandReceipt Execute(HotReloadCommand command);
    // UI와 AgentBridge가 같은 명령 경로를 호출한다.
}
```

| 모듈 | 책임 / 금지 |
| --- | --- |
| CompilationContextProvider | Unity API 접근은 메인 스레드. 실제 compiler input 캡처. 소스 변환이나 패치 금지. |
| BaselineStore / SnapshotStore | A0/W0/Sr 보관, hash 및 파일 수명. 런타임 패치 handle을 JSON에 직렬화하지 않음. |
| ChangeClassifier / ShapeValidator | semantic diff, 구조/생성 코드/미지원 구문 판정. 모르면 거부. |
| IlExtractor / Normalizer | Worker 내부 PE 읽기와 의미 키 생성. 게임 코드 실행 금지. |
| RuntimeMemberResolver | 원본 타입/멤버 연결, 모호성·MVID 검사. 임의 타입 복제/필드 추가 금지. |
| HarmonyBackend | 불변 본문 → CodeInstruction, 자기 owner 관리, 적용/복구. 파일 감시 책임 없음. |
| ReloadCoordinator | 세션 상태 머신, 직렬화된 command queue, revision/세대 검증, lease 수명. |
| Diagnostics / Journal | 정확한 실패 원인, 타이밍, 증거. 에러를 성공으로 변환하거나 게임 예외를 삼키지 않음. |
| UI / AgentBridge | 같은 coordinator 명령을 호출. UI 버튼마다 별도 패치 로직을 만들지 않음. |

### 12.1 설정 기본값

json

```
{
  "schemaVersion": 1,
  "enabledByDefault": false,
  "autoApply": true,
  "strictSession": true,
  "requireEditorNoInlining": true,
  "allowPlayModeExitFromAgent": false,
  "patchTargetThread": "MainThreadOnly",
  "debounceMilliseconds": 300,
  "pollingIntervalMilliseconds": 1000,
  "workerTimeoutSeconds": 60,
  "softPatchGenerationLimit": 100,
  "hardPatchGenerationLimit": 250,
  "historyLimit": 20,
  "allowlistedAssemblies": [],
  "allowlistedMethods": [],
  "excludedRoots": ["Library", "Temp", "obj", "bin", ".git"]
}
```

빈 allowlist는 전부 허용이 아니라 **어떤 게임 메서드도 패치하지 않음**이다. 위 수치는 운영 초기값이지 성능 실측값이 아니다. generation 제한과 timeout은 상태를 보고 조정하지만, safety check를 끄는 설정은 만들지 않는다. 사용자별 설정은 팀 공통 정책을 더 엄격하게 만들 수는 있어도 안전 경계를 임의로 완화하지 못하게 한다.

### 12.2 오류 식별자

`BaselineMissing`, `BaselineParityFailed`, `ContextChanged`, `StaleRevision`, `UnsupportedShapeChange`, `UnsupportedGeneratedCode`, `UnsupportedIL`, `TargetNotAllowlisted`, `InliningRisk`, `ExternalPatchConflict`, `WorkerUnavailable`, `CompileDiagnostics`, `PatchApplyFailed`, `RollbackFailed`, `RefreshOwnershipUnknown`, `BuildRequiresRecompile`를 안정된 기계 판독 코드로 둔다. 사람이 읽는 설명·파일/행/메서드·다음 행동을 함께 제공한다.

<a id="ui"></a>

## Editor UI·상태 머신·로그

Project Hot Reload 설계용 UI 시안 · NOT RUN

**Start****Pause Watching****Dry Run****Apply****Rollback****Stop & Recompile**

STATUS

### Baseline required

아직 실행 중 모듈과 소스 기준선을 확인하지 않았습니다.

Source revision **—**Effective generation **—**Refresh lease **Not held**

**Next action**

G0 환경 조사와 G1 백엔드 검증을 먼저 수행하세요.

여기 표시된 화면은 구현 목표이며 실제 도구 화면이 아닙니다.

Activity · Diagnostics · Methods · Compatibility · Settings · Agent

### 13.1 최소 화면

Overview에는 현재 session/source revision/effective generation/baseline id/Worker 상태/refresh lease 보유 여부/미적용 소스 수/지연된 import 경고를 표시한다. Methods는 정확한 MethodKey와 동작 Install/Replace/RemoveOverride를 보여준다. Diagnostics는 파일 위치로 이동할 수 있어야 하고, Activity는 append-only 이벤트 기록을 보여준다. UI Toolkit을 사용하되 기존 Editor 스타일과 충돌하지 않도록 범위를 제한한다.

### 13.2 상태 전이

text

```
Disabled → Preparing → Idle → Debouncing → Analyzing → Prepared → Applying → Idle
                                 │              │                     │
                                 │              ├─ CompileError ──────┤  기존 코드 유지
                                 │              └─ NeedsFullCompile   │  새 패치 중지
                                 │                                    └─ Recovering
                                 │                                         ├─ Idle
                                 │                                         └─ Quarantined
                                 └─ Paused / EditBatchOpen

명시적 정상 컴파일:
(any safe state) → Stopping → FullCompile → BaselineCapture → Disabled

예상치 못한 reload/context 변화:
(active) → Invalidated → Cleanup → Disabled

코드 롤백 성공:
Idle → Applying → SourceDiverged     (동일 소스의 자동 재적용 금지)
```

`CompileError`는 기존 Eg가 정상 실행 중이라는 뜻이지 소스가 정상이라는 뜻이 아니다. `NeedsFullCompile`은 실패를 숨기는 상태가 아니라 사용자에게 정확한 복귀 동작을 제공하는 정상적인 거부 결과다. `Quarantined`에서는 Apply/Resume을 비활성화한다.

### 13.3 세대 번호는 되돌리지 않는다

사용자 롤백은 이전 *본문 내용*을 복원하되 새 effective generation 번호를 부여한다. 예를 들어 E7 내용으로 롤백하더라도 E12 이후라면 결과는 E13이다. 이렇게 해서 오래된 요청이 우연히 같은 번호를 만나 재적용되는 ABA 문제를 막는다. 아직 공개하지 않은 commit이 실패해 복구된 경우에만 기존 generation을 그대로 유지한다.

### 13.4 로그

JSONL 로그 한 줄에 UTC, monotonic duration, sessionId, requestId, revision, generation, stage, resultCode, method count, diagnostics count, context hash를 기록한다. 사용자 소스 전체·로컬 token·개인 경로를 기본 export에 포함하지 않는다. 상세 소스 스냅샷 포함은 명시적 옵션으로 둔다. Console에는 요약을, 파일에는 상세 stack trace와 복구 결과를 남긴다.

<a id="bridge"></a>

## Claude·Codex용 명령 인터페이스

기존 Unity MCP가 실제로 연결되어 있으면 검증된 도구 인터페이스에 adapter를 추가한다. 존재하지 않는 MCP 함수 이름을 문서만 보고 호출하지 않는다. 별도 MCP가 없어도 사용할 수 있도록 **로컬 파일 inbox/outbox**를 기본 연결로 구현한다. HTTP 서버나 외부 네트워크 포트는 v1에 필요하지 않다.

### 14.1 명령 흐름

Editor는 `Library/ProjectHotReload/bridge/bridge-info.json`에 projectId, Editor pid/start time, protocolVersion, 현재 sessionId, inbox/outbox 경로 및 세션 token 파일 위치를 기록한다. 에이전트는 이 파일을 읽고 명령을 임시 파일로 작성한 뒤 `<requestId>.json`으로 atomic rename한다. Editor는 메인 스레드 coordinator로 전달하고 결과 파일을 같은 requestId로 기록한다.

| Action | 의미 / 권한 조건 |
| --- | --- |
| status / export\_diagnostics | 읽기 및 로그 내보내기. 현재 Editor 연결과 상태 확인. 시작 전에도 status 가능. |
| start / pause / resume | 세션 준비와 감시 제어. start는 baseline·allowlist 확인 후에만 성공. |
| begin\_edit\_batch / end\_edit\_batch | 여러 파일 수정의 자동 적용 장벽. batch owner와 token 검증. 종료 후 analyze는 하되 임의로 Play를 종료하지 않음. |
| analyze | 현재 source snapshot의 dry-run. 패치와 메타데이터 변경 없음. |
| apply / reapply\_source | 명시적 planId와 expected generation/revision에만 적용. 임의 최신 계획으로 대체 금지. |
| rollback\_last / revert\_all | 코드만 복원. expected generation 필수. 원본 소스는 수정하지 않음. |
| recompile\_and\_stop | Play 종료가 필요하면 프로젝트 설정과 명령 모두에서 허용해야 함. 승인 없으면 PermissionRequired 결과. |
| run\_smoke\_tests | 격리된 fixture/지정 테스트만 실행. 임의 사용자 scene에서 게임 메서드를 자동 호출하지 않음. |

json

```
{
  "protocolVersion": 1,
  "requestId": "8bdf0c5f-6a7d-40b9-a2fe-f35f30bece91",
  "projectId": "read-from-bridge-info",
  "sessionId": "read-from-current-status",
  "sessionToken": "read-locally-never-commit",
  "action": "apply",
  "planId": "plan-created-by-analyze",
  "expectedSourceRevision": 17,
  "expectedEffectiveGeneration": 4,
  "allowPlayModeExit": false
}
```

json

```
{
  "protocolVersion": 1,
  "requestId": "8bdf0c5f-6a7d-40b9-a2fe-f35f30bece91",
  "state": "completed",
  "resultCode": "Applied",
  "sessionId": "current-session-guid",
  "sourceRevision": 17,
  "effectiveGeneration": 5,
  "appliedMethodCount": 2,
  "requiresFullCompile": false,
  "diagnostics": [],
  "evidencePath": "Library/ProjectHotReload/.../apply-result.json"
}
```

### 14.2 명령 안전 계약

requestId를 멱등 키로 사용한다. 같은 내용의 재전송에는 기존 결과를 반환하고, 같은 ID에 다른 내용이 오면 거부한다. 세션·source revision·generation이 오래되었으면 거부한다. 명령으로 임의 C#·shell·외부 경로를 실행할 수 없게 한다.

token은 실수로 다른 프로젝트/세션에 명령을 넣는 것을 막는 장치다. 같은 OS 사용자 권한으로 실행되는 악성 프로세스까지 격리하는 보안 경계라고 설명하지 않는다. malformed/oversized JSON, 잘못된 encoding, path traversal, 오래된 request, 에이전트 중단을 테스트한다.

### 14.3 구현 후 에이전트의 작업 루프

text

```
1. status로 실제 Editor / session / baseline / generation 확인
2. begin_edit_batch
3. 지원 범위 안에서 소스 수정
4. end_edit_batch
5. analyze 결과의 오류와 지원 불가 변경을 읽음
6. 해당 planId + expected revision/generation으로 apply
7. 결과 JSON과 Unity Console 새 오류 확인
8. 지정된 기능 smoke test / 실제 관찰값 기록
9. 실패하면 소스를 수정하거나 code rollback 수행; 상태 복구로 오해하지 않음
10. 작업 완료 전 일반 컴파일 + 실제 회귀 테스트 수행
```

<a id="validation"></a>

## 검증 전략과 필수 테스트 매트릭스

Worker 단위 테스트만으로 Unity 패치가 동작한다고 주장하지 않는다. **순수 .NET 테스트 → Unity EditMode → Editor 내 PlayMode → 실제 프로젝트 smoke → 정상 Player 빌드**를 구분한다. 각 테스트에는 실행 환경, 코드/소스 해시, 예상값과 실제값, pass/fail, 로그 경로가 있어야 한다.

### 15.1 IL / 타입 / 컴파일

| ID | 시험 | 합격 조건 |
| --- | --- | --- |
| T01 | 원본 private 필드 read/write | 패치 전후 ReferenceEquals와 InstanceID가 같고 기존 값에서 계산이 이어짐. |
| T02 | static 상태 공유 | 기존 static 값이 보존되고 새 코드도 같은 저장소를 사용. cctor 중복 실행 0회. |
| T03 | int/float/bool/string/참조/Vector3 반환 | 각 값과 ABI가 정확. boxing/복사로 상태를 잃지 않음. |
| T04 | overload + nested type + private/internal 메서드 | 정확한 MethodKey만 변경. 같은 이름의 다른 메서드는 원본 유지. |
| T05 | partial 2파일 + 2개 asmdef | 전체 컨텍스트 컴파일 성공. 같은 타입 중복 생성 없음. |
| T06 | 같은 assembly의 private helper / 재귀 / 상호 호출 | 원본 MethodInfo로 연결되고 새 본문 관찰. 재귀가 patch wrapper를 잘못 이중 호출하지 않음. |
| T07 | base/virtual/interface 호출 | 원본 호출 의미 보존. base 호출이 override로 잘못 재진입하지 않음. |
| T08 | 기존에 캐시된 delegate 및 Unity Update 콜백 | 사전 NoInlining 대상이 commit 이후 새 본문을 실행. |
| T09 | local 추가/제거/형식 변경, 큰 switch | local/label 변환이 정확하며 InvalidProgramException 없음. |
| T10 | List 등 닫힌 generic 피연산자 | 기존 타입과 닫힌 메서드로 재연결. open generic target은 거부. |
| T11 | 기존 API의 out/ref 호출 | 지역 주소와 결과가 정확. Raycast 등 프로젝트에서 쓰는 대표 API 포함. |
| T12 | 일반/중첩 try-catch-finally 및 using | 정상/예외/early return 모두 finally 실행 횟수와 값이 기준 구현과 일치. |
| T13 | 명시적 property/indexer getter/setter | 기존 필드 사용 및 접근자 매칭 정확. auto-property 구조 변경은 거부. |
| T14 | using alias 변경으로 binding만 변함 | 본문 문자열이 같아도 새 참조 대상을 감지하거나 명확히 거부. NoChange 오판 금지. |
| T15 | 한글 경로·공백·define·nullable·rsp | Unity와 같은 source binding 및 오류 위치. Worker host BCL 혼입 없음. |
| T16 | baseline compiler parity | 대상 A0/W0의 정규화된 IL/메타데이터가 대응. 불일치면 패치하지 않음. |

### 15.2 실패·순서·복구

| ID | 시험 | 합격 조건 |
| --- | --- | --- |
| T17 | 컴파일 오류 발생 후 수정 | 오류 동안 Eg 유지. 수정 후 최신 revision만 적용. |
| T18 | A 수정 → B 수정 → A만 원본으로 revert | A override 제거, B 패치 유지. 원본 비교만 해서 놓치지 않음. |
| T19 | 같은 메서드 50회 재패치 | 매번 정확한 결과. 자신 소유 hook 수 1개. 이전 결과 재실행 없음. |
| T20 | 다중 메서드 배치 중간 실패 주입 | 모든 touched method를 이전 Eg로 복원. generation 승격 없음. |
| T21 | 복구 단계에도 실패 주입 | Quarantined 진입, 새 적용 차단, 진단과 복귀 안내 생성. |
| T22 | Rollback last / Revert all | 본문 내용만 복원하고 세대 ID는 단조 증가. 소스·게임 상태가 되돌아가지 않음을 확인. |
| T23 | 롤백 후 같은 source revision | 자동 재적용하지 않음. 명시적 reapply로만 재적용. |
| T24 | 빠른 연속 저장 20회 + 느린 Worker | 최신 snapshot만 최종 적용. stale 결과는 거부. |
| T25 | 하나의 batch에 지원 + 비지원 변경 | 어느 메서드도 적용하지 않음. 정확한 거부 사유 제공. |
| T26 | Worker crash/timeout/cancel | 기존 코드 유지. 자신의 process만 종료. 새 Worker 응답만 수용. |
| T27 | 외부 Harmony owner 충돌 | 충돌 감지 후 거부. 타 도구 patch를 제거하지 않음. |
| T28 | 조작된 MVID/context hash/원본 파일 변경 | Apply 직전 거부. 다른 baseline의 멤버를 사용하지 않음. |
| T29 | begin\_edit\_batch 도중 에이전트 중단 | 일부 소스를 자동 적용하지 않음. Paused 상태 및 회복 명령 제공. |
| T30 | 잘못된 JSON/중복 requestId/path traversal | 명령을 거부하고 session 유지. 동일한 올바른 요청은 중복 실행하지 않음. |

### 15.3 생명주기·거부·빌드

| ID | 시험 | 합격 조건 |
| --- | --- | --- |
| T31 | Play Mode 진입/종료 20회 | watcher/process/event 구독/lease 중복·누수 없음. |
| T32 | Domain Reload ON/OFF 각각 반복 | 도구 static registry와 구독이 정상 정리. 기존 프로젝트 설정을 강제 변경하지 않음. |
| T33 | 수동 Refresh / 외부 스크립트 compile | 세션을 무효화하고 정리. 오래된 plan 적용 없음. |
| T34 | 정상 종료 / 의도적 Worker 종료 / Editor 재시작 | stale journal을 현재 patch handle로 오인하지 않음. |
| T35 | RefreshLease 이중 Dispose / 예외 경로 | 자신이 획득한 횟수만큼만 해제. 테스트용 mock 계수와 실제 Editor 동작 대조. |
| T36 | 필드·초기값·const·attribute·signature 변경 | 정확한 FullCompileRequired. 기존 객체 값 임의 변경 없음. |
| T37 | new type/method, deleted type/method | 전체 배치 거부. 메서드 추가를 지원한다고 과장하지 않음. |
| T38 | async/yield/lambda/local function/anonymous type | 생성 코드 판별 후 거부. 우연히 컴파일되었다고 통과하지 않음. |
| T39 | generic target / struct instance / ref return | 지원 경계에 따라 거부. 외부 generic API 호출과 혼동하지 않음. |
| T40 | Burst/Jobs/ILPP/generated dependency | 해당 대상을 명확히 제외/거부. 도구가 새로 만든 Burst/metadata 오류는 0개. |
| T41 | NoInlining 누락 또는 worker thread 가능 대상 | 지원되는 경로로 허위 표시하지 않음. 등록 거부/경고와 정상 컴파일 안내. |
| T42 | 장시간 패치/GC/메모리/CPU | 누적 리소스를 계측. 설정된 한도에서 안전하게 새 패치 중지. |
| T43 | 도구 활성 → 정상 컴파일 → Player 빌드 | 원본 소스로 정상 빌드. Player에 도구·Harmony·Worker·fixture 포함 없음. |
| T44 | 도구 제거 후 clean compile | 게임 코드가 도구 타입에 의존하지 않고 원래 흐름으로 빌드 가능. |
| T45 | 고립된 clean checkout의 설치/테스트 재현 | 로컬 임시 DLL 경로와 개인 SDK 절대 경로 없이 재현. 버전/해시/라이선스 포함. |

### 15.4 실제 프로젝트 세 가지 smoke

| ID | 대상 선정 | 관찰할 것 |
| --- | --- | --- |
| P01 이동/회피/가속 | 기존 이동 시스템에서 메인 스레드의 계산/보정 메서드 1개. 실제 구조에 맞춰 선택. | 속도 계산식이 다음 호출부터 변경. 플레이어 instance, HP, 입력 연결, 현재 상태 객체가 유지. |
| P02 락온/카메라 | 기존 카메라 거리 또는 보간 계산 메서드 1개. | 새 거리/보간이 관찰되지만 lock-on target, Transform, 카메라 rig 참조가 재생성되지 않음. |
| P03 전투 또는 상태 판정 | 피격 데미지/공격 범위/상태 전환 조건 중 지원 가능한 메서드 1개. | 새 규칙 적용, 기존 적/플레이어 참조 유지. 의도된 상태 변화와 예기치 않은 초기화를 구별. |

Animancer 내부를 패치하지 않는다. 기존 상태 머신/AnimancerState 참조와 애니메이션 진행이 유지되는지 프로젝트의 API를 사용해 관찰한다. progress가 자연스럽게 증가하는 상황에서 전후 time이 완전히 같아야 한다고 잘못 테스트하지 않는다. 코루틴 본문 변경은 제외되지만, 호출자의 기존 코루틴 호출이 정상 작동하는지는 확인한다.

### 15.5 실행 증거

테스트 XML/JSON, Editor.log, source diff, Before/After assertion 결과, runtime/module fingerprint, 성능 CSV를 `Docs/HotReload/evidence/Gxx/`에 정리한다. 커다란 원본 로그는 별도 artifact로 보관하고 요약에는 정확한 상대 경로·해시를 기록한다. 실제 Unity가 없으면 해당 항목을 `NOT_RUN`으로 남긴다.

<a id="performance"></a>

## 성능·메모리·장기 세션 기준

아래 수치는 **측정해야 할 초기 목표값**이다. 상용 제품의 수치나 실제 프로젝트에서 달성한 수치가 아니다. G0에서 reference machine과 대표 어셈블리의 규모를 정하고 G10에서 결과를 채운다.

| 항목 | 초기 목표 / 행동 | 측정 방법 |
| --- | --- | --- |
| Editor idle | 도구 자신의 update 경로 steady-state 0 B/frame, 평균 0.1ms 이하 목표. 전체 게임 수치와 분리. | ProfilerMarker + 충분한 warm-up. UI 펼침/접힘과 로그 표시 상태 구분. |
| 소스 저장 → 적용 | 대표 1개 메서드, warm worker에서 p95 2초 이하를 1차 목표로 설정. | 같은 입력 시나리오 최소 50회, debounce/compile/prepare/commit/end-to-end 분리. |
| main-thread commit | 대표 1~3개 메서드 p95 8ms 이하 목표. | Unity 메인 스레드에서 계측. 초과하면 수치와 프레임 영향 기록, batch 크기/전처리 최적화. |
| 패치 후 메서드 실행 | reflection invoke 기반 hot path 금지. 대표 함수 성능 변화 계측. | baseline 대비 warm-run, 평균/p95/GC 비교. “Harmony니까 0 overhead” 단정 금지. |
| 장기 사용 | 최소 60분, 실제 패치 100회 이상, Play 진입/종료 20회. | Editor/Worker 메모리, loaded assembly 수, 동적 코드 관련 지표, handle/thread 수 기록. |
| 세대 제한 | 100세대 경고, 250세대 신규 패치 중지 기본값. | 한도 도달은 실패가 아닌 보호 동작. 정상 컴파일/필요 시 Editor 재시작 후 새 baseline. |
| 캐시 | A0/W0/현재 snapshot + 제한된 history만 유지. | 파일 크기/개수와 retained memory 측정. stale compilation/reference cache 폐기. |
| Worker 비용 | 불변 MetadataReference와 CSharpCompilation 캐시 재사용. | source tree 교체 전후 compile 시간과 process RSS 기록. 의존성 변경 시 캐시 무효화. |

### 동적 코드 수명은 별도 문제다

컴파일한 게임 DLL을 Editor에 계속 로드하지 않는 설계는 불필요한 어셈블리 축적을 줄이지만, Harmony가 만든 wrapper/dynamic code와 네이티브 리소스가 항상 즉시 회수된다는 뜻은 아니다. history 리스트를 비우거나 GC.Collect를 호출하는 것만으로 정리됐다고 판단하지 않는다. 실제 메모리 추세를 보고 session budget을 둔다.

정상 컴파일이 실제 runtime/domain을 갱신했는지 확인하여 누적 카운터를 초기화한다. 해당 백엔드에서 리소스가 남으면 Editor 재시작을 명시한다. Worker 프로세스 재시작은 Editor 안의 패치 리소스를 정리하는 방법이 아니다.

### 최적화 순서

정확성 → 입력/참조 캐시 → Worker warm-up → 변경 어셈블리 최소화 → 사전 해석/계획 준비 → UI 로그 가상화 순으로 진행한다. 측정 전 수동 네이티브 포인터 패치나 전체 프로젝트 rewrite로 성능을 해결하려 하지 않는다.

<a id="failures"></a>

## 문제별 복구와 금지되는 우회

| 증상/코드 | 확인 및 올바른 복구 | 금지되는 우회 |
| --- | --- | --- |
| CS0433 / duplicate type | 자기 어셈블리를 참조 집합에 넣었는지, 같은 DLL 중복 로딩 여부 확인. manifest 수정 후 재분석. | 타입 이름을 임의로 바꾸고 기존 객체를 새 타입으로 캐스팅. |
| MissingMethod / private access | MethodKey·declaring type·접근 컨텍스트·모듈 identity 확인. G1 최소 fixture로 재현. | 프로젝트 모든 멤버를 public으로 변경. |
| InvalidProgramException | local/label/stack/EH 매핑과 반환 ABI를 진단. 이전 generation 복구 및 해당 opcode 차단. | 예외를 삼키고 성공 UI 표시. |
| 패치 성공 표시지만 기존 값 실행 | 기존 caller/캐시 delegate/인라이닝/owner 중복 확인. Probe와 NoInlining 기준선 재구성. | “다음에 반영될 것”이라고 추측하거나 상태 복사로 위장. |
| BaselineParityFailed | compiler profile, defines, rsp, generator/ILPP 차이 확인. 실제 동등성을 확보하거나 제외. | 다른 compiler 출력도 같을 것이라고 가정. |
| 정상 코드 수정인데 FullCompileRequired | 검증기 사유와 실제 IL을 확인. 테스트와 함께 지원 범위를 확대하거나 거부 유지. | 미지원 opcode를 NOP으로 대체. |
| Worker가 응답하지 않음 | 자기 process만 종료, pending 취소, 최신 snapshot으로 재시작. | 전체 Unity/dotnet 프로세스 일괄 종료. |
| 패치 한 개만 적용된 상태에서 실패 | journal과 touched map으로 전체 이전 generation 복원. 실패하면 Quarantined. | 성공한 메서드만 남기고 일부 성공으로 보고. |
| Auto Refresh가 돌아오지 않음 | 소유 lease 획득/해제 기록과 현재 process 확인. 검증 가능한 자신의 lease만 해제. | AllowAutoRefresh를 여러 번 호출해 강제로 활성화. |
| Burst가 Harmony DLL metadata 오류 표시 | 도입 전 baseline과 비교, 정확한 의존성 빌드 조합을 바꿔 재현/해결. | 참고 README가 noise라 했다는 이유만으로 새 Console 오류를 허용. |
| 일반 컴파일 후 결과가 달라짐 | Worker/Unity 컨텍스트 및 비지원 변경 누락 확인. 이를 핵심 correctness bug로 취급. | 패치된 세션만 정상이라고 주장. |
| Player 빌드에 도구 DLL 포함 | asmdef와 모든 plugin importer/meta·패키징 경로·전이 참조 조사. clean build 재검증. | 파일명만 바꾸거나 배포 후 수동 삭제를 정상 절차로 둠. |

알려진 오류가 있더라도 새 오류를 도구가 만든 것인지 구별해야 한다. 도입 전 프로젝트 자체의 기존 실패는 baseline에 기록하되, 도구로 인해 생긴 오류를 기존 오류 목록에 섞어 감추지 않는다.

<a id="phases"></a>

## G0~G12 단계별 실행 계획과 요청문

G0부터 G12까지가 **이번 작업의 필수 완료 범위**다. 각 단계에서 구현→컴파일→테스트→수정→재테스트→증거 기록을 반복한다. 테스트는 G8에 몰아서 만드는 것이 아니라 매 단계 함께 작성하며, G8은 누락 보완과 통합 회귀 단계다.

| 단계 | 핵심 결과 | 의존 |
| --- | --- | --- |
| G0 | 저장소·실행 환경 조사와 기준 기록 | 없음 |
| G1 | 타입 보존 패치의 기술적 성립 검증 | G0 통과 |
| G2 | Editor 패키지와 안전한 생명주기 | G1 통과 |
| G3 | 실제 컨텍스트 기반 Worker 컴파일러 | G2 통과 |
| G4 | 정규화 IL·타입 해석·변경 검증기 | G3 통과 |
| G5 | 세대 계획·트랜잭션·롤백 | G4 통과 |
| G6 | 파일 감지·일관된 revision·배치 장벽 | G5 통과 |
| G7 | Editor UI와 에이전트 명령 연결 | G6 통과 |
| G8 | 전체 회귀·부정 테스트·결함 주입 | G7 통과 |
| G9 | 실제 액션 게임 코드에 제한 적용 | G8 통과 |
| G10 | 성능·장기 사용·리소스 한도 검증 | G9 통과 |
| G11 | 정상 빌드·제거 가능성·배포 문서 | G10 통과 |
| G12 | 독립 검수·잔여 결함 수정·최종 인계 | G11 통과 |

G0

<a id="phase-G0"></a>

### 저장소·실행 환경 조사와 기준 기록

추정에 기반한 패키지/클래스 이름을 제거하고 실제 구현 대상과 테스트 경로를 확정한다.

NOT RUN

1. 기존 AGENTS.md, CLAUDE.md, 프로젝트 지침을 먼저 읽고 작업 디렉터리·VCS 상태·사용자 미커밋 변경을 기록한다. 사용자 변경은 수정/되돌림하지 않는다.
2. ProjectVersion.txt, manifest.json, packages-lock.json, asmdef/asmref, rsp, API compatibility, Play Mode 옵션, build target, compiler/ILPP/generator, Harmony/Burst 등 관련 의존성을 조사한다.
3. 설치된 Unity 실행 파일, 실행 중 Editor의 버전·PID, OS/CPU, .NET SDK, 테스트/빌드 명령과 기존 Unity MCP 가용성을 확인한다. 도구가 실제로 있는지 확인하지 않고 이름을 만들어내지 않는다.
4. 기존 Console 오류와 테스트 상태를 baseline으로 저장한다. 별도 fixture 프로젝트 또는 격리 환경을 준비하고 실제 적용 후보 메서드 3개를 코드 위치와 함께 제시한다.

**산출물** `Inventory.md` · `CompatibilityMatrix.md` · `RepositoryMap.md` · `progress.json` · `evidence/G0/environment.json` · `adr/0001-scope-and-backend.md`

**통과 조건**

현재 Unity/OS와 컴파일 컨텍스트 경로가 증거로 확인되고, 원본 프로젝트 변경을 보존한 채 fixture 실행 경로가 정해져야 한다. Unity를 실행할 수 없으면 그 사실을 기록하고 G1 실행을 NOT\_RUN으로 남긴다.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G0을 실행해라. 먼저 현재 저장소의 기존 지침과 미커밋 변경을 확인하고, Unity/패키지/asmdef/컴파일러/OS/.NET/테스트 도구를 실제 파일과 실행 결과로 조사해라. 문서에 언급된 과거 엔진 버전을 하드코딩하지 마라. Inventory, CompatibilityMatrix, RepositoryMap, 초기 progress.json과 G0 증거를 작성해라. 구현 후보는 기존 이동·카메라·전투에서 실제 메서드 이름으로 선택하되 아직 게임 코드를 대규모 변경하지 마라. G0 통과 후 G1 최소 fixture 검증으로 이어가라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G1

<a id="phase-G1"></a>

### 타입 보존 패치의 기술적 성립 검증

이 프로젝트의 실제 Unity 런타임에서 핵심 경로가 작동함을 작은 실행 사례로 증명한다.

NOT RUN

1. 비제네릭 참조 타입 fixture와 MonoBehaviour fixture를 만들고 private 필드, static 필드, Vector3 반환, private helper를 포함한다. 대상에는 Editor 조건부 NoInlining을 미리 일반 컴파일한다.
2. 최소 Worker로 같은 컴파일 컨텍스트의 수정본을 PE로 생성하고, 원본 타입/멤버로 재연결한 본문을 Harmony transpiler로 교체한다. 수정 게임 DLL을 Editor에 Assembly.Load하지 않는다.
3. 직접 호출·기존 호출자·Unity 콜백·기존 delegate와 원복을 실제 Unity에서 실행한다. ReferenceEquals, InstanceID, 기존 필드 값, static 초기화 횟수를 단언한다.
4. 같은 메서드 연속 교체와 실패 후 복구를 최소 한 번 확인한다. private 접근/반환 ABI/인라이닝이 실패하면 소스와 실행 증거를 축소해 원인을 분리한다.

**산출물** `격리 fixture 코드와 실행 테스트` · `evidence/G1/probe-result.json` · `evidence/G1/Editor.log` · `adr/0002-backend-proof.md`

**통과 조건**

T01~T04/T08의 최소 사례와 원복이 실제 Unity에서 PASS여야 한다. 순수 .NET 실행, reflection으로 새 객체 실행, 콘솔 메시지만으로 대체 불가. 실패하면 백엔드 종속 단계는 BLOCKED이며 UI부터 완성한 것으로 처리하지 않는다.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G1 최소 기술 검증을 실제 Unity에서 구현·실행해라. 원본 private/static 상태를 유지하면서 외부 컴파일 결과의 IL을 원본 멤버로 재연결하고 Harmony로 교체해라. 새 게임 어셈블리를 로드하거나 기존 객체를 새 타입으로 교체하지 마라. 기존 delegate/Unity Update와 Vector3 반환, 반복 패치, 원복을 단언하고 로그를 남겨라. 이 검증이 실패하면 우선 원인을 해결하고, 성립하지 않는 백엔드 위에 큰 시스템을 쌓지 마라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G2

<a id="phase-G2"></a>

### Editor 패키지와 안전한 생명주기

의존성·세션·정리 경로를 먼저 안정화한다.

NOT RUN

1. G1 검증 버전을 고정하고 Editor 전용 패키지/asmdef/plugin importer 설정과 ThirdPartyNotices를 구성한다. Worker는 Assets 밖에 둔다.
2. Bootstrap, coordinator 상태 머신, owner id, RefreshLease, Play/compile/reload/quit 이벤트의 중복 없는 구독과 정리를 구현한다.
3. Pause와 StopAndRecompile의 의미를 분리하고 승인 없는 Play 종료를 막는다. 도구 자체/외부 SDK/PackageCache는 패치 대상에서 제외한다.
4. 보유 lease의 예외/중복 Dispose, Domain Reload ON/OFF, 반복 Play 진입/종료를 테스트한다. 빌드 경계의 정적 검사를 추가한다.

**산출물** `Editor 패키지 골격의 실제 동작 코드` · `세션/lease/생명주기 테스트` · `의존성 lock/hash/license 목록` · `evidence/G2/lifecycle-results.json`

**통과 조건**

T31~T35 관련 초기 테스트 PASS, 신규 Console 오류 0, lease/구독/worker 중복 없음. 단순 코드 스켈레톤으로 통과 불가.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G2를 구현해라. 검증된 의존성을 Editor 전용으로 격리하고 Bootstrap/세션/RefreshLease/Play·compile·reload·quit 정리를 연결해라. Pause와 StopAndRecompile을 구분하고 전역 Auto Refresh 설정을 덮어쓰지 마라. Domain Reload ON/OFF와 반복 진입에서 중복 구독·lease 불균형을 실제 테스트해라. 신규 오류를 제거한 뒤 G3로 진행해라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G3

<a id="phase-G3"></a>

### 실제 컨텍스트 기반 Worker 컴파일러

소스·참조·옵션을 재현하고 A0/W0/Eg/Sr 모델을 구현한다.

NOT RUN

1. Unity compiler context adapter와 baseline capture를 구현한다. source files/defines/references/options/response files/context hashes를 기록한다.
2. Worker 프로젝트·버전 고정·handshake·request/cancel/timeout을 구현하고 불변 source snapshot에서 변경 어셈블리 전체를 컴파일한다.
3. 자기 assembly 참조 제외, Unity reference profile 사용, partial/asmdef/dependency 순서/한글 경로를 지원한다. compiler 진단을 원본 파일/행/열로 매핑한다.
4. W0 baseline과 실제 A0 대상 본문의 동등성을 검사한다. source generator/ILPP를 재현하지 못한 대상은 정확하게 거부한다.

**산출물** `CompilationContextProvider` · `SnapshotStore / BaselineStore` · `Worker compile/analyze 명령` · `compiler profile lock` · `evidence/G3/compiler-context.json`

**통과 조건**

T05/T14~T16 PASS, 컴파일 실패 시 원래 실행 코드 불변. Worker host의 BCL이 Unity 참조에 섞이지 않음.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G3을 구현해라. Unity가 사용하는 실제 source/define/reference/option으로 외부 Worker 컴파일을 재현하고 A0/W0/Eg/Sr를 별도로 관리해라. 변경 파일 하나만 독립 컴파일하는 지름길은 사용하지 마라. partial·여러 asmdef·rsp·한글 경로·컴파일 오류 위치와 baseline IL parity를 테스트해라. 재현 못 하는 generator/ILPP는 지원 불가로 명시해라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G4

<a id="phase-G4"></a>

### 정규화 IL·타입 해석·변경 검증기

v1 지원 범위 전체를 실제로 해석하고 미지원 코드를 확실히 거부한다.

NOT RUN

1. MethodKey/TypeKey/MemberKey와 NormalizedMethodBody 직렬화 형식을 구현한다. raw metadata token의 직접 재사용을 금지한다.
2. 명령·local·label·switch·제네릭 피연산자·ref/out API 호출·일반/중첩 catch/finally를 정확히 변환한다. generator별 Label/LocalBuilder를 새로 만든다.
3. 원본 private/static/상속/virtual/interface/self-call 연결과 명시적 property/indexer 접근자를 처리한다. allowlist와 사전 NoInlining을 검사한다.
4. 구조 변경·초기화·새 멤버·async/iterator/lambda/local function·unsafe·미지원 opcode·generic target·Burst/Jobs를 semantic/출력 IL 양쪽에서 검출한다.

**산출물** `IR/protocol 모델과 schema` · `IlExtractor / IlNormalizer / RuntimeMemberResolver` · `ShapeValidator / IlValidator` · `Harmony body emitter` · `evidence/G4/il-test-results.json`

**통과 조건**

T01~T16 및 T36~T41 PASS. EH 또는 타입 재연결을 TODO로 남기고 v1 완료로 표시할 수 없음. 미지원 코드가 실제로 거부되는 negative test 필수.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G4를 구현해라. 문서의 IL/MethodKey/TypeKey 계약에 따라 원본 타입과 멤버를 재연결하고 locals·labels·switch·예외 영역·닫힌 generic API·out/ref API 호출을 지원해라. v1 지원 매트릭스의 필수 기능과 거부 기능 모두에 자동 테스트를 작성해라. raw token 재사용, 임의 NOP 대체, 게임 타입 재생성으로 문제를 피하지 마라. EH/반환 ABI/private 접근을 실제 Unity로 검증해라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G5

<a id="phase-G5"></a>

### 세대 계획·트랜잭션·롤백

여러 메서드를 일관되게 적용하고 실패·원복을 명시적으로 처리한다.

NOT RUN

1. Eg와 Sr의 차이로 Install/Replace/RemoveOverride/Keep을 만들고 구조 검증은 A0 기준으로 수행한다.
2. Prepare/Commit/Restore와 immutable generation registry, transaction journal, owner 충돌 및 중복 hook 방지를 구현한다.
3. 부분 실패의 전체 복구와 복구 실패 Quarantined를 구현한다. 사용자 롤백은 새 generation 번호를 부여하고 source divergence를 표시한다.
4. A→B→A 원복, 중간 실패 주입, 이중 복구 실패, stale generation, 외부 Harmony owner, 동일 source 자동 재적용 방지를 테스트한다.

**산출물** `PatchPlanner / PreparedBatch / GenerationStore` · `TransactionCoordinator / RecoveryJournal` · `Rollback 기능` · `evidence/G5/fault-injection-results.json`

**통과 조건**

T17~T23/T27/T28 PASS. 일부 성공을 전체 성공으로 보고하거나 게임 상태 복구라고 표현하지 않음.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G5를 구현해라. A0/Eg/Sr 기준으로 전체 원하는 세대 맵을 계획하고 Prepare-Commit-Restore를 연결해라. A를 수정한 뒤 B를 수정하고 A만 소스 원본으로 되돌리는 테스트를 반드시 통과시켜라. 패치 중간 실패와 복구 실패를 주입해 전체 원복/Quarantined를 검증해라. 롤백은 코드만 되돌리고 세대 번호는 단조 증가시키며 소스 자동 재적용을 막아라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G6

<a id="phase-G6"></a>

### 파일 감지·일관된 revision·배치 장벽

사람과 에이전트의 저장 패턴을 안전한 스냅샷으로 바꾼다.

NOT RUN

1. FileSystemWatcher + polling fallback + overflow rescan + content-hash dedupe를 구현한다. 경로/인코딩/rename/부분 쓰기를 처리한다.
2. debounce, 최신 revision 우선 queue, 오래된 결과 취소/폐기, apply 직전 재검증을 연결한다.
3. begin/end edit batch와 사용자 Pause를 구현한다. 배치 도중 에이전트가 사라져도 자동으로 미완성 변경을 적용하지 않는다.
4. context 파일 변경, 도구 자체 수정, 외부 Refresh/일반 컴파일 발생 시 session invalidation으로 연결한다.

**산출물** `Watcher / SnapshotScheduler / RevisionGuard` · `BatchBarrier` · `stress 저장 테스트` · `evidence/G6/revision-results.json`

**통과 조건**

T24~T26/T28/T29/T33 PASS. 수십 번 저장해도 최종 source와 실행 본문이 일치하며 stale 적용 0회.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G6을 구현해라. watcher 이벤트를 곧바로 patch로 연결하지 말고 stable snapshot과 revision을 거치게 해라. 중복 이벤트·빠른 저장·rename·부분 쓰기·watcher overflow·Worker 지연을 테스트해라. 에이전트용 edit batch 장벽과 stale 결과 폐기를 구현하고 미완성 배치를 timeout으로 자동 적용하지 마라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G7

<a id="phase-G7"></a>

### Editor UI와 에이전트 명령 연결

모든 기능이 실제 조작 가능한 도구가 되도록 한다.

NOT RUN

1. Overview/Methods/Diagnostics/Activity/Compatibility/Settings/Agent 탭과 세션 상태, lease, divergence 표시를 구현한다.
2. Start/Pause/Analyze/Apply/Rollback/RevertAll/StopAndRecompile 명령을 동일 coordinator 경로에 연결한다.
3. 로컬 파일 bridge-info/inbox/outbox, atomic request/results, 멱등성·token·revision·generation·path 제한을 구현한다. 기존 MCP는 확인 후 adapter로 연결한다.
4. 오류 파일 위치 이동과 로그 export를 제공하고, 실제 bridge 명령으로 end-to-end patch/rollback을 시험한다.

**산출물** `UI Toolkit 창과 settings provider` · `AgentBridge 및 protocol.command.schema.json` · `실제 명령 예제` · `evidence/G7/bridge-results.json`

**통과 조건**

버튼/명령이 실제 기능을 수행하고 T30 PASS. UI 성공 메시지가 아니라 generation 및 실제 메서드 관찰로 검증.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G7을 구현해라. UI와 AgentBridge가 같은 coordinator 명령을 사용하게 하고 상태/refresh lease/source divergence를 숨기지 마라. 파일 inbox/outbox로 실제 analyze→apply→rollback을 실행해라. duplicate request·stale session·malformed JSON·path traversal을 거부하는 테스트를 작성해라. 존재하지 않는 MCP 기능을 있는 것처럼 문서화하지 마라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G8

<a id="phase-G8"></a>

### 전체 회귀·부정 테스트·결함 주입

단계별 테스트를 완성된 v1 매트릭스로 통합한다.

NOT RUN

1. T01~T41 전체 테스트를 자동화하거나 불가피한 실제 관찰 절차를 명확히 작성하고, 가능한 자동 검증은 구현한다.
2. Worker 단위 테스트, Unity EditMode, Editor PlayMode 결과를 따로 저장한다. 테스트 fixture에서 사용자 scene을 덮어쓰지 않는다.
3. baseline 오류와 도구 도입 후 신규 오류를 비교한다. 놓친 v1 필수 기능은 구현하고 모든 실패를 수정 후 재실행한다.
4. 현재 Unity/OS 조합만 검증된 호환성으로 기록한다. 미실행 OS는 NOT\_RUN/UNSUPPORTED이지 PASS가 아니다.

**산출물** `완성된 test suites` · `테스트 실행 스크립트` · `evidence/G8/test-results.xml` · `evidence/G8/coverage-matrix.json`

**통과 조건**

필수 positive/negative 테스트 전체 PASS. SKIPPED/NOT\_RUN을 성공 집계에 넣지 않음.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G8에서 T01~T41의 누락 테스트를 채우고 전체 회귀를 실행해라. 실제 Unity 테스트와 Worker 테스트를 분리하고, 실패를 수정한 뒤 영향 범위 회귀를 다시 실행해라. mock 결과나 컴파일 성공만으로 런타임 patch PASS를 만들지 마라. 각 테스트의 환경·소스/모듈 hash·실제 값·로그 경로를 남겨라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G9

<a id="phase-G9"></a>

### 실제 액션 게임 코드에 제한 적용

PoC가 아니라 사용자의 실제 개발 흐름에서 가치를 검증한다.

NOT RUN

1. G0에서 확인한 이동·카메라·전투 메서드 3개를 다시 검토하고 main-thread/NoInlining/IL 지원 조건을 확인한다.
2. 필요한 최소 Editor 조건부 NoInlining과 allowlist를 추가하고 일반 컴파일해 baseline을 만든다. gameplay 구조를 도구에 맞춰 대규모 변경하지 않는다.
3. P01/P02/P03을 각각 저장→patch→관찰→rollback→정상 컴파일 순서로 실행한다. instance/참조/게임 상태와 새 동작을 분리해 단언한다.
4. Animancer 내부를 변경하지 않고 기존 animation state/시간 진행/입력/lock-on이 유지되는지 확인한다. 자동 smoke가 곤란한 부분은 정확한 수동 절차와 실제 관찰을 남긴다.

**산출물** `실제 프로젝트 allowlist` · `최소 annotation diff` · `evidence/G9/P01~P03 결과` · `ProjectUsageGuide.md`

**통과 조건**

3개 실제 기능에서 패치와 원복, 정상 컴파일 후 동작 일치가 확인되어야 함. demo fixture만으로 대체 불가.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G9를 실제 프로젝트 코드에 수행해라. 현재 저장소의 이동·카메라·전투에서 지원 가능한 메서드 3개를 선택하고 최소한의 Editor NoInlining/allowlist만 추가해라. 각 기능의 새 규칙이 반영되면서 기존 객체·상태·Animancer 참조가 유지되는지 실제 Play Mode로 확인해라. 패치 후 정상 컴파일 결과도 같아야 한다. 확인하지 않은 클래스명이나 smoke 결과를 만들지 마라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G10

<a id="phase-G10"></a>

### 성능·장기 사용·리소스 한도 검증

반복 사용해도 Editor가 느려지거나 불안정해지지 않도록 한다.

NOT RUN

1. 명시된 reference machine/대표 assembly 규모에서 저장→적용 50회 이상을 측정하고 debounce/compile/prepare/commit을 분리한다.
2. 최소 60분/100회 패치/20회 Play 전환 시나리오를 실행하고 Editor/Worker memory, handles, threads, assembly 수, generation을 기록한다.
3. 캐시 상한·history 보유·Worker 재시작·세대 한도 경고/중지를 구현한다. memory가 안정적이라는 판단에 raw 증거를 남긴다.
4. 목표 미달이면 측정 기반 최적화를 수행한다. 달성하지 못한 수치를 달성했다고 쓰지 않고 PERF\_VARIANCE와 재현 조건을 기록한다.

**산출물** `Profiler markers` · `evidence/G10/performance.csv` · `evidence/G10/soak-results.json` · `PerformanceReport.md`

**통과 조건**

안전 한도와 누수 방지 테스트 PASS, 성능 원자료 존재. 목표 미달은 수치/원인을 공개하고 수용 기준 변경은 ADR로 명시. 실행 시간을 실제로 확보하지 못했으면 NOT\_RUN.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G10을 실행해라. 문서의 성능 목표를 실측값으로 채우고 60분/100회 패치/20회 Play 전환 검증을 수행해라. 실제로 기다리며 측정하지 않은 장기 테스트를 PASS로 기록하지 마라. 캐시/handle/동적 코드 누적을 계측하고 세대 한도 보호 동작을 시험해라. 목표 미달은 원자료와 원인으로 보고하고 근거 없는 속도 주장을 하지 마라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G11

<a id="phase-G11"></a>

### 정상 빌드·제거 가능성·배포 문서

개발 도구가 게임의 빌드와 유지보수를 오염시키지 않도록 완결한다.

NOT RUN

1. 활성 patch session을 정상 종료하고 원본 소스를 일반 컴파일한 뒤 프로젝트의 기존 Player 빌드를 실행한다.
2. 빌드 결과의 assemblies/plugins/files를 검사하여 도구/Harmony/Worker/fixture가 유입되지 않았음을 확인한다.
3. 도구 비활성화/제거 후 clean compile과 기존 게임 테스트를 실행한다. 개인 경로에 의존하지 않는 재설치 절차를 별도 checkout에서 시험한다.
4. 사용법, 미지원 변경, recovery, agent workflow, dependency lock, source commit, 라이선스 고지, trouble-shooting을 실제 구현에 맞게 마감한다.

**산출물** `evidence/G11/build-report` · `evidence/G11/player-dependency-audit.json` · `Install/Usage/Troubleshooting 문서` · `완성된 ThirdPartyNotices.md`

**통과 조건**

T43~T45 PASS. 설치·제거·정상 빌드를 실제로 하지 못했으면 VERIFIED\_V1 아님.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G11을 실행해라. 패치 상태가 아닌 원본 소스의 정상 컴파일과 실제 Player 빌드를 수행하고 배포물에 개발 도구 의존성이 없는지 검사해라. 패키지 제거 후에도 게임이 컴파일되어야 한다. clean checkout 재설치와 버전/해시/라이선스/사용법/복구 문서를 마감해라. SDK가 없어 빌드를 못 했다면 정확히 NOT_RUN과 필요한 환경을 남겨라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

G12

<a id="phase-G12"></a>

### 독립 검수·잔여 결함 수정·최종 인계

코드 작성과 실제 완료를 분리해 마지막 판정을 내린다.

NOT RUN

1. 완료 체크리스트와 전체 source diff를 검수한다. 실제 호출 경로의 TODO/NotImplementedException/가짜 성공 반환/빈 interface 구현이 없는지 확인한다.
2. 가능하면 다른 에이전트 또는 별도 검수 세션에서 read-only 리뷰를 수행한다. 단일 에이전트면 검수 역할을 분리하되 독립 검수로 과장하지 않는다.
3. 발견된 결함을 수정하고 관련 테스트를 재실행한다. 누락된 필수 증거/실행 환경은 BLOCKED/NOT\_RUN으로 유지한다.
4. 최종 보고서에 지원 범위, 실제 플랫폼, 핵심 변경 파일, 테스트 수와 실패/미실행, 성능 원자료, 빌드 결과, 위험, 다음 실행 명령을 기록한다.

**산출물** `FinalReport.md` · `완성된 progress.json` · `evidence/index.json` · `ReviewFindings.md` · `ReleaseChecklist.md`

**통과 조건**

G0~G11 필수 gate와 최종 체크리스트가 모두 통과해야 VERIFIED\_V1. 그 외 상태는 원인을 숨기지 않고 정확히 분류.

단계별 요청문

```
공통 전제: 이 설계서와 저장소의 기존 지침을 읽고, 앞선 gate의 증거를 확인한 뒤 실행한다.

G12 최종 검수를 수행해라. 문서의 완료 기준과 실제 diff/테스트/빌드 증거를 대조하고 TODO·가짜 성공·미구현 필수 경로를 찾아 수정해라. 수정 후 영향 테스트를 재실행해라. 실제 지원 플랫폼과 미지원 기능을 명시한 FinalReport와 progress.json을 작성해라. 모든 필수 gate가 실제로 통과한 경우에만 VERIFIED_V1을 선언해라.

결과: 변경 파일, 실행 명령, 예상/실제 결과, 증거 경로, 남은 blocker를 기록하고 progress.json을 갱신한다.
```

<a id="master-request"></a>

## 전체 수행 요청문 — Claude / Codex 공통

한 에이전트를 구현 담당으로 정하고 아래 요청문과 설계서 전체를 함께 전달한다. 모델 이름에 따라 기술 요구를 달리하지 않는다. 작업 저장소에 접근할 수 있는 환경에서 실행해야 한다.

MASTER REQUEST

```
이 문서를 설계 참고용으로만 읽고 끝내지 말고, 현재 Unity 저장소에 실제로 동작하는 Project Hot Reload v1을 구현해라.

[작업 목표]
Play Mode에서 기존 객체/필드/참조를 유지한 채, 사전 허용된 기존 메서드 본문 변경을 반영하는 Editor 전용 개발 도구를 완성한다. 공개 동작을 참고한 독립 구현이며 상용 Hot Reload 비공개 코드·서버·인증 로직의 추출/복제는 하지 않는다.

[먼저 읽을 것]
1. 현재 저장소의 AGENTS.md, CLAUDE.md, 기타 프로젝트 지침.
2. 전달한 Unity_HotReload_Design_and_Agent_Handoff.html 또는 DESIGN_SPEC.md 전체.
3. 이미 존재하면 Docs/HotReload/progress.json, Inventory.md, adr/, evidence/.
서로 충돌하면 기존 저장소의 안전 규칙을 우선하고 기술적 차이는 ADR에 기록해라. 위 문서가 저장소에 없다면 실제 전달된 문서를 읽고 Docs/HotReload에 배치하되 기존 파일을 덮어쓰지 마라.

[실행 방식]
G0부터 G12까지 순서대로 구현하고 검증해라. 간단한 불명확성은 실제 저장소를 조사하고 가장 보수적인 선택을 기록하며 진행해라. 매 단계마다 계속할지 묻거나 계획만 제시하고 멈추지 마라. 다만 선행 안전 gate가 실패하면 그 문제를 먼저 해결하며, 해결되지 않은 백엔드 위에 나머지를 완료했다고 표시하지 마라.
사용자 미커밋 변경은 보존한다. reset/stash/대규모 파일 삭제/임의 엔진 업그레이드/전역 설정 변경/자동 commit·push는 하지 마라. 필요 시 격리된 fixture 프로젝트 또는 worktree를 사용하라. 테스트용 fixture에서의 정상 컴파일·Play 진입/종료는 작업 범위에 포함한다. 기존 사용자의 진행 중 게임 세션 종료나 저장되지 않은 scene 폐기는 승인 없이 하지 마라.

[필수 기술 계약]
- Worker가 Unity의 실제 source/define/reference/compiler option을 반영해 변경 어셈블리 전체를 컴파일한다. 변경 .cs 하나만 독립 컴파일하는 방식으로 마감하지 마라.
- A0(실제 로드 원본), W0(Worker baseline), Eg(현재 실행 세대), Sr(소스 snapshot)를 구분한다.
- 새 게임 assembly를 Editor에 Assembly.Load하지 않는다. Cecil 등으로 IL을 읽고 원본 Type/FieldInfo/MethodInfo에 의미 기반으로 재연결한다.
- G1에서 실제 Unity의 private/static 상태, 기존 caller/delegate, Vector3 반환, 반복 교체 및 원복을 먼저 증명한다.
- 검증된 Harmony 버전으로 메서드 본문을 교체한다. 직접 unsafe 함수 포인터 덮어쓰기는 도입하지 마라.
- allowlist + 사전 Editor 한정 NoInlining + 메인 스레드 대상만 v1에서 허용한다.
- v1 지원 매트릭스의 필수 항목과 모든 거부 조건을 구현한다. EH, locals, labels, generic operand, ref/out API 호출을 TODO로 남기지 마라.
- prepare/commit/restore, 소유 owner, 단조 증가 세대, stale revision 방지, 부분 실패 전체 원복, Quarantined를 구현한다.
- 롤백은 코드만 복원한다. 게임 월드 상태나 디스크 소스를 복원한다고 표현하지 마라.
- RefreshLease는 소유권과 획득/해제를 정확히 관리한다. 수동 Refresh를 차단할 수 있다고 가정하지 마라.
- 여러 파일을 수정하는 에이전트 작업에는 begin/end edit batch를 사용한다.
- UI와 로컬 AgentBridge가 같은 coordinator를 호출하게 한다. 미존재 MCP 도구를 가정하지 마라.
- 도구/Harmony/Worker/fixture가 Player 빌드에 포함되지 않아야 한다.

[구현 품질]
실제 사용 경로의 빈 구현, NotImplementedException, TODO, 가짜 성공 반환, reflection 호출로 동작을 흉내 내는 경로를 남기고 완료 처리하지 마라. 무조건 예외를 무시하거나 모든 필드를 public으로 바꾸는 우회를 하지 마라. 코드 구조와 주석은 단순하게 유지하고 새 기능은 필요한 모듈에만 추가해라.

[검증]
각 단계에 테스트를 작성하고 실제 실행해라. Worker 단위 테스트와 Unity EditMode/PlayMode는 별개다. T01~T45 및 P01~P03을 추적하고, 실패는 수정 후 재실행해라. 테스트 XML/JSON·Editor.log·module/source hash·성능 CSV·빌드 의존성 검사 결과를 evidence에 남겨라. 60분 장기 시험을 실제 수행하지 않았다면 NOT_RUN이다. 테스트 명령의 성공 종료 코드만 보지 말고 결과 파일과 실제 assertion을 확인해라.

[진행 보고와 중단 복구]
큰 단계가 끝날 때 구현한 것/검증한 것/막힌 것을 짧게 보고해라. Docs/HotReload/progress.json과 evidence/index.json을 갱신해 다른 에이전트가 이어받을 수 있게 해라. 실행 환경·권한·컨텍스트 한도 때문에 멈추면 수행 가능한 독립 작업은 완료하고, 마지막 검증 gate와 실패 명령·원인·정확한 다음 행동을 기록해라. 비동기 작업을 약속하거나 실행하지 않은 테스트를 통과로 적지 마라.

[최종 출력]
FinalReport.md에 지원 범위/실제 Unity·OS·의존성 버전/핵심 변경 파일/테스트 통과·실패·미실행/성능 실측/실제 Player 빌드 결과/잔여 위험/사용법을 정리해라. G0~G12 필수 gate가 모두 실제 통과한 경우에만 VERIFIED_V1으로 선언한다. 그렇지 않으면 IMPLEMENTED_UNVERIFIED, FAILED_VALIDATION, BACKEND_BLOCKED 중 실제 상황에 맞게 보고해라.

지금은 G0 환경 조사부터 시작해라.
```

**컨텍스트 길이 때문에 끊기는 경우**

문서와 checkpoint가 실제 저장소에 남아 있어야 다음 요청이 재개할 수 있다. 에이전트가 실행 가능한 도구와 컨텍스트 안에서 작업을 진행하도록 하며, 메시지 종료 이후 자동으로 계속 일한다고 가정하지 않는다.

<a id="resume-request"></a>

## 중단 후 재개 요청문

RESUME REQUEST

```
현재 Unity 저장소의 Project Hot Reload v1 작업을 이어서 진행해라.

먼저 기존 저장소 지침, Docs/HotReload/DESIGN_SPEC.md(또는 전달된 HTML), progress.json, Inventory.md, ADR, evidence/index.json을 읽어라. 대화 기억이나 이전 에이전트의 “완료” 표현보다 실제 파일과 테스트 증거를 우선한다.

현재 git diff와 사용자 변경, 실행 중 Editor 버전/PID, module MVID, source/context hash, Worker profile을 다시 확인해라. 환경이나 소스가 달라졌으면 관련 증거의 적용 범위를 재평가하고 필요한 gate를 재실행한다. 이전 세션의 JIT/patch handle을 디스크에서 복구해 재사용하지 마라.

진행 순서:
1. 마지막으로 실제 PASS한 gate와 첫 미완료/실패 gate를 확인한다.
2. 실패의 최소 재현과 blocker를 먼저 해결한다.
3. 다음 gate부터 문서의 구현→컴파일→테스트→수정→재실행 순서를 계속 수행한다.
4. 필수 테스트가 NOT_RUN이면 실행하거나 정확한 환경 blocker를 남긴다. 이를 PASS로 바꾸지 않는다.
5. 각 gate의 변경 파일·실행 명령·실제 결과·증거를 기록하고 progress.json을 갱신한다.

사용자 변경은 되돌리지 않는다. 엔진 업그레이드나 대규모 구조 변경으로 문제를 회피하지 않는다. 이전 작업을 무조건 처음부터 다시 만들거나 계획만 재작성하지 마라. 필요한 검증을 재수행하고 G12 완료 기준까지 진행해라. 실행이 불가능한 항목은 구현 상태와 검증 상태를 분리해 보고해라.
```

<a id="review-request"></a>

## 다른 에이전트에 넘기는 독립 검수 요청문

REVIEW REQUEST

```
Project Hot Reload v1 구현을 독립 검수해라. 우선 read-only로 조사하고 구현 에이전트와 동시에 같은 파일을 수정하지 마라.

설계서 전체, 현재 저장소 지침, git diff, progress.json, ADR, 증거 인덱스, 실제 테스트/빌드 결과를 읽어라. 설명 문장보다 실제 코드 경로와 결과 파일을 우선한다.

특히 확인할 것:
- 새 게임 assembly를 로드하거나 새 객체로 교체해 타입 정체성 문제를 숨기지 않았는가?
- 원본 private/static/상속/generic operand/local/branch/EH 재연결이 정확한가?
- NoInlining의 사전 컴파일과 현재 caller/delegate/Unity callback 동작을 실제로 확인했는가?
- A0/W0/Eg/Sr를 구분하고 소스 원복 시 RemoveOverride를 수행하는가?
- stale revision/이전 session/외부 owner/partial failure를 차단하고 전체 복구 또는 Quarantined로 가는가?
- code rollback을 source/world-state rollback과 혼동하지 않는가?
- RefreshLease와 이벤트 구독이 Domain Reload ON/OFF·예외·종료에서 정확히 정리되는가?
- Worker host BCL, source generator, ILPP 차이를 무시하지 않는가?
- unsupported 코드를 실수로 허용하거나 오류를 성공으로 반환하지 않는가?
- Player 산출물에 도구/Harmony/Worker/fixture가 포함되지 않는가?
- 45개 필수 테스트와 3개 실제 기능 smoke, 장기 성능 시험, 정상 빌드의 증거가 실재하는가?

결과는 심각도 순으로 작성해라. 각 결함에 파일:행, 재현 절차, 기대/실제, 위험, 권장 수정, 필요한 회귀 테스트를 붙인다. 실행하지 못한 항목과 아직 증거가 부족한 항목을 분리한다. 근거 없이 전체 승인하지 마라.

수정 권한을 받은 뒤에만 코드 변경을 수행하고, 변경 후 관련 테스트를 재실행한다. 필수 gate 미충족이면 VERIFIED_V1 판정을 거부하고 정확한 미충족 목록을 제출해라.
```

<a id="checkpoint"></a>

## 진행 상태·증거·인계 형식

`progress.json`은 에이전트가 다른 대화로 넘어가도 작업 상태를 복원하기 위한 기록이다. 이 문서와 동봉된 템플릿의 초기 상태는 모두 NOT\_STARTED / NOT\_RUN이다. 완료 여부를 체크박스나 설명 문장 하나로만 관리하지 않는다.

| 상태 | 사용 조건 |
| --- | --- |
| NOT\_STARTED | 코드/검증을 시작하지 않음. |
| IN\_PROGRESS | 구현 또는 검증 진행 중. 아직 gate 통과 아님. |
| PASS | 해당 gate의 필수 결과를 실제 실행 증거로 확인. |
| FAIL | 실행하여 실제 실패를 확인. 원인/다음 행동 기록. |
| BLOCKED | 선행 실패, 실행 환경/권한 등으로 진행 불가. 단순히 어렵다는 뜻으로 남용하지 않음. |
| IMPLEMENTED\_UNVERIFIED | 구현 파일이 있으나 해당 런타임/빌드를 검증하지 못함. |
| NOT\_RUN | 개별 테스트를 실제 실행하지 않음. PASS 집계에 포함하지 않음. |

checkpoint 예시 — 실제 진행 상태가 아님

```
{
  "schemaVersion": 1,
  "overallStatus": "IN_PROGRESS",
  "currentPhase": "G4",
  "phaseStatus": "BLOCKED",
  "lastVerifiedGate": "G3",
  "implemented": [
    "IL local / label 매핑"
  ],
  "verified": [
    "단순 switch fixture"
  ],
  "notVerified": [
    "중첩 finally의 실제 Unity 실행"
  ],
  "blockers": [
    {
      "code": "UnsupportedIL",
      "description": "예시: 중첩 finally emitter 오류",
      "command": "실제 실행한 명령을 기록",
      "evidencePath": "Docs/HotReload/evidence/G4/실제-로그-경로"
    }
  ],
  "nextAction": "중첩 finally 최소 재현을 수정하고 T12를 실제 Unity에서 재실행",
  "exampleOnly": true
}
```

### 증거 한 건의 필수 필드

`testId, startedAt, finishedAt, toolVersion, UnityVersion, OS/CPU, sourceHash, runtimeModuleIds, expected, observed, result, command, exitCode, logPath, resultArtifactHash`를 기록한다. 결과 파일이 없거나 assertion을 확인할 수 없는 경우 exitCode=0만으로 PASS를 작성하지 않는다.

### 작업 종료 보고 형식

text

```
현재 판정: VERIFIED_V1 / IMPLEMENTED_UNVERIFIED / FAILED_VALIDATION / BACKEND_BLOCKED
실제 검증 환경: Unity / OS / CPU / backend 및 compiler profile
완료 gate: ...
미완료 gate: ...
핵심 변경 파일: ...
테스트: PASS ... / FAIL ... / NOT_RUN ...
실제 프로젝트 smoke: P01 ... / P02 ... / P03 ...
정상 Player 빌드 및 도구 제외 검사: ...
성능: reference machine, sample 수, p50/p95, peak memory, 장기 시험 시간
잔여 제한 / 위험: ...
증거 인덱스: ...
다음에 실행할 정확한 명령 또는 작업: ...
```

<a id="acceptance"></a>

## 최종 완료 체크리스트

다음 항목이 전부 충족되어야 **VERIFIED\_V1**이다. 기능을 지원하지 않기로 한 것은 명시적 v1 경계로 허용되지만, 필수 기능을 미구현 상태로 두고 “지원하지 않음”으로 바꾸면 안 된다. 설계 변경이 필요하면 이유·대안·검증 영향을 ADR로 남긴다.

실제 저장소·Unity·OS·SDK·의존성 버전이 기록되어 있다.

G1의 원본 인스턴스/private/static/호출자/반환 ABI 검증이 실제 Unity에서 통과했다.

컴파일 context와 A0/W0/Eg/Sr가 분리되어 있으며 stale plan을 거부한다.

게임 assembly를 새로 로드하거나 객체를 교체하지 않고 원본 멤버로 IL을 재연결한다.

v1 필수 IL/local/label/EH/참조 경로와 모든 비지원 거부 테스트가 구현되어 있다.

반복 패치·소스 원복·전체 배치 원복·복구 실패 격리가 검증되었다.

NoInlining·allowlist·메인 스레드 경계와 외부 owner 충돌을 지킨다.

Refresh lease·watcher·Worker·이벤트가 Play/reload/quit/예외에서 정리된다.

UI와 AgentBridge에서 실제 analyze/apply/rollback/정상 컴파일 복귀가 동작한다.

45개 필수 테스트와 실제 게임 3개 smoke의 결과가 증거 파일로 존재한다.

장기/성능 시험을 실제로 수행했으며 미달 수치를 숨기지 않았다.

정상 소스 컴파일과 Player 빌드에서 개발 도구 의존성 제외를 확인했다.

도구 제거와 clean checkout 재설치를 실제로 검증했다.

실사용 필수 경로에 빈 구현·TODO·NotImplementedException·가짜 성공 반환이 없다.

라이선스·source commit·binary hash·호환성·미지원 범위·사용법이 실제 구현과 일치한다.

최종 검수 결함을 수정하고 관련 회귀를 재실행했으며 progress/evidence/FinalReport가 일치한다.

**이 문서 자체의 완료와 실제 구현의 완료는 다르다**

이 전달물은 설계·단계·요청문·검수 기준을 완성한 문서 패키지다. 실제 소스 구현과 Unity 실행 결과는 전달받은 에이전트가 저장소에서 만들어야 한다. 이 문서의 NOT\_RUN 표시는 그 구분을 위한 것이며 기능 실패 판정이 아니다.

<a id="extensions"></a>

## v1 이후의 확장 — 자동으로 약속하지 않는 범위

| 확장 | 먼저 해결해야 하는 별도 문제 | v1와의 관계 |
| --- | --- | --- |
| 새 helper 메서드 추가 | 원본 타입에 metadata를 추가할 수 없다는 제약, 호출을 다른 실행 단위로 lowering하는 규칙, private/type identity·rollback. | v1 완료 후 별도 ADR/테스트/지원 매트릭스로 시작. |
| async/coroutine/lambda/local function | 컴파일러 생성 타입과 필드, 기존 delegate·진행 중 state machine의 수명, 새 인스턴스만 적용하는 의미 정의. | 기존 실행 중 작업을 무조건 새 코드로 교체한다고 약속하지 않음. |
| 필드 추가·가상 필드 | 객체별 side storage, 값/참조/byref 의미, 초기값, static 공유, serialization/Inspector/Undo/GC 통합. | 단순 dictionary 하나 추가하는 작업으로 축소하지 않음. |
| 범용 NoInlining 없는 패치 | 이미 인라인된 호출자 탐지/재JIT/도메인 및 런타임 차이. | 현재 NoInlining 요구를 제거하려면 별도 correctness 증거 필요. |
| 다른 OS/CPU | 실제 Unity 런타임·patch backend·Worker 실행/배포·ABI 검증. | 문서의 이식 가능성과 실제 지원 판정을 구분. |
| 공식 MethodReload 어댑터 | 현재 설치 가능한 package의 실제 API/언어/호환성 확인과 v1 요구 비교. | 공식 경로로 전환하더라도 기능 동등성을 검증하기 전에는 대체 완료라고 하지 않음. [[S07]](#source-07) |
| IL2CPP/Player/콘솔 | Editor와 다른 실행 모델·코드 서명·배포/플랫폼 정책·인터프리터 등 별도 백엔드. | 이번 v1의 필수 작업이 아님. |

상용 제품의 공개 지원 범위는 이보다 넓다. 자체 도구가 그 기능을 모두 제공한다고 표기하지 않는다. 이번 목표는 불안정한 전체 복제가 아니라 **명시된 범위에서 끝까지 구현·검증·운영 가능한 개발 도구**다. [[S03]](#source-03)

<a id="sources"></a>

## 근거 자료와 참고 범위

다음 자료는 2026-11-12에 확인한 1차 자료다. 라이브 문서는 이후 바뀔 수 있으므로 구현 에이전트는 실제 선택 버전에 맞는 문서를 다시 확인한다. 사실 확인과 본 설계의 제안을 구분했으며, 링크된 제품 내부 코드를 확인하거나 Unity 실행을 수행했다는 의미는 아니다.

S01

<a id="source-01"></a>

### [S01] [Unity Asset Store · Hot Reload](https://assetstore.unity.com/packages/tools/utilities/hot-reload-edit-code-without-compiling-254358)

제품 식별과 공급자·라이선스 표기 확인. 상용 코드의 분석·복제 권한을 의미하지 않는다.

<https://assetstore.unity.com/packages/tools/utilities/hot-reload-edit-code-without-compiling-254358>

S02

<a id="source-02"></a>

### [S02] [Hot Reload · Getting Started](https://hotreload.net/documentation/getting-started)

변경 코드 컴파일과 Unity Auto Refresh 제어라는 공개 동작 확인.

<https://hotreload.net/documentation/getting-started>

S03

<a id="source-03"></a>

### [S03] [Hot Reload · Features](https://hotreload.net/documentation/features)

상용 제품의 공개 기능 범위. 자체 구현의 지원 보증으로 전용하지 않는다.

<https://hotreload.net/documentation/features>

S04

<a id="source-04"></a>

### [S04] [Hot Reload · Field Support](https://hotreload.net/documentation/fields)

필드 변경·초기화·Inspector 통합의 별도 제약 확인.

<https://hotreload.net/documentation/fields>

S05

<a id="source-05"></a>

### [S05] [Hot Reload · Technical Questions](https://hotreload.net/documentation/technical-questions)

패키지 코드·source generator·IL 후처리 관련 공개 설명.

<https://hotreload.net/documentation/technical-questions>

S06

<a id="source-06"></a>

### [S06] [Afterhours-Studio · Unity Reloader](https://github.com/Afterhours-Studio/unity-hot-reloader)

README의 Roslyn + Harmony 구조 및 MIT 표기 확인. 실제 반입 전 파일별 라이선스·의존성·커밋 감사 필요.

<https://github.com/Afterhours-Studio/unity-hot-reloader>

S07

<a id="source-07"></a>

### [S07] [Unity · MethodReload 소개 (2026-10-22)](https://unity.com/blog/skipping-domain-reload-faster-iteration-using-the-methodreload-feature)

실험적 공식 대안의 인터프리터 구조·태깅·언어 제약. 글의 패키지 버전은 발표 당시 값이다.

<https://unity.com/blog/skipping-domain-reload-faster-iteration-using-the-methodreload-feature>

S08

<a id="source-08"></a>

### [S08] [Unity · CompilationPipeline.GetAssemblies](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html)

Unity 컴파일 파이프라인에서 어셈블리 목록을 얻는 출발점.

<https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html>

S09

<a id="source-09"></a>

### [S09] [Unity · Compilation.Assembly](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.Assembly.html)

소스·참조·define 등 실제 컴파일 컨텍스트 수집.

<https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.Assembly.html>

S10

<a id="source-10"></a>

### [S10] [Unity · ScriptCompilerOptions](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.ScriptCompilerOptions.html)

컴파일 옵션 수집. 실제 설치 버전에서 제공되는 필드를 확인한다.

<https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.ScriptCompilerOptions.html>

S11

<a id="source-11"></a>

### [S11] [Unity · DisallowAutoRefresh](https://docs.unity3d.com/ja/current/ScriptReference/AssetDatabase.DisallowAutoRefresh.html)

중첩 카운터이며 명시적 Refresh를 차단하지 않는다는 API 계약.

<https://docs.unity3d.com/ja/current/ScriptReference/AssetDatabase.DisallowAutoRefresh.html>

S12

<a id="source-12"></a>

### [S12] [Unity · AllowAutoRefresh](https://docs.unity3d.com/ja/current/ScriptReference/AssetDatabase.AllowAutoRefresh.html)

획득·해제 횟수 균형 및 과도한 해제 위험.

<https://docs.unity3d.com/ja/current/ScriptReference/AssetDatabase.AllowAutoRefresh.html>

S13

<a id="source-13"></a>

### [S13] [Unity · AssemblyReloadEvents](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssemblyReloadEvents.html)

도메인/어셈블리 재로드 전후 정리 지점.

<https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssemblyReloadEvents.html>

S14

<a id="source-14"></a>

### [S14] [Unity · Domain Reload 비활성화](https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/code-reload-serialization/code-reloading-editor/domain-reloading)

Play Mode 간 static 필드·이벤트가 유지될 수 있다는 생명주기 제약.

<https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/code-reload-serialization/code-reloading-editor/domain-reloading>

S15

<a id="source-15"></a>

### [S15] [Harmony 2 · Transpiler](https://harmony.pardeike.net/v2/articles/patching-transpiler.html)

메서드 IL 변환과 패치 재적용 시 transpiler 재실행 동작.

<https://harmony.pardeike.net/v2/articles/patching-transpiler.html>

S16

<a id="source-16"></a>

### [S16] [Harmony 2 · CodeInstruction](https://harmony.pardeike.net/v2/articles/patching-transpiler-codes.html)

Label, LocalBuilder, exception block 처리 계약.

<https://harmony.pardeike.net/v2/articles/patching-transpiler-codes.html>

S17

<a id="source-17"></a>

### [S17] [Harmony 2 · Edge Cases](https://harmony.pardeike.net/v2/articles/patching-edgecases.html)

인라이닝·제네릭·기타 런타임 패치 위험.

<https://harmony.pardeike.net/v2/articles/patching-edgecases.html>

S18

<a id="source-18"></a>

### [S18] [Harmony 2 · Basics](https://harmony.pardeike.net/v2/articles/basics.html)

패치 owner 식별 및 의존성 관리의 출발점. v3 API와 혼용 금지.

<https://harmony.pardeike.net/v2/articles/basics.html>

S19

<a id="source-19"></a>

### [S19] [Microsoft · Roslyn Semantic Analysis](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis)

Compilation은 소스·참조·옵션을 함께 다룬다. 단일 파일 텍스트 비교만으로 충분하지 않다.

<https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis>

S20

<a id="source-20"></a>

### [S20] [Mono · Cecil](https://www.mono-project.com/docs/tools%2Blibraries/libraries/Mono.Cecil/)

어셈블리를 실행용으로 로드하지 않고 CIL과 메타데이터를 읽는 기능.

<https://www.mono-project.com/docs/tools%2Blibraries/libraries/Mono.Cecil/>

S21

<a id="source-21"></a>

### [S21] [Microsoft · Assembly load/unload](https://learn.microsoft.com/en-us/dotnet/standard/assembly/load-unload)

전통적 AppDomain 방식과 CoreCLR AssemblyLoadContext를 구별해야 하는 이유.

<https://learn.microsoft.com/en-us/dotnet/standard/assembly/load-unload>

S22

<a id="source-22"></a>

### [S22] [Microsoft · FileSystemWatcher](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher)

중복 이벤트·버퍼 오류 등을 고려해야 하는 파일 감지 API.

<https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher>

S23

<a id="source-23"></a>

### [S23] [Unity Test Framework · Command Line](https://docs.unity.cn/Packages/com.unity.test-framework%401.4/manual/reference-command-line.html)

EditMode/PlayMode 명령행 테스트의 기본 형태. 설치된 Test Framework 버전 문서로 재확인.

<https://docs.unity.cn/Packages/com.unity.test-framework%401.4/manual/reference-command-line.html>