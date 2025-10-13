using Assets.Scripts;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using ReVolt.patches;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace ReVolt
{
    public class RevoltTick : PowerTick
    {
        public bool IsDirty = true;

        public SortedList<float, List<Cable>> AllCables = new();
        public SortedList<float, List<CableFuse>> AllFuses = new();

        private static readonly PropertyInfo ProviderSetter;
        private static readonly PropertyInfo IODevSetter;

        float[] UsedPower = new float[1];
        float[] ProvidedPower = new float[1];

        private System.Random RNG; // Cannot use the Unity RNG because it throws an error if used in a thread

        public float PowerRatio;
        public bool IsPowerMet;

        static RevoltTick()
        {
            ProviderSetter = typeof(PowerTick).GetProperty(nameof(Providers));
            IODevSetter = typeof(PowerTick).GetProperty(nameof(InputOutputDevices));
        }

        public RevoltTick()
        {
            RNG = new System.Random();
        }

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
            IsDirty = false;

            RNG = new System.Random((int)from.ReferenceId);

            //ConsoleWindow.PrintAction($"Cable network {from.ReferenceId} is dirty, reinitializing lists");

            // We're dirty.  Reinitialize data
            Devices.Clear();
            Fuses.Clear();
            AllCables.Clear();

            // For now, use the original lists for devices and fuses
            lock (CableNetwork.PowerDeviceList)
                Devices.AddRange(CableNetwork.PowerDeviceList);

            // Add all fuses in.  Since we want to preferentially burn weaker fuses, we'll use a sorted list
            lock (CableNetwork.FuseList)
                foreach (var fuse in CableNetwork.FuseList.Where(x => x != null))
                    if (!AllFuses.ContainsKey(fuse.PowerBreak))
                        AllFuses.Add(fuse.PowerBreak, new List<CableFuse>(CableNetwork.FuseList.Count) { fuse });
                    else
                        AllFuses[fuse.PowerBreak].Add(fuse);

            // Add all cables in.  Since we want to preferentially burn weaker cables, we'll use a sorted list
            lock (CableNetwork.CableList)
                foreach (var cable in CableNetwork.CableList.Where(x => x != null))
                    if (!AllCables.ContainsKey(cable.MaxVoltage))
                        AllCables.Add(cable.MaxVoltage, new List<Cable>(CableNetwork.CableList.Count) { cable });
                    else
                        AllCables[cable.MaxVoltage].Add(cable);

            // Allocate our memoization arrays, if they've changed
            if (UsedPower.Length != Devices.Count)
            {
                UsedPower = new float[Devices.Count];
                ProvidedPower = new float[Devices.Count];
            }
        }

        public Cable TestBurnCable(float powerUsed)
        {
            // If we're within the power rating of the cable, no burn
            if (powerUsed <= AllCables.Keys[0])
                return null;

            // Do a cheap calculation to get the % chance to burn the cable
            var burnChance = (powerUsed / AllCables.Keys[0]) - 1.0f;

            // If we fail the chance, no burn
            if ((float)RNG.NextDouble() > burnChance * ReVolt.configCableBurnFactor.Value)
                return null;

            // Otherwise we'll burn one of the weak points
            return AllCables.Values[0].Pick();
        }

        public CableFuse TestBlowFuse(float powerUsed)
        {
            // If we're within the power rating of the weakest fuse (or if there are no fuses), no burn 
            if (AllFuses.Keys.Count == 0 || powerUsed <= AllFuses.Keys[0])
                return null;

            // Otherwise we *do* burn a fuse, no delay
            return AllFuses.Values[0].Pick();
        }

        public void CalculateState_New()
        {
            List<PowerProvider> newProviders = new();
            List<PowerProvider> newIODevs = new();

            int idx = Devices.Count;
            while (idx-- > 0)
            {
                var currentDevice = Devices[idx];
                if (currentDevice == null) // Sanity check for missing devices
                    continue;
                
                // Get how much power the device consumes and provides on this network
                UsedPower[idx] = currentDevice.GetUsedPower(CableNetwork);
                ProvidedPower[idx] = currentDevice.GetGeneratedPower(CableNetwork);

                // Sum network power requirement
                Required += UsedPower[idx];

                // If this is a power provider, we need to do some clerical work to keep the Network Analyzer cartridge happy
                if (ProvidedPower[idx] != 0)
                {
                    Potential += ProvidedPower[idx];
                    var Provider = new PowerProvider(currentDevice, CableNetwork);
                    newProviders.Add(Provider);

                    if (currentDevice.IsPowerInputOutput)
                        newIODevs.Add(Provider);
                }
            }

            // Write some data for tablets/etc to use
            ProviderSetter.SetValue(this, newProviders.ToArray());
            IODevSetter.SetValue(this, newIODevs.ToArray());
        }

        public void ApplyState_New()
        {
            // Some some basic bookkeeping and prep state data for below
            PowerTickPatches.CacheState(this);
            PowerRatio = Required == 0.0f ? 0.0f : Mathf.Clamp(Potential / Required, 0.0f, 1.0f);
            IsPowerMet = Potential >= Required;

            var demandRatio = Potential == 0.0f ? 0.0f : Mathf.Clamp(Required / Potential, 0.0f, 1.0f);
            var powerFlow = Mathf.Min(Required, Potential);

            // Check if we're going to be pulling enough power to pop fuses/cables, and use that later.
            var burnCable = TestBurnCable(powerFlow);
            var burnFuse = TestBlowFuse(powerFlow);

            // Now give everything power
            int idx = Devices.Count;
            while (idx-- > 0)
            {
                var currentDevice = Devices[idx];
                if (currentDevice == null)
                    continue;

                // If this device is a power consumer then give power to it based on power ratio + turn it on/off if it has power
                if (UsedPower[idx] >= 0.0f)
                {
                    float powerAvailable = UsedPower[idx] * PowerRatio; // Check and see how much of its demand was met

                    // We know how much power this device is going to receive, so give it that much power (even if it can't make use of it effectively)
                    currentDevice.ReceivePower(CableNetwork, powerAvailable);

                    // Depending on the power available, maybe or maybe don't power the device
                    // TODO later on implement brownouts here
                    if (powerAvailable > 0.0f && (IsPowerMet || (currentDevice.IsPowerProvider && ProvidedPower[idx] > 0.0f)))
                    {
                        if (!currentDevice.Powered)
                            currentDevice.SetPowerFromThread(CableNetwork, true).Forget();
                    }
                    else if (currentDevice.Powered && currentDevice.AllowSetPower(CableNetwork))
                        currentDevice.SetPowerFromThread(CableNetwork, false).Forget();
                }

                // If this was a power provider, draw power from it based on demand ratio
                if (ProvidedPower[idx] >= 0.0f)
                    currentDevice.UsePower(CableNetwork, demandRatio * ProvidedPower[idx]);
            }

            // Now that power has been passed, we need to blow any fusible elements.  Breakers handle their own tripping in their PowerTick() function.
            if (burnFuse != null)
                burnFuse.Break();
            else if (burnCable != null) // Only burn a cable if the fuse wasn't there to protect it
                burnCable.Break();
        }
    }
}
