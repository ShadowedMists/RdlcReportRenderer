using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises SunburstChart through the real RDL rendering path
    /// (ChartMapper.RenderCategoryGrouping populating ChartArea.CategoryNodes from the report's
    /// ChartCategoryHierarchy), as opposed to
    /// Microsoft.ReportViewer.DataVisualization.VisualRegressionTests, which builds Chart/Series
    /// objects directly in C# and can never reach that code path. See
    /// tasks/chart-gdi-type-abstraction.md for the SunburstChart.Name bug fix and the
    /// investigation that led to this test existing here instead of there.
    /// </summary>
    [TestClass]
    public class SunburstChartRdlTests
    {
        [TestMethod]
        public void SunburstChartWithCategoryHierarchy_MatchesBaseline()
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "SunburstReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            var rows = new List<SunburstDataRow>
            {
                new() { Category = "Fruit", SubCategory = "Apple", Value = 10 },
                new() { Category = "Fruit", SubCategory = "Banana", Value = 15 },
                new() { Category = "Vegetable", SubCategory = "Carrot", Value = 20 },
                new() { Category = "Vegetable", SubCategory = "Pea", Value = 5 },
            };
            report.DataSources.Add(new ReportDataSource("Data", rows));

            const string deviceInfo = "<DeviceInfo><OutputFormat>PNG</OutputFormat></DeviceInfo>";
            var actual = report.Render("IMAGE", deviceInfo);

            var result = ImageComparer.CompareToBaseline(actual, "SunburstChartWithCategoryHierarchy.png");
            Assert.IsTrue(result.Matches, result.Message);
        }
    }
}
