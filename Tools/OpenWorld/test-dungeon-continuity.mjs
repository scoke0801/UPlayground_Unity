import fs from 'node:fs';
import assert from 'node:assert/strict';
import {execFileSync} from 'node:child_process';

const path='Tools/OpenWorld/dungeon-continuity-plan.json';
const baseline=fs.readFileSync(path,'utf8');
execFileSync(process.execPath,['Tools/OpenWorld/plan-dungeon-continuity.mjs']);
assert.equal(fs.readFileSync(path,'utf8'),baseline,'같은 입력으로 배치가 달라지면 안 된다');
const p=JSON.parse(baseline);
assert.equal(p.sites.length,6);assert.equal(p.rewards.length,6);
assert.equal(new Set(p.rewards.map(r=>r.guid)).size,6);
for(const r of p.rewards) {
  assert.match(r.guid,/^[0-9a-f]{32}$/);assert(r.count>0);
  assert(fs.existsSync(`Assets/10.Datas/Item/Consume/${r.item}.asset`));
}
for(const r of p.runs) {
  assert.equal(r.a[0]===r.b[0]||r.a[1]===r.b[1],true);
  assert(r.height>=5);assert(r.a.every(Number.isFinite)&&r.b.every(Number.isFinite));
}
// 한 직선의 중복 구간은 벽 깜빡임과 이중 충돌을 만들므로 차단한다.
for(let i=0;i<p.runs.length;i++)for(let j=i+1;j<p.runs.length;j++) {
  const a=p.runs[i],b=p.runs[j];if(a.site!==b.site||a.base!==b.base)continue;
  const h=a.a[1]===a.b[1];if(h!==(b.a[1]===b.b[1]))continue;
  const fixed=h?1:0,axis=h?0:1;if(a.a[fixed]!==b.a[fixed])continue;
  const overlap=Math.min(Math.max(a.a[axis],a.b[axis]),Math.max(b.a[axis],b.b[axis]))-
    Math.max(Math.min(a.a[axis],a.b[axis]),Math.min(b.a[axis],b.b[axis]));
  assert(overlap<.001,`벽 중복: ${i},${j}`);
}
const corridor=p.runs.filter(r=>r.site==='corridor');
assert(corridor.some(r=>r.a[0]===686&&r.b[0]===686),'바깥 동쪽 길의 외벽 누락');
assert(corridor.some(r=>r.a[1]===500&&r.b[1]===500&&r.b[0]>660),'바깥 남쪽 길의 외벽 누락');
for(const g of p.gates)assert(g.scale>=1.25);
console.log(JSON.stringify({result:'PASS',scope:'배치 입력 정적 보조 검사',runs:p.runs.length,gates:p.gates.length,rewards:p.rewards.length,unityLoad:'미수행'}));

