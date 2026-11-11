import fs from 'node:fs';
import assert from 'node:assert/strict';

const path = 'Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity';
const backup = 'output/WeatherSky/backup/RiverBasinWorld.unity';
function documents(source) {
    const headers = [...source.matchAll(/^--- !u!(\d+) &(-?\d+).*$/gm)];
    return headers.map((h, i) => ({ id: h[2], type: h[1], text: source.slice(h.index, headers[i + 1]?.index ?? source.length) }));
}
const before = fs.readFileSync(backup, 'utf8').replaceAll('\r\n', '\n');
const after = fs.readFileSync(path, 'utf8').replaceAll('\r\n', '\n');
const original = new Map(documents(before).map(d => [d.id, d]));
const skyGuid = fs.readFileSync('Assets/02.Scripts/Gameplay/World/SceneEnvironmentRendering.cs.meta', 'utf8').match(/guid: (\w+)/)[1];
const sky = [...original.values()].find(d => d.text.includes(`guid: ${skyGuid}`));
assert.ok(sky);
const gameObjectId = sky.text.match(/m_GameObject: \{fileID: (-?\d+)/)[1];
const allowed = new Set([sky.id, gameObjectId, '9223372036854775807']);
const restored = [], changed = [], added = [];
const current = documents(after);
assert.ok([...original.keys()].every(id => current.some(d => d.id === id)), '기존 씬 블록 삭제 금지');
const result = current.map(d => {
    if (!original.has(d.id)) { added.push(d.id); return d.text; }
    if (original.get(d.id).text === d.text) return d.text;
    if (allowed.has(d.id)) { changed.push(d.id); return d.text; }
    restored.push({ id: d.id, type: d.type });
    return original.get(d.id).text;
});
fs.writeFileSync(path, after.slice(0, after.indexOf('--- !u!')) + result.join(''));
const report = { scene: path, changed, added, restoredAutomaticSerialization: restored, removed: [] };
fs.writeFileSync('output/WeatherSky/scene-diff-audit.json', JSON.stringify(report, null, 2));
console.log(JSON.stringify({ changed, added: added.length, restored: restored.length }));
