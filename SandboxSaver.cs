using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        if (!Settings.ModEnabled || InGame.instance == null || (InGameData.CurrentGame != null && !InGameData.CurrentGame.IsSandbox)) return;

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
            string fileName = $"{mapName}_{saveName}.mapsave";
            string savePath = Path.Combine(GetSaveFolder(), fileName);

            if (File.Exists(savePath))
            {
                PopupScreen.instance.ShowOkPopup($"A save with the name '{saveName}' for this map already exists. Please choose a different name.");
                return;
            }

            MapSaveDataModel saveModel = InGame.instance.CreateCurrentMapSave(
                InGame.instance.currentRoundId,
                InGame.instance.MapDataSaveId);

            var text = JsonConvert.SerializeObject(saveModel, SerializerSettings);
            var bytes = Encoding.UTF8.GetBytes(text);

            using var outputStream = new MemoryStream();
            using (var zlibStream = new ZLibStream(outputStream, CompressionMode.Compress))
            {
                zlibStream.Write(bytes, 0, bytes.Length);
            }

            File.WriteAllBytes(savePath, outputStream.ToArray());

            PopupScreen.instance.ShowOkPopup($"Saved successfully as '{saveName}'!");

            MelonLogger.Msg($"Successfully saved sandbox state to {savePath}");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Error saving sandbox state: {ex.Message}");
            PopupScreen.instance.ShowOkPopup("Error making save. See the log for details.");
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
            string mapName = InGame.instance.GetMap().mapModel.mapName;
            string fileName = $"{mapName}_{saveName}.mapsave";
            string savePath = Path.Combine(GetSaveFolder(), fileName);

            if (!File.Exists(savePath))
            {
                PopupScreen.instance.ShowOkPopup($"Could not find save file '{fileName}'.");
                return;
            }

            LoadSaveFromPath(savePath);
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Error loading sandbox save: {ex.Message}");
            PopupScreen.instance.ShowOkPopup("Error loading sandbox save.");
        }
    }

    private void LoadSaveFromPath(string savePath)
    {
        var bytes = File.ReadAllBytes(savePath);

        using var inputStream = new MemoryStream(bytes);
        using var outputStream = new MemoryStream();

        using (var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress))
        {
            zlibStream.CopyTo(outputStream);
        }

        var text = Encoding.UTF8.GetString(outputStream.ToArray());
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

        MelonLogger.Msg($"Successfully loaded sandbox state from {savePath}");
    }

    // dont call this before the Game.instance instance is initialised
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