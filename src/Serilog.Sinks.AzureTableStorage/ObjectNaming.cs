using System.Text.RegularExpressions;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Defines naming restrictions for Azure Table Storage objects
/// </summary>
public static class ObjectNaming
{
    /// <summary>
    /// The regex defining characters which are disallowed for key field values.
    /// </summary>
    /// <see href="https://msdn.microsoft.com/en-us/library/azure/dd179338.aspx"/>
    public static readonly Regex KeyFieldValueCharactersNotAllowedMatch =
        new Regex(@"(\\|/|#|\?|[\x00-\x1f]|[\x7f-\x9f])");

    /// <summary>
    /// Given a <param name="keyValue">key value</param>, returns a value
    /// which has been 'cleaned' of any disallowed characters and trimmed
    /// to the allowed length.
    /// </summary>
    public static string GetValidKeyValue(string keyValue)
    {
        keyValue = KeyFieldValueCharactersNotAllowedMatch.Replace(keyValue, "");
        return keyValue.Length > 1024 ? keyValue.Substring(0, 1024) : keyValue;
    }
}
