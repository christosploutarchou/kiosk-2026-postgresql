import re
from datetime import datetime
from pathlib import Path


def convert_timestamp(match):
    oracle_date = match.group(1).strip()

    # Normalize Oracle timestamp:
    # 1) With nanoseconds
    # 2) With microseconds
    # 3) With NO fractional seconds

    if "." in oracle_date:
        left, right = oracle_date.rsplit(".", 1)

        # right = "612000000 PM" OR " PM"
        if " " in right:
            fraction, ampm = right.split(" ", 1)

            if fraction.isdigit():
                # Trim or pad to 6 digits
                fraction = fraction[:6].ljust(6, "0")
                oracle_date = f"{left}.{fraction} {ampm}"
            else:
                # No fraction, only ". PM"
                oracle_date = f"{left}.000000 {ampm}"
    else:
        # No dot at all
        oracle_date = oracle_date.replace(" PM", ".000000 PM").replace(" AM", ".000000 AM")

    dt = datetime.strptime(
        oracle_date,
        "%d-%b-%y %I.%M.%S.%f %p"
    )

    return f"TIMESTAMP '{dt.strftime('%Y-%m-%d %H:%M:%S.%f')[:-3]}'"



def oracle_to_postgres(sql: str) -> str:
    # NVL → COALESCE
    sql = re.sub(r'\bNVL\s*\(', 'COALESCE(', sql, flags=re.IGNORECASE)

    # SYSDATE → CURRENT_TIMESTAMP
    sql = re.sub(r'\bSYSDATE\b', 'CURRENT_TIMESTAMP', sql, flags=re.IGNORECASE)

    # TO_TIMESTAMP with Oracle format
    sql = re.sub(
        r"TO_TIMESTAMP\(\s*'([^']+)'\s*,\s*'DD-MON-RR HH\.MI\.SSXFF AM'\s*\)",
        convert_timestamp,
        sql,
        flags=re.IGNORECASE
    )

    # Remove FROM DUAL
    sql = re.sub(r'\s+FROM\s+DUAL\b', '', sql, flags=re.IGNORECASE)

    # Oracle sequences: seq.NEXTVAL → nextval('seq')
    sql = re.sub(
        r'(\w+)\.NEXTVAL',
        r"nextval('\1')",
        sql,
        flags=re.IGNORECASE
    )

    # Oracle sequences: seq.CURRVAL → currval('seq')
    sql = re.sub(
        r'(\w+)\.CURRVAL',
        r"currval('\1')",
        sql,
        flags=re.IGNORECASE
    )

    # Remove Oracle double quotes (optional but common)
    sql = re.sub(r'"([^"]+)"', r'\1', sql)

    return sql


def convert_file(input_sql: str, output_sql: str):
    input_path = Path(input_sql)
    output_path = Path(output_sql)

    oracle_sql = input_path.read_text(encoding="utf-8")
    postgres_sql = oracle_to_postgres(oracle_sql)

    output_path.write_text(postgres_sql, encoding="utf-8")


if __name__ == "__main__":
    convert_file("oracle.sql", "postgres.sql")
