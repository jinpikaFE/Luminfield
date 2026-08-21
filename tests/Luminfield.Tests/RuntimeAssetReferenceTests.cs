using System.Text.RegularExpressions;
using Xunit;

namespace Luminfield.Tests;

public sealed partial class RuntimeAssetReferenceTests
{
    [Fact]
    public void EveryLiteralRuntimeAssetReferenceExists()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");

        foreach (var sourcePath in Directory.EnumerateFiles(
            sourceRoot,
            "*.cs",
            SearchOption.AllDirectories
        ))
        {
            var source = File.ReadAllText(sourcePath);
            foreach (Match match in RuntimeAssetPathPattern().Matches(source))
            {
                var resourcePath = match.Value;
                var assetPath = Path.Combine(
                    repositoryRoot,
                    resourcePath["res://".Length..]
                );
                Assert.True(
                    File.Exists(assetPath),
                    $"Missing runtime asset {resourcePath} referenced by {sourcePath}."
                );
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Luminfield repository root."
        );
    }

    [GeneratedRegex("res://assets/[^\"']+")]
    private static partial Regex RuntimeAssetPathPattern();
}
