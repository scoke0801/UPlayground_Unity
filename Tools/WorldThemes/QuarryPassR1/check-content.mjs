import fs from 'node:fs';
import assert from 'node:assert/strict';
const out='output/QuarryPassR1';
const paths=['output/WorldThemesSharedBaseline/Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity','Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity'];
function inherited(file){const t=fs.readFileSync(file,'utf8');const s=t.indexOf('--- !u!1001 &763221753');assert(s>=0);const block=t.slice(s,t.indexOf('\n--- !u!',s+10));const value=k=>block.match(new RegExp('propertyPath: '+k+'\\s+value: ([^\\r\\n]+)'))?.[1];return {guid:value('_guid'),owner:value('_ownerKey')};}
const before=inherited(paths[0]),after=inherited(paths[1]);assert.equal(after.guid,before.guid);assert.equal(after.guid,'362ba9f2d9b54a96835c28c53ed18c30');
const report=JSON.parse(fs.readFileSync(`${out}/content-contract-final.json`,'utf8'));
const entry=report.find(r=>r.id.includes('-7139124630326335096-763221753'));assert(entry);Object.assign(entry,after,{verification:'저장·재로드 후 씬 YAML prefab override를 기준선과 직접 대조'});
fs.writeFileSync(`${out}/content-contract-final.json`,JSON.stringify(report,null,2));
fs.writeFileSync(`${out}/content-contract-verification.json`,JSON.stringify({nativeCount:322,nativeCheck:'scene-preservation.json',prefabOverrideCount:1,before,after,preserved:true,source:'공통 기준 씬 사본 대 저장된 최종 검토 씬',note:'원래 owner를 보존하는 정규 통합 시 검토 씬 owner 변경 제외'},null,2));
console.log({inheritedGuidPreserved:true,contractCount:report.length});
