using System.Reflection;
using BepInEx.Configuration;
using LaunchPadBooster.Patching;

namespace ReVolt
{
    public class OptionPatch : HarmonyConditionalPatch
    {
        private readonly string _optionName;
        private readonly bool _condition = true;
        
        public OptionPatch(string optionName)
        {
            _optionName = optionName;
        }
        public OptionPatch(string optionName, bool condition)
        {
            _optionName = optionName;
            _condition = condition;
        }

        public override bool CanPatch => ((ConfigEntry<bool>)typeof(ReVolt).GetField(_optionName, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(ReVolt.Instance)).Value == _condition;
    }
}