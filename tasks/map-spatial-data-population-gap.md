# Map engine: spatial-element population is a no-op (data-bound AND static)

**Status: confirmed, not fixed. Found 2026-07-31 while researching a Map color-scale-legend test fixture (see `tasks/test-coverage-gaps.md` item 7 and `tasks/map-engine-cross-platform.md`'s Milestone B). This is a functional correctness bug, independent of the cross-platform/GDI+ work — present on Windows too, not a Linux-specific gap.**

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
