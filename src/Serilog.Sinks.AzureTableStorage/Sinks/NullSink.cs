using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage.Sinks
{
	/// <summary>
	/// An instance of this sink may be substituted when an Azure Table Storage instance
	/// is unable to be constructed.
	/// </summary>
	class NullSink : ILogEventSink
	{
		public void Emit(LogEvent logEvent)
		{
		}
	}
}
