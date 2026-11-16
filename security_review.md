# iTextSharp CVE Review

**Review scope:** Current `src/core` source in this repository; Bouncy Castle and other third-party dependencies excluded. No files were modified as part of the review.

The core identifies itself as `iTextSharp 4.1.6-NS_Modified` (`src/core/iTextSharp/text/Document.cs`, lines 107–117). This is a source-based comparison against publicly reported CVEs, not a runtime test or a complete vulnerability audit. The list is practical rather than guaranteed exhaustive.

## Findings

| CVE | Reported scope / severity | Assessment in current `src/core` | Evidence and notes |
|---|---|---|---|
| [CVE-2017-9096](https://nvd.nist.gov/vuln/detail/CVE-2017-9096) | XXE; reported for iText before 5.5.12 and iText 7 before 7.0.3. NVD severity: High (CVSS 8.8). | **Mitigated in inspected source** (confidence 9/10). | XFA processing calls are commented out in `src/core/iTextSharp/text/pdf/AcroFields.cs` (lines 115–123, 755–763, 1219–1232, 2360–2364) and `src/core/iTextSharp/text/pdf/PdfStamperImp.cs` (lines 153–158). The XFA/XML parser implementation is absent from the current source. This removes the previously identified XFA XXE path in the inspected core, but also disables XFA functionality. No runtime or built-artifact test was performed. |
| [CVE-2021-43113](https://nvd.nist.gov/vuln/detail/CVE-2021-43113) | Ghostscript command injection in iText tooling; NVD severity: Critical (CVSS 9.8). | **Not applicable to inspected core** (confidence 9/10). | `CompareTool`, `GhostscriptHelper`, and the reported Ghostscript invocation path were not found in `src/core`. Separately supplied tooling, add-ons, or downstream wrappers were not assessed. |
| [CVE-2017-20151](https://nvd.nist.gov/vuln/detail/CVE-2017-20151) | XXE in iText RUPS, a Java utility. | **Not applicable** (confidence 10/10). | The reported component is RUPS, not this .NET iTextSharp core. |
| [CVE-2022-24196](https://nvd.nist.gov/vuln/detail/CVE-2022-24196) | Denial of service in iText 7 `readStreamBytesRaw`; NVD reports versions before 7.2.2. NVD severity: Medium (CVSS 6.5). | **Not applicable to reported component** (confidence 8/10). | `readStreamBytesRaw` was not found in the inspected core. This CVE concerns the iText 7 implementation. |
| [CVE-2022-24197](https://nvd.nist.gov/vuln/detail/CVE-2022-24197) | `ByteBuffer.append` overflow / denial of service in iText 7; NVD reports versions before 7.2.2. NVD severity: Medium (CVSS 6.5). | **Cross-version applicability uncertain; not confirmed** (confidence 8/10). | A similarly named C# method exists in `src/core/iTextSharp/text/pdf/ByteBuffer.cs` (lines 203–215). Source similarity alone does not establish that the iText 7 issue is present in this fork. |
| [CVE-2022-24198](https://nvd.nist.gov/vuln/detail/CVE-2022-24198) | Reported out-of-bounds exception in iText 7.1.17 `ARCFOUREncryption.encryptARCFOUR`; disputed. NVD severity: Medium (CVSS 6.5). | **Cross-version applicability unresolved; not confirmed** (confidence 7/10). | A similarly named C# method exists in `src/core/iTextSharp/text/pdf/crypto/ARCFOUREncryption.cs` (lines 83–93). The report concerns iText 7; this review did not establish exploitable impact in this source. |
| [CVE-2023-6298](https://nvd.nist.gov/vuln/detail/CVE-2023-6298) | Array-index report for iText 8.0.2; disputed. NVD severity: Medium (CVSS 6.5). | **Not applicable to reported version; no affected path established** (confidence 9/10). | The advisory is specific to iText 8.0.2; no corresponding affected path was identified in the inspected iTextSharp core. |
| [CVE-2023-6299](https://nvd.nist.gov/vuln/detail/CVE-2023-6299) | Memory leak in iText 8.0.1, fixed in 8.0.2. NVD severity: Medium (CVSS 6.5). | **Not applicable to reported version** (confidence 9/10). | The reported iText 8 reference-table handling is not the inspected 4.1.6-derived core. |

## Limitations and follow-up

- This review checked NVD iText results, Snyk's NuGet `itextsharp` listing, and iText release notes; it should not be read as a guarantee that every CVE affecting every fork or integration has been identified.
- The review covered source under `src/core`; it did not test runtime behavior, inspect packaged binaries, or assess downstream application configuration.
- Bouncy Castle CVEs and all other dependency advisories are out of scope. The project references Bouncy Castle packages, so dependency versions should be assessed separately.
- A Snyk advisory for insufficient signature validation was noted, but it has no CVE ID and was not independently adjudicated in this CVE-only review.

---

# iTextSharp 4.1.6 CVE Review — Follow-up

**Review scope:** iTextSharp 4.1.6 source in this repository. BouncyCastle code and dependencies excluded. This is a source review and does not assess separately deployed binaries or application-specific integrations.

## Findings

No substantiated CVE-related vulnerabilities were found in the reviewed iTextSharp source.

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| — | — | — | — | No substantiated CVE findings. | — |

## CVEs and paths reviewed

- **[CVE-2017-9096](https://nvd.nist.gov/vuln/detail/CVE-2017-9096) (XXE):** The known XML parsing path appears absent from this fork. XFDF parsing is commented out in `src/core/iTextSharp/text/pdf/XfdfReader.cs` (lines 58–95), PDF metadata is returned as bytes rather than parsed as XML in `src/core/iTextSharp/text/pdf/PdfReader.cs` (lines 2227–2236), and XFA parsing is absent. This is removal of the parser path, not evidence that the old parser was hardened.
- **[CVE-2021-43113](https://nvd.nist.gov/vuln/detail/CVE-2021-43113) (Ghostscript command injection):** The vulnerable `CompareTool`/`GhostscriptHelper` path is absent from the reviewed core.
- **[CVE-2017-20151](https://nvd.nist.gov/vuln/detail/CVE-2017-20151) (iText RUPS XXE):** The reported component is Java RUPS, not this .NET iTextSharp library; RUPS and its `XfaFile` component are absent.
- **[CVE-2022-24196](https://nvd.nist.gov/vuln/detail/CVE-2022-24196), [CVE-2022-24197](https://nvd.nist.gov/vuln/detail/CVE-2022-24197), and [CVE-2022-24198](https://nvd.nist.gov/vuln/detail/CVE-2022-24198):** These concern later iText 7 implementations; no affected iTextSharp 4.1.6 path was established. CVE-2022-24198 was additionally outside the requested BouncyCastle-excluded scope.
- **[CVE-2023-6298](https://nvd.nist.gov/vuln/detail/CVE-2023-6298) and [CVE-2023-6299](https://nvd.nist.gov/vuln/detail/CVE-2023-6299):** These concern iText 8.x, not the 4.1.6-derived implementation reviewed here.

The review also checked the [NVD iText CVE search](https://services.nvd.nist.gov/rest/json/cves/2.0?keywordSearch=iText&resultsPerPage=50). CVE-2018-20170 is an OpenStack Keystone issue, and CVE-2019-11797 is rejected; neither substantiates an iTextSharp finding.
