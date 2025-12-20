using System.Runtime.InteropServices;
using System.Text;

namespace DataUsageReporter.Data;

/// <summary>
/// Windows Credential Manager wrapper for secure credential storage.
/// </summary>
public class CredentialManager : ICredentialManager
{
    private const string TargetPrefix = "DataUsageReporter:";

    public void Store(string key, string username, string password)
    {
        var targetName = TargetPrefix + key;
        var passwordBytes = Encoding.Unicode.GetBytes(password);

        var credential = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = targetName,
            UserName = username,
            CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length),
            CredentialBlobSize = (uint)passwordBytes.Length,
            Persist = CRED_PERSIST.LOCAL_MACHINE,
            Comment = "Data Usage Reporter SMTP credentials"
        };

        try
        {
            Marshal.Copy(passwordBytes, 0, credential.CredentialBlob, passwordBytes.Length);

            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException($"Failed to store credential. Error: {Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(credential.CredentialBlob);
        }
    }

    public (string Username, string Password)? Retrieve(string key)
    {
        var targetName = TargetPrefix + key;

        if (!CredRead(targetName, CRED_TYPE.GENERIC, 0, out var credentialPtr))
        {
            return null;
        }

        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
            var username = credential.UserName ?? string.Empty;

            var passwordBytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, (int)credential.CredentialBlobSize);
            var password = Encoding.Unicode.GetString(passwordBytes);

            return (username, password);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public void Delete(string key)
    {
        var targetName = TargetPrefix + key;
        CredDelete(targetName, CRED_TYPE.GENERIC, 0);
    }

    #region Windows Credential Manager P/Invoke

    private enum CRED_TYPE : uint
    {
        GENERIC = 1
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string targetName, CRED_TYPE type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredDelete(string targetName, CRED_TYPE type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr credential);

    #endregion
}
