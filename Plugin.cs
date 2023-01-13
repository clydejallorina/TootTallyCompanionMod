﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using TrombLoader.Helpers;
using UnityEngine.UI;
using TootTally.Graphics;
using TootTally.Replays;
using TootTally.Utils;
using TootTally.CustomLeaderboard;

namespace TootTally
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("AutoToot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.crispykevin.hovertoot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("TrombSettings", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("TrombLoader", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static void LogDebug(string msg) => Instance.Logger.LogDebug(msg);
        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);
        public static void LogWarning(string msg) => Instance.Logger.LogWarning(msg);

        public static Plugin Instance;
        private Dictionary<string, string> plugins = new();
        public const int BUILDDATE = 20230112;
        public ConfigEntry<string> APIKey { get; private set; }
        public ConfigEntry<bool> AllowTMBUploads { get; private set; }

        public string CalcSHA256Hash(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string ret = "";
                byte[] hashArray = sha256.ComputeHash(data);
                foreach (byte b in hashArray)
                {
                    ret += $"{b:x2}";
                }
                return ret;
            }
        }

        public void Log(string msg)
        {
            LogInfo(msg);
        }

        public string CalcFileHash(string fileLocation)
        {
            if (!File.Exists(fileLocation))
                return "";
            return CalcSHA256Hash(File.ReadAllBytes(fileLocation));
        }

        private void Awake()
        {
            if (Instance != null) return; // Make sure that this is a singleton (even though it's highly unlikely for duplicates to happen)
            Instance = this;

            // Config
            APIKey = Config.Bind("API Setup", "API Key", "SignUpOnTootTally.com", "API Key for Score Submissions");
            AllowTMBUploads = Config.Bind("API Setup", "Allow Unknown Song Uploads", false, "Should this mod send unregistered charts to the TootTally server?");
            object settings = OptionalTrombSettings.GetConfigPage("TootTally");
            if (settings != null)
            {
                OptionalTrombSettings.Add(settings, AllowTMBUploads);
                OptionalTrombSettings.Add(settings, APIKey);
            }

            // Read every plugin being loaded by BepInEx and hash it
            // foreach (KeyValuePair<string, BepInEx.PluginInfo> plugin in Chainloader.PluginInfos)
            // {
            //     LogInfo($"PLUGIN: {plugin.Key} | HASH: {CalcFileHash(plugin.Value.Location)}");
            // }

            AssetManager.LoadAssets();
            Harmony.CreateAndPatchAll(typeof(SongSelect));
            Harmony.CreateAndPatchAll(typeof(ReplaySystemJson));
            Harmony.CreateAndPatchAll(typeof(GameObjectFactory));
            Harmony.CreateAndPatchAll(typeof(GlobalLeaderboardManager));
            Harmony.CreateAndPatchAll(typeof(PopUpNotifManager));
            LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static string GenerateBaseTmb(string songFilePath, SingleTrackData singleTrackData = null)
        {
            if (singleTrackData == null) singleTrackData = GlobalVariables.chosen_track_data;
            var tmb = new JSONObject();
            tmb["name"] = singleTrackData.trackname_long;
            tmb["shortName"] = singleTrackData.trackname_short;
            tmb["trackRef"] = singleTrackData.trackref;
            int year = 0;
            int.TryParse(new string(singleTrackData.year.Where(char.IsDigit).ToArray()), out year);
            tmb["year"] = year;
            tmb["author"] = singleTrackData.artist;
            tmb["genre"] = singleTrackData.genre;
            tmb["description"] = singleTrackData.desc;
            tmb["difficulty"] = singleTrackData.difficulty;
            using (FileStream fileStream = File.Open(songFilePath, FileMode.Open))
            {
                var binaryFormatter = new BinaryFormatter();
                var savedLevel = (SavedLevel)binaryFormatter.Deserialize(fileStream);
                var levelData = new JSONArray();
                savedLevel.savedleveldata.ForEach(arr =>
                {
                    var noteData = new JSONArray();
                    foreach (var note in arr) noteData.Add(note);
                    levelData.Add(noteData);
                });
                tmb["savednotespacing"] = savedLevel.savednotespacing;
                tmb["endpoint"] = savedLevel.endpoint;
                tmb["timesig"] = savedLevel.timesig;
                tmb["tempo"] = savedLevel.tempo;
                tmb["notes"] = levelData;
            }
            return tmb.ToString();
        }

        public void Update()
        {

        }


        //Would like to rewrite this somewhere else than in plugin
        public static class SongSelect
        {
            public static string songHash { get; private set; }
            public static int maxCombo;

            [HarmonyPatch(typeof(LoadController), nameof(LoadController.LoadGameplayAsync))]
            [HarmonyPrefix]
            public static void AddSongToDBIfNotExist(LoadController __instance)
            {
                string trackRef = GlobalVariables.chosen_track;
                bool isCustom = Globals.IsCustomTrack(trackRef);
                string songFilePath = GetSongFilePath(isCustom, trackRef);
                string tmb = File.ReadAllText(songFilePath, Encoding.UTF8);
                songHash = isCustom ? Instance.CalcFileHash(songFilePath) : trackRef;

                __instance.StartCoroutine(TootTallyAPIService.GetHashInDB(songHash, isCustom, (songHashInDB) =>
                {
                    if (Instance.AllowTMBUploads.Value && songHashInDB == 0)
                    {
                        SerializableClass.Chart chart = new SerializableClass.Chart { tmb = tmb };
                        __instance.StartCoroutine(TootTallyAPIService.AddChartInDB(chart));
                    }
                }));
                maxCombo = 0; // Reset tracked maxCombo
            }

            public static string GetSongFilePath(bool isCustom, string trackRef)
            {
                return isCustom ?
                    Path.Combine(Globals.ChartFolders[trackRef], "song.tmb") :
                    $"{Application.streamingAssetsPath}/leveldata/{trackRef}.tmb";
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.updateHighestCombo))]
            [HarmonyPostfix]
            public static void UpdateCombo(GameController __instance)
            {
                maxCombo = __instance.highestcombo_level;
            }

        }
    }
}
