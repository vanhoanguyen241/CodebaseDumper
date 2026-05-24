// CodebaseDumper.Tests/Engine/TreeBuilderTests.cs

using System;
using System.Collections.Generic;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using Xunit;

namespace CodebaseDumper.Tests.Engine;

/// <summary>
/// Kiểm thử snapshot cho <see cref="TreeBuilder"/>.
/// </summary>
public class TreeBuilderTests
{
    private readonly ITreeBuilder _builder = new TreeBuilder();
    private const string RootPath = "/fakepath/project";

    [Fact]
    public void BuildEmptyList_ReturnsRootOnly()
    {
        var files = Array.Empty<FileEntry>();

        string result = _builder.Build(files, RootPath);

        Assert.Equal("./", result);
    }

    [Fact]
    public void BuildSingleFile_ReturnsRootAndFile()
    {
        var files = new[]
        {
            new FileEntry("index.js", "/fakepath/project/index.js", 123)
        };

        string result = _builder.Build(files, RootPath);

        string expected = "./" + Environment.NewLine + "└── index.js";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildNestedDirs_UsesCorrectBoxChars()
    {
        var files = new[]
        {
            new FileEntry("src/index.js", "/fakepath/project/src/index.js", 100),
            new FileEntry("src/components/Header.tsx", "/fakepath/project/src/components/Header.tsx", 200),
            new FileEntry("src/components/Footer.tsx", "/fakepath/project/src/components/Footer.tsx", 150),
            new FileEntry("src/utils/helpers.ts", "/fakepath/project/src/utils/helpers.ts", 80)
        };

        string result = _builder.Build(files, RootPath);

        string expected = string.Join(Environment.NewLine,
            "src/",
            "├── index.js",
            "├── components/",
            "│   ├── Header.tsx",
            "│   └── Footer.tsx",
            "└── utils/",
            "    └── helpers.ts"
        );
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildMultipleTopLevel_OrderedDirsFirst()
    {
        var files = new[]
        {
            new FileEntry("lib/util.js", "/fakepath/project/lib/util.js", 10),
            new FileEntry("src/index.js", "/fakepath/project/src/index.js", 20),
            new FileEntry("README.md", "/fakepath/project/README.md", 30)
        };

        string result = _builder.Build(files, RootPath);

        string expected = string.Join(Environment.NewLine,
            "./",
            "├── lib/",
            "│   └── util.js",
            "├── src/",
            "│   └── index.js",
            "└── README.md"
        );
        Assert.Equal(expected, result);
    }
}