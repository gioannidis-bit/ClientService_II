using System;
using ClientService.Classes.Connections.Http;
using ClientService.Classes.Connections.Socket;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;
using ClientService.Models.Enumerators;

namespace ClientService.Classes.Factories;

public class ConcreteConnectionFactory : ConnectionFactory
{
	public override IConnection GetConnection(ConfigurationModel config)
	{
		return config.ServerConnectionType switch
		{
			ServerConnectionTypeEnum.Socket => new SocketConnection(config), 
			ServerConnectionTypeEnum.Http => new HttpConnection(config), 
			_ => throw new ApplicationException($"Scanner '{config.ScannerType}' cannot be created"), 
		};
	}
}
