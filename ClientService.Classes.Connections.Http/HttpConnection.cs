using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using ClientService.Classes.Interfaces;
using ClientService.Models;
using ClientService.Models.Base;
using ClientService.Models.Enumerators;
using log4net;
using Newtonsoft.Json;

namespace ClientService.Classes.Connections.Http;

public class HttpConnection : IConnection
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private ServerConnectionTypeEnum ServerConnectionType { get; set; }

	private string ServerUrl { get; set; }

	private int ServerPort { get; set; }

	private string ServerCredentials { get; set; }

	private int StationId { get; set; }

	public HttpConnection(ConfigurationModel config)
	{
		ServerConnectionType = config.ServerConnectionType;
		ServerUrl = config.ServerUrl;
		ServerPort = config.ServerPort;
		ServerCredentials = config.ServerCredentials;
		StationId = config.StationId;
	}

	public int Connect()
	{
		try
		{
			return 0;
		}
		catch
		{
			return -1;
		}
	}

	public int Disconnect()
	{
		return 0;
	}

	public string Receive(byte[] data)
	{
		return "OK";
	}

	public string Send(string data)
	{
		try
		{
			Post(data);
		}
		catch
		{
			return "ERROR";
		}
		return "OK";
	}

	public void Post(string data)
	{
		if (string.IsNullOrWhiteSpace(ServerUrl) || !ServerUrl.Contains("http"))
		{
			return;
		}
		try
		{
			if (!string.IsNullOrWhiteSpace(data))
			{
				StringContent content = new StringContent(JsonConvert.SerializeObject(new DataFromPcModel
				{
					stationId = StationId,
					scannerData = data
				}), Encoding.UTF8, "application/json");
				using HttpClient client = new HttpClient();
				client.BaseAddress = new Uri(ServerUrl);
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
				AuthenticationHeaderValue authorizationHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(new ASCIIEncoding().GetBytes(ServerCredentials)));
				client.DefaultRequestHeaders.Authorization = authorizationHeader;
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				string action = "api/PassportScanner/SendDataFromLocalPC";
				_ = client.PostAsync(action, content).Result.Content.ReadAsStringAsync().Result;
				return;
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message, ex);
			throw;
		}
	}

	public void Dispose()
	{
	}
}
