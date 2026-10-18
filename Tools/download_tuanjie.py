"""Fetch the official Tuanjie 1.10.4 editor using resumable HTTP byte ranges."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import json
import re
import shutil
import subprocess

ROOT = Path('F:/Unity/Installers')
NAME = 'TuanjieSetup64-2022.3.62t16.exe'
URL = 'https://download.tuanjie.cn/download_tuanjie/ab17684acf1d/Windows64EditorInstaller/' + NAME
TOTAL = 4144214424


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    target = ROOT / NAME
    if target.exists() and target.stat().st_size == TOTAL:
        print('Editor download already complete.', flush=True)
        return
    parts = ROOT / (NAME + '.parts')
    parts.mkdir(exist_ok=True)
    plan_path = parts / 'plan.json'
    if plan_path.exists():
        plan = json.loads(plan_path.read_text())
    else:
        prefix = target.stat().st_size if target.exists() else 0
        if prefix >= TOTAL:
            raise RuntimeError('Unexpected existing installer size')
        chunk = (TOTAL - prefix + 7) // 8
        plan = {'prefix': prefix, 'ranges': [
            [start, min(start + chunk, TOTAL) - 1]
            for start in range(prefix, TOTAL, chunk)]}
        plan_path.write_text(json.dumps(plan))

    def fetch(item):
        index, (start, end) = item
        output = parts / f'{index}.part'
        expected = end - start + 1
        if output.exists() and output.stat().st_size == expected:
            return index
        tail, headers = parts / f'{index}.tail', parts / f'{index}.headers'
        # Recover a valid partial response left by an interrupted download.
        if tail.exists() and tail.stat().st_size and headers.exists():
            received = output.stat().st_size if output.exists() else 0
            ranges = re.findall(r'content-range:\s*bytes (\d+)-(\d+)/(\d+)', headers.read_text(errors='replace'), re.I)
            if ranges and tuple(map(int, ranges[-1])) == (start + received, end, TOTAL):
                if received + tail.stat().st_size > expected:
                    raise RuntimeError('Saved response exceeds its HTTP range')
                with output.open('ab') as destination, tail.open('rb') as source:
                    shutil.copyfileobj(source, destination, 4 * 1024 * 1024)
                tail.unlink()
                if output.stat().st_size == expected:
                    return index
        for attempt in range(8):
            received = output.stat().st_size if output.exists() else 0
            if received > expected:
                raise RuntimeError(f'Range {index} is too large')
            request_start = start + received
            command = ['curl.exe', '--location', '--fail',
                       '--connect-timeout', '20', '--speed-limit', '131072', '--speed-time', '40',
                       '--range', f'{request_start}-{end}', '--dump-header', str(headers),
                       '--output', str(tail), URL]
            with (parts / f'{index}.log').open('wb') as log:
                subprocess.run(command, stdout=log, stderr=log)
            body_size = tail.stat().st_size if tail.exists() else 0
            header_text = headers.read_text(errors='replace') if headers.exists() else ''
            ranges = re.findall(r'content-range:\s*bytes (\d+)-(\d+)/(\d+)', header_text, re.I)
            if body_size:
                if not ranges or tuple(map(int, ranges[-1])) != (request_start, end, TOTAL):
                    raise RuntimeError('Server did not honor the requested HTTP range')
                if received + body_size > expected:
                    raise RuntimeError('HTTP range returned too much data')
                with output.open('ab') as destination, tail.open('rb') as source:
                    shutil.copyfileobj(source, destination, 4 * 1024 * 1024)
                tail.unlink()
            if output.exists() and output.stat().st_size == expected:
                return index
        raise RuntimeError(f'Range {index} is incomplete; run this command again to resume')

    print(f'Reusing {plan["prefix"] // 1048576} MiB; downloading {len(plan["ranges"])} ranges.', flush=True)
    with ThreadPoolExecutor(max_workers=6) as pool:
        for task in as_completed([pool.submit(fetch, item) for item in enumerate(plan['ranges'])]):
            print(f'Range {task.result() + 1}/{len(plan["ranges"])} complete', flush=True)
    # Only truncate this tool's own incomplete installer, never an installed editor.
    with target.open('r+b' if target.exists() else 'w+b') as output:
        if output.seek(0, 2) < plan['prefix']:
            raise RuntimeError('The saved installer prefix is missing')
        output.truncate(plan['prefix'])
        output.seek(plan['prefix'])
        for index in range(len(plan['ranges'])):
            with (parts / f'{index}.part').open('rb') as source:
                shutil.copyfileobj(source, output, 4 * 1024 * 1024)
    if target.stat().st_size != TOTAL:
        raise RuntimeError('Final installer size mismatch')
    print('Editor download complete. Verify Authenticode before installation.', flush=True)


if __name__ == '__main__':
    main()
