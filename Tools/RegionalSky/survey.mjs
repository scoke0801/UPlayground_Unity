import fs from 'node:fs';
import crypto from 'node:crypto';

// 원본 씬을 열거나 저장하지 않는 직렬화 보조 조사. 실제 Unity 로드 검사를 대체하지 않는다.
const scene = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const source = fs.readFileSync(scene, 'utf8');
const heads = [...source.matchAll(/^--- !u!(\d+) &(-?\d+).*$/gm)];
const docs = heads.map((h, i) => ({ type: h[1], id: h[2], text: source.slice(h.index, heads[i + 1]?.index ?? source.length) }));
const scalar = (text, key) => text.match(new RegExp(`^  ${key}: ?(.*)$`, 'm'))?.[1]?.trim();
const id = value => value?.match(/fileID: (-?\d+)/)?.[1];
const names = new Map(docs.filter(d => d.type === '1').map(d => [d.id, scalar(d.text, 'm_Name')]));
const transforms = new Map(docs.filter(d => d.type === '4').map(d => [id(scalar(d.text, 'm_GameObject')), d]));
const selected = docs.filter(d => (['114', '215', '104'].includes(d.type) && /aa71b2344a03951498765f2edbe96bf2|25ba1bcc32aaf4a489e07d741dc6437b|2193309a95ca55842812ffc650751448|ReflectionProbe:|m_Fog:/.test(d.text)) ||
  (['23','33'].includes(d.type) && names.get(id(scalar(d.text,'m_GameObject'))) === 'LakeWater') ||
  (d.type === '1' && /LakeWater|Creek|Harbor|Shrine|Route|Waypoint|Directional|Global Volume|PlayerSpawn|StartPoint/i.test(names.get(d.id) ?? '')));
const rows = selected.map(d => ({id:d.id,type:d.type,name:names.get(id(scalar(d.text,'m_GameObject'))) ?? names.get(d.id),text:d.text,transform:transforms.get(d.id)?.text}));
fs.mkdirSync('output/RegionalSky/Followup', {recursive:true});
fs.writeFileSync('output/RegionalSky/Followup/scene-survey.json', JSON.stringify({scene,sha256:crypto.createHash('sha256').update(source).digest('hex'),scope:'정적 보조 조사',rows},null,2));
console.log(JSON.stringify(rows.map(r => ({id:r.id,type:r.type,name:r.name, text:r.type!=='1'?r.text:undefined,transform:r.transform})),null,2));
