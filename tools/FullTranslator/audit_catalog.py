"""Compatibility entry point for the consolidated structural audit."""
import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).resolve().parents[1] / "audit_public_catalog.py"), run_name="__main__")
