namespace DataUsageReporter.Data;

/// <summary>
/// Secure credential storage wrapper using Windows Credential Manager.
/// </summary>
public interface ICredentialManager
{
    /// <summary>
    /// Stores credentials securely.
    /// </summary>
    void Store(string key, string username, string password);

    /// <summary>
    /// Retrieves stored credentials.
    /// </summary>
    (string Username, string Password)? Retrieve(string key);

    /// <summary>
    /// Deletes stored credentials.
    /// </summary>
    void Delete(string key);
}
