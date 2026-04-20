using Diablo4.WinUI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Helpers;

[TestClass]
public class AuthenticodeVerifierTests
{
    [TestMethod]
    public void IsTrustedAuthenticodeSigned_WhenPathIsNullOrEmpty_ReturnsFalse()
    {
        Assert.IsFalse(AuthenticodeVerifier.IsTrustedAuthenticodeSigned(string.Empty));
        Assert.IsFalse(AuthenticodeVerifier.IsTrustedAuthenticodeSigned("   "));
    }

    [TestMethod]
    public void IsTrustedAuthenticodeSigned_WhenFileDoesNotExist_ReturnsFalse()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), $"missing-exe-{Guid.NewGuid():N}.exe");

        var result = AuthenticodeVerifier.IsTrustedAuthenticodeSigned(missingFile);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsTrustedAuthenticodeSigned_WhenFileIsUnsignedDummy_ReturnsFalse()
    {
        var dummyExe = Path.Combine(Path.GetTempPath(), $"unsigned-exe-{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(dummyExe, [0x4D, 0x5A, 0x90, 0x00]); // jen MZ hlavička, ne validní EXE

        try
        {
            var result = AuthenticodeVerifier.IsTrustedAuthenticodeSigned(dummyExe);
            Assert.IsFalse(result, "Nepodepsaný dummy soubor nesmí být označen jako důvěryhodný.");
        }
        finally
        {
            if (File.Exists(dummyExe))
            {
                File.Delete(dummyExe);
            }
        }
    }
}
