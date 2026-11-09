import fs from 'node:fs';
import crypto from 'node:crypto';
const out='output/ShadeCreekR1';
const load=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const source=load(`${out}/source-hashes.json`);
const preserved=source.map(r=>({...r,unchanged:hash(r.path)===r.hash}));
const before=load(`${out}/contracts-before.json`),after=load(`${out}/contracts-after.json`);
const key=r=>JSON.stringify([r.id,r.type,r.position]);
const byId=new Map(after.map(r=>[key(r),r]));
const normalize=s=>{const o=JSON.parse(s);if(o.MonoBehaviour?._ownerKey)o.MonoBehaviour._ownerKey=o.MonoBehaviour._ownerKey.replace(/(GlobalObjectId_V1-\d+-)[a-f0-9]+/,'$1SCENE');return JSON.stringify(o);};
const changed=[],missing=[];
for(const r of before){const v=byId.get(key(r));if(!v){missing.push(r);continue;}if(normalize(r.json)!==normalize(v.json)||JSON.stringify(r.position)!==JSON.stringify(v.position))changed.push({id:r.id,type:r.type,before:r,after:v});}
const report={sourceFiles:preserved,sourceChanges:preserved.filter(x=>!x.unchanged),originalContractCount:before.length,missing,changed,allowedContractChange:'NavMeshSurface 전용 데이터 참조만 허용. SceneEntityId ownerKey의 씬 GUID만 정규화',unapproved:changed.filter(x=>!x.type.endsWith('.NavMeshSurface'))};
const sourceText=fs.readFileSync(`${out}/Source.before.unity`,'utf8'),reviewText=fs.readFileSync('Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity','utf8');
const blocks=s=>new Map([...s.matchAll(/^--- !u!\d+ &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[1],m[2]]));
const originalBlocks=blocks(sourceText),reviewBlocks=blocks(reviewText);
report.existingParticleRenderers=['6968311534778827825','6968311534221124968'].map(id=>({id,found:originalBlocks.has(id)&&reviewBlocks.has(id),unchanged:originalBlocks.has(id)&&originalBlocks.get(id)===reviewBlocks.get(id)}));
report.rainPrefabUnchanged='Rain의 빈 Renderer 재질은 기존 공용 LakeOfLifeEnvironment.prefab에서도 별도 확인. 이번 저작 대상 밖이며 수정하지 않음';
fs.writeFileSync(`${out}/preservation.json`,JSON.stringify(report,null,2));
console.log(JSON.stringify({sourceFiles:preserved.length,sourceChanges:report.sourceChanges.length,contracts:before.length,missing:missing.length,changed:changed.slice(0,8).map(x=>({id:x.id,type:x.type})),unapproved:report.unapproved.length}));
if(report.sourceChanges.length||missing.length||report.unapproved.length)process.exitCode=1;

