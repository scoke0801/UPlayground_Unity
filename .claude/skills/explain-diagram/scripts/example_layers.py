"""svgkit 사용 예시이자 템플릿. 복사해서 내용만 바꿔 쓴다.

실행: python .claude/skills/explain-diagram/scripts/example_layers.py <출력.html>
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from svgkit import Diagram, html_page

out = sys.argv[1] if len(sys.argv) > 1 else "example.html"

# 1) 계층 구조도: 세로 3층 + 층 사이 화살표
d1 = Diagram(940, 470, title="전투 시스템 3계층")
d1.group("판단 — 무엇을 할까", 20, 56, 900, 100, kind="a")
d1.group("수치 — 얼마나 / 비용은", 20, 196, 900, 100, kind="b")
d1.group("타이밍 — 언제 맞나", 20, 336, 900, 100, kind="c")
d1.node("bt", 60, 92, 240, 56, "Behavior Tree", "상황을 보고 Ability 선택", kind="primary")
d1.node("ai", 400, 92, 200, 56, "aiSelectable", "선택 후보 필터", kind="muted")
d1.node("gas", 60, 232, 240, 56, "GameplayAbilitySO", "비용·쿨다운·Variant", kind="primary")
d1.node("pay", 400, 232, 240, 56, "Motion Payload", "motionKey + HitPhase", kind="data")
d1.node("ms", 60, 372, 240, 56, "MotionSetAsset", "타임라인 이벤트", kind="primary")
d1.node("hit", 400, 372, 240, 56, "히트박스·VFX·SFX", "프레임 단위 발화", kind="data")
d1.node("ext", 730, 232, 170, 56, "AbilitySetSO", "캐릭터별 Ability 묶음", kind="external")
d1.edge("bt", "ai", "후보")
d1.edge("bt", "gas", "활성화 요청", a_side="b", b_side="t")
d1.edge("gas", "pay", "실행 Payload")
d1.edge("gas", "ms", "motionKey 해석", a_side="b", b_side="t")
d1.edge("ms", "hit", "재생")
d1.edge("ext", "pay", "참조", dashed=True)

# 2) 흐름도: 가로 파이프라인
d2 = Diagram(900, 200, title="입력에서 피격까지")
names = [("입력", "InputBuffer"), ("상태 전이", "CanTransitionState"), ("Ability", "Prepare/Commit"), ("모션", "MotionSet"), ("판정", "HitPhase")]
xs = [20 + i * 175 for i in range(5)]
for i, ((n, s), x) in enumerate(zip(names, xs)):
    d2.node(f"s{i}", x, 80, 150, 64, n, s, kind="primary" if i in (0, 4) else "default", badge=str(i + 1))
for i in range(4):
    d2.edge(f"s{i}", f"s{i + 1}")

html_page(
    "전투 시스템 구조 (예시)",
    "한 줄 요약: 판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 맡는다.",
    [
        ("전체 구조", "<p>세 계층은 서로의 책임을 넘지 않는다.</p>", d1,
         "세로 3층이 판단·수치·타이밍 책임이다. <b>실선 화살표</b>는 호출 방향, <b>점선</b>은 참조다. 왼쪽 열을 위에서 아래로 따라가면 공격 하나가 실행되는 순서가 된다."),
        ("실행 흐름", "<p>번호 순서대로 한 번의 공격이 처리된다.</p>", d2,
         "배지 숫자 순서대로 읽는다. 파랑 박스(입력·판정)가 시작과 끝이다."),
        ("용어", "<table><tr><th>용어</th><th>뜻</th></tr><tr><td>Payload</td><td>Ability가 실행할 내용물</td></tr></table>", None),
    ],
    out,
)
print("saved", out)
