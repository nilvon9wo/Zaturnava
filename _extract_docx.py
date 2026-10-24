import zipfile, re, html, os

os.makedirs("extracted", exist_ok=True)

z = zipfile.ZipFile("Chaturaji.docx")

# Extract text
xml = z.read("word/document.xml").decode("utf-8")
paras = re.split(r"</w:p>", xml)
out = []
for p in paras:
    ts = re.findall(r"<w:t[^>]*>(.*?)</w:t>", p, re.DOTALL)
    if ts:
        out.append("".join(ts))
text = html.unescape("\n".join(out))
with open("extracted/chaturaji_text.txt", "w", encoding="utf-8") as f:
    f.write(text)

# Extract images
for n in z.namelist():
    if "media" in n:
        fname = n.split("/")[-1]
        with open(os.path.join("extracted", fname), "wb") as f:
            f.write(z.read(n))

print("done")
