﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UnityEngine.EventSystems;
using UniRx;

#pragma warning disable CS0649

public class SongSelectSceneKeyboardInputController : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public ContinuedKeyPressDetector leftArrowKeyPressDetector;
    [InjectedInInspector]
    public ContinuedKeyPressDetector rightArrowKeyPressDetector;

    [Inject]
    private EventSystem eventSystem;

    [Inject]
    private SongSelectSceneController songSelectSceneController;

    private float fuzzySearchLastInputTimeInSeconds;
    private string fuzzySearchText = "";
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;

    void Start()
    {
        rightArrowKeyPressDetector.ContinuedKeyPressEventStream
            .Subscribe(_ => songSelectSceneController.OnNextSong());
        leftArrowKeyPressDetector.ContinuedKeyPressEventStream
            .Subscribe(_ => songSelectSceneController.OnPreviousSong());
    }

    void Update()
    {
        EKeyboardModifier modifier = InputUtils.GetCurrentKeyboardModifier();

        // Open / close search via Ctrl
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (songSelectSceneController.IsSearchEnabled())
            {
                songSelectSceneController.DisableSearch();
            }
            else
            {
                songSelectSceneController.EnableSearch(SearchInputField.ESearchMode.ByTitleOrArtist);
            }
        }

        // Fuzzy search. Do not handle other input in this case.
        if (IsFuzzySearchActive())
        {
            UpdateFuzzySearchInput();
            return;
        }

        if (modifier != EKeyboardModifier.None)
        {
            return;
        }

        if (songSelectSceneController.IsSearchEnabled())
        {
            // Close search via Escape or Return / Enter
            if (Input.GetKeyUp(KeyCode.Escape)
                || (Input.GetKeyUp(KeyCode.Return) && songSelectSceneController.IsSearchTextInputHasFocus()))
            {
                songSelectSceneController.DisableSearch();
            }
        }
        else
        {
            // Open the main menu via Escape or Backspace
            if (Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Backspace))
            {
                SceneNavigator.Instance.LoadScene(EScene.MainScene);
            }

            // Random song select via R
            if (Input.GetKeyUp(KeyCode.R) && IsNoControlOrSongButtonFocused())
            {
                songSelectSceneController.OnRandomSong();
            }

            // Open the song editor via E
            if (Input.GetKeyUp(KeyCode.E) && IsNoControlOrSongButtonFocused())
            {
                songSelectSceneController.StartSongEditorScene();
            }
        }

        // Select next / previous song with arrow keys or mouse wheel
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.mouseScrollDelta.y < 0)
        {
            songSelectSceneController.OnNextSong();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.mouseScrollDelta.y > 0)
        {
            songSelectSceneController.OnPreviousSong();
        }

        // Open the sing scene via Return / Enter
        if (Input.GetKeyUp(KeyCode.Return) && IsNoControlOrSongButtonFocused())
        {
            songSelectSceneController.CheckAudioAndStartSingScene();
        }

        // Toggle active players with Tab
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            songSelectSceneController.ToggleSelectedPlayers();
        }
    }

    // Directly jump to song with the title that starts with the letters hit on the keyboard
    private void UpdateFuzzySearchInput()
    {
        // When there is no keyboard input for a while, then reset the search term.
        if (fuzzySearchLastInputTimeInSeconds + fuzzySearchResetTimeInSeconds < Time.time)
        {
            fuzzySearchText = "";
        }

        string typedLetter = InputUtils.GetTypedLetter();
        if (!typedLetter.IsNullOrEmpty())
        {
            fuzzySearchLastInputTimeInSeconds = Time.time;
            fuzzySearchText += typedLetter;
            songSelectSceneController.JumpToSongWhereTitleStartsWith(fuzzySearchText);
        }
    }

    // Check that no other control has the focus, such as a checkbox of a player profile.
    // In such a case, keyboard input could be intended to change the controls state, e.g., (de)select the checkbox.
    private bool IsNoControlOrSongButtonFocused()
    {
        GameObject focusedControl = eventSystem.currentSelectedGameObject;
        bool focusedControlIsSongButton = (focusedControl != null && focusedControl.GetComponent<SongRouletteItem>() != null);
        return focusedControl == null || focusedControlIsSongButton;
    }

    private bool IsFuzzySearchActive()
    {
        // In the Unity editor, the Alt key is not sent to the game. So use F1 as alternative.
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.F1);
    }
}
