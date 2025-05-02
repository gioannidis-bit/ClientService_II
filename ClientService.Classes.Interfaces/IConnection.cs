namespace ClientService.Classes.Interfaces;

public interface IConnection
{
	int Connect();

	string Send(string data);

	string Receive(byte[] data);

	int Disconnect();

	void Dispose();
}
