P09 NPC 외형 생성 파이프라인

설계와 보호 범위
- 기존 BuildPreset/CharacterBuildConfig/PrefabBuildPipeline을 사용한다. 새 SO도 기존 BuildPreset처럼 Editor 어셈블리에 둔다.
- 조합의 단일 소스는 Assets/10.Datas/Actor/GeneratedNpcAppearances/NpcAppearanceSet.asset이다.
- 생성 순서는 세트 번호 → 성별 → SO의 헤어 순서 → 컬러 순서 → 얼굴·피부 묶음 순서다. 배열 순서를 바꾸면 해당 순번의 외형도 바뀐다.
- 프리셋 생성은 전개·config.Validate·외형 중복·출력 소유권/dirty 검사 후에만 쓴다. 기존 생성물은 삭제 없이 갱신하고 GUID를 보존한다.
- Undo 그룹과 기존 자산 메모리 백업을 함께 사용한다. 실패 시 Undo를 되돌리고 백업을 복원·저장하며 이번 실행이 만든 경로·폴더만 정리한다.
- 기본 출력 외의 임의 경로는 거부한다. 테스트만 전용 임시 루트에서 실행한다.

2단계 프리팹 빌드의 사전 설계
- 기존 빌더의 교체 경로를 바꾸지 않는다. 배치 입력 전체의 프리팹·NpcData·ActorDefinition 고정 경로가 모두 비어 있을 때만 기존 파이프라인에 넘긴다.
- 배치 시작 전 ActorDatabase의 메모리 스냅샷을 보관한다. 기존 정의는 수정하지 않고 새 정의 등록만 허용한다.
- 자산 생성은 기존 파이프라인 내부에서 이루어진다. 각 단계의 기존 트랜잭션을 사용하고 성공한 결과의 신규 경로를 수집한다.
- 배치 실패 시 데이터베이스 스냅샷을 복원하고 해당 배치의 신규 프리팹·NpcData·ActorDefinition만 제거한다. 완료된 기존 파이프라인은 임시 계층을 이미 파괴하므로 바깥 그룹에서 구조 Undo를 다시 되감지 않는다(실제 네이티브 크래시 확인).
- 메모리/씬 비용을 제한하도록 작은 범위로 나누어 실행한다. 이미 생성된 범위의 재빌드는 거부한다. 프리셋 재생성과 프리팹 재빌드는 별개다.
- 기존 빌더의 전체 교체 트랜잭션을 일반화하거나 기존 NPC를 재빌드하는 작업은 포함하지 않는다.

초안 근거
- 카탈로그 의상은 이름이 서술형이 아니라 Armor_001~012이며, Arm/Chest/Waist/Leg의 ContentId가 동일하다.
- 성별·세트 아이콘 경로: DemoScene_Data/Icons_Equipment/256/Armor/{Male|Female}_Armor_{01~12}/icon_P09_{Male|Fem}_Armor_{001~012}_{부위}.png.
- 기존 WorldResidents 예: Taeon은 남성 헤어13/컬러01, Haeri·Yeondu는 여성 헤어03/컬러01, Onyu·Suan·Yerin은 여성 헤어11/컬러01이다. 이들은 NpcFaces를 사용하는 사례도 있으므로 헤어 사용 이외의 얼굴 설정은 복사하지 않는다.
- 헤어 01~13과 컬러 01~09를 의상·성별에 분산하고 성별 안에서 두 헤어의 쌍이 반복되지 않게 작성했다. 시각적 적합성은 미검증이다.
- 얼굴형 01~03 ↔ 같은 번호 피부색 01~03으로 고정했다. 여성 원본 파일의 Famale 오탈자를 그대로 참조한다.
- 헤어 컬러 01~09의 눈 색 매핑은 각각 01,02,03,04,05,01,02,03,04이다. SO의 각 행에서 수정할 수 있다.
- 새 NPC 데이터는 새로 생성하며 표시 이름은 주민, 무기 없음, 물리 의상은 꺼짐이다. 배회 반경 1.4/대기 7은 기존 WorldResidents 설정을 참고한 초안이다.

카탈로그 특이사항
- 헤어 14종: 01~13은 Hair_01~13, 14는 SkinHead_{0}(대머리)이다. 14는 헤어 컬러로 구별되지 않아 이번 2컬러 축에서 제외했다. 헤어 스타일 아이콘 참조는 없다.
- 수염은 00(없음, 빈 메시)와 Male_FacialHair_01~08의 9개 값이며 아이콘은 없다. 남성 세트마다 한 값만 고정하고 여성은 SO 참조도 비운다.
- 가슴 크기는 S(01: 0.85/0.8/0.85), M(02: 1/1/1), L(03: 1.2/1.2/1.2)이며 모두 bust_01_{R|L} 본 스케일이다. 여성 세트별 한 값, 남성 미사용이다.
- 헤어10은 각 헤어 컬러 SO에 전용 재질 매핑이 있으므로 기존 GetMaterial 경로를 사용한다.
- 피부 SO에는 성별 필드가 없고 원본 컨테이너에만 성별이 있다. 규칙 검증은 컨테이너 참조를 기준으로 한다. 외형 적용은 동일 ID의 다른 성별 재질을 고르지 않도록 선택 SO를 직접 사용한다.

사람 검수 작업 명세 (사용자 테스트 대기)
V01 / SO의 24개 규칙을 검사: 헤어2×컬러2×얼굴·피부3, 남성 수염 고정, 여성 가슴 크기 고정이 의도와 맞아야 한다.
V02 / 생성 프리팹을 성별·세트별로 놓고 12개 변형을 확인: 얼굴/피부/헤어 색, 수염 부착, 의상 관통·빈 부위·반대 성별 노출·가슴 본 변형에 이상이 없어야 한다.
V03 / URP 조명 아래 가까운 거리·중간 거리에서 식별성, 눈 색, 피부·헤어 재질을 검수한다. 조합이 수치상 다르더라도 식별성이 충분한지 별도로 판단한다.
V04 / NPC 배치 씬에서 Editor Play Mode 실행: NPC 이동·상호작용과 Console 오류 여부를 확인한다. 장면/입력/대사 연결은 이 생성 작업에서 저작하지 않았다.
결과 회신: 항목 ID / 통과·실패·미수행 / 세트·성별·순번 / 실제 증상 및 Console 로그.

실행
프리셋 생성: UPlayGround.EditorTools.UPlaygroundToolCatalog.TryExecute("p09.npc-appearance.generate", out string error).
테스트: EditMode의 UPlayGround.Editor.P09Builder.Tests.NpcAppearanceGeneratorTests.
툴 런처에는 등록하지 않는다. 생성된 BuildPreset은 Editor 제작 자산이며 런타임에서는 빌드된 프리팹을 사용한다.

프리팹 신규 빌드: p09.npc-appearance.build-next는 다음 미생성 묶음 최대 12개를 만든다. 코드에서는 NpcAppearancePrefabBuilder.BuildRange(start, count)를 사용할 수 있다.
기존 프리팹 재빌드는 거부한다. SO 편집 후 프리셋 갱신은 가능하지만 프리팹 교체는 별도 검토 대상이다.
별도 batchmode 전용 전체 신규 빌드: -executeMethod UPlayGround.Editor.P09Builder.NpcAppearancePrefabBuilder.BuildAllBatch.

최종 자동 검증 결과
- Unity 6000.3.21f1에서 컴파일 실패 없음, 최종 Console 오류 0.
- EditMode 11개 통과 / 실패 0 / 건너뜀 0. GUID·내용 보존, config.Validate 전수 오류, Head None, 의상 ContentId, 중복·성별 제한, 실제 예외 주입 롤백을 포함한다.
- 실제 프리셋 288개를 재생성하여 GUID와 직렬화 내용이 동일함을 추가 확인했다.
- 신규 프리팹 첫 빌드 후 실패 주입 시 DB 내용 복원 및 프리팹·NpcData·ActorDefinition 잔존 0을 확인했다.
- 구조 Undo 크래시가 검사 중 한 차례 발생했다. 이미 파괴된 임시 계층에 대한 바깥 Undo를 제거한 뒤 같은 실패 주입을 통과했다. 상세 호출 스택은 Logs/NpcAppearances/PrefabRollback-crash.txt에 보존했다.
- 프리팹 288개, NpcActorSO 288개, ActorDefinitionSO 288개를 생성했다. DB에 신규 ID 288개가 각각 한 번씩 등록됐고 기존 정의는 덮어쓰지 않았다.
- 프리팹·NPC 데이터·정의 연결 288개 통과, 전체 계층 Missing Script 0. 기존 프리팹 재빌드 거부와 DB 보존도 확인했다.
- 전체 프리팹 파일 합계 612,913,716바이트(약 584.5 MiB). 기존 빌더의 모듈 계층을 포함한 산출물이며 실제 씬에서 다수 NPC를 배치하는 성능은 미검증이다.
- 시각 검수, Editor Play Mode, Windows Player 빌드는 미수행. 이 작업은 Player 빌드 대상 변경이 아니다.
- 테스트 임시 폴더는 정리했다. 기존 작업 중 변경은 보존했고 커밋하지 않았다.
- 파일 전체 목록: Tools/NpcAppearances/FILES.txt. 검증 근거: Logs/NpcAppearances/EditMode-final.json, PrefabRollback.txt, PrefabValidation.txt, FinalChecks.txt.
