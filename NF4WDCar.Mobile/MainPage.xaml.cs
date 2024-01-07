using InTheHand.Bluetooth;
using System.Text;

namespace NF4WDCar.Mobile;

public partial class MainPage : ContentPage
{

    private const string PRIMARY_SERVICE_UUID = "A7EEDF2C-DA87-4CB5-A9C5-5151C78B0057";
    private const string LED_CHARACTERISTIC_UUID = "A7EEDF2C-DA8A-4CB5-A9C5-5151C78B0057";
    private GattCharacteristic? _ledCharacteristic;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        if (_ledCharacteristic is not null && _ledCharacteristic.Service.Device.Gatt.IsConnected)
            _ledCharacteristic.Service.Device.Gatt.Disconnect();
    }

    private async void SelectDeviceButton_Clicked(object sender, EventArgs e)
    {
        BluetoothDevice device = await Bluetooth.RequestDeviceAsync();

        if (device is null)
        {
            await DisplayAlert("Warning", "Device is not selected", "OK");
            return;
        }

        await device.Gatt.ConnectAsync();

        GattService primaryService = await device.Gatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(Guid.Parse(PRIMARY_SERVICE_UUID)));

        if (primaryService is null)
        {
            await DisplayAlert("Warning", "Primary service is not found.", "OK");
            device.Gatt.Disconnect();
            return;
        }

        _ledCharacteristic = await primaryService.GetCharacteristicAsync(BluetoothUuid.FromGuid(Guid.Parse(LED_CHARACTERISTIC_UUID)));

        if (_ledCharacteristic is null)
        {
            await DisplayAlert("Warning", "Led characteristic is not found.", "OK");
            device.Gatt.Disconnect();
            return;
        }

        selectDeviceButton.IsVisible = false;
        ledControlButton.IsEnabled = true;
    }

    private async void LedControlButton_Pressed(object sender, EventArgs e)
    {
        if (_ledCharacteristic is null)
            return;

        await _ledCharacteristic.WriteValueWithoutResponseAsync(Encoding.UTF8.GetBytes("on"));
    }

    private async void LedControlButton_Released(object sender, EventArgs e)
    {
        if (_ledCharacteristic is null)
            return;
        
        await _ledCharacteristic.WriteValueWithoutResponseAsync(Encoding.UTF8.GetBytes("off"));
    }
}
