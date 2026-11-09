import fs from 'node:fs';
import assert from 'node:assert/strict';
const sourcePath='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const copyPath='Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity';
const source=fs.readFileSync(sourcePath,'utf8'),copy=fs.readFileSync(copyPath,'utf8');
const identity=/^  _guid: ([^\r\n]+)\r?\n  _ownerKey: GlobalObjectId_V1-\d+-[a-f0-9]+-(\d+-\d+)/gm;
const ids=s=>new Map([...s.matchAll(identity)].map(m=>[m[2],m[1]]));
const before=ids(source),after=ids(copy);
for(const [id,value] of before)assert.equal(after.get(id),value,`진행 ID ${id}`);
const blocks=s=>new Map([...s.matchAll(/^--- !u!(\d+) &(\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],body:m[3]}]));
const original=blocks(source),review=blocks(copy);const changes=[];let removed=0;
for(const [id,a] of original){const b=review.get(id);if(!b){removed++;continue;}const normalize=s=>s.replace(/^  _ownerKey: .*$/gm,'  _ownerKey: COPY_OWNER').replace(/\r/g,'');if(normalize(a.body)!==normalize(b.body))changes.push({id,type:a.type});}
assert.equal(removed,0,'기존 개체 제거');
const result={sceneEntityIds:before.size,idsPreserved:true,removedSerializedObjects:removed,changedExistingBlocks:changes,newBlocks:review.size-original.size,scope:'저장된 YAML 비교. 플레이 검증이 아님'};
fs.writeFileSync('output/LakeReworkR2/serialized-audit.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
