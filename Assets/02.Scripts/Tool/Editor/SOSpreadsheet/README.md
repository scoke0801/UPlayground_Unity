# SO 스프레드시트 (독립 모듈)

프로젝트의 ScriptableObject 에셋을 타입별로 모아 행(에셋) × 열(직렬화 필드) 스프레드시트로
조회/편집하는 UIToolkit 에디터 툴. 메뉴: `UPlayGround/SO 스프레드시트`.

배열/리스트는 열로 펼치지 않고 **"N Items" 요약 셀**로 표시하며, 클릭하면 **우측 상세 패널**에서
전체 리스트(추가/삭제/재정렬 포함)를 편집한다 (Game Data Workbench 스타일).
자식 필드 토글이 꺼진 중첩 클래스({…} 셀)도 같은 방식으로 패널에서 편집한다.

상세 패널은 요약 셀을 정확히 누르지 않아도 **행의 빈 영역 클릭**으로 열린다
(현재/마지막으로 봤던 열 → 첫 요약 열 순으로 대상 선택). 같은 행이나 같은 요약 셀을
**다시 클릭하면 패널이 닫힌다** (토글). 편집 필드/버튼/스크롤바 위의 클릭은 본래 동작을 유지한다.

## 검색 / 필터

- **검색 필드**: 기본은 에셋 이름 검색. 입력은 250ms 디바운스로 반영된다.
- **값 검색 토글**: 켜면 셀 값(숫자/문자열/enum 표시명/오브젝트 참조 이름)에서도 검색한다.
  타입의 전체 에셋을 로드하므로 첫 검색만 느릴 수 있다 (이후 캐시).
- **필터 ▾**: 열 단위 값 필터를 칩으로 추가한다. 조건이 빈 필터는 전체 통과.
  - enum/bool 열 → 값 다중 선택 메뉴 ([Flags] enum은 비트 포함 판정)
  - 숫자 열 → 비교식: `10`, `>=10`, `<5`, `3..8`
  - 문자열/참조 열 → 포함 텍스트 (대소문자 무시)
  - 리스트 요약 열 → 요소 수 기준 비교식
  - 필터는 타입을 바꾸면 초기화되고, 도메인 리로드에는 유지된다.

## 성능 설계

- 커스텀 드로어가 없는 단순 타입 셀은 PropertyField 대신 **타입 전용 필드**(IntegerField,
  FloatField, EnumField, ObjectField 등)로 그린다. PropertyField의 드로어 해석·내부 재생성
  비용이 페이지 전환/스크롤 병목의 주범이라 이 경로가 체감 성능의 핵심이다.
- `UpdateIfRequiredOrScript`는 갱신 패스당 행 1회만 호출한다 (이전에는 행×열마다 호출).
- ObjectField 픽커 타입 제한과 EnumField 생성용 enum 타입은 열 구성 시 1회 리플렉션해 캐시한다.

## 구성

| 파일 | 역할 |
| --- | --- |
| `SOSpreadsheetModel.cs` | 스캔 / 열 평탄화(배열은 요약 열) / 필터·정렬·페이지네이션. UI 비의존 데이터 계층 |
| `SOSpreadsheetWindow.cs` | UIToolkit 창. MultiColumnListView 테이블, 틀 고정(TwoPaneSplitView), 우측 상세 패널, 툴바 |
| `SOPropertyDrawerUtility.cs` | 커스텀 PropertyDrawer/데코레이터 판별 (UnityEditor 내부 리플렉션, 실패 시 무해) |
| `SOSpreadsheet.uss` | 셀/툴바 스타일. 창 스크립트 위치 기준으로 로드 |
| `SOSpreadsheet.Editor.asmdef` | Editor 전용 독립 어셈블리. **참조 0개** |

## 다른 프로젝트로 이식

이 폴더를 통째로 복사하면 끝. Unity/UnityEditor API에만 의존하며 프로젝트 코드 참조가 없다.

- 요구 버전: Unity 6000.0+ (MultiColumnListView 정렬 API `sortingMode` 사용)
- 조정 포인트:
  - 메뉴 경로: `SOSpreadsheetWindow.Open`의 `[MenuItem]` 문자열
  - 스캔 제외 폴더: `SOSpreadsheetModel.ExternalPathPrefixes`
  - 네임스페이스 `UPlayGround.Tool.Editor.SOSpreadsheet`는 원 프로젝트 관례를 따른 것으로,
    다른 프로젝트에서 강제 사항 아님 (asmdef 격리라 충돌 없음)

## 알려진 제약

- 열(가로) 방향 가상화는 MultiColumnListView가 지원하지 않아, 표시 열이 수백 개면
  IMGUI 버전보다 스크롤이 무거울 수 있다. 열 표시 메뉴로 숨기거나 페이지 크기를 줄일 것.
- 틀 고정은 스플리터 좌우 두 리스트뷰의 세로 스크롤 동기화로 구현되어,
  고정 영역 너비는 스플리터 드래그로 조정한다.
- 상세 패널은 한 번에 하나의 (에셋, 필드) 조합만 표시한다. 다른 요약 셀을 클릭하면
  패널 내용이 교체된다. 외부(인스펙터)에서 리스트 크기를 바꾸면 표의 "N Items" 숫자는
  다음 갱신(정렬/페이지 이동/새로고침) 전까지 낡을 수 있다.
