import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const matches=[];
const walk=dir=>{for(const entry of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,entry.name);if(entry.isDirectory())walk(p);else if(entry.name.endsWith('.cs')){
 const text=fs.readFileSync(p,'utf8');const lines=text.split(/\r?\n/);const rows=[];
 for(let i=0;i<lines.length;i++)if(/MaterialPropertyBlock|SetPropertyBlock\s*\(|GetPropertyBlock\s*\(|\.colors32?\s*=|SetColors\s*\(/.test(lines[i]))rows.push({line:i+1,text:lines[i].trim()});
 if(rows.length)matches.push({path:p.replaceAll('\\','/'),sha256:crypto.createHash('sha256').update(text).digest('hex'),matches:rows});
}}};
walk('Assets/02.Scripts');
fs.writeFileSync('output/WorldThemeProduction/ShadeCreek/mpb-writer-inventory.json',JSON.stringify({status:'코드 후보 목록만. 정확 tree Renderer 역참조/실행 범위/외부 플러그인·runtime lookup 소유권 미검증',scope:'Assets/02.Scripts/**/*.cs',files:matches},null,2));
console.log(JSON.stringify({candidateFiles:matches.length,treeNamedCandidates:matches.filter(r=>/Tree|Forest|Nature|Wind|Water|Creek|OpenWorld|SceneEnvironment/i.test(r.path)).map(r=>r.path)},null,2));
