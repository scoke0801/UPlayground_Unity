"""설명 문서 HTML 조립 도구 (표준 라이브러리만 사용).

`BT Editor Design Guide.html` 같은 에디토리얼 스타일(히어로·번호 섹션·카드 그리드·칩·전후 비교·로드맵)을
만들기 위한 컴포넌트와 페이지 조립기. SVG 도식은 svgkit.Diagram을 그대로 끼운다.

    from dockit import Section, html_page, card, grid, chips, compare, steps, callout, stats, table
    html_page("제목", "한 줄 요약", [Section("전체 그림", body_html, diagram=d, caption="...")], out, tag="DESIGN")

모든 컴포넌트는 HTML 문자열을 돌려주며 Section.body 안에 이어 붙여 쓴다. 사용자 텍스트는 자동 이스케이프되지 않으므로
(본문에 <code> 같은 태그를 쓰려고) 신뢰할 수 있는 문자열만 넣고, 꺾쇠가 필요한 평문은 esc()를 거친다.
"""
from __future__ import annotations

import html
from dataclasses import dataclass
from pathlib import Path

esc = html.escape

FONT_LINK = ('<link rel="preconnect" href="https://fonts.googleapis.com">'
             '<link href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+KR:wght@300;400;500;600'
             '&family=IBM+Plex+Mono:wght@400;500&display=swap" rel="stylesheet">')
SANS = "'IBM Plex Sans KR','Pretendard','Malgun Gothic','Noto Sans KR','Segoe UI',sans-serif"
MONO = "'IBM Plex Mono','Cascadia Mono',Consolas,monospace"

DARK = """--bg:#0f0f11;--bg2:#16161a;--bg3:#1e1e24;--bd:#2e2e38;--bd2:#3a3a48;
--tx:#e8e8f0;--tx2:#9898b0;--tx3:#6a6a82;--accent:#6c9cf0;--accent-bg:#1b2438;--accent-bd:#2c3d63;
--ok:#5cc88a;--ok-bg:#15261d;--ok-bd:#245238;--warn:#e6a24c;--warn-bg:#2a2214;--warn-bd:#5a4421;
--bad:#ee7a6e;--bad-bg:#2a1816;--bad-bd:#5c2b27;--grid:rgba(255,255,255,.05);--shadow:0 4px 24px rgba(0,0,0,.45);"""
LIGHT = """--bg:#f6f7fa;--bg2:#ffffff;--bg3:#eef0f5;--bd:#dde1ea;--bd2:#c6ccd9;
--tx:#1c2230;--tx2:#566074;--tx3:#8a93a6;--accent:#3b6fd4;--accent-bg:#e8f0ff;--accent-bd:#b8cdf5;
--ok:#2f8f58;--ok-bg:#e7f6ed;--ok-bd:#a9dbbd;--warn:#b86a12;--warn-bg:#fff1df;--warn-bd:#efc995;
--bad:#c4473c;--bad-bg:#fdecea;--bad-bd:#f0b7b1;--grid:rgba(28,34,48,.06);--shadow:0 4px 18px rgba(28,34,48,.10);"""

CSS = f"""
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:{SANS};background:var(--bg);color:var(--tx);font-size:15px;line-height:1.7}}
.page{{max-width:1180px;margin:0 auto;padding:48px 28px 120px}}
code{{font-family:{MONO};font-size:.88em;background:var(--bg3);border:1px solid var(--bd);padding:1px 6px;border-radius:4px}}
a{{color:var(--accent)}}
.hero{{border-bottom:1px solid var(--bd);padding-bottom:36px;margin-bottom:32px}}
.tag{{display:inline-block;font-family:{MONO};font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:var(--accent);
background:var(--accent-bg);border:1px solid var(--accent-bd);padding:3px 10px;border-radius:4px;margin-bottom:16px}}
.hero h1{{font-size:clamp(28px,4vw,38px);font-weight:600;letter-spacing:-.02em;line-height:1.25;margin-bottom:12px}}
.hero .lead{{color:var(--tx2);font-size:16px;max-width:720px}}
.meta{{display:flex;flex-wrap:wrap;gap:8px;margin-top:18px}}
.meta span{{font-family:{MONO};font-size:12px;color:var(--tx2);background:var(--bg2);border:1px solid var(--bd);padding:3px 10px;border-radius:100px}}
.toc{{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:56px}}
.toc a{{font-size:13px;color:var(--tx2);text-decoration:none;background:var(--bg2);border:1px solid var(--bd);padding:5px 12px;border-radius:6px}}
.toc a:hover{{color:var(--tx);border-color:var(--bd2)}} .toc a b{{font-family:{MONO};font-weight:500;color:var(--tx3);margin-right:6px;font-size:11px}}
section.sec{{margin-bottom:72px;scroll-margin-top:16px}}
.sec-head{{display:flex;align-items:baseline;gap:16px;margin-bottom:24px;padding-bottom:12px;border-bottom:1px solid var(--bd)}}
.sec-num{{font-family:{MONO};font-size:12px;color:var(--tx3);letter-spacing:.08em}}
.sec-head h2{{font-size:23px;font-weight:600;letter-spacing:-.01em}}
.sec-sub{{color:var(--tx2);font-size:14px;margin:-12px 0 20px}}
.sec p{{color:var(--tx2);margin:0 0 12px;max-width:820px}} .sec p b,.sec li b{{color:var(--tx);font-weight:600}}
.sec ul,.sec ol{{color:var(--tx2);padding-left:22px;margin:0 0 14px;max-width:820px}} .sec li{{margin:4px 0}}
.grid{{display:grid;gap:18px;margin:18px 0}} .g2{{grid-template-columns:repeat(2,1fr)}} .g3{{grid-template-columns:repeat(3,1fr)}} .g4{{grid-template-columns:repeat(4,1fr)}}
@media(max-width:860px){{.g3,.g4{{grid-template-columns:repeat(2,1fr)}}}} @media(max-width:560px){{.g2,.g3,.g4{{grid-template-columns:1fr}}}}
.card{{background:var(--bg2);border:1px solid var(--bd);border-radius:10px;padding:22px}}
.card.accent{{border-top:2px solid var(--accent)}} .card.ok{{border-top:2px solid var(--ok)}} .card.warn{{border-top:2px solid var(--warn)}} .card.bad{{border-top:2px solid var(--bad)}}
.card .lbl{{font-family:{MONO};font-size:10px;letter-spacing:.1em;text-transform:uppercase;color:var(--tx3);margin-bottom:10px}}
.card h3{{font-size:16px;font-weight:600;margin-bottom:8px}} .card p{{font-size:14px;margin:0 0 8px}} .card p:last-child{{margin:0}}
.chip{{display:inline-flex;align-items:center;gap:6px;font-size:12px;padding:3px 10px;border-radius:100px;margin:3px 4px 3px 0;border:1px solid}}
.chip.bad{{background:var(--bad-bg);border-color:var(--bad-bd);color:var(--bad)}} .chip.bad::before{{content:'!';font-weight:700;font-size:10px}}
.chip.ok{{background:var(--ok-bg);border-color:var(--ok-bd);color:var(--ok)}} .chip.ok::before{{content:'\\2192';font-weight:700}}
.chip.warn{{background:var(--warn-bg);border-color:var(--warn-bd);color:var(--warn)}} .chip.info{{background:var(--accent-bg);border-color:var(--accent-bd);color:var(--accent)}}
.cmp{{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin:18px 0}} @media(max-width:720px){{.cmp{{grid-template-columns:1fr}}}}
.cmp>div{{border:1px solid var(--bd);border-radius:10px;padding:20px;background:var(--bg2)}}
.cmp .before{{border-top:2px solid var(--bad)}} .cmp .after{{border-top:2px solid var(--ok)}}
.cmp h4{{font-family:{MONO};font-size:11px;letter-spacing:.1em;text-transform:uppercase;margin-bottom:10px}}
.cmp .before h4{{color:var(--bad)}} .cmp .after h4{{color:var(--ok)}}
.steps{{list-style:none;padding:0!important;margin:18px 0;display:grid;gap:12px;max-width:none!important}}
.steps li{{display:grid;grid-template-columns:44px 1fr auto;gap:14px;align-items:start;background:var(--bg2);border:1px solid var(--bd);border-radius:10px;padding:16px 18px;margin:0}}
.steps .n{{font-family:{MONO};font-size:13px;color:var(--accent);background:var(--accent-bg);border:1px solid var(--accent-bd);border-radius:8px;text-align:center;padding:4px 0}}
.steps h4{{font-size:15px;font-weight:600;color:var(--tx)}} .steps p{{font-size:13px;margin:2px 0 0}}
.callout{{border:1px solid var(--bd);border-left:3px solid var(--accent);background:var(--bg2);border-radius:8px;padding:14px 18px;margin:18px 0}}
.callout.ok{{border-left-color:var(--ok)}} .callout.warn{{border-left-color:var(--warn)}} .callout.bad{{border-left-color:var(--bad)}}
.callout b.t{{display:block;font-size:13px;margin-bottom:2px;color:var(--tx)}} .callout p{{margin:0;font-size:14px}}
.stats{{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:14px;margin:18px 0}}
.stat{{background:var(--bg2);border:1px solid var(--bd);border-radius:10px;padding:16px 18px}}
.stat b{{display:block;font-family:{MONO};font-size:26px;font-weight:500;color:var(--tx);line-height:1.2}} .stat span{{font-size:12px;color:var(--tx2)}}
.swatches{{display:grid;gap:10px;margin:18px 0}} .sw{{display:flex;align-items:center;gap:14px;padding:10px 14px;border-radius:8px;background:var(--bg3);border:1px solid var(--bd)}}
.sw i{{width:28px;height:28px;border-radius:6px;flex-shrink:0}} .sw b{{font-weight:500;font-size:14px;color:var(--tx)}} .sw span{{font-size:12px;color:var(--tx3);font-family:{MONO};margin-left:auto}}
table{{border-collapse:collapse;width:100%;font-size:14px;margin:16px 0;background:var(--bg2);border-radius:10px;overflow:hidden}}
th,td{{border:1px solid var(--bd);padding:9px 12px;text-align:left;vertical-align:top;color:var(--tx2)}} th{{background:var(--bg3);color:var(--tx);font-weight:600}} td b{{color:var(--tx)}}
pre.code{{background:var(--bg3);border:1px solid var(--bd);border-radius:10px;padding:16px 18px;overflow-x:auto;font-family:{MONO};font-size:13px;line-height:1.6;color:var(--tx);margin:16px 0}}
figure{{margin:22px 0}}
.canvas{{position:relative;background:var(--bg3);border:1px solid var(--bd);border-radius:12px;overflow-x:auto;box-shadow:var(--shadow)}}
.canvas::before{{content:'';position:absolute;inset:0;background-image:linear-gradient(var(--grid) 1px,transparent 1px),linear-gradient(90deg,var(--grid) 1px,transparent 1px);background-size:32px 32px;pointer-events:none}}
.canvas svg{{display:block;position:relative;max-width:100%;height:auto}}
figcaption{{font-size:14px;color:var(--tx2);margin-top:10px;line-height:1.7;max-width:860px}} figcaption b{{color:var(--tx)}}
.lg{{display:flex;flex-wrap:wrap;gap:6px 16px;margin-top:8px}} .lg span{{display:inline-flex;align-items:center;gap:7px;font-size:12px;color:var(--tx2)}}
.lg i{{width:14px;height:14px;border-radius:4px;border:1.4px solid}}
"""

LEGEND = {"primary": "핵심 주체", "default": "일반 구성요소", "data": "데이터·이펙트",
          "external": "외부·연결 지점", "warn": "미확인·주의", "muted": "보조·배경"}
SWATCH_VAR = {"default": "n0", "primary": "n1", "data": "n2", "external": "n3", "warn": "n4", "muted": "n5"}


# ---- 컴포넌트 ---------------------------------------------------------------
def chip(text: str, kind: str = "info") -> str:
    """kind: info / ok(개선) / warn / bad(문제)"""
    return f'<span class="chip {kind}">{text}</span>'


def chips(items, kind: str = "info") -> str:
    return "".join(chip(t, kind) for t in items)


def card(title: str, body: str = "", label: str = "", kind: str = "") -> str:
    """kind: '' / accent / ok / warn / bad — 위쪽 강조선 색."""
    lab = f'<div class="lbl">{label}</div>' if label else ""
    return f'<div class="card {kind}">{lab}<h3>{title}</h3>{body}</div>'


def grid(cols: int, *cells: str) -> str:
    return f'<div class="grid g{cols}">{"".join(cells)}</div>'


def compare(before_title: str, before_html: str, after_title: str, after_html: str) -> str:
    """전·후(문제·개선) 2열 비교."""
    return (f'<div class="cmp"><div class="before"><h4>{before_title}</h4>{before_html}</div>'
            f'<div class="after"><h4>{after_title}</h4>{after_html}</div></div>')


def steps(items) -> str:
    """로드맵·순서. items = [(제목, 설명, 태그|None), ...]"""
    rows = []
    for i, it in enumerate(items, 1):
        t, d = it[0], it[1]
        tag = chip(it[2], "info") if len(it) > 2 and it[2] else ""
        rows.append(f'<li><span class="n">{i:02d}</span><div><h4>{t}</h4><p>{d}</p></div><div>{tag}</div></li>')
    return f'<ol class="steps">{"".join(rows)}</ol>'


def callout(kind: str, title: str, body: str) -> str:
    """kind: info / ok / warn / bad. 핵심 결론·주의에 쓴다."""
    k = "" if kind == "info" else kind
    return f'<div class="callout {k}"><b class="t">{title}</b><p>{body}</p></div>'


def stats(items) -> str:
    """수치 타일. items = [(값, 라벨), ...]"""
    return '<div class="stats">' + "".join(f'<div class="stat"><b>{v}</b><span>{l}</span></div>' for v, l in items) + "</div>"


def swatches(items) -> str:
    """색 샘플. items = [(css색, 이름, 용도), ...]"""
    return '<div class="swatches">' + "".join(
        f'<div class="sw"><i style="background:{c}"></i><b>{n}</b><span>{u}</span></div>' for c, n, u in items) + "</div>"


def table(headers, rows) -> str:
    th = "".join(f"<th>{h}</th>" for h in headers)
    tr = "".join("<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>" for r in rows)
    return f"<table><tr>{th}</tr>{tr}</table>"


def code(text: str) -> str:
    return f'<pre class="code">{esc(text)}</pre>'


# ---- 페이지 ----------------------------------------------------------------
@dataclass
class Section:
    title: str
    body: str = ""
    diagram: object = None      # svgkit.Diagram
    caption: str = ""           # 그림 설명(도식이 있으면 필수)
    sub: str = ""               # 제목 아래 한 줄 부제


def html_page(title: str, lead: str, sections: list, out: str | Path, legend: dict | None = None,
              theme: str = "dark", tag: str = "", meta: list | None = None, toc: bool = True) -> Path:
    """설명 문서 HTML을 만든다.

    sections: Section 목록. 예전 형식 튜플 (제목, 본문, Diagram|None[, 그림설명])도 받는다.
    theme: dark(기본, 참고 문서와 같은 어두운 에디토리얼) / light / auto(시스템 설정 따라감).
    tag: 히어로 위 작은 라벨(예: DESIGN). meta: 히어로 아래 칩(예: ["2026-11-13", "코드 대조 완료"]).
    도식에 쓰인 노드 색은 범례로 자동 표시된다. 색 의미를 바꾸려면 legend={kind: 뜻}.
    """
    meaning = {**LEGEND, **(legend or {})}
    secs = [s if isinstance(s, Section) else Section(*s) for s in sections]
    if theme == "auto":
        root = f":root{{{LIGHT}}}@media(prefers-color-scheme:dark){{:root{{{DARK}}}}}"
        attr = ""
    else:
        root = f":root{{{DARK if theme == 'dark' else LIGHT}}}"
        attr = f' data-theme="{theme}"'
    parts, nav, fig_no = [], [], 0
    for i, s in enumerate(secs, 1):
        nav.append(f'<a href="#s{i}"><b>{i:02d}</b>{esc(s.title)}</a>')
        fig = ""
        if s.diagram:
            fig_no += 1
            for w in s.diagram.check():
                print("WARN:", w)
            if not s.caption:
                print(f"WARN: 도식 '{s.diagram.title}' 에 그림 설명(caption)이 없다")
            chips_html = "".join(
                f'<span><i style="background:var(--{SWATCH_VAR[k]});border-color:var(--{SWATCH_VAR[k]}s)"></i>{esc(meaning.get(k, k))}</span>'
                for k in s.diagram.kinds_used if k in SWATCH_VAR)
            svg = s.diagram.to_svg(theme=None if theme == "auto" else theme, transparent=True)
            fig = (f'<figure><div class="canvas">{svg}</div><figcaption><b>그림 {fig_no}.</b> {s.caption}'
                   f'<div class="lg">{chips_html}</div></figcaption></figure>')
        sub = f'<p class="sec-sub">{s.sub}</p>' if s.sub else ""
        parts.append(f'<section class="sec" id="s{i}"><div class="sec-head"><span class="sec-num">{i:02d}</span>'
                     f"<h2>{esc(s.title)}</h2></div>{sub}{s.body}{fig}</section>")
    tag_html = f'<span class="tag">{esc(tag)}</span><br>' if tag else ""
    meta_html = '<div class="meta">' + "".join(f"<span>{esc(m)}</span>" for m in meta) + "</div>" if meta else ""
    toc_html = f'<nav class="toc">{"".join(nav)}</nav>' if toc and len(secs) > 2 else '<div style="margin-bottom:40px"></div>'
    # 도식 노드 색 변수(범례용)는 svgkit 팔레트를 페이지에도 노출한다
    from svgkit import LIGHT as SV_L, DARK as SV_D
    if theme == "auto":
        legend_vars = f":root{{{SV_L}}}@media(prefers-color-scheme:dark){{:root{{{SV_D}}}}}"
    else:
        legend_vars = f":root{{{SV_D if theme == 'dark' else SV_L}}}"
    legend_vars = legend_vars.replace("--bg:", "--sv-bg:").replace("--ink:", "--sv-ink:").replace("--ink2:", "--sv-ink2:").replace("--line:", "--sv-line:")
    doc = (f'<!doctype html><html lang="ko"{attr}><head><meta charset="utf-8">'
           f'<meta name="viewport" content="width=device-width,initial-scale=1"><title>{esc(title)}</title>{FONT_LINK}'
           f"<style>{root}{legend_vars}{CSS}</style></head><body><div class=\"page\">"
           f'<header class="hero">{tag_html}<h1>{esc(title)}</h1><p class="lead">{lead}</p>{meta_html}</header>'
           f'{toc_html}{"".join(parts)}</div></body></html>')
    p = Path(out)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(doc, encoding="utf-8")
    return p
