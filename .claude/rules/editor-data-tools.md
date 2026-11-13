---
paths:
  - "Assets/02.Scripts/**/Editor/**"
---

# Editor 데이터 도구 안전 규칙

에셋을 일괄 생성·수정·가져오는 Editor 도구를 고칠 때 적용한다. Claude Code는 Editor 폴더 파일을 읽거나 고칠 때 자동으로 읽고, 다른 Agent는 AGENTS.md 안내를 따라 직접 연다.

- SO Spreadsheet import의 기존 에셋 식별은 GUID 정확 일치, path 정확 일치 순으로 처리한다. GUID/path가 제공됐는데 유효하지 않으면 이름으로 폴백하지 말고 import를 실패시킨다.
- 이름 폴백은 GUID와 path가 둘 다 없을 때만 허용하며, 동일 이름 후보가 여러 개면 모호성 오류로 처리한다. ObjectReference export에는 GUID/path/name과 함께 assembly-qualified type을 보존한다.
- import 중 예외가 발생하면 해당 Undo group 전체를 `Undo.RevertAllDownToGroup`으로 롤백하고 저장한다. 일부 적용 상태를 성공처럼 collapse하지 않는다.
- P09 빌더는 기존 에셋을 삭제·교체하는 경로가 있어 현재 완전한 transaction으로 간주하지 않는다. 이 경로를 수정할 때는 임시 스테이징, 기존 에셋 백업/복구, 중간 단계 실패 테스트를 포함한 별도 설계를 먼저 수립한다.
- 새 도구의 노출 여부는 [코드 컨벤션](code-conventions.md)의 툴 런처 규칙을 따른다.
