using System;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;

namespace ClientService.Models;

public class Scanner : IScanner
{
	public Scanner(ConfigurationModel config)
	{
	}

	public int Connect()
	{
		throw new NotImplementedException();
	}

	public int Disconnect()
	{
		throw new NotImplementedException();
	}

    public bool IsConnected()
    {
        throw new NotImplementedException();
    }

    public string Send(string data)
	{
		throw new NotImplementedException();
	}
}
