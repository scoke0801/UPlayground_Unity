# 궁극기 전신 모션 제작

2026-11-15 사용자 제작 진행 지시로 시작. [설계 정본](../../../Assets/docs/design/combat/player/ULTIMATE_MOTION_REDESIGN.md) · [조사](REFERENCE_REDESIGN.md) · [전투 위키](../../wiki/combat.md).

## 현재 단계

**2026-11-25 이후 정본: [캐릭터별 재검증·재제작](PER_CHARACTER_REBUILD.md).** 이 문서의 절차 합성 방식은 원인 진단 후 폐기했다. 아래는 이력이다.

후속 현재 상태: [손·무기 결합과 FX 재작업](GRIP_FX_REVISION.md). 사용자 지적에 따라 12명 파지 후보와 FX를 실제 연결했으며 검증 중이다. 아래 정정 당시의 미수정 설명은 이전 시점의 기록이다. 기존 품질 반려를 철회하거나 새 적용본의 미술 인수를 완료한 것은 아니다.

**12명 모션 품질 사용자 반려·재작업 필요. 전체 완료 판정 철회(2026-11-15).** 사용자가 전원 모션 품질이 무너졌다고 보고했다. 이는 검수 대기가 아닌 품질 실패다. 캐릭터별 결함과 원인은 아직 재진단하지 않았으며 이번 정정에서 모션을 수정하거나 재검증하지 않았다.

기존 완료 보고는 데이터 적용·자동검사·정지 샘플 확인을 최종 모션 품질과 혼동했다. 537프레임 뷰어는 FX·KCC·무기 숨김 이벤트를 실행하지 않는 12Hz 정지 샘플이다. 기존 독립 QA 수행 기록은 보존하되 품질 합격의 근거로 사용하지 않는다. 마지막 축 수정 이후 추가 검사 미수행도 그대로 남긴다. 다음 작업은 실제 적용본의 연속 동작에서 준비·무게중심·무기 궤적·접촉·회수 결함을 캐릭터별로 확인하고 대표 모션부터 재작업하는 것이다. 현재 파일·구매 원본·미커밋 변경은 보존한다.

아래는 반려된 적용본의 제작 이력이며 품질 통과를 뜻하지 않는다. 유라·묘령·릴리 N3 접지 보정까지 실행 `.anim`에 GUID를 유지해 재추출했고 손목 축을 재베이크했다. 모델별 매핑24개와 기존 MotionKey를 사용하며 Ability/Payload·타격 횟수·피해·비용·쿨다운을 보존한다.

- 컷인: 등장0.24초·유지1.1초(화린1.05초)·퇴장0.3초. 화린 연속 회전과 F122 피니시, 레이네 F49 발사·무기 숨김, 세라/릴리 별도 동작·타격 타이밍을 적용했다.
- 설아: 시위 위치3곡선으로 F8~18 손 접점 연결, F45 해제와 감쇠 복귀. F42 변형 메시의 시위 가중치 정점 중심과 중지 근위지골 거리0.0004326982m를 MCP로 측정했다. 이 수치는 리드 직접 수신 기록이며 독립 측정 원문 파일은 없다. [근접 화면](../../../output/ultimate-redesign/bow-detail/SeolA_014.png)은 자연스러운 연속 해제 판정과 구분한다.
- 리안: 몸과 독립 무기 클립 동시 샘플 후 실제 Sword_Root 축을 손목에 적용. F70 전방 스냅 [측정](../../../output/ultimate-redesign/lian-snap-axis.txt) forward dot=1. 연화·묘묘 지팡이 머리가 뒤를 향하던 문제도 수정했다. 당시 독립 QA가 수정 화면을 재확인했으나 이후 사용자 품질 반려가 있으므로 이 확인을 모션 품질 합격이나 결함 부재로 해석하지 않는다.
- [12명 537프레임 검수 뷰어](../../../output/ultimate-redesign/final-models/index.html): 실제 장비만 표시하고 매 프레임 BakeMesh·bounds 갱신. 리안 몸/무기 동시 재생. 정지 샘플은 FX·KCC·창/방패 숨김 이벤트를 실행하지 않으므로 전투 영상으로 취급하지 않는다. 이전 contact/board는 구버전 비교본이며 최신 근거는 개별 PNG와 뷰어다.

접지·시위 반영 후 [EditMode20/20 원문](REDESIGN_EDITMODE_FINAL.json), [이전 관련 PlayMode7/7 실행 기록](REDESIGN_PLAYMODE_RECEIPT.json), Console 오류0 확인. PlayMode 상세 개별 결과는 domain reload 후 result:null이며 작업 성공·완료7건만 복원됐다. 지팡이/채찍 축 수정 후 추가 데이터13건 재검사는 Editor가 Play 모드로 전환되어 시작 거부(0건, job `3c0f4d55e4f8464ab300891d87f4b970`)됐다. 사용 중인 Play를 임의로 종료하지 않았다. 자동검사와 [UR01~16 직접 검수](USER_TEST_REDESIGN.md)를 구분한다. 현재 모션 미술 품질은 사용자 반려이며 개별 실전 기능 항목은 미확정이다.

최종 소스 [품질·revision 명세](../../../ArtSource/Animation/UltimateRedesign/source-quality-manifest.json), [207파일 SHA 목록](../../../ArtSource/Animation/UltimateRedesign/file-manifest.json), [N3 접지 전후](../../../ArtSource/Animation/UltimateRedesign/grounding-comparison.json)를 보존했다. 구매 원본12 SHA 일치, 생성본 재임포트 본 보존 확인. 소스152,111,064바이트와 인용 진단본은 보존하며 자체 `.blend1`만 정리했다. [재현 본문](../../../Tools/UltimateRedesign/README.md)은 자동 실행되지 않는다. 아래 절은 제작 경과이며 현재 판정은 이 절을 우선한다.
## 시작 조사

- 실제 모델 ParentConstraint: Hwarin=Weapon/DoubleAxe (Hand.r/Chest), Reine=Weapon/Spear (Hand.R/Chest), Sera=Weapon/GreatSword (hand_r/spine_03).
- 화린 원본은 Frank 2Hand Whirlwind. 단일 양손 장병기를 독립된 도끼 두 자루로 가정한 설계를 교정했다. 모델 장착 슬롯이 하나인 사실을 확인했으며 무기 장착 화면 촬영은 MCP 응답 실패로 아직 미수행이다.
- 초기 아트 작업표: output/harness/20261115-115157-12ab0deb. 경로는 제작 전 placeholder이며 실제 산출물에 맞춰 갱신한다.
- Blender 5.2 background CLI 실행 가능을 아티스트가 확인. 기존 열린 Blender 작업을 덮지 않고 ArtSource 파생 작업으로 제작한다.
- Unity 상태 조회는 처음 idle였지만 이후 연결 종료/No Unity instances/stale_status가 반복된다. 사용자에게 열린 Editor 상태 확인을 요청했고 아트 제작은 계속한다. 두 번째 Editor 실행·잠금/Library 삭제는 하지 않는다.

## 적용 전 확인

1. 원본 리그와 실제 모델 그립을 대조한 무기 포함 연속 프리뷰.
2. 애니메이션 없는 RestBind의 실제 본 자세로 독립 Avatar를 구성하고 Unity 자세 보정 후 검증한다. 애니메이션 첫 자세를 bind로 삼거나 원본 HumanDescription을 그대로 복사하는 방식은 채택하지 않는다.
3. 실제 새 해제/접촉을 기준으로 이벤트 시점을 조정하고 기존 피해 총량/횟수·비용·쿨다운 보존. Spear 도약/KCC 수직과 clip pelvis 높이 이중 적용 검사.
4. 공유 GreatSword의 릴리 소비를 확인. 세라 전용 동작을 공유본에 무조건 덮지 않고 검수용 파생에서 먼저 확인.
5. 대표3종의 FX 없는 연속 동작과 실제 실행 확인 전 나머지 캐릭터 양산하지 않는다.

## 첫 임포트 검수와 수정 중인 문제

3종 FBX를 `Assets/07.Animations/Player/UltimateRedesign/`에 독립 임포트했다. 기존 MotionSet·Ability 매핑은 변경하지 않았다. Unity 연결은 간헐적으로 복구되었으나 응답 종료가 반복되어 파일 저장 여부를 별도 확인했다.

첫 [실제 모델·무기 샘플](../../../output/ultimate-redesign/models.png)은 실패다. 원본 Frank HumanDescription을 새 FBX의 첫 애니메이션 자세에 맞춰 보정한 기존 ConfigureFrankAvatar 경로가 잘못된 기준 자세를 만들었다. GreatSword의 실제 Sera 양손 간격이 F0/F42/F55에서 0.833/0.280/0.763m까지 벌어졌고 PlayableGraph에서도 동일했다. Transform 중복 커브는0개여서 중복 적용 문제는 아니다.

새 export는 원본 리그 좌표계를 유지하므로 원본 HumanDescription에 Armature wrapper만 추가해 독립 Avatar를 만들었다. Sera 같은 시점 간격이0.106/0.129/0.101m로 개선됐다. 이 수치가 실제 자루 그립 합격을 뜻하지는 않는다. 기존 importer 코드와 다른 작업 자산은 수정하지 않았다.

Blender 쪽에서도 손목·자루 축 저작 부족을 발견해 재저작 중이다. 두 손 목표를 일정 world offset으로 두던 방식에서 원본 무기 파지축을 따라 회전시키는 방식으로 바꾼다. Spear 공통 도약 상승은 본 애니메이션에서 제거하고 별도 `vertical-trajectory.json`으로 분리했다. KCC와 메시가 따로 움직이는 방식으로 임시 연결하지 않는다.

## R2 제작과 Unity 후보 검수 결과

- [Blender 제작·해시 목록](../../../ArtSource/Animation/UltimateRedesign/file-manifest.json), [재임포트 검사](../../../ArtSource/Animation/UltimateRedesign/roundtrip-validation.json), [소스 연속 프리뷰](../../../ArtSource/Animation/UltimateRedesign/continuous-preview.html).
- DoubleAxe 5초(F24~108 회전, F122 접촉), Spear 3.6초(F49 해제 후보, F69 착지), GreatSword 3.6초(F42 정점, F55 접촉).
- 본 누락0, 비정상 수치0, 위치 오차 최대0.00000371m, 회전 roundtrip 검사 통과. DoubleAxe는 입력 yaw가 아니라 재임포트 pelvis 방향을 unwrap하여 회전 정지·역행0 확인. 기존 QA의 누락본 건너뛰기·입력값만 검사 문제를 수정했다.
- Unity의 독립 Humanoid Avatar 3개 valid/human, 클립 길이5/3.6/3.6초 확인. 애니메이션 없는 `*_RestBind.fbx`의 raw skeleton에 Unity `AvatarSetupTool.MakePoseValid`를 적용했다. GreatSword F42 source/target HumanPose 팔 근육값 일치 확인. 기존 helper의 animated bind 보정은 최종 경로가 아니다.
- 실제 모델의 장비 source0 회전은 화린(32.70,197.78,60.51), 레이네(357.17,191.73,302.87), 세라(0,-170,-180). 소스 무기 소켓과 동일하지 않아 대상별 `*_GripCandidate.anim`을 별도 제작했다. 손목 방향을 조정하고 화린·세라는 보조손 IK 후보를 베이크했다. 보조손 목표 solve 최대오차 화린64.5mm/세라53.8mm로, 물리적 자루 접촉 합격을 뜻하지 않는다.
- 산출물: `Assets/07.Animations/Player/UltimateRedesign/`의 실행 후보 FBX3, RestBind3, 대상별 GripCandidate.anim3 및 meta. 구매 원본·기존 실행 클립·MotionSet/Ability 매핑은 이번 제작 단계에서 변경하지 않았다.
- 최종 준비표: `output/harness/20261115-122622-409de2df`.

### 화면 근거 정정

`models.png`와 `r2`~`r8`의 GPU skin 프리뷰는 최신 본 변형을 반영하지 않아 손·부츠가 잘못 보였다. source/target 손 위치와 HumanPose를 대조하고 `SkinnedMeshRenderer.BakeMesh`를 프레임마다 수행해 원인을 확인했다. **그 화면만으로 대상 모델의 스킨·부츠 결함을 확정한 추정은 철회한다.** 초기 importer 경로 비교 기록은 진단 이력이며 최종 미술 판정에 승계하지 않는다.

현재 유효한 근거는 [실제 모델·무기 12Hz 연속 프리뷰](../../../output/ultimate-redesign/baked-sequence/index.html)와 [접촉 시트](../../../output/ultimate-redesign/baked-sequence/contact.png)다. 매프레임 메시 베이크한 정지 샘플 연속 표시이며, 실제 전투 PlayMode 녹화가 아니다. 창은 해제 후에도 손에 붙어 있고 KCC 상승을 합성하지 않았다. 모델 프리팹에는 저장 변경이 없다.

### 남은 제작·인수

1. 화린 회전 중 실제 무기축과 F122 내려찍기 방향, 양손 접촉을 확정해야 한다. 소스 회전 무정지는 Unity 무기 실루엣 합격을 대신하지 않는다.
2. 레이네 손목·실제 창끝과 해제 방향, 해제 후 팔로스루, KCC 상승/착지·투사체 이벤트를 함께 연결해야 한다.
3. 세라 보조손 접촉과 무게를 받는 자세를 다듬고 실제 칼끝 접촉에 타격을 맞춰야 한다. 릴리와 공유하는 실행 데이터는 아직 교체하지 않는다.
4. 대표3종 인수 후 나머지9명의 전용 동작 제작과 전체 캐릭터 실전 검증을 수행한다. 현재 전체 요청 완료가 아니다.
5. 이번 단계는 신규 C# 런타임 변경·Player/Addressables 빌드·전투 회귀 테스트를 수행하지 않았다. 기존 자동 검사 통과를 신규 모션 품질 근거로 사용하지 않는다. 사용자 미술·플레이 인수는 대기다.

최종 QA 후 화린 큰 날 축이 +Z가 아닌 -Z인 것을 실제 메시로 확인하고 대상 wrist 후보를 수정했다. 새 BakeMesh F122 인접 샘플에서 큰 날이 지면을 향한다. [수정 접촉 프레임](../../../output/ultimate-redesign/axe-axis-fixed/DoubleAxe_049.png). 연속 프리뷰의 화린 프레임도 수정본으로 교체했다. 축 반전 지적은 수정됐지만 양손 실제 접촉·중량감·실전 인수까지 통과한 것은 아니다.

QA 최종: 화린 내려찍기 축 수정 확인. 정점 날끝이 잘린 촬영 지적으로 3종 연속 프레임을 FOV45로 재촬영했다. 이전 contact.png는 FOV35의 키포즈 참고이며 연속 프리뷰가 최신 구도다. RestBind 이전 GreatSword_BindRestCandidate는 미채택 진단본으로 Assets에 남아 있으며 실행 참조 없음.

## 전체 제작 재개 — 대표3 실행 연결

사용자가 전체 완료까지 진행을 지시했다. 대표3은 `Player/UltimateRedesign/{Hwarin,Reine,Sera}_Ultimate.asset`으로 독립 생성하고 `Bindings/`의 모델별 Player/Weapon MotionSet 파생을 통해 실제 모델에 연결했다. 기존 Ability/Payload/MotionKey와 공유 Signature를 보존했다. 같은 대검을 쓰는 릴리에는 세라 클립이 적용되지 않는다. 생성·매핑 명령은 `Tools/UltimateRedesign/`에 재현용 텍스트로 보존했다(이미 생성된 경로 재실행은 보호 오류).

- 화린 회전5회는0.8/1.36/1.92/2.48/3.04초, 마지막1회는122/30초. 입력 이동은0.8~3.6초, 회피·이동 캔슬은 피니시 이후.
- 레이네 발사와 무기 숨김은49/30초, 복원84/30초. 도약26/30초, 정점43/30초, 착지69/30초를 목표로 SO에 배율을 저작했다. 실제 Player.prefab Gravity=-30, Rise/Fall=1.5 기준 초기속도6.352941m/s, 상승배율0.2491349, 하강배율0.10650888. 목표 높이1.8m이며 이론 궤적이지 실제 KCC 경사·벽 검증 완료가 아니다. 소스 본에서 공통 상승은 제거되어 중복상승을 피한다.
- 세라 타격55/30·58/30초, 거대검 보간 접촉도55/30초. 피해량/타격횟수 변경 없음.
- 나머지9명은 리그·무기 구조 조사 후 Blender 제작 진행. 라온 검/칼집, 설아 시위, 리안11마디 채찍 등 추가 본 트랙을 몸과 함께 제작한다.

이번 단계의 실제 장비·전투 감각 인수는 사용자 테스트 대기로 남긴다. 신규12명을 대상으로 소비 경로와 판정·비용 보존을 검사하도록 기존 테스트를 갱신 중이다.
## 12명 데이터 연결과 재저작 검수

신규12 MotionSet 및 모델별 Player/Weapon 파생 매핑24개를 작성했다. 리안은 별도 `Lian_Weapon_Ultimate.asset`과 target경로로 변환한120개 generic 본곡선을 추가했다. 무기 RestBind 12본의 실제 대상 local rotation 차이0°, 위치·스케일 일치 확인 후 `hand_r_Align/` 경로를 붙였으며, 일반 채찍 모션은 보존했다.

9명 첫 제작본 검수에서 구매 bind를 직립 자세로 가정한 오류를 발견했다. 라온/아린 뿐 아니라 활·지팡이·쌍검·채찍·방패·릴리의 몸/어깨/발 중립을 명시적으로 정렬해 재저작 중이다. 대표3 감사에서도 레이네 원본 몸축29.43° 기울기·발 방향 차이를 확인해 동일 교정 대상으로 추가했다. 화린/세라는 몸축4.63°이고 발 방향도 실제 검수와 일치하여 현재 제작본 유지. **데이터 연결 완료와 모션 미술 완료를 구분하며 첫 제작본의 기술 검사를 최종 품질로 승계하지 않는다.**

선택 검증: SignatureMotionEventTests EditMode7/7. SignatureImpactCameraTests + SignatureProjectileLifetimeTests PlayMode7/7(진영 미설정 픽스처2건 수정 후). 테스트 스텁에 Player/Monster를 명시했으며 사용자 기존 ProjectileRuntime 진영 폴백 변경은 보존했다. 첫 PlayMode 호출의 잘못된 namespace로 실행 결과를 얻지 못한 작업은 통과로 집계하지 않는다. 실제12모델 신규매핑과 타격·비용·공유원본 보존 검사13건을 갱신하고 실행 중이다.

최종 적용 파일 해시 준비표: [20261115-134814-043721c1](../../../output/harness/20261115-134814-043721c1/review.md). 준비표 자체는 검증 통과 증명이 아니다.
