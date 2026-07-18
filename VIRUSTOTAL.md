# VirusTotal Scan Results — v0.9.6

Security scans for the compiled binaries distributed with this release. Both files came back clean (0 malicious / 0 suspicious detections).

| File | SHA-256 | VirusTotal Link |
|---|---|---|
| `PRT-fika.Client.dll` | `86492c2486e4a5aa83ffa7cc614979ea75320eff1bd40dad5dc507b5c31b1514` | https://www.virustotal.com/gui/file/86492c2486e4a5aa83ffa7cc614979ea75320eff1bd40dad5dc507b5c31b1514/detection |
| `PRT-fika.Server.dll` | `c9eb5a263c0994e87604da32a4479fcc753c4425241a596beb9e945f9ae8bbb4` | https://www.virustotal.com/gui/file/c9eb5a263c0994e87604da32a4479fcc753c4425241a596beb9e945f9ae8bbb4/detection |

**Note:** Both files show one "medium" Sigma rule match ("Sysmon File Executable Creation Detected"). This is not an antivirus detection — it's a generic behavioral rule that fires on the creation of any monitored executable file, regardless of content. No antivirus engine flagged either file as malicious or suspicious.


