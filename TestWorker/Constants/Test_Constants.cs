﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWorker.Constants
{
	public static class Test_Constants
	{
		//public const int WorkerSleepDefault = 5;
		//public static int ThreadCountDefault = Environment.ProcessorCount;
		//public const bool IsSingleThreadDefault = false;
		public const bool StartAtCreationDefault = true;
		//public const int RetryCountTreshold = 6;
		//public const int KeepSessionAliveInMinutes = 30;

		public const string DataTimezone = "CET";
		public const string ProviderName = "test";

		public const string RebusConnString = "Endpoint=sb://ark-playground.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fc3hUuRJJmx/IpQ+89QyYP8VVA6IkwQcToSEt/51+rU=";

	}
}
