import { cpSync, existsSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const source = resolve("out");
const destination = resolve("dist");
const clientDirectory = resolve(destination, "client");

if (!existsSync(source)) {
  throw new Error("Next.js 정적 출력 폴더(out)를 찾을 수 없습니다.");
}

if (existsSync(destination)) {
  rmSync(destination, { recursive: true, force: true });
}

const serverDirectory = resolve(destination, "server");
const hostingDirectory = resolve(destination, ".openai");

mkdirSync(clientDirectory, { recursive: true });
mkdirSync(serverDirectory, { recursive: true });
mkdirSync(hostingDirectory, { recursive: true });
cpSync(source, clientDirectory, { recursive: true });
cpSync(resolve(".openai", "hosting.json"), resolve(hostingDirectory, "hosting.json"));

writeFileSync(
  resolve(serverDirectory, "index.js"),
  [
    "export default {",
    "  async fetch(request, env) {",
    "    return env.ASSETS.fetch(request);",
    "  },",
    "};",
    "",
  ].join("\n"),
  "utf8",
);
