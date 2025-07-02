using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ouro.Tools.Opm
{
    /// <summary>
    /// Semantic version resolver with range support
    /// </summary>
    public class VersionResolver
    {
        private static readonly Regex VersionPattern = new Regex(
            @"^(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
            RegexOptions.Compiled
        );
        
        public class SemanticVersion : IComparable<SemanticVersion>
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Patch { get; set; }
            public string PreRelease { get; set; } = "";
            public string BuildMetadata { get; set; } = "";
            
            public static SemanticVersion Parse(string version)
            {
                var match = VersionPattern.Match(version);
                if (!match.Success)
                {
                    throw new ArgumentException($"Invalid semantic version: {version}");
                }
                
                return new SemanticVersion
                {
                    Major = int.Parse(match.Groups[1].Value),
                    Minor = int.Parse(match.Groups[2].Value),
                    Patch = int.Parse(match.Groups[3].Value),
                    PreRelease = match.Groups[4].Success ? match.Groups[4].Value : "",
                    BuildMetadata = match.Groups[5].Success ? match.Groups[5].Value : ""
                };
            }
            
            public int CompareTo(SemanticVersion? other)
            {
                if (other == null) return 1;
                
                var majorComp = Major.CompareTo(other.Major);
                if (majorComp != 0) return majorComp;
                
                var minorComp = Minor.CompareTo(other.Minor);
                if (minorComp != 0) return minorComp;
                
                var patchComp = Patch.CompareTo(other.Patch);
                if (patchComp != 0) return patchComp;
                
                // Pre-release versions have lower precedence
                if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
                    return 1;
                if (!string.IsNullOrEmpty(PreRelease) && string.IsNullOrEmpty(other.PreRelease))
                    return -1;
                    
                return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
            }
            
            public override string ToString()
            {
                var version = $"{Major}.{Minor}.{Patch}";
                if (!string.IsNullOrEmpty(PreRelease))
                    version += $"-{PreRelease}";
                if (!string.IsNullOrEmpty(BuildMetadata))
                    version += $"+{BuildMetadata}";
                return version;
            }
        }
        
        public interface IVersionRange
        {
            bool Satisfies(SemanticVersion version);
            string ToString();
        }
        
        public class ExactVersionRange : IVersionRange
        {
            private readonly SemanticVersion version;
            
            public ExactVersionRange(string version)
            {
                this.version = SemanticVersion.Parse(version);
            }
            
            public bool Satisfies(SemanticVersion version)
            {
                return this.version.CompareTo(version) == 0;
            }
            
            public override string ToString() => version.ToString();
        }
        
        public class CaretVersionRange : IVersionRange
        {
            private readonly SemanticVersion baseVersion;
            
            public CaretVersionRange(string version)
            {
                baseVersion = SemanticVersion.Parse(version.TrimStart('^'));
            }
            
            public bool Satisfies(SemanticVersion version)
            {
                if (baseVersion.Major == 0)
                {
                    if (baseVersion.Minor == 0)
                    {
                        // 0.0.x - only patch updates allowed
                        return version.Major == 0 && version.Minor == 0 && version.Patch >= baseVersion.Patch;
                    }
                    // 0.x.y - minor and patch updates allowed
                    return version.Major == 0 && version.Minor == baseVersion.Minor && version.Patch >= baseVersion.Patch;
                }
                // x.y.z - minor and patch updates allowed
                return version.Major == baseVersion.Major && 
                       (version.Minor > baseVersion.Minor || 
                        (version.Minor == baseVersion.Minor && version.Patch >= baseVersion.Patch));
            }
            
            public override string ToString() => $"^{baseVersion}";
        }
        
        public class TildeVersionRange : IVersionRange
        {
            private readonly SemanticVersion baseVersion;
            
            public TildeVersionRange(string version)
            {
                baseVersion = SemanticVersion.Parse(version.TrimStart('~'));
            }
            
            public bool Satisfies(SemanticVersion version)
            {
                // ~x.y.z - patch updates only
                return version.Major == baseVersion.Major && 
                       version.Minor == baseVersion.Minor && 
                       version.Patch >= baseVersion.Patch;
            }
            
            public override string ToString() => $"~{baseVersion}";
        }
        
        public class RangeVersionRange : IVersionRange
        {
            private readonly SemanticVersion? min;
            private readonly SemanticVersion? max;
            private readonly bool minInclusive;
            private readonly bool maxInclusive;
            
            public RangeVersionRange(string range)
            {
                var parts = range.Split(' ');
                foreach (var part in parts)
                {
                    if (part.StartsWith(">="))
                    {
                        min = SemanticVersion.Parse(part.Substring(2));
                        minInclusive = true;
                    }
                    else if (part.StartsWith(">"))
                    {
                        min = SemanticVersion.Parse(part.Substring(1));
                        minInclusive = false;
                    }
                    else if (part.StartsWith("<="))
                    {
                        max = SemanticVersion.Parse(part.Substring(2));
                        maxInclusive = true;
                    }
                    else if (part.StartsWith("<"))
                    {
                        max = SemanticVersion.Parse(part.Substring(1));
                        maxInclusive = false;
                    }
                }
            }
            
            public bool Satisfies(SemanticVersion version)
            {
                if (min != null)
                {
                    var comp = version.CompareTo(min);
                    if (minInclusive ? comp < 0 : comp <= 0)
                        return false;
                }
                
                if (max != null)
                {
                    var comp = version.CompareTo(max);
                    if (maxInclusive ? comp > 0 : comp >= 0)
                        return false;
                }
                
                return true;
            }
            
            public override string ToString()
            {
                var parts = new List<string>();
                if (min != null)
                    parts.Add($"{(minInclusive ? ">=" : ">")}{min}");
                if (max != null)
                    parts.Add($"{(maxInclusive ? "<=" : "<")}{max}");
                return string.Join(" ", parts);
            }
        }
        
        public static IVersionRange ParseRange(string range)
        {
            if (string.IsNullOrWhiteSpace(range) || range == "*" || range == "latest")
            {
                return new AnyVersionRange();
            }
            
            if (range.StartsWith("^"))
            {
                return new CaretVersionRange(range);
            }
            
            if (range.StartsWith("~"))
            {
                return new TildeVersionRange(range);
            }
            
            if (range.Contains(">") || range.Contains("<"))
            {
                return new RangeVersionRange(range);
            }
            
            return new ExactVersionRange(range);
        }
        
        public static string ResolveVersion(List<string> availableVersions, string versionRange)
        {
            var range = ParseRange(versionRange);
            var semanticVersions = availableVersions
                .Select(v => SemanticVersion.Parse(v))
                .Where(v => range.Satisfies(v))
                .OrderByDescending(v => v)
                .ToList();
                
            if (!semanticVersions.Any())
            {
                throw new InvalidOperationException($"No version satisfies range: {versionRange}");
            }
            
            return semanticVersions.First().ToString();
        }
        
        public class AnyVersionRange : IVersionRange
        {
            public bool Satisfies(SemanticVersion version) => true;
            public override string ToString() => "*";
        }
        
        /// <summary>
        /// Resolves dependency conflicts using a resolution strategy
        /// </summary>
        public static Dictionary<string, string> ResolveDependencyConflicts(
            Dictionary<string, List<string>> dependencyRequirements,
            Dictionary<string, List<string>> availableVersions)
        {
            var resolved = new Dictionary<string, string>();
            
            foreach (var package in dependencyRequirements)
            {
                var packageName = package.Key;
                var requirements = package.Value;
                var available = availableVersions.ContainsKey(packageName) 
                    ? availableVersions[packageName] 
                    : new List<string>();
                
                if (!available.Any())
                {
                    throw new InvalidOperationException($"No versions available for package: {packageName}");
                }
                
                // Find a version that satisfies all requirements
                var satisfyingVersions = available
                    .Select(v => SemanticVersion.Parse(v))
                    .Where(v => requirements.All(req => ParseRange(req).Satisfies(v)))
                    .OrderByDescending(v => v)
                    .ToList();
                
                if (!satisfyingVersions.Any())
                {
                    throw new InvalidOperationException(
                        $"No version of {packageName} satisfies all requirements: {string.Join(", ", requirements)}");
                }
                
                resolved[packageName] = satisfyingVersions.First().ToString();
            }
            
            return resolved;
        }
    }
} 