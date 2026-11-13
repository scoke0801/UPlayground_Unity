"""dockit 컴포넌트 전체를 쓰는 예시 템플릿 (에디토리얼 스타일 설계 문서).

실행: python -I .claude/skills/explain-diagram/scripts/example_rich.py <출력.html> [dark|light|auto]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from svgkit import Diagram
from dockit import (Section, html_page, card, grid, chips, compare, steps, callout, stats, swatches, table, code)

out = sys.argv[1] if len(sys.argv) > 1 else "example_rich.html"
theme = sys.argv[2] if len(sys.argv) > 2 else "dark"

# 도식: 파이프라인 + 분기
d = Diagram(960, 300, title="요청 처리 파이프라인")
d.group("입력", 20, 56, 260, 200, kind="d")
d.group("처리", 300, 56, 380, 200, kind="a")
d.group("출력", 700, 56, 240, 200, kind="b")
d.node("in", 50, 110, 200, 60, "요청", "사용자 입력", kind="muted", badge="1")
d.node("v", 320, 110, 110, 60, "검증", "스키마 확인", badge="2")
d.node("r", 540, 110, 120, 60, "실행", "핵심 로직", kind="primary", badge="3")
d.node("out", 730, 110, 180, 60, "응답", "결과 전달", kind="data", badge="4")
d.node("err", 320, 190, 340, 50, "실패 처리", "검증 실패 시 여기로", kind="warn")
d.edge("in", "v")
d.edge("v", "r", "통과")
d.edge("r", "out")
d.edge("v", "err", "실패", dashed=True, a_side="b", b_side="t")

sections = [
    Section("한눈에 보기", sub="문서 하나로 구조와 결정, 다음 일까지 읽는다",
            body=(stats([("4", "처리 단계"), ("2", "분기 경로"), ("0", "미해결 항목")])
                  + callout("ok", "결론", "검증을 앞단에 두고 실행은 한 곳에서만 한다. 실패 경로는 실행 단계를 건드리지 않는다.")
                  + grid(3,
                         card("입력", "<p>요청을 받아 형식만 확인한다.</p>", label="STAGE 1", kind="accent"),
                         card("실행", "<p>유일한 상태 변경 지점이다.</p>", label="STAGE 3", kind="ok"),
                         card("실패", "<p>실행 전에 끊고 원인을 돌려준다.</p>", label="BRANCH", kind="warn")))),
    Section("구조도", sub="그림은 설명과 함께 읽는다",
            body="<p>아래 도식이 전체 흐름이다. 실행 단계는 하나뿐이어서 롤백 경로가 필요 없다.</p>",
            diagram=d,
            caption="<b>무엇을 보여 주나:</b> 요청이 처리되는 순서. <b>읽는 법:</b> 배지 1→4를 따라가고, 점선은 실패 분기다. 핵심은 실패가 실행 단계에 도달하지 않는다는 점이다."),
    Section("문제와 개선", sub="전·후를 한 화면에서 비교한다",
            body=(compare("현재", "<p>검증이 실행 안에 섞여 있다.</p>" + chips(["실패 시 부분 반영", "원인 추적 어려움"], "bad"),
                          "개선", "<p>검증을 앞단으로 분리한다.</p>" + chips(["실패해도 상태 불변", "단계별 로그"], "ok")))),
    Section("작업 순서", sub="의존 순서대로 진행한다",
            body=steps([("검증 분리", "스키마 확인을 별도 단계로 꺼낸다", "P0"),
                        ("실패 경로", "실패 응답과 로그 형식을 통일한다", "P1"),
                        ("회귀 확인", "기존 경로가 같은 결과를 내는지 대조한다", "검증")])),
    Section("참고 자료",
            body=(table(["항목", "값", "비고"], [["단계 수", "<b>4</b>", "검증·실행·응답·실패"], ["상태 변경 지점", "<b>1</b>", "실행 단계"]])
                  + swatches([("var(--accent)", "핵심", "primary"), ("var(--ok)", "개선", "after"), ("var(--bad)", "문제", "before")])
                  + code("Validate(request)\\n  .Then(Execute)\\n  .Else(Fail)"))),
]

html_page("설계 문서 예시", "컴포넌트를 한 번씩 써 본 템플릿이다. 복사해서 내용만 바꾼다.", sections, out,
          theme=theme, tag="DESIGN", meta=["2026-11-13", "예시", f"theme={theme}"])
print("saved", out)
