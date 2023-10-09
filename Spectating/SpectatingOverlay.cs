﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using TootTally.Graphics;
using TootTally.Utils;
using static TootTally.Spectating.SpectatingManager;
using TootTally.Graphics.Animation;
using static Mono.Security.X509.X520;

namespace TootTally.Spectating
{
    public static class SpectatingOverlay
    {
        private static GameObject _overlayCanvas;
        private static GameObject _pauseTextHolder;
        private static CustomAnimation _pauseTextHolderAnimation;
        private static LoadingIcon _loadingIcon;
        private static SpectatingViewerIcon _viewerIcon;
        private static bool _isInitialized;
        private static SocketSpectatorInfo _spectatorInfo;
        private static UserState _currentUserState;

        public static void Initialize()
        {
            _overlayCanvas = new GameObject("TootTallySpectatorOverlayCanvas");
            _overlayCanvas.SetActive(true);
            GameObject.DontDestroyOnLoad(_overlayCanvas);
            Canvas canvas = _overlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2;
            CanvasScaler scaler = _overlayCanvas.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            _loadingIcon = GameObjectFactory.CreateLoadingIcon(_overlayCanvas.transform, Vector2.zero, new Vector2(128, 128), AssetManager.GetSprite("icon.png"), false, "SpectatorLoadingSwirly");
            var rect = _loadingIcon.iconHolder.GetComponent<RectTransform>();
            rect.anchorMax = rect.anchorMin = new Vector2(.9f, .1f);

            _viewerIcon = GameObjectFactory.CreateDefaultViewerIcon(_overlayCanvas.transform, "ViewerIcon");

            _pauseTextHolder = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, new Vector2(1000,350), 8f, "PauseTextOverlay");
            _pauseTextHolder.SetActive(false);
            _pauseTextHolder.transform.Find("FSLatencyPanel").GetComponent<Image>().enabled = false;
            var rectPauseText = _pauseTextHolder.GetComponent<RectTransform>();
            rectPauseText.anchorMax = rectPauseText.anchorMin = rectPauseText.pivot = Vector2.one / 2f;
            var pauseTextContainer = _pauseTextHolder.transform.Find("FSLatencyPanel/LatencyFG").gameObject;
            GameObjectFactory.DestroyFromParent(pauseTextContainer, "title");
            GameObjectFactory.DestroyFromParent(pauseTextContainer, "subtitle");
            GameObjectFactory.DestroyFromParent(pauseTextContainer, "MainPage");

            var pauseText = GameObjectFactory.CreateSingleText(pauseTextContainer.transform, "PauseText", "Host paused the song.\n Waiting for an action.", GameTheme.themeColors.leaderboard.text);
            pauseText.fontSize = 72;
            pauseText.alignment = TMPro.TextAlignmentOptions.Center;
            pauseText.rectTransform.pivot = new Vector2(0, .5f);

            _isInitialized = true;
        }

        public static void UpdateViewerList(SocketSpectatorInfo spectatorInfo)
        {
            if (spectatorInfo == null)
            {
                _viewerIcon?.UpdateViewerCount(0);
                _spectatorInfo = null;
            }
            else
            {
                _spectatorInfo = spectatorInfo;
                _viewerIcon?.UpdateViewerCount(spectatorInfo.count);
            }
            UpdateViewIcon();
        }

        public static void SetCurrentUserState(UserState newUserState)
        {
            _currentUserState = newUserState;
            UpdateViewIcon();
        }

        public static void UpdateViewIcon()
        {
            if (!Plugin.Instance.ShowSpectatorCount.Value || _viewerIcon == null) return;

            if (_spectatorInfo == null || _spectatorInfo.count < 1)
                _viewerIcon.Hide();
            else if (IsInLevelSelect)
            {
                _viewerIcon.SetAnchorMinMax(new Vector2(.92f, .97f));
                _viewerIcon.Show();
            }
            else if (IsInGameController)
            {
                _viewerIcon.SetAnchorMinMax(new Vector2(.03f, .07f));
                _viewerIcon.Show();
            }
            else _viewerIcon.Hide();
        }

        private static bool IsInGameController => _currentUserState == UserState.Playing || _currentUserState == UserState.Paused;
        private static bool IsInLevelSelect => _currentUserState == UserState.SelectingSong;

        public static void ShowViewerIcon() => _viewerIcon?.Show();
        public static void HideViewerIcon() => _viewerIcon?.Hide();

        public static void ShowLoadingIcon()
        {
            if (_loadingIcon == null) return;

            _loadingIcon.StartRecursiveAnimation();
            _loadingIcon.Show();
        }

        public static void HideLoadingIcon()
        {
            if (_loadingIcon == null) return;

            _loadingIcon.Hide();
            _loadingIcon.StopRecursiveAnimation(true);
        }
        public static bool IsLoadingIconVisible() => _loadingIcon.IsVisible();

        public static void ShowPauseText()
        {
            if (_pauseTextHolder == null) return;

            _pauseTextHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1500);
            _pauseTextHolder.SetActive(true);
            _pauseTextHolderAnimation?.Dispose();
            _pauseTextHolderAnimation = AnimationManager.AddNewPositionAnimation(_pauseTextHolder, Vector2.zero, 0.8f, new Utils.Helpers.EasingHelper.SecondOrderDynamics(2.25f, .94f, 1.15f));
        }

        public static void HidePauseText()
        {
            if (_pauseTextHolder == null) return;

            _pauseTextHolderAnimation?.Dispose();
            _pauseTextHolder.SetActive(false);

        }

    }
}
