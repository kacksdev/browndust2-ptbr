"""Compare every native Windows executable icon frame with the approved ICO."""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path


def unpack(data, fmt, offset):
    return struct.unpack_from('<' + fmt, data, offset)


def ico_frames(data):
    reserved, kind, count = unpack(data, 'HHH', 0)
    if reserved != 0 or kind != 1 or count == 0:
        raise ValueError('Invalid ICO header')
    frames = []
    for i in range(count):
        w, h, colors, zero, planes, bits, size, offset = unpack(data, 'BBBBHHII', 6 + i * 16)
        payload = data[offset:offset + size]
        if len(payload) != size:
            raise ValueError('Truncated ICO frame')
        frames.append(((w, h, colors, zero, planes, bits), payload))
    return frames


def resources(data):
    if data[:2] != b'MZ':
        raise ValueError('Missing DOS header')
    pe, = unpack(data, 'I', 60)
    if data[pe:pe + 4] != b'PE\0\0':
        raise ValueError('Missing PE header')
    count, = unpack(data, 'H', pe + 6)
    optional_size, = unpack(data, 'H', pe + 20)
    optional = pe + 24
    magic, = unpack(data, 'H', optional)
    directory = optional + {0x10b: 96, 0x20b: 112}[magic]
    resource_rva, resource_size = unpack(data, 'II', directory + 16)
    sections = [unpack(data, 'IIII', optional + optional_size + i * 40 + 8) for i in range(count)]

    def offset(rva):
        for virtual_size, address, raw_size, raw_pointer in sections:
            if address <= rva < address + max(virtual_size, raw_size):
                delta = rva - address
                if delta >= raw_size:
                    raise ValueError('Resource outside raw section')
                return raw_pointer + delta
        raise ValueError('Unmapped resource RVA')

    if resource_size == 0:
        raise ValueError('Executable has no native resources')
    base = offset(resource_rva)
    result = {}

    def visit(relative, keys):
        if len(keys) > 3 or relative >= resource_size:
            raise ValueError('Invalid resource tree')
        named, ids = unpack(data, 'HH', base + relative + 12)
        for i in range(named + ids):
            name, target = unpack(data, 'II', base + relative + 16 + 8 * i)
            if name & 0x80000000:
                continue  # Native icon IDs and languages are numeric.
            path = keys + (name,)
            if target & 0x80000000:
                visit(target & 0x7fffffff, path)
            else:
                rva, size, _, _ = unpack(data, 'IIII', base + target)
                start = offset(rva)
                payload = data[start:start + size]
                if len(payload) != size:
                    raise ValueError('Truncated resource')
                result[path] = payload

    visit(0, ())
    return result


def audit(executable, expected):
    frames = ico_frames(expected.read_bytes())
    if executable.suffix.lower() == '.zip':
        with zipfile.ZipFile(executable) as archive:
            if archive.namelist() != ['BD2PTBR.exe']:
                raise ValueError('Application ZIP must contain only BD2PTBR.exe')
            binary = archive.read('BD2PTBR.exe')
    else:
        binary = executable.read_bytes()
    native = resources(binary)
    groups = [(keys, value) for keys, value in native.items() if keys[0] == 14]
    if not groups:
        raise ValueError('No RT_GROUP_ICON')
    checks = []
    for keys, group in groups:
        reserved, kind, count = unpack(group, 'HHH', 0)
        if (reserved, kind, count) != (0, 1, len(frames)):
            raise ValueError(f'Native group {keys} has {count} frames; expected {len(frames)}')
        for i, (header, expected_payload) in enumerate(frames):
            entry = unpack(group, 'BBBBHHIH', 6 + i * 14)
            native_payload = native.get((3, entry[7], keys[2]))
            match = entry[:6] == header and entry[6] == len(expected_payload) and native_payload == expected_payload
            checks.append({'group': keys[1], 'language': keys[2], 'size': header[0] or 256,
                           'matches': match, 'sha256': hashlib.sha256(native_payload or b'').hexdigest()})
    return {'status': 'PASS' if all(c['matches'] for c in checks) else 'FAIL',
            'executable': str(executable.resolve()), 'expectedIcon': str(expected.resolve()),
            'groups': len(groups), 'frames': checks}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('executable', type=Path)
    parser.add_argument('icon', type=Path)
    parser.add_argument('--report', type=Path)
    args = parser.parse_args()
    report = audit(args.executable, args.icon)
    text = json.dumps(report, indent=2)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(text + '\n', encoding='utf-8')
    print(text)
    return 0 if report['status'] == 'PASS' else 1


if __name__ == '__main__':
    raise SystemExit(main())
