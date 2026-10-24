"""Audit the consolidated release snapshot; semantic review is a separate task."""
import collections
import hashlib
import json
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    catalog = ROOT / "catalog"
    manifest = json.loads((catalog / "manifest.json").read_text(encoding="utf-8"))
    expected_names = {item["file"] for item in manifest["files"]}
    if expected_names != {p.name for p in catalog.glob("*.jsonl")}:
        raise ValueError("Catalog files differ from the manifest")
    records = {}
    statuses = collections.Counter()
    for item in manifest["files"]:
        path = catalog / item["file"]
        if path.parent != catalog or not path.is_file():
            raise ValueError("Invalid catalog file")
        data = path.read_bytes()
        if len(data) != item["bytes"] or hashlib.sha256(data).hexdigest() != item["sha256"]:
            raise ValueError("Catalog hash or size mismatch: " + item["file"])
        for number, line in enumerate(data.decode("utf-8").splitlines(), 1):
            row = json.loads(line)
            key, source, target = (row.get(name) for name in ("key", "source", "translation"))
            if not isinstance(key, str) or not key.strip() or key in records:
                raise ValueError(f"Missing or duplicate key: {path.name}:{number}")
            if not isinstance(source, str) or not isinstance(target, str) or (source.strip() and not target.strip()):
                raise ValueError("Missing source or translation: " + key)
            if unicodedata.normalize("NFC", target) != target:
                raise ValueError("Translation is not NFC: " + key)
            if hashlib.sha256(source.encode("utf-8")).hexdigest() != row["sourceHash"]:
                raise ValueError("Source identity mismatch: " + key)
            records[key] = row
            statuses[row.get("status", "unspecified")] += 1
    music = {r["source"] for r in records.values() if r["scope"] == "common"
             and r["table"] == "NameTextTable" and 4310000 <= r["id"] <= 4339999}
    protected = [r for r in records.values() if r["scope"] == "common" and (
        (r["table"] == "NameTextTable" and 4310000 <= r["id"] <= 4339999)
        or (r["table"] in ("NameTextTable", "LocalTextTable")
            and 4000000 <= r["id"] <= 4399999 and r["source"] in music))]
    if len(records) != manifest["expectedEffectiveEntries"]:
        raise ValueError("Effective entry count mismatch")
    if len(protected) != manifest["protectedMusicKeys"] or any(r["source"] != r["translation"] for r in protected):
        raise ValueError("Protected music identity mismatch")
    report = dict(schemaVersion=1, modVersion=manifest["modVersion"],
                  effectiveEntries=len(records), statuses=dict(sorted(statuses.items())),
                  protectedMusicKeys=len(protected), duplicateKeys=0, invalidRecords=0,
                  unicodeFailures=0, hashFailures=0, ready=True,
                  semanticReview="Not certified by this structural audit",
                  runtimeMarkers="Validated separately by RuntimeCatalogAudit using PlaceholderValidator.cs")
    text = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    (catalog / "audit-report.json").write_text(text, encoding="utf-8")
    print(text, end="")


if __name__ == "__main__":
    main()
