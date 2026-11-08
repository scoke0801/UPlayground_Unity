// Unity가 만든 신규 오브젝트는 유지하고 기존 오브젝트의 자동 재직렬화만 작업 전 바이트로 복원한다.
import fs from 'node:fs';
const backupRoot='Logs/HumanoidGrade/Remaining/BossBindingBackup';
const first=fs.readdirSync(backupRoot).sort()[0];
const allowed=new Set(['_motionSet','_abilitySet','_socketDict','m_Children','m_IsActive','m_Sources','m_Modification']);
function split(text){return text.replace(/\r\n/g,'\n').split(/(?=^--- !u!)/m);}
function fields(block){const parts=block.split(/(?=^  \w+:)/m);return {prefix:parts.shift(),entries:parts.map(p=>[p.match(/^  (\w+):/)[1],p])};}
const report=[];
for(const name of ['MonsterActor_Sera_GreatSword','MonsterActor_SeolA_Bow']){
  const path=`Assets/03.Prefabs/Actor/Monster/Humanoid/${name}.prefab`;
  const before=fs.readFileSync(`${backupRoot}/${first}/${path}`,'utf8');
  const blocks=split(before).slice(1);
  const baseline=new Map(blocks.map(b=>[b.match(/^--- !u!\d+ &(-?\d+)/)[1],b]));
  const serialized=`Logs/HumanoidGrade/Remaining/Serialized/${path}`;
  if(!fs.existsSync(serialized))throw Error('Unity 저장 직후 검토용 사본 누락');
  const current=split(fs.readFileSync(serialized,'utf8'));
  let restored=0;
  const result=current.map((block,index)=>{
    if(index===0)return block;
    const id=block.match(/^--- !u!\d+ &(-?\d+)/)[1];
    if(!baseline.has(id))return block;
    const old=fields(baseline.get(id)),now=fields(block),newFields=new Map(now.entries);
    // 중첩 프리팹의 추가 자식은 PrefabInstance의 AddedGameObjects와 stripped 오브젝트 표현을 함께 보존한다.
    if(old.prefix!==now.prefix)return block;
    const merged=old.prefix+old.entries.map(([key,value])=>allowed.has(key)&&newFields.has(key)?newFields.get(key):value).join('');
    if(merged!==block)restored++;
    return merged;
  }).join('');
  const expectedIds=[...baseline.keys()];
  for(const id of expectedIds)if(!result.includes(` &${id}\n`)&&!result.includes(` &${id} stripped\n`))throw Error('기존 fileID 누락: '+id);
  if(process.argv.includes('--apply')){
    const live=fs.readFileSync(path,'utf8').replace(/\r\n/g,'\n');
    if(live!==current.join('')&&live!==result)throw Error('저장 직후/정리 결과와 다른 사용자 변경: '+path);
    fs.writeFileSync(path,before.includes('\r\n')?result.replace(/\n/g,'\r\n'):result);
  }
  report.push({path,restoredExistingBlocks:restored,applied:process.argv.includes('--apply')});
}
fs.writeFileSync('Logs/HumanoidGrade/Remaining/serialization-cleanup.json',JSON.stringify(report,null,2));
console.log(report);
