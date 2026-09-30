# -*- coding: utf-8 -*-
"""Import/cap nhat du lieu don thu nam 2026 tu Excel vao QuanLyHoSo.

Cach dung:
    python3 import_nam2026.py <db_path> <excel_path>
    python3 import_nam2026.py <db_path> <excel_path> --update-existing

LUU Y: luon sao luu database truoc khi import/cap nhat du lieu.
"""
import argparse
import os
import re
import sqlite3
import unicodedata
import zipfile
from collections import Counter
from datetime import datetime, timedelta
from xml.etree import ElementTree as ET

FALLBACK_AREA = "Đơn vị khác trong tỉnh"
RECEIVER_NAME = "Lê Võ Mỹ Ý"
CANON_CASE = ["Khiếu nại", "Kiến nghị", "Phản ánh", "Tố cáo", "Tố giác"]
CASE_FIX = {
    "phán ánh": "Phản ánh", "phản ảnh": "Phản ánh", "phan anh": "Phản ánh",
    "tố gáic": "Tố giác", "tố giac": "Tố giác",
}
CHUO_WORDS = ("chờ kết quả", "chơờ kết quả", "chờ két quả", "đợi kết quả")
YEAR_RE = re.compile(r"(?<!\d)(19[3-9]\d|20[0-2]\d)(?!\d)")
BIRTH_RE = re.compile(
    r"(?:(?P<label>\b(?:(?:năm\s+)?(?:sinh|sunh|sinih|inh|sn))\s*"
    r"(?:(?:năm|n8am|ngày)\s*)?[:\-]?\s*)"
    r"(?P<birth>19\d{2}|20[0-2]\d|\d{1,2}(?:[./-]\d{1,2})?(?:[./-]\d{2,4})?)|"
    r"(?P<bare>19\d{2}|20[0-2]\d))",
    re.IGNORECASE,
)
ADDRESS_MARKER_RE = re.compile(
    r"\b(?:cùng\s+)?(?:ngụ\s*(?:tại)?|địa\s*chỉ|nơi\s*ở|nơi\s*cư\s*trú|"
    r"thường\s*trú|trú\s*tại|cư\s*trú|sống\s*tại)\s*[:\-]?\s*",
    re.IGNORECASE,
)
ADDRESS_WORD_RE = re.compile(
    r"\b(?:số|tổ|khóm|ấp|khu\s*phố|thôn|xã|phường|thị\s*trấn|đường|hẻm|"
    r"quận|huyện|thành\s*phố|tp\.?|tỉnh|đặc\s*khu|kdc)\b",
    re.IGNORECASE,
)
PERSON_SEPARATOR_RE = re.compile(
    r"(?:\n\s*\d*\s*[.)/-]?\s*|\.\s+(?=[A-ZĐ])|;\s*|\s+và\s+|\s+cùng\s+|"
    r"(?:uỷ|ủy)\s+quyền\s+cho\s+)",
    re.IGNORECASE,
)
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
ISO = "%Y-%m-%dT%H:%M:%S.0000000+07:00"
EXTRA_LABELS = (("Ngày đề đơn", "G"), ("Số TT đơn đã xử lý", "M"), ("Đơn liên quan", "O"),
                ("Cán bộ đề xuất", "P"), ("Đơn vị chuyển", "E"), ("Theo dõi chuyển đơn", "Q"),
                ("Kết quả chuyển đơn", "R"), ("Chi chú", "N"))


def iso_date(value):
    value = (value or "").strip()
    if not value:
        return ""
    if re.fullmatch(r"\d{5}", value):
        return (datetime(1899, 12, 30) + timedelta(days=int(value))).strftime(ISO)
    m = re.match(r"^(\d{1,2})/(\d{1,2})/(\d{4})", value)
    if m:
        return datetime(int(m.group(3)), int(m.group(2)), int(m.group(1))).strftime(ISO)
    # Sua loi go ro rang dang 30/32026 -> 30/3/2026.
    m = re.fullmatch(r"(\d{1,2})/(\d{1,2})(\d{4})", value)
    if m and 1 <= int(m.group(2)) <= 12:
        return datetime(int(m.group(3)), int(m.group(2)), int(m.group(1))).strftime(ISO)
    return ""


def is_invalid_source_row(col):
    """Dong dem rong trong Excel: chi co STT va gia tri 0/1 o cot noi dung."""
    return col["H"] in ("0", "1") and not any(
        col[ch] for ch in "BCDEFGIJKLMNOPQR"
    )


def norm_case_type(raw):
    value = (raw or "").strip().strip(".").strip()
    if not value:
        return "", None
    low = value.lower()
    for canon in CANON_CASE:
        if low == canon.lower():
            return canon, None
    for part in re.split(r"[,;/&]", low):
        for canon in CANON_CASE:
            if part.strip() == canon.lower():
                return canon, value
    for bad, good in CASE_FIX.items():
        if low == bad:
            return good, value
    return value[0].upper() + value[1:], None


def parse_excel(path):
    z = zipfile.ZipFile(path)
    shared = ["".join(t.text or "" for t in si.iter("{%s}t" % NS["m"]))
              for si in ET.fromstring(z.read("xl/sharedStrings.xml")).findall("m:si", NS)]
    rows = ET.fromstring(z.read("xl/worksheets/sheet1.xml")).findall(".//m:row", NS)

    def cellval(c):
        t = c.get("t")
        v = c.find("m:v", NS)
        if t == "s" and v is not None:
            raw = shared[int(v.text)] if v.text is not None else ""
        elif t == "inlineStr":
            raw = "".join(x.text or "" for x in c.iter("{%s}t" % NS["m"]))
        else:
            raw = (v.text if v is not None else "") or ""
        return unicodedata.normalize("NFC", raw)

    data = {ch: [] for ch in "ABCDEFGHIJKLMNOPQR"}
    for row in rows[1:]:
        seen = set()
        for c in row.findall("m:c", NS):
            col = re.match(r"[A-R]", c.get("r")).group()
            data[col].append(cellval(c))
            seen.add(col)
        for ch in "ABCDEFGHIJKLMNOPQR":
            if ch not in seen:
                data[ch].append("")
    return data


def _clean_name(value):
    value = re.sub(r"^\s*\d+\s*[.)/-]\s*", "", value or "")
    value = re.sub(r"^[\s,;:()]+", "", value)
    value = re.sub(r"^(?:và|cùng)\s+", "", value, flags=re.IGNORECASE)
    value = re.sub(r"\s+", " ", value).strip(" ,;.:-()")
    return value


def _birth_text(match):
    raw = (match.group("birth") or match.group("bare")).strip()
    year = YEAR_RE.search(raw)
    if year and re.fullmatch(r"\d{4}", raw):
        return f"sinh năm {year.group(1)}"
    if "/" in raw or "." in raw or "-" in raw:
        return f"sinh ngày {raw}"
    return f"sinh năm {raw}"


def _person_name_before(sender, match, previous_birth_end):
    original = sender[previous_birth_end:match.start()]
    segment = original.rstrip(" ,;:-")
    offset = 0
    separators = list(PERSON_SEPARATOR_RE.finditer(segment))
    if separators:
        offset = separators[-1].end()
        segment = segment[offset:]
    segment = re.sub(r"\b(?:(?:sinh\s+năm|năm\s+sinh)\s*)+$", "", segment,
                     flags=re.IGNORECASE).rstrip(" ,;:-")
    if "," in segment:
        comma = segment.rfind(",")
        offset += comma + 1
        segment = segment[comma + 1:]
    marker = ADDRESS_MARKER_RE.search(segment)
    if marker:
        segment = segment[:marker.start()]
    name = _clean_name(segment)
    relative = original.find(name, offset) if name else -1
    start = previous_birth_end + relative if relative >= 0 else match.start() - len(name)
    return name, start


def _trim_address(value):
    value = re.sub(r"^[\s,;:.-]+", "", value or "")
    value = re.sub(r"\s+", " ", value)
    value = re.sub(r"\s+(?:và|cùng)\s*$", "", value, flags=re.IGNORECASE)
    value = re.sub(r"\s+(?:(?:uỷ|ủy)\s+quyền\s+cho)\s*$", "", value, flags=re.IGNORECASE)
    value = value.strip(" ,;.-")
    value = re.sub(r"(?:^|\s)\d+\s*[.),/-]?\s*$", "", value)
    value = re.sub(r"^(?:sinh\s+năm\s*(?:19\d{2}|20[0-2]\d)?\s*)+", "", value,
                   flags=re.IGNORECASE)
    value = re.split(r"\s*\((?:người|được|do|theo|là)\b", value,
                     maxsplit=1, flags=re.IGNORECASE)[0]
    return value.strip(" ,;.-")


def _people_without_birth(sender, existing):
    """Lay them ten dung ngay truoc marker dia chi, vi du 'va A, ngu tai ...'."""
    found = []
    for marker in ADDRESS_MARKER_RE.finditer(sender):
        before = sender[:marker.start()].rstrip(" ,;:-")
        separators = list(PERSON_SEPARATOR_RE.finditer(before))
        start = separators[-1].end() if separators else 0
        candidate = before[start:]
        if "," in candidate:
            comma = candidate.rfind(",")
            start += comma + 1
            candidate = candidate[comma + 1:]
        name = _clean_name(candidate)
        absolute_start = start
        if not name or BIRTH_RE.search(name) or YEAR_RE.search(name) or ADDRESS_WORD_RE.search(name):
            continue
        if any(name.casefold() == person["name"].casefold() for person in existing):
            continue
        found.append({"name": name, "birth": "", "name_start": absolute_start,
                      "birth_end": marker.start()})
    return found


def _inside_relation_parentheses(sender, position):
    open_at = sender.rfind("(", 0, position)
    close_at = sender.rfind(")", 0, position)
    if open_at <= close_at:
        return False
    prefix = sender[open_at + 1:position].lstrip()
    return bool(re.match(r"(?:là|người|được|do|theo)\b", prefix, re.IGNORECASE))


def parse_sender(raw):
    """Tach nguoi gui, dia chi va tao tien to noi dung.

    SenderName chi giu nguoi dau tien. Khi co nhieu nguoi, tat ca ten va
    thong tin sinh duoc giu trong content_prefix theo dung thu tu nguon.
    """
    sender = unicodedata.normalize("NFC", (raw or "").strip())
    sender = sender.replace("\r\n", "\n").replace("\r", "\n")
    sender = re.sub(r"\bsinh\s+năm\s+sinh\s+năm\b", "sinh năm", sender,
                    flags=re.IGNORECASE)
    phone_match = re.search(r"(?<!\d)0[35789]\d{8}(?!\d)", sender)
    phone = phone_match.group() if phone_match else ""

    births = list(BIRTH_RE.finditer(sender))
    people = []
    previous_end = 0
    for match in births:
        if _inside_relation_parentheses(sender, match.start()):
            continue
        name, name_start = _person_name_before(sender, match, previous_end)
        if match.group("bare"):
            before = sender[match.start() - 1:match.start()]
            after = sender[match.end():match.end() + 1]
            invalid_name_words = ("ngày", "tháng", "năm", "công văn", "cv ", "số ")
            if (before in "/.-" or after in "/.-" or
                    any(word in name.casefold() for word in invalid_name_words) or
                    not (2 <= len(name.split()) <= 12)):
                continue
        if not name or (match.group("bare") and ADDRESS_WORD_RE.match(name)):
            continue
        previous_end = match.end()
        people.append({
            "name": name,
            "birth": _birth_text(match),
            "name_start": name_start,
            "birth_end": match.end(),
        })
    people.extend(_people_without_birth(sender, people))
    people.sort(key=lambda person: person["name_start"])

    first_marker = ADDRESS_MARKER_RE.search(sender)
    if people:
        sender_name = people[0]["name"]
    else:
        name_end = first_marker.start() if first_marker else len(sender)
        sender_name = _clean_name(sender[:name_end].split("\n", 1)[0])
        # Dia chi khong co marker nhung bat dau sau dau phay bang tu dia chi.
        if "," in sender_name:
            parts = sender_name.split(",")
            for idx in range(1, len(parts)):
                if ADDRESS_WORD_RE.search(parts[idx]):
                    sender_name = _clean_name(",".join(parts[:idx]))
                    break

    contact = ""
    if people:
        first_end = people[0]["birth_end"]
        second_start = people[1]["name_start"] if len(people) > 1 else len(sender)
        first_tail = sender[first_end:second_start]
        marker = ADDRESS_MARKER_RE.search(first_tail)
        if marker:
            contact = _trim_address(first_tail[marker.end():])
        elif ADDRESS_WORD_RE.search(first_tail):
            contact = _trim_address(first_tail)
        elif len(people) > 1 and "," in first_tail:
            contact = _trim_address(first_tail)

        if not contact:
            marker = ADDRESS_MARKER_RE.search(sender, first_end)
            if marker:
                later_starts = [
                    person["name_start"] for person in people[1:]
                    if person["name_start"] > marker.end()
                ]
                address_end = min(later_starts) if later_starts else len(sender)
                contact = _trim_address(sender[marker.end():address_end])

        # Dia chi chung thuong nam sau thong tin sinh cua nguoi cuoi cung.
        if not contact:
            shared_tail = sender[people[-1]["birth_end"]:]
            marker = ADDRESS_MARKER_RE.search(shared_tail)
            if marker:
                contact = _trim_address(shared_tail[marker.end():])
            elif ADDRESS_WORD_RE.search(shared_tail):
                contact = _trim_address(shared_tail)
    elif first_marker:
        contact = _trim_address(sender[first_marker.end():])
    elif "," in sender:
        parts = sender.split(",")
        for idx in range(1, len(parts)):
            if ADDRESS_WORD_RE.search(parts[idx]):
                contact = _trim_address(",".join(parts[idx:]))
                break

    if phone:
        contact = _trim_address(contact.replace(phone, ""))

    labels = [
        f'{person["name"]}, {person["birth"]}' if person["birth"] else person["name"]
        for person in people
    ]
    return {
        "name": sender_name,
        "contact": contact,
        "phone": phone,
        "people": labels,
    }


def build_content(content, sender):
    content = (content or "").strip()
    people = sender["people"]
    if not people:
        return content
    # Khong lap lai tien to neu noi dung Excel da mo dau bang nguoi gui.
    if content.casefold().startswith(sender["name"].casefold()):
        return content
    if len(people) == 1:
        return f"{people[0]}, {content}" if content else people[0]
    if content:
        content = content[:1].lower() + content[1:]
    prefix = " cùng ".join(people)
    return f"{prefix} cùng {content}" if content else prefix


def build_rows(data, area_names, processor, receiver=RECEIVER_NAME):
    area_lookup = [(name.casefold(), name) for name in area_names if len(name) >= 3]
    n = len(data["A"])
    now = datetime.now().strftime(ISO)
    rows, histories, new_case_types = [], [], set()
    area_stat = Counter()

    for i in range(n):
        col = {ch: (data[ch][i] if i < len(data[ch]) else "").strip() for ch in "ABCDEFGHIJKLMNOPQR"}
        if not any(col[ch] for ch in "BCDFGHJK"):
            continue
        if is_invalid_source_row(col):
            continue

        content = col["H"]
        note_parts = []
        extra_parts = []

        case_type, case_orig = norm_case_type(col["C"])
        if case_orig:
            extra_parts.append(f"Loại gốc: {case_orig}")
        elif case_type and case_type not in CANON_CASE:
            new_case_types.add(case_type)

        k = col["K"]
        k_low = k.lower()
        if any(w in k_low for w in CHUO_WORDS):
            status = "Chờ kết quả"
        elif k_low in LUU_STATUS_SET:
            status = "Đã giải quyết"
        elif k:
            status = "Đã giải quyết"
        else:
            status = "Mới tiếp nhận"

        if content in ("1", "0"):
            note_parts.append("(Nội dung gốc trong Excel: '{}')".format(content))
        for label, ch in EXTRA_LABELS:
            if col[ch]:
                extra_parts.append(f"{label}: {col[ch]}")

        sender = parse_sender(col["F"])
        sender_name = sender["name"]
        contact = sender["contact"]
        phone = sender["phone"]
        content_full = build_content(content, sender)

        area = ""
        addr_text = (contact + " " + content).casefold()
        matches = [
            (-len(key), addr_text.find(key), name.casefold(), name)
            for key, name in area_lookup if key in addr_text
        ]
        if matches:
            area = min(matches)[3]
            area_stat["từ địa chỉ"] += 1
        if not area:
            for ch in ("E", "J"):
                if col[ch] in area_names:
                    area = col[ch]
                    area_stat["từ đơn vị"] += 1
                    break
        if not area:
            area = FALLBACK_AREA
            area_stat["mặc định"] += 1

        received = iso_date(col["B"])
        handed = iso_date(col["I"])
        note = "; ".join(p for p in [col["L"]] + note_parts if p)
        additional = " | ".join(extra_parts)
        # Giu lien ket on dinh voi STT/dong Excel, ke ca khi bo dong rac.
        record_code = "HS-2026-{:06d}".format(i + 1)

        rows.append((
            record_code, received, col["D"], receiver, sender_name, phone, contact,
            area, "", content_full, case_type, "", "", "", "", "", "", "",
            status, processor, note, additional, now, now,
        ))

        if status == "Đã giải quyết":
            hist_content = "Lưu đơn" if k_low in LUU_STATUS_SET else k
            processed_at = handed or received or now
            histories.append((record_code, "Lưu hồ sơ", processed_at, processor, hist_content, 1))

    return rows, histories, sorted(new_case_types), area_stat


LUU_STATUS_SET = {"lưu đơn"}


def parse_args():
    parser = argparse.ArgumentParser(description="Import/cap nhat du lieu ho so nam 2026")
    parser.add_argument("db_path")
    parser.add_argument("excel_path")
    parser.add_argument("--update-existing", action="store_true",
                        help="Cap nhat cac ma HS-2026 da ton tai thay vi bo qua")
    parser.add_argument("--receiver", default=RECEIVER_NAME)
    parser.add_argument("--processor", default="admin")
    parser.add_argument("--prune-invalid-source-rows", action="store_true",
                        help="Xoa cac ho so da tao tu dong Excel chi co noi dung 0/1")
    return parser.parse_args()


def backup_database(conn, db_path):
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_path = f"{db_path}.pre_nam2026_fix_{stamp}.bak"
    target = sqlite3.connect(backup_path)
    try:
        conn.backup(target)
    finally:
        target.close()
    return backup_path


def main():
    args = parse_args()
    db_path, excel_path = args.db_path, args.excel_path

    conn = sqlite3.connect(db_path)
    conn.execute("PRAGMA foreign_keys = ON")
    area_names = {r[0] for r in conn.execute("SELECT Name FROM Areas")}

    data = parse_excel(excel_path)
    rows, histories, new_case_types, area_stat = build_rows(
        data, area_names, args.processor, args.receiver)
    existing_case_types = {
        r[0] for r in conn.execute(
            "SELECT Name FROM CatalogItems WHERE CatalogType='CaseType'"
        )
    }
    case_types_to_add = [name for name in new_case_types if name not in existing_case_types]

    existing = {r[0] for r in conn.execute("SELECT RecordCode FROM Records")}
    invalid_codes = {
        "HS-2026-{:06d}".format(i + 1)
        for i in range(len(data["A"]))
        if is_invalid_source_row({
            ch: (data[ch][i] if i < len(data[ch]) else "").strip()
            for ch in "ABCDEFGHIJKLMNOPQR"
        })
    }
    prune_codes = sorted(existing & invalid_codes) if args.prune_invalid_source_rows else []
    insert_rows = [r for r in rows if r[0] not in existing]
    update_rows = [r for r in rows if r[0] in existing] if args.update_existing else []
    print(f"Dong hop le: {len(rows)} | dong rac: {len(invalid_codes)} | "
          f"them moi: {len(insert_rows)} | "
          f"cap nhat: {len(update_rows)} | lich su xu ly: {len(histories)} | "
          f"CaseType moi: {len(case_types_to_add)}")
    print("Dia bàn:", ", ".join(f"{k}={v}" for k, v in area_stat.items()))
    print("CaseType se them:", case_types_to_add)

    cur = conn.cursor()
    rec_sql = """INSERT INTO Records (RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName,
        SenderPhone, ContactAddress, AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field,
        RelatedPerson, ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate,
        Status, ProcessorName, Note, AdditionalNote, CreatedAt, UpdatedAt)
        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"""
    if update_rows or prune_codes:
        backup_path = backup_database(conn, os.path.abspath(db_path))
        print("Backup:", backup_path)

    cur.executemany(rec_sql, insert_rows)

    if update_rows:
        update_sql = """UPDATE Records SET
            ReceivedDate=?, ReceiveSource=?, ReceiverName=?, SenderName=?, SenderPhone=?,
            ContactAddress=?, AreaName=?, IncidentAddress=?, Content=?, CaseType=?,
            Note=?, AdditionalNote=?, UpdatedAt=?
            WHERE RecordCode=?"""
        cur.executemany(update_sql, [
            (
                row[1], row[2], row[3], row[4], row[5], row[6], row[7], row[8],
                row[9], row[10], row[20], row[21], row[23], row[0],
            )
            for row in update_rows
        ])

    if prune_codes:
        cur.executemany("DELETE FROM Records WHERE RecordCode=?", [(code,) for code in prune_codes])
        print("Da xoa dong rac:", len(prune_codes))

    if case_types_to_add:
        base = conn.execute(
            "SELECT COALESCE(MAX(DisplayOrder),0) FROM CatalogItems WHERE CatalogType='CaseType'").fetchone()[0]
        for idx, name in enumerate(case_types_to_add, start=1):
            conn.execute(
                "INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive) VALUES ('CaseType', ?, ?, 1)",
                (name, base + idx))

    if histories and insert_rows:
        code_to_id = dict(conn.execute("SELECT RecordCode, Id FROM Records").fetchall())
        inserted_codes = {row[0] for row in insert_rows}
        cur.executemany(
            """INSERT INTO ProcessHistories (RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted)
               VALUES (?,?,?,?,?,?)""",
            [(code_to_id[code], title, processed, proc, content, flag)
             for code, title, processed, proc, content, flag in histories
             if code in code_to_id and code in inserted_codes])

    conn.commit()
    print("integrity:", conn.execute("PRAGMA integrity_check").fetchone()[0])
    print("Tong ho so:", conn.execute("SELECT COUNT(*) FROM Records").fetchone()[0])
    print("Theo trang thai:", conn.execute(
        "SELECT Status, COUNT(*) FROM Records GROUP BY Status ORDER BY COUNT(*) DESC").fetchall())
    print("Theo loai don (top 8):", conn.execute(
        "SELECT CaseType, COUNT(*) FROM Records GROUP BY CaseType ORDER BY COUNT(*) DESC LIMIT 8").fetchall())
    print("Mau:", conn.execute(
        "SELECT RecordCode, ReceivedDate, Status, AreaName FROM Records ORDER BY Id DESC LIMIT 3").fetchall())
    conn.close()


if __name__ == "__main__":
    main()
