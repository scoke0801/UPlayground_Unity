// 작업 직전 씬과 문서 단위 비교. 자동 Constraint 추가만 검증 후 선택 복원한다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const folder='output/OpenWorld/EnvironmentFollowup';
const scenePath='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
function index(path){const text=fs.readFileSync(path,'utf8');const hs=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$/gm)];return new Map(hs.map((h,i)=>[h[2],{type:h[1],block:text.slice(h.index,hs[i+1]?.index??text.length)}]));}
const before=index(folder+'/backup/'+scenePath),after=index(scenePath);
const changed=[...before].filter(([id,b])=>after.has(id)&&after.get(id).block!==b.block).map(([id,b])=>({id,type:b.type,old:b.block,current:after.get(id).block}));
const removed=[...before.keys()].filter(id=>!after.has(id));assert.equal(removed.length,0,'기존 문서 삭제');
function props(block){return new Map([...block.matchAll(/- target: (.*)\r?\n\s+propertyPath: (.*)\r?\n\s+value: ?(.*)\r?\n\s+objectReference: (.*)/g)].map(m=>[m[1]+'|'+m[2],m[3]+'|'+m[4]]));}
const deltas=changed.map(c=>{const b=props(c.old),a=props(c.current);const raw=c.current.match(/propertyPath: m_Name\r?\n\s+value: (.*)/)?.[1]??'';return {...c,name:raw.startsWith('"')?JSON.parse(raw):raw,delta:[...new Set([...a.keys(),...b.keys()])].filter(k=>a.get(k)!==b.get(k)).map(key=>({key,old:b.get(key),current:a.get(key)}))};});
fs.writeFileSync(folder+'/scene-diff.json',JSON.stringify(deltas.map(({old,current,...d})=>d),null,2));
const automatic=deltas.filter(c=>c.type==='1001'&&c.name.startsWith('Enemy_'));
if(process.argv.includes('--repair-automatic')&&automatic.length){
 for(const c of automatic){assert(c.delta.every(d=>d.old===undefined&&/\|m_(LocalPosition|LocalRotation|RotationOffset|TranslationOffset)/.test(d.key)),'기존 적 속성 보존 실패');const structure=b=>b.replace(/    m_Modifications:[\s\S]*?    m_RemovedComponents:/,'    m_Modifications:\n    m_RemovedComponents:');assert.equal(structure(c.old),structure(c.current),'적 구조 변경');}
 let scene=fs.readFileSync(scenePath,'utf8');for(const c of automatic)scene=scene.replace(c.current,c.old);fs.writeFileSync(scenePath,scene);fs.writeFileSync(folder+'/automatic-serialization-restored.json',JSON.stringify(automatic.map(({old,current,...d})=>d),null,2));console.log('자동 Constraint 복원='+automatic.length);process.exit(0);
}
const moved=new Set(fs.readFileSync(folder+'/moved-transforms.txt','utf8').trim().split(/\r?\n/).map(s=>s.split('-').at(-2)));
const envRoot=[...before].find(([,b])=>b.type==='1'&&/^  m_Name: AuthoredEnvironment\r?$/m.test(b.block))[0];
const rootTransform=[...before].find(([,b])=>b.type==='4'&&b.block.includes(`m_GameObject: {fileID: ${envRoot}}`))[0];
for(const c of deltas){
 if(c.type==='1001'){assert(moved.has(c.id),'비대상 프리팹 변경 '+c.name+' '+c.id);assert(c.delta.every(d=>/\|m_Local(Position|Rotation)\.[xyzw]$|\|m_LocalEulerAnglesHint\.[xyz]$/.test(d.key)),'이동 외 속성 변경 '+c.name);}
 else if(c.type==='114'){const strip=b=>b.replace(/  m_NavMeshData: .*\r?\n/,'');assert.equal(strip(c.old),strip(c.current),'NavMesh 이외 컴포넌트 변경');}
 else {assert.equal(c.id,rootTransform,'비대상 문서 변경 '+c.id);const strip=b=>b.replace(/  m_Children:[\s\S]*?  m_Father:/,'  m_Children:\n  m_Father:');assert.equal(strip(c.old),strip(c.current),'환경 루트 자식 외 변경');}
}
assert.equal(deltas.filter(c=>c.type==='1001').length,37,'기존 소품 수정 수');
assert.equal(fs.readFileSync(folder+'/identities-before.txt','utf8'),fs.readFileSync(folder+'/identities-after.txt','utf8'),'진행 ID/좌표 변경');
const added=[...after.keys()].filter(id=>!before.has(id));
const report={existingDocuments:before.size,removedDocuments:removed.length,changedDocuments:changed.length,addedDocuments:added.length,movedProps:moved.size,changed: deltas.map(({old,current,...d})=>d)};
fs.writeFileSync(folder+'/scene-preservation.json',JSON.stringify(report,null,2));console.log(JSON.stringify({...report,changed:undefined}));
