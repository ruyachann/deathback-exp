"""Read-only direct UPM registry/lock check. No automatic upgrades."""
import argparse, json, re
from pathlib import Path
from urllib.request import urlopen, Request

def report(root, fetch):
    declared = json.loads((root/'Packages/manifest.json').read_text(encoding='utf-8-sig'))['dependencies']
    locked = json.loads((root/'Packages/packages-lock.json').read_text(encoding='utf-8-sig'))['dependencies']
    rows = []
    for name, pinned in sorted(declared.items()):
        if not re.fullmatch(r'com\.unity\.[a-z0-9.-]+', name):
            raise ValueError('Only Unity registry package names allowed: '+name)
        row = {'package':name, 'pinned':pinned, 'resolved':locked.get(name,{}).get('version')}
        try:
            registry = fetch(name)
            row['listed'] = pinned in registry.get('versions', {})
            row['registry_tags'] = registry.get('dist-tags', {})
            row['needs_review'] = row['resolved'] != pinned or not row['listed']
        except Exception as error:
            row['error'] = str(error); row['needs_review'] = True
        rows.append(row)
    return {'policy':'read-only; registry tags are information, not compatibility approval', 'packages':rows}

def fetch(name):
    request = Request('https://packages.unity.com/'+name, headers={'User-Agent':'Deathback dependency check'})
    with urlopen(request, timeout=20) as response:
        return json.load(response)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--root',type=Path,default=Path(__file__).resolve().parent.parent)
    args=parser.parse_args()
    result=report(args.root, fetch)
    print(json.dumps(result,ensure_ascii=False,indent=2))
    return int(any(p['needs_review'] for p in result['packages']))

if __name__=='__main__':raise SystemExit(main())
