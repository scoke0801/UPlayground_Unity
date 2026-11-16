# 라온 런타임 운동 데이터 빌드 차단 수정

## 원인과 반영 (2026-11-15)

라온 모델의 플레이어 모션 바인딩이 궁극기 전용으로 교체되면서 실제 Katana 소유자는 `UltimateRedesign/Bindings/Raon_WeaponMotions.asset`으로 바뀌었으나, `motionImpactCatalog` 참조가 빠졌다. 기존 `RaonKatana` 분석 묶음은 이전 `Set_Katana.asset`을 기준으로 남아 있었다. MotionWarp 검사 통과와 별개로 MotionImpactBuildValidation이 `Player.Katana.Light.00`에서 첫 실패를 보고했다.

기존 `CombatMotionImpactBaker.Bake`로 현재 실행 소유자를 기준으로 약공격 00/01/02·강공격 00의 총 4개 공격과 4개 타격 구간을 재측정하고 연결·출처 지문·측정 원본 식별 정보를 갱신했다. 운동 표본과 속도 수치는 기존과 동일했다. 검증기·C# 코드는 변경하지 않았다.

- [현재 무기 모션 세트](../../Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/UltimateRedesign/Bindings/Raon_WeaponMotions.asset): 기존 런타임 카탈로그 연결.
- [런타임 운동 데이터](../../Assets/10.Datas/Combat/MotionImpact/RaonKatanaMotionImpact.asset): 4개 출처 지문 갱신.
- [분석 카탈로그](../../Assets/10.Datas/Editor/CombatMotionAnalysis/RaonKatana.asset): 현재 소유자·의존성·생성 시각 갱신. 검수 메모와 수동 태그 보존.

## 검증

열린 Unity Editor의 MCP에서 기존 베이크와 전체 MotionImpact 빌드 전 검사, MotionWarp 전체 매핑 검사를 실행했다. 최초 두 검사 모두 통과했다. 베이크가 저장한 Ability 4개는 diff를 확인해 기본 필드 추가뿐인 자동 재직렬화를 제거했다. 원래 Ability 값·GUID·기존 사용자 변경은 보존했다. 정리 후 최종 검사 원문은 [검증 결과](motion-impact-build-validation.txt)에 보존한다.

Player·Addressables 빌드, C# 컴파일, Play Mode는 실행하지 않았다. 실전 라온 약공격 연계·강공격의 타격 방향과 반응은 **사용자 테스트 대기**다. MotionWarp 미매핑 경고 12건은 별도 잔존 경고다. lilToon immutable package 변경 경고와 그림자 아틀라스 축소 경고는 이번 예외 원인이 아니며 패키지 원본은 수정하지 않았다.

## 재발 방지와 정리

[전투 저작 지침 6.2](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md#62-런타임-운동-데이터-연결재베이크)에 모션 세트 교체·복제 후 MotionImpact 연결·재베이크·Player 빌드 없는 전체 검사 절차를 추가했다. [궁극기 재현 진입점](../../Tools/UltimateRedesign/README.md)과 [전투 위키](../wiki/combat.md)에서 연결한다. MotionWarp와 MotionImpact 통과를 구분한다.

이번 연결·베이크·정리용 임시 PowerShell/C# 본문은 검증 종료 후 제거한다. 에셋과 검증 원문은 보존한다. 기존 미추적 아트·meta·프리팹 변경은 보존하고 커밋하지 않는다.

최종 결과: 자동 재직렬화 정리 후 MotionImpact 전체 빌드 전 검사와 MotionWarp 전체 매핑 검사 모두 통과했다. 관련 문서 로컬 링크 91개와 수정 범위 diff 공백 검사를 통과했다. Ability 4개의 Git diff는 0이며, 임시 실행 본문은 제거했다.
