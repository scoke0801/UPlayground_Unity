// 읽기 전용 역참조와 사용자 변경 보존 검사. SO 생성·수정은 하지 않는다.
import fs from 'node:fs';
const root='Assets/10.Datas/Ability/Actor/Humanoid';
const originals=fs.readdirSync(root).filter(p=>p.endsWith('.asset')&&(p.startsWith('GA_')||p.startsWith('AbilitySet_'))).map(p=>root+'/'+p);
const guids=new Map(originals.map(p=>[fs.readFileSync(p+'.meta','utf8').match(/guid: (\w+)/)[1],p]));
const hits=[];
function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){if(entry.name==='ExternalAssets')continue;const p=dir+'/'+entry.name;if(entry.isDirectory())walk(p);else if(/\.(asset|prefab|unity)$/.test(p)){const text=fs.readFileSync(p,'utf8');for(const [guid,source] of guids)if(text.includes(guid))hits.push(`${source} <- ${p}`);}}}
walk('Assets');
fs.writeFileSync('Logs/HumanoidGrade/unclassified-references.txt',hits.join('\n')+'\n');
console.log(hits.join('\n'));
