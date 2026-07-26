# WebRequest → HttpClient migration (SYSLIB0014)

**Status: NOT STARTED.** Documented 2026-07-26 during an obsolete-warning cleanup pass. Priority: LOW — scheduled after the PDF cross-platform work (`tasks/pdf-render-callstack-analysis.md`) but before the Map engine GDI+ abstraction (`tasks/gauge-gdi-type-abstraction.md`-style effort for Map).

## Why this wasn't fixed inline

The obsolete-warning cleanup (2026-07-26) fixed the other ~470 obsolete warnings found in a full rebuild directly (DtdProcessing, CaseInsensitiveHashCodeProvider, Enum.ToString(IFormatProvider), XmlConvert date overloads, legacy exception serialization members, etc.). The `SYSLIB0014` (`WebRequest.Create` obsolete) warnings were deliberately left out of that pass and documented here instead, because a real fix isn't a call-by-call swap — it's a rewrite of this codebase's whole synchronous HTTP request/response pipeline.

## Scope

`WebRequest.Create(...)` / `HttpWebRequest` call sites (10 total):

- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Diagnostics/ExternalResourceLoader.cs:16` (x2)
- `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms.Utilities/ImageLoader.cs:115`
- `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Gauge.WebForms/ImageLoader.cs:135`
- `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Map.WebForms.BingMaps/BingMapsService.cs:33,38`
- `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Map.WebForms/ImageLoader.cs:131`
- `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Map.WebForms/MapCore.cs:3133`
- `Microsoft.ReportViewer.NETCore/Microsoft.Reporting.NETCore/WebRequestHelper.cs:24`
- `Microsoft.ReportViewer.WinForms/Microsoft.Reporting.WinForms/WebRequestHelper.cs:24`

`WebRequestHelper.GetServerUrlAccessObject` (WinForms + NETCore, near-identical duplicates) is the highest-value/highest-risk site: it returns a live `HttpWebRequest` to callers that then set additional properties, write a POST body via `GetRequestStream()`, and read the response synchronously via `GetResponse()`/`GetResponseStream()`. It also depends on `HttpWebRequest`-specific members with no 1:1 `HttpClient` equivalent used elsewhere in this codebase's request path: `.Credentials` (Windows/NTLM auth via `ICredentials`), `.CookieContainer`, custom header injection, and a `Timeout` in milliseconds set directly on the request object rather than a shared client.

## Why it's a real migration, not a warning fix

- `HttpClient` has no direct `CookieContainer`/`Credentials`-per-request model — those move to a shared `HttpClientHandler`/`SocketsHttpHandler`, which changes lifetime/pooling assumptions (`HttpClient` is meant to be reused, `HttpWebRequest` is per-call).
- The POST body write via `GetRequestStream()` and the response read via `GetResponseStream()` are synchronous; `HttpClient`'s equivalent APIs are async-first. Converting requires either introducing `async`/`await` up the call chain (bigger ripple) or sync-over-async (`.GetAwaiter().GetResult()`), which has its own deadlock/perf caveats.
- `ExceptionFromWebResponseUnwrapped` (both `WebRequestHelper.cs` files) pattern-matches on `WebException`/`WebExceptionStatus` to detect cancellation and to read the SOAP fault body off `WebException.Response`. `HttpClient` throws `HttpRequestException`/`TaskCanceledException` instead, so this error-translation logic needs a parallel rewrite, not a type swap.
- 3 of the 10 sites (`BingMapsService.cs`, the 3 `ImageLoader.cs` copies, `MapCore.cs`) live in the Map engine, which `TODO.md` already defers as a whole (GDI+ abstraction, Bing Maps EOL) — fixing their `WebRequest` usage in isolation would be wasted effort ahead of that decision.

## Recommended approach when this is picked up

1. Introduce a single `HttpClient`-based helper (likely replacing both `WebRequestHelper.cs` copies) with an async signature; audit each of the 10 call sites' callers to see how far the `async` needs to propagate.
2. Decide sync-over-async vs. full async propagation per call site — the image-loader sites (Chart/Gauge/Map) are simple GET-and-decode calls and are the easiest to convert; `WebRequestHelper.GetServerUrlAccessObject` (report-server SOAP calls with POST body, cookies, bearer/NTLM auth) is the hard case and should be tackled last, once the pattern is proven on the simple sites.
3. Rewrite `ExceptionFromWebResponseUnwrapped`'s exception translation against `HttpRequestException`/`OperationCanceledException` semantics; keep the existing SOAP `rs:MoreInformation` fault-body parsing logic (still valid — just needs to read the response body via `HttpClient`'s stream instead of `WebException.Response`).
4. Do not attempt this for the 3 Map-engine sites until the Map engine deferral (`TODO.md`) is revisited — see the Bing Maps EOL note there.
