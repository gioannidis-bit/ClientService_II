using ClientService.Classes.Interfaces;
using ClientService.Models.Base;

namespace ClientService.Classes.Factories;

public abstract class ScannerFactory
{
	public abstract IScanner GetScanner(ConfigurationModel config);
}
