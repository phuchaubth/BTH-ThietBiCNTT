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
async def main():
    await asyncio.start_server(smtp, "127.0.0.1", 2525); await asyncio.start_server(tele, "127.0.0.1", 8099)
    await asyncio.Event().wait()
asyncio.run(main())
