﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Desktop_TextViewer : Desktop_Window
{
    //TODO: in save file, just save the keys (on load, spawn new buttons and generate a new dictionary with the relevante keys)
    static readonly Dictionary<TextDocument, Desktop_ListButton> documents = new Dictionary<TextDocument, Desktop_ListButton>();
    static Desktop_TextViewer current;
    static Desktop_ListButton currentButton;

    public Transform docListContainer;
    public Text docTitle;
    public Text docContents;
    public Desktop_ListButton docListButtonPrefab;

    public Color SelectedButton;
    public Color UnselectedButton;

    //TextDocument currentDocument;
    protected override void Awake()
    {
        base.Awake();
        gameObject.SetActive(false);
        current = this;
        docTitle.text = "";
        docContents.text = "";
    }

    public static void OpenDocument(TextDocument doc)
    {
        if (!documents.ContainsKey(doc))
        {
            Desktop_ListButton docButton = Instantiate(current.docListButtonPrefab.gameObject, current.docListContainer.transform).GetComponent<Desktop_ListButton>();
            documents.Add(doc, docButton);
            docButton.Setup(doc);
        }
        current.ExpandWindow();
        current.FocusWindow();
        UpdateCurrentButton(documents[doc]);
        //current.currentDocument = doc;
        current.docTitle.text = doc.Title;
        current.docContents.text = doc.Contents;
    }

    public static void UpdateCurrentButton(Desktop_ListButton newButton)
    {
        if (currentButton)
        {
            currentButton.SetInactiveButton();
        }
        currentButton = newButton;
        newButton.SetActiveButton();
    }
}