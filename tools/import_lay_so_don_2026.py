# -*- coding: utf-8 -*-
"""Replace record data from the 2026 legacy .xls register.

The source workbook is never modified. Dry-run is the default; pass --apply to
replace Records, RecordAttachments, and ProcessHistories in the target database.
Users, areas, catalogs, settings, and system logs are left untouched.
"""

import argparse
import re
import sqlite3
import unicodedata
from collections import Counter
from datetime import datetime
from pathlib import Path

import xlrd


SHEET_NAME = "Cấp số đơn"
ISO = "%Y-%m-%dT%H:%M:%S.0000000+07:00"
STATUS = "Mới tiếp nhận"

# Two malformed source cells whose intended dates are unambiguous from the
# adjacent sequential register entries.
DATE_CORRECTIONS = {
    1222: datetime(2026, 4, 20),
    4501: datetime(2026, 10, 8),
}

COMMON_SURNAMES = {
    "bạch", "bùi", "cao", "châu", "danh", "diệp", "dương", "đào", "đặng",
    "đinh", "đỗ", "đoàn", "hà", "hồ", "hoàng", "huỳnh", "kiên", "la",
    "lâm", "lê", "lương", "lý", "mai", "ngô", "nguyễn", "nông", "phan",
    "phạm", "quách", "tạ", "thái", "tô", "tống", "trần", "trịnh", "trương",
    "vũ", "võ",
}

LEADING_ACTION_RE = re.compile(
    r"^\s*[`'\"]?\s*(?:"
    r"TLĐ|PCĐ|"
    r"hướng\s+dẫn\s+tòa|hướng\s+dẫn\s+đơn|phiếu\s+hướng\s+dẫn|"
    r"công\s+văn|CV|thông\s+báo|TB"
    r")\s*[,.:;-]?\s*",
    re.IGNORECASE,
)
COMMON_ACTION_RE = re.compile(
    r"^\s*[`'\"]?\s*(?:"
    r"(?:kết\s+quả\s+)?trả\s+(?:lời|loời|lơời)\s+đ(?:ơn|ơm)|"
    r"phiếu\s+(?:chuyển|chuuyển)\s+đơn|"
    r"phiêí\s+chuyển\s+đơn"
    r")\s*",
    re.IGNORECASE,
)
TITLE_RE = re.compile(
    r"^(?:(?:của|cho)\s+)?(?:(?:ông|bà|anh|chị|ông\s+bà)\s+)?",
    re.IGNORECASE,
)
PERSON_SPLIT_RE = re.compile(r"\s*(?:,|;|\s+và\s+|\s+với\s+)\s*", re.IGNORECASE)
PAREN_RE = re.compile(r"\s*\([^)]*\)\s*")
ORGANIZATION_RE = re.compile(
    r"^(?:cục|công\s+ty|cty|ngân\s+hàng|tòa\s+án|TAND|VKS|UBND|ban\s+|phòng\s+)",
    re.IGNORECASE,
)


def parse_args():
    parser = argparse.ArgumentParser(
        description="Dry-run or replace QuanLyHoSo records from the 2026 .xls register"
    )
    parser.add_argument("db_path")
    parser.add_argument("xls_path")
    parser.add_argument("--apply", action="store_true")
    return parser.parse_args()


def text(value):
    if value is None:
        return ""
    if isinstance(value, float) and value.is_integer():
        value = int(value)
    return unicodedata.normalize("NFC", str(value)).strip()


def normalized_key(value):
    return text(value).casefold()


def parse_date(book, sheet, row_index, excel_row):
    if excel_row in DATE_CORRECTIONS:
        return DATE_CORRECTIONS[excel_row]

    cell = sheet.cell(row_index, 2)
    if cell.ctype == xlrd.XL_CELL_DATE:
        return xlrd.xldate.xldate_as_datetime(cell.value, book.datemode)

    raw = text(cell.value)
    for pattern in ("%d/%m/%Y", "%d-%m-%Y", "%Y-%m-%d"):
        try:
            return datetime.strptime(raw, pattern)
        except ValueError:
            pass
    raise ValueError(f"Dòng Excel {excel_row}: ngày không hợp lệ: {raw!r}")


def _fold_word(value):
    folded = unicodedata.normalize("NFD", value.casefold())
    return "".join(ch for ch in folded if unicodedata.category(ch) != "Mn").replace("đ", "d")


def _looks_like_person(segment):
    words = re.findall(r"[A-Za-zÀ-ỹĐđ]+", segment)
    if not words:
        return False
    first = words[0].casefold()
    return first in COMMON_SURNAMES and 2 <= len(words) <= 7


def extract_sender(summary):
    value = re.sub(r"\s+", " ", text(summary)).strip(" `,.;:-")
    if not value:
        return ""

    if "nặc danh" in value.casefold():
        return "Nặc danh"

    value, common_count = COMMON_ACTION_RE.subn("", value, count=1)
    value, leading_count = LEADING_ACTION_RE.subn("", value, count=1)
    action_removed = common_count > 0 or leading_count > 0

    # Most long-form labels end at the first standalone word "đơn".
    match = re.search(r"\bđơn\b", value, flags=re.IGNORECASE)
    if match:
        value = value[match.end():]
        action_removed = True

    value = TITLE_RE.sub("", value).strip(" `,.;:-")
    value_without_notes = PAREN_RE.sub(" ", value).strip(" `,.;:-")
    segments = [TITLE_RE.sub("", item).strip(" `,.;:-")
                for item in PERSON_SPLIT_RE.split(value_without_notes)]
    nonempty_segments = [item for item in segments if item]
    if (len(nonempty_segments) > 1
            and not ORGANIZATION_RE.search(value_without_notes)):
        last_segment = nonempty_segments[-1]
        if _looks_like_person(last_segment) or len(last_segment.split()) <= 4:
            return last_segment
    people = [item for item in segments if _looks_like_person(item)]
    if people:
        return people[-1]

    # Handle descriptive text before a recognizable surname, for example
    # "tra lời Thanh tra Hoàng Văn Long".
    words = value_without_notes.split()
    surname_positions = [index for index, word in enumerate(words)
                         if word.casefold().strip(" ,.;:-") in COMMON_SURNAMES]
    if surname_positions:
        return " ".join(words[surname_positions[-1]:]).strip(" `,.;:-")

    # Organizations and single-word foreign names remain valid sender labels
    # only when a known action label was removed. A descriptive document title
    # without a person (for example "Phúc đáp CV ...") yields a blank sender.
    return (value_without_notes or value) if action_removed else ""


def build_content(summary, recipient):
    summary = text(summary).rstrip(" .")
    recipient = text(recipient)
    if not recipient:
        return summary
    return f"{summary}. Chuyển {recipient}"


def load_rows(xls_path):
    book = xlrd.open_workbook(str(xls_path), on_demand=True)
    try:
        sheet = book.sheet_by_name(SHEET_NAME)
        seen = set()
        rows = []
        duplicate_rows = []
        blank_code_rows = []

        for row_index in range(1, sheet.nrows):
            excel_row = row_index + 1
            code = text(sheet.cell_value(row_index, 0))
            key = normalized_key(code)
            if not key:
                blank_code_rows.append(excel_row)
                continue
            if key in seen:
                duplicate_rows.append((excel_row, code))
                continue
            seen.add(key)

            received = parse_date(book, sheet, row_index, excel_row)
            summary = text(sheet.cell_value(row_index, 3))
            recipient = text(sheet.cell_value(row_index, 5))
            sender = extract_sender(summary)
            content = build_content(summary, recipient)
            if not summary:
                raise ValueError(f"Dòng Excel {excel_row}: nội dung trích yếu trống")
            rows.append({
                "excel_row": excel_row,
                "record_code": code,
                "received_date": received.strftime(ISO),
                "sender_name": sender,
                "content": content,
            })
        return rows, duplicate_rows, blank_code_rows
    finally:
        book.release_resources()


def validate_database(connection):
    required = {"Users", "Areas", "CatalogItems", "Records", "RecordAttachments",
                "ProcessHistories", "SystemLogs"}
    tables = {row[0] for row in connection.execute(
        "SELECT name FROM sqlite_master WHERE type='table'"
    )}
    missing = sorted(required - tables)
    if missing:
        raise ValueError("Database thiếu bảng bắt buộc: " + ", ".join(missing))
    integrity = connection.execute("PRAGMA integrity_check").fetchone()[0]
    if integrity != "ok":
        raise ValueError("Database integrity_check thất bại: " + integrity)


def print_summary(rows, duplicate_rows, blank_code_rows):
    print(f"Hồ sơ nguồn: {len(rows) + len(duplicate_rows) + len(blank_code_rows):,}")
    print(f"Giữ để nhập: {len(rows):,}")
    print(f"Loại do trùng mã: {len(duplicate_rows):,}")
    print(f"Loại do mã trống: {len(blank_code_rows):,}")
    print("Sửa ngày xác định: dòng 1222=20/04/2026; dòng 4501=08/10/2026")
    print("Mã trống:", blank_code_rows)
    sender_counts = Counter(row["sender_name"] for row in rows)
    blank_senders = [row["excel_row"] for row in rows if not row["sender_name"]]
    print("Người gửi tách được:", len(sender_counts) - (1 if "" in sender_counts else 0))
    print("Dòng không có tên để tách:", blank_senders)
    print("Mẫu đầu:")
    for row in rows[:5]:
        print(row)
    print("Mẫu cuối:")
    for row in rows[-5:]:
        print(row)


def replace_records(connection, rows):
    now = datetime.now().strftime(ISO)
    insert_sql = """
INSERT INTO Records (
    RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName,
    SenderPhone, ContactAddress, AreaName, IncidentAddress, Content,
    CaseType, ContentGroup, Field, RelatedPerson, ExpectedHandlingMethod,
    SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, Status,
    ProcessorName, Note, AdditionalNote, CreatedAt, UpdatedAt
) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
"""
    payload = [(
        row["record_code"], row["received_date"], "", "", row["sender_name"],
        "", "", "", "", row["content"],
        "", "", "", "", "", "", "", "", STATUS,
        "", "", "", now, now,
    ) for row in rows]

    connection.execute("BEGIN IMMEDIATE")
    try:
        connection.execute("DELETE FROM RecordAttachments")
        connection.execute("DELETE FROM ProcessHistories")
        connection.execute("DELETE FROM Records")
        connection.executemany(insert_sql, payload)
        connection.execute(
            "DELETE FROM sqlite_sequence WHERE name IN ('Records','RecordAttachments','ProcessHistories')"
        )
        connection.commit()
    except Exception:
        connection.rollback()
        raise


def main():
    args = parse_args()
    db_path = Path(args.db_path).resolve()
    xls_path = Path(args.xls_path).resolve()
    if not db_path.is_file():
        raise FileNotFoundError(db_path)
    if not xls_path.is_file():
        raise FileNotFoundError(xls_path)

    rows, duplicate_rows, blank_code_rows = load_rows(xls_path)
    print_summary(rows, duplicate_rows, blank_code_rows)

    connection = sqlite3.connect(str(db_path))
    try:
        connection.execute("PRAGMA foreign_keys = ON")
        validate_database(connection)
        before = {
            table: connection.execute(f"SELECT COUNT(*) FROM {table}").fetchone()[0]
            for table in ("Users", "Areas", "CatalogItems", "Records",
                          "RecordAttachments", "ProcessHistories", "SystemLogs")
        }
        print("Trước import:", before)

        if not args.apply:
            print("DRY RUN: database chưa thay đổi. Dùng --apply sau khi đã sao lưu và dừng Server.")
            return

        replace_records(connection, rows)
        validate_database(connection)
        after = {
            table: connection.execute(f"SELECT COUNT(*) FROM {table}").fetchone()[0]
            for table in before
        }
        print("Sau import:", after)
        for table in ("Users", "Areas", "CatalogItems", "SystemLogs"):
            if after[table] != before[table]:
                raise RuntimeError(f"Dữ liệu hệ thống bị thay đổi ngoài ý muốn: {table}")
        if after["Records"] != len(rows):
            raise RuntimeError("Số hồ sơ sau import không khớp nguồn đã lọc")
        duplicate_codes = connection.execute(
            "SELECT COUNT(*) FROM (SELECT RecordCode FROM Records "
            "GROUP BY RecordCode HAVING COUNT(*) > 1)"
        ).fetchone()[0]
        blank_codes = connection.execute(
            "SELECT COUNT(*) FROM Records WHERE TRIM(RecordCode) = ''"
        ).fetchone()[0]
        blank_senders = connection.execute(
            "SELECT COUNT(*) FROM Records WHERE SenderName = ''"
        ).fetchone()[0]
        nonblank_other_business_fields = connection.execute("""
SELECT COUNT(*) FROM Records
WHERE ReceiveSource <> '' OR ReceiverName <> '' OR SenderPhone <> ''
   OR ContactAddress <> '' OR AreaName <> '' OR IncidentAddress <> ''
   OR CaseType <> '' OR ContentGroup <> '' OR Field <> ''
   OR RelatedPerson <> '' OR ExpectedHandlingMethod <> ''
   OR SenderExpectedHandlingMethod <> '' OR SeverityLevel <> ''
   OR ExpectedResultDate <> '' OR ProcessorName <> '' OR Note <> ''
   OR AdditionalNote <> ''
""").fetchone()[0]
        print("Kiểm tra mã trùng:", duplicate_codes)
        print("Kiểm tra mã trống:", blank_codes)
        print("Người gửi trống do trích yếu không có tên:", blank_senders)
        print("Hồ sơ có trường nghiệp vụ ngoài 4 trường yêu cầu:", nonblank_other_business_fields)
        if duplicate_codes or blank_codes or nonblank_other_business_fields:
            raise RuntimeError("Kiểm tra dữ liệu sau import không đạt")
        print("Mẫu đã lưu:", connection.execute(
            "SELECT RecordCode, ReceivedDate, SenderName, Content "
            "FROM Records ORDER BY Id LIMIT 1"
        ).fetchone())
        print("Ngày đã sửa:", connection.execute(
            "SELECT RecordCode, ReceivedDate FROM Records "
            "WHERE RecordCode IN ('1226/PC-VPCQCSĐT','4444/VPCQCSĐT') "
            "ORDER BY RecordCode"
        ).fetchall())
        print("IMPORT_OK")
    finally:
        connection.close()


if __name__ == "__main__":
    main()
