// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage;

#pragma warning disable CA1308 // Normalize strings to uppercase

internal sealed class Options
{
    private enum ParseResult { Success, Unknown, Invalid };

    public bool ShowHelpOnly { get; set; }
    public OptionValue<int> HitThreshold { get; set; } = new(1);
    public OptionValue<int> CoverageThreshold { get; set; } = new(100);
    public OptionValue<int> BranchThreshold { get; set; } = new(2);
    public OptionValue<bool> LatestOnly { get; set; }
    public OptionValue<bool> NoCollapse { get; set; }
    public OptionValue<int> MaxLineNumber { get; set; } = new(50_000);
    public OptionValue<bool> NoLogo { get; set; }
    public OptionValue<VerbosityLevel> Verbosity { get; set; } = new(VerbosityLevel.Normal);
    public List<string> GlobPatterns { get; } = [];

    public void ParseCommandLineArguments(string[] args)
    {
        if (args == null)
        {
            return;
        }
        if (args.Length > 0 && args[0] == "/?")
        {
            ShowHelpOnly = true;
            return;
        }

        char[] valueSeparators = [':', '='];
        bool canHaveOptions = true;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (canHaveOptions && arg.StartsWith('-'))
            {
                if (arg == "--")
                {
                    canHaveOptions = false;
                }
                else
                {
                    arg = arg[1..].ToLowerInvariant();
                    if (arg.StartsWith('-')) arg = arg[1..];
                    ParseResult result = ParseSwitch(arg);
                    if (ShowHelpOnly)
                    {
                        return;
                    }
                    if (result != ParseResult.Success)
                    {
                        string? nextArg = (i + 1) < args.Length ? args[i + 1] : null;
                        bool takeNext = true;
                        int pos = arg.IndexOfAny(valueSeparators);
                        if (pos >= 0)
                        {
                            if ((pos + 1) < arg.Length)
                            {
                                nextArg = arg[(pos + 1)..];
                                takeNext = false;
                            }
                            arg = arg[..pos];
                        }
                        result = ParseIntOption(arg, nextArg);
                        if (result == ParseResult.Unknown)
                        {
                            result = ParseStringOption(arg, nextArg);
                        }
                        switch (result)
                        {
                            case ParseResult.Unknown:
                                throw new ArgumentException($"Unknown command line argument {args[i]}");
                            case ParseResult.Invalid:
                                throw new ArgumentException("Invalid command line argument"
                                    + $" {args[i]} {(takeNext ? nextArg : string.Empty)}");
                            default:
                                if (takeNext) i++;
                                break;
                        }
                    }
                }
            }
            else if (!GlobPatterns.Contains(arg))
            {
                GlobPatterns.Add(arg);
            }
        }
    }

    private ParseResult ParseSwitch(string @switch)
    {
        ParseResult result = ParseResult.Success;
        switch (@switch)
        {
            case "h" or "help" or "?":
                ShowHelpOnly = true;
                break;
            case "lo" or "latest-only" or "latestonly":
                LatestOnly = true;
                break;
            case "nologo" or "no-logo":
                NoLogo = true;
                break;
            case "no-collapse" or "nocollapse":
                NoCollapse = true;
                break;
            default:
                result = ParseResult.Unknown;
                break;
        }
        return result;
    }

    private ParseResult ParseIntOption(string option, string? value)
    {
        bool found = true;
        bool parsed = int.TryParse(value, out int intVal);
        bool valid = false;
        switch (option)
        {
            case "ht" or "hit-threshold" or "hitthreshold":
                if (parsed && intVal >= 0)
                {
                    HitThreshold = intVal;
                    valid = true;
                }
                break;
            case "ct" or "coverage-threshold" or "coveragethreshold":
                if (parsed && intVal >= 0 && intVal <= 100)
                {
                    CoverageThreshold = intVal;
                    valid = true;
                }
                break;
            case "bt" or "branch-threshold" or "branchthreshold":
                if (parsed && intVal >= 0)
                {
                    BranchThreshold = intVal;
                    valid = true;
                }
                break;
            case "max-linenumber" or "maxlinenumber":
                if (parsed && intVal >= 1)
                {
                    MaxLineNumber = intVal;
                    valid = true;
                }
                break;
            case "v" or "verbosity":
                if (!parsed)
                {
                    found = false; // Strings will be parsed later
                }
                else if (Enum.IsDefined((VerbosityLevel)intVal))
                {
                    Verbosity = (VerbosityLevel)intVal;
                    valid = true;
                }
                break;
            default:
                found = false;
                break;
        }
        Debug.Assert((valid && parsed) || !valid);
        return found ? (valid ? ParseResult.Success : ParseResult.Invalid) : ParseResult.Unknown;
    }

    private ParseResult ParseStringOption(string option, string? value)
    {
        bool found = true;
        bool valid = false;
        switch (option)
        {
            case "v" or "verbosity":
                if (Enum.TryParse(value, ignoreCase: true, out VerbosityLevel level) && Enum.IsDefined(level))
                {
                    Verbosity = level;
                    valid = true;
                }
                else
                {
                    valid = true;
                    switch (value?.ToLowerInvariant())
                    {
                        case "q":
                            Verbosity = VerbosityLevel.Quiet;
                            break;
                        case "m":
                            Verbosity = VerbosityLevel.Minimal;
                            break;
                        case "n":
                            Verbosity = VerbosityLevel.Normal;
                            break;
                        case "d":
                            Verbosity = VerbosityLevel.Detailed;
                            break;
                        case "diag":
                            Verbosity = VerbosityLevel.Diagnostic;
                            break;
                        default:
                            valid = false;
                            break;
                    }
                }
                break;
            default:
                found = false;
                break;
        }
        return found ? (valid ? ParseResult.Success : ParseResult.Invalid) : ParseResult.Unknown;
    }

    public Options Clone()
    {
        Options clone = new()
        {
            ShowHelpOnly = ShowHelpOnly,
            HitThreshold = HitThreshold,
            CoverageThreshold = CoverageThreshold,
            BranchThreshold = BranchThreshold,
            LatestOnly = LatestOnly,
            NoCollapse = NoCollapse,
            MaxLineNumber = MaxLineNumber,
            NoLogo = NoLogo,
            Verbosity = Verbosity,
        };
        clone.GlobPatterns.AddRange(GlobPatterns);
        return clone;
    }

    // Union with other options (e.g. from settings files)
    // Rules:
    // - ignore ShowHelpOnly
    // - if (other.OptionValue.IsSet && !this.OptionValue.IsSet) this.OptionValue = other.OptionValue
    // - GlobPatterns: treat like HashSet.UnionWith with casesensitive comparer.
    public void UnionWith(Options other)
    {
        if (other.HitThreshold.IsSet && !HitThreshold.IsSet)
            HitThreshold = other.HitThreshold;
        if (other.CoverageThreshold.IsSet && !CoverageThreshold.IsSet)
            CoverageThreshold = other.CoverageThreshold;
        if (other.BranchThreshold.IsSet && !BranchThreshold.IsSet)
            BranchThreshold = other.BranchThreshold;
        if (other.LatestOnly.IsSet && !LatestOnly.IsSet)
            LatestOnly = other.LatestOnly;
        if (other.NoCollapse.IsSet && !NoCollapse.IsSet)
            NoCollapse = other.NoCollapse;
        if (other.MaxLineNumber.IsSet && !MaxLineNumber.IsSet)
            MaxLineNumber = other.MaxLineNumber;
        if (other.NoLogo.IsSet && !NoLogo.IsSet)
            NoLogo = other.NoLogo;
        if (other.Verbosity.IsSet && !Verbosity.IsSet)
            Verbosity = other.Verbosity;

        foreach (string glob in other.GlobPatterns)
        {
            if (!GlobPatterns.Contains(glob)) GlobPatterns.Add(glob);
        }
    }
}
