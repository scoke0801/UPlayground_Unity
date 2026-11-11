import fs from 'node:fs';

// 기존 블록의 자동 재직렬화만 복구하고 새 참조와 자식은 보존한다.
function documents(source) {
    const headers = [...source.matchAll(/^--- !u!(\d+) &(-?\d+).*$/gm)];
    return headers.map((h, i) => ({ id: h[2], type: h[1], text: source.slice(h.index, headers[i + 1]?.index ?? source.length) }));
}
const reports = [];
for (const name of ['EnvironmentEffects', 'LakeOfLifeEnvironment', 'DroughtVillageEnvironment']) {
    const path = `Assets/03.Prefabs/LivingWorld/Environment/${name}.prefab`;
    const before = fs.readFileSync(`output/WeatherSky/backup/${name}.prefab`, 'utf8').replaceAll('\r\n', '\n');
    const after = fs.readFileSync(path, 'utf8').replaceAll('\r\n', '\n');
    const old = new Map(documents(before).map(d => [d.id, d]));
    const changed = [], added = [], restored = [];
    const result = documents(after).map(d => {
        if (!old.has(d.id)) { added.push(d.id); return d.text; }
        const original = old.get(d.id);
        if (original.text === d.text) return d.text;
        // 의도한 변경은 FX 루트의 자식 목록과 EnvironmentEffects 직렬화 필드뿐이다.
        const isEffects = d.text.includes('guid: 9d5db32391b1c06439192d83bf28c36b');
        const addsChildren = d.type === '4' && d.text.includes('m_Children:') &&
            (d.text.match(/  - \{fileID:/g)?.length ?? 0) > (original.text.match(/  - \{fileID:/g)?.length ?? 0);
        if (isEffects || addsChildren) { changed.push(d.id); return d.text; }
        restored.push(d.id);
        return original.text;
    });
    const prefix = after.slice(0, after.indexOf('--- !u!'));
    fs.writeFileSync(path, prefix + result.join(''));
    reports.push({ path, changed, added: added.length, restoredAutomaticSerialization: restored });
}
fs.writeFileSync('output/WeatherSky/prefab-diff-audit.json', JSON.stringify(reports, null, 2));
console.log(JSON.stringify(reports));
