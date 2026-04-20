using System;
using System.Runtime.InteropServices;

namespace Diablo4.WinUI.Helpers;

/// <summary>
/// Ověření Authenticode podpisu spustitelného souboru přes WinVerifyTrust API.
/// Slouží jako "soft" varovný signál – nepoužívat jako jediný bezpečnostní kontrolní bod.
/// </summary>
internal static class AuthenticodeVerifier
{
    private const uint WtdUiNone = 2;
    private const uint WtdRevokeNone = 0;
    private const uint WtdChoiceFile = 1;
    private const uint WtdStateActionVerify = 1;
    private const uint WtdStateActionClose = 2;
    private const uint WtdSafer = 1;

    private static readonly Guid WintrustActionGenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    internal static bool IsTrustedAuthenticodeSigned(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var fileInfoPtr = IntPtr.Zero;
        var dataPtr = IntPtr.Zero;

        try
        {
            var fileInfo = new WintrustFileInfo
            {
                cbStruct = (uint)Marshal.SizeOf<WintrustFileInfo>(),
                pcwszFilePath = filePath,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WintrustFileInfo>());
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var data = new WintrustData
            {
                cbStruct = (uint)Marshal.SizeOf<WintrustData>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WtdUiNone,
                fdwRevocationChecks = WtdRevokeNone,
                dwUnionChoice = WtdChoiceFile,
                pInfoUnion = fileInfoPtr,
                dwStateAction = WtdStateActionVerify,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = WtdSafer,
                dwUIContext = 0,
                pSignatureSettings = IntPtr.Zero
            };

            dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WintrustData>());
            Marshal.StructureToPtr(data, dataPtr, false);

            var actionId = WintrustActionGenericVerifyV2;
            int verifyResult = WinVerifyTrust(IntPtr.Zero, ref actionId, dataPtr);

            data = Marshal.PtrToStructure<WintrustData>(dataPtr);
            data.dwStateAction = WtdStateActionClose;
            Marshal.StructureToPtr(data, dataPtr, false);
            _ = WinVerifyTrust(IntPtr.Zero, ref actionId, dataPtr);

            return verifyResult == 0;
        }
        catch (DllNotFoundException ex)
        {
            AppDiagnostics.LogWarning("WinVerifyTrust knihovna není dostupná, kontrola podpisu byla přeskočena.", ex);
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            AppDiagnostics.LogWarning("WinVerifyTrust API není dostupné, kontrola podpisu byla přeskočena.", ex);
            return false;
        }
        finally
        {
            if (dataPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(dataPtr);
            }

            if (fileInfoPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(fileInfoPtr);
            }
        }
    }

    [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, IntPtr pWVTData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WintrustFileInfo
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WintrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pInfoUnion;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }
}
