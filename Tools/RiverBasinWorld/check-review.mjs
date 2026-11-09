import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const output='output/RiverBasinWorld';
const plan=JSON.parse(fs.readFileSync('Tools/RiverBasinWorld/production-plan.json'));
const audit=JSON.parse(fs.readFileSync(`${output}/audit.json`));
for(const r of plan.routes)assert.deepEqual(r.points[0],r.points.at(-1),`${r.id} 순환 미연결`);
assert(plan.river.slice(1).every((p,i)=>p[1]<=plan.river[i][1]),'하류 역수위');
assert(plan.detail.planarLength>=300&&plan.detail.planarLength<=400);
assert.equal(audit.terrainCount,9);assert.equal(audit.seamError,0);assert.equal(audit.missingScripts,0);
assert(audit.routes.every(r=>r.unsupported===0&&r.blocked===0&&r.navFailures===0));
assert.equal(audit.invalidMaterialRenderers.length,0);
assert(audit.structures.every(r=>r.matched));
assert(JSON.parse(fs.readFileSync(`${output}/preservation.json`)).every(r=>r.preserved));
assert(JSON.parse(fs.readFileSync(`${output}/owned-assets.json`)).every(r=>r.hasMeta));
let links=0;for(const file of [`${output}/RESULT.html`,`${output}/TEST_SPEC.html`,'Tools/RiverBasinWorld/README.html']){
 const text=fs.readFileSync(file,'utf8');for(const [,url] of text.matchAll(/(?:href|src)="([^"]+)"/g)){if(url.startsWith('#')||url.includes('://'))continue;assert(fs.existsSync(path.resolve(path.dirname(file),url)),`${file} 누락 링크: ${url}`);links++;}
}
console.log({result:'PASS',links,routes:audit.routes.length,artAcceptance:'대기 / NO-SHIP'});
