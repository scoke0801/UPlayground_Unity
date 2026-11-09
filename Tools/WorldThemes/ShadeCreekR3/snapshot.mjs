import fs from 'node:fs';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';

// Play 중에는 에셋을 만들지 않고 디스크 기준선만 기록한다.
const out='output/ShadeCreekR3';
fs.mkdirSync(out,{recursive:true});
if(fs.existsSync(`${out}/baseline.json`))throw Error('시작 기준선 덮어쓰기 금지');
const files=new Set();
function collect(p){
 if(!fs.existsSync(p))return;
 if(fs.statSync(p).isDirectory())for(const f of fs.readdirSync(p))collect(`${p}/${f}`);
 else files.add(p);
}
for(const p of ['Assets/01.Scenes/ArtReview/WorldThemes','Assets/10.Datas/World/ThemeStudies/ShadeCreekR1','Assets/10.Datas/World/ThemeStudies/ShadeCreekR2','Assets/10.Datas/World/OpenWorld','Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity','Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity.meta','ProjectSettings'])collect(p);
const status=execFileSync('git',['status','--porcelain=v1','-z'],{encoding:'utf8'});
fs.writeFileSync(`${out}/git-status-start.txt`,status.replaceAll('\0','\n'));
for(const row of status.split('\0').filter(Boolean)){
 const p=row.slice(3);
 if(!p.startsWith('Tools/WorldThemes/ShadeCreekR3'))collect(p);
}
const rows=[...files].sort().map(path=>({path,bytes:fs.statSync(path).size,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex')}));
const prior=JSON.parse(fs.readFileSync('output/ShadeCreekR2/source-delta.json','utf8'));
const main=rows.find(r=>r.path==='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity');
const result={at:new Date().toISOString(),editorObserved:{playing:true,scene:'Assets/01.Scenes/Test/CombatTestMap.unity',compiling:false,compileFailed:false},rows,canonicalMatchesR2ProductionBaseline:main.sha256===prior.sources[1].sha256,scope:'디스크 기준선. Play 상태의 미저장 메모리 변경은 포함하지 않음'};
fs.writeFileSync(`${out}/baseline.json`,JSON.stringify(result,null,2));
console.log({files:rows.length,canonicalMatchesR2ProductionBaseline:result.canonicalMatchesR2ProductionBaseline});
