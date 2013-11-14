using System;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace  NSF.Share
{
	/// <summary>
	/// 调试日志输入对象。
	/// </summary>
	public static class Log
	{
		private static ILog Instance = LogManager.GetLogger("loggerDebug");

		public static void Error(String oString)
		{
			Instance.Error(oString);
		}

		public static void Error(String fmtString, params object[] argv)
		{
			Instance.ErrorFormat(fmtString, argv);
		}

		public static void Error(Exception e)
		{
			Instance.Error(e);
		}

		public static void Warn(String oString)
		{
			Instance.Warn(oString);
		}

		public static void Warn(String fmtString, params object[] argv)
		{
			Instance.WarnFormat(fmtString, argv);
		}

		public static void Info(String oString)
		{
			Instance.Info(oString);
		}

		public static void Info(String fmtString, params object[] argv)
		{
			Instance.InfoFormat(fmtString, argv);
		}

		public static void Debug(String oString)
		{
			Instance.Debug(oString);
		}

		public static void Debug(String fmtString, params object[] argv)
		{
			Instance.DebugFormat(fmtString, argv);
		}
	}
}

