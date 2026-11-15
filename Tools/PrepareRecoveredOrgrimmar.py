"""Copy a visual-only dependency closure from an AssetRipper export, never game scripts."""
import argparse
import hashlib
import json
import re
import shutil
from collections import Counter, deque
from pathlib import Path

PROJECT = Path(__file__).resolve().parent.parent
GUID = re.compile(r"guid: ([0-9a-f]{32})")
DOCUMENT = re.compile(r"(?m)^--- !u!(\d+) &(-?\d+)[^\n]*\n")
ALLOWED_COMPONENTS = {1, 4, 23, 33, 111, 137}  # Objects/transforms, renderers/meshes, legacy animation.


def visual_prefab(text):
    matches = list(DOCUMENT.finditer(text))
    removed = {m.group(2) for m in matches if int(m.group(1)) not in ALLOWED_COMPONENTS}
    counts = Counter(int(m.group(1)) for m in matches if m.group(2) in removed)
    chunks = [text[:matches[0].start()]]
    for i, match in enumerate(matches):
        if match.group(2) in removed:
            continue
        chunk = text[match.start():matches[i + 1].start() if i + 1 < len(matches) else len(text)]
        chunk = re.sub(r"(?m)^  - component: \{fileID: (-?\d+)\}\n",
                       lambda m: "" if m.group(1) in removed else m.group(), chunk)
        chunk = re.sub(r"(?m)^  m_Layer: \d+$", "  m_Layer: 0", chunk)
        chunk = re.sub(r"(?m)^  m_TagString: .+$", "  m_TagString: Untagged", chunk)
        chunks.append(chunk)
    return "".join(chunks), dict(counts)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("export_assets", type=Path)
    args = parser.parse_args()
    source = args.export_assets.resolve()
    destination = PROJECT / "Assets/_Project/RecoveredOrgrimmar"
    index = {}
    for meta in source.rglob("*.meta"):
        if not meta.with_suffix("").is_file():
            continue
        match = GUID.search(meta.read_text())
        if match:
            index[match.group(1)] = meta.with_suffix("")
    original = source / "AssetBundles/initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d/ORG.prefab"
    cleaned, removed = visual_prefab(original.read_text())
    queue = deque([original])
    visited = set()
    records = []
    unresolved = set()
    while queue:
        file = queue.popleft()
        if file in visited:
            continue
        visited.add(file)
        if file.suffix not in {".prefab", ".asset", ".mat", ".anim", ".png", ".jpg", ".tga"}:
            raise ValueError("Unexpected visual dependency: " + str(file))
        relative = file.relative_to(source)
        output = destination / relative
        output.parent.mkdir(parents=True, exist_ok=True)
        text = cleaned if file == original else None
        if file.suffix in {".prefab", ".asset", ".mat", ".anim"}:
            text = file.read_text() if text is None else text
            if file.suffix == ".prefab" and file != original:
                raise ValueError("Unexpected nested prefab; inspect it before copying")
            for guid in set(GUID.findall(text)):
                if guid in index:
                    dependency = index[guid]
                    if dependency.suffix == ".shader":
                        # Compiled client shaders are unavailable in this export. Rebind in Unity.
                        continue
                    queue.append(dependency)
                elif not guid.startswith("0000000000000000"):
                    unresolved.add(guid)
            if file.suffix == ".mat":
                text = re.sub(r"(?m)^  m_Shader: .+$", "  m_Shader: {fileID: 0}", text)
            output.write_text(text)
        else:
            shutil.copyfile(file, output)
        meta_text = file.with_name(file.name + ".meta").read_text()
        meta_text = re.sub(r"(?m)^  assetBundleName:.*$", "  assetBundleName:", meta_text)
        output.with_name(output.name + ".meta").write_text(meta_text)
        records.append({"source": str(relative), "destination": str(output.relative_to(PROJECT)),
                        "source_sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
    # A missing compiled shader is expected; all visual asset references must resolve.
    expected = {"0000000deadbeef15deadf00d0000000"}  # AssetRipper's verified missing-shader marker.
    unexpected = unresolved - expected
    if unexpected:
        raise ValueError("Unresolved exported dependency GUIDs: " + ",".join(sorted(unexpected)))
    report = {"root": str(original.relative_to(source)), "visual_files": len(records),
              "removed_component_classes": removed, "unresolved_shader_guids": sorted(unresolved),
              "files": records}
    path = PROJECT / "Docs/Recovery/ORG-import-manifest.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"copied": len(records), "types": dict(Counter(Path(r['destination']).suffix for r in records)),
                      "removed_component_classes": removed}))


if __name__ == "__main__":
    main()
