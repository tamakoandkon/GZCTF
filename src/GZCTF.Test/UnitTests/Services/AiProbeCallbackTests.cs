using GZCTF.Services.Container.Manager;
using Xunit;

namespace GZCTF.Test.UnitTests.Services;

/// <summary>
/// Tests for the decoy-flag callback derivation used by the AI probe sidecar.
/// </summary>
public class AiProbeCallbackTests
{
    [Theory]
    [InlineData("http://10.0.0.5:30000/api/ai-detection/report",
        "http://10.0.0.5:30000/api/ai-detection/marked-flag")]
    [InlineData("http://10.0.0.5:30000/api/ai-detection/report/",
        "http://10.0.0.5:30000/api/ai-detection/marked-flag")]
    [InlineData("https://ctf.example.com/report", "https://ctf.example.com/marked-flag")]
    [InlineData("https://ctf.example.com/custom/hook", "https://ctf.example.com/custom/hook/marked-flag")]
    public void DerivesMarkedFlagCallback(string reportUrl, string expected)
    {
        Assert.Equal(expected, AiProbeScript.MarkedFlagCallback(reportUrl));
    }
}
