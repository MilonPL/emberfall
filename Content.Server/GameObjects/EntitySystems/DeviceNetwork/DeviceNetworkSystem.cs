﻿using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public delegate void OnReceiveNetMessage(int frequency, string sender, IReadOnlyDictionary<string, string> payload, bool broadcast);

    public class DeviceNetworkSystem : EntitySystem, IDeviceNetwork
    {
        private const int PACKAGES_PER_TICK = 30;

        private readonly IRobustRandom _random = IoCManager.Resolve<IRobustRandom>();
        private readonly Dictionary<int, List<NetworkDevice>> _devices = new Dictionary<int, List<NetworkDevice>>();
        private readonly Queue<NetworkPackage> _packages = new Queue<NetworkPackage>();

        /// <inheritdoc/>
        public DeviceNetworkConnection Register(int netId, int frequency, OnReceiveNetMessage messageHandler, bool receiveAll = false)
        {
            var address = GenerateValidAddress(netId, frequency);

            var device = new NetworkDevice
            {
                Address = address,
                Frequency = frequency,
                ReceiveAll = receiveAll,
                ReceiveNetMessage = messageHandler
            };

            AddDevice(netId, device);

            return new DeviceNetworkConnection(this, netId, address, frequency);
        }

        public DeviceNetworkConnection Register(int netId, OnReceiveNetMessage messageHandler, bool receiveAll = false)
        {
            return Register(netId, 0, messageHandler, receiveAll);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var i = PACKAGES_PER_TICK;
            while (_packages.Count > 0 && i > 0)
            {
                i--;

                var package = _packages.Dequeue();

                if (package.Broadcast)
                {
                    BroadcastPackage(package);
                    continue;
                }

                SendPackage(package);
            }
        }

        public bool EnqueuePackage(int netId, int frequency, string address, IReadOnlyDictionary<string, string> data, bool broadcast = false)
        {
            if (!_devices.ContainsKey(netId))
                return false;

            var package = new NetworkPackage()
            {
                NetId = netId,
                Frequency = frequency,
                Address = address,
                Broadcast = broadcast,
                Data = data
            };

            _packages.Enqueue(package);
            return true;
        }

        public void RemoveDevice(int netId, int frequency, string address)
        {
            var device = DeviceWithAddress(netId, frequency, address);
            _devices[netId].Remove(device);
        }

        public void SetDeviceReceiveAll(int netId, int frequency, string address, bool receiveAll)
        {
            var device = DeviceWithAddress(netId, frequency, address);
            device.ReceiveAll = receiveAll;
        }

        public bool GetDeviceReceiveAll(int netId, int frequency, string address)
        {
            var device = DeviceWithAddress(netId, frequency, address);
            return device.ReceiveAll;
        }

        private string GenerateValidAddress(int netId, int frequency)
        {
            var unique = false;
            var devices = DevicesForFrequency(netId, frequency);
            var address = "";

            while (!unique)
            {
                address = _random.Next().ToString("x");
                unique = !devices.Exists(device => device.Address == address);
            }

            return address;
        }

        private void AddDevice(int netId, NetworkDevice networkDevice)
        {
            if(!_devices.ContainsKey(netId))
                _devices[netId] = new List<NetworkDevice>();

            _devices[netId].Add(networkDevice);
        }

        private List<NetworkDevice> DevicesForFrequency(int netId, int frequency)
        {
            if (!_devices.ContainsKey(netId))
                return new List<NetworkDevice>();

            var result = _devices[netId].FindAll(device => device.Frequency == frequency);

            return result;
        }

        private NetworkDevice DeviceWithAddress(int netId, int frequency, string address)
        {
            var devices = DevicesForFrequency(netId, frequency);

            var device = devices.Find(device => device.Address == address);

            return device;
        }

        private List<NetworkDevice> DevicesWithReceiveAll(int netId, int frequency)
        {
            if (!_devices.ContainsKey(netId))
                return new List<NetworkDevice>();

            var result = _devices[netId].FindAll(device => device.Frequency == frequency && device.ReceiveAll);

            return result;
        }

        private void BroadcastPackage(NetworkPackage package)
        {
            var devices = DevicesForFrequency(package.NetId, package.Frequency);
            SendToDevices(devices, package);
        }

        private void SendPackage(NetworkPackage package)
        {
            var devices = DevicesWithReceiveAll(package.NetId, package.Frequency);
            var device = DeviceWithAddress(package.NetId, package.Frequency, package.Address);

            devices.Add(device);

            SendToDevices(devices, package);
        }

        private void SendToDevices(List<NetworkDevice> devices, NetworkPackage package)
        {
            for (int index = 0; index < devices.Count; index++)
            {
                var device = devices[index];
                device.ReceiveNetMessage(package.Frequency, device.Address, package.Data, false);
            }
        }

        internal class NetworkDevice
        {
            public int Frequency;
            public string Address;
            public OnReceiveNetMessage ReceiveNetMessage;
            public bool ReceiveAll;
        }

        internal class NetworkPackage
        {
            public int NetId;
            public int Frequency;
            public string Address;
            public bool Broadcast;
            public IReadOnlyDictionary<string, string> Data; 
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum BaseNetworks
    {
        PRIVATE,
        WIRED,
        WIRELESS
    }
}
