using System;
using System.Text;
using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;

namespace Serilog.Sinks.AzureTableStorage.Sinks.KeyGenerator
{
    public class PropertiesKeyGenerator : DefaultKeyGenerator
    {
        // Valid RowKey name characters
        static readonly Regex _rowKeyNotAllowedMatch = new Regex(@"(\\|/|#|\?|[\x00-\x1f]|[\x7f-\x9f])");

        /// <summary>
        /// Generate a valid string for a table property key by removing invalid characters
        /// </summary>
        /// <param name="s">
        /// The input string
        /// </param>
        /// <returns>
        /// The string that can be used as a property
        /// </returns>
        public static string GetValidStringForTableKey(string s)
        {
            return _rowKeyNotAllowedMatch.Replace(s, "");
        }

        /// <summary>
        /// Automatically generates the RowKey using the following template: {Level|MessageTemplate|IncrementedRowId}
        /// </summary>
        /// <param name="logEvent">the log event</param>
        /// <param name="additionalRowKeyPostfix">suffix for the RowKey</param>
        /// <returns>The generated RowKey</returns>
        public override string GenerateRowKey(LogEvent logEvent, string additionalRowKeyPostfix = null)
        {
            var prefixBuilder = new StringBuilder(512);

            // Join level and message template
            prefixBuilder.Append(logEvent.Level).Append('|').Append(GetValidStringForTableKey(logEvent.MessageTemplate.Text));

            var postfixBuilder = new StringBuilder(512);

            if (additionalRowKeyPostfix != null)
                postfixBuilder.Append('|').Append(GetValidStringForTableKey(additionalRowKeyPostfix));

            // Append GUID to postfix
            postfixBuilder.Append('|').Append(Guid.NewGuid());

            // Truncate prefix if too long
            var maxPrefixLength = 1024 - postfixBuilder.Length;
            if (prefixBuilder.Length > maxPrefixLength)
            {
                prefixBuilder.Length = maxPrefixLength;
            }

            return prefixBuilder.Append(postfixBuilder).ToString();
        }
    }
}
