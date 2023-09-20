﻿using System;
using System.Reflection;

namespace Qualia.Tools;

public static class VersionHelper
{
    public static (string, string) GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var buildDate = new DateTime(2000, 1, 1)
            .AddDays(version.Build).AddSeconds(version.Revision * 2);

        return ($"{version}", $"{buildDate.ToString("f", Culture.Current)}");
    }
}