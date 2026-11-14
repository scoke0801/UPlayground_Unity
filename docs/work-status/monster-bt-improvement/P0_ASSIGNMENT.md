# P0 연결 정상화

[상태](STATUS.md) · [전 연결표](P1_AUDIT_BEFORE.md) · [후 연결표](P0_AUDIT_AFTER.md) · [이동/GUID 증거](P0_GUID_EVIDENCE.md)

- `EnemyBehavior_Test_AggressiveGroundMelee_Tuned`를 `EnemyBehavior_GroundMelee_Aggressive`로 승격했다. SourceJson과 Generated 모두 AssetDatabase.MoveAsset으로 이동했고 재import가 같은 Generated 경로를 덮어쓰는 것을 확인했다.
- BT GUID `8b0e128fd2f18c942bb8de0b6fd81ab8`, JSON GUID `859112e2cbf006847bde3123cc526037` 전후 동일. 참조 13개 모두 같은 에셋을 가리킨다. griffin의 미사용 칸도 참조 자체는 보존했다. 비행의 실제 BT는 변경하지 않았다.
- 감사에서 Test BT를 가리키는 BehaviorData 0, 빈/깨진 BT 0. Humanoid의 Organic 에셋은 프로젝트 루트에서 정상 해석되므로 수정하지 않았다.
- BT 삭제·커밋 없음. 미연결 RangedKiter/Example/Organic 계열/Test는 보존하고 향후 실제 사용처 확인 뒤 정리 여부만 결정한다.

## 결정 대기

리치(원거리 시전형 후보), 거미 여왕(공격 구성에 맞는 보스형), 거미 졸개(무리형), 드라이어드(공격 구성 조사), 해골 검사(Disciplined), 파티 캐릭터 몬스터판 10개(무기별 Humanoid)는 사용자 결정 질문 응답 전까지 연결을 바꾸지 않는다. 이름 승격만으로 이들의 행동 정체성 문제가 해결됐다고 보고하지 않는다.

## 검증과 사용자 확인

GUID와 모든 기존 연결은 Unity에서 대조 완료. 이번 승격은 행동 변경 없이 이름·경로만 바꿨다. 후속 런타임 단계 작업은 독립 구현으로 진행하되 사용자 플레이 합격을 승계하지 않는다.

사용자 테스트 대기: LakeVillageWorld의 기존 스폰 리치·거미 여왕·졸개·드라이어드를 상대해 기존과 같은 감지/접근/공격이 실행되는지 확인한다. 실제 스폰이 없는 몬스터는 기존 전투 테스트 배치에서 확인하고 씬·개체를 결과에 적는다. `P0-01 / 통과·실패·미수행 / 실제 결과 / 로그 또는 영상`으로 회신한다. 아직 원형을 재연결하지 않았으므로 행동 차이를 합격 조건으로 두지 않는다.
