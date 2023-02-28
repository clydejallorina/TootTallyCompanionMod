﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TootTally.Graphics;
using TootTally.Graphics.Animation;
using TootTally.Utils;
using TootTally.Utils.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TootTally.Multiplayer
{
    public class MultiplayerController
    {
        private static PlaytestAnims _currentInstance;
        private static GameObject _mainPanel, _mainPanelFg, _mainPanelBorder, _acceptButton, _declineButton, _topBar;
        private static CanvasGroup _acceptButtonCanvasGroup, _topBarCanvasGroup, _mainTextCanvasGroup, _declineButtonCanvasGroup;

        public MultiplayerController(PlaytestAnims __instance)
        {
            _currentInstance = __instance;
            _currentInstance.factpanel.gameObject.SetActive(false);

            GameObject canvasWindow = GameObject.Find("Canvas-Window").gameObject;
            Transform panelTransform = canvasWindow.transform.Find("Panel");

            _mainPanel = GameObjectFactory.CreateMultiplayerMainPanel(panelTransform, "MultiPanel");

            _mainPanelFg = _mainPanel.transform.Find("panelfg").gameObject;

            _mainPanelBorder = _mainPanel.transform.Find("Panelbg1").gameObject;

            _topBar = _mainPanel.transform.Find("top").gameObject;
            _topBarCanvasGroup = _topBar.GetComponent<CanvasGroup>();
            _mainTextCanvasGroup = _mainPanelFg.transform.Find("FactText").GetComponent<CanvasGroup>();

        }

        public void AddAcceptDeclineButtonsToPanelFG()
        {
            _acceptButton = GameObjectFactory.CreateCustomButton(_mainPanelFg.transform, new Vector2(-80, -340), new Vector2(200, 50), "Accept", "AcceptButton", OnAcceptButtonClick).gameObject;
            _acceptButtonCanvasGroup = _acceptButton.AddComponent<CanvasGroup>();
            _declineButton = GameObjectFactory.CreateCustomButton(_mainPanelFg.transform, new Vector2(-320, -340), new Vector2(200, 50), "Decline", "DeclineButton", OnDeclineButtonClick).gameObject;
            _declineButtonCanvasGroup = _declineButton.AddComponent<CanvasGroup>();
        }

        public void OnMultiplayerHomeScreenEnter()
        {
            DestroyFactTextTopBarAndAcceptDeclineButtons();
            AddHomeScreenPanelsToMainPanel();
            AnimateHomeScreenPanels();
        }

        public void AddHomeScreenPanelsToMainPanel()
        {
            #region topPanel
            GameObject topPanel = GameObjectFactory.CreateEmptyMultiplayerPanel(_mainPanel.transform, "TopPanel", new Vector2(1230, 50), new Vector2(0, 284));
            topPanel.GetComponent<RectTransform>().localScale = Vector2.zero;
            HorizontalLayoutGroup topPanelLayoutGroup = topPanel.transform.Find("panelfg").gameObject.AddComponent<HorizontalLayoutGroup>();
            topPanelLayoutGroup.padding = new RectOffset(8, 8, 8, 8);
            Text lobbyText = GameObjectFactory.CreateSingleText(topPanel.transform.Find("panelfg"), "TitleText", "TootTally Multiplayer Lobbies", Color.white);
            lobbyText.alignment = TextAnchor.MiddleLeft;
            Text serverText = GameObjectFactory.CreateSingleText(topPanel.transform.Find("panelfg"), "ServerText", "Current Server: localHost", Color.white);
            serverText.alignment = TextAnchor.MiddleRight;
            #endregion

            #region topPanel
            GameObject leftPanel = GameObjectFactory.CreateEmptyMultiplayerPanel(_mainPanel.transform, "LeftPanel", new Vector2(750, 564), new Vector2(-240, -28));
            leftPanel.GetComponent<RectTransform>().localScale = Vector2.zero;
            VerticalLayoutGroup leftPanelLayoutGroup = leftPanel.transform.Find("panelfg").gameObject.AddComponent<VerticalLayoutGroup>();
            leftPanelLayoutGroup.childForceExpandHeight = leftPanelLayoutGroup.childScaleHeight = leftPanelLayoutGroup.childControlHeight = false;
            leftPanelLayoutGroup.padding = new RectOffset(8, 8, 8, 8);
            GameObjectFactory.CreateLobbyInfoRow(leftPanel.transform.Find("panelfg"), "TestRow1", "gristCollector's Lobby", 1, 16, "Never gonna give you up", 69f);
            GameObjectFactory.CreateLobbyInfoRow(leftPanel.transform.Find("panelfg"), "TestRow2", "Electrostats's Lobby", 1, 32, "Taps", 1f);
            GameObjectFactory.CreateLobbyInfoRow(leftPanel.transform.Find("panelfg"), "TestRow3", "Lumpytf's private room", 1, 1, "Forever Alone", 12f);
            GameObjectFactory.CreateLobbyInfoRow(leftPanel.transform.Find("panelfg"), "TestRow4", "GloomHonk's Meme songs", 1, 99, "tt is love tt is life", 224f);
            #endregion

            #region topPanel
            GameObject topRightPanel = GameObjectFactory.CreateEmptyMultiplayerPanel(_mainPanel.transform, "TopRightPanel", new Vector2(426, 280), new Vector2(402, 114));
            topRightPanel.GetComponent<RectTransform>().localScale = Vector2.zero;
            #endregion

            #region topPanel
            GameObject bottomRightPanel = GameObjectFactory.CreateEmptyMultiplayerPanel(_mainPanel.transform, "BottomRightPanel", new Vector2(426, 280), new Vector2(402, -170));
            bottomRightPanel.GetComponent<RectTransform>().localScale = Vector2.zero;
            #endregion

        }

        public void AnimateHomeScreenPanels()
        {
            GameObject topPanel = _mainPanel.transform.Find("TopPanel").gameObject;
            GameObject leftPanel = _mainPanel.transform.Find("LeftPanel").gameObject;
            GameObject topRightPanel = _mainPanel.transform.Find("TopRightPanel").gameObject;
            GameObject bottomRightPanel = _mainPanel.transform.Find("BottomRightPanel").gameObject;

            AnimationManager.AddNewSizeDeltaAnimation(_mainPanelFg, new Vector2(1240, 630), 0.8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));
            AnimationManager.AddNewSizeDeltaAnimation(_mainPanelBorder, new Vector2(1250, 640), 0.8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f), (sender) =>
            {
                AnimationManager.AddNewScaleAnimation(topPanel, Vector2.one, .8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));
                AnimationManager.AddNewScaleAnimation(leftPanel, Vector2.one, .8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));

                //testing button
                /*CustomButton selectSongButton = GameObjectFactory.CreateCustomButton(leftPanel.transform, Vector2.zero, new Vector2(200, 200), "SelectSong", "SelectSongButton", delegate
                {
                    MultiplayerManager.UpdateMultiplayerState(MultiplayerState.SelectSong);
                });
                selectSongButton.GetComponent<RectTransform>().localScale = Vector2.zero;
                AnimationManager.AddNewScaleAnimation(selectSongButton.gameObject, Vector2.one, .8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));*/

                AnimationManager.AddNewScaleAnimation(topRightPanel, Vector2.one, .8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));
                AnimationManager.AddNewScaleAnimation(bottomRightPanel, Vector2.one, .8f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));

                MultiplayerManager.UpdateMultiplayerState(MultiplayerState.Home);
            });
        }

        public void EnterMainPanelAnimation()
        {
            AnimationManager.AddNewPositionAnimation(_mainPanel, new Vector2(0, -20), 2f, new EasingHelper.SecondOrderDynamics(1.25f, 1f, 0f));
        }

        public void OnAcceptButtonClick()
        {
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.LoadHome);
            AnimationManager.AddNewSizeDeltaAnimation(_acceptButton, Vector2.zero, 1f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f));
            AnimationManager.AddNewSizeDeltaAnimation(_declineButton, Vector2.zero, 1f, new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f), (sender) => DestroyFactTextTopBarAndAcceptDeclineButtons());
            _currentInstance.sfx_ok.Play();
        }

        public void OnDeclineButtonClick()
        {
            MultiplayerManager.UpdateMultiplayerState(MultiplayerState.ExitScene);
            GameObject.Destroy(_mainPanel);
        }

        public void DestroyFactTextTopBarAndAcceptDeclineButtons()
        {
            GameObject.DestroyImmediate(_mainPanelFg.transform.Find("FactText").gameObject);
            GameObject.DestroyImmediate(_topBar);
            if (_acceptButton != null)
                GameObject.DestroyImmediate(_acceptButton);
            if (_declineButton != null)
            GameObject.DestroyImmediate(_declineButton);
        }

        public void OnExitAnimation()
        {
            AnimationManager.AddNewScaleAnimation(_mainPanel, Vector2.zero, 2f, new EasingHelper.SecondOrderDynamics(.75f, 1f, 0f));
        }



        public enum MultiplayerState
        {
            None,
            FirstTimePopUp,
            LoadHome,
            Home,
            Lobby,
            Hosting,
            SelectSong,
            ExitScene,
        }
    }
}
