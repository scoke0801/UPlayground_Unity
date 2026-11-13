import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';

// 문서의 U,V 좌표를 그대로 써서 그림과 경로 길이의 기준이 달라지지 않게 한다.
const root = path.resolve(import.meta.dirname, '../..');
const output = path.join(root, 'Assets/docs/reference/regular-world/Routes');
fs.mkdirSync(output, { recursive: true });
const nodes = {
  M: { name: '호수 마을', uv: [.37, .56], offset: [-18, -20] },
  O: { name: '관측 유적', uv: [.26, .25], offset: [-18, 32] },
  K: { name: '채석 요새', uv: [.18, .65], offset: [-18, -20] },
  Q: { name: '하단 광산', uv: [.24, .55], offset: [-18, 30] },
  P: { name: '서쪽 고개', uv: [.26, .78], offset: [-20, -18] },
  N: { name: '호수 북안', uv: [.48, .81], offset: [-12, -22] },
  D: { name: '설산 수도원', uv: [.73, .88], offset: [18, -20] },
  R: { name: '숲 능선', uv: [.82, .67], offset: [18, -18] },
  S: { name: '옛 사당', uv: [.76, .48], offset: [18, 32] },
  B1: { name: '숲 다리', uv: [.70, .49], offset: [-18, -24], kind: 'bridge' },
  B2: { name: '초원 다리', uv: [.58, .19], offset: [20, 36], kind: 'bridge' },
  B3: { name: '북안 계류교', uv: [.4872, .8136], offset: [60, 8], kind: 'bridge' },
  F: { name: '선택 여울', uv: [.51, .265], offset: [-20, -18], kind: 'ford' },
  H: { name: '해안 어촌', uv: [.81, .15], offset: [18, 32] },
  L: { name: '섬 수문 유적', uv: [.49, .66], offset: [18, -18] },
  E: { name: '초원 진입', uv: [.24, .12], offset: [-18, 32] },
};
const uv = id => nodes[id].uv;
const townToBridge = [uv('M'), [.44, .53], [.54, .52], [.62, .51], [.67, .50], uv('B1')];
const townToNorth = [uv('M'), [.32, .59], [.29, .64], [.30, .72], [.36, .79], uv('N')];
const river = [[.48, .96], [.48, .90], uv('B3'), [.49, .78], [.49, .66], [.60, .55], uv('B1'), [.75, .45], [.70, .38], [.59, .31], uv('F'), [.43, .24], uv('B2'), [.79, .10], [.87, .035]];
const loops = [
  { id: 'A', title: '생활권 순환', color: '#9a4b20', filename: 'Route_A_LivingLoop.svg',
    points: [...townToBridge, [.74, .50], [.80, .43], [.78, .34], [.69, .27], [.64, .23], uv('B2'), [.54, .16], [.44, .17], [.35, .24], uv('O'), [.31, .37], uv('M')],
    stops: ['M', 'B1', 'B2', 'O', 'M'],
    caption: '마을 → 숲 다리 → 가을 강변 → 초원 다리 → 관측 유적 → 마을',
    note: '두 다리를 기본 횡단으로 사용 · 여울은 검증 후 여는 선택 지름길' },
  { id: 'B', title: '협곡 탐험 순환', color: '#785278', filename: 'Route_B_CanyonLoop.svg',
    points: [uv('M'), uv('Q'), [.20, .60], uv('K'), [.20, .72], uv('P'), [.34, .84], uv('N'), ...townToNorth.slice(0, -1).reverse()],
    stops: ['M', 'Q', 'K', 'P', 'N', 'M'],
    caption: '마을 → 하단 광산 → 채석 요새 → 서쪽 고개 → 호수 북안 → 마을',
    note: '광산 내부는 점선 구간 · 지상 우회로를 함께 확보해 진행 불능 방지' },
  { id: 'C', title: '고지대 탐험 순환', color: '#21637a', filename: 'Route_C_HighlandLoop.svg',
    points: [...townToNorth, uv('B3'), [.56, .85], [.65, .91], uv('D'), [.82, .82], [.87, .74], uv('R'), [.85, .58], uv('S'), [.74, .50], ...townToBridge.slice().reverse()],
    stops: ['M', 'N', 'B3', 'D', 'R', 'S', 'B1', 'M'],
    caption: '마을 → 호수 북안 → 설산 수도원 → 숲 능선 → 옛 사당 → 숲 다리 → 마을',
    note: '고지 접근과 하산을 다른 길로 구성 · 활공 없이 왕복하는 육상 경로' },
];
const branches = [
  { id: 'entry', points: [uv('E'), [.25, .18], uv('O')], kind: 'main' },
  { id: 'coast', points: [uv('B2'), [.66, .20], [.74, .17], uv('H')], kind: 'main' },
  { id: 'ford', points: [[.69, .27], [.61, .275], uv('F'), [.46, .29], [.38, .34], [.31, .37]], kind: 'optional' },
  { id: 'quarry-front', points: [uv('M'), [.29, .61], [.25, .68], uv('K')], kind: 'optional' },
  { id: 'island-bridge', points: [[.30, .72], [.35, .69], uv('L')], kind: 'structure' },
  { id: 'shrine-rear', points: [uv('S'), [.84, .45], [.83, .37], [.78, .34]], kind: 'optional' },
];
const world = p => ({ x: (p[0] - .5) * 1536, z: (p[1] - .5) * 1536 });
const length = points => Math.round(points.slice(1).reduce((sum, p, i) => sum + Math.hypot(p[0] - points[i][0], p[1] - points[i][1]) * 1536, 0));
const xy = p => [80 + p[0] * 1040, 140 + (1 - p[1]) * 1040];
const pts = ps => ps.map(p => xy(p).map(v => v.toFixed(1)).join(',')).join(' ');
const text = (x, y, content, extra = '') => `<text x="${x}" y="${y}" ${extra}>${content}</text>`;
const line = (ps, color, width, dash = '') => `<polyline points="${pts(ps)}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linejoin="round" stroke-linecap="round" ${dash ? `stroke-dasharray="${dash}"` : ''}/>`;
const area = (ps, fill, stroke = 'none') => `<polygon points="${pts(ps)}" fill="${fill}" stroke="${stroke}" stroke-width="2"/>`;
const lake = [[.35, .59], [.34, .68], [.39, .76], [.43, .79], [.52, .79], [.61, .75], [.64, .65], [.60, .55], [.49, .56], [.43, .58]];
const boundary = [[.83, 0], [.82, .08], [.90, .13], [.95, .23], [1, .26], [1, 0]];

function render(active) {
  const focus = loops.find(l => l.id === active);
  const title = focus ? `${focus.id} ${focus.title}` : '월드 전체 이동 경로';
  const selected = new Set(focus?.stops ?? Object.keys(nodes));
  const background = [
    '<rect x="80" y="140" width="1040" height="1040" fill="#dce3c9"/>',
    area([[0, .45], [.24, .42], [.33, .56], [.29, .72], [.25, .85], [0, .86]], '#e6c8a3'),
    area([[0, .83], [.20, .84], [.39, .88], [.55, .84], [.76, .78], [1, .83], [1, 1], [0, 1]], '#d4d8d3'),
    area([[.06, 1], [.19, .91], [.35, .96], [.48, .88], [.61, .96], [.74, .93], [.88, .90], [1, .97], [1, 1]], '#f4f7f6', '#c2cccd'),
    area([[.59, .68], [.70, .77], [.92, .78], [1, .65], [1, .29], [.83, .23], [.69, .29], [.59, .36], [.63, .48]], '#dcc99d'),
    area(boundary, '#a9c9cc'),
    line(river, '#f5f8ee', 21), line(river, '#649ca6', 14),
    area(lake, '#9fcbd0', '#679ba6'),
    '<g fill="none" stroke="#b2b9ad" stroke-width="2" opacity=".6">',
    ...[[[.05,.83],[.15,.89],[.25,.85],[.36,.90]], [[.58,.83],[.66,.92],[.76,.82],[.88,.87]], [[.13,.48],[.23,.51],[.28,.61]], [[.08,.63],[.16,.70],[.23,.73]], [[.67,.64],[.76,.73],[.86,.65],[.93,.70]]].map(p => line(p, '#b2b9ad', 2)), '</g>',
    text(120, 200, '北 N', 'font-size="22" font-weight="700"'),
    text(420, 202, '설산 · 산악 고개', 'class="terrain-label"'),
    text(116, 610, '건조 협곡', 'class="terrain-label"'),
    text(949, 648, '가을 숲', 'class="terrain-label"'),
    text(200, 1080, '강변 초원', 'class="terrain-label"'),
    text(1040, 1130, '해안', 'class="terrain-label"'),
  ];
  let routes = '';
  for (const loop of loops) routes += line(loop.points, '#f8f7f0', 12) + line(loop.points, focus ? '#aba89c' : '#786b54', 6);
  for (const branch of branches) routes += line(branch.points, '#f8f7f0', 10) + line(branch.points, branch.kind === 'main' ? '#786b54' : '#5d7259', 4, branch.kind === 'main' ? '' : '10 8');
  if (focus) {
    routes += line(focus.points, '#fffdf6', 13) + line(focus.points, focus.color, 7);
    for (const fraction of [.18, .48, .78]) {
      const i = Math.min(focus.points.length - 2, Math.floor(focus.points.length * fraction));
      const a = focus.points[i], b = focus.points[i + 1];
      routes += `<polyline points="${pts([[a[0]+(b[0]-a[0])*.3,a[1]+(b[1]-a[1])*.3],[a[0]+(b[0]-a[0])*.7,a[1]+(b[1]-a[1])*.7]])}" fill="none" stroke="${focus.color}" stroke-width="7" marker-end="url(#arrow)"/>`;
    }
  }
  // 하단 광산의 내부 연결을 지상 도로로 오인하지 않도록 마지막에 점선을 겹친다.
  routes += line([uv('Q'), [.20, .60], uv('K')], focus?.id === 'B' ? focus.color : '#786b54', 6, '5 8');
  const markers = Object.entries(nodes).map(([id, n]) => {
    const [x,y] = xy(n.uv), [dx,dy] = n.offset;
    const color = selected.has(id) ? (focus?.color ?? '#354a3f') : '#7a8179';
    const shape = n.kind === 'bridge' ? `<rect x="${x-11}" y="${y-8}" width="22" height="16" rx="2" fill="#fbfaf4" stroke="${color}" stroke-width="3"/>` : `<circle cx="${x}" cy="${y}" r="${n.kind === 'ford' ? 8 : 10}" fill="#fbfaf4" stroke="${color}" stroke-width="3" ${n.kind==='ford' ? 'stroke-dasharray="3 3"' : ''}/>`;
    return shape + text(x+dx, y+dy, `${id} ${n.name}`, `class="node-label" text-anchor="${dx < 0 ? 'end' : 'start'}" fill="${color}"`);
  }).join('');
  const scale = 1040 * 200 / 1536;
  const footer = [
    `<line x1="100" y1="1220" x2="${100+scale}" y2="1220" stroke="#354a3f" stroke-width="5"/>`, text(100, 1250, '200m'),
    '<line x1="320" y1="1220" x2="375" y2="1220" stroke="#786b54" stroke-width="6"/>', text(385,1228,'주요 연결'),
    '<line x1="555" y1="1220" x2="610" y2="1220" stroke="#5d7259" stroke-width="4" stroke-dasharray="10 8"/>', text(620,1228,'선택·구조물 경로'),
    text(100, 1290, focus ? `${focus.caption}` : 'M 거점 · B1/B2 기본 횡단 · F 선택 여울 · Q↔K 광산 내부 연결'),
    text(100, 1324, focus ? `평면 제안 길이 ${(length(focus.points)/1000).toFixed(2)}km · ${focus.note}` : 'A 생활권 / B 협곡 / C 고지대 순환은 개별 도면에서 강조'),
    text(100, 1360, '1,536 × 1,536m · 설계 좌표에 따른 경로 시안 · 실제 지형·경사·통행 검증 전', 'font-size="17" fill="#626b64"'),
  ].join('');
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="1400" viewBox="0 0 1200 1400" role="img" aria-labelledby="title desc"><title id="title">${title}</title><desc id="desc">독립 정규 월드의 지역과 하천, 주요 연결과 선택 경로를 보여주는 제안 도면</desc><defs><marker id="arrow" markerWidth="5" markerHeight="5" refX="4" refY="2.5" orient="auto"><path d="M0,0 L5,2.5 L0,5" fill="${focus?.color ?? '#786b54'}"/></marker></defs><style>text{font-family:'Malgun Gothic','Apple SD Gothic Neo',sans-serif;font-size:20px;fill:#33453b}.terrain-label{font-size:23px;fill:#64715e}.node-label{font-size:19px;font-weight:700;paint-order:stroke;stroke:#faf9f3;stroke-width:5px;stroke-linejoin:round}</style><rect width="1200" height="1400" fill="#faf9f3"/>${text(80,65,title,'font-size="34" font-weight="700"')}${text(80,105,'지역별 경로 확대 설계 · 지형 채색은 이해를 위한 개략 표현','font-size="19" fill="#626b64"')}${background.join('')}${routes}${markers}<rect x="80" y="140" width="1040" height="1040" fill="none" stroke="#aab3a6" stroke-width="2"/>${footer}</svg>`;
}

for (const loop of loops) {
  assert.deepEqual(loop.points[0], loop.points.at(-1), `${loop.id}: 순환 미연결`);
  assert(loop.stops.every(id => nodes[id]), `${loop.id}: 거점 누락`);
}
for (const p of [...river, ...loops.flatMap(l=>l.points), ...branches.flatMap(b=>b.points)]) assert(p.every(n=>Number.isFinite(n)&&n>=0&&n<=1), '좌표 범위 오류');
for (const id of ['B1','B2','B3','F']) assert(river.some(p=>p[0]===uv(id)[0]&&p[1]===uv(id)[1]), `${id}: 하천 횡단 좌표 불일치`);
// 지도에서 선이 이어져 보여도 다리 없는 도하가 생기면 제작 입력으로 쓰지 못한다.
function intersection(a, b, c, d) {
  const rx=b[0]-a[0], ry=b[1]-a[1], sx=d[0]-c[0], sy=d[1]-c[1], det=rx*sy-ry*sx;
  if (Math.abs(det)<1e-10) return null;
  const t=((c[0]-a[0])*sy-(c[1]-a[1])*sx)/det, u=((c[0]-a[0])*ry-(c[1]-a[1])*rx)/det;
  return t>=-1e-8 && t<=1+1e-8 && u>=-1e-8 && u<=1+1e-8 ? [a[0]+t*rx,a[1]+t*ry] : null;
}
for (const route of [...loops, ...branches.filter(b=>b.kind!=='structure')]) {
  for (let i=1;i<route.points.length;i++) for (let j=1;j<river.length;j++) {
    const point=intersection(route.points[i-1],route.points[i],river[j-1],river[j]);
    if (point) assert(['B1','B2','B3','F'].some(id=>Math.hypot(point[0]-uv(id)[0],point[1]-uv(id)[1])<1e-6), `${route.id}: 횡단 시설 없는 하천 교차`);
  }
}
fs.writeFileSync(path.join(output,'Route_Overview.svg'), render());
for (const loop of loops) fs.writeFileSync(path.join(output,loop.filename), render(loop.id));
const manifest = { version: 1, status: 'proposed_not_terrain_validated', mapSizeMeters:1536, axes:{u:'east',v:'north'}, nodes:Object.fromEntries(Object.entries(nodes).map(([id,n])=>[id,{...n,world:world(n.uv)}])), river, loops:loops.map(l=>({...l,planarLengthMeters:length(l.points),worldPoints:l.points.map(world)})), branches, notes:['길이는 XZ 평면의 제안 경로 길이이며 경사·계단·탐험 정지를 포함하지 않는다.','문서와 U,V 좌표를 공유하며 Unity 자동 배치는 수행하지 않았다.'] };
fs.writeFileSync(path.join(output,'route-layout.json'),JSON.stringify(manifest,null,2)+'\n');
console.log(JSON.stringify({files:4,loops:manifest.loops.map(l=>({id:l.id,meters:l.planarLengthMeters})),checks:'순환 폐합·거점·좌표 범위·횡단 노드·무시설 도하 검사 통과'},null,2));
