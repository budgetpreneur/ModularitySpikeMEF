using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
	public class LoggingMethod
	{
		public static void WriteToLog(IPubnubLog pubnubLog, string logText, PNLogVerbosity logVerbosity)
		{
			if (pubnubLog != null && logVerbosity == PNLogVerbosity.BODY)
			{
				try
				{
					pubnubLog.WriteToLog(logText);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.ToString());
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine(logText);
			}
		}
	}
}
