import base64
import re
import uuid
from pathlib import Path
import sys

# ================= CONFIG =================
INPUT_SQL = "oracle_inserts.sql"
OUTPUT_SQL = "postgres_inserts.sql"

SCHEMA = "KIOSK"
TABLE = "USERS"

PASSWORD_COLUMNS = {"PASS"}
# ==========================================


def die(message):
    print("❌", message)
    sys.exit(1)


def is_base64(s):
    try:
        return base64.b64encode(base64.b64decode(s)).decode() == s
    except Exception:
        return False


def decode_base64(s):
    return base64.b64decode(s).decode("utf-8")


def format_uuid(u):
    """
    Convert 32-char Oracle UUID to PostgreSQL UUID
    """
    return str(uuid.UUID(u.replace("-", "")))


def split_values(value_string):
    """
    Safely split VALUES(...) handling commas inside quotes
    """
    values = []
    buffer = ""
    inside_quotes = False

    for char in value_string:
        if char == "'":
            inside_quotes = not inside_quotes

        if char == "," and not inside_quotes:
            values.append(buffer.strip())
            buffer = ""
        else:
            buffer += char

    values.append(buffer.strip())
    return values


def parse_insert(sql):
    """
    Parse a single Oracle INSERT statement
    """
    pattern = (
        r"INSERT\s+INTO\s+(\w+)\s*"
        r"\((.*?)\)\s*"
        r"VALUES\s*\((.*?)\)\s*;"
    )

    match = re.search(pattern, sql, flags=re.I | re.S)
    if not match:
        return None

    table, columns, values = match.groups()
    columns = [c.strip().upper() for c in columns.split(",")]
    values = split_values(values)

    return columns, values


def convert_value(value, column):
    """
    Convert Oracle value → PostgreSQL value
    """
    if value.upper() == "NULL":
        return "NULL"

    if value.startswith("'") and value.endswith("'"):
        raw = value[1:-1]

        # 32-char UUID
        if re.fullmatch(r"[A-Fa-f0-9]{32}", raw):
            return f"'{format_uuid(raw)}'::uuid"

        # Base64 password
        if column in PASSWORD_COLUMNS and is_base64(raw):
            return f"'{decode_base64(raw)}'"

        return f"'{raw}'"

    # NUMBER(1) → BOOLEAN
    if value == "1":
        return "TRUE"
    if value == "0":
        return "FALSE"

    return value


def main():
    if not Path(INPUT_SQL).exists():
        die(f"{INPUT_SQL} not found")

    sql_text = Path(INPUT_SQL).read_text(encoding="utf-8")

    inserts = re.findall(
        r"INSERT\s+INTO\s+.*?;",
        sql_text,
        flags=re.I | re.S
    )

    print(f"🔎 Found {len(inserts)} INSERT statements")

    if not inserts:
        die("No INSERT statements found")

    output = []
    converted = 0

    for idx, insert_sql in enumerate(inserts, 1):
        parsed = parse_insert(insert_sql)
        if not parsed:
            print(f"⚠️ Skipped INSERT #{idx}")
            continue

        columns, values = parsed

        if len(columns) != len(values):
            print(f"⚠️ Column/value mismatch in INSERT #{idx}")
            continue

        converted_values = []
        for col, val in zip(columns, values):
            converted_values.append(convert_value(val.strip(), col))

        # ✅ Fixed line: no backslash issues
        statement = (
            f'INSERT INTO "{TABLE}" '
            f"({', '.join([f'\"{c}\"' for c in columns])})\n"
            f"VALUES ({', '.join(converted_values)});\n"
        )

        output.append(statement)
        converted += 1

    if not output:
        die("No valid INSERT statements generated")

    Path(OUTPUT_SQL).write_text("".join(output), encoding="utf-8")

    print(f"✅ PostgreSQL INSERT file created: {OUTPUT_SQL}")
    print(f"✅ Rows written: {converted}")

    print("\n🔐 After import (RECOMMENDED):\n")
    print(
        'CREATE EXTENSION IF NOT EXISTS pgcrypto;\n\n'
        'UPDATE "KIOSK"."USERS"\n'
        'SET "PASS" = crypt("PASS", gen_salt(\'bf\'));\n'
    )


if __name__ == "__main__":
    main()
