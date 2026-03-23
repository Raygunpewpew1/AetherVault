#!/usr/bin/env python3
"""
Normalize MTGJSON AllPrintings SQLite columns that should be JSON arrays but sometimes
arrive as comma-separated text (e.g. availability: "mtgo, paper").

Run once on the extracted database before FTS/index steps. Idempotent for already-valid arrays.

Usage: python3 normalize_mtg_sqlite_columns.py AllPrintings.sqlite
"""
from __future__ import annotations

import json
import sqlite3
import sys

# Columns the app treats as string arrays in search / CardMapper.
LIST_COLUMNS = ("availability", "finishes", "keywords")

TABLES = ("cards", "tokens")


def normalize_cell(value: str) -> str:
    """Return canonical JSON array text, or original if not a parseable list/CSV."""
    s = value.strip()
    if not s:
        return "[]"
    try:
        parsed = json.loads(s)
        if isinstance(parsed, list):
            return json.dumps(parsed, separators=(",", ":"))
        return value
    except json.JSONDecodeError:
        pass
    parts = [p.strip() for p in s.split(",") if p.strip()]
    return json.dumps(parts, separators=(",", ":"))


def column_exists(conn: sqlite3.Connection, table: str, column: str) -> bool:
    cur = conn.execute(f'PRAGMA table_info("{table}")')
    return any(row[1] == column for row in cur.fetchall())


def normalize_table(conn: sqlite3.Connection, table: str) -> tuple[int, str]:
    """Returns (rows_updated, summary line)."""
    total = 0
    for col in LIST_COLUMNS:
        if not column_exists(conn, table, col):
            continue
        cur = conn.execute(f'SELECT rowid, "{col}" FROM "{table}"')
        updates: list[tuple[str, int]] = []
        for rowid, val in cur:
            if val is None or not str(val).strip():
                continue
            new_val = normalize_cell(str(val))
            if new_val != val:
                updates.append((new_val, rowid))
        if updates:
            conn.executemany(f'UPDATE "{table}" SET "{col}" = ? WHERE rowid = ?', updates)
            total += len(updates)
    return total, table


def main() -> int:
    path = sys.argv[1] if len(sys.argv) > 1 else "AllPrintings.sqlite"
    conn = sqlite3.connect(path)
    try:
        grand = 0
        for table in TABLES:
            n, t = normalize_table(conn, table)
            grand += n
            if n:
                print(f"normalize_mtg_sqlite_columns: updated {n} row/column cells in {t}")
        conn.commit()
        if grand == 0:
            print("normalize_mtg_sqlite_columns: no changes (already JSON or empty)")
    finally:
        conn.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
