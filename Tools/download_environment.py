"""Download official Unity installers; resume via HTTP Range when available."""
from pathlib import Path
import subprocess
import concurrent.futures

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / 'Tools' / '.downloads'
CACHE.mkdir(exist_ok=True)
BASE = 'https://download.unitychina.cn/download_unity/4af31df58517/'
FILES = {
    'UnitySetup64-2022.3.62f1.exe': BASE + 'Windows64EditorInstaller/UnitySetup64-2022.3.62f1.exe',
    'UnitySetup-WebGL-Support-for-Editor-2022.3.62f1.exe': BASE + 'TargetSupportInstaller/UnitySetup-WebGL-Support-for-Editor-2022.3.62f1.exe',
}

def download(item):
    name, url = item
    dest = CACHE / name
    expected = 3754412664 if name.startswith('UnitySetup64') else 587832392
    if dest.exists() and dest.stat().st_size == expected:
        print(f'Cached {name}: {dest.stat().st_size}', flush=True)
        return
    subprocess.run(['curl.exe','--fail','--location','--retry','3','--continue-at','-',
                    '--connect-timeout','30','--output',str(dest),url],check=True)
    if dest.stat().st_size != expected:
        raise RuntimeError('Incomplete download: '+name)
    print(f'Ready {dest}', flush=True)

if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        list(pool.map(download, FILES.items()))
