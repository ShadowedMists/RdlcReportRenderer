# Map engine: spatial-element population is a no-op (data-bound AND static)

**Status: static-point (embedded `MapPoints`) population fixed 2026-07-31, along with 5 independent pre-existing `NullReferenceException` bugs it exposed, and — as of a follow-up pass later the same day — the color-scale legend now genuinely renders on Windows (see "Resolved, 2026-07-31 (later pass)" below). Dataset-bound population and non-point geometry (lines/polygons) remain unfixed. This is a functional correctness bug, independent of the cross-platform/GDI+ work — present on Windows too, not a Linux-specific gap.**

## The bug

Three separate mechanisms exist in `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/` for turning RDL-defined spatial data (points/polygons/lines, whether embedded literally in the RDL or driven by a dataset) into the Map engine's actual in-memory shapes (`Microsoft.Reporting.Map.WebForms.Symbol`/`Shape`). **All three have a dead/empty core step**, meaning `MapCore.Symbols`/`MapCore.Shapes` end up with zero elements regardless of what a report author puts in a `<MapPointLayer>`/`<MapPolygonLayer>`/`<MapLineLayer>`:

1. **`SpatialDataSetMapper.ProcessRow`** (`SpatialDataSetMapper.cs:63-65`) — the dataset-bound path (`<MapSpatialDataSet><DataSetName>/<SpatialField>`). `Populate()` correctly iterates every dataset row (`m_dataSetInstance.MoveNext()`) and calls `ProcessRow(spatialFieldIndex, nonSpatialFieldInfos)` per row — but the method body is **completely empty**:
   ```csharp
   private void ProcessRow(int spatialFieldIndex, FieldInfo[] nonSpatialFieldInfos)
   {
   }
   ```
   It never reads `m_dataSetInstance.Row[spatialFieldIndex]`, never parses the geometry, never constructs a `Symbol`/`Shape`, never calls the fully-implemented (but never-invoked) sibling `ProcessNonSpatialFields(spatialElement, nonSpatialFieldInfos)`.

2. **`EmbeddedSpatialDataMapper.AddSpatialElement`** (`EmbeddedSpatialDataMapper.cs:51-53`) — the static-embedded path (`<MapPoints><MapPoint><VectorData>...`/`<MapPolygons>`/`<MapLines>`, no dataset at all). `AddSpatialElements()` correctly iterates every embedded `MapPoint`/`MapPolygon`/`MapLine` and calls `AddSpatialElement(...)` — same problem, empty body:
   ```csharp
   private void AddSpatialElement(MapSpatialElement embeddedElement)
   {
   }
   ```
   Never parses `embeddedElement`'s `VectorData` (a WKT string per the RDL schema), never builds a core shape, never calls the fully-implemented sibling `ProcessNonSpatialFields(embeddedElement, spatialElement)`.

3. **`VectorLayerMapper.CreateSpatialElementFromDataRegion`** (`VectorLayerMapper.cs:301-335`) — the `MapSpatialDataRegion` path (`<MapSpatialDataRegion><VectorData>`, spatial data supplied by another report item/expression). Not an empty method this time, but genuinely **dead code** — reads `vectorData` from `mapSpatialDataRegion.Instance.VectorData`, then:
   ```csharp
   ISpatialElement spatialElement = null;
   if (spatialElement == null)
   {
       return null;
   }
   ```
   `spatialElement` is declared `null` and never assigned from `vectorData` at all before the null-check discards it. Same shape of gap: the WKT-parsing step needed to turn `vectorData` into a real `ISpatialElement` is simply missing.

All three read like decompiler output for logic that didn't survive decompilation (the surrounding plumbing — dictionary registration, field-definition bookkeeping, rule application — is fully present and correct; only the actual "parse geometry into a shape" step is missing in every case). Given the shared shape of the gap across three independent classes, this is very likely one missing capability (WKT/geometry parsing into `Symbol`/`Shape`), not three unrelated bugs.

## Why this was invisible until now

`tasks/test-coverage-gaps.md` already noted Map had **zero automated test coverage on any platform** before 2026-07-27's `MapRdlTests.cs`, and that fixture (`SimpleMapReport.rdlc`) deliberately has **no layers at all** (just a bare `<MapViewport>`) — it was written to confirm the *existing* (already-limited) behavior works, not to exercise data-bound rendering. No test, manual or automated, appears to have ever rendered a Map with actual point/polygon/line data through this fork's `LocalReport.Render` path. The Map engine's rich internal machinery (`GroupRule.cs`/`PathRule.cs`/`ShapeRule.cs`/`SymbolRule.cs`'s color/size/marker rule application, `ColorSwatchPanel`, hit-testing, etc.) is all real and exercised by the **`Microsoft.ReportViewer.DataVisualization.VisualRegressionTests`** suite — but only by constructing `MapCore`/`Symbol`/`Shape` objects directly in C#, never through the RDL-to-Map data-population path this bug affects. That's why this is a genuine blind spot, not something anyone would have hit by accident.

## Consequence for the color-scale-legend fixture goal

This is what blocked the `tasks/test-coverage-gaps.md` item 7 fixture attempt: even with a schema-correct RDL (`DataSet` + `MapDataRegion` + `Group` + `MapPointLayer` + `MapSpatialDataSet` + `MapColorRangeRule`, worked out via research and included below for reference), `MapCore.Symbols.Count` stays 0 after rendering, which means:
- `ColorRuleMapper`'s `SymbolRule.UpdateAutoRanges()`/`ShapeRule.RegenerateColorRanges()` bail out immediately (`SymbolRule.cs:280-284`/`ShapeRule.cs:505-518`, both guard on `mapCore.Symbols.Count == 0`/`mapCore.Shapes.Count == 0`).
- Even swatches added during earlier processing get wiped by `MapCore.ApplyAllRules()` (`MapCore.cs:5400-5444`, called from every `Paint()` when `AutoUpdates` is true) before the empty-guard re-adds nothing.
- So `ColorSwatchPanel.Colors` is empty at final render time regardless of RDL correctness — not fixable from the RDL side at all.

## A reference RDL fixture (schema/property-name accurate, blocked on the bug above)

Traced via `VectorLayerMapper.CreateSpatialDataMapper` (chooses `SpatialDataSetMapper` when `MapSpatialData is MapSpatialDataSet`), `MapMember.IsStatic`/`MapDynamicMemberInstance` (dataset-row iteration), and `RuleMapper`/`ColorRuleMapper` (rule property names):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition" xmlns:rd="http://schemas.microsoft.com/SQLServer/reporting/reportdesigner">
  <DataSources>
    <DataSource Name="LocalSource">
      <ConnectionProperties>
        <DataProvider>System.Data.DataSet</DataProvider>
        <ConnectString>/* Local Connection */</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name="Data">
      <Query>
        <DataSourceName>LocalSource</DataSourceName>
        <CommandText>/* Local Query */</CommandText>
      </Query>
      <Fields>
        <Field Name="Id">
          <DataField>Id</DataField>
          <rd:TypeName>System.Int32</rd:TypeName>
        </Field>
        <Field Name="Location">
          <DataField>Location</DataField>
          <rd:TypeName>System.String</rd:TypeName>
        </Field>
        <Field Name="Value">
          <DataField>Value</DataField>
          <rd:TypeName>System.Double</rd:TypeName>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <ReportSections>
    <ReportSection>
      <Body>
        <ReportItems>
          <Map Name="SimpleMap">
            <MapDataRegions>
              <MapDataRegion Name="MapDataRegion1">
                <DataSetName>Data</DataSetName>
                <MapMember>
                  <Group Name="MapMemberGroup1">
                    <GroupExpressions>
                      <GroupExpression>=Fields!Id.Value</GroupExpression>
                    </GroupExpressions>
                  </Group>
                </MapMember>
              </MapDataRegion>
            </MapDataRegions>
            <MapLayers>
              <MapPointLayer Name="MapPointLayer1">
                <MapDataRegionName>MapDataRegion1</MapDataRegionName>
                <MapSpatialDataSet>
                  <DataSetName>Data</DataSetName>
                  <SpatialField>Location</SpatialField>
                </MapSpatialDataSet>
                <MapPointRules>
                  <MapColorRangeRule>
                    <DataValue>=Fields!Value.Value</DataValue>
                    <DistributionType>EqualInterval</DistributionType>
                    <BucketCount>5</BucketCount>
                    <StartValue>0</StartValue>
                    <EndValue>100</EndValue>
                    <ShowInColorScale>true</ShowInColorScale>
                    <StartColor>Green</StartColor>
                    <MiddleColor>Yellow</MiddleColor>
                    <EndColor>Red</EndColor>
                  </MapColorRangeRule>
                </MapPointRules>
              </MapPointLayer>
            </MapLayers>
            <MapViewport>
              <MapCoordinateSystem>Planar</MapCoordinateSystem>
            </MapViewport>
            <MapColorScale Name="MapColorScale1" />
            <Top>0in</Top>
            <Left>0in</Left>
            <Height>3in</Height>
            <Width>3in</Width>
          </Map>
        </ReportItems>
        <Height>3in</Height>
      </Body>
      <Width>3in</Width>
      <Page>
        <PageHeight>3.5in</PageHeight>
        <PageWidth>3.5in</PageWidth>
      </Page>
    </ReportSection>
  </ReportSections>
</Report>
```

Test-side data would follow the `GaugeRdlTests.cs`/`SimpleGaugeReport.rdlc` pattern (`report.DataSources.Add(new ReportDataSource("Data", rows))` with a `List<T>` of POCOs providing `Id`/`Location` (WKT, e.g. `"POINT (-122.33 47.60)"`)/`Value`).

## What a real fix would need (not attempted — scope estimate only)

- A WKT (well-known text) parser for at minimum `POINT`/`POLYGON`/`LINESTRING` (and probably `MULTI*` variants, per `Path.SaveWKT`'s own `MULTILINESTRING` output showing the engine already speaks WKT in the *export* direction — `Path.cs`'s `SaveWKT`/`SaveWKB` do the reverse conversion already, so their inverse is the missing piece, not something to invent from scratch).
- Wiring that parser into all three call sites (`SpatialDataSetMapper.ProcessRow`, `EmbeddedSpatialDataMapper.AddSpatialElement`, `VectorLayerMapper.CreateSpatialElementFromDataRegion`), each constructing the appropriate core type (`Symbol` for points, `Path`/`Shape` for lines/polygons — check `ISpatialElementCollection`/`CoreSpatialElementManager`'s exact factory methods) and calling the already-implemented `ProcessNonSpatialFields` to populate field values for rule evaluation.
- A real test fixture (the one above, or similar) to prove point/polygon rendering actually reaches pixels — this would be Map's first-ever real data-bound rendering test.
- This is a multi-day feature addition, not a bug-fix-sized change — flagged here for prioritization, not started.

## Deeper design constraint found 2026-07-31 while scoping the actual fix (read this before implementing `SpatialDataSetMapper.ProcessRow`)

A naive fix — just parse the WKT, build a `Symbol`, call `CreateSpatialElement()`/`AddSpatialElement()`/register into `m_spatialElementsDictionary` — **will not correctly drive the color rule**, even though it would make `MapCore.Symbols.Count > 0` (i.e., a naive "did it stop being empty" check would pass while the real behavior is still wrong). Traced the full render sequence in `VectorLayerMapper`:

1. `Render()` calls `PopulateSpatialElements()` (runs `SpatialDataMapper.Populate()`, walking the raw `DataSetInstance` once, **outside any live report scope**) *before* `RenderSpatialElements()` (which walks the dataset a **second time**, through the live report-processing group engine, via `MapDynamicMemberInstance.MoveNext()` at `RenderGrouping()` — the same mechanism Tablix/List row groups use).
2. For each row during this second, live-scope walk, `RenderInnerMostMember()` calls `GetSpatialElementsFromDataRegionKey()` (`VectorLayerMapper.cs:166-179`), which **returns `null` unless `m_mapVectorLayer.MapBindingFieldPairs != null`** — i.e., without `<MapBindingFieldPairs>` configured on the layer, this always returns null, and `RenderSpatialElementGroup(..., hasScope: true)` (the branch that would set `hasScope=true`) never runs for any element.
3. Elements that never got matched this way still get rendered, but through `RenderNonBoundSpatialElements()` (`VectorLayerMapper.cs:150-164`) — called with `hasScope = (mapDataRegion == null)`. Since a real `MapDataRegionName` binding means `mapDataRegion != null`, this comes out `hasScope: false`.
4. `hasScope` is not cosmetic: `PointLayerMapper.RenderPoint`/`RenderSymbolRuleFields` **only calls `ColorRuleMapper`'s `SetRuleFieldValue` when `hasScope` is true**. And `RuleMapper.SetRuleFieldValue`/`EvaluateRuleDataValue` (`RuleMapper.cs:221-246`, `482-491`) evaluates the `MapColorRangeRule`'s `DataValue` expression **live**, against whatever report scope happens to be current at the moment it runs — it is not a cached/stored value. So with `hasScope: false`, the color rule either evaluates against the wrong (parent/outer) scope or doesn't run its field-value step at all.
5. **The fix therefore needs the populate-time key and the live-grouping-time key to actually match**, so the row created during `Populate()` gets correctly recognized as "the same row" during the live walk and gets `hasScope: true`. Both keys are computed via `SpatialDataMapper.CreateCoreSpatialElementKey`/`VectorLayerMapper.CreateDataRegionSpatialElementKey`, keyed off `MapBindingFieldPairs`:
   - **Populate-time** (`SpatialDataMapper.cs:73-100`): reads the key value off the **already-stored field** on the newly-created spatial element (`coreSpatialElement[GetUniqueFieldName(layerName, bindingFieldName)]`) — meaning `ProcessNonSpatialFields` must run *before* `OnSpatialElementAdded`, and the binding field (e.g. `Id`) must be among the dataset fields copied onto the element (i.e., listed in `<MapSpatialDataSet><MapFieldNames>` too, not just used as the `<MapBindingFieldPairs>` target).
   - **Live-walk-time** (`VectorLayerMapper.cs:181-189`): evaluates the `<MapBindingFieldPair><BindingExpression>` (e.g. `=Fields!Id.Value`) **live**, against the current report scope.
   - These two values must compare equal — **confirmed a real, concrete footgun here**: `SpatialElementKey.Equals` (`SpatialElementKey.cs`) does per-value `object.Equals`, which for boxed value types requires an *exact* runtime type match (`((object)1).Equals((object)1L)` is `false` — boxed `int` vs. boxed `long` never match, let alone `int` vs. `string`). The stored field's type comes from `Field.Type`/`AddFieldDefinition`'s type inference at populate time; the live-evaluated binding expression's type comes from the RDL field's declared `<rd:TypeName>`. These must produce identical CLR types for the same logical value, or every row will silently fail to match and fall back to `hasScope: false` with no visible error.
6. This means the earlier "reference RDL fixture" in this file is necessary but **not sufficient** — it also needs a `<MapBindingFieldPairs><MapBindingFieldPair><FieldName>Id</FieldName><BindingExpression>=Fields!Id.Value</BindingExpression></MapBindingFieldPair></MapBindingFieldPairs>` block added to the `<MapPointLayer>`, and `<MapSpatialDataSet><MapFieldNames><MapFieldName>Id</MapFieldName></MapFieldNames></MapSpatialDataSet>` so `Id` actually gets stored as a field on each element during `Populate()`.

**Why this matters for whoever picks this up:** a fix that skips this key-matching design (e.g., always passing `hasScope: true` as a shortcut, or hard-coding `SpatialElementKey(null)` matching) would very likely "work" for a single-element toy fixture by accident, but silently misbehave the moment there's more than one dataset row, or would misreport the wrong row's field values against the wrong element — exactly the class of bug this codebase's own "verify for real, not just pass/fail" discipline exists to catch. Recommend writing a *multi-row* test fixture (3+ points with distinct `Value`s spanning the color range) specifically to catch a key-matching bug, not a single-point smoke test — a single-row fixture could pass even with `hasScope` wired wrong, since there'd be nothing to mismatch against.

## Static-point (embedded `MapPoints`) fix chase, 2026-07-31

Rather than the dataset-bound path (blocked on the `MapBindingFieldPairs`/`hasScope` design above), implemented the **static/embedded** path first — a `<MapPointLayer><MapPoints><MapPoint><VectorData>POINT (x y)</VectorData></MapPoint>...` layer with no `MapDataRegion` at all. This sidesteps the entire key-matching problem: `RenderNonBoundSpatialElements()` computes `hasScope = (mapDataRegion == null)`, which is unconditionally `true` for a layer with no data region, so the color rule's `DataValue` expression evaluates correctly without needing any binding-field wiring. A literal (non-expression) `<DataValue>50</DataValue>` was used rather than `=Fields!X.Value`, since static `MapPoint`s have no dataset scope to bind to at all (per the "Consequence for the color-scale-legend fixture goal" section above) — confirmed this is sufficient: `SymbolRule.UpdateColorSwatchAndLegend` (`SymbolRule.cs:399+`) populates `ColorSwatchPanel.Colors` from the rule's own static bucket configuration (`StartValue`/`EndValue`/`BucketCount`/colors), gated only on `mapCore.Symbols.Count > 0` and `PredefinedSymbols.Count > 0` — not on real per-point value variation.

**What was implemented:**
- **`WktGeometryParser.cs`** (new file) — a minimal WKT parser, `TryParsePoint(string, out MapPoint)`, handling `POINT (x y)` only. `LINESTRING`/`POLYGON`/`MULTI*` are not implemented.
- **`EmbeddedSpatialDataMapper.AddSpatialElement`** — implemented for the `Symbol` (point) case only: parses `embeddedElement.VectorData` via `WktGeometryParser`, builds a `Symbol` via `m_spatialElementManager.CreateSpatialElement()`, calls `symbol.SetPoints(...)`, `ProcessNonSpatialFields(embeddedElement, spatialElement)` (already-implemented, just never called before), `m_spatialElementManager.AddSpatialElement(spatialElement)`, and `OnSpatialElementAdded(...)` to register it in the dictionary — mirroring the exact working pattern already used by `PolygonLayerMapper.RenderPolygonCenterPoint` (a genuine, non-stub call site doing the same `CreateSpatialElement`→configure→`AddSpatialElement` sequence). Non-`Symbol` elements (i.e. a `MapPolygonLayer`'s or `MapLineLayer`'s embedded elements) are left as before (no-op) — matching the "leave a partial conversion honestly partial" convention rather than half-implementing polygon/line support.

**5 independent, pre-existing `NullReferenceException` bugs found and fixed while getting the above to actually render** (each confirmed via a real stack trace, obtained by temporarily instrumenting the relevant `catch` blocks and reverting before commit — none of these paths had ever been exercised before, since no static point had ever previously reached them):

1. **`PointLayerMapper`'s constructor** (`PointLayerMapper.cs`) only built `m_pointTemplateMapper` when `mapPointLayer.MapPointTemplate != null` (i.e., only when a report author configured a custom `<MapMarkerTemplate>`). `PolygonLayerMapper`'s constructor builds its own equivalent unconditionally — confirmed that's the correct pattern; `PointLayerMapper`'s conditional was the bug. Fixed by removing the `if` guard.
2. **`SymbolMarkerTemplateMapper.RenderPointTemplate`** dereferenced `((MapMarkerTemplate)mapPointTemplate).MapMarker` unconditionally after calling `base.RenderPointTemplate(...)` (whose own implementation *does* null-check `mapPointTemplate` first) — crashes whenever there's no custom template and no `MapMarkerRule` (the `ignoreMarker` guard only trips when a marker rule exists). Fixed by adding `|| mapPointTemplate == null` to the existing early-return check.
3. **`PointTemplateMapper.GetSize`** dereferenced `mapPointTemplate.Size` unconditionally — same root cause (no template configured). Fixed with an early `mapPointTemplate == null` check returning the existing default-size fallback.
4. **`VectorLayerMapper.GetLegendSymbolMarker`** cast `((MapMarkerTemplate)GetMapPointTemplate())` and dereferenced `.MapMarker` unconditionally. Fixed with a null check on `GetMapPointTemplate()` before the cast.
5. **`MapMapper.RenderColorScale`** called `RenderColorScaleTitle()` unconditionally even when `m_map.MapColorScale.MapColorScaleTitle` is null (no `<MapColorScaleTitle>` configured) — `SetColorScaleTitleProperties`/`RenderColorScaleTitleStyle` both dereference it without a null check. Fixed by guarding the call with `if (m_map.MapColorScale.MapColorScaleTitle != null)`, matching the exact convention `RenderDistanceScale`/`RenderBorderSkin` already use for their own optional sub-elements in the same file.

**Verified**: `dotnet build --no-incremental` 0 errors (both `Microsoft.ReportViewer.Common` and `Microsoft.ReportViewer.DataVisualization`). Full Windows suite (189+137, including the new `MapWithStaticPointsAndColorScale_RendersToImage` test) passes. WSL re-run of the Map test suite also passes (2/2).

## What's still not proven — read before treating this as "done"

**The original goal — a real, visually-confirmed color-scale legend — is still not achieved.** Two further gaps were found by actually looking at the rendered output (not just checking the tests pass):

1. **A marker renders on Windows, but the identical fixture renders as a fully blank white image on WSL.** This is a new, genuine, cross-platform discrepancy in the marker-drawing path itself (likely somewhere in `MapGraphics.CreateMarker`'s Skia-backend implementation, or a viewport/coordinate-mapping difference between backends) — not yet root-caused. Confirmed by rendering the same test on both platforms and comparing the actual PNGs, not just the well-formed-PNG assertion (which passes on both, since a blank image is still a valid PNG).
2. **The `ColorSwatchPanel` itself was never confirmed to draw anything, on either platform.** A temporary diagnostic placed at the top of `ColorSwatchPanel.Render` (writing `Colors.Count`/`IsEmpty` to a file, reverted before commit) never fired at all during the test — meaning `Render` isn't even being called for this panel. Traced one level into `MapCore.RenderPanels` (`MapCore.cs:3924+`): panels are skipped via `if (absoluteSize.Width < 1.0 || absoluteSize.Height < 1.0 || !panel.IsRenderVisible(g, bounds)) continue;` — so either `ColorSwatchPanel.GetBoundRect`/`AutoSize`/`GetOptimalSize` is producing a near-zero size, or `IsRenderVisible` is returning false, for reasons not yet investigated. This is a third, separate layer of never-before-exercised code (dockable-panel positioning), stopped here rather than chased further to avoid an open-ended session.

**Recommended next steps, in order:**
1. Root-cause the Windows-vs-WSL marker rendering discrepancy first (smaller, more contained than the panel-visibility question, and blocks any further Linux verification of Map layer content at all).
2. Trace `ColorSwatchPanel`'s `GetBoundRect`/`AutoSize`/`IsRenderVisible` path to find why the panel isn't rendered — re-add the temporary `Colors.Count` diagnostic (same pattern used above) as a first step to at least confirm whether `Colors` has entries at all (independent of whether the panel draws).
3. Only once both of the above are understood, extend `WktGeometryParser`/`EmbeddedSpatialDataMapper` to `LINESTRING`/`POLYGON`, and separately tackle the dataset-bound `MapBindingFieldPairs`/`hasScope` design described earlier in this file.
4. Do not mark `tasks/test-coverage-gaps.md` item 7 done until a rendered PNG on both platforms visibly shows color swatches — not just a non-crashing render.

## Resolved, 2026-07-31 (later pass): both open questions above are now answered

**1. Windows-vs-WSL marker discrepancy — root-caused, architectural, not a new bug.** Traced (with an agent's help) to a fact already on record in `docs/decisions.md`/`docs/platform-support.md`: **Map has no Skia backend at all** — `MapGraphics.cs`/`Viewport.cs`/`MapCore.cs` use raw `System.Drawing` types directly, with no `IGraphicsPath`/interface layer the way Chart/Gauge have. Per `docs/decisions.md`'s 2026-07-28 entry, GDI+ **cannot construct any `System.Drawing` object at all on Linux under .NET 10** — not even a bare `Font`/`Pen`/`Bitmap` — even with `libgdiplus` installed. `MapCore.Paint(graphics)` hits this the moment it needs a real `Font`/`Pen`/`Brush`. The reason this produces a **silent blank image** rather than a visible crash: `DynamicImageInstance.GetImage`'s `catch (Exception) { return CreateExceptionImage(exception); }` swallows the failure, and `CreateExceptionImage` **itself** is 100% GDI+ and fails the same way, with its own inner catch returning `null`. Two independent layers of graceful-degradation-by-design each swallow the failure, leaving a blank image with no diagnostic. This is a pre-existing, already-deferred gap (a full Skia backend for Map), not something the static-point fix introduced — no further action taken here beyond confirming and cross-referencing it.

**2. `ColorSwatchPanel` never rendering — root-caused and fixed. Two real, independent bugs found, both now fixed:**

- **Bug A (the actual blocker): a literal, non-expression `<DataValue>50</DataValue>` on a `MapColorRangeRule` evaluates to a `string` ("50"), not a numeric type.** Traced via a `GetBucketCount`/`IsRuleFieldScalar` diagnostic chase: `RuleMapper.EvaluateRuleDataValue()` returns `dataValue.Value` for a non-expression `ReportVariantProperty`, and for a literal RDL value without an explicit type, `ExpressionInfo.Value` is the raw string `"50"` — never coerced to a number. `RuleMapper.SetRuleFieldValue` (called once per point during `RenderSpatialElements()`) then calls `CoreSpatialElementManager.GetFieldType("50")`, which hits `TypeCode.String`'s default case and registers the rule field as `typeof(string)`. Later, `RuleMapper.IsRuleFieldScalar` checks `FieldDefinitions[Field].Type != typeof(string)` — since it *is* `typeof(string)`, this returns `false`, which routes `GetBucketCount()` through `GetDistinctValuesCount(field)` instead of returning the RDL's real `<BucketCount>5</BucketCount>`. `GetDistinctValuesCount` in turn filters spatial elements by `spatialElement.Layer == m_mapVectorLayer.Name` (see Bug B) — with zero elements passing that filter, it returns `0`, so `SetSymbolRuleColors`'s `for (i = 0; i < bucketCount; i++)` loop never runs, and `PredefinedSymbols` (hence `ColorSwatchPanel.Colors`) stays empty. **This is a test-fixture bug, not a product bug**: real RDL authoring convention is to write numeric constants as expressions (`=50`), not bare literals, precisely so they get typed correctly — `MapColorScaleReport.rdlc`'s `<DataValue>50</DataValue>` was simply the wrong syntax. **Fix**: changed the fixture to `<DataValue>=50</DataValue>`.
- **Bug B (a real, latent product bug, fixed defensively alongside Bug A): `EmbeddedSpatialDataMapper.AddSpatialElement` never set `Symbol.Layer`.** `CoreSpatialElementManager.GetSpatialElementCount()`/`GetDistinctValuesCount()` both filter on `spatialElement.Layer == m_mapVectorLayer.Name` — since the new `AddSpatialElement` code (added earlier the same day) never set `.Layer`, both methods always return `0` for embedded/static layers regardless of how many elements actually exist, silently breaking any rule that depends on them (e.g. `SymbolField == "(Name)"`'s distinct-bucket-per-point mode, or the `IsRuleFieldScalar == false` fallback hit by Bug A above). **Fix**: `EmbeddedSpatialDataMapper.AddSpatialElement` now sets `symbol.Layer = m_mapVectorLayer.Name` right after `SetPoints`, mirroring what `GetSpatialElementCount`/`GetDistinctValuesCount`'s filter design clearly intends but which no existing code path (not even `PolygonLayerMapper`'s working center-point path) actually did — this codebase apparently never previously had a working code path that populated `.Layer` for population-time-created elements at all.
- **A third, unrelated fixture-only bug found and fixed along the way: the `<MapViewport><MapSize><Width>3</Width><Height>3</Height></MapSize></MapViewport>` in the original fixture set the *viewport itself* to a near-zero absolute size** (confirmed via an `AdjustAutoSize` diagnostic: `Common.MapCore.Viewport.GetSizeInPixels()` came back tiny, making `empty.Width`/`empty.Height` go *negative* after subtracting margins, tripping `AutoSizePanel`'s `<= 0.1` fallback that pins any docked panel to a 0.1×0.1px box regardless of its real content). This was also why the very first Windows render showed all 3 markers clustered in one corner — the entire viewport was only a few pixels across, not the panel/legend problem it first looked like. **Fix**: removed the incorrect `<MapSize>` override; `<MapViewport>` now just declares `<MapCoordinateSystem>Planar</MapCoordinateSystem>` (matching the working reference fixture earlier in this file) and lets the viewport default to filling the map area.

**Verified by direct visual inspection** (not just pass/fail) after all three fixes: rendered `MapColorScaleReport.rdlc` on Windows now shows a genuine green→yellow→orange→red color-scale legend with `0`/`20`/`40`/`60`/`80`/`100` labels, and all 3 markers appear spread correctly across the map at their real `(0,0)`/`(1,1)`/`(2,2)` positions (no more corner-clustering). WSL renders the same known blank (per point 1 above) — not a regression, unchanged from before.

**`tasks/test-coverage-gaps.md` item 7 can now be marked done** — the color-scale legend has been proven to render, on Windows, with a real rendered-and-inspected PNG, not just a non-crashing test.

**What's genuinely still open, for a future pass:** dataset-bound (real per-row data via `MapBindingFieldPairs`/`hasScope`) population remains unimplemented (see the design-constraint section above); `LINESTRING`/`POLYGON` WKT parsing remains unimplemented; a full Skia backend for Map remains deferred/out of scope.
