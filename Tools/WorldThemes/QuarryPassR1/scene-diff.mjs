import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const baseline='output/WorldThemesSharedBaseline/Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const review='Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity';
const output='output/QuarryPassR1';
const sha=text=>crypto.createHash('sha256').update(text).digest('hex');
function parse(text){
 const rows=new Map();
 const starts=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n/gm)];
 for(let i=0;i<starts.length;i++){const m=starts[i];assert(!rows.has(m[2]),'중복 fileID');rows.set(m[2],{type:m[1],text:text.slice(m.index,starts[i+1]?.index??text.length)});}
 return rows;
}
assert.equal(parse('--- !u!1 &1\nA\n--- !u!4 &2\nB\n').size,2);
const oldText=fs.readFileSync(baseline,'utf8'),newText=fs.readFileSync(review,'utf8');
const before=parse(oldText),after=parse(newText),changes=[],added=[],removed=[];
for(const [id,old] of before){
 const next=after.get(id);if(!next){removed.push({id,...old});continue;}
 if(old.text===next.text)continue;
 changes.push({fileId:id,type:old.type,beforeHash:sha(old.text),afterHash:sha(next.text),before:old.text,after:next.text});
}
for(const [id,row]of after)if(!before.has(id))added.push({fileId:id,...row});
fs.writeFileSync(`${output}/scene-changes.json`,JSON.stringify({baseline,review,baselineHash:sha(oldText),reviewHash:sha(newText),coordinateSpace:'Unity world metres; prefab overrides retain source fileID and instance ID',changes,added,removed},null,2));
const ids=text=>[...text.matchAll(/^\s+_guid: (.+)\n\s+_ownerKey: (.+)/gm)].map(m=>({guid:m[1].trim(),owner:m[2].trim()}));
const oldIds=ids(oldText),newIds=ids(newText);const normalize=r=>r.map(x=>({...x,owner:x.owner.replace(/(GlobalObjectId_V1-\d+-)[a-f0-9]+/,'$1SCENE')})).sort((a,b)=>a.guid.localeCompare(b.guid));
const guidPreserved=JSON.stringify(normalize(oldIds))===JSON.stringify(normalize(newIds));
const report={changed:changes.length,added:added.length,removed:removed.length,changedTypes:[...new Set(changes.map(r=>r.type))],baselineSceneIds:oldIds.length,reviewSceneIds:newIds.length,idsAndOwnerIdentityPreserved:guidPreserved,ownerSceneGuidRemapRequired:true,sourceSceneUnchanged:sha(fs.readFileSync('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity','utf8'))===sha(oldText)};
fs.writeFileSync(`${output}/scene-preservation.json`,JSON.stringify(report,null,2));console.log(JSON.stringify(report));
