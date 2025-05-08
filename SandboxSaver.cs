using System.Diagnostics;
using System.IO;
using System.Text;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Utils;
using Il2CppNewtonsoft.Json;
using Il2CppSystem;
using MelonLoader;
using SandboxSaver;

[assembly: MelonInfo(typeof(SandboxSaver.SandboxSaver), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SandboxSaver;

public class SandboxSaver : BloonsTD6Mod
{
    private const string SaveFolderName = "SandboxSaves";

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
    };

    public override void OnApplicationStart()
    {
        ModHelper.Msg<SandboxSaver>("SandboxSaver loaded!");
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (!Settings.ModEnabled || InGame.instance == null) return;

        if (PopupScreen.instance.IsPopupActive()) return;

        if (Settings.MakeSaveHotkey.JustPressed())
            HandleMakeSaveHotkey();

        if (Settings.LoadSaveHotkey.JustPressed())
            HandleLoadSaveHotkey();
    }

    private void HandleMakeSaveHotkey()
    {
        PopupScreen.instance.SafelyQueue(screen =>
            screen.ShowSetNamePopup(
                "SandboxSaver",
                "Enter save name:",
                (Action<string>)MakeSave,
                ""
            )
        );
    }

    private void MakeSave(string saveName)
    {
        try
        {
            string mapName = InGame.instance.GetMap().mapModel.mapName;
            string fileName = $"{mapName}_{saveName}.json";
            string savePath = Path.Combine(GetSaveFolder(), fileName);

            if (File.Exists(savePath))
            {
                PopupScreen.instance.SafelyQueue(screen =>
                    screen.ShowOkPopup($"A save with the name '{saveName}' for this map already exists. Please choose a different name."));
                return;
            }

            MapSaveDataModel saveModel = InGame.instance.CreateCurrentMapSave(
                InGame.instance.currentRoundId,
                InGame.instance.MapDataSaveId);

            var text = JsonConvert.SerializeObject(saveModel, SerializerSettings);
            File.WriteAllText(savePath, text, Encoding.UTF8);

            PopupScreen.instance.SafelyQueue(screen =>
                screen.ShowOkPopup($"Saved successfully as '{saveName}'!"));

            MelonLogger.Msg($"Saved save to {savePath}");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Error saving sandbox state: {ex.Message}");
            PopupScreen.instance.SafelyQueue(screen =>
                screen.ShowOkPopup("Error making save. See the log for details."));
        }
    }

    private void HandleLoadSaveHotkey()
    {
        PopupScreen.instance.SafelyQueue(screen =>
            screen.ShowSetNamePopup(
                "SandboxSaver",
                "Enter save name to load:",
                (Action<string>)LoadSave,
                ""
            )
        );
    }

    private void LoadSave(string saveName)
    {
        try
        {
            if (InGameData.CurrentGame == null || !InGameData.CurrentGame.IsSandbox)
            {
                //PopupScreen.instance.SafelyQueue(screen =>
                //    screen.ShowOkPopup("Saves can only be loaded while in Sandbox mode."));
                return;
            }

            string mapName = InGame.instance.GetMap().mapModel.mapName;
            string fileName = $"{mapName}_{saveName}.json";
            string savePath = Path.Combine(GetSaveFolder(), fileName);

            if (!File.Exists(savePath))
            {
                PopupScreen.instance.SafelyQueue(screen =>
                    screen.ShowOkPopup($"Could not find save file '{fileName}'."));
                return;
            }

            var text = File.ReadAllText(savePath, Encoding.UTF8);
            var saveModel = JsonConvert.DeserializeObject<MapSaveDataModel>(text, SerializerSettings);

            if (saveModel == null)
            {
                throw new System.Exception("Failed to deserialize save data");
            }

            InGame.Bridge.ExecuteContinueFromCheckpoint(
                InGame.Bridge.MyPlayerNumber,
                new KonFuze(),
                ref saveModel,
                true,
                false);

            MelonLogger.Msg($"Successfully loaded save from {savePath}");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Error loading save: {ex.Message}");
            PopupScreen.instance.SafelyQueue(screen =>
                screen.ShowOkPopup("Error loading save."));
        }
    }

    private static string GetSaveFolder()
    {
        string savePath = Path.Combine(Game.instance.playerService.configuration.playerDataRootPath, SaveFolderName);
        Directory.CreateDirectory(savePath);
        return savePath;
    }

    public static void OpenSaveFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = GetSaveFolder(),
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
