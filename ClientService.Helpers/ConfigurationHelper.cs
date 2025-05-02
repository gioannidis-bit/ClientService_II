using System.IO;
using System.Reflection;
using ClientService.Models.Base;
using log4net;
using Newtonsoft.Json;

namespace ClientService.Helpers;

public class ConfigurationHelper
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public ConfigurationModel GetConfiguration()
	{
		ConfigurationModel configuration = null;
		string sPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (sPath[sPath.Length - 1].ToString() != "\\")
		{
			sPath += "\\";
		}
		using StreamReader reader = new StreamReader(sPath + "configuration.json");
		return JsonConvert.DeserializeObject<ConfigurationModel>(reader.ReadToEnd());
	}
}
