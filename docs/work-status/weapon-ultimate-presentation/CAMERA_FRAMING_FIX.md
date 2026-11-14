# 궁극기 카메라 구도 회귀 복구

2026-11-14 사용자 [실패 화면](evidence/camera-framing-user.png): 상체가 화면 위로 잘리고 바닥·구조물이 중앙을 가린다. 앞선 자동 검사는 데이터 연결과 시간 길이를 확인했으나 실제 구도를 검증하지 못했다. 그 상태의 자동 생성 카메라를 실제 궁극기에 연결한 것은 잘못된 적용이었다.

## 확인과 판단

- Raon/Hwarin Sequence에 이번 작업에서 추가한 Snapshot이 기존 MotionSet 카메라 연출 위에 실행된다. Raon 실제 모션에는 기존 CameraLookAtSocketEvent와 CameraEffectEvent가 있다.
- 생성한 Snapshot은 actorAnchor=None(root/발 위치), lookAtTarget=Center, useCollision=true다.
- CameraSnapshotSequenceBehavior.BuildPoseFromShot은 주시 회전을 계산한 뒤 root를 충돌 피벗으로 사용해 위치만 보정한다. CameraCollision.GetAvailableDistance는 피벗이 지형과 겹치면0을 반환한다. 따라서 지면에 가까운 root에서 카메라가 접히고 보정 전 회전이 남는 문제가 코드와 화면 증상에 부합한다.
- 실제 실패 프레임의 충돌값을 측정하지 않았으므로 충돌 경로는 유력 원인이다. Snapshot이 기존 연출을 덮는 연결은 코드에서 확인했다. Astra가 읽기 전용으로 이 판단과 복구 범위를 검증했다.

## 반영

- UltimateSequence_Raon.cameraProfile과 UltimateSequence_Hwarin.cameraProfile에서 이번에 추가한 자동 생성 Snapshot만 해제했다. 다른 수동 카메라가 연결돼 있으면 덮어쓰지 않는 조건으로 처리했다.
- 기존 MotionSet/Stage 카메라 경로로 되돌렸다. 얼굴·프로필·기존 이벤트·판정·MotionKey·Slow·Stage는 보존했다. Raon events3/Stage=false, Hwarin events4/Stage=true 그대로다.
- 무기별 카메라 후보10종과 meta는 보관한다. 현재 실제 궁극기에서는 재생하지 않는다. 공용 카메라 코드는 변경하지 않았다.
- 검사를 초안의 강제 연결 요구에서 기존 궁극기를 덮지 않는 조건으로 수정했다. 후보5샷·시간 시계·실제 MotionKey 기준 길이·잘못된 입력 거부 검사는 유지했다.

## 검사와 남은 확인

[관련 EditMode6/6](evidence/camera-framing-editmode.json) 통과(job328262564a0449cabdbe51cd3b332f8c), 실패0/건너뜀0. 컴파일 오류 없음. [Unity 재조회](evidence/camera-framing-bindings.json)에서 cameraProfile=null, 얼굴/프로필 유지, 후보10종 보존, managed reference 누락0, dirty=false를 확인했다. 독립 QA도 디스크와 결과 원본을 대조했다.

실제 화면의 구도 복구는 아직 확인하지 않았다. 짧은 Play 점검을 시도했으나 당시 로드된 LakeVillageWorld에서 플레이어 준비가 끝나지 않아 입력/스킬 실행 없이 종료했다. 새 자동화나 장시간 조작을 추가하지 않았다. 이번 요청 시작 시의 CombatTestMap을 dirty=false로 복원했고 Play Mode는 종료 상태다.

사용자 재검사: 첨부 화면을 찍은 같은 위치·같은 캐릭터로 궁극기를 다시 발동한다. 기존 카메라 연출이 실행되고 상체가 프레임 밖으로 잘리거나 바닥으로 접히지 않는지 확인한다. 얼굴 패널과 기존 타격/슬로모션도 유지돼야 한다. 여전히 같은 현상이 있으면 해당 시점 영상과 캐릭터·위치를 기준으로 기존 카메라 경로를 별도로 조사한다. 미확인인 실제 구도를 자동 검사 통과로 대신하지 않는다.

[전투 위키](../../wiki/combat.md) · [전체 상태](STATUS.md) · [사용자 테스트](USER_TESTS.md)

부산물은 사용자 실패 캡처와 검사 JSON만 보존한다. 신규 아트/클립/임시 씬은 만들지 않았다. [복구 준비표](../../../output/harness/20261114-192534-98eabddd/review.md)의 소스 해시와 diff/문서 링크를 확인했다. 커밋·스테이징·빌드는 하지 않았다.
