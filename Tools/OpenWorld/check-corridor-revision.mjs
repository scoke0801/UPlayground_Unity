import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const folder='output/OpenWorld/DungeonWallRevision';
function index(path){
 const text=fs.readFileSync(path,'utf8'),headers=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$/gm)],map=new Map();
 for(let i=0;i<headers.length;i++){
  const h=headers[i],block=text.slice(h.index,headers[i+1]?.index??text.length);
  map.set(h[2],{type:h[1],hash:crypto.createHash('sha256').update(block).digest('hex'),block});
 }
 return map;
}
const before=index(folder+'/backup/LakeVillageWorld.before.unity');
const after=index('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity');
const removed=[],changed=[],added=[];
for(const [id,b] of before){const a=after.get(id);if(!a)removed.push({id,type:b.type,block:b.block});else if(a.hash!==b.hash)changed.push({id,type:b.type,old:b.block,current:a.block});}
for(const [id,a] of after)if(!before.has(id))added.push({id,type:a.type});
const names=new Set(fs.readFileSync(folder+'/removed.txt','utf8').trim().split(/\r?\n/));
const removedPrefabs=removed.filter(r=>r.type==='1001');
assert.equal(removedPrefabs.length,191);
for(const c of changed){
 if(c.type==='4'){assert(['655333175','1691330124'].includes(c.id),'예상 밖 Transform 변경 '+c.id);continue;}
 assert.equal(c.type,'114','예상 밖 기존 문서 변경');
 assert(c.current.includes('Unity.AI.Navigation::Unity.AI.Navigation.NavMeshSurface'));
 const withoutNav=s=>s.replace(/  m_NavMeshData: .*\r?\n/,'');
 assert.equal(withoutNav(c.old),withoutNav(c.current),'NavMesh 연결 이외의 설정 변경');
}
for(const r of removedPrefabs){const name=r.block.match(/propertyPath: m_Name\r?\n\s+value: (.*)/)?.[1];assert(names.has(name),'예상 밖 프리팹 제거 '+name);}
// 씬 스크립트가 제거된 장식 객체를 참조하지 않는지 저장본 기준으로 검사한다.
const removedIds=new Set(removed.map(r=>r.id));
for(const [id,a] of after)if(a.type==='114')for(const m of a.block.matchAll(/\{fileID: (-?\d+)\}/g))assert(!removedIds.has(m[1]),'스크립트 참조 소실 '+id+' → '+m[1]);
const summary={removed:removed.length,removedPrefabs:removedPrefabs.length,added:added.length,changed:changed.map(c=>({id:c.id,type:c.type,name:c.current.match(/  m_Name: (.*)/)?.[1]??c.current.match(/propertyPath: m_Name\r?\n\s+value: (.*)/)?.[1]??'',old:c.old,current:c.current}))};
fs.writeFileSync(folder+'/scene-diff.json',JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify({...summary,changed:summary.changed.map(({old,current,...r})=>r)}));
