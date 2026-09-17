"""Send a bounded task and source snapshot to Sonnet; never modify source files.

No network/proxy or permission policy changes. Output is a candidate, not approval.
Usage: python Tools/sonnet_packet.py Collaboration/tasks/001-input-system.md \
       --include Packages/manifest.json --mode implement
"""
from pathlib import Path
from datetime import datetime, timezone
import argparse
import hashlib
import json
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
MODEL = 'claude-sonnet-5'


def project_file(name):
    path = (ROOT / name).resolve()
    if not path.is_relative_to(ROOT) or not path.is_file():
        raise ValueError('Input must be an existing file inside this project: ' + name)
    return path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('task')
    parser.add_argument('--include', action='append', default=[])
    parser.add_argument('--mode', choices=['implement', 'review', 'exchange'], required=True)
    parser.add_argument('--timeout', type=int, default=120)
    parser.add_argument('--debug-file', help='Optional diagnostic file inside this project; do not share raw logs.')
    args = parser.parse_args()
    if not 1 <= args.timeout <= 1800:
        parser.error('timeout must be 1..1800 seconds')
    task = project_file(args.task)
    paths = [project_file('AGENTS.md'), project_file('CLAUDE.md'), task]
    paths += [project_file(name) for name in args.include]
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    run = ROOT / 'Collaboration/runs' / (stamp + '-' + args.mode)
    run.mkdir(parents=True)
    packet = []
    for path in dict.fromkeys(paths):
        raw = path.read_bytes()
        packet.append({'path': path.relative_to(ROOT).as_posix(),
                       'sha256': hashlib.sha256(raw).hexdigest(),
                       'content': raw.decode('utf-8-sig')})
    (run / 'input.json').write_text(json.dumps(packet, ensure_ascii=False, indent=2), encoding='utf-8')
    instruction = (
        'You are Claude Code Sonnet 5, assigned by the GPT-6 Astra design coordinator. '
        'Tools are disabled in this transport. Analyze only the supplied snapshot; '
        'do not claim you ran tests or read other files. Do not change models. '
        'Write Japanese. The task file defines scope and acceptance criteria. '
    )
    if args.mode == 'implement':
        instruction += (
            'Return ONLY JSON with keys summary, files (array of {path, base_sha256, content}), '
            'checks_not_run (array), risks (array). Include only the permitted changed files; '
            'content is the complete replacement. If the task cannot be solved, return an empty '
            'files array and explain in risks. Your candidate will be reviewed before acceptance. '
        )
    elif args.mode == 'review':
        instruction += (
            'Perform an independent review without editing. Report concrete findings with severity, '
            'file/line, trigger, fix, target SHA256; separate static findings from unverified runtime behavior. '
            'Conclude approve or request changes for the specified scope, and list unexecuted checks. '
        )
    else:
        instruction += (
            'Compare the supplied Codex and Claude review reports; respond to each finding and list '
            'agreements, disagreements with evidence, needed changes, and remaining validation. '
            'Do not mark integration complete without both reviews of the same source version. '
        )
    prompt = instruction + '\n\nINPUT PACKET:\n' + json.dumps(packet, ensure_ascii=False)
    report = {'requested_model': MODEL, 'mode': args.mode, 'task': args.task,
              'started_utc': stamp, 'status': 'pending',
              'sources': [{k: p[k] for k in ('path', 'sha256')} for p in packet]}
    cli = shutil.which('claude')
    if not cli:
        report['status'] = 'cli_missing'
    else:
        command = [cli, '-p', '--model', MODEL, '--safe-mode', '--no-session-persistence',
                   '--tools', '', '--output-format', 'json']
        if args.debug_file:
            debug = (ROOT / args.debug_file).resolve()
            if not debug.is_relative_to(ROOT):
                parser.error('debug-file must stay inside the project')
            debug.parent.mkdir(parents=True, exist_ok=True)
            command += ['--debug-file', str(debug)]
        try:
            result = subprocess.run(command, input=prompt, cwd=ROOT, capture_output=True,
                                    text=True, encoding='utf-8', errors='replace', timeout=args.timeout)
            report['exit_code'] = result.returncode
            try:
                response = json.loads(result.stdout)
                report['models_reported'] = list(response.get('modelUsage', {}))
                report['is_error'] = response.get('is_error')
                report['status'] = ('candidate_received' if result.returncode == 0 and not response.get('is_error')
                                    else 'failed')
                # Usage records are stronger evidence than a model claiming its own identity.
                report['requested_model_verified'] = (
                    bool(report['models_reported']) and
                    all(m == MODEL or m.startswith(MODEL + '-') for m in report['models_reported']))
                if report['status'] == 'candidate_received' and not report['requested_model_verified']:
                    report['status'] = 'model_unverified'
                body = response.get('result', '')
                (run / 'response.txt').write_text(body, encoding='utf-8')
            except (ValueError, TypeError):
                report['status'] = 'invalid_json_response'
        except subprocess.TimeoutExpired:
            report['status'] = 'timeout'
        except OSError as error:
            report['status'] = 'launch_failed'
            report['error_type'] = type(error).__name__
    (run / 'status.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'run': str(run), 'status': report['status'],
                      'models_reported': report.get('models_reported', [])}, ensure_ascii=False))
    return 0 if report['status'] == 'candidate_received' else 2


if __name__ == '__main__':
    sys.exit(main())
