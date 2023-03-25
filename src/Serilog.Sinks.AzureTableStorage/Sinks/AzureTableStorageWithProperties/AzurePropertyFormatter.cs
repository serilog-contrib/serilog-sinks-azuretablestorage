// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Events;
using System;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Converts <see cref="LogEventProperty"/> values into simple scalars,
    /// dictionaries and lists so that they can be persisted in Azure Table Storage.
    /// </summary>
    public static class AzurePropertyFormatter
    {
        /// <summary>
        /// Simplify the object so as to make handling the serialized
        /// representation easier.
        /// </summary>
        /// <param name="value">The value to simplify (possibly null).</param>
        /// <param name="format">A format string applied to the value, or null.</param>
        /// <param name="formatProvider">A format provider to apply to the value, or null to use the default.</param>
        /// <returns>An Azure Storage entity EntityProperty</returns>
        public static object ToEntityProperty(LogEventPropertyValue value, string format = null, IFormatProvider formatProvider = null)
        {
            if (value is ScalarValue scalar)
            {
                return SimplifyScalar(scalar.Value);
            }

            if (value is DictionaryValue dict)
            {
                return dict.ToString(format, formatProvider);
            }

            if (value is SequenceValue seq)
            {
                return seq.ToString(format, formatProvider);
            }

            if (value is StructureValue str)
            {
                return str.ToString(format, formatProvider);
            }

            return null;
        }

        private static object SimplifyScalar(object value)
        {
            if (value == null) return null;

            var valueType = value.GetType();

            if (valueType == typeof(byte[])) return (byte[])value;
            if (valueType == typeof(bool)) return (bool)value;
            if (valueType == typeof(DateTimeOffset)) return (DateTimeOffset)value;
            if (valueType == typeof(DateTime)) return (DateTime)value;
            if (valueType == typeof(double)) return (double)value;
            if (valueType == typeof(Guid)) return (Guid)value;
            if (valueType == typeof(int)) return (int)value;
            if (valueType == typeof(long)) return (long)value;
            if (valueType == typeof(string)) return (string)value;

            return value.ToString();
        }
    }
}
