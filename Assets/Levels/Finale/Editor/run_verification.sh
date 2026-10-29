#!/bin/bash
set -euo pipefail

# Close the project's Unity Editor before running this batch verification.
finale_project_root="$(cd "$(dirname "$0")/../../../.." && pwd)"
finale_unity_editor="${FINALE_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity}"
finale_reports="$finale_project_root/Assets/Levels/Finale/Verification"
finale_logs="$(mktemp -d /tmp/finale-verification.XXXXXX)"
mkdir -p "$finale_reports"
finale_unity_args=(-batchmode -projectPath "$finale_project_root")
if [[ "${FINALE_GRAPHICS:-0}" != "1" ]]; then finale_unity_args+=(-nographics); fi
printf 'Unity verification logs: %s\n' "$finale_logs"

"$finale_unity_editor" "${finale_unity_args[@]}" \
  -runTests -testPlatform PlayMode \
  -assemblyNames 'XunJiYiDa.Levels.Finale.PlayMode.Tests;XunJiYiDa.Levels.Shared.PlayMode.Tests' \
  -testResults "$finale_reports/AllPlayMode.xml" -logFile "$finale_logs/play.log"

"$finale_unity_editor" "${finale_unity_args[@]}" \
  -runTests -testPlatform EditMode \
  -assemblyNames 'XunJiYiDa.Levels.Finale.Editor.Tests;XunJiYiDa.Levels.Shared.Editor.Tests' \
  -testResults "$finale_reports/AllEditMode.xml" -logFile "$finale_logs/edit.log"

python3 - "$finale_reports" "$finale_logs" <<'PY'
import pathlib, sys, xml.etree.ElementTree as ET
for name in ('AllEditMode', 'AllPlayMode'):
    path = pathlib.Path(sys.argv[1]) / (name + '.xml')
    root = ET.parse(path).getroot()
    passed, failed, skipped = (int(root.get(key, '0')) for key in ('passed', 'failed', 'skipped'))
    if passed == 0 or failed:
        raise SystemExit(f'{name}: invalid/failed run: {root.attrib}')
    summary = f'Unity 6000.3.19f1\n{name}: {passed} passed, {failed} failed, {skipped} skipped\n'
    path.with_suffix('.summary.txt').write_text(summary)
    print(summary.strip())
print('Logs:', sys.argv[2])
PY
