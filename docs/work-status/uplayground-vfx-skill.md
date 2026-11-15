# UPlayground FX 제작 스킬

2026-11-14. 상태: 스킬 작성·Codex 사용자 폴더 설치·정적 검사 완료. 실제 FX 제작 실행과 미술 인수는 미수행이다.

## 정본과 설치

- 저장소 정본: [SKILL.md](../../.claude/skills/uplayground-vfx/SKILL.md).
- 조건별 상세: [기존 연결·MCP 호출·촬영 제한](../../.claude/skills/uplayground-vfx/references/project-workflow.md).
- UI 메타데이터: [openai.yaml](../../.claude/skills/uplayground-vfx/agents/openai.yaml).
- 설치본: `C:/Users/scoke/.codex/skills/uplayground-vfx/`의 동일 3개 파일. 기본 자동 선택을 유지한다. 이 세션의 도구 목록에 동적 등록됐다고 단정하지 않는다.
- 사용 예: `$uplayground-vfx 기존 참격 프리팹을 활용해 화염 베기 FX를 만들고 현재 공격 모션에 연결해줘.` 자동 선택 목록에서 보이지 않으면 저장소의 SKILL.md 경로를 직접 지정해 읽도록 요청할 수 있다.
- 저장소 지침을 변경하면 설치본도 동기화하고 `skill-creator/scripts/quick_validate.py` 검사와 파일 해시 대조를 다시 수행한다. 사용자 설치본 변경이 있으면 덮어쓰기 전에 비교한다.

## 조사와 적용

[Scenario 원본](https://github.com/scenario-labs/skills/blob/main/skills/game-engines/unity/scenario-unity-vfx/SKILL.md)의 제작·촬영·측정 관점을 참고해 한국어 지침으로 독립 작성했다. 원본 코드·전용 도구·정량 결과를 복사하거나 프로젝트 검증으로 승계하지 않았다. Scenario MCP, AgentKit, unity-cli, 새 패키지를 설치하지 않았다.

`Packages/manifest.json`에서 Coplay Unity MCP 10.3.0, URP/VFX Graph 17.3.0을 확인했다. 설치 패키지의 `ManageVFX.cs`, `ParticleWrite.cs`, `ParticleControl.cs`, `VfxGraphAssets.cs`와 실제 프로젝트 연결 소스를 정적으로 대조했다. 실제 도구를 호출해 현재 Editor 연결/그래프 실행을 확인한 것은 아니다.

반영 범위는 Shuriken 중심 제작, 기존 템플릿·노출값을 통한 VFX Graph 활용, 일반 파티클/참격 MotionEvent, 무기 궤적과 기존 풀의 선택적 재사용이다. VFX Graph 노드 추가·연결이 기존 `manage_vfx`에 없고 컴파일 define 조건이 있다는 점을 명시했다. Katana의 기존 중복 FX 확인, 액터 기준 `World` 좌표계, 본 평가 뒤 스폰 계약을 반영했다.

## 검증 결과

| 항목 | 결과·근거 |
|---|---|
| 저장소 정본 구조 | 공식 `quick_validate.py` 실행, `Skill is valid!` |
| Codex 설치본 구조 | 동일 검사 실행, `Skill is valid!` |
| 설치 무결성 | SKILL.md / project-workflow.md / openai.yaml의 SHA256 원본·설치본 3/3 일치 |
| 문서 연결 | 새 스킬·상태 문서의 로컬 링크와 참조 프로젝트 파일 존재 확인 |
| 변경 격리 | 기존 미커밋 C#·프리팹·데이터·문서 변경 보존. 위키는 이 스킬 항목만 추가 |
| 미수행 | 실제 FX 제작, Unity 컴파일·Play Mode·성능 측정·미술 인수. 코드/에셋 변경 없는 스킬 작업이므로 실행하지 않음 |

공식 검사는 기존 uv/Python/PyYAML을 오프라인으로 사용했다. 별도 테스트 프레임워크·런타임·임시 제작 에셋은 만들지 않았다. Player/Addressables 빌드와 커밋은 수행하지 않았다. 정리 대상 부산물은 해당 없음이다.

## 다음 사용에서 확인할 항목

첫 실제 FX 요청에서 도구 스키마/Editor 상태·현재 원본을 재확인하고, 요청한 프리팹 제작·로드·짧은 시각/수명 검증을 수행한다. 모션 연결은 실제 장착 무기의 최종 MotionSet을 추적한다. 사용자 화면·체감 확인은 별도 테스트 명세로 전달하며, 이번 구조 검사만으로 제작 품질 통과를 보고하지 않는다.

분야 연결: [월드와 아트 위키](../wiki/world-art.md#unity-fx-제작-스킬).
