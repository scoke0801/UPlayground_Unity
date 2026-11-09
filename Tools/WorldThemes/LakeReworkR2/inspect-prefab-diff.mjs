import fs from 'node:fs';
const read=p=>new Map([...fs.readFileSync(p,'utf8').matchAll(/^--- !u!(\d+) &(\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],body:m[3]}]));
const a=read('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity'),b=read('Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity');
const props=s=>new Map([...s.matchAll(/    - target: ([^\n]+)\r?\n      propertyPath: ([^\r\n]+)\r?\n      value: ([^\r\n]*)\r?\n      objectReference: ([^\r\n]+)/g)].map(m=>[m[1]+'|'+m[2],m[3]+'|'+m[4]]));
const counts={},other=[];
for(const [id,src] of a){if(src.type!=='1001')continue;const dst=b.get(id);if(!dst||src.body===dst.body)continue;const ap=props(src.body),bp=props(dst.body);for(const key of new Set([...ap.keys(),...bp.keys()]))if(ap.get(key)!==bp.get(key)){const prop=key.split('|')[1].replaceAll("'", "");counts[prop]=(counts[prop]||0)+1;if(!['m_LocalPosition.y','m_LocalPosition.x','m_LocalPosition.z','m_Materials.Array.data[0]'].includes(prop))other.push({id,prop,before:ap.get(key),after:bp.get(key)});}}
fs.writeFileSync('output/LakeReworkR2/prefab-diff.json',JSON.stringify({counts,other},null,2));console.log(JSON.stringify({counts,other}));

