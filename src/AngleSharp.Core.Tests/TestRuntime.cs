namespace AngleSharp.Core.Tests;

using System;

public static class TestRuntime
{
    public static Boolean UsePrefetchedTextSource { get; set; } =
        Environment.GetEnvironmentVariable("prefetched") == "true";

    public static Boolean UseUtf8StreamingTextSource { get; set; } =
        Environment.GetEnvironmentVariable("utf8streamingtextsource") == "true";
}
