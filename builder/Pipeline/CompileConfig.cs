using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NCBOX.Builder.Pipeline;

public sealed class CompileConfig
{
    public OptimizationConfig Optimization { get; set; } = new();
    public DebugConfig Debug { get; set; } = new();

    public static CompileConfig Load(string rootDir)
    {
        string configPath = Path.Combine(rootDir, "builder", "compile-config.json");
        if (!File.Exists(configPath))
        {
            return new CompileConfig();
        }

        try
        {
            string json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<CompileConfig>(json, options) ?? new CompileConfig();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning] Failed to parse compile-config.json: {ex.Message}. Using default settings.");
            Console.ResetColor();
            return new CompileConfig();
        }
    }

    public Dictionary<string, string> ToMsBuildProperties(bool isDebug)
    {
        var props = new Dictionary<string, string>();

        if (isDebug)
        {
            props["PublishAot"] = "false";
            props["DebugType"] = "portable";
            props["DebugSymbols"] = "true";
            return props;
        }

        props["PublishAot"] = "true";
        props["OptimizationPreference"] = Optimization.Preference;
        props["IlcFoldIdenticalMethodBodies"] = Optimization.FoldIdenticalMethodBodies ? "true" : "false";
        props["IlcTrimMetadata"] = Optimization.TrimMetadata ? "true" : "false";
        props["StripSymbols"] = Optimization.StripSymbols ? "true" : "false";
        props["IlcGenerateStackTraceData"] = Optimization.GenerateStackTraceData ? "true" : "false";
        props["InvariantGlobalization"] = Optimization.InvariantGlobalization ? "true" : "false";

        props["EventSourceSupport"] = Optimization.EventSourceSupport ? "true" : "false";
        props["DebuggerSupport"] = Optimization.DebuggerSupport ? "true" : "false";
        props["UseSystemResourceKeys"] = Optimization.UseSystemResourceKeys ? "true" : "false";
        props["HttpActivityPropagationSupport"] = Optimization.HttpActivityPropagationSupport ? "true" : "false";
        props["EnableUnsafeBinaryFormatterSerialization"] = Optimization.EnableUnsafeBinaryFormatterSerialization ? "true" : "false";
        props["EnableUnsafeUTF7Encoding"] = Optimization.EnableUnsafeUTF7Encoding ? "true" : "false";

        if (!string.IsNullOrWhiteSpace(Optimization.TrimMode))
        {
            props["TrimMode"] = Optimization.TrimMode;
        }

        if (Debug.KeepIntermediatePdbFiles)
        {
            props["DebugType"] = "portable";
            props["DebugSymbols"] = "true";
        }
        else
        {
            props["DebugType"] = "none";
            props["DebugSymbols"] = "false";
        }

        return props;
    }
}

public sealed class OptimizationConfig
{
    public string Preference { get; set; } = "Size";
    public bool FoldIdenticalMethodBodies { get; set; } = true;
    public bool TrimMetadata { get; set; } = true;
    public bool StripSymbols { get; set; } = true;
    public bool GenerateStackTraceData { get; set; } = false;
    public bool InvariantGlobalization { get; set; } = true;
    public bool EventSourceSupport { get; set; } = false;
    public bool DebuggerSupport { get; set; } = false;
    public bool UseSystemResourceKeys { get; set; } = true;
    public bool HttpActivityPropagationSupport { get; set; } = false;
    public bool EnableUnsafeBinaryFormatterSerialization { get; set; } = false;
    public bool EnableUnsafeUTF7Encoding { get; set; } = false;
    public string TrimMode { get; set; } = "full";
}

public sealed class DebugConfig
{
    public bool KeepIntermediatePdbFiles { get; set; } = true;
}
