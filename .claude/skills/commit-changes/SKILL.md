---
name: commit-changes
description: "UPlayGround 작업 트리의 미커밋 변경을 단위(기능·시스템)별로 나눠 여러 커밋으로 수행한다. 100MB 초과 파일과 3D 모델 파일(.blend/.blend1/.fbx 등)은 자동 제외한다. 다음 상황에서 사용한다: '수정사항 단위별로 커밋해줘', '변경 전부 커밋', '작업 트리 정리해서 커밋', '모델 파일 빼고 커밋', 'git 커밋 요청'. 메시지 문안은 write-commit-message 스킬의 컨벤션을 따른다. 반대로 메시지 문안만 요청하거나(write-commit-message), 푸시·리베이스·되돌리기 등 다른 git 조작에는 사용하지 않는다."
---

# Commit Changes (단위별 분할 커밋)

작업 트리의 모든 미커밋 변경을 **시스템/기능 단위**로 나눠 순서대로 커밋한다. 메시지 형식은 `write-commit-message` 스킬(한국어, 명사형 종결, 접두사 없음, AI 푸터 없음)을 따른다.

## 절차

1. **현황 파악**
   - `git status --short`로 수정·삭제·신규를 본다. 신규 디렉터리는 접혀 보이므로 `git ls-files -m -o --exclude-standard`로 개별 파일을 펼친다.
   - 변경이 많으면 `git diff --stat`과 애매한 파일의 `git diff -U0`만 골라 읽어 소속 단위를 판단한다.

2. **제외 대상 선별 (커밋하지 않고 남긴다)**
   - **100MB 초과 파일**: PowerShell로 변경 파일 크기를 재서 걸러낸다. 예: `git ls-files -m -o --exclude-standard | ForEach-Object { Get-Item -LiteralPath $_ } | Where-Object { $_.Length -gt 100MB }` (사용자가 다른 기준을 말하면 그 값을 쓴다).
   - **모델 파일**: `.blend`, `.blend1`, `.fbx`, `.obj`, `.glb`, `.gltf`, `.max`, `.ma`, `.mb`, `*.before-uv.*` 등. 사용자가 제외 범위를 따로 말하면 그에 따른다.
   - **임시·자동 생성물로 보이는 것**(예: `InitTestScene<GUID>.unity`, 내용 없는 임시 파일)은 임의로 커밋하지 말고 제외 후 보고한다.
   - 비밀값·자격증명으로 보이는 파일은 커밋하지 않고 즉시 알린다.

3. **단위 분할 계획**
   - 한 커밋 = 한 시스템/기능. 데이터·프리팹·테스트·문서는 소속 기능 커밋에 함께 넣는다. `.meta`는 짝 파일과 같은 커밋에 둔다.
   - 여러 기능이 공유하는 파일(GameManager, SaveManager, 입력 정의 등)은 가장 밀접한 커밋에 넣고, 그 때문에 중간 커밋이 단독 빌드되지 않을 수 있음을 최종 보고에 밝힌다.
   - `.gitignore`가 신규 에셋의 예외 규칙을 포함하면 **가장 먼저** 따로 커밋해 이후 `git add`가 막히지 않게 한다.
   - 문서/가이드, 일회성 제작 스크립트는 코드 기능 커밋과 분리한다.
   - 계획은 순서 목록으로 짧게 정리한 뒤 바로 진행한다(사용자가 확인을 요구했을 때만 멈춘다).

4. **커밋 수행** (단위마다 반복)
   - 경로를 명시해 `git add <경로들>`로 해당 단위만 stage한다. `git add -A`/`.`은 쓰지 않는다.
   - stage 후 `git diff --cached --name-only`로 제외 대상(모델 등)이 섞이지 않았는지, `git diff --cached --stat`으로 범위가 맞는지 확인한다.
   - 커밋은 PowerShell here-string으로 한다(닫는 `'@`는 0열). 한 줄이면 `-m "..."`.
     ```powershell
     git commit -q -m @'
     제목 줄

       - 불릿 1
       - 불릿 2
     '@
     ```
   - 훅 우회(`--no-verify`), 서명 해제, amend, push는 사용자가 명시하지 않는 한 하지 않는다.
   - tracked인데 ignore 경로에 있는 파일은 `git add` 시 경고가 나도 tracked 변경은 stage된다. 실제 stage 여부는 `git diff --cached --name-only`로 확인한다.
   - 이 환경은 git 경고(LF→CRLF)와 로그 출력 때문에 종료 코드가 255가 될 수 있다. 종료 코드만 믿지 말고 `git log --oneline`, `git status --short`로 결과를 확인한다.

5. **마무리 보고**
   - 만든 커밋 목록(해시 또는 제목)을 표로 정리한다.
   - 제외한 파일과 사유(100MB 초과 / 모델 / 임시물)를 명시한다.
   - 단독 빌드되지 않을 수 있는 커밋, 컴파일·테스트 미수행 여부를 사실대로 적는다.
   - 푸시는 하지 않았음을 밝힌다.

## 주의

- 무엇을 커밋할지 사용자 의도가 우선이다. 사용자가 범위(특정 폴더만, 특정 기능만)를 지정하면 그 범위만 처리한다.
- 삭제된 파일(`D`)도 해당 기능 커밋에 포함한다(`git add`로 삭제를 stage).
- 사용 후 확인용으로 `git status`에 남은 항목이 전부 의도된 제외 대상인지 점검한다.
