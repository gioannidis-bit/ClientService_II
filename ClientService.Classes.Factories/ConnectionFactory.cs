using ClientService.Classes.Interfaces;
using ClientService.Models.Base;

namespace ClientService.Classes.Factories;

public abstract class ConnectionFactory
{
	public abstract IConnection GetConnection(ConfigurationModel config);
}
