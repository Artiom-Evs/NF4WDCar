using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace NF4WDCar.MCU
{
    public static class Program
    {
        private const string DEVICE_NAME = "NF-4WD";
        private const int BUILTIN_LED = 5;

        private static GattLocalCharacteristic _ledCharacteristic;
        private static GpioController _gpioController;
        private static GpioPin _ledPin;

        public static void Main()
        {
            Debug.WriteLine(">> Start main.");

            _gpioController = new GpioController();
            _ledPin = _gpioController.OpenPin(BUILTIN_LED, PinMode.Output);
            _ledPin.Write(PinValue.High);

            BluetoothLEServer server = BluetoothLEServer.Instance;

            server.DeviceName = DEVICE_NAME;

            Guid serviceUuid = new("A7EEDF2C-DA87-4CB5-A9C5-5151C78B0057");
            Guid ledCharUuid = new("A7EEDF2C-DA8A-4CB5-A9C5-5151C78B0057");

            GattServiceProviderResult result = GattServiceProvider.Create(serviceUuid);

            if (result.Error != BluetoothError.Success)
            {
                Debug.WriteLine(">> GATT service provider creation failed.");
                return;
            }

            GattServiceProvider serviceProvider = result.ServiceProvider;

            // get automatically created primary service.
            GattLocalService primaryService = serviceProvider.Service;

            #region Create led control characteristic

            GattLocalCharacteristicResult characteristicResult = primaryService.CreateCharacteristic(
                 ledCharUuid,
                 new GattLocalCharacteristicParameters()
                 {
                     CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Write
                 });

            if (characteristicResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine(">> Led control characteristic creation failed.");
                return;
            }

            _ledCharacteristic = characteristicResult.Characteristic;
            _ledCharacteristic.ReadRequested += LedCharacteristic_ReadRequested;
            _ledCharacteristic.WriteRequested += LedCharacteristic_WriteRequested;

            #endregion
            #region Start advertising

            serviceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters()
            {
                IsConnectable = true,
                IsDiscoverable = true,
            });

            #endregion

            Debug.WriteLine(">> End main.");
            Thread.Sleep(Timeout.Infinite);
        }

        private static void LedCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs e)
        {
            DataWriter dw = new();

            // on my LILYGO board builtin LED works with inverted logic
            if (_ledPin.Read() == PinValue.Low)
                dw.WriteString("on");
            else
                dw.WriteString("off");

            e.GetRequest().RespondWithValue(dw.DetachBuffer());
        }

        private static void LedCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            GattWriteRequest request = e.GetRequest();

            DataReader dr = DataReader.FromBuffer(request.Value);
            string state = dr.ReadString(dr.UnconsumedBufferLength);

            // on my LILYGO board builtin LED works with inverted logic
            if (state == "on")
                _ledPin.Write(PinValue.Low);
            else if (state == "off")
                _ledPin.Write(PinValue.High);
            else
                Debug.WriteLine(@$">> Unsupported command for LED control: ""{state}"".");

            request.Respond();
        }
    }
}
