import fs from 'node:fs';
import crypto from 'node:crypto';
const dir='output/RedForestR1/Integration';
const main='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const parse=s=>new Map([...s.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],text:m[0]}]));
const before=parse(read(`${dir}/LakeVillageWorld.before.unity`)),after=parse(read(main));
const plan=JSON.parse(read(`${dir}/merge-plan.json`));
const allowed=new Set(plan.changes.map(c=>c.id));
const missing=[],unexpected=[];let unchanged=0;
for(const [id,b] of before){const a=after.get(id);if(!a)missing.push(id);else if(a.text===b.text)unchanged++;else if(!allowed.has(id))unexpected.push(id);}
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const sources=JSON.parse(read('output/RedForestR1/source-hashes.json')).filter(v=>!v.Path.endsWith('LakeVillageWorld.unity')).map(v=>({path:v.Path,unchanged:hash(v.Path)===v.Hash}));
const ids=s=>[...s.matchAll(/(?:_guid: |propertyPath: _guid\n      value: )([^\r\n]+)/g)].map(m=>m[1]).sort();
const diskIdsMatch=JSON.stringify(ids(read(`${dir}/LakeVillageWorld.before.unity`)))===JSON.stringify(ids(read(main)));
const report={missing,unexpected,unchanged,changed:before.size-unchanged,added:after.size-before.size,diskIdsMatch,sources,mainSha:hash(main)};
fs.writeFileSync(`${dir}/preservation.json`,JSON.stringify(report,null,2));
console.log(JSON.stringify({...report,sources:sources.length,sourceFailures:sources.filter(v=>!v.unchanged)}));
if(missing.length||unexpected.length||!diskIdsMatch||sources.some(v=>!v.unchanged))process.exitCode=1;
