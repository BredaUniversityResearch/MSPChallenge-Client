using System;
using System.Collections.Generic;
using System.Linq;
namespace MSP2050.Scripts
{
	public class CommandLineArgumentsManager
	{
		private static CommandLineArgumentsManager instance;
		private Dictionary<string, string> m_CommandLineArguments = new Dictionary<string, string>();
		
		public enum CommandLineArgumentName
		{
			Team,
			User,
			Password,
			ServerAddress,
			ConfigFileName,
			SessionEntryIndex,
			AutoLogin
		}

		private CommandLineArgumentsManager()
		{
#if UNITY_EDITOR
			return;
#endif
			var allowedNames = Enum.GetNames(typeof(CommandLineArgumentName));
			var args = Environment.GetCommandLineArgs();
			for (var n = 1; n < args.Length; n++)
			{
				var arg = args[n];
				var parts = arg.Split('=');
				if (parts.Length != 2) continue;
				if (!allowedNames.Contains(parts[0])) continue;
				m_CommandLineArguments[parts[0]] = parts[1];
			}			
		}

		public static CommandLineArgumentsManager GetInstance()
		{
			if (null != instance) return instance;
			instance = new CommandLineArgumentsManager();
			return instance;
		}
		
		public string GetCommandLineArgumentValue(CommandLineArgumentName name)
		{
			return !m_CommandLineArguments.ContainsKey(name.ToString()) ? null : m_CommandLineArguments[name.ToString()];
		}		
		
		public string AutoFill(CommandLineArgumentName commandLineArgumentName, string defaultValue)
		{
			var autoFillValue = GetCommandLineArgumentValue(commandLineArgumentName);
			return autoFillValue ?? defaultValue;
		}		
	}
}
