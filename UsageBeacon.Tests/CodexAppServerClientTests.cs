using System.Reflection;
using System.Text.Json;
using UsageBeacon.Services;

namespace UsageBeacon.Tests;

public sealed class CodexAppServerClientTests
{
    [Fact]
    public void Window_UsesNoResetSentinel_WhenResetsAtIsMissing()
    {
        var dto = JsonSerializer.Deserialize<CodexRateLimitsDto>(
            """
            {
                "rateLimits": {
                    "primary": { "usedPercent": 40, "windowDurationMins": 10080 }
                }
            }
            """)!;

        var weekly = dto.WeeklyRateLimit();

        Assert.NotNull(weekly);
        Assert.Equal(DateTime.MinValue, weekly!.ResetsAt);
        Assert.Equal(0.40, weekly.Utilization, precision: 10);
    }

    [Fact]
    public void Window_AcceptsFractionalUsedPercent()
    {
        var dto = JsonSerializer.Deserialize<CodexRateLimitsDto>(
            """
            {
                "rateLimits": {
                    "primary": {
                        "usedPercent": 12.5,
                        "windowDurationMins": 300,
                        "resetsAt": 1700000000
                    }
                }
            }
            """)!;

        var fiveHour = dto.FiveHourRateLimit();

        Assert.NotNull(fiveHour);
        Assert.Equal(0.125, fiveHour!.Utilization, precision: 10);
        Assert.Equal(
            DateTimeOffset.FromUnixTimeSeconds(1700000000).LocalDateTime,
            fiveHour.ResetsAt);
    }

    [Fact]
    public void ResolveExecutable_PrefersNvmSymlinkOverPath()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("UsageBeaconTests-");
        var nvmDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "nvm"));
        var pathDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "path"));
        var originalNvmSymlink = Environment.GetEnvironmentVariable("NVM_SYMLINK");
        var originalPath = Environment.GetEnvironmentVariable("PATH");

        try
        {
            var nvmCommandPath = Path.Combine(nvmDirectory.FullName, "codex.cmd");
            File.WriteAllText(nvmCommandPath, "@echo off");
            File.WriteAllText(Path.Combine(pathDirectory.FullName, "codex.exe"), string.Empty);
            Environment.SetEnvironmentVariable("NVM_SYMLINK", nvmDirectory.FullName);
            Environment.SetEnvironmentVariable("PATH", pathDirectory.FullName);

            var cl = new CodexAppServerClient();
            var method = typeof(CodexAppServerClient).GetMethod(
                "ResolveExecutable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var result = method.Invoke(cl, null);

            Assert.Equal(nvmCommandPath, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NVM_SYMLINK", originalNvmSymlink);
            Environment.SetEnvironmentVariable("PATH", originalPath);
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void ResolveExecutable_SkipsUnsupportedWhereResult()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("UsageBeaconTests-");
        var originalPath = Environment.GetEnvironmentVariable("PATH");

        try
        {
            var extensionlessPath = Path.Combine(tempDirectory.FullName, "codex");
            var commandPath = Path.Combine(tempDirectory.FullName, "codex.cmd");
            File.WriteAllText(extensionlessPath, string.Empty);
            File.WriteAllText(commandPath, "@echo off");
            Environment.SetEnvironmentVariable("PATH", tempDirectory.FullName);

            var cl = new CodexAppServerClient([]);
            var method = typeof(CodexAppServerClient).GetMethod(
                "ResolveExecutable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var result = method.Invoke(cl, null);

            Assert.Equal(commandPath, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            tempDirectory.Delete(recursive: true);
        }
    }
}
