// 씬 저장이 기존 객체를 제거하거나 환경 보정 범위 밖의 문서를 바꾸지 않았는지 검사한다.
import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const folder='output/OpenWorld/EnvironmentPolish';
function indexScene(path) {
  const text=fs.readFileSync(path,'utf8');
  const headers=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$/gm)];
  const result=new Map();
  for(let i=0;i<headers.length;i++) {
    const h=headers[i],block=text.slice(h.index,i+1<headers.length?headers[i+1].index:text.length);
    result.set(h[2],{type:h[1],hash:crypto.createHash('sha256').update(block).digest('hex'),name:block.match(/^  m_Name: (.*)$/m)?.[1]??'',block});
  }
  return result;
}
const before=indexScene(folder+'/backup/LakeVillageWorld.unity');
const after=indexScene('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity');
const changed=[],removed=[];
for(const [id,b] of before) {
  const a=after.get(id);
  if(!a)removed.push(id);
  else if(a.hash!==b.hash)changed.push({id,type:b.type,name:a.name,old:b.block,current:a.block});
}
assert.equal(removed.length,0,'기존 씬 문서 소실');
const added=[...after.keys()].filter(id=>!before.has(id));
function properties(block) {
  const result=new Map();
  for(const m of block.matchAll(/- target: (.*)\r?\n\s+propertyPath: (.*)\r?\n\s+value: ?(.*)\r?\n\s+objectReference: (.*)/g))result.set(m[1]+'|'+m[2],m[3]+'|'+m[4]);
  return result;
}
const deltas=changed.map(({old,current,...c})=>{
  const b=properties(old),a=properties(current);
  const name=current.match(/propertyPath: m_Name\r?\n\s+value: (.*)/)?.[1]??'';
  return {...c,name:name.startsWith('"')?JSON.parse(name):name,delta:[...new Set([...a.keys(),...b.keys()])].filter(k=>a.get(k)!==b.get(k)).map(k=>({key:k,old:b.get(k),current:a.get(k)}))};
});
fs.writeFileSync(folder+'/scene-diff-summary.json',JSON.stringify(deltas,null,2));
assert.equal(added.length,0,'새 씬 객체가 생성됨');
const automatic=deltas.filter(c=>c.type==='1001'&&c.name.startsWith('Enemy_'));
if(process.argv.includes('--repair-automatic')&&automatic.length){
  for(const c of automatic){
    assert(c.delta.every(d=>d.old===undefined&&/\|m_(LocalPosition|LocalRotation|RotationOffset|TranslationOffset)/.test(d.key)),'적의 기존 사용자 변경을 되돌릴 수 없습니다.');
    const a=changed.find(x=>x.id===c.id);
    const structure=b=>b.replace(/    m_Modifications:[\s\S]*?    m_RemovedComponents:/,'    m_Modifications:\n    m_RemovedComponents:');
    assert.equal(structure(a.old),structure(a.current),'적의 비자동 구조 변경');
  }
  let scene=fs.readFileSync('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity','utf8');
  for(const c of automatic){const a=changed.find(x=>x.id===c.id);scene=scene.replace(a.current,a.old);}
  fs.writeFileSync('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity',scene);
  fs.writeFileSync(folder+'/automatic-serialization-restored.json',JSON.stringify(automatic,null,2));
  console.log('자동 Constraint 직렬화 복원='+automatic.length+'; 다시 검사를 실행하세요.');
  process.exit(0);
}
// 프리팹 오버라이드에 저장된 바위 높이/접지 48개, 터널 메시 2개, NavMeshSurface 1개만 허용한다.
const rockPrefixes=['Heart_ShoreRock_','Stone_ShoreRock_','Shrine_ShoreRock_','Northwest_Garden_','Southeast_Garden_','Quality_Lake_RockCluster_'];
for(const c of deltas){
  assert(['1001','114'].includes(c.type),'예상 밖 문서 종류: '+c.type);
  if(c.type==='114'){
    const a=changed.find(x=>x.id===c.id);
    const withoutNav=b=>b.replace(/  m_NavMeshData: .*\r?\n/,'');
    assert.equal(withoutNav(a.old),withoutNav(a.current),'NavMesh 이외의 컴포넌트 변경');
    continue;
  }
  const rock=rockPrefixes.some(prefix=>c.name.startsWith(prefix));
  const tunnel=['여왕_동굴_터널_R2','거미_동굴_터널_R2'].includes(c.name);
  assert(rock||tunnel,'예상 밖 프리팹 변경: '+c.name);
  assert(c.delta.every(d=>rock?/\|m_Local(Scale|Position)\.y$/.test(d.key):/\|m_Mesh$/.test(d.key)),'예상 밖 변경 속성: '+c.name);
}
assert.equal(deltas.filter(c=>rockPrefixes.some(p=>c.name.startsWith(p))).length,48,'바위 수');
assert.equal(deltas.filter(c=>c.type==='1001'&&!rockPrefixes.some(p=>c.name.startsWith(p))).length,2,'터널 수');
assert.equal(deltas.filter(c=>c.type==='114').length,1,'NavMeshSurface 수');
assert.equal(fs.readFileSync(folder+'/identities-before.txt','utf8'),fs.readFileSync(folder+'/identities-after.txt','utf8'),'진행 ID 또는 좌표 변경');
const report={existingDocuments:before.size,addedDocuments:added.length,removed,changed:changed.map(({old,current,...c})=>c)};
fs.writeFileSync(folder+'/scene-preservation.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({existingDocuments:before.size,addedDocuments:added.length,removedDocuments:removed.length,changedDocuments:changed.length}));
