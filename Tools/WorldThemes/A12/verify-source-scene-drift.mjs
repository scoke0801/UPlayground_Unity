import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const beforePath='output/WorldThemeProduction/A00/backup/LakeVillageWorld.unity';
const afterPath='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const beforeBuffer=fs.readFileSync(beforePath),afterBuffer=fs.readFileSync(afterPath);
const before=beforeBuffer.toString('utf8'),after=afterBuffer.toString('utf8');
function parse(text){
  const h=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$/gm)];
  return {prefix:text.slice(0,h[0].index),order:h.map(x=>x[2]),docs:new Map(h.map((x,i)=>[x[2],{id:x[2],type:x[1],text:text.slice(x.index,h[i+1]?.index??text.length)}]))};
}
const b=parse(before),a=parse(after);
const removed=[...b.docs.keys()].filter(k=>!a.docs.has(k));
const added=[...a.docs.keys()].filter(k=>!b.docs.has(k));
const changed=[...a.docs.values()].filter(d=>b.docs.has(d.id)&&b.docs.get(d.id).text!==d.text);
const expectedIds=['2','453226482'];
assert.deepEqual(changed.map(d=>d.id).sort(),expectedIds.sort());
assert.equal(added.length,0);assert.equal(removed.length,0);
assert.deepEqual(a.order,b.order);assert.equal(a.prefix,b.prefix);
const oldSky='10a503df4c24e194b936fc9ce33f55a9';
const newSky='2049df751ffceebfe97da2adda918667';
assert.equal(a.docs.get('2').text,b.docs.get('2').text.replace(oldSky,newSky));
// 삽입 위치에 관계없이 기존 binding 전체를 비교한다.
const stripSun=s=>s.replace(/^  _sunLight: \{fileID: 453226480\}\r?\n/m,'');
assert.equal(stripSun(a.docs.get('453226482').text),b.docs.get('453226482').text.replace(oldSky,newSky));
assert(a.docs.get('453226482').text.includes('_sunLight: {fileID: 453226480}'));
assert.equal(a.docs.get('453226480').type,'108');
assert(a.docs.get('2').text.includes('m_Sun: {fileID: 453226480}'));
let rebuilt=after;
for(const d of changed)rebuilt=rebuilt.replace(d.text,b.docs.get(d.id).text);
assert.equal(sha(Buffer.from(rebuilt,'utf8')),sha(beforeBuffer));
const manifest=JSON.parse(fs.readFileSync('output/WorldThemeProduction/A00/source-hashes.json','utf8'));
const protectedFiles=['Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity.meta','Assets/10.Datas/World/OpenWorld/Terrain.asset','Assets/10.Datas/World/OpenWorld/Terrain.asset.meta'];
const protectedHashes=protectedFiles.map(path=>{const actual=sha(fs.readFileSync(path)),expected=manifest.find(r=>r.path===path)?.sha256?.toLowerCase();assert.equal(actual,expected,path);return {path,expected,actual,unchanged:true};});
const skyPath='Assets/10.Datas/World/Sky/DaySky_Clear.mat';
assert(fs.readFileSync(skyPath+'.meta','utf8').includes('guid: '+newSky));
assert.equal(sha(fs.readFileSync(afterPath)),sha(afterBuffer),'Source scene changed during review');
const report={method:'A12 independent disk document/byte reconstruction; no Unity calls',before:{path:beforePath,sha256:sha(beforeBuffer),documents:b.docs.size},after:{path:afterPath,sha256:sha(afterBuffer),documents:a.docs.size},addedDocuments:added,removedDocuments:removed,changedDocuments:changed.map(d=>({id:d.id,type:d.type,oldHash:sha(Buffer.from(b.docs.get(d.id).text)),newHash:sha(Buffer.from(d.text))})),unchangedDocumentCount:a.docs.size-changed.length,allTransformDocumentsUnchanged:[...a.docs.values()].filter(d=>d.type==='4').every(d=>d.text===b.docs.get(d.id).text),onlyWhitelistedRenderChanges:true,originalBytesReconstructedByRevertingExactlyTwoDocuments:true,sunBinding:{reference:'453226480',existingLightDocumentType:'108',matchesRenderSettingsSun:true,newLightObjectAdded:false},protectedHashes,newSkyAsset:{path:skyPath,guid:newSky,sha256:sha(fs.readFileSync(skyPath)),metaSha256:sha(fs.readFileSync(skyPath+'.meta'))},limits:['Disk source scene is render-only drift; live dirty ArtReview changes are not covered','Runtime class changes and new sky visual/Play behaviour were not tested by A12','Preserving the external sky does not approve it as a generated candidate or alter A01 comparison look']};
fs.mkdirSync('output/WorldThemeProduction/A12',{recursive:true});
fs.writeFileSync('output/WorldThemeProduction/A12/source-scene-drift-independent.json',JSON.stringify(report,null,2));
console.log(JSON.stringify({beforeDocuments:b.docs.size,afterDocuments:a.docs.size,changed:report.changedDocuments.map(d=>({id:d.id,type:d.type})),unchanged:report.unchangedDocumentCount,allTransformsUnchanged:report.allTransformDocumentsUnchanged,afterSha:report.after.sha256,exactReconstruction:true,protectedFilesUnchanged:protectedHashes.length}));
