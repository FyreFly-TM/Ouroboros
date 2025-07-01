using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Ouroboros.Testing.Infrastructure
{
    /// <summary>
    /// Code coverage tracking and reporting
    /// </summary>
    public class CodeCoverage
    {
        private readonly Dictionary<string, FileCoverage> fileCoverage = new();
        private readonly object lockObject = new();

        public static CodeCoverage Instance { get; } = new CodeCoverage();

        /// <summary>
        /// Track coverage for a source file
        /// </summary>
        public void TrackFile(string filePath, int totalLines)
        {
            lock (lockObject)
            {
                if (!fileCoverage.ContainsKey(filePath))
                {
                    fileCoverage[filePath] = new FileCoverage
                    {
                        FilePath = filePath,
                        TotalLines = totalLines,
                        CoveredLines = new HashSet<int>()
                    };
                }
            }
        }

        /// <summary>
        /// Mark a line as covered
        /// </summary>
        public void CoverLine(string filePath, int lineNumber)
        {
            lock (lockObject)
            {
                if (fileCoverage.TryGetValue(filePath, out var coverage))
                {
                    coverage.CoveredLines.Add(lineNumber);
                }
            }
        }

        /// <summary>
        /// Mark multiple lines as covered
        /// </summary>
        public void CoverLines(string filePath, int startLine, int endLine)
        {
            lock (lockObject)
            {
                if (fileCoverage.TryGetValue(filePath, out var coverage))
                {
                    for (int i = startLine; i <= endLine; i++)
                    {
                        coverage.CoveredLines.Add(i);
                    }
                }
            }
        }

        /// <summary>
        /// Get coverage summary
        /// </summary>
        public CoverageSummary GetSummary()
        {
            lock (lockObject)
            {
                var totalLines = fileCoverage.Values.Sum(f => f.TotalLines);
                var coveredLines = fileCoverage.Values.Sum(f => f.CoveredLines.Count);
                
                return new CoverageSummary
                {
                    TotalFiles = fileCoverage.Count,
                    TotalLines = totalLines,
                    CoveredLines = coveredLines,
                    CoveragePercentage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0,
                    FileCoverages = fileCoverage.Values.ToList()
                };
            }
        }

        /// <summary>
        /// Generate HTML coverage report
        /// </summary>
        public void GenerateHtmlReport(string outputPath)
        {
            var summary = GetSummary();
            var html = new StringBuilder();

            html.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <title>Ouroboros Code Coverage Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .summary { background: #f0f0f0; padding: 20px; border-radius: 5px; margin-bottom: 20px; }
        .file-coverage { margin-bottom: 30px; }
        .coverage-bar { width: 300px; height: 20px; background: #e0e0e0; border-radius: 3px; overflow: hidden; }
        .coverage-fill { height: 100%; background: #4CAF50; }
        .low-coverage { background: #f44336; }
        .medium-coverage { background: #ff9800; }
        .high-coverage { background: #4CAF50; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        .covered { background-color: #c8e6c9; }
        .uncovered { background-color: #ffcdd2; }
        pre { margin: 0; }
    </style>
</head>
<body>
    <h1>Ouroboros Code Coverage Report</h1>");

            // Summary section
            html.AppendLine($@"
    <div class='summary'>
        <h2>Coverage Summary</h2>
        <p>Total Files: {summary.TotalFiles}</p>
        <p>Total Lines: {summary.TotalLines:N0}</p>
        <p>Covered Lines: {summary.CoveredLines:N0}</p>
        <p>Coverage: {summary.CoveragePercentage:F2}%</p>
        <div class='coverage-bar'>
            <div class='coverage-fill {GetCoverageClass(summary.CoveragePercentage)}' style='width: {summary.CoveragePercentage}%'></div>
        </div>
    </div>");

            // File details
            html.AppendLine("<h2>File Coverage</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>File</th><th>Lines</th><th>Covered</th><th>Coverage</th></tr>");

            foreach (var file in summary.FileCoverages.OrderBy(f => f.FilePath))
            {
                var percentage = file.CoveragePercentage;
                html.AppendLine($@"
                <tr>
                    <td><a href='#{Path.GetFileNameWithoutExtension(file.FilePath)}'>{file.FilePath}</a></td>
                    <td>{file.TotalLines}</td>
                    <td>{file.CoveredLines.Count}</td>
                    <td>
                        <div class='coverage-bar' style='width: 100px; display: inline-block;'>
                            <div class='coverage-fill {GetCoverageClass(percentage)}' style='width: {percentage}%'></div>
                        </div>
                        {percentage:F2}%
                    </td>
                </tr>");
            }

            html.AppendLine("</table>");

            // Detailed file coverage
            html.AppendLine("<h2>Detailed Coverage</h2>");
            foreach (var file in summary.FileCoverages.OrderBy(f => f.FilePath))
            {
                html.AppendLine($@"
                <div class='file-coverage' id='{Path.GetFileNameWithoutExtension(file.FilePath)}'>
                    <h3>{file.FilePath}</h3>
                    <p>Coverage: {file.CoveragePercentage:F2}% ({file.CoveredLines.Count}/{file.TotalLines} lines)</p>");

                if (File.Exists(file.FilePath))
                {
                    html.AppendLine("<pre>");
                    var lines = File.ReadAllLines(file.FilePath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var lineNumber = i + 1;
                        var covered = file.CoveredLines.Contains(lineNumber);
                        var cssClass = covered ? "covered" : "uncovered";
                        html.AppendLine($"<span class='{cssClass}'>{lineNumber,4}: {System.Net.WebUtility.HtmlEncode(lines[i])}</span>");
                    }
                    html.AppendLine("</pre>");
                }

                html.AppendLine("</div>");
            }

            html.AppendLine("</body></html>");

            File.WriteAllText(outputPath, html.ToString());
        }

        /// <summary>
        /// Generate Cobertura XML report for CI integration
        /// </summary>
        public void GenerateCoberturaReport(string outputPath)
        {
            var summary = GetSummary();
            
            var xml = new XDocument(
                new XElement("coverage",
                    new XAttribute("line-rate", (summary.CoveragePercentage / 100).ToString("F4")),
                    new XAttribute("branch-rate", "0"),
                    new XAttribute("version", "1.0"),
                    new XAttribute("timestamp", DateTimeOffset.Now.ToUnixTimeSeconds()),
                    new XElement("sources",
                        new XElement("source", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    ),
                    new XElement("packages",
                        new XElement("package",
                            new XAttribute("name", "Ouroboros"),
                            new XAttribute("line-rate", (summary.CoveragePercentage / 100).ToString("F4")),
                            new XAttribute("branch-rate", "0"),
                            new XElement("classes",
                                summary.FileCoverages.Select(file =>
                                    new XElement("class",
                                        new XAttribute("name", Path.GetFileNameWithoutExtension(file.FilePath)),
                                        new XAttribute("filename", file.FilePath),
                                        new XAttribute("line-rate", (file.CoveragePercentage / 100).ToString("F4")),
                                        new XAttribute("branch-rate", "0"),
                                        new XElement("lines",
                                            Enumerable.Range(1, file.TotalLines).Select(line =>
                                                new XElement("line",
                                                    new XAttribute("number", line),
                                                    new XAttribute("hits", file.CoveredLines.Contains(line) ? "1" : "0")
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            );

            xml.Save(outputPath);
        }

        /// <summary>
        /// Generate simple text report
        /// </summary>
        public void GenerateTextReport(string outputPath)
        {
            var summary = GetSummary();
            var report = new StringBuilder();

            report.AppendLine("Ouroboros Code Coverage Report");
            report.AppendLine("==============================");
            report.AppendLine();
            report.AppendLine($"Total Files: {summary.TotalFiles}");
            report.AppendLine($"Total Lines: {summary.TotalLines:N0}");
            report.AppendLine($"Covered Lines: {summary.CoveredLines:N0}");
            report.AppendLine($"Coverage: {summary.CoveragePercentage:F2}%");
            report.AppendLine();
            report.AppendLine("File Coverage:");
            report.AppendLine("--------------");

            foreach (var file in summary.FileCoverages.OrderBy(f => f.FilePath))
            {
                report.AppendLine($"{file.FilePath,-60} {file.CoveragePercentage,6:F2}% ({file.CoveredLines.Count}/{file.TotalLines})");
            }

            File.WriteAllText(outputPath, report.ToString());
        }

        private string GetCoverageClass(double percentage)
        {
            if (percentage >= 80) return "high-coverage";
            if (percentage >= 60) return "medium-coverage";
            return "low-coverage";
        }
    }

    public class FileCoverage
    {
        public string FilePath { get; set; }
        public int TotalLines { get; set; }
        public HashSet<int> CoveredLines { get; set; }
        public double CoveragePercentage => TotalLines > 0 ? (double)CoveredLines.Count / TotalLines * 100 : 0;
    }

    public class CoverageSummary
    {
        public int TotalFiles { get; set; }
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public double CoveragePercentage { get; set; }
        public List<FileCoverage> FileCoverages { get; set; }
    }

    /// <summary>
    /// Attribute to mark methods that should be instrumented for coverage
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CoverageInstrumentAttribute : Attribute
    {
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Coverage instrumentation helper
    /// </summary>
    public static class CoverageInstrumentation
    {
        /// <summary>
        /// Instrument a method call for coverage tracking
        /// </summary>
        public static T Track<T>(string file, int startLine, int endLine, Func<T> method)
        {
            CodeCoverage.Instance.CoverLines(file, startLine, endLine);
            return method();
        }

        /// <summary>
        /// Instrument a void method call for coverage tracking
        /// </summary>
        public static void Track(string file, int startLine, int endLine, Action method)
        {
            CodeCoverage.Instance.CoverLines(file, startLine, endLine);
            method();
        }

        /// <summary>
        /// Mark a single line as covered
        /// </summary>
        public static void Line(string file, int line)
        {
            CodeCoverage.Instance.CoverLine(file, line);
        }
    }
} 