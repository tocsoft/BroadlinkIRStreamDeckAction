using Broadlink.NET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Tocsoft.StreamDeck;
using Tocsoft.StreamDeck.Events;

namespace Tocsoft.BroadlinkIRStreamDeckAction
{
    public class TriggerHandler
    {
        private readonly IActionManager<MyActionSettings> manager;
        private readonly DeviceManager deviceManager;
        private DeviceInfo? currentDevice = null;
        private byte[] payload;

        public TriggerHandler(DeviceManager deviceManager, IActionManager<MyActionSettings> manager)
        {
            this.manager = manager;
            this.deviceManager = deviceManager;
            deviceManager.DevicesFound += DeviceManager_DevicesFound;
        }

        private void DeviceManager_DevicesFound(object sender, IEnumerable<DeviceInfo> e)
        {
            var deviceList = e;
            if (currentDevice != null)
            {
                deviceList = e.Union(new[] { currentDevice.Value }).Distinct();
            }

            manager.SendToPropertyInspector(new
            {
                Event = "devicesFound",
                Devices = deviceList
            });
        }


        public async Task DiscoverDevices()
        {
            await deviceManager.StartDevicesDiscovery();
            DeviceManager_DevicesFound(this, deviceManager.DiscoveredDevices);
        }

        public async Task OnSendToPluginAsync(SendEvent evnt)
        {
            if (evnt.Action == "load")
            {
                await DiscoverDevices();
            }
            if (evnt.Action == "learn")
            {
                await LearnCode();
            }
        }

        private async Task LearnCode()
        {
            if (currentDevice.HasValue)
            {
                var payload = await deviceManager.LearnCodeAsync(currentDevice.Value);
                if (payload != null)
                {
                    await manager.SendToPropertyInspector(new
                    {
                        Event = "commandLearned",
                        Command = Convert.ToBase64String(payload),
                    });
                }
            }
        }

        public void OnWillAppear(WillAppearEvent willAppearEvent)
        {
            try
            {
                OnSettingsChanged(willAppearEvent.Payload.Settings.ToObject<MyActionSettings>());
            }
            catch
            {

            }
        }

        public async Task OnPropertyInspectorDidAppear(PropertyInspectorDidAppearEvent evnt)
        {
            await DiscoverDevices();
        }

        public async Task OnSettingsChanged(MyActionSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.Payload))
            {
                this.payload = Convert.FromBase64String(settings.Payload);
            }
            else
            {
                this.payload = null;
            }
            currentDevice = settings.Device;
        }

        public async Task OnKeyDownAsync()
        {
            if (currentDevice.HasValue)
            {
                await deviceManager.SendCodeAsync(currentDevice.Value, this.payload);
            }
        }

        public class MyActionSettings
        {
            public DeviceInfo? Device { get; set; }
            public string Payload { get; set; }
        }
        public class SendEvent
        {
            public string Action { get; set; }
        }
    }
}
