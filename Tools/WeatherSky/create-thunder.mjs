import fs from 'node:fs';

// 외부 음원 없이 재현 가능한 원경 천둥의 저역 럼블을 생성한다.
const rate = 22050;
let seed = 5913, low = 0, mid = 0;
const samples = new Float32Array(rate * 6);
for (let i = 0; i < samples.length; i++) {
    const t = i / rate;
    seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
    const noise = seed / 4294967296 * 2 - 1;
    low += 0.018 * (noise - low);
    mid += 0.18 * (noise - mid);
    const attack = (1 - Math.exp(-t * 60)) * Math.exp(-t * 1.3);
    const rumble = (1 - Math.exp(-t * 6)) * Math.exp(-t * 0.7);
    const echoes = [0.24, 0.61, 1.15].reduce((v, delay) => v + (t >= delay ? Math.exp(-(t - delay) * 5) : 0), 0);
    samples[i] = (low * (rumble * 5 + echoes * 0.6) + mid * attack * 0.35) * Math.min(1, Math.max(0, 6 - t));
}
const peak = samples.reduce((v, s) => Math.max(v, Math.abs(s)), 0);
const output = Buffer.alloc(44 + samples.length * 2);
output.write('RIFF'); output.writeUInt32LE(output.length - 8, 4); output.write('WAVEfmt ', 8);
output.writeUInt32LE(16, 16); output.writeUInt16LE(1, 20); output.writeUInt16LE(1, 22);
output.writeUInt32LE(rate, 24); output.writeUInt32LE(rate * 2, 28);
output.writeUInt16LE(2, 32); output.writeUInt16LE(16, 34);
output.write('data', 36); output.writeUInt32LE(samples.length * 2, 40);
samples.forEach((s, i) => output.writeInt16LE(Math.round(s / peak * 29000), 44 + i * 2));
const target = 'Assets/06.Sounds/Environment/Weather_Thunder_Rumble.wav';
fs.writeFileSync(target, output);
console.log(target);
