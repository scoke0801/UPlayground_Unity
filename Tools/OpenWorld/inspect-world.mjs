// 씬을 열거나 저장하지 않고 통합 설계의 직렬화 근거를 수집한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const root = process.cwd();
const out = path.join(root, 'output/OpenWorld');
fs.mkdirSync(out, { recursive: true });
function files(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e =>
    e.isDirectory() ? files(path.join(dir, e.name)) : [path.join(dir, e.name).replaceAll('\\', '/')]);
}
const read = p => {
  const bytes = fs.readFileSync(p);
  return bytes.toString(bytes[0] === 0xff && bytes[1] === 0xfe ? 'utf16le' : 'utf8').replace(/^\uFEFF/, '');
};
const sources = files('Assets/02.Scripts').filter(p => p.endsWith('.cs'));
const scriptGuids = new Map();
for (const p of sources) {
  if (fs.existsSync(p + '.meta')) {
    const guid = read(p + '.meta').match(/^guid: (\w+)/m)?.[1];
    if (guid) scriptGuids.set(guid, p);
  }
}
const patterns = /CurrentMapID|OnSceneChanged|OnSceneContextReady|GetActiveScene\(|ReleaseSceneAssets|DroughtVillage|SaltwindHarbor|StonegateCitadel/;
const references = [];
for (const p of sources) {
  read(p).split('\n').forEach((line, i) => {
    if (patterns.test(line)) references.push({ path: p, line: i + 1, text: line.trim() });
  });
}
fs.writeFileSync(path.join(out, 'references.json'), JSON.stringify(references, null, 2));
const sceneFiles = [...files('Assets/01.Scenes/Scenario'), ...files('Assets/01.Scenes/Ingame')].filter(p => p.endsWith('.unity'));
const scenes = [];
for (const p of sceneFiles) {
  const text = read(p);
  const blocks = text.split(/^--- !u!/m).slice(1);
  const objects = new Map();
  const transforms = new Map();
  const components = [];
  const counts = {};
  for (const block of blocks) {
    const header = block.match(/^(\d+) &(-?\d+)/);
    if (!header) continue;
    const [, type, id] = header;
    counts[type] = (counts[type] ?? 0) + 1;
    const go = block.match(/m_GameObject: \{fileID: (-?\d+)\}/)?.[1];
    if (type === '1') objects.set(id, { name: block.match(/^  m_Name: (.*)$/m)?.[1], active: block.match(/m_IsActive: (\d)/)?.[1] });
    if (type === '4' && go) transforms.set(go, {
      id, parent: block.match(/m_Father: \{fileID: (-?\d+)\}/)?.[1],
      position: block.match(/m_LocalPosition: (.*)/)?.[1],
      rotation: block.match(/m_LocalRotation: (.*)/)?.[1],
    });
    const script = scriptGuids.get(block.match(/m_Script: \{fileID: \d+, guid: (\w+)/)?.[1]);
    if (script && /SceneContext|Arrival|PlacementLoader|PlayerActor.cs|WorldLighting|EnvironmentController|PortalActor|FlowGraphRunner|TriggerComposer|SceneEntityId|NavMesh|NpcActor.cs/.test(script)) {
      components.push({ id, gameObject: go, script, fields: block.split('\n').filter(l => /^  (?!m_)/.test(l)).slice(0, 48) });
    }
    if (['20', '108', '218', '215', '157'].includes(type)) components.push({ id, gameObject: go, type, fields: block.split('\n').filter(l => /m_TerrainData|m_Type:|m_Enabled:|m_Range:|m_Intensity:|m_FarClipPlane/.test(l)) });
  }
  for (const component of components) Object.assign(component, { object: objects.get(component.gameObject), transform: transforms.get(component.gameObject) });
  scenes.push({ path: p, bytes: fs.statSync(p).size, sha256: crypto.createHash('sha256').update(text).digest('hex'), counts, components });
}
fs.writeFileSync(path.join(out, 'scene-inventory.json'), JSON.stringify(scenes, null, 2));
console.log(JSON.stringify({ scenes: scenes.map(s => ({ path: s.path, bytes: s.bytes, terrains: s.counts['218'] ?? 0, prefabInstances: s.counts['1001'] ?? 0, contexts: s.components.filter(c => c.script?.endsWith('/SceneContext.cs')) })), references: references.length }, null, 2));
