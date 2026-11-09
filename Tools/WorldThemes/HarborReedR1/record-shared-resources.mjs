import fs from 'node:fs';
import crypto from 'node:crypto';
const survey=JSON.parse(fs.readFileSync('output/HarborReedR1/survey.json','utf8'));
const candidates=[...survey.renderers.flatMap(r=>r.materials),...survey.terrain.layers.flatMap(l=>[l.path,l.texture]),...survey.terrain.details.map(d=>d.prefab),'Assets/01.Scenes/Scenario/OpenWorld/Regions/SaltwindHarborRegion.unity','Assets/10.Datas/World/OpenWorld/HarborNaturalWater.mat','Assets/03.Prefabs/Environment/HarborRevision/Harbor_TimberDeck.prefab','Assets/03.Prefabs/Environment/HarborRevision/Harbor_StoneQuay.prefab','Assets/10.Datas/Camera/CameraOccluderNameRules.asset'];
const rows=[...new Set(candidates)].filter(p=>p&&fs.existsSync(p)).map(path=>({path,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'),guid:fs.existsSync(path+'.meta')?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,mode:'shared-readonly'}));
fs.writeFileSync('output/HarborReedR1/shared-resource-baseline.json',JSON.stringify({observedAt:new Date().toISOString(),note:'공통 씬 snapshot 이후 C 제작 전 읽기 관측. 사용자 후속 저장과 최종 비교할 참조 자원 해시. 전체 프로젝트 스냅샷 아님.',rows},null,2));
console.log(rows.length);
