#!/bin/sh
cd /Users/jonghyun/ProjectRepo/UnityProj/UPlayground_Unity/Tools/LayaWorker
HF_HOME=/Users/jonghyun/Documents/Codex/2026-11-07/new-chat/work/laya-runtime/hf HF_HUB_OFFLINE=1 /Users/jonghyun/Documents/Codex/2026-11-07/new-chat/work/laya-runtime/venv/bin/python -u laya_worker.py serve --offline --config enemy-data/worker-config.json --host 127.0.0.1 --port 8765
