namespace ClientService.Classes.Devices.IDBox;

public enum CommunicationMode
{
	NONE = -1,
	USB_CDC = 0,
	UART_9600 = 1,
	UART_115200 = 3,
	USB_HID_CDC = 16
}
