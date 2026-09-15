# 차기 프로젝트를 위한 두 Unity 프로젝트 분석

> 작성일: 2026-09-14 · 대상: UPlayground / UPlayBall_Unity · 방식: 로컬 문서·설정·코드 정적 분석
> 차기 프로젝트의 장르·플랫폼·인원·일정은 미정이다. 따라서 공통 출발 구조와 장르별 선택안을 제시한다. 이 문서의 제안은 신규 프로젝트용이며 기존 프로젝트의 변경 지시가 아니다.

## 1. 핵심 판단

차기 프로젝트는 **UPlayBall의 규칙·진행·표현 분리와 재현 가능한 검증 방식을 기본으로 삼고, UPlayground의 액션 실행·카메라·콘텐츠 저작 기술을 필요한 만큼 결합하는 방향**이 적합하다. 두 저장소를 통째로 합치거나 어느 한쪽을 복제하면 게임별 데이터, 초기화 순서, UI와 플러그인 의존까지 함께 따라온다.

우선 가져갈 것은 기능의 개수보다 세 가지 개발 능력이다.

- **재현:** 같은 입력에서 같은 결과를 확인하고, 실패 원인을 기록하는 능력.
- **저작:** 기획자가 데이터를 수정하고 오류를 출시 전에 발견하는 능력.
- **체감:** 입력·판정·연출·진행이 플레이어에게 명확하게 연결되는 능력.

UPlayground는 액션 시스템과 저작 도구가 풍부하다. UPlayBall은 화면 없이도 실제 규칙과 장기 진행을 검증할 구조가 강하다. 어느 쪽도 이번 분석만으로 출시 품질이나 재미가 검증되었다고 판단할 수는 없다. 코드와 테스트 파일의 존재는 실제 플레이 완성도를 대신하지 않는다.

**첫 산출물은 범용 프레임워크가 아니라, 저장·복원과 계측까지 포함한 작은 플레이 루프여야 한다.** 루프의 재미가 확인된 뒤 필요한 기술만 추출한다.

## 2. 조사 범위와 사실의 등급

| 구분 | 이번 문서에서의 의미 |
| --- | --- |
| 확인 | 현재 작업 디렉터리의 설정·asmdef·코드·파일 목록에서 직접 확인 |
| 기록 | 기존 보고서가 주장하는 구현·테스트·성능 결과. 이번에 재실행하지 않음 |
| 판단 | 확인한 구조와 기록을 바탕으로 한 설계 해석 |
| 제안 | 차기 프로젝트에서 검증하고 채택할 작업·기준 |

조사한 경로는 `C:/UsingProject/UnityProject/UPlayground`와 `C:/UsingProject/UnityProject/UPlayBall_Unity`다. `Assets/02.Scripts`, `Assets/Tests`, 주요 데이터 폴더, `Packages/manifest.json`, 프로젝트 설정, 온보딩·현황·최근 보고서, Headless 프로젝트 정의를 확인했다. 전체 코드의 줄 단위 감사, 모든 에셋의 참조 무결성 검사, 이미지·사운드 품질 평가는 수행하지 않았다.

UPlayground의 HEAD는 `ff25b9ae`(2026-08-28), UPlayBall은 `33bfadd`(2026-09-14)로 확인했다. UPlayground에는 FlowGraph·대화·데이터 등 다수의 기존 미커밋 변경이 있다. 따라서 **이 문서는 HEAD만이 아니라 조사 시점 작업 트리의 스냅샷**을 설명한다. UPlayBall의 상태 출력에는 미추적 `.claude/`가 있었다. 커밋 해시는 전체 조사 내용의 재현을 보장하지 않는다.

이번 작업은 게임 코드를 변경하지 않았고 Unity 컴파일, Play Mode, Player Build, 대량 시뮬레이션을 실행하지 않았다. 검색 도구 `rg`가 실행되지 않아 PowerShell로 파일 열거와 텍스트 검사를 수행했다. 다음 수치는 품질 점수가 아닌 규모 파악용이다. HTML은 제목·표·본문 대응, 로컬 링크와 앵커, 태그 구조를 정적으로 검사했다. 연결 가능한 브라우저가 없어 실제 화면 렌더 검수는 수행하지 못했다.

## 3. 프로젝트 비교와 현재 규모

| 항목 | UPlayground | UPlayBall_Unity |
| --- | --- | --- |
| 플레이 경험 | 실시간 TPS 액션, 이동·공격·방어·탐색 | 선수 커리어와 구단주 운영, 확률 경기·성장·시즌 진행 |
| 핵심 품질 질문 | 입력 의도대로 움직이는가, 맞고 피한 이유를 알 수 있는가 | 선택이 결과를 바꾸는가, 다음 경기와 시즌을 진행하고 싶은가 |
| Unity 설정 | 6000.3.21f1 | 6000.3.21f1 |
| 공통 패키지 선언 | URP 17.3.0 / Input System 1.20.0 / Addressables 2.9.1 / Test Framework 1.6.0 | 동일 버전 선언 |
| 자체 스크립트 파일 | 1,431개 | 892개 |
| Assets/Tests의 C# 파일 | 80개: EditMode 74 / Editor 3 / PlayMode 3 | 246개: EditMode 233 / PlayMode 13 |
| 활성 빌드 씬 등록 | 6개: Boot·Title·Loading·인게임 3개 | 4개: Boot·Loading·Management·Match |
| 주요 기술 자산 | Ability·MotionSet·BT·KCC·카메라·FlowGraph·데이터 도구 | 결정론 RNG·경기 엔진·장기 진행·Headless 회귀·공용 UI 셸 |
| 중심적인 구조 부담 | Actor·Data·UI의 연결 범위, Unity 및 외부 플러그인 의존 | 성장·월드·저장 상태의 규모, 초기 준비 비용, UI 조정 계층의 비대화 |

측정 규칙: 스크립트 수는 각 저장소의 `Assets/02.Scripts/**/*.cs`를 재귀 집계했다. Editor 및 이 폴더 내부 테스트도 포함하며 외부 에셋·Library·Packages는 제외했다. 테스트 수는 `Assets/Tests/**/*.cs`의 **파일 수**다. 테스트 케이스 수·실행 수·커버리지 비율이 아니다. UPlayground의 MotionSet 내부 테스트는 별도 경로이므로 80개에 포함되지 않는다. 패키지 수치는 manifest 선언이며 새 프로젝트 호환성을 검증한 결과가 아니다. [G01] [B01]

폴더별로 UPlayground는 GameActor 442, Data 281, UI 158, Editor 106, Tool 92, Manager 84개다. UPlayBall은 Presentation 347, Game 289, Simulation 110, Core 105, Editor 41개다. Game에는 하위 Unity 폴더가 포함되고 Data에도 Editor가 포함되므로 이 숫자를 런타임 asmdef별 크기로 해석하면 안 된다.

## 4. UPlayground 분석

### 4.1 액션 게임의 제작 기반

GameActor와 상태 머신, KCC 이동, Animancer 기반 MotionSet, Ability 데이터, BT 판단이 연결된 구조다. 각 계층의 의도가 분명하다. **BT는 행동을 선택하고, GAS는 능력 실행 조건·수치·비용을 정의하며, MotionSet은 실행 타이밍을 표현한다.** 이 구분은 차기 액션 프로젝트에서도 유지할 가치가 있다. [G02] [G03]

현재 Payload 구현은 `AbilityAttackInfo.motionKey`의 유효성으로 모션 실행 가능성을 구분하고, 공격 실행에는 추가로 `baseInfo`를 요구한다. 따라서 모든 모션 Ability에 공격 히트 데이터를 강제하는 식의 일괄 검증은 잘못될 수 있다. 검증기는 이동·연출용 실행과 실제 공격을 구분해야 한다. [G04]

플레이어 경험 관점에서 재사용할 단위는 공격 클래스 하나가 아니라 **입력 → 선입력/활성화 → 이동·모션 → 타격 → 피격 피드백 → 캔슬/복귀**의 연속 경로다. 히트스톱, 카메라, 방어 성공 피드백이 빠진 이식은 컴파일이 되어도 원래의 전투 감각을 보존했다고 볼 수 없다.

### 4.2 모듈화는 진행되었지만 독립성의 수준이 다르다

| 실제 모듈 | 확인한 경계 | 이식 해석 |
| --- | --- | --- |
| UPlayGround.Core | noEngineReferences=true, CurrencyWallet·MerchantTradeCalculator·PartyRosterService 3개 C# 파일 | 작은 순수 규칙 후보. 이름이 Core라고 전체 기반 시설이 들어 있는 것은 아님 |
| UPlayGround.Ability.Core | asmdef의 사용자 참조 목록은 비어 있지만 UnityEngine using과 SO 타입 존재 | 프로젝트 비의존과 엔진 비의존을 구분해야 함 |
| UPlayGround.Ability.UPlayGround | Ability.Core·MotionSet.Core·Data 참조 | 프로젝트 전용 실행 Payload와 연결을 교체해야 함 |
| UPlayGround.MotionSet.Core | 사용자 asmdef 참조는 비어 있으나 UnityEngine·Animation/에셋 관련 타입 사용 | Unity용 타임라인 모듈 후보 |
| UPlayGround.MotionSet.Animancer | MotionSet.Core·Animancer 참조 | 애니메이션 백엔드 어댑터가 분리되어 있음 |
| UPlayGround.Camera | Data·Contracts·Core·UniTask·InputSystem·URP Core 참조 | 호스트 어댑터는 있지만 단독 복사만으로 완결되지 않음 |
| UPlayGround.Actor / UI | Actor는 Camera와 전투·이동 플러그인, UI는 Actor·Camera 등 참조 | 프로젝트 조립과 화면·액터 전체 복사는 이식 비용이 큼 |
| UPlayGround.FlowGraph | Core·Data·Contracts 참조 | 흐름 저작 후보이나 노드의 서비스 계약까지 함께 점검해야 함 |

위 표는 실제 asmdef와 코드에서 확인했다. **Ability.Core의 일부 런타임 파일이 순수 C#이어도 해당 어셈블리 전체가 순수 C#이라는 뜻은 아니다.** `AbilityCoreTypes`, `AbilityTaskGraphSO`, `AttributeProfileSO` 등에 Unity 의존이 있다. MotionSet.Core도 같은 구분이 필요하다. [G05] [G06]

### 4.3 카메라와 초기화

`ICameraRuntimeAdapter`로 입력·설정·월드 대상·에셋·시간 제어를 연결하고, UPlayground 구현은 Camera 밖의 Manager 계층에 둔다. 이 구조는 외부 재사용의 좋은 출발점이다. 다만 `CameraTargetInfo`에는 `MonsterActorGrade` 같은 Data enum이 남아 있으며 asmdef도 Data와 Contracts를 참조한다. 이식 가능한 어댑터가 있다는 사실과 제품화된 독립 패키지라는 평가는 구분한다. [G07]

GameManager는 초기화 상태·실패 사유·매니저별 초기화 시간을 노출한다. Services는 중복 계약을 거절하고 Play Mode 재진입 때 레지스트리를 초기화한다. 이러한 **초기화 실패의 관측과 수명주기 정리**는 가져갈 가치가 있다. 반면 차기 순수 규칙 계층에서는 정적 서비스 조회 대신 필요한 계약을 생성자로 전달하는 편이 테스트 범위를 좁힐 수 있다. [G08]

### 4.4 콘텐츠와 제작 도구

FlowGraphRunner는 진입점 상태를 러너에 보관하고, 그래프 실행 추적·취소·예외와 프레임 내 즉시 실행 상한을 갖는다. 에셋 공유로 런타임 진행 상태가 섞이지 않게 한 점과 추적 정보는 재사용 가치가 있다. 현재 FlowGraph 일부가 미커밋 변경 상태이므로 추출 기준은 별도로 고정해야 한다. [G09]

Ability Editor, MotionSet 타임라인, SO Spreadsheet, 통합 툴 런처는 데이터 제작 비용을 줄이는 자산이다. 다만 편집 도구를 옮길 때는 런타임 데이터 스키마·Undo·에셋 식별·실패 복구까지 함께 가져가야 한다. 기존 규칙에 있는 GUID/path 정확 일치, 모호한 이름 거절, Undo 전체 롤백은 차기 데이터 파이프라인의 기본 계약으로 적합하다. P09 빌더는 기존 지침상 완전한 transaction으로 간주되지 않으므로 안전한 범용 import 기반으로 그대로 채택하지 않는다. [G02]

### 4.5 구조상의 주의점

실측 파일 크기는 GameplayAbilityEditorWindow 2,916줄, MotionSetDrawer 2,825줄, TimelineView 2,733줄, ActorAnimator 2,124줄이다. 크기만으로 결함을 단정하지는 않지만, 추출 시 UI·모델·명령·검증이 어느 단위로 나뉘는지 우선 조사할 신호다. 파일을 나누는 것보다 변경 이유와 테스트 경계를 분리해야 한다.

기존 모듈화 보고서는 2026-07-17 Phase 6 빌드·참조 검사 통과를 기록하지만 Phase 7 카메라 스모크는 미완료로 남긴다. 현재는 이후 코드 변경도 많아 **과거 0건 기록을 현재 무결성 보증으로 인용하면 안 된다.** Dryad·Training Dummy Motion 미해결에 관한 기존 지침도 이번 조사에서 현재 에셋을 재검증하지 않았으므로 과거 주의사항으로만 취급한다. [G10]

## 5. UPlayBall_Unity 분석

### 5.1 순수 규칙부터 표현까지 이어지는 계층

의존 화살표는 사용하는 쪽에서 사용되는 쪽으로 읽는다. 아래는 주 흐름이며, 실제 Presentation은 Simulation·Core도 직접 참조한다.

```text
Presentation → Game.Unity → Game → Simulation → Core
                      Unity 경계 │ 순수 C# 영역
Editor → 저작·검증 대상 모듈
```

Core·Simulation·Game의 asmdef는 `noEngineReferences: true`를 켠다. 해당 폴더에서 `Game/Unity`를 제외하고 검사한 행 시작 `using UnityEngine/UnityEditor`는 0건이었다. 이 검사는 완전한 컴파일 검증을 대신하지 않지만, 설정과 소스 양쪽에서 경계 의도를 확인한다. [B02]

`Baseball.Game.Headless.csproj`는 Assets의 Game 소스를 직접 포함하고 `Game/Unity/**`를 제외한다. 현재 TargetFramework는 `net10.0`이다. Unity 생성 DLL을 재활용하지 않고 실제 소스로 외부 회귀를 구성하는 방식은 차기 프로젝트에서 우선 가져갈 개발 자산이다. .NET 러너와 Unity의 실행 환경은 다르므로 양쪽 결과 대조는 여전히 필요하다. [B03]

### 5.2 결정론과 출력 분리

PCG32 RNG와 Seed 파생, 입력·규칙의 버전 관리, 이벤트 소비 계약을 사용한다. `IMatchEventSink.Record(in MatchEvent)`와 버퍼/무출력 소비자는 계산과 중계를 분리한다. 단, 이벤트가 struct라고 전체 경로가 무할당인 것은 아니다. 실제 버퍼는 List를 사용하고 `ToArray`도 제공하므로 버퍼 증가·결과 복사의 할당은 별도로 측정해야 한다. [B04]

`MatchExecutionProfile`은 엔진 종류, 외부 판단 입력, 이벤트 출력, 판단 추적을 분리한다. DetailedInteractive·DetailedBackground·AggregateBackground가 존재한다. **관전 출력을 생략하는 것과 계산 해상도를 낮추는 것은 서로 다른 최적화다.** 현재 지침은 구단주 모드의 다른 조에만 간이 타석 경로를 허용한다. 차기 프로젝트에서도 간이 경로를 열기 전에 동일 규칙 범위와 통계 허용 오차를 먼저 정의해야 한다. [B05] [B06]

같은 Seed만 저장하면 모든 변경 후에도 결과가 영구히 같아지는 것은 아니다. RNG 호출 순서, 입력 순서, 규칙·콘텐츠 버전이 달라지면 결과가 달라질 수 있다. 차기 회귀 기록은 Seed와 함께 입력 스냅샷, 실행 프로필, 코드 기준, 데이터 버전을 남겨야 한다.

### 5.3 플레이 루프와 UI

선수 모드와 구단주 모드는 의사결정 주체가 다르다. 선수 모드의 감독 판단과 구단주 모드의 직접 운영을 구분하면서 경기 규칙을 공유한다. 차기 게임에서도 모드가 여러 개라면 공통 계산과 모드별 권한을 분리하고, 화면 조건문으로 권한을 대신하지 않는 것이 좋다. [B06]

공용 셸의 `SharedGameShellPresenter`는 View, 모드 Profile, 상태 공급자를 주입받고 Route의 존재·노출·활성 조건을 검사한다. 화면 외형 공유보다 **탐색 계약·뒤로 가기·상태 스냅샷**을 재사용하는 방식이 더 유용하다. 실제 Owner/Career 화면은 게임 전용 흐름이 많아 그대로 범용 UI로 옮기는 것은 권하지 않는다. [B07]

### 5.4 저장과 진행 원자성

현재 코드의 버전 상수는 `NewGameFlow.CurrentSaveVersion=21`, `ManagerHistoricalSaveAdapter.CurrentSaveVersion=41`, 별도 `CareerSaveAdapter.CurrentSaveVersion=1`이다. 서로 다른 저장 구조의 상수이므로 하나의 프로젝트 버전처럼 합치지 않는다. AGENTS에 남은 v16 설명이나 구현 현황의 v28/v30 요약보다 현재 코드와 최신 정책을 우선해야 한다. [B08]

구단주 저장기는 SemaphoreSlim으로 저장 순서를 보호하고, 임시 파일 쓰기·Flush·File.Replace/Move로 파일을 교체한다. 확인한 `ManagerHistoricalSaveJsonStore.WriteAtomic`은 `File.Replace`의 백업 인자를 null로 전달한다. 따라서 이 경로를 지속 백업까지 보장하는 저장기로 표현할 수 없다. 임시 파일 기반 교체와 손상 복구용 백업 정책은 별개다. [B09]

최신 구단주 정책은 수동 저장 전용이다. 성장·경기 명령의 메모리 적용 및 실패 복구와 디스크 저장 시점을 분리한다. 차기 프로젝트에는 이 분리를 가져가되 수동 저장 자체를 모든 장르의 기본값으로 가져가지는 않는다. 체크포인트·자동 저장·수동 슬롯 중 무엇이 플레이 경험에 맞는지는 별도로 결정한다. [B10]

### 5.5 성능과 검증의 실제 한계

최근 보고서는 격리 Unity에서 슬롯 복원 6,593ms, 첫 경기 6,863ms, 다음 경기 1,524ms를 기록한다. 단일 실행과 특정 슬롯의 수치이며 원본 사용자 세션의 지연을 직접 측정한 결과가 아니다. **반복 계산 최적화 이후에도 최초 준비 비용이 남는다는 사례**로 의미가 있다. 본 조사에서 재측정하지 않았다. [B11]

같은 보고서에는 성장판 17건 통과와 별개로 가챠 Fixture의 등급 풀 누락에 따른 6건 실패가 명시되어 있다. 원본 로그에서 작업 스레드의 TextAsset.bytes 접근 오류를 발견했다는 기록도 있으며 첫 경기 지연과의 인과는 미확인이다. 현재 코드에서 재현·해결 여부를 확인하지 않았으므로 열린 검증 항목으로 남긴다. 테스트 수가 많다는 이유로 전체 스위트 정상이나 스레드 안전성을 단정할 수 없다. [B11]

대형 파일은 CareerGrowth.Workspace 2,299줄, CareerMatch 2,101줄, OwnerModeShellCoordinator 2,060줄, ManagerHistoricalSaveAdapter 1,866줄이다. 차기 구조에서는 화면 조정, 순수 진행 명령, 저장 변환을 초기부터 구분하되, 실제 공통 요구가 나타나기 전에 거대한 범용 커리어 프레임워크를 만들지는 않는다.

## 6. 공통 교훈과 차기 프로젝트에서 바꿀 점

| 관찰 | 플레이어/제작에 미치는 영향 | 차기 프로젝트의 기준 |
| --- | --- | --- |
| 두 프로젝트 모두 데이터와 전용 도구가 발전 | 콘텐츠 증가는 빨라지지만 잘못된 참조도 대량 생성 가능 | 저작과 검증을 같은 데이터 스키마에서 생성·실행 |
| 모듈 이름과 실제 엔진 의존 수준이 다름 | 추출 후 예상 밖 참조와 빌드 비용 발생 | 순수 C# / Unity / 프로젝트 전용을 명시하고 컴파일로 강제 |
| 설명 문서보다 코드·정책이 빠르게 변함 | 오래된 저장 버전·구조를 전제로 후속 작업 | 현재 계약 문서 1개, 변경 이력, 검증 보고서를 분리 |
| 작은 명령이 거대한 준비·저장·화면 갱신을 유발할 수 있음 | 버튼 하나에도 기다림이 생김 | 클릭부터 상호작용 복귀까지 전체 경로 계측 |
| 정적 서비스와 조립 계층이 필요 | 초기화 순서와 재진입에 따라 실패 가능 | 순수 규칙은 명시적 주입, Unity 조립부는 부팅·종료 회귀 |
| 자동 검증과 실화면 검수가 서로 다른 결과를 제공 | 계산이 맞아도 입력·가독성·체감은 나쁠 수 있음 | 논리·통계·데이터·실행·플레이 평가를 별도 통과 조건으로 둠 |

전체적으로 기능 추가를 멈추라는 뜻이 아니다. **기능을 더할 때 그 기능이 만드는 선택, 결과 피드백, 제작·검증 비용을 함께 설계하자**는 결론이다.

## 7. 재사용 후보와 이식 조건

| 후보 | 원본 | 결정 | 필요한 작업 | 완료 증거 |
| --- | --- | --- | --- | --- |
| 순수 도메인·진행 계층 분리 | UPlayBall | 우선 채택할 구조 | 차기 규칙으로 작은 Domain/Application 구성 | Unity 참조 없이 컴파일·테스트 |
| RNG·Seed 파생·입력 재현 | UPlayBall | 코드 추출 후보 | 필요한 최소 타입만 복사, 버전·순서 계약 고정 | 같은 입력의 결과·이벤트 비교 |
| Headless 러너 방식 | UPlayBall | 우선 채택 | 새 소스 경로·런타임·테스트 발견 범위 구성 | 실제 게임 경로 실행, 실패 종료 코드 |
| CurrencyWallet 등 작은 규칙 | UPlayground | 개별 추출 후보 | 화폐 단위·상한·거래 실패 의미 검토 | 경계값·실패 시 상태 불변 |
| 저장 원자 교체·명령 복구 | 양쪽 | 패턴 채택 | DTO·파일 IO·저장 정책 분리, 백업/손상 계약 결정 | 중간 실패·재로드·동시 저장 검사 |
| 카메라 어댑터 | UPlayground | 3D 액션이면 우선 후보 | Data/Contracts 의존과 등급 정보 정리, 설정·입력 연결 | 락온·탐색·전투·복귀 스모크 |
| MotionSet Core/Animancer | UPlayground | 모션 저작이 중요할 때 후보 | 백엔드·에셋·이벤트 타입·meta 함께 검토 | 실제 클립의 이벤트·루프·중단 검증 |
| Ability 실행과 저작기 | UPlayground | 조건부 후보 | Unity 정의와 순수 정책의 경계 결정, Payload 교체 | 활성화·취소·비용·중첩·저장 테스트 |
| BT·AgentTick | UPlayground | 다수 액터 AI가 필요할 때 | Actor와 감지·쿼리 연결 점검 | 실제 액터 수에서 틱·할당·반응 지연 측정 |
| FlowGraph | UPlayground | 분기 이벤트 콘텐츠가 많을 때 | 노드 서비스·영속 ID·중단/복원 계약 정리 | 비정상 순서·씬 이탈·저장 복원 |
| 공용 UI 셸 | UPlayBall | 운영형 다화면 게임 후보 | Route·Profile·상태 공급자 개념만 우선 추출 | 모드 권한·복귀 경로·구독 해제 |
| SO Spreadsheet·툴 런처 | UPlayground | 제작 반복이 생길 때 | 데이터 타입 결합 제거, transaction 안전성 검증 | 실패 import 뒤 에셋 변경 0 |
| 액터/매니저/화면 전체 | 양쪽 | 통째 이식 보류 | 차기 수직 슬라이스의 필요성이 입증된 부분부터 | 불필요한 게임 전용 서비스 없이 부팅 |
| 캐릭터·UI 이미지·사운드·외부 원본 | 양쪽 | 기술 분석만으로 채택 불가 | 실제 사용 범위·출처·라이선스·플랫폼 적합성 목록 확인 | 선택한 리소스별 출처와 사용 조건 기록 |

이식 비용은 현재 저·중·고 수준의 설계 판단만 가능하다. 실제 공수는 최소 호스트 프로젝트에서 한 후보를 빌드해 참조 폐쇄 범위를 확인한 뒤 산정한다. 라이브러리의 최신 버전이나 시장 비교는 이번 로컬 분석 범위에 포함하지 않았다.

## 8. 제안하는 차기 프로젝트 구조

### 8.1 장르 공통 최소 구조

```text
NextGame.Domain       상태·값·순수 규칙, 엔진 참조 금지
NextGame.Application  진행 명령·유스케이스·저장 DTO/계약
NextGame.Unity        씬·입력·에셋·파일 IO·시간·플러그인 어댑터
NextGame.Presentation 화면·카메라·소리·연출의 소비자
NextGame.Editor       저작·검증·진단
Tests / Tools         순수 규칙 회귀 + Unity 통합/플레이 검증
```

Domain과 Application이 Unity를 참조하지 않게 하되, 액션 물리까지 억지로 순수 C#에 옮기지는 않는다. KCC·애니메이션·충돌 판정은 Unity 실행 계층에 남기고 비용·쿨다운·성장·보상·진행 조건처럼 분리 가치가 있는 규칙부터 추출한다. 별도 Simulation 어셈블리는 시뮬레이션 규모가 커져 Application과 변경 이유가 달라질 때 추가한다.

이벤트는 결과 전달과 관측을 위한 계약으로 사용한다. 이벤트가 필요한 곳마다 전역 이벤트 버스를 만들거나, 단순 함수 반환까지 이벤트로 바꾸지는 않는다. 시간과 난수는 테스트가 필요한 규칙에만 주입한다. 모든 유틸리티에 인터페이스를 붙이는 식의 확대를 피한다.

### 8.2 데이터 흐름

```text
저작 데이터(SO/JSON)
  → ID·참조·범위·스키마 검증
  → 실행에 필요한 정의/설정
  → 명령 + 현재 상태 + 시간/난수
  → 새 상태 + 결과/진단
  → UI·연출 갱신

상태 스냅샷 → 저장 DTO → 선택한 저장 정책 → 파일 IO
```

SO는 정의, 세이브는 진행 상태를 소유한다. 동일한 비용·공격력·성장 보정을 UI와 실행 코드에서 다시 계산하지 않고, 실행 규칙이 산출한 값과 설명용 내역을 공유한다. 기술 ID와 플레이어 노출 이름을 분리한다. 콘텐츠 버전, 저장 포맷 버전, 시뮬레이션 규칙 버전도 서로 다른 책임으로 둔다.

### 8.3 장르에 따른 시작점

| 차기 방향 | 첫 플레이 루프 | 우선 가져올 것 | 나중으로 미룰 것 |
| --- | --- | --- | --- |
| 3D 액션 | 적의 예고를 보고 공격·방어·회피를 선택, 보상 후 재도전 | 카메라·MotionSet·입력·작은 Ability 경로 + 순수 보상 규칙 | 다수 캐릭터·큰 월드·방대한 Ability 라이브러리 |
| 운영/시뮬레이션 | 편성/성장 선택 → 한 경기/라운드 → 원인 확인 → 다음 선택 | 결정론·Headless·진행 상태·UI 셸 | 수십 시즌·복잡한 수집 경제·대량 역사 데이터 |
| 액션+운영 혼합 | 준비 결정이 실제 전투 결과에 영향을 주고 결과가 성장으로 환류 | 순수 준비/성장 규칙 + Unity 액션 어댑터 | 모든 시스템을 한 번에 통합하는 범용 프레임워크 |

공통으로 초반 실패가 다음 선택에 도움이 되어야 한다. 액션은 공격 예고와 피격 원인, 운영은 능력치·기용·확률·비용의 기여를 드러내야 한다. 분석 문서만으로 최적 장르를 선택할 수는 없으므로 이 셋 중 하나를 제품 방향 결정 단계에서 고른다.

## 9. 단계별 실행 계획과 통과 조건

기간은 팀 규모와 목표 플랫폼이 없어 확정하지 않는다. 아래는 달력 일정 대신 **중단·통과가 가능한 순서**다.

| 단계 | 작업 | 산출물 | 다음 단계로 가는 조건 |
| --- | --- | --- | --- |
| 0. 제품 가설 | 핵심 선택·목표 플레이어·세션 길이·플랫폼·제외 범위 결정 | 1페이지 제품 정의, 성공/실패 가설 | 첫 루프의 재미를 무엇으로 볼지 합의 |
| 1. 최소 기술 검증 | 필요한 모듈 하나를 빈 호스트에 연결 | 부팅 가능한 샘플, 의존 목록, 외부 실행 테스트 | 깨끗한 환경에서 재현, 불필요한 게임 서비스 없음 |
| 2. 수직 슬라이스 | 한 루프를 입력부터 피드백·저장까지 완주 | 플레이 가능한 빌드, 관찰 기록, 기준 성능 | 사용자가 목표·실패 이유·다음 선택을 이해 |
| 3. 제작 검증 | 두 번째 콘텐츠를 같은 도구로 제작 | 추가 적/경기/이벤트, 제작 시간 기록 | 코드 특수 분기 없이 저작·검증·복원 가능 |
| 4. 규모 검증 | 목표 액터/시즌/저장 크기로 확대 | 부하·장기 회귀·실화면 보고서 | 목표 환경 예산 내 실행, 상태·참조 오류 없음 |
| 5. 재사용 안정화 | 실제로 반복된 모듈만 패키지화 | 샘플·테스트·버전·이식 가이드 | 별도 소비 프로젝트에서도 재현 |

단계 2에서 재미가 성립하지 않으면 기능을 늘리기 전에 입력·규칙·피드백 가설을 바꾼다. 단계 3에서 새 콘텐츠마다 분기가 필요하면 도구나 데이터 구조를 수정한다. 구조 추출 자체가 제품 개발을 오래 멈추게 하면 최소 샘플까지 범위를 줄인다.

차기 프로젝트 착수 시 제품 책임자는 장르·핵심 경험·제외 범위를, 기술 책임자는 의존·저장·성능 예산을, 콘텐츠 책임자는 데이터 저작 절차를, 검증 책임자는 회귀와 플레이 판정을 정한다. 한 사람이 여러 역할을 맡아도 결정 항목은 분리해 기록한다.

## 10. 검증과 성능 예산

| 검증 층 | 확인할 내용 | 증거 |
| --- | --- | --- |
| 순수 규칙 | 비용·보상·쿨다운·성장·실패 시 상태 불변 | 실제 실행 코드의 단위/회귀 테스트 |
| 결정론 | 동일 입력·Seed·규칙 버전의 결과 | 정규화 상태 해시와 필요 시 이벤트 순서 비교 |
| 통계 | 우월 전략·능력 민감도·장기 고착 | 여러 독립 Seed, 표본 크기, 분포와 허용 범위 |
| 데이터 | ID 중복·누락·참조·필수 타이밍·불법 조합 | 저작기와 배치 validator 보고서 |
| Unity 통합 | 부팅·씬 전환·입력·종료·재진입·저장 복원 | 에디터 로그, 실제 테스트 빌드 |
| 체감 | 반응·가독성·실패 원인·반복 동기 | 플레이 관찰, 재시도·중단 지점 기록 |
| 배포 경로 | Player Build·리소스 로드·타깃 장치 | 해당 빌드와 실행 결과 |

제안하는 계측은 평균 FPS만이 아니다. 프레임 시간 p95/p99, 입력부터 첫 피드백까지 시간, 냉간/반복 진입, 저장 크기에 따른 직렬화·IO·복원 시간, 첫 콘텐츠 제작과 두 번째 제작의 소요를 함께 본다.

60fps를 제품 목표로 선택한다면 프레임 전체 예산은 약 16.67ms다. 이는 제안의 예시이며 현재 프로젝트가 달성했다는 뜻이 아니다. CPU·GPU 예산, 지원 하드웨어, 대표 씬을 정한 뒤 측정한다. UPlayBall의 기존 Headless 지침은 baseline 대비 1.15배를 경고, 1.30배를 회귀 후보로 삼지만, 이를 다른 프로젝트의 절대 합격선으로 복사하지 않는다. 동일 장치·빌드·입력·냉간 조건에서 기준선을 먼저 만든다. [B03]

자동화는 다음 층으로 나눈다. 빠른 변경 검사는 규칙·데이터·컴파일을 확인하고, 장기 검사는 복수 Seed와 큰 상태를 다룬다. 출시 후보 검사는 실제 Player와 입력 장치에서 수행한다. 느린 대량 검증을 매 수정마다 강제하거나, 빠른 검사만으로 장기 안정성을 주장하지 않는다.

## 11. 이식 전 체크리스트

- [ ] 사용할 작업 트리 스냅샷을 고정하고 미커밋 변경을 포함한 추출 기준을 기록했다.
- [ ] 각 후보의 asmdef, 패키지 버전, 하위 의존, 에셋 로드 키를 목록화했다.
- [ ] 제3자 플러그인·이미지·음원·외부 데이터의 실제 사용 조건을 확인했다.
- [ ] 스크립트 이동 시 meta와 GUID를 보존했고 SerializeReference 타입 이동의 MovedFrom을 검토했다.
- [ ] 타입 매핑이 깨진 상태에서 에셋 저장·일괄 재직렬화를 하지 않았다.
- [ ] 새 호스트가 필요한 입력·시간·에셋·월드·저장 계약을 제공한다.
- [ ] 일반 모션과 공격 Ability의 검증 규칙을 구분했다.
- [ ] 저장 정책과 명령 실패 복구를 분리하고, 저장 실패·손상·버전 불일치 UX를 정했다.
- [ ] 테스트 발견 범위와 제외 대상, 도구 런타임 버전을 기록했다.
- [ ] 실제 게임이 실행하는 경로를 회귀 도구에서도 사용한다.
- [ ] 컴파일·테스트·실화면·성능의 실행/미실행을 각각 표시했다.
- [ ] 두 번째 콘텐츠 제작 후에만 추가 일반화의 필요성을 평가했다.

## 12. 우선 정리할 문서와 결정 목록

차기 저장소에는 현재 계약을 찾기 쉬운 작은 문서 체계를 둔다.

| 문서 | 반드시 포함할 내용 |
| --- | --- |
| PROJECT_BRIEF.md | 핵심 경험, 장르/플랫폼, 목표 세션, 제외 범위, 수직 슬라이스 |
| ARCHITECTURE.md | 실제 asmdef 의존, 상태 소유자, Unity 경계, 조립 순서 |
| DATA_AUTHORING.md | 정의/상태 구분, ID 규약, validator, import 실패 복구 |
| SAVE_CONTRACT.md | 모드별 저장 시점, 스냅샷, 버전, 손상·실패 처리 |
| VALIDATION.md | 실행 명령, 테스트 제외 범위, 기준 데이터, 플레이 통과 조건 |
| reports/ | 날짜·코드/데이터 기준·환경·결과·미검증을 남기는 불변 기록 |
| decisions/ | 중요한 선택의 이유와 재검토 조건 |

UPlayground의 온보딩·CLAUDE·사용자 지침 사이에는 MotionSet와 FlowGraph의 반영 정도가 다르다. UPlayBall도 AGENTS와 구현 현황, 최신 PROJECT 상단 기록의 저장 버전·정책이 다르다. 이 문서들은 의도를 이해하는 데 유용하지만, 차기 문서 생성 시에는 **현재 코드 확인 → 현재 계약 → 과거 작업 기록**의 순서로 대조한다. 기존 원본 문서는 이번 작업에서 수정하지 않았다.

착수 전에 남은 결정은 장르, 목표 플랫폼/입력, 팀 규모, 첫 루프, 저장 정책, 이식할 플러그인, 기준 성능 환경이다. 이 결정 전에도 순수 규칙·재현 도구의 소규모 샘플은 만들 수 있지만, 게임 전체 기반의 대규모 이식은 제품 방향을 정한 뒤 진행하는 것이 적절하다.

## 13. 근거 파일 색인

아래 링크는 이 문서의 위치를 기준으로 두 저장소의 실제 파일을 가리킨다. 문서만 다른 곳으로 복사하면 링크가 깨질 수 있다. Markdown과 HTML의 본문 내용은 동일하며 HTML은 외부 라이브러리 없이 열 수 있다.

### G01

프로젝트 설정과 패키지

- [ProjectSettings/ProjectVersion.txt](<../../ProjectSettings/ProjectVersion.txt>)
- [ProjectSettings/EditorBuildSettings.asset](<../../ProjectSettings/EditorBuildSettings.asset>)
- [Packages/manifest.json](<../../Packages/manifest.json>)

### G02

현재 프로젝트 계약

- [CLAUDE.md](<../../CLAUDE.md>)
- [AGENTS.md](<../../AGENTS.md>)
- [Assets/docs/guide/CODE_AUTHORING_GUIDE.md](<../../Assets/docs/guide/CODE_AUTHORING_GUIDE.md>)

### G03

전투 저작 경계

- [Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md](<../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md>)

### G04

실행 Payload

- [Assets/02.Scripts/Ability/UPlayGround/UPlayGroundMotionAbilityPayloadSO.cs](<../../Assets/02.Scripts/Ability/UPlayGround/UPlayGroundMotionAbilityPayloadSO.cs>)
- [Assets/02.Scripts/Data/Ability/GameplayAbilitySO.cs](<../../Assets/02.Scripts/Data/Ability/GameplayAbilitySO.cs>)

### G05

Ability와 순수 Core

- [Assets/02.Scripts/Ability/Core/UPlayGround.Ability.Core.asmdef](<../../Assets/02.Scripts/Ability/Core/UPlayGround.Ability.Core.asmdef>)
- [Assets/02.Scripts/Ability/Core/AbilityCoreTypes.cs](<../../Assets/02.Scripts/Ability/Core/AbilityCoreTypes.cs>)
- [Assets/02.Scripts/Ability/Core/AbilityTaskGraphSO.cs](<../../Assets/02.Scripts/Ability/Core/AbilityTaskGraphSO.cs>)
- [Assets/02.Scripts/Ability/Core/AbilitySystemRuntime.cs](<../../Assets/02.Scripts/Ability/Core/AbilitySystemRuntime.cs>)
- [Assets/02.Scripts/Core/UPlayGround.Core.asmdef](<../../Assets/02.Scripts/Core/UPlayGround.Core.asmdef>)

### G06

MotionSet 모듈

- [Assets/02.Scripts/MotionSet/Core/UPlayGround.MotionSet.Core.asmdef](<../../Assets/02.Scripts/MotionSet/Core/UPlayGround.MotionSet.Core.asmdef>)
- [Assets/02.Scripts/MotionSet/Core/Data/Motion.cs](<../../Assets/02.Scripts/MotionSet/Core/Data/Motion.cs>)
- [Assets/02.Scripts/MotionSet/Core/Timeline/MotionPlaybackContracts.cs](<../../Assets/02.Scripts/MotionSet/Core/Timeline/MotionPlaybackContracts.cs>)
- [Assets/02.Scripts/MotionSet/Animancer/UPlayGround.MotionSet.Animancer.asmdef](<../../Assets/02.Scripts/MotionSet/Animancer/UPlayGround.MotionSet.Animancer.asmdef>)

### G07

카메라 이식 경계

- [Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md](<../../Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md>)
- [Assets/02.Scripts/Camera/UPlayGround.Camera.asmdef](<../../Assets/02.Scripts/Camera/UPlayGround.Camera.asmdef>)
- [Assets/02.Scripts/Camera/Integration/CameraRuntimeServices.cs](<../../Assets/02.Scripts/Camera/Integration/CameraRuntimeServices.cs>)

### G08

초기화와 서비스

- [Assets/02.Scripts/Manager/GameManager.cs](<../../Assets/02.Scripts/Manager/GameManager.cs>)
- [Assets/02.Scripts/Contracts/Services.cs](<../../Assets/02.Scripts/Contracts/Services.cs>)

### G09

FlowGraph 런타임

- [Assets/02.Scripts/FlowGraph/FlowGraphRunner.cs](<../../Assets/02.Scripts/FlowGraph/FlowGraphRunner.cs>)
- [Assets/02.Scripts/FlowGraph/UPlayGround.FlowGraph.asmdef](<../../Assets/02.Scripts/FlowGraph/UPlayGround.FlowGraph.asmdef>)

### G10

과거 모듈화 검증 기록

- [Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md](<../../Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md>)
- [Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html](<../../Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html>)
- [Assets/docs/onboarding/PROJECT_ONBOARDING_GUIDE.html](<../../Assets/docs/onboarding/PROJECT_ONBOARDING_GUIDE.html>)

### B01

프로젝트 설정과 패키지

- [UPlayBall_Unity/ProjectSettings/ProjectVersion.txt](<../../../UPlayBall_Unity/ProjectSettings/ProjectVersion.txt>)
- [UPlayBall_Unity/ProjectSettings/EditorBuildSettings.asset](<../../../UPlayBall_Unity/ProjectSettings/EditorBuildSettings.asset>)
- [UPlayBall_Unity/Packages/manifest.json](<../../../UPlayBall_Unity/Packages/manifest.json>)

### B02

순수 계층 asmdef

- [UPlayBall_Unity/Assets/02.Scripts/Core/Baseball.Core.asmdef](<../../../UPlayBall_Unity/Assets/02.Scripts/Core/Baseball.Core.asmdef>)
- [UPlayBall_Unity/Assets/02.Scripts/Simulation/Baseball.Simulation.asmdef](<../../../UPlayBall_Unity/Assets/02.Scripts/Simulation/Baseball.Simulation.asmdef>)
- [UPlayBall_Unity/Assets/02.Scripts/Game/Baseball.Game.asmdef](<../../../UPlayBall_Unity/Assets/02.Scripts/Game/Baseball.Game.asmdef>)
- [UPlayBall_Unity/Assets/02.Scripts/Presentation/Baseball.Presentation.asmdef](<../../../UPlayBall_Unity/Assets/02.Scripts/Presentation/Baseball.Presentation.asmdef>)

### B03

외부 회귀 도구

- [UPlayBall_Unity/Tools/HeadlessRegression/Baseball.Game.Headless.csproj](<../../../UPlayBall_Unity/Tools/HeadlessRegression/Baseball.Game.Headless.csproj>)
- [UPlayBall_Unity/Tools/HeadlessRegression/EditModeTestRunner/Program.cs](<../../../UPlayBall_Unity/Tools/HeadlessRegression/EditModeTestRunner/Program.cs>)
- [UPlayBall_Unity/docs/지침/Headless_Regression_Guidelines_UPlayBall.md](<../../../UPlayBall_Unity/docs/지침/Headless_Regression_Guidelines_UPlayBall.md>)

### B04

난수와 이벤트 출력

- [UPlayBall_Unity/Assets/02.Scripts/Simulation/Random/Pcg32Random.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Simulation/Random/Pcg32Random.cs>)
- [UPlayBall_Unity/Assets/02.Scripts/Simulation/Match/MatchEventSink.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Simulation/Match/MatchEventSink.cs>)

### B05

경기 실행 프로필

- [UPlayBall_Unity/Assets/02.Scripts/Simulation/Match/MatchExecutionProfile.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Simulation/Match/MatchExecutionProfile.cs>)

### B06

제품 계약과 구현 기록

- [UPlayBall_Unity/BaseballManager_PROJECT.md](<../../../UPlayBall_Unity/BaseballManager_PROJECT.md>)
- [UPlayBall_Unity/AGENTS.md](<../../../UPlayBall_Unity/AGENTS.md>)
- [UPlayBall_Unity/docs/구현_현황.md](<../../../UPlayBall_Unity/docs/구현_현황.md>)
- [UPlayBall_Unity/docs/성장_시스템_현황.md](<../../../UPlayBall_Unity/docs/성장_시스템_현황.md>)

### B07

공용 UI 셸

- [UPlayBall_Unity/Assets/02.Scripts/Presentation/SharedUI/Shell/SharedGameShellPresenter.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Presentation/SharedUI/Shell/SharedGameShellPresenter.cs>)

### B08

저장 버전의 소유자

- [UPlayBall_Unity/Assets/02.Scripts/Game/Career/NewGameFlow.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Game/Career/NewGameFlow.cs>)
- [UPlayBall_Unity/Assets/02.Scripts/Game/Career/Persistence/CareerSaveAdapter.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Game/Career/Persistence/CareerSaveAdapter.cs>)
- [UPlayBall_Unity/Assets/02.Scripts/Game/Historical/ManagerHistoricalSaveAdapter.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Game/Historical/ManagerHistoricalSaveAdapter.cs>)

### B09

구단주 파일 저장

- [UPlayBall_Unity/Assets/02.Scripts/Game/Unity/Persistence/ManagerHistoricalSaveJsonStore.cs](<../../../UPlayBall_Unity/Assets/02.Scripts/Game/Unity/Persistence/ManagerHistoricalSaveJsonStore.cs>)

### B10

현재 수동 저장 정책

- [UPlayBall_Unity/docs/reports/owner-manual-save.md](<../../../UPlayBall_Unity/docs/reports/owner-manual-save.md>)

### B11

최초 경기 성능·검증 제한 기록

- [UPlayBall_Unity/docs/reports/owner-cold-start-performance.md](<../../../UPlayBall_Unity/docs/reports/owner-cold-start-performance.md>)

[G01]: #g01
[G02]: #g02
[G03]: #g03
[G04]: #g04
[G05]: #g05
[G06]: #g06
[G07]: #g07
[G08]: #g08
[G09]: #g09
[G10]: #g10
[B01]: #b01
[B02]: #b02
[B03]: #b03
[B04]: #b04
[B05]: #b05
[B06]: #b06
[B07]: #b07
[B08]: #b08
[B09]: #b09
[B10]: #b10
[B11]: #b11



