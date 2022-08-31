using Broadlink.NET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Tocsoft.StreamDeck;
using Tocsoft.StreamDeck.Events;

namespace Tocsoft.BroadlinkIRStreamDeckAction
{
    public class DeviceManager
    {
        private readonly Client client;
        private HashSet<DeviceInfo> discoveredDevices = new HashSet<DeviceInfo>();
        private Dictionary<DeviceInfo, RMDevice> devices = new Dictionary<DeviceInfo, RMDevice>();

        public IEnumerable<DeviceInfo> DiscoveredDevices => discoveredDevices;

        public DeviceManager(Client client)
        {
            this.client = client;
            client.DeviceHandler += Client_DeviceHandler;
        }

        public async Task StartDevicesDiscovery()
        {
            await client.DiscoverAsync();
            if (this.discoveredDevices.Any())
            {
                DevicesFound?.Invoke(this, this.discoveredDevices);
            }
        }

        public event EventHandler<IEnumerable<DeviceInfo>> DevicesFound;

        public async Task<byte[]> LearnCodeAsync(DeviceInfo deviceInfo)
        {
            var device = await ConnectAsync(deviceInfo);
            if (device != null)
            {
                return await device.LearnIRAsync();
            }
            return null;
        }

        public async Task SendCodeAsync(DeviceInfo deviceInfo, byte[] code)
        {
            var device = await ConnectAsync(deviceInfo);
            if (device != null)
            {
                await device.SendRemoteCommandAsync(code);
            }
        }

        public SemaphoreSlim semephore = new SemaphoreSlim(1);
        public async Task<RMDevice> ConnectAsync(DeviceInfo deviceInfo)
        {
            await semephore.WaitAsync();
            try
            {
                RMDevice result;
                if (!devices.TryGetValue(deviceInfo, out result))
                {
                    result = new RMDevice()
                    {
                        DeviceType = deviceInfo.DeviceType,
                        EndPoint = IPEndPoint.Parse(deviceInfo.Address),
                        MacAddress = PhysicalAddress.Parse(deviceInfo.MacAddress).GetAddressBytes()
                    };

                    devices[deviceInfo] = result;
                }

                if (result.IsAuthorized)
                    return result;
                var cts = new CancellationTokenSource(5000);
                if (!await result.AuthorizeAsync(cts.Token))
                {
                    return null;
                }
                return result;
            }
            finally
            {
                semephore.Release();
            }
        }

        private void Client_DeviceHandler(object sender, BroadlinkDevice e)
        {
            var details = new DeviceInfo(e);

            if (this.discoveredDevices.Add(details))
            {
                DevicesFound?.Invoke(this, this.discoveredDevices);
            }
        }
    }

    public struct DeviceInfo
    {
        public string Address;
        public string MacAddress;
        public short DeviceType;

        public DeviceInfo(string address, string macAddress, short deviceType)
        {
            Address = address;
            MacAddress = macAddress;
            DeviceType = deviceType;
        }

        public DeviceInfo(BroadlinkDevice device)
        {
            Address = device.EndPoint.ToString();
            MacAddress = new PhysicalAddress(device.MacAddress).ToString();
            DeviceType = device.DeviceType;
        }

        public override bool Equals(object obj)
        {
            return obj is DeviceInfo other &&
                   Address == other.Address &&
                   MacAddress == other.MacAddress &&
                   DeviceType == other.DeviceType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, MacAddress, DeviceType);
        }

        public void Deconstruct(out string address, out string macAddress, out short deviceType)
        {
            address = Address;
            macAddress = MacAddress;
            deviceType = DeviceType;
        }

        public static implicit operator (string Address, string MacAddress, short DeviceType)(DeviceInfo value)
        {
            return (value.Address, value.MacAddress, value.DeviceType);
        }

        public static implicit operator DeviceInfo((string Address, string MacAddress, short DeviceType) value)
        {
            return new DeviceInfo(value.Address, value.MacAddress, value.DeviceType);
        }
    }
}
