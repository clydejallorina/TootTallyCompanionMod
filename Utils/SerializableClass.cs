﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TootTally.Utils
{
    public static class SerializableClass
    {
        [Serializable]
        public class Chart
        {
            public string tmb;
        }


        [Serializable]
        public class ScoreDataFromDB
        {
            public int score;
            public string player;
            public string played_on;
            public string grade;
            public int[] noteTally;
            public string replay_id;
            public int max_combo;
            public float percentage;
            public string game_version;
        }

        [Serializable]
        public class SendableModInfo
        {
            public string name;
            public string version;
            public string hash;

        }


        [Serializable]
        public class APISubmission
        {
            public string apiKey;
        }

        [Serializable]
        public class ReplayUUIDSubmission
        {
            public string apiKey;
            public string songHash;
        }

        [Serializable]
        public class ReplayStopSubmission
        {
            public string apiKey;
            public string replayId;
        }


        [Serializable]
        public class User
        {
            public string username;
            public int id;
        }

        #region Theme
        [Serializable]
        public struct BackButtonJson
        {
            public string background;
            public string outline;
            public string text;
            public string shadow;
        }

        [Serializable]
        public struct CapsulesJson
        {
            public string year;
            public string yearShadow;
            public string composer;
            public string composerShadow;
            public string genre;
            public string genreShadow;
            public string description;
            public string descriptionShadow;
            public string tempo;
        }

        [Serializable]
        public struct DiffStarJson
        {
            public string gradientStart;
            public string gradientEnd;
        }

        [Serializable]
        public struct LeaderboardJson
        {
            public string panelBody;
            public string scoresBody;
            public string rowEntry;
            public string yourRowEntry;
            public string headerText;
            public string text;
            public string textOutline;
            public SliderJson slider;
            public TabsJson tabs;
        }

        [Serializable]
        public struct NotificationJson
        {
            public string border;
            public string background;
            public string defaultText;
            public string warningText;
            public string errorText;
            public string textOutline;
        }

        [Serializable]
        public struct PlayButtonJson
        {
            public string background;
            public string outline;
            public string text;
            public string shadow;
        }

        [Serializable]
        public struct RandomButtonJson
        {
            public string background;
            public string outline;
            public string text;
            public string normal;
            public string pressed;
            public string highlighted;
            public string selected;
        }

        [Serializable]
        public struct ReplayButtonJson
        {
            public string text;
            public string normal;
            public string pressed;
            public string highlighted;
            public string selected;
        }

        [Serializable]
        public class JsonThemeDeserializer
        {
            public string version;
            [SerializeField]
            public ThemeJson theme;
        }

        [Serializable]
        public struct ScrollSpeedSliderJson
        {
            public string handle;
            public string text;
            public string background;
            public string fill;
        }

        [Serializable]
        public struct SliderJson
        {
            public string handle;
            public string background;
            public string fill;
        }

        [Serializable]
        public struct SongButtonJson
        {
            public string background;
            public string text;
            public string textOver;
            public string outline;
            public string outlineOver;
            public string shadow;
            public string square;
        }

        [Serializable]
        public struct TabsJson
        {
            public string normal;
            public string pressed;
            public string highlighted;
            public string selected;
        }

        [Serializable]
        public struct ThemeJson
        {
            [SerializeField]
            public LeaderboardJson leaderboard;
            public ScrollSpeedSliderJson scrollSpeedSlider;
            public NotificationJson notification;
            public ReplayButtonJson replayButton;
            public CapsulesJson capsules;
            public RandomButtonJson randomButton;
            public BackButtonJson backButton;
            public PlayButtonJson playButton;
            public SongButtonJson songButton;
            public DiffStarJson diffStar;
        }
        #endregion
    }
}
