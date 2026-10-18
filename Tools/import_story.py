"""Read the supplied workbook without changing it; merge reviewed runtime content."""
import argparse
import json
import re
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
NS = {'m': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}

def read_workbook(path):
    result = {}
    with zipfile.ZipFile(path) as archive:
        strings = []
        if 'xl/sharedStrings.xml' in archive.namelist():
            strings = [''.join(s.itertext()) for s in ET.fromstring(archive.read('xl/sharedStrings.xml')).findall('m:si', NS)]
        rels = {r.attrib['Id']: r.attrib['Target'].lstrip('/') for r in ET.fromstring(archive.read('xl/_rels/workbook.xml.rels'))}
        for sheet in ET.fromstring(archive.read('xl/workbook.xml')).find('m:sheets', NS):
            rid = sheet.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']
            target = rels[rid]
            if not target.startswith('xl/'):
                target = 'xl/' + target
            records = []
            for row in ET.fromstring(archive.read(target)).findall('.//m:sheetData/m:row', NS):
                record = {}
                for cell in row:
                    if cell.find('m:f', NS) is not None:
                        continue
                    value, inline = cell.find('m:v', NS), cell.find('m:is', NS)
                    text = value.text if value is not None else ''.join(inline.itertext()) if inline is not None else ''
                    if cell.attrib.get('t') == 's' and text:
                        text = strings[int(text)]
                    if text:
                        record[re.sub(r'\d', '', cell.attrib['r'])] = text
                if record:
                    record['_row'] = int(row.attrib['r'])
                    records.append(record)
            result[sheet.attrib['name']] = records
    return result

def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def convert(path=None):
    snapshot = ROOT / 'Source/workbook_snapshot.json'
    if path is not None:
        source = read_workbook(path)
        write_json(snapshot, source)
    else:
        source = json.loads(snapshot.read_text(encoding='utf-8'))
    authored = json.loads((ROOT / 'Source/content.json').read_text(encoding='utf-8'))
    variable_rows = [r for r in source['状态变量'] if r.get('B') in ['mcv', 'candle', 'fragments', 'pacts', 'ending']]
    variables = [{'key': r['B'], 'name': r['A'], 'initial': int(r['E']), 'min': int(r['F']), 'max': int(r['G'])} for r in variable_rows]
    keys = {r['name']: r['key'] for r in variables}
    edges = []
    for r in source['剧情链']:
        if not re.fullmatch('E[0-9]+', r.get('A', '')):
            continue
        conditions = []
        for a, b, c in [('I', 'J', 'K'), ('L', 'M', 'N')]:
            if r.get(a) in keys:
                conditions.append({'key': keys[r[a]], 'op': r[b], 'value': int(r[c])})
        edges.append({'id': r['A'], 'from': r['C'], 'to': r['D'], 'kind': r['E'], 'text': r.get('F', ''), 'priority': int(r.get('O', 10)), 'conditions': conditions, 'any': r.get('H') == '任一满足'})
    nodes = []
    previous_background = 'tianque'
    for r in source['幕节点']:
        if not re.fullmatch('N[0-9]+', r.get('A', '')):
            continue
        node_id = r['A']
        assets = r.get('L', '').split(';')
        background = next((a[3:].lower() for a in assets if a.startswith('BG_')), previous_background)
        previous_background = background
        changes = []
        for name, op, number in re.findall(r'(\S+)\s+(增加|减少|设为)\s+(-?\d+)', r.get('J', '')):
            changes.append({'key': keys[name], 'op': 'set' if op == '设为' else 'add', 'value': int(number) * (-1 if op == '减少' else 1)})
        if node_id == 'N303':
            changes = []  # Reviewed on 2026-10-16: choice nodes already settle +/-8.
        item = authored['nodes'].get(node_id)
        if item is None:
            raise ValueError(f'Missing authored content for {node_id}')
        node = {'id': node_id, 'title': r['B'], 'plot': r['C'], 'chapter': r.get('D', ''), 'kind': r['E'], 'summary': r.get('G', ''), 'background': background, 'characters': [a[5:].lower() for a in assets if a.startswith('CHAR_')], 'music': r.get('P', 'M_PROLOGUE').replace('M_', '').lower(), 'changes': changes, 'lines': [], 'mechanic': '', 'targets': [], 'choices': [], 'unlocks': [], 'effects': r.get('O', '')}
        node.update(item)
        node['lines'] = [{'speaker': line[0], 'text': line[1], 'expression': line[2] if len(line)>2 else 0} for line in item['lines']]
        nodes.append(node)
    story = {'version': 1, 'title': '山海神隐录', 'entry': 'N001', 'variables': variables, 'nodes': nodes, 'edges': edges, 'codex': authored['codex']}
    validate(story)
    write_json(ROOT / 'Assets/Resources/Story/story.json', story)
    print(f'Imported {len(nodes)} nodes, {len(edges)} edges, {sum(len(n["lines"]) for n in nodes)} dialogue lines.')

def validate(story):
    ids = [n['id'] for n in story['nodes']]
    if len(ids) != len(set(ids)):
        raise ValueError('Duplicate node IDs')
    for edge in story['edges']:
        assert edge['from'] in ids and edge['to'] in ids, edge
    visited, pending = set(), [story['entry']]
    while pending:
        current = pending.pop()
        if current in visited:
            continue
        visited.add(current)
        pending.extend(e['to'] for e in story['edges'] if e['from'] == current)
    assert visited == set(ids), f'Unreachable nodes: {set(ids) - visited}'
    for node in story['nodes']:
        assert node['lines'], node['id']
        for choice in node['choices']:
            assert choice['to'] in ids, choice

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('workbook', nargs='?', type=Path)
    args = parser.parse_args()
    default_workbook = ROOT.parent / '山海神隐录_Unity分支剧情填写模板.xlsx'
    convert(args.workbook or (default_workbook if default_workbook.is_file() else None))
