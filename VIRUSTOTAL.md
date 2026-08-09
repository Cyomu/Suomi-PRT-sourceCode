# VirusTotal Scan Results — v1.0.0E (experimental, SPT 4.1)

Security scans for the compiled binaries of the **experimental SPT 4.1 build**. These are different
binaries from the stable 4.0.13 release, so they have their own hashes and their own scan reports.

| File | SHA-256 | Result | VirusTotal Link |
|---|---|---|---|
| `PRT-fika.Client.dll` | `23E692534150C6FF97035F119BF0AEB00B084F29D5484AE20DD305AC677598C1` | Clean — 0 / 71 | [VirusTotal](https://www.virustotal.com/gui/file/23e692534150c6ff97035f119bf0aeb00b084f29d5484ae20dd305ac677598c1) |
| `PRT-fika.Server.dll` | `4E71633ABB61FE7E1D09C6A7E79CAD7FBEA5FD7A88F40B843EF4DFA7FF032BD6` | 1 / 71 — false positive, see below | [VirusTotal](https://www.virustotal.com/gui/file/4e71633abb61fe7e1d09c6a7e79cad7fbea5fd7a88f40b843ef4dfa7ff032bd6) |

---

## About the single detection on the server DLL

One engine out of 71 flags the server DLL. This is a false positive, and the same detection appears
on the stable 4.0.13 build, where it comes from MaxSecure as `Trojan.Malware.300983.susgen` — the
`susgen` suffix literally means *suspicious generic*: a heuristic guess, not a match against known
malware. It fires on traits like "small, unsigned .NET assembly", which describes practically every
SPT server mod ever published.

The mod contains no obfuscation, no packing, no loading of external code and makes no network
requests of its own. On the server side it only registers items, traders and loot in SPT's own
database at startup.

## Verify it yourself

* **Check the hashes.** Compare the SHA-256 of the files you downloaded against the table above
  (in PowerShell: `Get-FileHash .\PRT-fika.Server.dll`).
* **Read the source.** The full, unobfuscated source of this build is in the `spt-4.1` branch of this
  repository — you can read exactly what it does, or build it yourself.
* **Rescan at any time.** Open the links above and press *Reanalyze* for fresh results instead of a
  cached report.
