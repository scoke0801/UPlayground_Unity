import fs from 'node:fs';
import assert from 'node:assert/strict';

const folder='output/OpenWorld/WorldPlayability/FixedAppearanceV2';
const paths=fs.readFileSync(`${folder}/prefabs.txt`,'utf8').trim().split(/\r?\n/);
const records=[];
for(const path of paths) {
  const original=fs.readFileSync(`${folder}/backup/${path}`,'utf8');
  const current=fs.readFileSync(path,'utf8');
  const ids=text=>new Set([...text.matchAll(/^--- !u!\d+ &(-?\d+)/gm)].map(m=>m[1]));
  const oldIds=ids(original),newIds=ids(current);
  const expectedGuid=fs.readFileSync(`${folder}/preflight.txt`,'utf8').split(/\r?\n/).find(row=>row.startsWith(path+'|')).split('|')[1];
  assert.equal(fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1],expectedGuid,`${path}: GUID 변경`);
  const added=[...newIds].filter(id=>!oldIds.has(id));
  const dangling=[...current.matchAll(/\{fileID: (-?\d+)\}/g)].map(m=>m[1]).filter(id=>id!=='0'&&!newIds.has(id));
  assert.equal(dangling.length,0,`${path}: 파일 내부 참조 누락 ${dangling.slice(0,5)}`);
  assert.equal(added.length,0,`${path}: 기존 객체 식별자 교체 ${added.slice(0,5)}`);
  const missingBones=text=>text.split(/^--- !u!/m).filter(block=>block.startsWith('137 ')).reduce((total,block)=>total+(block.match(/m_Bones:\s*\n((?:  -.*\n)*)/)?.[1].match(/fileID: 0\}/g)?.length??0),0);
  assert(missingBones(current)<=missingBones(original),`${path}: 누락 본 증가`);
  records.push({path,before:oldIds.size,after:newIds.size,removed:oldIds.size-newIds.size,missingLocalReferences:0,replacedObjectIds:0,missingBoneReferences:missingBones(current)});
}
fs.writeFileSync(`${folder}/serialization-check.json`,JSON.stringify({prefabs:records.length,records},null,2)+'\n');
console.log(JSON.stringify({prefabs:records.length,missingLocalReferences:0,replacedObjectIds:0}));
