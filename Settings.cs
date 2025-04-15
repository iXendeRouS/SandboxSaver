using BTD_Mod_Helper.Api.Data;
using BTD_Mod_Helper.Api.ModOptions;

namespace SandboxSaver
{
    public class Settings : ModSettings
    {
        public static readonly ModSettingBool ModEnabled = new(true);

        public static readonly ModSettingButton OpenSavesFolder = new(SandboxSaver.OpenSaveFolder)
        {
            buttonText = "Open"
        };

        public static readonly ModSettingHotkey LoadSaveHotkey = new(UnityEngine.KeyCode.L)
        {
            description = "Show the load sandbox save prompt"
        };

        public static readonly ModSettingHotkey MakeSaveHotkey = new(UnityEngine.KeyCode.M)
        {
            description = "Make a save of the current sandbox instance"
        };
    }
}
