import fs from 'node:fs';
import crypto from 'node:crypto';
const main='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const review='Assets/01.Scenes/ArtReview/WorldThemes/VillageConnectionR1.unity';
const blocks=s=>new Map([...s.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],body:m[3]}]));
const a=blocks(fs.readFileSync(main,'utf8')),b=blocks(fs.readFileSync(review,'utf8'));
const missing=[],changed=[];
for(const [id,v] of a){const w=b.get(id);if(!w){missing.push(id);continue;}if(v.body.replace(/(_ownerKey: GlobalObjectId_V1-\d+-)[a-f0-9]+/g,'$1SCENE')!==w.body.replace(/(_ownerKey: GlobalObjectId_V1-\d+-)[a-f0-9]+/g,'$1SCENE'))changed.push({id,type:v.type,before:v.body.slice(0,200),after:w.body.slice(0,200)});}
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const source=JSON.parse(fs.readFileSync('output/VillageConnectionR1/source-hashes.json','utf8').replace(/^\uFEFF/,''));
const report={missing,changed,newBlocks:[...b.keys()].filter(k=>!a.has(k)).length,sourcePreserved:source.map(v=>({path:v.Path,unchanged:hash(v.Path)===v.Hash}))};
fs.writeFileSync('output/VillageConnectionR1/scene-diff.json',JSON.stringify(report,null,2));console.log(JSON.stringify({missing:missing.length,changed:changed.map(v=>({id:v.id,type:v.type})),newBlocks:report.newBlocks,sourcePreserved:report.sourcePreserved}));

