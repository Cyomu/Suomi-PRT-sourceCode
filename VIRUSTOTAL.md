# VirusTotal Scan Results — v1.0.0

Security scans for the compiled binaries distributed with this release.

| File | SHA-256 | Result | VirusTotal Link |
|---|---|---|---|
| `PRT-fika.Client.dll` | `38CCFC469308F7C57656AB33CF098717CFA6824B0EFF0E2E9722624065D47379` | Clean — no detections | https://www.virustotal.com/gui/file/38ccfc469308f7c57656ab33cf098717cfa6824b0eff0e2e9722624065d47379 |
| `PRT-fika.Server.dll` | `9FA858428C52BF6FD1D0C44B50D281EBAF0F68300DA61C1CE1A2A0C5650467F6` | 1 / 71 — see note below | https://www.virustotal.com/gui/file/9fa858428c52bf6fd1d0c44b50d281ebaf0f68300da61c1ce1a2a0c5650467f6 |

---

## About the single detection on the server DLL

One engine out of 71 — **MaxSecure** — flags `PRT-fika.Server.dll` as `Trojan.Malware.300983.susgen`. **This is a false positive.** Why we are confident:

* **70 of 71 engines report the file as clean**, including every major vendor (Microsoft, Kaspersky, ESET, Bitdefender, Dr.Web and the rest). A real threat does not slip past all of them and get caught only by one small engine.
* **The signature name says so itself.** The `susgen` suffix stands for *suspicious generic* — it is not a match against a known piece of malware, but a heuristic guess. It fires on traits like "small .NET assembly, not signed with a code-signing certificate", which describes essentially every SPT server mod ever published.
* **The previous release (v0.9.7) was flagged by nobody at all**, built the same way from the same kind of code. Nothing about how the mod is built changed — MaxSecure's heuristics did.
* **There is nothing here for it to find.** The mod contains no obfuscation, no packing, no downloading or loading of external code, and makes no network requests of its own. On the server side it only registers items, traders and loot in SPT's own database at startup.

## Verify it yourself

You do not have to take our word for it:

* **Check the hashes.** Compare the SHA-256 of the files you downloaded against the table above (in PowerShell: `Get-FileHash .\PRT-fika.Server.dll`). If they match, you have exactly the files we scanned.
* **Read the source.** The full, unobfuscated source code of both DLLs is published at <https://github.com/Cyomu/Suomi-PRT-sourceCode> — you can read exactly what the mod does, and build it yourself if you prefer.
* **Rescan at any time.** Open the links above and press *Reanalyze* for fresh results instead of a cached report.

If your antivirus quarantines the server DLL, this is the detection behind it.
