# Máy chủ giả lập: SMTP (cổng 2525) + Telegram Bot API (cổng 8099). Ghi mọi tin nhận được vào mock.log
import asyncio, json, sys
LOG = sys.argv[1]
def ghi(s):
    with open(LOG, "a", encoding="utf-8") as f: f.write(s + "\n")
async def smtp(reader, writer):
    writer.write(b"220 mock ESMTP\r\n"); await writer.drain()
    rcpt = []; data = False; buf = []
    while True:
        line = await reader.readline()
        if not line: break
        s = line.decode(errors="ignore").rstrip("\r\n")
        if data:
            if s == ".":
                data = False; body = "\n".join(buf)
                subj = next((l for l in buf if l.lower().startswith("subject:")), "")
                tep = [l.split("filename=",1)[1].strip('"; ') for l in buf if "filename=" in l]
                ghi("EMAIL " + ",".join(rcpt) + " | " + subj + (" | ATTACH " + ",".join(tep) if tep else "")); buf = []; rcpt = []
                writer.write(b"250 OK queued\r\n")
            else: buf.append(s)
        else:
            u = s.upper()
            if u.startswith("EHLO"): writer.write(b"250-mock\r\n250-8BITMIME\r\n250 SMTPUTF8\r\n")
            elif u.startswith("HELO"): writer.write(b"250 mock\r\n")
            elif u.startswith("MAIL"): writer.write(b"250 OK\r\n")
            elif u.startswith("RCPT"): rcpt.append(s.split(":",1)[1].strip(" <>")); writer.write(b"250 OK\r\n")
            elif u == "DATA": data = True; writer.write(b"354 go\r\n")
            elif u == "QUIT": writer.write(b"221 bye\r\n"); await writer.drain(); break
            else: writer.write(b"250 OK\r\n")
        await writer.drain()
    writer.close()
async def tele(reader, writer):
    head = b""
    while b"\r\n\r\n" not in head: head += await reader.read(1024)
    h, _, rest = head.partition(b"\r\n\r\n")
    if b"chunked" in h.lower():
        body = b""
        while True:
            while b"\r\n" not in rest: rest += await reader.read(1024)
            size_line, _, rest = rest.partition(b"\r\n"); size = int(size_line.split(b";")[0], 16)
            if size == 0: break
            while len(rest) < size + 2: rest += await reader.read(1024)
            body += rest[:size]; rest = rest[size + 2:]
        rest = body
    else:
        n = int(next((l.split(b":")[1] for l in h.split(b"\r\n") if l.lower().startswith(b"content-length")), b"0"))
        while len(rest) < n: rest += await reader.read(n - len(rest))
    try:
        j = json.loads(rest); ghi("TELEGRAM " + str(j["chat_id"]) + " | " + j["text"].split("\n")[0])
    except Exception as e: ghi("TELEGRAM-ERR " + repr(e))
    body = b'{"ok":true,"result":{}}'
    writer.write(b"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + str(len(body)).encode() + b"\r\nConnection: close\r\n\r\n" + body)
    await writer.drain(); writer.close()
async def doc_http(reader):
    head = b""
    while b"\r\n\r\n" not in head: head += await reader.read(1024)
    h, _, rest = head.partition(b"\r\n\r\n")
    lines = h.decode(errors="ignore").split("\r\n")
    hd = {l.split(":",1)[0].strip().lower(): l.split(":",1)[1].strip() for l in lines[1:] if ":" in l}
    if "chunked" in hd.get("transfer-encoding", "").lower():
        body = b""
        while True:
            while b"\r\n" not in rest: rest += await reader.read(1024)
            size_line, _, rest = rest.partition(b"\r\n"); size = int(size_line.split(b";")[0], 16)
            if size == 0: break
            while len(rest) < size + 2: rest += await reader.read(1024)
            body += rest[:size]; rest = rest[size + 2:]
        rest = body
    else:
        n = int(hd.get("content-length", "0"))
        while len(rest) < n: rest += await reader.read(n - len(rest))
    return lines[0], hd, rest
# Zalo OA giả lập (cổng 8097): access token hợp lệ là token phát gần nhất; refresh token "R1" → cấp "NEW1"/"R2", "R2" → "NEW2"/"R3"…
ZALO = {"access": "NEW0-chua-cap", "refresh": "R1", "n": 0}
async def zalo(reader, writer):
    dong, hd, body = await doc_http(reader)
    if "/v4/oa/access_token" in dong:
        from urllib.parse import parse_qs
        f = {k: v[0] for k, v in parse_qs(body.decode()).items()}
        if f.get("refresh_token") == ZALO["refresh"] and hd.get("secret_key") == "SECRET":
            ZALO["n"] += 1; ZALO["access"] = f"NEW{ZALO['n']}"; ZALO["refresh"] = f"R{ZALO['n'] + 1}"
            ghi(f"ZALO-REFRESH -> {ZALO['access']}")
            out = json.dumps({"access_token": ZALO["access"], "refresh_token": ZALO["refresh"], "expires_in": "90000"})
        else: out = json.dumps({"error": -14014, "message": "Invalid refresh token"})
    else:
        if hd.get("access_token") != ZALO["access"]: out = json.dumps({"error": -216, "message": "Access token is invalid"}); ghi("ZALO-TOKEN-SAI " + hd.get("access_token", ""))
        else:
            j = json.loads(body); ghi("ZALO " + j["recipient"]["user_id"] + " | " + j["message"]["text"].split("\n")[0])
            out = json.dumps({"error": 0, "message": "Success", "data": {"message_id": "m1"}})
    b = out.encode()
    writer.write(b"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + str(len(b)).encode() + b"\r\nConnection: close\r\n\r\n" + b)
    await writer.drain(); writer.close()
async def main():
    await asyncio.start_server(smtp, "127.0.0.1", 2525); await asyncio.start_server(tele, "127.0.0.1", 8099); await asyncio.start_server(zalo, "127.0.0.1", 8097)
    await asyncio.Event().wait()
asyncio.run(main())
