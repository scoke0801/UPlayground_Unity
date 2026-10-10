# 생명의 호수 내러티브 개편 구현 기록

## 적용 범위

기획서의 시작–화린–리안–묘령–신전–귀로 흐름을 실행 데이터와 런타임에 반영했다. 기준 맵은 현재 새 게임에서 로드하는 원본 LakeOfLife다. 사용자 작업 중인 LakeOfLifeScenario의 배치·지형·내비게이션·HUD는 이번 작업으로 덮어쓰지 않았다. 보물의 진실, 제2장 목적지, 주인공의 소원은 새로 확정하지 않았다.

## 플레이 변화

| 구간 | 구현 |
|---|---|
| 시작 | 안내 질문과 반복 설명을 줄이고 신전으로 향하는 현재 목표를 먼저 제시 |
| 화린 | 함께 상황을 해결한 뒤 접근·이름 교환. 영입 확정은 필수 대화 정상 종료 뒤 |
| 리안 | 화린과 안부를 먼저 나누고 무언 접근의 여백 뒤 수색·동행 이유 제시 |
| 묘령 | 부상 부위를 감싸는 자세, 짧은 경고, 물러남. HP 패배 없이도 12초 충돌 뒤 정지. 주인공이 먼저 거리를 내주고 화해한 뒤 영입 |
| 신전 | 반복 추론과 근거 없는 도발을 줄임. 분신 얼굴 공개를 명시적 클로즈업으로 지정. 공개 전에는 전투와 목표 노출 보류 |
| 분신 | 라온·아린의 현재 플레이어 모델로 전용 프리팹 생성. 기존 BossAlternateSelfRaon / BossAlternateSelfNenmir ID와 Variant Set 참조 보존. 기존 카타나 GAS/모션 재사용 |
| 결말 | 전투 직후 동료들이 곁에서 기다림. 신전 도착 지점으로 직접 돌아온 뒤 동료들이 앞서 걷고 주인공이 따라붙으며 제1장 종료 |
| 주민 | 높임말·관계와 생활상의 관심사를 구분. 헤이즐은 약을 챙기고 미아는 준에게 다가감. 준 후일담에서 확인되지 않은 집 안 위치를 단정하지 않음 |

전투 종료 위치는 임의이므로 승리 직후의 보행은 제거했다. 돌·나무·플레이어 충돌체 사이를 가로지르던 동선 대신, 공간을 검증한 귀로에서 함께 걷는 행동을 보여준다. 분신은 기존 나무뿌리 안쪽에서 인접한 검증 위치로 옮겼다. 원본 씬 전체를 재저작하지 않고 Flow의 해당 스폰 좌표만 조정했다.

## 저작·런타임

- Dialogue Event에 액터·기준 인물·상대 위치·처음 기준 자세·이동/정지·속도·도착 거리·제한 시간·후퇴 시 시선 정책을 추가했다. 일반 진행 입력은 행동을 건너뛰지 않으며 명시적 스킵은 기존 완료 계약을 따른다.
- 비전투 MotionSet과 임시 손 소품을 함께 저작한다. JSON 왕복에 에셋 경로와 부착 위치를 보존한다. 묘령의 IdleWounded01, 헤이즐의 Item_TakeOut과 TGV 병은 기존 리소스를 재사용했다.
- 플레이어·몬스터·NPC의 KCC 이동을 사용한다. 취소·이탈은 이동, 소품, 모션 스냅샷을 정리한다. 지면 검사에서 다른 액터를 제외하며, 몬스터 도착은 보간 Transform을 재판정하지 않고 이동 상태의 결과를 받는다.
- 파괴 예정 대역은 Destroy 전에 비활성화하여 같은 프레임의 다음 대화가 재사용하지 않게 했다.
- Flow의 WhileIdle 진입점과 게이트는 실행 중 중복을 막고 완료·취소 후 재개한다. WaitPlayerInVolume은 저장 복원 당시 이미 목적지 안에 있는 경우도 검사한다. 대화 Cancelled 출력은 정상 완료와 분리된다.
- IStoryActorStaging 계약의 전투 홀드는 정상 공개와 취소 정리 양쪽에서 해제된다. 하위 모듈에 신규 Manager 싱글톤 의존을 추가하지 않았다.
- 통합 툴 런처의 내러티브/대화에 분신 캐스팅 구성을 등록했다. 기존 원본 모델을 덮어쓰지 않고 전용 프리팹을 생성한다. 연결 실패 시 정의·프로필을 복구한다. 새 프리팹과 meta만 저장소 제외 규칙의 예외로 추가했다.

## 저장·중단 복구

| 지점 | 보존 진행 | 재개 |
|---|---|---|
| 화린·리안 대화 취소 | CombatResolved | 조우를 떠났다가 재접근하면 필수 대화 재시도 |
| 묘령 경고 취소 | IntroductionPending | 경고부터 |
| 묘령 충돌 도중 저장 | CombatActive | 충돌부터, 12초 타이머 재시작 |
| 묘령 화해 취소 | CombatResolved | 화해부터, 영입 미확정 |
| 기존 묘령 영입 확정 저장 | RecruitmentCommitted | 기존 후속 대화 경로 |
| 제단 공개 취소 | treasure_reached | 전투 보류 후 재접근하여 공개 |
| 최종 전투 후 대화 취소 | alternate_self_defeated | 귀로 지점에서 기다림 장면 재개, 보스 재전투 없음 |
| 귀로 도중 저장 | farewell_completed | 귀로 도착부터 대기 |
| 종료 연출 취소 | chapter1_completed 미설정 | 지점을 나갔다 돌아오면 종료 연출 재생 |
| 기존 제1장 완료 저장 | chapter1_completed | 새 귀로 목표로 기존 완료를 되돌리지 않음 |

기존 Dialogue/Quest/Objective ID와 GUID 및 영입 enum 값을 보존했다. 선택 의뢰를 메인 완료의 선행 조건으로 추가하지 않았다. 조이 저녁 대화의 완료 이벤트 연결도 복구했다.

## 실행 검증

Unity 6000.3.21f1, 원본 LakeOfLife에서 검사했다. 아래 장면 재생은 자동 입력과 지정 지점 배치로 실행한 검사다. 게임패드 수동 완주나 처음 보는 플레이어의 이해도 검사를 대신하지 않는다. 발화 사이 자동 진행 간격은 1초이므로 소요 시간을 실제 독서/플레이 시간 개선 수치로 제시하지 않는다.

| 검사 | 결과 | 근거 |
|---|---|---|
| 최종 스토리·콘텐츠·대화·Flow 및 분신 무결성 EditMode | 83/83 통과 | editmode-story-final.xml |
| Ability 포함 확장 회귀 | 314/316 통과 | editmode-final.xml, 아래 기존 실패 명시 |
| 중단·재진입·게이트·전투 홀드 PlayMode | 5/5 통과 | recovery-1.xml |
| 라온 실제 위치 9개 장면, 공개 후 AI/GAS 공격 | 통과 | playback-raon-final4.xml, combat-Raon.txt |
| 아린 실제 위치 9개 장면, 공개 후 AI/GAS 공격 | 통과 | playback-arin-final4.xml, combat-Arin.txt |
| 묘령 HP 감소 없는 충돌 종료 | 통과 | myo-ceasefire-2.xml, myo-ceasefire.txt |
| 두 분신의 플레이어 메시 일치·Missing Script·공격 모션/HitPhase | 통과 | LakeAlternateSelfIntegrityTests |
| 종료 문구 활성화·알파·자동 종료 | 통과 | 두 경로 장면 재생 검사 |
| Windows Player Build | 진행 중 | unity-player-build.log |

로그, XML, 화면 캡처는 tmp/narrative-overhaul/에 있다. 카메라 RenderTexture 캡처에는 UI 오버레이가 나타나지 않아 종료 문구는 활성화·알파 검사로 구분했다. UI 화면을 시각 검수했다고 주장하지 않는다.

## 기존 문제와 검증 한계

- 확장 회귀의 두 실패는 AGENTS.md에 기록된 Dryad 3개·Training Dummy 1개의 Motion Key 매핑 누락이다. Story_LakeRoad_Beast도 같은 Dryad 매핑을 사용한다. 근거 없이 모션을 대입하지 않았다.
- AbilityDataValidator.ValidateAll 전체 결과에는 기존 범위의 오류 7개가 있다: 기본 플레이어 Ability/Ultimate의 HitPhase 2개, 패시브 ID 중복 4개, PA_SkillCooldown의 Effect 참조 1개. 새 분신이 연결한 세트/Ability/Payload 범위에는 오류가 없었다. 전문은 ability-validation.txt에 보존한다.
- 원본 맵 부팅에 기존 Missing Script 경고가 있다. 신규 분신의 Missing Script 0 검사는 전체 프로젝트 0을 의미하지 않는다.
- 두 맵 로드를 같은 Play Mode에서 연속 실행할 때 기존 UI_HUD_PlayerInfo.RefreshGaugeAvailability → PartyManager.ActiveCharacterType의 파괴된 PlayerActor 참조가 관찰됐다. 사용자 작업 중인 HUD 영역을 임의 수정하지 않았다. 개별 새 부팅 검사는 이 문제의 해결을 의미하지 않는다.
- 게임패드 수동 완주, 모든 임의 위치의 저장/로드·역순 접근 조합, 실제 이동 중 길찾기 정체, 초면 플레이어의 동기 이해는 아직 수동 합격 판정하지 않았다. 제공된 환경의 네이티브 앱 조작은 비활성화되어 자동 Unity Play Mode로 검증 범위를 확보했다.
- 동료 모델의 무기 부착·대기 자세에는 기존 시각 품질 문제가 남아 있다. 장면 종료 성공과 애니메이션·아트 최종 품질 합격은 구분한다.

## 보존 문안과 산출물

[최신 대사 목록](LAKE_NARRATIVE_OVERHAUL_DIALOGUES.txt)은 36개 그래프의 도달 가능 문안, 행동, 미연결 보존 문안을 구분한다. 오래된 추출본과 PostRescue 파일을 삭제하지 않았다. 참조 목록은 정적 검색 결과이며 파일 존재만으로 실제 재생 여부를 판정하지 않는다.

미연결 본문을 일괄 비우는 작업은 자동 승인 검토가 콘텐츠 훼손 위험으로 거부하여 적용하지 않았다. 원문과 ID를 유지하고 목록으로 관리하는 방식으로 마무리했다. 커밋은 하지 않았다.
