using Game.Input;
using Game.Modding;
using Game.Settings;

namespace ExtraDetailingTools
{
    [SettingsUIGroupOrder(kQOLGroup, kKeybindingGroup, kQuickActionsGroup)]
    [SettingsUIShowGroupName(kQOLGroup, kKeybindingGroup, kQuickActionsGroup)]
    [SettingsUIKeyboardAction(nameof(OpenTransformTool), Usages.kDefaultUsage, Usages.kEditorUsage, Usages.kToolUsage)]
    [SettingsUIKeyboardAction(nameof(ToggleShowMarker), Usages.kDefaultUsage, Usages.kEditorUsage)]
    [SettingsUIKeyboardAction(nameof(EnterMoveBinding), "EDT.InTransformTool", Usages.kDefaultUsage, Usages.kToolUsage)]
    [SettingsUIKeyboardAction(nameof(EnterRotateBinding), "EDT.InTransformTool", Usages.kDefaultUsage, Usages.kToolUsage)]
    [SettingsUIKeyboardAction(nameof(UndoBinding), "EDT.InTransformTool", Usages.kDefaultUsage, Usages.kToolUsage)]
    [SettingsUIKeyboardAction(nameof(RedoBinding), "EDT.InTransformTool", Usages.kDefaultUsage, Usages.kToolUsage)]
    internal class Settings : ModSetting
    {   

        public const string kMainSection = "Main";
        public const string kTTTSection = "TransformTool";

        public const string kQOLGroup = "QOL";
        public const string kKeybindingGroup = "KeyBinding";
        public const string kQuickActionsGroup = "QuickActions";

        public Settings(IMod mod) : base(mod)
        {
        }

        [SettingsUIKeyboardBinding(BindingKeyboard.M, nameof(ToggleShowMarker), ctrl: true)]
        [SettingsUISection(kMainSection, kQOLGroup)]
        public ProxyBinding ToggleShowMarker { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.L, nameof(OpenTransformTool))]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding OpenTransformTool { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.K, nameof(EnterMoveBinding))]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding EnterMoveBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.J,actionName: nameof(EnterRotateBinding))]
        [SettingsUISection(kTTTSection, kKeybindingGroup)]
        public ProxyBinding EnterRotateBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Z, nameof(UndoBinding), ctrl: true)]
        [SettingsUISection(kTTTSection, kKeybindingGroup, kQuickActionsGroup)]
        public ProxyBinding UndoBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Y, nameof(RedoBinding), ctrl: true)]
        [SettingsUISection(kTTTSection, kKeybindingGroup, kQuickActionsGroup)]
        public ProxyBinding RedoBinding { get; set; }

        [SettingsUISection(kTTTSection, kQOLGroup)]
        [SettingsUISlider(min = 50f, max = 500f, step = 10f, unit = "integer")]
        public int HandleScreenSize { get; set; } = 150;

        public override void SetDefaults()
        {
            HandleScreenSize = 150;
        }
    }
}
