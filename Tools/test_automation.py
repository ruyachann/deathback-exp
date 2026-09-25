"""Offline integration/guard tests. No CLI calls; no live project changes."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

import automation as a


def stream_fixture(model='claude-sonnet-5', auxiliary=True):
    answer = '{"nonce":"abc","status":"ok"}'
    usage = {'claude-sonnet-5': {}}
    if auxiliary:
        usage['claude-haiku-4-5-20251001'] = {}
    return [
        {'type': 'system', 'subtype': 'init', 'model': 'claude-sonnet-5', 'session_id': 'test-session'},
        {'type': 'assistant', 'session_id': 'test-session', 'parent_tool_use_id': None,
         'message': {'model': model, 'content': [{'type': 'text', 'text': answer}]}},
        {'type': 'result', 'session_id': 'test-session', 'is_error': False,
         'result': answer, 'modelUsage': usage}]


def write_stream(path, events):
    path.write_text('\n'.join(json.dumps(event) for event in events), encoding='utf-8')


class AutomationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        self.before = json.dumps({'dependencies': {'com.unity.inputsystem': '1.12.0', 'other': '2.0'}}).encode()
        for name, data in [(a.TARGET, self.before), (a.TASK, b'task'), ('AGENTS.md', b'rules'),
                           ('CLAUDE.md', b'rules'), ('Assets/LoopRoom/Dummy.cs', b'class Dummy {}')]:
            p = self.root/name
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(data)
        self.run = self.root/'run'
        self.run.mkdir()
        self.calls = []

    def client(self, role, phase, packet):
        self.calls.append((role, phase, copy.deepcopy(packet)))
        if phase == 'check':
            return {'nonce': packet['nonce'], 'status': 'ok'}
        if phase == 'design':
            return {'verdict': 'ready', 'plan': 'Update only dependency', 'acceptance': ['Static diff']}
        if phase.startswith('implement'):
            manifest = json.loads(packet['original'])
            manifest['dependencies']['com.unity.inputsystem'] = '1.17.0'
            return {'path': a.TARGET, 'base_sha256': packet['base_sha256'],
                    'content': json.dumps(manifest), 'summary': 'Update'}
        contract = copy.deepcopy(packet['response_contract'])
        contract.update(verdict='approve', findings=[], unverified=['Unity not executed'])
        return contract

    def unchanged(self):
        self.assertEqual((self.root/a.TARGET).read_bytes(), self.before)

    def test_happy_path_order_independence_and_backup(self):
        result = a.run_task(self.root, self.run, self.client)
        self.assertEqual(result['status'], 'applied_pending_unity_validation')
        self.assertEqual((self.run/'manifest.before.json').read_bytes(), self.before)
        self.assertEqual(json.loads((self.root/a.TARGET).read_bytes())['dependencies']['com.unity.inputsystem'], '1.17.0')
        self.assertEqual([(r, p) for r, p, _ in self.calls], [
            ('sol', 'design'), ('sol', 'implement-1'), ('sol', 'review-1'),
            ('sonnet', 'review-1'), ('sol', 'exchange-1'), ('sonnet', 'exchange-1'),
            ('sol', 'acceptance-1'), ('sol', 'final-report')])
        for _, phase, payload in self.calls:
            if phase.startswith('review'):
                self.assertNotIn('independent_reviews', payload)
            if phase.startswith('exchange'):
                self.assertEqual(set(payload['independent_reviews']), {'sol', 'sonnet'})

    def test_wrong_model_or_cli_failure_does_not_write(self):
        def failure(role, phase, payload):
            if role == 'sonnet':
                raise a.Stopped('Wrong model or CLI failure')
            return self.client(role, phase, payload)
        with self.assertRaises(a.Stopped):
            a.run_task(self.root, self.run, failure)
        self.unchanged()

    def test_out_of_scope_and_path_traversal(self):
        for path in ['../outside.json', '/tmp/other', a.TARGET]:
            content = self.before.decode().replace('1.12.0', '1.17.0').replace('2.0', '3.0')
            with self.assertRaises(a.Stopped):
                a.validate_candidate({'path': path, 'base_sha256': a.sha(self.before), 'content': content},
                                     self.before, a.sha(self.before))
        self.unchanged()

    def test_duplicate_json_key_refused(self):
        with self.assertRaises(a.Stopped):
            a.parse_json('{"verdict":"request_changes","verdict":"approve"}')

    def test_stale_review_refused(self):
        def stale(role, phase, payload):
            result = self.client(role, phase, payload)
            if phase == 'exchange-1':
                result['candidate_sha256'] = 'stale'
            return result
        with self.assertRaises(a.Stopped):
            a.run_task(self.root, self.run, stale)
        self.unchanged()

    def test_disagreement_bounded_no_apply(self):
        def disagree(role, phase, payload):
            result = self.client(role, phase, payload)
            if role == 'sonnet' and phase.startswith('exchange'):
                result['verdict'] = 'request_changes'
            return result
        with self.assertRaises(a.Stopped):
            a.run_task(self.root, self.run, disagree, rounds=2)
        self.assertEqual(sum(p.startswith('implement') for _, p, _ in self.calls), 2)
        self.unchanged()

    def test_repair_is_reviewed_again(self):
        def repair(role, phase, payload):
            result = self.client(role, phase, payload)
            if role == 'sonnet' and phase == 'exchange-1':
                result['verdict'] = 'request_changes'
            return result
        result = a.run_task(self.root, self.run, repair)
        self.assertEqual(result['status'], 'applied_pending_unity_validation')
        self.assertIn(('sol', 'review-2'), [(r, p) for r, p, _ in self.calls])
        self.assertIn(('sonnet', 'implement-2'), [(r, p) for r, p, _ in self.calls])

    def test_supervisor_can_stop_worker_approvals(self):
        def veto(role, phase, payload):
            response = self.client(role, phase, payload)
            if phase.startswith('acceptance'):
                response['verdict'] = 'request_changes'
            return response
        with self.assertRaises(a.Stopped):
            a.run_task(self.root, self.run, veto, rounds=1)
        self.unchanged()

    def test_sol_can_be_first_implementer(self):
        a.run_task(self.root, self.run, self.client, implementer='sol')
        self.assertIn(('sol', 'implement-1'), [(r, p) for r, p, _ in self.calls])

    def test_default_check_does_not_call_astra(self):
        a.connection_check(self.client)
        self.assertEqual([r for r, _, _ in self.calls], ['sol', 'sonnet'])

    def test_astra_requires_explicit_escalation_reason(self):
        with self.assertRaises(a.Stopped):
            a.run_task(self.root, self.run, self.client, supervisor='astra')
        self.assertEqual(self.calls, [])
        self.unchanged()
        a.run_task(self.root, self.run, self.client, supervisor='astra',
                   escalation_reason='Important architecture decision')
        self.assertIn(('astra', 'design'), [(r, p) for r, p, _ in self.calls])
        self.assertIn(('astra', 'acceptance-1'), [(r, p) for r, p, _ in self.calls])

    def test_final_report_failure_does_not_hide_application(self):
        def failed_report(role, phase, payload):
            if phase == 'final-report':
                raise a.Stopped('Report timeout')
            return self.client(role, phase, payload)
        outcome = a.run_task(self.root, self.run, failed_report)
        self.assertEqual(outcome['status'], 'applied_pending_supervisor_report')
        self.assertEqual(json.loads((self.root/a.TARGET).read_bytes())['dependencies']['com.unity.inputsystem'], '1.17.0')

    def test_user_change_not_overwritten(self):
        def edited(role, phase, payload):
            result = self.client(role, phase, payload)
            if role == 'sonnet' and phase == 'exchange-1':
                (self.root/a.TARGET).write_text('{"user":"change"}')
            return result
        with self.assertRaises(a.Stopped):
            a.run_task(self.root, self.run, edited)
        self.assertEqual((self.root/a.TARGET).read_text(), '{"user":"change"}')

    def test_change_between_original_read_and_snapshot(self):
        real_snapshot = a.snapshot
        def racing_snapshot(root):
            manifest = json.loads(self.before)
            manifest['dependencies']['user-added'] = '2.0'
            (root/a.TARGET).write_text(json.dumps(manifest))
            return real_snapshot(root)
        with patch.object(a, 'snapshot', side_effect=racing_snapshot):
            with self.assertRaises(a.Stopped):
                a.run_task(self.root, self.run, self.client)
        self.assertEqual(json.loads((self.root/a.TARGET).read_text())['dependencies']['user-added'], '2.0')
        self.assertEqual(self.calls, [])

    def test_sonnet_adapter_rejects_model_mismatch(self):
        def fake_process(command, prompt, cwd, timeout, stdout_path, stderr_path):
            write_stream(stdout_path, stream_fixture(model='claude-haiku-4-5-20251001'))
        with patch.object(a, 'find_cli', return_value=sys.executable), patch.object(a, 'bounded_process', side_effect=fake_process):
            client = a.Clients(self.root, self.run, 10)
            with self.assertRaises(a.Stopped):
                client('sonnet', 'check', {})

    def test_sonnet_adapter_extracts_json_response(self):
        def fake_process(command, prompt, cwd, timeout, stdout_path, stderr_path):
            self.assertIn('stream-json', command)
            self.assertIn('--verbose', command)
            write_stream(stdout_path, stream_fixture())
        with patch.object(a, 'find_cli', return_value=sys.executable), patch.object(a, 'bounded_process', side_effect=fake_process):
            client = a.Clients(self.root, self.run, 10)
            self.assertEqual(client('sonnet', 'check', {})['nonce'], 'abc')

    def test_missing_primary_model_not_accepted_from_usage(self):
        events = stream_fixture()
        del events[1]['message']['model']
        with self.assertRaises(a.Stopped):
            a.sonnet_stream('\n'.join(map(json.dumps, events)), a.MODELS['sonnet'])

    def test_result_from_different_session_refused(self):
        events = stream_fixture()
        events[-1]['session_id'] = 'other'
        with self.assertRaises(a.Stopped):
            a.sonnet_stream('\n'.join(map(json.dumps, events)), a.MODELS['sonnet'])

    def test_result_content_must_match_verified_primary(self):
        events = stream_fixture()
        events[-1]['result'] = '{"different":true}'
        with self.assertRaises(a.Stopped):
            a.sonnet_stream('\n'.join(map(json.dumps, events)), a.MODELS['sonnet'])

    def test_usage_only_old_format_is_insufficient(self):
        with self.assertRaises(a.Stopped):
            a.sonnet_stream(json.dumps(stream_fixture()[-1]), a.MODELS['sonnet'])

    def test_replace_failure_preserves_original(self):
        with patch.object(a.os, 'replace', side_effect=PermissionError('file locked')):
            with self.assertRaises(PermissionError):
                a.run_task(self.root, self.run, self.client)
        self.unchanged()
        self.assertEqual(list((self.root/'Packages').glob('*.tmp')), [])

    def test_connection_echo_required(self):
        self.assertEqual(a.connection_check(self.client)['status'], 'connection_check_passed')
        with self.assertRaises(a.Stopped):
            a.connection_check(lambda *args: {'nonce': 'wrong', 'status': 'ok'})
        self.unchanged()

    def test_timeout_kills_process(self):
        with self.assertRaises(a.Stopped):
            a.bounded_process([sys.executable, '-c', 'import time; time.sleep(30)'], '', self.root, .2,
                              self.run/'out.log', self.run/'err.log')

    def test_timeout_stops_child_before_delayed_write(self):
        marker = self.root/'child-wrote.txt'
        child = self.root/'child.py'
        child.write_text('import time\nfrom pathlib import Path\ntime.sleep(1.5)\nPath(' +
                         repr(str(marker)) + ').write_text("unexpected")\n')
        parent = self.root/'parent.py'
        parent.write_text('import subprocess, sys, time\nsubprocess.Popen([sys.executable, ' +
                          repr(str(child)) + '])\ntime.sleep(30)\n')
        with self.assertRaises(a.Stopped):
            a.bounded_process([sys.executable, str(parent)], '', self.root, .5,
                              self.run/'out.log', self.run/'err.log')
        time.sleep(1.3)
        self.assertFalse(marker.exists())

    def test_lock_stops_second_run_without_removing_lock(self):
        (self.root/'Collaboration/automation.lock').write_text('existing run')
        with patch.object(a, 'ROOT', self.root):
            self.assertEqual(a.main(['check']), 1)
        self.assertEqual((self.root/'Collaboration/automation.lock').read_text(), 'existing run')

    def test_pause_stops_before_cli_launch(self):
        (self.root/'Collaboration/PAUSE.json').write_text('{}')
        with patch.object(a, 'ROOT', self.root), patch.object(a, 'Clients') as clients:
            self.assertEqual(a.main(['check']), 2)
            clients.assert_not_called()
        client = a.Clients.__new__(a.Clients)
        client.root = self.root
        with patch.object(a, 'bounded_process') as launch:
            with self.assertRaises(a.Stopped):
                client('sonnet', 'check', {})
            launch.assert_not_called()


if __name__ == '__main__':
    unittest.main(verbosity=2)
