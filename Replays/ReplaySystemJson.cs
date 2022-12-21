﻿using BepInEx;
using HarmonyLib;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TootTally.Graphics;
using TrombLoader.Helpers;
using UnityEngine;

namespace TootTally.Replays
{
    public static class ReplaySystemJson
    {
        private static int _targetFramerate;
        private static int _scores_A, _scores_B, _scores_C, _scores_D, _scores_F, _totalScore;
        private static int[] _noteTally; // [nasties, mehs, okays, nices, perfects]
        private static int _replayIndex;
        private static List<int[]> _frameData = new List<int[]>(), _noteData = new List<int[]>();
        private static CustomButton[] _replayBtnArray;

        public static bool wasPlayingReplay;
        private static bool _isReplayPlaying, _isReplayRecording;
        private static bool _isTooting;

        private static float _nextPositionTarget, _lastPosition;
        private static float _nextTimingTarget, _lastTiming;
        private static float _elapsedTime;

        private static string _replayFileName;

        #region LevelSelectControllerPatches

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        public static void AddReplayButtonToLeaderboardPannel(List<SingleTrackData> ___alltrackslist, LevelSelectController __instance)
        {
            GameObject leaderboard = GameObject.Find("MainCanvas/FullScreenPanel/Leaderboard").gameObject;

            _replayBtnArray = new CustomButton[5];

            ReadReplayConfig(___alltrackslist);


            for (int i = 0; i < _replayBtnArray.Length; i++)
            {
                Transform scoreTextTranform = leaderboard.transform.Find((i + 1).ToString()).transform;

                string replayFileName = ReplayConfig.ConfigEntryReplayFileNameArray[i].Value;
                _replayBtnArray[i] =
                    InteractableGameObjectFactory.CreateCustomButton(scoreTextTranform, new Vector2(92, 5), new Vector2(14, 14), "R", "ReplayButton" + i, delegate { _replayFileName = replayFileName; __instance.playbtn.onClick?.Invoke(); });
                _replayBtnArray[i].gameObject.SetActive(replayFileName != "NA");
            }
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.advanceSongs))]
        [HarmonyPostfix]
        public static void SetActiveReplayButtons(List<SingleTrackData> ___alltrackslist, LevelSelectController __instance)
        {
            ReadReplayConfig(___alltrackslist);
            for (int i = 0; i < _replayBtnArray.Length; i++)
            {
                string replayFileName = ReplayConfig.ConfigEntryReplayFileNameArray[i].Value;
                _replayBtnArray[i].RemoveAllOnClickActions();
                _replayBtnArray[i].gameObject.SetActive(replayFileName != "NA");
                _replayBtnArray[i].button.onClick.AddListener(delegate { _replayFileName = replayFileName; __instance.playbtn.onClick?.Invoke(); });

            }
        }
        #endregion

        #region GameControllerPatches

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void GameControllerPostfixPatch(GameController __instance)
        {
            ClearData();
            if (_replayFileName == null)
                StartReplayRecorder(__instance);
            else
                StartReplayPlayer(__instance);
        }
        [HarmonyPatch(typeof(GameController), nameof(GameController.isNoteButtonPressed))]
        [HarmonyPostfix]
        public static void GameControllerIsNoteButtonPressedPostfixPatch(ref bool __result) // Take isNoteButtonPressed's return value and changed it to mine, hehe
        {
            if (_isReplayPlaying)
                __result = _isTooting;
        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
        [HarmonyPostfix]
        public static void PointSceneControllerPostfixPatch(PointSceneController __instance)
        {
            if (_isReplayRecording)
                StopReplayRecorder(__instance);
            else if (_isReplayPlaying)
                StopReplayPlayer(__instance);

        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
        [HarmonyPrefix]
        public static void GameControllerUpdatePrefixPatch(GameController __instance)
        {
            if (_isReplayRecording)
                RecordFrameDataV2(__instance);
            else if (_isReplayPlaying)
                PlaybackReplay(__instance);
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.getScoreAverage))]
        [HarmonyPrefix]
        public static void GameControllerGetScoreAveragePrefixPatch(GameController __instance)
        {
            if (_isReplayRecording)
                RecordNote(__instance);
            else if (_isReplayPlaying)
                SetNoteScore(__instance);

        }


        [HarmonyPatch(typeof(GameController), nameof(GameController.getScoreAverage))]
        [HarmonyPostfix]
        public static void GameControllerGetScoreAveragePostfixPatch(GameController __instance)
        {
            if (_isReplayRecording)
                AddNoteJudgementToNoteData(__instance);
            else if (_isReplayPlaying)
                UpdateInstanceTotalScore(__instance);
        }

        [HarmonyPatch(typeof(PauseCanvasController), nameof(PauseCanvasController.showPausePanel))]
        [HarmonyPostfix]
        static void PauseCanvasControllerShowPausePanelPostfixPatch(PauseCanvasController __instance)
        {
            _isReplayPlaying = _isReplayRecording = false;
            Plugin.LogInfo("Level paused, stopped " + (_isReplayPlaying ? "replay" : "recording"));
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.pauseQuitLevel))]
        [HarmonyPostfix]
        static void GameControllerPauseQuitLevelPostfixPatch(GameController __instance)
        {
            ClearData();
            _isReplayPlaying = _isReplayRecording = false;
            _replayFileName = null;
            Plugin.LogInfo("Level quit, clearing replay data");
        }
        #endregion

        #region Config

        public static string GetSongHash(string trackref) => Plugin.Instance.CalcFileHash(Plugin.SongSelect.GetSongFilePath(true, trackref));

        public static void ReadReplayConfig(List<SingleTrackData> ___alltrackslist)
        {
            string trackref = ___alltrackslist[GlobalVariables.levelselect_index].trackref;
            bool isCustom = Globals.IsCustomTrack(trackref);
            if (isCustom)
            {
                string songName = ___alltrackslist[GlobalVariables.levelselect_index].trackname_short;
                string songHash = GetSongHash(trackref);
                if (songHash != null)
                    ReplayConfig.ReadConfig($"{songName} - {songHash}");
            }
        }
        public static void ReadReplayConfig()
        {
            string trackref = GlobalVariables.chosen_track_data.trackref;
            bool isCustom = Globals.IsCustomTrack(trackref);
            if (isCustom)
            {
                string songName = GlobalVariables.chosen_track_data.trackname_short;
                string songHash = GetSongHash(trackref);
                if (songHash != null)
                    ReplayConfig.ReadConfig($"{songName} - {songHash}");
            }
        }
        #endregion

        #region ReplayRecorder
        private static void StartReplayRecorder(GameController __instance)
        {
            _isReplayRecording = true;
            wasPlayingReplay = false;
            _targetFramerate = Application.targetFrameRate > 60 || Application.targetFrameRate < 1 ? 60 : Application.targetFrameRate;
            _elapsedTime = 0;
            _scores_A = _scores_B = _scores_C = _scores_D = 0;
            Plugin.LogInfo("Started recording replay");
        }

        private static void StopReplayRecorder(PointSceneController __instance)
        {
            SaveReplayToFile(__instance);
            _isReplayRecording = false;
            Plugin.LogInfo("Replay recording finished");
        }

        private static void RecordFrameDataV2(GameController __instance)
        {
            float deltaTime = Time.deltaTime;
            _elapsedTime += deltaTime;
            if (_isReplayRecording && _elapsedTime >= 1f / _targetFramerate)
            {
                _elapsedTime = 0;
                float noteHolderPosition = __instance.noteholder.transform.position.x * 10; // 1 decimal precision
                float pointerPos = __instance.pointer.transform.localPosition.y * 100; //times 100 and convert to int for 2 decimal precision
                bool isTooting = __instance.noteplaying;
                _frameData.Add(new int[] { (int)noteHolderPosition, (int)pointerPos, isTooting ? 1 : 0 });
            }
        }

        private static void RecordNote(GameController __instance)
        {
            var noteIndex = __instance.currentnoteindex;
            var totalScore = __instance.totalscore;
            var multiplier = __instance.multiplier;
            var currentHealth = __instance.currenthealth;
            _scores_A = __instance.scores_A;
            _scores_B = __instance.scores_B;
            _scores_C = __instance.scores_C;
            _scores_D = __instance.scores_D;


            _noteData.Add(new int[] { noteIndex, totalScore, multiplier, (int)currentHealth, -1 }); // has to do the note judgement on postfix
        }

        private static void AddNoteJudgementToNoteData(GameController __instance)
        {
            var noteLetter = _scores_A != __instance.scores_A ? 4 :
               _scores_B != __instance.scores_B ? 3 :
               _scores_C != __instance.scores_C ? 2 :
               _scores_D != __instance.scores_D ? 1 : 0;
            _noteData[_noteData.Count - 1][4] = noteLetter;
        }

        private static void SaveReplayToFile(PointSceneController __instance)
        {
            string replayDir = Path.Combine(Paths.BepInExRootPath, "Replays/");
            // Create Replays directory in case it doesn't exist
            if (!Directory.Exists(replayDir)) Directory.CreateDirectory(replayDir);

            string username = "TestUser";
            string songName = GlobalVariables.chosen_track_data.trackname_short;
            DateTimeOffset currentDateTime = new DateTimeOffset(DateTime.Now.ToUniversalTime());
            string currentDateTimeUnix = currentDateTime.ToUnixTimeSeconds().ToString();
            string replayFilename = $"{username} - {songName} - {currentDateTimeUnix}";

            var replayJson = new JSONObject();
            replayJson["username"] = username;
            replayJson["date"] = currentDateTimeUnix;
            replayJson["song"] = songName;
            replayJson["samplerate"] = _targetFramerate;
            replayJson["scrollspeed"] = GlobalVariables.gamescrollspeed;
            var replayFrameData = new JSONArray();
            OptimizeFrameData(ref _frameData);
            _frameData.ForEach(frame =>
            {
                var frameDataJsonArray = new JSONArray();
                foreach (var frameData in frame)
                    frameDataJsonArray.Add(frameData);
                replayFrameData.Add(frameDataJsonArray);
            });
            replayJson["framedata"] = replayFrameData;


            _noteData[_noteData.Count - 1][1] = GlobalVariables.gameplay_scoretotal; // Manually set the last note's totalscore to the actual totalscore because game is weird...
            var replayNoteData = new JSONArray();
            _noteData.ForEach(notes =>
            {
                var noteDataJsonArray = new JSONArray();
                foreach (var noteData in notes)
                    noteDataJsonArray.Add(noteData);
                replayNoteData.Add(noteDataJsonArray);
            });
            replayJson["notedata"] = replayNoteData;

            File.WriteAllText(replayDir + replayFilename, replayJson.ToString());

            ReadReplayConfig();
            ReplayConfig.SaveToConfig(replayFilename);
        }


        private static void OptimizeFrameData(ref List<int[]> rawReplayFrameData)
        {
            Plugin.LogInfo("Optimizing Replay...");

            //Look for matching position && tooting values and remove same frames with the same positions
            for (int i = 0; i < rawReplayFrameData.Count - 1; i++)
            {
                for (int j = i + 1; j < rawReplayFrameData.Count && rawReplayFrameData[i][1] == rawReplayFrameData[j][1] && rawReplayFrameData[i][2] == rawReplayFrameData[j][2];)
                {
                    rawReplayFrameData.Remove(rawReplayFrameData[j]);
                }
            }
        }

        private static void OptimizeNoteData(ref List<int[]> rawReplayNoteData)
        {
            for (int i = 0; i < rawReplayNoteData.Count; i++)
            {
                //todo
            }
        }
        #endregion

        #region ReplayPlayer
        private static void StartReplayPlayer(GameController __instance)
        {
            _lastTiming = 0;
            _isTooting = false;
            _totalScore = 0;
            _noteTally = new int[5];
            _isReplayPlaying = wasPlayingReplay = true;
            Plugin.LogInfo("Loading replay: " + _replayFileName);
            if (LoadReplay(_replayFileName))
                Plugin.LogInfo("Started replay");
            else
                __instance.pauseQuitLevel();
        }

        private static void StopReplayPlayer(PointSceneController __instance)
        {
            _isReplayPlaying = false;
            GlobalVariables.gameplay_notescores = _noteTally;
            Plugin.LogInfo("Replay finished");
        }

        private static bool LoadReplay(string replayFileName)
        {
            string replayDir = Path.Combine(Paths.BepInExRootPath, "Replays/");
            if (!Directory.Exists(replayDir))
            {
                Plugin.LogInfo("Replay folder not found");
                return false;
            }

            if (!File.Exists(replayDir + replayFileName))
            {
                Plugin.LogInfo("Replay File not found");
                return false;
            }

            string jsonFile = File.ReadAllText(replayDir + replayFileName);
            var replayJson = JSONObject.Parse(jsonFile);
            GlobalVariables.gamescrollspeed = replayJson["scrollspeed"];
            foreach (JSONArray jsonArray in replayJson["framedata"])
                _frameData.Add(new int[] { jsonArray[0], jsonArray[1], jsonArray[2] });
            foreach (JSONArray jsonArray in replayJson["notedata"])
                _noteData.Add(new int[] { jsonArray[0], jsonArray[1], jsonArray[2], jsonArray[3], jsonArray[4] });
            _replayIndex = 0;
            return true;
        }

        private static void PlaybackReplay(GameController __instance)
        {
            if (!__instance.controllermode) __instance.controllermode = true;

            var currentMapPosition = __instance.noteholder.transform.position.x * 10;

            if (_frameData.Count > _replayIndex && _lastPosition != 0)
            {
                var newCursorPosition = Lerp(_lastPosition, _nextPositionTarget, (_lastTiming - currentMapPosition) / (_lastTiming - _nextTimingTarget));
                SetCursorPosition(__instance, newCursorPosition);
            }
            else
            {
                __instance.totalscore = _totalScore;
            }


            while (_frameData.Count > _replayIndex && _isReplayPlaying && currentMapPosition <= _frameData[_replayIndex][0]) //smaller or equal to because noteholder goes toward negative
            {
                _lastTiming = _frameData[_replayIndex][0];
                _lastPosition = _frameData[_replayIndex][1] / 100f;
                if (_replayIndex < _frameData.Count - 1)
                {
                    _nextTimingTarget = _frameData[_replayIndex + 1][0];
                    _nextPositionTarget = _frameData[_replayIndex + 1][1] / 100f;
                }
                else
                {
                    _nextTimingTarget = _lastTiming;
                    _nextPositionTarget = _lastPosition;
                }


                SetCursorPosition(__instance, _frameData[_replayIndex][1] / 100f);
                if ((_frameData[_replayIndex][2] == 1 && !_isTooting) || (_frameData[_replayIndex][2] == 0 && _isTooting)) //if tooting state changes
                    _isTooting = !_isTooting;
                _replayIndex++;
            }

        }

        private static void SetNoteScore(GameController __instance)
        {
            var note = _noteData.Find(x => x[0] == __instance.currentnoteindex);

            if (note != null)
            {
                __instance.totalscore = _totalScore = note[1]; //total score has to be set postfix because notes SOMEHOW still give more points than they should during replay...
                __instance.multiplier = note[2];
                __instance.currenthealth = note[3];
                _noteTally[note[4]]++;
            }
        }

        private static void UpdateInstanceTotalScore(GameController __instance)
        {
            __instance.totalscore = _totalScore;
        }

        #endregion

        #region Utils
        private static float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat + (secondFloat - firstFloat) * by;
        }

        private static void SetCursorPosition(GameController __instance, float newPosition)
        {
            Vector3 pointerPosition = __instance.pointer.transform.localPosition;
            pointerPosition.y = newPosition;
            __instance.pointer.transform.localPosition = pointerPosition;
        }
        private static void ClearData()
        {
            _frameData.Clear();
            _noteData.Clear();
        }
        #endregion
    }
}
