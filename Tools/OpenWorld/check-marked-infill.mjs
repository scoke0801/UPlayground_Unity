import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const folder=process.argv[2]??'output/OpenWorld/MarkedInfill';
const scene='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
function indexScene(path) {
  const text=fs.readFileSync(path,'utf8');
  const headers=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$/gm)];
  const result=new Map();
  for(let i=0;i<headers.length;i++) {
    const h=headers[i],block=text.slice(h.index,i+1<headers.length?headers[i+1].index:text.length);
    result.set(h[2],{type:h[1],hash:crypto.createHash('sha256').update(block).digest('hex'),name:block.match(/^  m_Name: (.*)$/m)?.[1]??''});
  }
  return result;
}
const before=indexScene(folder+'/backup/LakeVillageWorld.unity');
const after=indexScene(scene);
const changed=[],removed=[];
for(const [id,b] of before) {
  const a=after.get(id);
  if(!a)removed.push({id,...b});
  else if(a.hash!==b.hash)changed.push({id,type:b.type,name:b.name});
}
const added=[...after.keys()].filter(id=>!before.has(id));
assert.equal(removed.length,0,'기존 씬 개체가 제거되었습니다.');
// 기존 월드 부모 Transform의 자식 연결과 NavMeshSurface 참조만 바뀌어야 한다.
assert.equal(changed.length,2,'예상하지 않은 기존 씬 문서 변경');
assert.deepEqual(changed.map(c=>c.type).sort(),['114','4']);
assert.equal(fs.readFileSync(folder+'/identities-before.txt','utf8'),fs.readFileSync(folder+'/identities-after.txt','utf8'));
const report={existingDocuments:before.size,addedDocuments:added.length,removed,changed};
fs.writeFileSync(folder+'/scene-preservation.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report));
