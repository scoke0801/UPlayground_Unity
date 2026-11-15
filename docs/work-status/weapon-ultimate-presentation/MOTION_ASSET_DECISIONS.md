# 궁극기 클립·MotionSet 채택 조사

2026-11-14 · [설계 확정 방향과 에셋 대장 §6](../../../Assets/docs/design/combat/player/PLAYABLE_ULTIMATE_PRESENTATION_CONCEPT_DESIGN.md#6-클립-조사-후-확정한-제작-방향과-에셋) · [컨셉 상태](MISSING_ULTIMATE_CONCEPTS.md) · [전투 위키](../../wiki/combat.md)

## 결정

**기존 클립을 MotionSet으로 구성하는 것이 기본이며, 컨셉·그립·타격 궤적을 함께 만족하지 못한 구간만 Blender MCP 제작으로 전환한다.** 설계 §6에 실제 입력 에셋 14개, 연결 FBX 서브 클립 GUID/local ID, 캐릭터별 적용 범위, 두 대검 조합의 구간을 추가했다.

- 아린·묘령·리안·설아·연화·묘묘·유라 7명: 현행 궁극기 실행 소스 재사용 채택. 실제 다단·공중·회전 동작과 기존 발사 수를 보존한다.
- 세라·릴리: 각자 다른 대검 조합 입력을 채택했다. 메모리상 MotionSet에서 몸 동작을 샘플링했으며 실제 실행 연결은 하지 않았다. 세라는 타격 창 길이 차이 때문에 조건부, 릴리는 시간 정합을 확인한 조합 후보다.
- 레이네: 현행 보존과 직선 동작 후보 확보. 현행 점프 동작과 빈 이벤트 목록을 확인했으므로 직선 공격·명중 연출의 적용 확정은 보류한다. 다른 소비 경로에서 판정을 실행하는지 확인하기 전 공격 고장이라고 단정하지 않는다.
- 현재 Blender 신규 클립이 필수라고 확정한 대상은 없다. 실패 조건과 제작할 최소 구간·예정 납품 경로는 확정했으며 전원 신규 제작이나 구매 원본 변경은 하지 않는다.

## 조사 범위와 근거

1. [실행 연결 10명](motion-audit/bindings.json): 정의 → AbilitySet.GetPlayerAbility(Ultimate) → Variant Payload의 MotionKey → 실제 모델 PlayerMotionSet의 무기별/폴백 해석을 Unity API로 추적했다. 단순 파일명 추측이 아니다.
2. [등록 모션 조사 111개](motion-audit/inventory.json): Player의 해당 무기 Skill/HeavyAttack/일부 Attack/Guard, 추가 GreatSword Idle, 실제 Player Staff Ultimate, Whip 무기 클립 2종. Clip 경로·GUID·정밀도 보존 문자열 local ID·길이·fps·Humanoid 여부·커브 수·구간·속도·로컬 이벤트와 전역 환산 시각·레이어·의존성 해시를 기록했다. 구매 라이브러리 전체의 모든 클립을 시각 검수한 조사는 아니다.
3. 실제 PlayerModel 프리팹 10종과 GreatSword/Spear/SwordShield/Bow/DualBlade/Whip 대안을 격리 PreviewRenderUtility에서 샘플링했다. [기존 P09PosePreview](../../../Assets/02.Scripts/Editor/P09Builder/Preview/P09PosePreview.cs)를 재사용해 Animancer·그립 레이어·ParentConstraint·CPU 스킨 베이크를 평가했다. 새 런타임/미리보기 도구를 프로젝트에 추가하지 않았다.

### 주요 확인 결과

| 항목 | 실제 확인 | 설계 반영 |
|---|---|---|
| 아린 | Katana_Skill_Ultimate는 같은 FBX의 3구간 체인, 총5.010001초·Collision16개 | 단일 사선 ‘한 번’ 공격으로 오해하지 않도록 현행 체인 명시 |
| 설아 | Bow Skill2는 공중 동작·발사 이벤트2개 | 단발 지상 Bow Skill1로 임의 교체하지 않음 |
| 유라 | DualBlade Skill2는 Rolling·Collision5개 | 고정 카메라에서 실제 양손 순차 궤적을 유지 |
| 리안 | 몸 Skill2와 Generic Whip_Skill2를 함께 샘플링해야 실제 채찍 변형이 나옴 | L1/LW1 한 쌍을 확정 입력으로 지정. Critical은 대안 조사만 수행 |
| 플레이어 Staff | 실제 매핑은 Magic_Attack_2,2.166667초·발사1.05초·오른손 쥐기 레이어 존재 | 과거 P09Staff Magic_Attack_1 후보보다 실제 Player 소스를 우선 |
| 대검 | Skill3은 공중 동작, Heavy3은 지지 자세의 내려베기, Skill1은 넓은 베기 후보 | 세라 Heavy3, 릴리 Skill1 조합 입력 선정. 점프 슬램을 세라 고정 자세의 완료품으로 채택하지 않음 |
| 레이네 | Default.Ultimate가 Spear Skill2로 해석됨, 해당 MotionSet의 로컬/전역 공격 이벤트 없음 | 창 직선 후보 Skill1 확보. 실제 판정 소비자 미확정 유지 |

## 포즈 근거와 한계

| 대상 | 프리뷰 근거 | 확인 범위 |
|---|---|---|
| 아린 | [현재 체인](motion-audit/Arin_Current.png) | 몸·검의 연속 동작 분류 |
| 묘령 | [현재](motion-audit/MyoRyeong_Current.png) · [Shield1 대안](motion-audit/MyoRyeong_Shield1.png) | 방패와 검 자세 비교 |
| 레이네 | [현재 점프](motion-audit/Reine_Current.png) · [직선 후보](motion-audit/Reine_Spear1.png) | 점프/낮은 찌르기 구분. 현재 점프 일부 프레임은 카메라 상단 잘림 |
| 리안 | [현재 몸+무기](motion-audit/Lian_Current.png) · [Critical 대안](motion-audit/Lian_Critical.png) | 동기화된 무기 본의 곡선 확인 |
| 설아 | [현재](motion-audit/SeolA_Current.png) · [Bow1 대안](motion-audit/SeolA_Bow1.png) | 공중/지상 자세와 이벤트 수 비교 |
| 세라 | [Heavy3](motion-audit/Sera_Heavy3.png) · [메모리 조합](motion-audit/Sera_Recipe.png) · [현행](motion-audit/Sera_GS2.png) | 지지·내려베기 몸 동작과 현행 비교 |
| 릴리 | [Skill1](motion-audit/Lili_GS1.png) · [메모리 조합](motion-audit/Lili_Recipe.png) · [현행](motion-audit/Lili_GS2.png) | 넓은 베기 몸 동작과 현행 비교 |
| 연화·묘묘 | [연화](motion-audit/YeonHoa_Staff.png) · [묘묘](motion-audit/Myomyo_Staff.png) | 같은 실행 모션의 모델별 자세. 런타임 의상/외형 세팅의 최종 인수와 구분 |
| 유라 | [현재](motion-audit/Yura_Current.png) · [Attack1 대안](motion-audit/Yura_Attack1.png) | 회전/공중 동작과 양손 궤적 분류 |

- 위 이미지는 **모션 분류 근거**다. 최종 전투 화면·무기 부착 합격·연속 전환·실제 명중 통과가 아니다. 6포즈의 듬성한 샘플로 전체 타격 창을 검증하지 않았다.
- 샘플러는 게임 이벤트를 실행하지 않고 root 위치/회전을 고정한다. 실제 Root Motion/Warp·장비 전환·보조손 오차·사거리 경계·타격 결과는 미검증이다. 원본 설정 플래그만으로 루트 이동이 검증됐다고 보지 않는다.
- 초반 탐색 캡처에는 런타임 초기화를 하지 않아 InteractionObject 생활 도구가 함께 보였다. 이후 임시 프리뷰 복제본에서만 해당 그룹을 숨기고 핵심 캡처를 다시 만들었다. 원본 프리팹은 수정하지 않았다. 탐색 캡처 일부는 남겨 비교 근거로 보존하며 장비 오류의 실전 증거로 사용하지 않는다.
- 초기 샘플은 정규화0/.2/.4/.6/.8/1이다. 끝 경계의 반복/복귀 혼동을 줄이기 위해 최종 재촬영은 마지막을 .999로 제한했다. 일부 추가 재촬영은 연결 종료로 완료하지 못해 마지막 프레임을 복귀·루프 합격 근거로 삼지 않는다.
- 묘묘 등은 모델 프리팹 단독 표시이며 런타임 의상/장비/물리 적용을 재현하지 않았다. 미술 인수는 하지 않았다.

## Astra 검토

`gpt-6-astra` 디자이너가 설계와 inventory를 읽기 전용으로 검토했다. 리드가 초기 조합의 숫자 보존을 판정 보존으로 확대하지 않도록 아래 결론을 채택했다.

- 세라: donor 타격창을 준비 길이만큼 이동하면1.812321~1.954899초. 현행 창 끝2.312321초까지0.357422초 차이가 있다. **이벤트 전역 시각 유지와 판정 결과 보존은 다르다.** 조건부 조합으로 기록하고 전체0.5초의 실제 칼날/HitBox 궤적을 확인한다.
- 릴리: 준비0.423532초를 더하면 donor와 현행 타격창 시각이 일치한다. 공간적 적중 영역·그립·전환까지 같다고 단정하지 않는다.
- 레이네: 빈 이벤트와 점프가 있는 상태에서 직선 명중 FX를 확정하지 않는다. 신규 Blender 클립만으로 실행 판정 미확정을 해결하지 않는다.
- 기존 소스 부적합이 확인되기 전 Blender ‘필수’ 선언은 하지 않고, 실패가 구체화되면 최소 피니시/연결을 제작한다.

## 자동 확인·미수행

최종 문서 정적 검사: [검사 결과](motion-audit/validation.json). 로컬 링크79개 누락0, 등록 MotionSet111개 경로/연결 클립 누락0, 실제 실행 연결10명, local ID 문자열 보존 확인. 릴리 타격 창 시간 정합 오차는0.000001초 미만이며 세라의 창 끝 차이를 별도 기록했다. 관련 `git diff --check` 공백 오류0. 이 수치 검사는 실전 판정·미술 통과를 뜻하지 않는다.

- **수행:** Unity AssetDatabase 모션111개 조회, 실제 모델10종의 Ability/MotionKey 해석, 선택 입력14개 식별, 두 대검 메모리 조합 샘플링. JSON·포즈 파일 보존. 주요 결과를 리드가 이미지로 열어 대조했다.
- **수행:** 메모리 조합은 기존 에셋 복제본의 motions만 교체하고 종료 시 Dispose/Destroy했다. 새 SO/FBX를 저장하거나 실행 매핑을 바꾸지 않았다. 성공한 프리뷰 응답에서 CombatTestMap의 dirty=false를 확인했다.
- **미수행:** EditMode/PlayMode·실제 전투·연속 블렌드·전체 타격창 궤적 검사·무기/칼집/활시위 접촉 품질·미술 인수. 런타임 코드/데이터 변경이 없어 컴파일·테스트·빌드를 추가하지 않았다.
- **연결 한계:** 말미 재촬영에서 Unity MCP 연결이 끊겼고 마지막 조회는 `No Unity Editor instances found` 응답이다. Editor를 새로 열거나 재시작하지 않았다. 일부 추가 재촬영과 종료 시점 Editor 상태의 최종 재조회는 미수행이다. 기존 성공 캡처·조회와 이 실패를 구분한다.

## 다음 구현·사용자 확인

설계 §6.3의 구간으로 전용 MotionSet을 분리한 뒤, 연속 프리뷰와 실제 명중/빗나감·중단 복구를 먼저 확인한다. 세라/릴리는 같은 적·같은 위치의 정면/측면/사거리 경계에서 비교한다. 시간을 옮기거나 무기 판정을 넓혀 조합을 통과시키지 않는다. 실패가 재현되면 구간/블렌드/그립 조합을 재검토하고, 여전히 안 되는 최소 동작만 Blender MCP로 제작한다. 얼굴·색 없이 두 대검의 방향과 무게가 구분되는지는 **사용자 테스트 대기**다.

## 부산물과 재현

조사 JSON·PNG와 [조회 코드](motion-audit/inventory-query.cs.txt)·[실행 연결 조회](motion-audit/bindings-query.cs.txt)·[프리뷰 템플릿](motion-audit/preview-template.cs.txt)·[세라 조합](motion-audit/Sera-recipe-preview.cs.txt)·[릴리 조합](motion-audit/Lili-recipe-preview.cs.txt)을 이 상태 폴더에 보존한다. 템플릿의 CHARACTER/ASSET_RELATIVE/OUTPUT/WEAPON_MOTION_PATH는 치환용이며 제품 코드가 아니다. 말미 재촬영용 코드와 이전 성공 이미지의 경계가 다를 수 있으므로 이미지 합격을 자동 재현한 것으로 간주하지 않는다. 임시 프리뷰 객체는 코드 finally로 정리하며 별도 프로젝트·빌드·원본 구매 파일·Blender 파일은 만들지 않았다. 기존 미커밋 변경은 원복하지 않았다.
