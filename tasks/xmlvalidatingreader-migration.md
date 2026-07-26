# XmlValidatingReader → XmlReader+XmlReaderSettings migration (CS0618)

**Status: NOT STARTED.** Documented 2026-07-26 during an obsolete-warning cleanup pass. Priority: LOW — same tier as `tasks/webrequest-httpclient-migration.md`, deferred for the same reason: it's a real redesign, not a warning fix.

## Scope

`Microsoft.ReportViewer.Common/Microsoft.ReportingServices.ReportProcessing/RDLValidatingReader.cs` derives from `XmlValidatingReader` (obsolete, CS0618: "Use XmlReader created by XmlReader.Create() method using appropriate XmlReaderSettings instead"). Its subclass, `ReportPublishing.cs`'s nested `RmlValidatingReader`, is the active RDL/RML schema validator used when loading report definitions — this runs against untrusted report XML, so correctness here is security-relevant, not just a style concern.

## Why this wasn't fixed inline

`RDLValidatingReader`/`RmlValidatingReader` use inheritance (`: XmlValidatingReader`) and rely on members that have no direct equivalent on `XmlReader`:

- `base.SchemaType` — on a schema-validating `XmlReader` (created via `XmlReaderSettings.ValidationType = ValidationType.Schema`), the equivalent lives on `IXmlSchemaInfo` via `reader.SchemaInfo`, not a `SchemaType` property.
- `base.Schemas` (an `XmlSchemaSet`) — moves to `XmlReaderSettings.Schemas`.
- `base.ValidationEventHandler`/`base.ValidationType` — move to `XmlReaderSettings.ValidationEventHandler`/`.ValidationType`, set before calling `XmlReader.Create`, not mutable after the fact the same way.
- `base.EOF`, `base.NodeType`, `base.NamespaceURI`, `base.Read()`, `base.Skip()` — all exist on `XmlReader` too, but since `XmlReader.Create` returns an `XmlReader` instance rather than something subclassable in the same way, the whole class needs to change from **inheriting** `XmlValidatingReader` to **wrapping** an `XmlReader` (composition), forwarding each member it currently gets "for free" via inheritance.

This is a from-scratch redesign of the class shape (inheritance → wrapping), not a call-by-call swap, and it sits directly in the path that validates report XML before processing — worth doing carefully and in isolation, not as a drive-by fix alongside a warning sweep.

## Recommended approach when this is picked up

1. Rewrite `RDLValidatingReader` (`Microsoft.ReportingServices.ReportProcessing` namespace) as a wrapper holding an inner `XmlReader` built via `XmlReader.Create(innerReader, settings)`, with `settings.ValidationType = ValidationType.Schema` and the schema set assigned to `settings.Schemas`.
2. Re-derive `Validate(out string message)`'s current use of `base.SchemaType` from `((IXmlSchemaInfo)reader.SchemaInfo)?.SchemaType` instead.
3. Update `RmlValidatingReader` (`ReportPublishing.cs`) the same way — it adds a schema at construction time (`base.Schemas.Add(...)`) and overrides `Read()`, both of which need the settings-based equivalents.
4. There is a second, unrelated `RDLValidatingReader` in the `Microsoft.ReportingServices.ReportPublishing` namespace (`RDLValidatingReader.cs` under `ReportPublishing/`) — it does NOT derive from `XmlValidatingReader` (only has the `XmlReaderSettings.ProhibitDtd`-style CS0618, already fixed separately) and is out of scope for this migration.
5. Validate against the existing RDL/RML test suite (report definitions with intentionally invalid schema to confirm validation errors still surface with the same messages/line numbers) before considering this done — this is the one CS0618 site in the whole obsolete-warning sweep with actual behavioral risk if the schema-info plumbing is subtly wrong.
