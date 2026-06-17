using Colossal.AssetPipeline.Importers;
using Game.Input;
using Game.Modding;
using Game.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools
{
    internal class Settings : ModSetting
    {

        public const string kMainSection = "Main";
        public const string kTTTSection = "TransformTool";

        public const string kQOLGroup = "QOL";
        public const string kKeybindingGroup = "KeyBinding";

        public Settings(IMod mod) : base(mod)
        {
        }

        [SettingsUIKeyboardBinding(BindingKeyboard.M, nameof(ToggleShowMarker), ctrl: true)]
        [SettingsUISection(kMainSection, kQOLGroup)]
        public ProxyBinding ToggleShowMarker { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.W, nameof(EnterMoveBinding))]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding EnterMoveBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.X,actionName: nameof(EnterRotateBinding))]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding EnterRotateBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Z, nameof(UndoBinding), ctrl: true)]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding UndoBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Y, nameof(RedoBinding), ctrl: true)]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding RedoBinding { get; set; }

        public override void SetDefaults()
        {
            //throw new NotImplementedException();
        }
    }
}
