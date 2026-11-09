import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const output='output/WorldThemeProduction/ShadeCreek';fs.mkdirSync(output,{recursive:true});
const targets=[
 'Assets/03.Prefabs/Environment/RedTreeCreek/CreekArt.prefab',
 'Assets/11.Material/Environment/RedTreeCreek/CreekPool.mat',
 'Assets/11.Material/Environment/RedTreeCreek/CreekBark.mat',
 'Assets/11.Material/Environment/RedTreeCreek/CreekSilt.mat',
 'Assets/11.Material/Environment/RedTreeCreek/CreekStone.mat'
].map(file=>({path:file,guid:fs.readFileSync(file+'.meta','utf8').match(/^guid: (\w+)/m)[1],sha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')}));
const walk=(dir,result=[])=>{for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.posix.join(dir,e.name);if(e.isDirectory())walk(p,result);else result.push(p);}return result;};
const files=[...walk('Assets/01.Scenes'),...walk('Assets/10.Datas/World'),...walk('Assets/03.Prefabs/Environment')].filter(p=>/\.(unity|asset|prefab|json|mat)$/i.test(p));
const refs=[],skipped=[];let scanned=0;
for(const file of files){
 const size=fs.statSync(file).size;
 if(!file.endsWith('.unity')&&size>8*1024*1024){skipped.push({file,size,reason:'8MiB 초과 비씬 에셋'});continue;}
 const bytes=fs.readFileSync(file);scanned++;
 for(const target of targets){let count=0,from=0;while((from=bytes.indexOf(target.guid,from))>=0){count++;from+=target.guid.length;}if(!count)continue;
  const text=bytes.toString('utf8');const lines=text.split(/\r?\n/);const locations=[];for(let i=0;i<lines.length;i++)if(lines[i].includes(target.guid))locations.push({line:i+1,property:lines[i].trim().slice(0,220)});
  refs.push({file,size,target:target.path,guid:target.guid,count,locations});
 }
}
const data={status:'디스크 GUID 정확 역참조. 실제 로드·MPB 실행·vertex color·서사 상태 검증은 미수행',targets,scannedFiles:scanned,skipped,searchRoots:['Assets/01.Scenes','Assets/10.Datas/World','Assets/03.Prefabs/Environment'],references:refs,limits:['GUID 기반 정적 직접 참조만 추적. Addressables/동적 문자열 로딩과 ScriptableObject 간접 연쇄는 별도 확인 필요.','CreekArt 프리팹 파일 존재 또는 참조 부재를 현재 월드 물길 존재/부재로 환산하지 않는다.']};
fs.writeFileSync(output+'/creek-guid-references.json',JSON.stringify(data,null,2));
console.log(JSON.stringify({scanned,skipped:skipped.length,targets:targets.map(t=>({path:t.path,guid:t.guid})),references:refs.map(r=>({file:r.file,target:r.target,count:r.count}))},null,2));
