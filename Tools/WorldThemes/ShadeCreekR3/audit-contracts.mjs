import fs from 'node:fs';
const load=p=>JSON.parse(fs.readFileSync(`output/ShadeCreekR3/${p}.json`,'utf8').replace(/^\uFEFF/,''));
const before=load('contracts-current-canonical'),after=load('contracts-after');
const key=r=>JSON.stringify([r.id,r.type,r.position]);
const normalize=s=>{const o=JSON.parse(s);if(o.MonoBehaviour?._ownerKey)o.MonoBehaviour._ownerKey=o.MonoBehaviour._ownerKey.replace(/(GlobalObjectId_V1-\d+-)[a-f0-9]+/,'$1SCENE');return JSON.stringify(o);};
const index=new Map(after.map(r=>[key(r),r]));const missing=[],changed=[];
for(const r of before){const candidate=index.get(key(r));if(!candidate)missing.push(r);else if(normalize(r.json)!==normalize(candidate.json))changed.push({id:r.id,type:r.type,before:r,after:candidate});}
const report={count:before.length,missing,changed,unapproved:changed.filter(c=>!c.type.endsWith('.NavMeshSurface')),scope:'현재 코드로 로드한 정규 씬과 R3 계약 비교. owner 씬 GUID만 정규화; 전용 NavMesh 참조 변경은 별도 허용'};
fs.writeFileSync('output/ShadeCreekR3/contracts-preservation.json',JSON.stringify(report,null,2));console.log({count:report.count,missing:missing.length,changed:changed.map(c=>c.type),unapproved:report.unapproved.length});
