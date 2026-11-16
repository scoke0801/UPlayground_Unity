# MotionWarp 빌드 차단 수정 (2026-11-15)

## 반영과 판정

MotionWarpBuildValidator와 같은 `TryValidateMappedProfiles`를 열린 Unity Editor의 MCP execute_code로 실행해 **오류 0, 미매핑 경고 12건**을 확인했다. Player/Addressables 빌드는 실행하지 않았다.

- `Player/Signature/Katana_Signature.asset`: 실제 12개 Avatar의 이동 궤적을 측정해 v3 프로필 12개 생성.
- `Player/Signature/Katana_Raon_Signature.asset`: BokuseiAvatar 프로필 1개 생성.
- `Player/UltimateRedesign/Arin_Ultimate.asset`, `Raon_Ultimate.asset`: 각 실제 리그에서 해당 1.133~1.333초 구간이 제자리임을 확인해 기존 베이크 도구가 translationWeight를 1에서 0으로 변경. 회전 가중치는 보존.
- 위 4개 Custom 워프는 settingsVersion을 1로 이관했다. 변경 전후 ResolveSettings JSON 일치 검사와 변경 개수 4 검사를 통과했다. 클립·타격·FX·GAS 수치는 수정하지 않았다.

## 수행한 검증과 근거

기존 `BakeMissingProjectProfiles`가 PlayMode 기준 프로필과 오프라인 측정을 대조한 뒤 누락 프로필을 적용했다. 최초 준비 실행은 프로필 적용 후 실행 설정 미이관 15건으로 실패했고, 이관 후 전체 매핑 검증을 다시 실행해 통과했다. 검증기를 완화하지 않았다. C# 소스 변경이 없어 컴파일·Hot Reload는 수행하지 않았다. 실행 직전 Editor는 Edit Mode, 컴파일 중 아님, scriptCompilationFailed=false였다.

- [전체 매핑 검증 원문](motionwarp-build-validation.txt)
- [프로필 측정 원문](motionwarp-build-bake.json): 프로필 13개, OK 13, 측정 실패 0. 제자리로 분류한 궁극기 2개는 이 보고서의 적용 행에서 제외된다.
- 제자리 판정 원문: `Logs/MotionWarpReplacement/stationary-windows.txt`.
- 기존 도구: [WarpRootMotionBatchBakeWindow.cs](../../Assets/02.Scripts/GameActor/Editor/WarpRootMotionBatchBakeWindow.cs).
- 운영: [전투 저작](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [검증 지침](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md).

## 남은 확인과 보존

실제 Player 빌드와 아린·라온 궁극기/카타나 시그니처의 접근·회전 감각은 **사용자 테스트 대기**다. 미매핑 12개에 임의 실행 매핑을 추가하지 않았다. 제공된 lilToon Preserve 출력과 그림자 아틀라스 해상도 경고는 이 MotionWarp 검증의 실패 원인이 아니다.

대상 4개 에셋은 현재 Git 추적 목록에 없으므로 git diff에 표시되지 않는다. 디스크 저장과 Unity 검증 결과로 반영을 확인했다. 구매 원본·기존 사용자 미커밋 변경·meta는 보존했다. 이번 연결·이관용 임시 본문은 실행 후 제거하며 측정·검증 결과는 위 링크로 보존한다. 커밋하지 않았다.

연결: [전투 위키](../wiki/combat.md).

## 재발 방지 문서 연결

[전투 저작 지침 6.1](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md#61-motionwarp-설정프로필-준비)에 신규·복제·클립/구간/리그 변경 시 설정 이관, 프로필 준비, 전체 매핑 검증을 같은 작업에서 수행하는 절차를 추가했다. 최종 체크리스트와 [궁극기 재현 진입점](../../Tools/UltimateRedesign/README.md)에서도 연결한다. 모션 스킬이 필독으로 지정한 기존 전투 지침에 절차를 두었으며 외부 설치 스킬은 수정하지 않았다. 이번 후속은 문서만 변경해 Unity 검증·빌드는 재실행하지 않는다.
