"""설명 문서용 SVG 다이어그램 빌더 (표준 라이브러리만 사용).

사용법:
    import sys; sys.path.insert(0, ".claude/skills/explain-diagram/scripts")
    from svgkit import Diagram, grid, html_page

    d = Diagram(960, 420, title="전투 3계층")
    d.group("판단", 20, 60, 280, 320, kind="a")
    d.node("bt", 50, 120, 220, 70, "BT", sub="무엇을 할지 판단", kind="primary")
    d.node("gas", 380, 120, 220, 70, "GAS", sub="수치·비용·쿨다운")
    d.edge("bt", "gas", "Ability 선택")
    d.save_svg("out.svg")

kind: default / primary / data / external / warn / muted  (노드)
      a / b / c / d                                      (그룹 배경 톤)
라이트·다크 테마는 SVG 내부 CSS 변수로 자동 전환된다.
"""
from __future__ import annotations

import html
import unicodedata
from pathlib import Path

FONT = "'Pretendard','Malgun Gothic','Noto Sans KR','Segoe UI',sans-serif"

STYLE = f"""
.bg{{fill:var(--bg)}}
text{{font-family:{FONT};fill:var(--ink)}}
.title{{font-size:20px;font-weight:700}}
.lbl{{font-size:14px;font-weight:600}}
.sub{{font-size:12px;fill:var(--ink2)}}
.glabel{{font-size:12px;font-weight:700;fill:var(--ink2);letter-spacing:.02em}}
.elabel{{font-size:12px;fill:var(--ink2)}}
.edge{{fill:none;stroke:var(--line);stroke-width:1.6}}
.edge.dashed{{stroke-dasharray:6 4}}
.arrow{{fill:var(--line)}}
.pill{{fill:var(--bg3,var(--bg));stroke:none}}
.n-default{{fill:var(--n0);stroke:var(--n0s)}}
.n-primary{{fill:var(--n1);stroke:var(--n1s)}}
.n-data{{fill:var(--n2);stroke:var(--n2s)}}
.n-external{{fill:var(--n3);stroke:var(--n3s);stroke-dasharray:5 3}}
.n-warn{{fill:var(--n4);stroke:var(--n4s)}}
.n-muted{{fill:var(--n5);stroke:var(--n5s)}}
.g-a{{fill:var(--ga)}} .g-b{{fill:var(--gb)}} .g-c{{fill:var(--gc)}} .g-d{{fill:var(--gd)}}
.node,.grp{{stroke-width:1.4}}
.grp{{stroke:none}}
.badge{{fill:var(--n1s)}}
.badget{{font-size:11px;font-weight:700;fill:#fff}}
"""

LIGHT = """--bg:#ffffff;--ink:#1c2230;--ink2:#566074;--line:#7b8599;
--n0:#f3f5f9;--n0s:#b9c1d1;--n1:#e3edff;--n1s:#3b6fd4;--n2:#e6f6ec;--n2s:#3d9a62;
--n3:#ffffff;--n3s:#8a93a6;--n4:#fff0e0;--n4s:#d9822b;--n5:#eceef2;--n5s:#c5cad4;
--ga:#f6f8fc;--gb:#f4faf6;--gc:#fdf7ef;--gd:#f8f4fc;"""
DARK = """--bg:#161a22;--ink:#e8ebf2;--ink2:#9aa3b5;--line:#7f8aa0;
--n0:#222836;--n0s:#3b4357;--n1:#1d2d4d;--n1s:#6a9bf0;--n2:#18332a;--n2s:#58b97f;
--n3:#161a22;--n3s:#6c758a;--n4:#3a2a18;--n4s:#e8a050;--n5:#1e222c;--n5s:#343b4b;
--ga:#1b2029;--gb:#1a2420;--gc:#241f19;--gd:#211c28;"""


def text_width(s: str, px: float) -> float:
    """CJK(전각)는 1em, 그 외는 0.58em으로 폭을 추정한다."""
    w = 0.0
    for ch in s:
        w += px if unicodedata.east_asian_width(ch) in ("W", "F") else px * 0.58
    return w


def wrap(s: str, max_w: float, px: float) -> list[str]:
    """max_w 안에 들어가도록 글자 단위로 줄바꿈한다 (\\n 존중)."""
    out: list[str] = []
    for para in s.split("\n"):
        line = ""
        for ch in para:
            if text_width(line + ch, px) > max_w and line:
                out.append(line)
                line = ch
            else:
                line += ch
        out.append(line)
    return out


def grid(cols: int, x0: float, y0: float, cw: float, ch: float, gx: float, gy: float, n: int):
    """n개 칸의 (x, y) 좌표 목록. 노드 크기가 cw x ch."""
    return [(x0 + (i % cols) * (cw + gx), y0 + (i // cols) * (ch + gy)) for i in range(n)]


class Diagram:
    _counter = 0

    def __init__(self, width: int, height: int, title: str | None = None, desc: str = ""):
        Diagram._counter += 1
        self._uid = f"d{Diagram._counter}"
        self.w, self.h, self.title, self.desc = width, height, title, desc
        self._groups: list[str] = []
        self._edges: list[str] = []
        self._nodes: list[str] = []
        self._extras: list[str] = []
        self._label_boxes: list = []
        self.kinds_used: list[str] = []
        self.boxes: dict[str, tuple[float, float, float, float]] = {}

    # ---- 요소 ----
    def group(self, label: str, x, y, w, h, kind: str = "a"):
        """레이어/영역 배경 박스. 노드보다 먼저 그려진다."""
        self._groups.append(
            f'<rect class="grp g-{kind}" x="{x}" y="{y}" width="{w}" height="{h}" rx="14"/>'
            f'<text class="glabel" x="{x + 14}" y="{y + 22}">{html.escape(label)}</text>'
        )

    def node(self, id: str, x, y, w, h, label: str, sub: str = "", kind: str = "default", badge: str = ""):
        """박스 노드. label은 굵게, sub는 작은 보조 설명(자동 줄바꿈)."""
        self.boxes[id] = (x, y, w, h)
        if kind not in self.kinds_used:
            self.kinds_used.append(kind)
        lines_l = wrap(label, w - 20, 14)
        lines_s = wrap(sub, w - 20, 12) if sub else []
        total = len(lines_l) * 18 + len(lines_s) * 16
        ty = y + (h - total) / 2 + 14
        parts = [f'<g id="{html.escape(id)}"><rect class="node n-{kind}" x="{x}" y="{y}" width="{w}" height="{h}" rx="10"/>']
        for ln in lines_l:
            parts.append(f'<text class="lbl" x="{x + w / 2}" y="{ty}" text-anchor="middle">{html.escape(ln)}</text>')
            ty += 18
        for ln in lines_s:
            parts.append(f'<text class="sub" x="{x + w / 2}" y="{ty}" text-anchor="middle">{html.escape(ln)}</text>')
            ty += 16
        if badge:
            bw = max(20, text_width(badge, 11) + 12)
            parts.append(f'<rect class="badge" x="{x - 6}" y="{y - 8}" width="{bw}" height="18" rx="9"/>'
                         f'<text class="badget" x="{x - 6 + bw / 2}" y="{y + 5}" text-anchor="middle">{html.escape(badge)}</text>')
        parts.append("</g>")
        self._nodes.append("".join(parts))

    def edge(self, a: str, b: str, label: str = "", dashed: bool = False, via: float | None = None,
             a_side: str | None = None, b_side: str | None = None, both: bool = False):
        """a→b 직교 연결선. 방향은 상대 위치로 자동 결정, a_side/b_side(l/r/t/b)로 강제 가능."""
        ax, ay, aw, ah = self.boxes[a]
        bx, by, bw, bh = self.boxes[b]
        acx, acy, bcx, bcy = ax + aw / 2, ay + ah / 2, bx + bw / 2, by + bh / 2
        dx, dy = bcx - acx, bcy - acy
        horizontal = abs(dx) * (ah + bh) >= abs(dy) * (aw + bw)
        sa = a_side or (("r" if dx > 0 else "l") if horizontal else ("b" if dy > 0 else "t"))
        sb = b_side or {"r": "l", "l": "r", "b": "t", "t": "b"}[sa]
        pa = self._anchor(ax, ay, aw, ah, sa)
        pb = self._anchor(bx, by, bw, bh, sb)
        pts = self._route(pa, sa, pb, sb, via)
        d = "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in pts)
        cls = "edge dashed" if dashed else "edge"
        ms = f' marker-start="url(#ahs-{self._uid})"' if both else ""
        self._edges.append(f'<path class="{cls}" d="{d}" marker-end="url(#ah-{self._uid})"{ms}/>')
        if label:
            mx, my, vertical = self._mid(pts)
            lw = text_width(label, 12) + 10
            if vertical:  # 세로 선은 라벨을 선 오른쪽에 붙여 그룹 라벨·노드와 겹침을 줄인다
                lx = mx + 6
                self._extras.append(f'<text class="elabel" x="{lx:.1f}" y="{my + 4:.1f}">{html.escape(label)}</text>')
                box = (lx - 2, my - 9, lw, 18)
            else:
                box = (mx - lw / 2, my - 9, lw, 18)
                self._extras.append(
                    f'<rect class="pill" x="{box[0]:.1f}" y="{box[1]:.1f}" width="{lw:.1f}" height="18" rx="4"/>'
                    f'<text class="elabel" x="{mx:.1f}" y="{my + 4:.1f}" text-anchor="middle">{html.escape(label)}</text>')
            self._label_boxes.append((f"{a}->{b}:{label}", box))

    def note(self, x, y, text: str, width: float = 220, kind: str = "muted"):
        """자유 배치 메모 상자."""
        lines = wrap(text, width - 20, 12)
        h = len(lines) * 16 + 18
        parts = [f'<rect class="node n-{kind}" x="{x}" y="{y}" width="{width}" height="{h}" rx="8"/>']
        for i, ln in enumerate(lines):
            parts.append(f'<text class="sub" x="{x + 10}" y="{y + 20 + i * 16}">{html.escape(ln)}</text>')
        self._extras.append("".join(parts))

    def text(self, x, y, s: str, cls: str = "sub", anchor: str = "start"):
        self._extras.append(f'<text class="{cls}" x="{x}" y="{y}" text-anchor="{anchor}">{html.escape(s)}</text>')

    def check(self) -> list[str]:
        """엣지 라벨이 노드와 겹치면 경고 목록을 반환한다. 저장 전에 호출해 빈 목록인지 확인."""
        warns = []
        for name, (lx, ly, lw, lh) in self._label_boxes:
            for nid, (x, y, w, h) in self.boxes.items():
                if lx < x + w and lx + lw > x and ly < y + h and ly + lh > y:
                    warns.append(f"라벨 '{name}' 이(가) 노드 '{nid}' 와 겹침 — 노드 간격을 넓히거나 라벨을 줄여라")
            if lx + lw > self.w or lx < 0:
                warns.append(f"라벨 '{name}' 이(가) 캔버스 밖으로 나감")
        return warns

    # ---- 출력 ----
    def to_svg(self, theme: str | None = None, transparent: bool = False) -> str:
        """theme: None(시스템 설정 따름) / 'light' / 'dark'. transparent=True면 배경을 그리지 않아 카드 색이 비친다."""
        th = f' data-theme="{theme}"' if theme else ""
        head = (f'<svg xmlns="http://www.w3.org/2000/svg" class="dg" viewBox="0 0 {self.w} {self.h}" width="{self.w}" '
                f'height="{self.h}"{th} role="img" aria-label="{html.escape(self.title or "diagram")}">')
        if self.desc:
            head += f"<desc>{html.escape(self.desc)}</desc>"
        defs = ("<defs><style>svg.dg{" + LIGHT + "}@media (prefers-color-scheme:dark){svg.dg:not([data-theme=light]){"
                + DARK + "}}svg.dg[data-theme=dark]{" + DARK + "}" + STYLE + "</style>"
                f'<marker id="ah-{self._uid}" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto">'
                '<path class="arrow" d="M0,1 L10,5 L0,9 z"/></marker>'
                f'<marker id="ahs-{self._uid}" viewBox="0 0 10 10" refX="1" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse">'
                '<path class="arrow" d="M0,1 L10,5 L0,9 z"/></marker></defs>')
        body = "" if transparent else f'<rect class="bg" width="{self.w}" height="{self.h}"/>'
        if self.title:
            body += f'<text class="title" x="20" y="34">{html.escape(self.title)}</text>'
        body += "".join(self._groups) + "".join(self._edges) + "".join(self._nodes) + "".join(self._extras)
        return head + defs + body + "</svg>"

    def save_svg(self, path: str | Path) -> Path:
        for w in self.check():
            print("WARN:", w)
        p = Path(path)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(self.to_svg(), encoding="utf-8")
        return p

    # ---- 내부 ----
    @staticmethod
    def _anchor(x, y, w, h, side):
        return {"l": (x, y + h / 2), "r": (x + w, y + h / 2), "t": (x + w / 2, y), "b": (x + w / 2, y + h)}[side]

    @staticmethod
    def _route(pa, sa, pb, sb, via):
        (x1, y1), (x2, y2) = pa, pb
        if sa in "lr" and sb in "lr":
            if abs(y1 - y2) < 1:
                return [pa, pb]
            mx = via if via is not None else (x1 + x2) / 2
            return [pa, (mx, y1), (mx, y2), pb]
        if sa in "tb" and sb in "tb":
            if abs(x1 - x2) < 1:
                return [pa, pb]
            my = via if via is not None else (y1 + y2) / 2
            return [pa, (x1, my), (x2, my), pb]
        return [pa, (x2, y1), pb] if sa in "lr" else [pa, (x1, y2), pb]

    @staticmethod
    def _mid(pts):
        segs = [(abs(b[0] - a[0]) + abs(b[1] - a[1]), a, b) for a, b in zip(pts, pts[1:])]
        total = sum(s[0] for s in segs)
        acc = 0.0
        for ln, a, b in segs:
            if acc + ln >= total / 2:
                t = (total / 2 - acc) / ln if ln else 0
                return a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, abs(b[0] - a[0]) < 1
            acc += ln
        return pts[0][0], pts[0][1], False


def html_page(*args, **kwargs):
    """설명 문서 HTML 생성. 구현은 dockit.html_page(컴포넌트·테마 포함)로 옮겼다. 예전 import 경로 호환용."""
    from dockit import html_page as _html_page
    return _html_page(*args, **kwargs)
