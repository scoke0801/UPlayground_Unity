import json, html, sys, os
root = sys.argv[1]
items = json.load(open(os.path.join(root, 'manifest.json'), encoding='utf-8'))
e = html.escape

CATS = [
    ('tree', '나무·그루터기·통나무'),
    ('plant', '덤불·꽃·풀'),
    ('rock', '바위·돌·지형'),
    ('structure', '건물·구조물·울타리'),
    ('prop', '소품·기타'),
    ('dungeon', '던전 키트'),
]

def pack(p):
    parts = p.split('/')
    if parts[1] == 'ExternalAssets':
        if len(parts) > 4 and parts[3] == 'PolyartStudio':
            return 'Polyart ' + parts[4]
        if len(parts) > 3:
            return parts[3] if parts[2] != 'Character' else 'Suriyun ' + parts[4]
    return '/'.join(parts[1:4]) if len(parts) > 4 else '/'.join(parts[1:-1])

CSS = '''
body{font-family:'Malgun Gothic',sans-serif;background:#14181d;color:#e6e9ee;margin:0;padding:24px 32px}
a{color:#7fb3ff}h1{margin:0 0 4px}h2{margin:36px 0 12px;border-bottom:1px solid #3a4452;padding-bottom:6px}
.sub{color:#8d99a8;font-size:13px}nav{margin:10px 0}nav a{margin-right:14px}
input{padding:8px 12px;width:320px;border-radius:6px;border:1px solid #3a4452;background:#1d232b;color:#e6e9ee;margin:12px 0}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:12px}
.card{background:#1d232b;border:1px solid #2c3541;border-radius:8px;overflow:hidden}
.card img{width:100%;aspect-ratio:1;display:block;background:#a8c0e0}
.card .t{padding:8px 10px;font-size:12px}.card b{font-size:13px;display:block;margin-bottom:3px;word-break:break-all}
.card .p{color:#8d99a8;word-break:break-all;cursor:pointer}.card .m{color:#7fb3ff;margin-top:3px}
.idx a.c{display:block;background:#1d232b;border:1px solid #2c3541;border-radius:8px;padding:14px 18px;margin:10px 0;text-decoration:none;color:#e6e9ee}
'''

def nav(cur):
    s = '<nav><a href="index.html">목차</a>'
    for k, t in CATS:
        s += f'<a href="{k}.html"{" style=\"font-weight:bold\"" if k == cur else ""}>{t}</a>'
    return s + '</nav>'

counts = {}
for k, title in CATS:
    lst = [i for i in items if i['cat'] == k]
    counts[k] = len(lst)
    groups = {}
    for i in lst:
        groups.setdefault(pack(i['path']), []).append(i)
    out = [f'<!doctype html><html lang="ko"><head><meta charset="utf-8"><title>{e(title)}</title><style>{CSS}</style></head><body>',
           f'<h1>{e(title)} <span class="sub">({len(lst)})</span></h1>', nav(k),
           '<div class="sub">Unity 프리팹 캡처 (LOD0, 동일 각도·조명). 경로 클릭 시 복사. H·W는 월드 단위(m), tris는 LOD0 삼각형 수.</div>',
           '<input id="q" placeholder="이름·경로 검색…" oninput="f()">']
    for g in sorted(groups):
        out.append(f'<section><h2>{e(g)} <span class="sub">({len(groups[g])})</span></h2><div class="grid">')
        for i in sorted(groups[g], key=lambda x: x['path']):
            out.append(f'<div class="card" data-s="{e((i["name"] + i["path"]).lower())}"><img loading="lazy" src="img/{i["img"]}">'
                       f'<div class="t"><b>{e(i["name"])}</b><div class="p" onclick="navigator.clipboard.writeText(this.textContent)">{e(i["path"])}</div>'
                       f'<div class="m">H {i["h"]} × W {i["w"]} m · {i["tris"]:,} tris</div></div></div>')
        out.append('</div></section>')
    out.append('''<script>function f(){const q=document.getElementById('q').value.toLowerCase();
document.querySelectorAll('.card').forEach(c=>c.style.display=c.dataset.s.includes(q)?'':'none')}</script></body></html>''')
    open(os.path.join(root, k + '.html'), 'w', encoding='utf-8').write(''.join(out))

idx = [f'<!doctype html><html lang="ko"><head><meta charset="utf-8"><title>환경 에셋 카탈로그</title><style>{CSS}</style></head><body>',
       '<h1>환경 에셋 카탈로그</h1><div class="sub">Unity 환경 프리팹을 카테고리별로 캡처한 목록. 카테고리는 이름·경로 기반 자동 분류라 일부 오분류가 있을 수 있음.</div><div class="idx">']
for k, t in CATS:
    idx.append(f'<a class="c" href="{k}.html"><b>{e(t)}</b> <span class="sub">— {counts[k]}개</span></a>')
idx.append('</div></body></html>')
open(os.path.join(root, 'index.html'), 'w', encoding='utf-8').write(''.join(idx))
print(counts, sum(counts.values()))
