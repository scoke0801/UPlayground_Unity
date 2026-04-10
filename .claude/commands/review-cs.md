# Git 수정된 CS 파일 리뷰

git에서 수정된 C# 파일들을 분석하고 코드 리뷰를 수행한다.

## 인자 파싱

`$ARGUMENTS` 형식: `[--staged | --commit <hash> | --branch <name>]`

- 인자 없음 — 기본값. 워킹트리에서 수정된(unstaged + staged) 모든 `.cs` 파일 리뷰.
- `--staged` — 스테이징된 `.cs` 파일만 리뷰.
- `--commit <hash>` — 특정 커밋에서 변경된 `.cs` 파일 리뷰.
- `--branch <name>` — main 브랜치 대비 해당 브랜치에서 변경된 `.cs` 파일 리뷰.

## 실행 단계

### 1단계: 수정된 CS 파일 목록 수집

인자에 따라 아래 Bash 명령 중 하나를 실행한다:

- 인자 없음:
  ```bash
  git diff --name-only HEAD -- "*.cs"
  git ls-files --others --exclude-standard -- "*.cs"
  ```
- `--staged`:
  ```bash
  git diff --cached --name-only -- "*.cs"
  ```
- `--commit <hash>`:
  ```bash
  git diff --name-only <hash>^ <hash> -- "*.cs"
  ```
- `--branch <name>`:
  ```bash
  git diff --name-only main...<name> -- "*.cs"
  ```

파일이 없으면 "리뷰할 수정된 CS 파일이 없습니다." 출력 후 종료.

### 2단계: diff 및 파일 내용 분석

각 파일에 대해:

1. `git diff HEAD -- <파일경로>` (또는 해당 모드에 맞는 diff 명령)로 변경사항(diff)을 확인한다.
2. 파일 전체를 Read 툴로 읽어 맥락을 파악한다.
3. 필요 시 `semantic_search_nodes_tool` 또는 `query_graph_tool`로 관련 클래스/함수 관계를 조회한다.

### 3단계: 코드 리뷰 출력

각 파일마다 아래 구조로 리뷰를 출력한다:

---

## `<파일경로>`

### 변경 요약
변경 사항을 1~3문장으로 간결하게 설명.

### 리뷰 항목

| 심각도 | 위치 | 내용 |
|--------|------|------|
| 🔴 Critical | `ClassName:line` | 버그, 크래시 위험, 보안 문제 |
| 🟠 Warning  | `ClassName:line` | 잠재적 문제, 성능 우려 |
| 🟡 Suggestion | `ClassName:line` | 코드 품질, 가독성 개선 |
| 🔵 Info | `ClassName:line` | 참고 사항, 패턴 불일치 |

이슈가 없으면: ✅ 이슈 없음

### 아키텍처 적합성
CLAUDE.md의 프로젝트 아키텍처(매니저 시스템, 상태머신, 컴포넌트 패턴, 코드 컨벤션)와 비교하여 위반 사항이나 권장 패턴 미적용 여부를 평가.

---

### 4단계: 전체 요약 출력

모든 파일 리뷰가 끝나면 아래 형식으로 종합 요약을 출력한다:

---

## 전체 요약

- **리뷰 파일 수:** N개
- **Critical:** N건 / **Warning:** N건 / **Suggestion:** N건
- **즉시 수정 필요 항목:** (Critical 항목 목록)
- **전반적 평가:** 1~2문장.

---
