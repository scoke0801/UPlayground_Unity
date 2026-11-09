import fs from 'node:fs';
import assert from 'node:assert/strict';

// 현재 씬에만 적용하는 증분 저작. 이전 전체 월드 생성기는 실행하지 않는다.
const old = JSON.parse(fs.readFileSync('Tools/OpenWorld/dungeon-art-plan.json'));
const survey = JSON.parse(fs.readFileSync('output/OpenWorld/DungeonContinuity/before.json'));
const sites = old.sites.filter(s => ['quarry','crypt','fort','waystation','chain'].includes(s.id));
const runs = [], gates = [];
const run = (s,a,b,height=5) => runs.push({site:s.id,a:[s.x+a[0],s.z+a[1]],b:[s.x+b[0],s.z+b[1]],height});
const gate = (s,x,z,yaw=0,scale=1.5) => gates.push({site:s.id,x:s.x+x,z:s.z+z,yaw,scale});
function box(s,x0,z0,x1,z1,height=5) {
  run(s,[x0,z0],[x1,z0],height); run(s,[x1,z0],[x1,z1],height);
  run(s,[x1,z1],[x0,z1],height); run(s,[x0,z1],[x0,z0],height);
}
const q=sites.find(s=>s.id==='quarry');
box(q,-40,-20,32,28,6); box(q,-30,12,-6,28,6); box(q,12,4,32,28);
gate(q,-18,12); gate(q,22,4); gate(q,0,-20,0,2);
const c=sites.find(s=>s.id==='crypt');
box(c,-48,-35,48,42,6); gate(c,0,-35,0,2); gate(c,48,-15,90,2);
for(const side of [-1,1])for(const z of [-22,5,30]) {
  const x=side*35; box(c,x-10,z-9,x+10,z+9); gate(c,x,z-9);
}
const f=sites.find(s=>s.id==='fort');
box(f,-42,-34,42,34,6); gate(f,0,-34,0,2.2); gate(f,0,34,0,2);
for(const x of [-22,22]) { box(f,x-8,-8,x+8,18); gate(f,x,-8); }
const w=sites.find(s=>s.id==='waystation');
box(w,-44,-30,44,34,6); gate(w,0,-30,0,2.2); gate(w,0,34,0,2);
for(const side of [-1,1]) { const x=side*32; box(w,x-8,-19,x+8,21); gate(w,x-side*8,-4,90); }
box(w,-10,22,10,38); gate(w,0,22);

// 사슬 제단의 기존 중앙 기둥·사슬은 유지하고, 전실과 외벽으로 규모를 확장한다.
const t=sites.find(s=>s.id==='chain');
box(t,-38,-40,38,38,8); gate(t,0,-40,0,2.4); gate(t,38,0,90,2);
run(t,[-38,-18],[-14,-18],7);run(t,[14,-18],[38,-18],7);
run(t,[-14,-40],[-14,-18],7);run(t,[14,-40],[14,-18],7);
run(t,[-14,-18],[14,-18],7);gate(t,0,-18,0,2);

// 기존 이동선과 새 벽이 만나는 곳은 벽을 생략하지 않고 문으로 연결한다.
for(const r of runs) {
  const s=sites.find(s=>s.id===r.site), dx=r.b[0]-r.a[0], dz=r.b[1]-r.a[1];
  for(const route of s.routes)for(let i=1;i<route.points.length;i++) {
    const p=route.points[i-1], e=route.points[i], ex=e[0]-p[0],ez=e[1]-p[1];
    const cross=dx*ez-dz*ex;if(Math.abs(cross)<.001)continue;
    const px=p[0]-r.a[0],pz=p[1]-r.a[1];
    const u=(px*ez-pz*ex)/cross,v=(px*dz-pz*dx)/cross;
    if(u<=.08||u>=.92||v<0||v>1)continue;
    const x=r.a[0]+u*dx,z=r.a[1]+u*dz;
    if(!gates.some(g=>g.site===s.id&&Math.hypot(g.x-x,g.z-z)<10))
      gates.push({site:s.id,x,z,yaw:Math.abs(dx)>.1?0:90,scale:2});
  }
}

// 옛 회랑은 바닥 셀의 합집합 경계를 사용하여 바깥 순환길까지 빠짐없이 감싼다.
const corridor={id:'corridor',name:'옛 회랑',parent:'AuthoredEnvironment/월드 구간/던전/옛 회랑'};
sites.push(corridor);
const floors=survey.filter(r=>r.prefab.endsWith('/Dng_Floor_4x4.prefab'));
const cells=new Map(floors.map(r=>[`${Math.round((r.min[0]+r.max[0])/2)},${Math.round((r.min[2]+r.max[2])/2)}`,r]));
for(const [key,r] of cells) {
  const [x,z]=key.split(',').map(Number);
  for(const [nx,nz,a,b] of [[x-4,z,[x-2,z-2],[x-2,z+2]],[x+4,z,[x+2,z-2],[x+2,z+2]],
    [x,z-4,[x-2,z-2],[x+2,z-2]],[x,z+4,[x-2,z+2],[x+2,z+2]]]) {
    if(cells.has(`${nx},${nz}`))continue;
    runs.push({site:'corridor',a,b,height:5,base:r.max[1]-.04});
  }
}
// 남쪽 진입문, 각 방의 석문. 바깥 동선의 양끝은 방으로 연결된다.
for(const [x,z,yaw,scale] of [[650,492,0,1.7],[650,520,0,1.7],[650,574,0,1.7],
  [662,588,90,1.7],[650,548,0,1.7],[650,560,0,1.7]]) gates.push({site:'corridor',x,z,yaw,scale,base:100});

const rewards=[
  {site:'corridor',x:610,z:552,base:104.04,item:'rare_hp_potion',count:3},
  {site:'quarry',x:566,z:1111,item:'rare_hp_potion',count:3},
  {site:'crypt',x:1525,z:1107,item:'companion_exp_potion',count:2},
  {site:'fort',x:1865,z:629,item:'rare_hp_potion',count:4},
  {site:'waystation',x:2031,z:208,item:'companion_exp_potion',count:2},
  {site:'chain',x:590,z:887,item:'rare_hp_potion',count:5},
];
rewards.forEach((r,i)=>r.guid=`d08e65f39a2342daae0e73d8b7a1${String(i+1).padStart(4,'0')}`);
// 방의 뒷벽과 외곽벽이 겹치는 구간은 한 벽으로 합친다.
const lines=new Map();
for(const r of runs) {
  const horizontal=Math.abs(r.a[1]-r.b[1])<.01;
  const key=[r.site,horizontal,horizontal?r.a[1]:r.a[0],r.base??'terrain'].join('|');
  const row={...r,lo:Math.min(r.a[horizontal?0:1],r.b[horizontal?0:1]),hi:Math.max(r.a[horizontal?0:1],r.b[horizontal?0:1]),horizontal};
  if(!lines.has(key))lines.set(key,[]);lines.get(key).push(row);
}
const merged=[];
for(const rows of lines.values()) {
  const ends=[...new Set(rows.flatMap(r=>[r.lo,r.hi]))].sort((a,b)=>a-b);
  for(let i=1;i<ends.length;i++) {
    const lo=ends[i-1],hi=ends[i],mid=(lo+hi)/2;
    const covering=rows.filter(r=>r.lo<=mid&&r.hi>=mid);if(!covering.length)continue;
    const r=covering.reduce((a,b)=>a.height>b.height?a:b);
    merged.push({...r,a:r.horizontal?[lo,r.a[1]]:[r.a[0],lo],b:r.horizontal?[hi,r.a[1]]:[r.a[0],hi]});
  }
}
assert.equal(new Set(rewards.map(r=>r.guid)).size,6);
for(const r of runs)assert(Math.hypot(r.a[0]-r.b[0],r.a[1]-r.b[1])>0);
for(const g of gates)assert(g.scale>=1.25);
const plan={version:1,sites,runs:merged,gates,rewards,chainScale:1.35};
fs.writeFileSync('Tools/OpenWorld/dungeon-continuity-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({sites:sites.length,runs:merged.length,gates:gates.length,rewards:rewards.length}));
