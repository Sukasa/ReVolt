using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using ReVolt.Assets.Scripts.patches;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace ReVolt
{
    public class RevoltTick : PowerTick
    {
        public bool IsDirty = true;

        public SortedList<float, List<Cable>> AllCables = new();

        PropertyInfo ProviderSetter;
        PropertyInfo IODevSetter;

        float[] UsedPower = new float[1];
        float[] ProvidedPower = new float[1];

        public float PowerRatio;
        public bool IsPowerMet;

        public void Initialize_New(CableNetwork from)
        {
            if (CableNetwork != from) // If our cable network changes somehow, mark dirty
                IsDirty = true;

            // Basic housekeeping
            CableNetwork = from;
            Potential = 0.0f;
            Required = 0.0f;
            Consumed = 0.0f;

            if (!IsDirty) // Only rebuild if dirty
                return;

            UnityEngine.Debug.LogWarning("RevoltTick re-initializing on dirty network");
            System.Console.WriteLine("RevoltTick re-initializing on dirty network");

            ProviderSetter = GetType().GetProperty(nameof(Providers));
            IODevSetter = GetType().GetProperty(nameof(InputOutputDevices));

            // We're dirty.  Reinitialize data
            Devices.Clear();
            Fuses.Clear();

            // For now, use the original lists for devices and fuses
            lock (CableNetwork.PowerDeviceList)
                Devices.AddRange(CableNetwork.PowerDeviceList);


            lock (CableNetwork.FuseList)
                Fuses.AddRange(CableNetwork.FuseList);

            // Add all cables in.  Since we want to preferentially burn weaker cables, we'll use a sorted list
            AllCables.Clear();
            lock (CableNetwork.CableList)
                foreach (var cable in CableNetwork.CableList)
                    if (!AllCables.ContainsKey(cable.MaxVoltage))
                        AllCables.Add(cable.MaxVoltage, new List<Cable>(CableNetwork.CableList.Count) { cable });
                    else
                        AllCables[cable.MaxVoltage].Add(cable);

        }

        public void TestBurnCable(float powerUsed)
        {

        }

        public void CalculateState_New()
        {
            PowerSupplies.Clear();
            List<PowerProvider> newProviders = new();
            List<PowerProvider> newIODevs = new();

            if (UsedPower.Length != Devices.Count)
            {
                UsedPower = new float[Devices.Count];
                ProvidedPower = new float[Devices.Count];
            }

            int idx = Devices.Count;
            while (idx-- > 0)
            {
                var currentDevice = Devices[idx];
                if (currentDevice == null)
                {
                    UsedPower[idx] = 0f;
                    ProvidedPower[idx] = 0f;

                    continue;
                }

                UsedPower[idx] = currentDevice.GetUsedPower(CableNetwork);
                Required += UsedPower[idx];

                ProvidedPower[idx] = currentDevice.GetGeneratedPower(CableNetwork);

                if (ProvidedPower[idx] != 0)
                {
                    Potential += ProvidedPower[idx];
                    PowerSupplies[currentDevice.ReferenceId] = ProvidedPower[idx];
                    var Provider = new PowerProvider(currentDevice, CableNetwork);
                    newProviders.Add(Provider);

                    if (currentDevice.IsPowerInputOutput)
                        newIODevs.Add(Provider);
                }
            }

            ProviderSetter.SetValue(this, newProviders.ToArray());
            IODevSetter.SetValue(this, null);
        }

        private readonly Dictionary<long, float> PowerSupplies = new();



        public void ApplyState_New()
        {
            PowerTickCacheStatePatch.CacheState(this);
            PowerRatio = Potential == 0.0f ? 0.0f : Mathf.Clamp(Required / Potential, 0.0f, 1.0f);
            IsPowerMet = Potential >= Required;

            // TODO check if a cable should break

            // Then check if a fuse should pop first

            // Pop a fuse, otherwise a cable, otherwise none

            // Now give everything power
            int idx = Devices.Count;
            while (idx-- > 0)
            {
                var currentDevice = Devices[idx];

                if (currentDevice == null)
                    continue;



                if (UsedPower[idx] >= 0.0f)
                {
                    float powerAvailable = UsedPower[idx] * PowerRatio;

                    UniTaskVoid uniTaskVoid;
                    if (powerAvailable > 0.0f && (IsPowerMet || (currentDevice.IsPowerProvider && Potential > 0.0f)))
                    {
                        if (!currentDevice.Powered)
                        {
                            uniTaskVoid = currentDevice.SetPowerFromThread(CableNetwork, true);
                            uniTaskVoid.Forget();
                        }
                    }
                    else if (currentDevice.AllowSetPower(CableNetwork) && currentDevice.Powered)
                    {
                        uniTaskVoid = currentDevice.SetPowerFromThread(CableNetwork, false);
                        uniTaskVoid.Forget();
                    }
                }

                // If this was a power provider, draw power from it
                if (ProvidedPower[idx] >= 0.0f)
                {
                    var powerConsumed = Required * ProvidedPower[idx] / Potential;
                    currentDevice.ReceivePower(CableNetwork, powerConsumed);
                }
            }
        }
    }
}
