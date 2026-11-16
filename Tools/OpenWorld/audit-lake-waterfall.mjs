// Unity 연결 종료 후에도 저장본과 미저장 내용 포함 백업의 차이를 확인한다.
import fs from 'node:fs';
import crypto from 'node:crypto';
const beforePath = 'output/lake-waterfall/backup/LakeVillageWorld.before.unity';
const scenePath = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
async function documents(path) {
  const docs = new Map();
  let pending = '';
  const consume = block => {
    const header = block.match(/^--- !u!(\d+) &(-?\d+)/);
    if (!header) return;
    docs.set(header[2], {type:Number(header[1]),hash:crypto.createHash('sha256').update(block).digest('hex'),sample:['174632','10641126','10641124'].includes(header[2])?block:undefined,name:block.match(/^  m_Name: (.*)$/m)?.[1], gameObject:block.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1],position:block.match(/^  m_LocalPosition: (.*)$/m)?.[1],scale:block.match(/^  m_LocalScale: (.*)$/m)?.[1]});
  };
  for await (const chunk of fs.createReadStream(path,{encoding:'utf8'})) {
    pending += chunk;
    let index;
    while ((index = pending.indexOf('\n--- !u!',1)) >= 0) {
      consume(pending.slice(0,index+1));
      pending = pending.slice(index+1);
    }
    if (!pending.startsWith('---') && pending.includes('--- !u!')) pending=pending.slice(pending.indexOf('--- !u!'));
  }
  consume(pending);
  return docs;
}
const before=await documents(beforePath),after=await documents(scenePath);
const removed=[...before.keys()].filter(id=>!after.has(id));
const changed=[...before.entries()].filter(([id,doc])=>after.has(id)&&doc.hash!==after.get(id).hash).map(([id,doc])=>({id,type:doc.type,name:doc.name??after.get(doc.gameObject)?.name,beforePosition:doc.position,afterPosition:after.get(id).position,beforeScale:doc.scale,afterScale:after.get(id).scale}));
const added=[...after.entries()].filter(([id])=>!before.has(id));
const assets=fs.readdirSync('Assets/10.Datas/World/OpenWorld/LakeWaterfall').sort();
const report={beforeDocuments:before.size,afterDocuments:after.size,removed,changed,addedDocuments:added.length,addedByType:added.reduce((r,[,d])=>(r[d.type]=(r[d.type]??0)+1,r),{}),addedGameObjects:added.filter(([,d])=>d.type===1).map(([,d])=>d.name),assets,missingMeta:assets.filter(p=>!p.endsWith('.meta')&&!assets.includes(p+'.meta'))};
fs.writeFileSync('output/lake-waterfall/saved-scene-audit.json',JSON.stringify(report,null,2));
console.log(JSON.stringify({removed:removed.length,changed:changed.length,byType:changed.reduce((r,d)=>(r[d.type]=(r[d.type]??0)+1,r),{}),transforms:changed.filter(d=>d.type===4),added:added.length,addedByType:report.addedByType,missingMeta:report.missingMeta},null,2));
if(removed.length||report.missingMeta.length)process.exitCode=1;
