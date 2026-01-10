import re
from datetime import datetime

# Input / output file paths
input_file = "oracle_inserts.sql"
output_file = "postgres_inserts.sql"

# Regex patterns
to_date_pattern = re.compile(r"TO_DATE\('(\d{1,2}-[A-Z]{3}-\d{2,4})','[^\']*'\)", re.IGNORECASE)
uuid_pattern = re.compile(r"'([0-9a-fA-F\-]{36})'")  # simple UUID match

# Month map for Oracle MON -> numeric
month_map = {
    'JAN': '01', 'FEB': '02', 'MAR': '03', 'APR': '04', 'MAY': '05', 'JUN': '06',
    'JUL': '07', 'AUG': '08', 'SEP': '09', 'OCT': '10', 'NOV': '11', 'DEC': '12'
}

def convert_to_date(match):
    date_str = match.group(1)  # e.g., 28-MAY-21
    parts = date_str.split('-')
    day = parts[0].zfill(2)
    mon = month_map[parts[1].upper()]
    year = parts[2]
    # Convert 2-digit year if needed
    if len(year) == 2:
        yr = int(year)
        year = str(2000 + yr) if yr < 50 else str(1900 + yr)
    return f"'{year}-{mon}-{day}'::date"

def convert_uuid(match):
    uuid_str = match.group(1)
    return f"'{uuid_str}'::uuid"

with open(input_file, "r", encoding="utf-8") as f:
    content = f.read()

# Convert TO_DATE
content = to_date_pattern.sub(convert_to_date, content)

# Convert Oracle NULL to PostgreSQL NULL (case-insensitive)
content = re.sub(r'\bNULL\b', 'NULL', content, flags=re.IGNORECASE)

# Convert UUID fields (optional, only valid UUIDs)
# Note: This naively converts any 36-char hex with dashes
content = uuid_pattern.sub(convert_uuid, content)

# Write output
with open(output_file, "w", encoding="utf-8") as f:
    f.write(content)

print(f"Conversion complete. PostgreSQL inserts written to {output_file}")
