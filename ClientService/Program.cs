using System;
using System.Reflection;
using log4net;

namespace ClientService;

internal class Program
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	[STAThread]
	private static void Main(string[] args)
	{
		try
		{
			new MainCode().Main();
		}
		catch (Exception ex)
		{
			logger.Error(ex.ToString());
		}
	}
}
