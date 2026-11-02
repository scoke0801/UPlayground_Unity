import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';

const folder='output/OpenWorld/DistrictInfill';
fs.mkdirSync(folder,{recursive:true});
const current=JSON.parse(fs.readFileSync('Tools/OpenWorld/district-infill-plan.json','utf8'));
const previous=JSON.parse(fs.readFileSync(`${folder}/plan-before-resume.json`,'utf8'));
const repair=JSON.parse(fs.readFileSync('Tools/OpenWorld/district-route-repair.json','utf8'));
for(const key of ['regions','placements','fields','patches','cropPath'])assert.deepEqual(current[key],previous[key],`${key} 변경`);
assert.deepEqual(current.routes,repair.routes);
assert.deepEqual(current.reservedRoutes.slice(0,-current.routes.length),previous.reservedRoutes.slice(0,-previous.routes.length));
assert.deepEqual(current.reservedRoutes.slice(-current.routes.length),current.routes);
const output=`${folder}/reproduced-plan.json`;
try {
  const result=spawnSync(process.execPath,['Tools/OpenWorld/plan-district-infill.mjs',output],{encoding:'utf8'});
  assert.equal(result.status,0,result.stderr);
  assert.equal(fs.readFileSync(output,'utf8'),fs.readFileSync('Tools/OpenWorld/district-infill-plan.json','utf8'),'저작 입력 재현 실패');
} finally {
  if(fs.existsSync(output))fs.unlinkSync(output);
}
const rows=fs.readFileSync(`${folder}/placements.csv`,'utf8').trim().split(/\r?\n/).slice(1).map(line=>line.split(','));
const skipped=fs.readFileSync(`${folder}/skipped.csv`,'utf8').trim().split(/\r?\n/).slice(1).map(line=>line.split(',')[0]);
assert.equal(rows.length+skipped.length,current.placements.length,'계획·적용·제외 수량 불일치');
const applied=new Set(rows.map(r=>r[0]));
assert.equal(applied.size,rows.length,'적용 ID 중복');
for(const p of current.placements)assert(applied.has(p.id)!==skipped.includes(p.id),`적용·제외 분류 오류: ${p.id}`);
const stats={applied:rows.length,skipped:skipped.length,byKind:{},byRegion:{}};
for(const row of rows){stats.byKind[row[5]]=(stats.byKind[row[5]]??0)+1;stats.byRegion[row[1]]=(stats.byRegion[row[1]]??0)+1;}
stats.planSha256=crypto.createHash('sha256').update(fs.readFileSync('Tools/OpenWorld/district-infill-plan.json')).digest('hex');
fs.writeFileSync(`${folder}/input-validation.json`,JSON.stringify(stats,null,2)+'\n');
console.log(JSON.stringify(stats));
