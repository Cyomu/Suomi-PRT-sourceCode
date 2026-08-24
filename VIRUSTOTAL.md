# VirusTotal Scan Results — v1.0.2

Security scans for the compiled binaries of the **experimental SPT 4.1 build**. These are different
binaries from the stable 4.0.13 release, so they have their own hashes and their own scan reports.

| File | SHA-256 | Result | VirusTotal Link |
|---|---|---|---|
| `PRT-fika.Client.dll` | `d503289bf30d870cd8393a5086ee0a28c41ce61fa1486e00b867914024f4ff2d` | Clean — 0 / 69 | [VirusTotal](https://www.virustotal.com/gui/file/d503289bf30d870cd8393a5086ee0a28c41ce61fa1486e00b867914024f4ff2d) |
| `PRT-fika.Server.dll` | `ce3fe20925305c42dedaea33387c7d0545d85e1b836d42015e704d409cdcfad9` | 0 / 70 | [VirusTotal](https://www.virustotal.com/gui/file/ce3fe20925305c42dedaea33387c7d0545d85e1b836d42015e704d409cdcfad9) |

---

## Verify it yourself

* **Check the hashes.** Compare the SHA-256 of the files you downloaded against the table above
  (in PowerShell: `Get-FileHash .\PRT-fika.Server.dll`).
* **Read the source.** The full, unobfuscated source of this build is in the `spt-4.1` branch of this
  repository — you can read exactly what it does, or build it yourself.
* **Rescan at any time.** Open the links above and press *Reanalyze* for fresh results instead of a
  cached report.
