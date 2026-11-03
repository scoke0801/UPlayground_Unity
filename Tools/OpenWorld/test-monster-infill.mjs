// 숲 보강 입력의 영역별 누락과 기존 배치 ID 손상을 막는다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const infill=read('Tools/OpenWorld/monster-infill-resolved.json');
const baseline=read('output/OpenWorld/MonsterPopulation/Infill/population.before.json');
const current=read('Tools/OpenWorld/monster-population-resolved.json');
const inside=(m,poly)=>{let result=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [x,z]=poly[i],[px,pz]=poly[j];if((z>m.z)!==(pz>m.z)&&m.x<(px-x)*(m.z-z)/(pz-z)+x)result=!result;}return result;};
const members=infill.groups.flatMap(g=>g.members);
assert.equal(infill.zones.length,6);
assert.equal(new Set(members.map(m=>m.guid)).size,members.length);
for(const zone of infill.zones){const groups=infill.groups.filter(g=>g.zone===zone.id);assert(groups.length>=8,zone.name+' 조우 부족');for(const g of groups)for(const m of g.members){assert(inside(m,zone.polygon),g.id+' 영역 이탈');const distance=Math.hypot(m.x-baseline.start.x,m.z-baseline.start.z);const band=baseline.bands.slice().sort((a,b)=>a.radius-b.radius).find(b=>distance<b.radius);assert.equal(m.level,band.level,g.id+' 레벨');for(const r of baseline.safeRegions)assert(!(m.x>r.x-30&&m.x<r.x+r.width+30&&m.z>r.y-30&&m.z<r.y+r.height+30),g.id+' 도시 안전 영역');}}
const old=new Map(current.groups.map(g=>[g.id,g]));for(const group of baseline.groups)assert.deepEqual(old.get(group.id),group,'기존 조우 변경: '+group.id);
assert.equal(baseline.groups.length,57);
assert.equal(baseline.groups.reduce((n,g)=>n+g.members.length,0),140);
const report='후보 검증 통과: '+infill.groups.length+'개 조우·'+members.length+'마리 / 6개 영역 모두 8개 이상 조우 / 영역·레벨·도시 안전 폭·GUID 중복·기존 57개 조우 보존. Unity 실제 저장 여부는 별도 확인.\n';
fs.writeFileSync('output/OpenWorld/MonsterPopulation/Infill/plan-check.txt',report);console.log(report);
