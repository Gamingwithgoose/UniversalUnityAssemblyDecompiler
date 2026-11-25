"""Export locally installed Unity assets without modifying game files."""
import collections
import json
import pathlib
import re
import sys
import UnityPy

def safe(name):
    return re.sub(r'[^\w. -]', '_', str(name)).strip(' .')[:100] or 'unnamed'

def main():
    root = pathlib.Path(sys.argv[1]).resolve()
    out = pathlib.Path(sys.argv[2]).resolve()
    out.mkdir(parents=True, exist_ok=True)
    # UnityPy resolves external references and streamed resources from the data directory.
    env = UnityPy.load(str(root))
    counts = collections.Counter()
    failures = 0
    manifest = out / 'asset_index.jsonl'
    with manifest.open('w', encoding='utf-8') as index:
        for obj in env.objects:
            kind = obj.type.name
            source = str(getattr(obj.assets_file, 'name', 'unknown'))
            name = 'unnamed'
            paths = []
            try:
                if kind in ('Texture2D', 'Sprite', 'AudioClip', 'TextAsset', 'Mesh', 'MonoBehaviour', 'AnimationClip', 'AnimatorController', 'Material', 'GameObject', 'MonoScript'):
                    data = obj.read()
                    name = getattr(data, 'm_Name', '') or getattr(data, 'name', '') or kind
                    folder = out / {'Texture2D':'Textures','Sprite':'Sprites','AudioClip':'Audio','TextAsset':'TextAssets','Mesh':'Meshes','MonoBehaviour':'ScriptableObjects'}.get(kind, kind)
                    folder.mkdir(exist_ok=True)
                    stem = safe(source) + '_' + str(obj.path_id) + '_' + safe(name)
                    def write(ext, content):
                        p = folder / (stem + ext)
                        if isinstance(content, str):
                            p.write_text(content, encoding='utf-8')
                        else:
                            p.write_bytes(content)
                        paths.append(str(p.relative_to(out)))
                    if kind in ('Texture2D', 'Sprite'):
                        p = folder / (stem + '.png')
                        data.image.save(p)
                        paths.append(str(p.relative_to(out)))
                    elif kind == 'AudioClip':
                        for n, sample in data.samples.items():
                            write('_' + safe(n), sample)
                    elif kind == 'TextAsset':
                        raw = data.m_Script
                        write('.txt' if isinstance(raw, str) else '.bin', raw)
                    elif kind == 'Mesh':
                        write('.obj', data.export())
                    else:
                        write('.json', json.dumps(obj.read_typetree(), ensure_ascii=False, indent=2, default=str))
                    counts[kind] += 1
            except Exception as exc:
                failures += 1
                # Preserve raw serialized bytes when a stripped type tree or unsupported decoder prevents conversion.
                try:
                    folder = out / 'Raw' / safe(kind)
                    folder.mkdir(parents=True, exist_ok=True)
                    p = folder / (safe(source) + '_' + str(obj.path_id) + '.bin')
                    p.write_bytes(obj.get_raw_data())
                    paths.append(str(p.relative_to(out)))
                except Exception:
                    pass
                print(f'WARN {kind} {source}:{obj.path_id}: {exc}', flush=True)
            index.write(json.dumps({'type':kind,'name':str(name),'source':source,'pathId':obj.path_id,'files':paths}, ensure_ascii=False) + '\n')
    print(f'Exported: {dict(counts)}; conversion warnings: {failures}', flush=True)
    if not counts and not failures:
        raise RuntimeError('No supported assets found in selected game data folder.')
    if not counts:
        raise RuntimeError('No assets converted; check warnings and Raw fallback files.')

if __name__ == '__main__':
    main()
