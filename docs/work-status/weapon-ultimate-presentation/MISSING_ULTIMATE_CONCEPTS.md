# 전용 궁극기 연출 미보유 캐릭터 컨셉 상태

2026-11-14 · [설계 정본](../../../Assets/docs/design/combat/player/PLAYABLE_ULTIMATE_PRESENTATION_CONCEPT_DESIGN.md) · [전투 위키](../../wiki/combat.md) · [기존 연출 구현 상태](STATUS.md)

## 범위와 현재 판정

후속 클립 조사 반영: [실제 에셋 채택·조합·Blender 전환 기록](MOTION_ASSET_DECISIONS.md). 설계 §6의 입력14개와 제작 방향을 추가했다. 7명 현행 재사용, 두 대검은 메모리 조합 확인/적용 조건부, 레이네 직선 교체는 실행 판정 경로 확인 대기다. 아래 최초 설계 작업의 ‘Unity 실행 없음’은 당시 범위이며 후속 프리뷰 조회와 구분한다.

사용자 요청에 따라 Astra와 논의해 정식 플레이어블 중 전용 UltimateSequence가 없는 10명의 연출 컨셉을 작성했다. 문서 설계이며 이번 작업에서 코드·Ability·MotionSet·Sequence·이미지를 제작하거나 수정하지 않았다. 기존 미커밋 문서·아트·에셋은 보존했다. 커밋·빌드·Unity 실행은 하지 않았다.

‘궁극기 없음’을 실행 능력 부재로 해석하지 않는다. 현재 대상은 Arin, MyoRyeong, Reine, Lian, SeolA, Sera, Lili, YeonHoa, Myomyo, Yura다. H09는 정규 스토리에서 미사용으로 명시돼 제외했다. Raon/Hwarin 전용 시퀀스는 기존 것으로 유지한다.

## 조사 근거

- `Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_*.asset` 13개의 characterType·defaultWeaponType·abilitySet 참조를 확인했다. Ability 하위 meta GUID와 대조해 Set 경로를 확인했다.
- `Assets/10.Datas/Actor/Ultimate/`의 Sequence 에셋은 Raon/Hwarin 2종이다. 전용 Sequence 부재를 공용 Ability/MotionKey 부재로 확대하지 않았다.
- Sera/Lili는 GreatSword Set, YeonHoa/Myomyo는 Staff Set을 공유한다. Reine은 Spear 정의와 Default 이름 Set의 조합이다.
- Staff 공용 GA의 `Player.Staff.Ultimate`, 설명 ‘전방으로 마력의 빛을 쏩니다’, Payload `Staff.Ultimate`와 `Motions/Staff_Ultimate.asset` 존재를 확인했다. 실제 모델별 매핑과 시각 적합성은 이번 문서 작업에서 실행 검증하지 않았다.
- 정규 스토리 §6의 12명·H09 제외와 R01 인물 문서 §2·§4의 행동·금지 캐릭터화를 대조했다. R01 말투/세부 연기는 잠정안이라는 한계를 유지했다.

## Astra 논의와 채택 결정

사용자 지시에 따라 `gpt-6-astra` 디자이너에게 읽기 전용 정본 조사와 초안 검토를 맡겼다. 파일 수정·Unity 슬롯은 주지 않았다. 리드가 설계·위키·상태를 통합했다.

| 논점 | 검토 후 결정 |
|---|---|
| 아린의 전진 연속 거합 | 다단·이동을 새로 암시하므로 기본안에서 제외. 사선의 지속을 채택하고 라온 실제 영상 비교를 남김 |
| 레이네·유라·아린의 ‘길 열기’ 중복 | 레이네 직선 정렬/회수, 유라 좌우 교차, 아린 사선으로 화면 문법을 나눔 |
| 레이네 창으로 모으기 | 실제 흡인/강제 이동을 요구하므로 제외. 현재 위치의 창끝과 실제 도달점 강조 |
| Staff 두 명이 모두 수렴하는 문제 | 연화 단일 수직 도형, 묘묘 여러 점의 수렴 채택. 둘 다 기존 전방 방출을 보존하며 원형 광역 폭발로 변경하지 않음 |
| GreatSword 세로/가로 차이 | 컨셉 목표로 채택하되 다른 자세·베기 방향은 전용 모션 후속안으로 명시. 현재 모션과 어긋나는 FX를 얹지 않음 |
| Whip 적 접촉 | 비접촉 곡선→직선→회수가 기본. T3 구속/던지기는 기존 사용자 결정 대기 유지 |
| 카메라 과장 | 안정된 3/4 우선, 기존 자동 Snapshot 후보를 성공한 구도로 취급하지 않음 |

최종 초안도 Astra가 읽기 전용으로 검토했다. 대상 누락·큰 설계 충돌 없음으로 보고했고, 표시 계약을 Actor → Contracts → `Svc.FaceStrip` / UI 별칭으로 명확히 하는 수정과 기존 컷인 우선 재사용 문구를 권고했다. 두 항목을 반영했다. 기존 컷인 제작 완료를 전용 Sequence 완료와 혼동하지 않는다.

## 검증과 남은 확인

- 문서 정적 검사 **통과**: 대상 정의 10/10의 무기값 일치·대상 표 누락 0·캐릭터별 스토리보드 10절·기존 Sequence 2종을 PowerShell로 대조했다. Set 경로는 앞선 GUID 조사로 확인했다. 신규 설계/상태의 로컬 링크 17개 파일·폴더 존재, 문서 meta GUID 형식 정상, 관련 `git diff --check` 공백 오류 0. 추가한 위키·설계 목차의 연결 대상도 존재한다. 새 파일은 미추적 상태로 보존하며 stage/commit하지 않았다.
- 현재 `SkillCutIn`의 PNG는 11장이다. 대상 10명 중 유라의 `CutIn_Yura.png`는 이 폴더에서 확인되지 않았고 나머지 9명 파일은 존재한다. 파일 존재는 Sprite 임포트·실행 연결·미술 인수를 뜻하지 않는다. 아린/묘령의 최신 작화와 임포트/연결 잔여는 [별도 기록](FRESH_ARIN_MYORYEONG.md)을 따른다. 이번에는 누락 제작·자동 연결을 수행하지 않았다.
- Unity 컴파일·EditMode·PlayMode: 미수행. 문서만 변경해 실행 코드에 영향이 없으며 과거 통과 수치를 이번 작업의 검증으로 승계하지 않는다.
- 사용자 테스트 대기: 컨셉/최종 이름의 미술 방향, 실제 제작 후 동일 무기 간 차별성·무게감·카메라·반복 피로. 설계 §5 C01~C06은 향후 구현 후 확인 기준이다.
- 다음 구현에서 먼저 할 일: 공유 Set에서 실제 Payload/MotionKey/모델 매핑 확인 → 모션과 일치하는 FX/카메라 시안 → 부족한 전용 모션 후보 검토. 설계만으로 새 Ability나 모션을 생성하지 않는다.

## 부산물

임시 캡처·프로젝트·빌드·추가 아트 원본은 만들지 않았다. 새 설계 Markdown과 Unity 문서 meta, 이 상태 문서와 위키/설계 목차 연결만 남긴다. 기존 일러스트와 작업 중 아트 자료는 보존한다.
