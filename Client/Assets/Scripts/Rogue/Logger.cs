using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Logger
{

	public enum LogLevel
	{
		Trace,
		Info,
		Warn,
		Error,
		Fatal
	}

	static void write(string level, string format, params object[] obj )
	{
		Debug.Log (string.Format (format, obj.Select (o => o.ToString ()).ToArray ()));
	}

	public static void Info (string format, params object[] obj)
	{
		write ("Info", format, obj);
	}

	public static void Warn(string format, params object[] obj)
	{
		write ("Warn", format, obj);
	}

	public static void Error(string format, params object[] obj)
	{
		write ("Error", format, obj);
	}
}
