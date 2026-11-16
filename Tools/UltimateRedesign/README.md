# 궁극기 재제작 재현·재개

현재 상태 정본: [제작 상태](../../docs/work-status/weapon-signature/MOTION_PRODUCTION.md). 이 폴더의 `.cs.txt`는 Unity `execute_code`에 전달하는 재현용 본문이며 자동 실행 코드가 아니다.

## MotionWarp 변경 후 필수 확인

클립 재추출·MotionSet 복제·구간 변경·모델 매핑 변경 후에는 [전투 저작 지침 6.1](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md#61-motionwarp-설정프로필-준비)에 따라 실행 설정 이관 → 리그별 프로필 준비 → 전체 매핑 검증을 수행한다. 이 폴더의 모션 적용 본문 실행만으로 준비 완료로 판정하지 않는다. [실제 빌드 차단 수정·검증 기록](../../docs/work-status/motionwarp-build-fix.md)을 함께 확인한다.

무기 모션 세트 복제·교체 시에는 [전투 저작 지침 6.2](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md#62-런타임-운동-데이터-연결재베이크)의 MotionImpact 연결·전체 재베이크·빌드 전 검증도 수행한다. MotionWarp 통과만으로 다른 빌드 검증까지 통과했다고 판정하지 않는다.

## 손·무기 결합과 FX 후속 수정

FX 추가 수정 순서: `RefineWindImpactFx.cs.txt` → `FinalizeWindMaterial.cs.txt` → `CreateWindTexture.cs.txt`. 이미 적용했으며 앞 단계는 충격광 배율을 누적하므로 재실행하지 않는다. 회오리 최종 재질은 자체 생성 텍스처와 URP Particles/Unlit, 메시 리본 폭 0.48을 사용한다. Renderer vertex stream은 Position/Normal/Color/UV다.

[현재 적용·검증 상태](../../docs/work-status/weapon-signature/GRIP_FX_REVISION.md)를 우선한다. 아래의 이전 제작 완료 표현은 반려된 버전의 이력이다.

- `BuildGripCandidate.cs.txt`: 대표 양손 무기의 후보 저작 본문. 캐릭터·무기·축과 손가락/해제 정책을 실제 모델에 맞춰 사용하며 전원에 동일 파라미터를 재사용하지 않는다. 생성 시 Constraint 커브는 접촉 검사에만 사용한다.
- `BuildRaonGripCandidate.cs.txt`: 라온 검 추가 본의 파지 위치 저작. 기존 칼집·회전 커브는 보존한다.
- `BuildFxRevision.cs.txt`, `BuildImpactFxRevision.cs.txt`: 구매/프로젝트 원본을 보존하는 FX 파생 제작. 최종 지팡이 빔 외곽 폭은 2.4, 중심 폭은 0.5로 저장했다.
- `ApplyGripFxRevision.cs.txt`: 변경 전 백업 후 12명 클립·FX를 연결하고 Constraint 커브를 종료 복구 가능한 MotionEvent로 이관한다. 이미 적용했으므로 그대로 재실행하지 않는다. 기존 후보/백업 보호 검사를 우회하지 않는다.
- `InspectGripRevision.cs.txt`, `InspectFxRevision.cs.txt`: 격리 진단 본문. 수동 Playable 평가만으로 Constraint가 갱신되지 않으므로 실전 검증으로 취급하지 않는다. 실제 검수에서는 보정 이벤트의 값과 장비 소스를 수동 적용한 정지 샘플을 별도로 표시한다.

파생 클립·프리팹과 변경 전 백업이 최종 산출물이다. 이 진단 본문은 최종 인게임 캡처나 일괄 자동 복구 도구가 아니다.

## 적용 완료한 재현 순서

1. `RefreshGroundedThree.cs.txt`: 이미 Assets에 복사한 Yura/MyoRyeong/Lili N3 FBX에서 기존 `.anim` GUID를 보존해 재추출한다. 다른9명은 재추출하지 않는다.
2. `BakeWeaponAxes.cs.txt`: 신규 몸 클립의 실제 장비축을 다시 맞춘다. 유라·묘령·릴리 외4명 재적용은 같은 축을 맞추는 멱등 보정이다.
3. `BakeBowString.cs.txt`: 설아 실제 중지 근위지골을 당김 접점으로 F8~18 연결, F45 해제 후 감쇠 반동을 저작한다. 적용 완료했다. 실제 활의 변형·손 접점을 캡처로 확인하고 과도한 변형이면 수정한다.
4. `RenderFinalModels.cs.txt`: 실제12 모델을 매 프레임 BakeMesh하고 리안 몸/무기 클립을 동시 샘플한다. FX·KCC·전투 이벤트는 실행하지 않는 모션 검수다. 결과 `output/ultimate-redesign/final-models`를 직접 보고 축·관통·발·소품을 수정한다. 적용 완료했다.
5. `CorrectStaffWhipAxes.cs.txt`로 두 지팡이 머리 축과 리안 Sword_Root 축을 맞춘다. 이 단계를 BakeWeaponAxes 뒤에 적용해야 한다. 최종 반영 완료했다.
6. `SignatureAbilityDataTests`13건과 Console 컴파일 오류를 확인하고 MCP 결과 원문을 상태 문서 옆에 보관한다. 이전 이벤트7·PlayMode7 원문은 job ID로 재조회한다.
7. 제작 상태·전투 위키·하네스 입력 해시를 최종 결과로 갱신한다. 실제 전투·아트 인수는 UR01~16 사용자 테스트 대기다.

`Create*`/`Map*`은 이미 생성한 데이터 경로를 보호하므로 그대로 재실행하지 않는다. `RefreshNeutralBody`는 전체9명 몸을 다시 추출해 소품 커브를 지우므로 현재 사용하지 않는다. `BakeRaonProps`는 최신 중립 body 적용 후 이미 실행했다. `.pending` 테스트는 이미 실제 Assets에 반영된 제작 사본이며 재복사하지 않는다.

최종: N3 재추출·시위·축 보정·537프레임 촬영 완료. EditMode20/20 이후 축 수정 뒤 재검사는 Editor Play로 시작 거부되어 미수행. 기존 Play 세션 유지. 실제 전투 인수는 사용자 테스트 대기.
