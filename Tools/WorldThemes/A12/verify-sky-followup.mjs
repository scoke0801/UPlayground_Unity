import fs from 'node:fs';
import crypto from 'node:crypto';
const sha=b=>crypto.createHash('sha256').update(b).digest('hex').toUpperCase();
const beforePath='output/RegionalSky/Followup/baseline/LakeVillageWorld.before-integration.unity';
const afterPath='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
function parse(path){const bytes=fs.readFileSync(path),s=bytes.toString('utf8'),h=[...s.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$/gm)];return {path,sha256:sha(bytes),prefix:s.slice(0,h[0].index),order:h.map(x=>x[2]),docs:new Map(h.map((x,i)=>[x[2],{id:x[2],type:x[1],text:s.slice(x.index,h[i+1]?.index??s.length)}]))};}
const b=parse(beforePath),a=parse(afterPath),removed=[...b.docs.keys()].filter(k=>!a.docs.has(k)),added=[...a.docs.values()].filter(d=>!b.docs.has(d.id)),changed=[...a.docs.values()].filter(d=>b.docs.has(d.id)&&b.docs.get(d.id).text!==d.text);
const changes=changed.map(d=>{const old=b.docs.get(d.id).text.split(/\r?\n/),now=d.text.split(/\r?\n/);return {id:d.id,type:d.type,beforeOnly:old.filter(x=>!now.includes(x)),afterOnly:now.filter(x=>!old.includes(x))};});
const expectedChanged=['453226482','874872977','879054241','1474497645','9223372036854775807'];
const errors=[];if(changed.map(x=>x.id).sort().join()!=expectedChanged.sort().join())errors.push('Unexpected changed documents');if(added.length!==6||removed.length)errors.push('Unexpected added/removed count');
const unchangedTransforms=[...b.docs.values()].filter(d=>d.type==='4'&&a.docs.get(d.id)?.text===d.text).length;
const allOldTransforms=[...b.docs.values()].filter(d=>d.type==='4').length;
if(unchangedTransforms!==allOldTransforms)errors.push('Existing Transform changed');
// 기존 콘텐츠 모든 문서가 위5개 외 바이트 동일하므로 콘텐츠 ID 직렬화도 동일하다.
const files=['Assets/10.Datas/World/OpenWorld/Terrain.asset','Assets/10.Datas/World/OpenWorld/Terrain.asset.meta','Assets/10.Datas/World/OpenWorld/VillageBuildingGroundingNavigation.asset','Assets/10.Datas/World/OpenWorld/VillageBuildingGroundingNavigation.asset.meta','Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity.meta'].map(path=>({path,sha256:sha(fs.readFileSync(path))}));
const baseline=JSON.parse(fs.readFileSync('Tools/WorldThemes/Integration/source-baseline-v2.json','utf8'));
const result={scope:'A12 independent disk YAML document/byte comparison; no Unity calls',before:{path:b.path,sha256:b.sha256,documents:b.docs.size},after:{path:a.path,sha256:a.sha256,documents:a.docs.size},unchangedOriginalDocuments:b.docs.size-changed.length-removed.length,changed:changes,added:added.map(d=>({id:d.id,type:d.type,text:d.text})),removed,existingTransforms:{total:allOldTransforms,unchanged:unchangedTransforms},files,errors,proposedBaseline:{...baseline,version:'A12-proposed-source-baseline-after-RegionalSkyFollowup',sourceSceneHash:a.sha256,parentSourceSceneHash:b.sha256,status:'Proposal only. A00 must adopt explicitly; Integration baseline not modified',acceptance:'Disk preservation only; user TPS/art/GPU pending'}};
Object.assign(result.proposedBaseline,{
  previousSourceSceneHash:b.sha256,
  geometryEvidence:'output/WorldThemeProduction/A12/sky-followup-independent.json',
  independentEvidence:'output/WorldThemeProduction/A12/sky-followup-independent.json',
  changeScope:'RegionalSky followup: existing104766 documents; changed5, added6, removed0. Other104761 documents byte-identical. All existing46248 Transform documents byte-identical. Changed bindings/WorldLighting route/WaterRenderer reflection consumption/SceneRoots; new Region and ReflectionProbe roots.',
  preserve:'Preserve external RegionalSky source changes, original terrain/current Nav, historical ArtReview artifacts and any live user unsaved state. Do not revert or save/discard user state.',
  reviewDocument:'output/WorldThemeProduction/A12/sky-followup-baseline-review.md'
});
// 상세 차이와 제안 양쪽에 같은 최신 근거를 기록한다.
fs.writeFileSync('output/WorldThemeProduction/A12/sky-followup-independent.json',JSON.stringify(result,null,2));
fs.writeFileSync('output/WorldThemeProduction/A12/source-baseline-proposed-skyfollowup.json',JSON.stringify(result.proposedBaseline,null,2));
console.log(JSON.stringify({before:result.before,after:result.after,unchanged:result.unchangedOriginalDocuments,changed:changes,added:added.map(d=>({id:d.id,type:d.type})),transforms:result.existingTransforms,errors},null,2));
