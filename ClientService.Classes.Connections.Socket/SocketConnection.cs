using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;
using ClientService.Models.Enumerators;
using log4net;

namespace ClientService.Classes.Connections.Socket;

public class SocketConnection : IConnection, IDisposable
{
	private delegate void SetTextCallback(string text);

	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private System.Net.Sockets.Socket _clientSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

	private ServerConnectionTypeEnum ServerConnectionType { get; set; }

	private string ServerUrl { get; set; }

	private int ServerPort { get; set; }

	private string ServerCredentials { get; set; }

	public SocketConnection(ConfigurationModel config)
	{
		ServerConnectionType = config.ServerConnectionType;
		ServerUrl = config.ServerUrl;
		ServerPort = config.ServerPort;
		ServerCredentials = config.ServerCredentials;
	}

	public int Connect()
	{
		try
		{
			LoopConnect();
			return 0;
		}
		catch
		{
			return -1;
		}
	}

	private void LoopConnect()
	{
		while (!_clientSocket.Connected)
		{
			try
			{
				_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Debug, optionValue: true);
				_clientSocket.Connect(ServerUrl, ServerPort);
			}
			catch
			{
				throw;
			}
		}
		logger.Info("Socket Connection Initiated");
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
			if (!_clientSocket.Connected)
			{
				LoopConnect();
			}
			byte[] buffer = Encoding.ASCII.GetBytes(data);
			if (_clientSocket.Connected)
			{
				_clientSocket.Send(buffer);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
		return "OK";
	}

	public void Dispose()
	{
		Disconnect();
		_clientSocket = null;
	}

	~SocketConnection()
	{
	}
}
