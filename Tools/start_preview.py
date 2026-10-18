"""Open the local preview, reusing a running preview server when possible."""
import argparse
import functools
import http.server
import urllib.error
import urllib.request
import webbrowser
from serve import Handler, ROOT


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
    marker = '<title>山海神隐录</title>' if args.h5 else '山海神隐录 · 浏览器预览'
    url = f'http://127.0.0.1:{args.port}{relative_url}'
    try:
        with urllib.request.urlopen(url, timeout=2) as response:
            running = marker in response.read(65536).decode('utf-8')
    except (OSError, UnicodeError, urllib.error.URLError):
        running = False
    if running:
        print(f'Preview already running: {url}', flush=True)
        if not args.no_browser:
            webbrowser.open(url)
        return
    entry = directory / ('index.html' if args.h5 else 'Preview/index.html')
    if not entry.is_file() or (args.h5 and not any((directory / 'Build').glob('*.wasm*'))):
        raise SystemExit('Game files are missing. Extract the complete package or build H5 first.')
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
