using System;
using System.IO;
using System.Reflection;
using ClientService.Models.Enumerators;
using log4net;

namespace ClientService.Helpers;

public class CardToCloudHelper
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void SendToCloud(string text, RowSeparatosEnum rowSeparator, int delay)
	{
		try
		{
			string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\\\ClientServiceDesktop\\\\ClientServiceDesktop.exe"));
			string args = "placeholder " + text.Replace(" ", "") + " " + rowSeparator.ToString() + " " + delay;
			ProcHelper.StartProcessAsCurrentUser(path, args);
		}
		catch (Exception ex)
		{
			logger.Error("[DEBUG] Scan Failed!");
			logger.Error("Error: " + ex.Message);
		}
	}

	~CardToCloudHelper()
	{
	}
}
