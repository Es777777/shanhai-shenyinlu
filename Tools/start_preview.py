"""Open the local preview, reusing a running preview server when possible."""
import argparse
import functools
import http.server
import urllib.error
import urllib.parse
import urllib.request
import webbrowser
from serve import Handler, ROOT


def same_served_file(url, file):
    """Compare bounded streams so a reused address really serves this package."""
    with urllib.request.urlopen(url, timeout=5) as response, file.open('rb') as local:
        while True:
            block=local.read(1024*1024)
            if not block:
                return response.read(1)==b''
            if response.read(len(block))!=block:
                return False


def matches_running_build(url, directory, entry, h5):
    if not same_served_file(url,entry):
        return False
    if h5:
        for file in sorted((directory/'Build').rglob('*')):
            if file.is_file() and not same_served_file(
                    urllib.parse.urljoin(url,urllib.parse.quote(file.relative_to(directory).as_posix())),file):
                return False
    return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int)
    parser.add_argument('--h5', action='store_true', help='Run the engine-exported WebGL build')
    parser.add_argument('--no-browser', action='store_true')
    args = parser.parse_args()
    args.port = args.port or (8766 if args.h5 else 8765)
    directory = ROOT
    if args.h5:
        directory = ROOT / 'Builds' / 'H5' if (ROOT / 'Builds' / 'H5').is_dir() else ROOT
    relative_url = '/' if args.h5 else '/Preview/'
    url = f'http://127.0.0.1:{args.port}{relative_url}'
    entry = directory / ('index.html' if args.h5 else 'Preview/index.html')
    if not entry.is_file() or (args.h5 and not any((directory / 'Build').glob('*.wasm*'))):
        raise SystemExit('Game files are missing. Extract the complete package or build H5 first.')
    try:
        running = matches_running_build(url,directory,entry,args.h5)
        if not running:
            raise SystemExit(
                f'Another game build is using {url}\n'
                'Close the previous game server window, then start this package again.\n'
                'Reuse the same address to keep access to your browser saves.\n'
                '检测到同一地址正在运行其他版本。请关闭旧游戏的服务窗口，再启动本包；'
                '沿用原地址可继续访问原浏览器存档。')
    except (OSError, UnicodeError, urllib.error.URLError):
        running = False
    if running:
        print(f'Preview already running: {url}', flush=True)
        if not args.no_browser:
            webbrowser.open(url)
        return
    try:
        server = http.server.ThreadingHTTPServer(
            ('127.0.0.1', args.port), functools.partial(Handler, directory=str(directory)))
    except OSError as error:
        raise SystemExit(f'Cannot start local preview on port {args.port}: {error}')
    print(f'Open {url}\nKeep this window open while playing. Ctrl+C stops the server.', flush=True)
    if not args.no_browser:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == '__main__':
    main()
