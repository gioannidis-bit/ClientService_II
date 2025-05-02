using System;
using ClientService.Classes.Devices.AccessIS;
using ClientService.Classes.Devices.DeskoDev;
using ClientService.Classes.Devices.IDBox;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;
using ClientService.Models.Enumerators;

namespace ClientService.Classes.Factories;

public class ConcreteScannerFactory : ScannerFactory
{
	public override IScanner GetScanner(ConfigurationModel config)
	{
		return config.ScannerType switch
		{
			ScannerTypeEnum.AccessIS => new AccessISScanner(config), 
			ScannerTypeEnum.Desko => new DeskoScanner(config), 
			ScannerTypeEnum.IDBox => new IDBoxScanner(config), 
			_ => throw new ApplicationException($"Scanner '{config.ScannerType}' cannot be created"), 
		};
	}
}
