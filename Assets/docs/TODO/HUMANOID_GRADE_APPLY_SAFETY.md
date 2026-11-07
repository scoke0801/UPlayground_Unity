# 등급 공격 적용의 저장·복구 경계

신규 일반 창병은 별도 `HumanoidSpearAuthoring` 경로로 제작했다. 기존 Reien이나 P09를 재제작하지 않고 신규 데이터·프리팹을 완성한 뒤 DB 두 곳에 1행씩만 추가한다. 신규 전투 데이터 저장 후·프리팹 저장 후·DB 공개 후 세 실패 지점 모두 원본 DB/meta 바이트 복구와 신규 에셋 제거를 확인했다(`Logs/HumanoidSpear/rollback-1~3.txt`, `Backup/`). 최종 생성 뒤에는 경로 충돌로 재실행을 거부하며 GUID를 다시 만들지 않는다. 자세한 연결과 검증 결과는 `HUMANOID_SPEAR_SOLDIER_RESULT.md`에 기록한다.

이번 작업은 P09 전체 Build/BuildStaffMonsters를 실행하지 않는다. 기존 삭제·교체 경로의 transaction 보장은 별도 과제로 남긴다.

등급 공격 적용은 다음 독립 경로로 수행한다.

1. 제작 JSON에서 원본 Set, 선택 공격, 적용 Definition을 명시한다. 이름으로 런타임 분기하지 않는다.
2. 원본과 실제 프리팹 MotionKey를 검사하고, 새 Ability/Payload/Set을 메모리에서 먼저 구성한다. 공유 원본은 수정하지 않는다.
3. 저장 전에 대상 Definition/Profile과 P09 제작표의 원본 바이트 및 meta를 `Logs/HumanoidGrade/Backup`에 보관한다. 기존 에셋을 삭제·대체하지 않으며 GUID와 actorId를 유지한다.
4. 신규 데이터만 AssetDatabase.CreateAsset으로 저장한다. 기존 참조 교체는 하나의 Undo 그룹으로 묶는다. 모든 대상 참조를 교체한 뒤 검사하고 저장한다.
5. 신규 에셋 저장 뒤와 참조 교체 뒤에 실패를 주입한다. 예외이면 Undo.RevertAllDownToGroup 후 새 에셋만 삭제하고 기존 파일을 원본 바이트로 복원·Import한다. 동일 경로 재실행은 덮어쓰지 않고 충돌로 중단한다.
6. 성공 뒤 P09 제작표의 무기 역할마다 Normal/Elite Set GUID를 명시한다. Build의 생성과 Validate가 같은 grade 선택 함수를 사용하고 Profile/Definition을 함께 연결한다. 원본 Profile의 등급에 의존해 파생 등급을 결정하지 않는다.

P09 전체 재제작을 변경할 후속 작업은 별도 임시 에셋 경로에서 프리팹·스탯·도감까지 완성한 뒤 기존 GUID를 보존하는 CopySerialized 적용, DB 공개 직전 실패 및 공개 후 실패 복구, 외형/스탯 수동 튜닝 보존을 검증해야 한다. 이번 참조 교체의 복구 검증은 전체 빌더의 삭제 경로 안전성을 의미하지 않는다.

대검 참조 교정으로 Armor08~12의 생성 스탯에 남아 있던 검방패 보정도 달라진다. 이 다섯 개는 **저장된 모든 Attribute가 교정 전 검방패 공식 결과와 정확히 일치하는 경우에만** 기존 `MonsterStatBakeService`로 같은 AttributeProfile/Poise/BreakGauge 에셋을 제자리 갱신한다. 15개 에셋과 meta를 별도로 백업하고, 첫 개체 저장 후 실패를 주입하여 Undo와 바이트 복구를 검사한 뒤 실제 적용한다. 원본 GS002와 Armor06/07처럼 기존 공식과 다르게 조율된 스탯은 변경하지 않는다. 이 작업도 삭제·대체·프리팹 재제작을 하지 않는다.

## 후속 적용 경로

`HumanoidGradeAuthoring.ApplyAdditions`는 제작표의 `followup` 공격만 기존 Set에 Add한다. 기존 Set과 기존 Fork를 재생성하지 않는다. 카타나·채찍의 기존 응징 Fork에만 공용 쿨다운을 지정하고, 신규 Fork도 같은 그룹을 사용한다. 신규 저장 후와 Set 연결 후의 실패 주입을 세 무기에 각각 실행해 6건 모두 에셋/meta 바이트 복구를 확인했다. 근거는 `Logs/HumanoidGrade/Remaining/*.rollback*.txt`와 `Backup/`이다. 이미 적용된 경로는 충돌로 중단하며 임의 덮어쓰지 않는다.

`Tools/OpenWorld/repair-humanoid-boss-bindings.csx`는 Sera/Nenmir의 두 프리팹과 Definition/Profile만 대상으로 한다. 같은 Avatar인 Sera 플레이어 모델의 활성 대검 부착·무기/신체 HitBox를 복사하고 Constraint의 외부 본 참조를 대상 프리팹 본으로 다시 연결한다. 기존 오연결 무기 오브젝트는 외부 fileID 보존을 위해 비활성화한다. Nenmir는 기존 Bow_004를 유지하며 부착 MainWeapon 판정을 추가한다. 첫 프리팹 저장 후와 Profile 연결 후의 실패 주입에서 원본 프리팹/Definition/Profile/meta 바이트를 복구했다. 별도 스토리 분신 보스와 NPC·플레이어 원본은 수정하지 않는다. **적용 후 재실행하지 않는다.** 적용 여부 검사는 전용 EditMode 테스트와 실제 소유자 전표를 사용한다.

저장 전에 `EditorApplication.isUpdating/isCompiling/isPlayingOrWillChangePlaymode`와 컴파일 실패를 검사한다. 이번 실행 중 대량 임포트로 자동 승인 검토가 저장을 거부했으며, `ready_for_tools=true`, 임포트/컴파일 중 아님, Console 오류 0을 다시 확인한 뒤에만 복구 시험과 실제 저장을 진행했다. 임포트 중 거부된 작업을 다른 실행 경로로 우회하지 않았다.

프리팹 저장의 자동 재직렬화는 `restore-humanoid-boss-serialization.mjs`의 작업 전 백업 대조로 분리한다. Unity가 만든 신규 오브젝트는 유지하고, 기존 fileID 오브젝트는 의도한 `_motionSet/_abilitySet/_socketDict`, 자식 연결, 표시, Constraint source 및 중첩 프리팹 `m_Modification` 이외 필드의 바이트를 복원한다. Sera 32개·Nenmir 22개 기존 블록의 새 기본 필드·도구 미리보기 배열 변화를 제거했고 기존 fileID 누락 0을 검사했다. Definition에 자동 추가된 빈 faction·기본 속성 배율도 작업 전 형식으로 복구했다. 정리 1차에서 중첩 Bow의 AddedGameObjects를 제거한 오류가 실제 로드 EditMode 검사에 잡혔으며, Unity 저장 직후 사본을 복구하고 PrefabInstance/stripped 표현을 함께 보존하도록 수정했다. 재로드 뒤 Bow MainWeapon, Sera 무기·양손·양발 판정이 모두 지원 형상으로 존재함을 확인했다. 이 스크립트는 해당 배치의 최초 백업과 `Remaining/Serialized` 검토용 사본을 읽는 복구 보조 도구이며 다른 배치에 사용하지 않는다.
