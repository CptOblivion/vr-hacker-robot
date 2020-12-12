using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Desktop_PINPad : Desktop_Window
{
    static Desktop_PINPad current;
    public static Desktop_Node.NodeIndexEvent OnPasswordSuccess;
    public static Desktop_Node.NodeIndexEvent OnPasswordFail;
    static int TargetPIN = -1;
    static int CurrentPIN = -1;
    static readonly int MaxPIN = 100000; //this number should be a 1, followed by 0s, filling the number of digits available for the pin
    public TMP_Text EnteredPINReadout;
    public TMP_Text WindowTitle;
    public Color ColorWrongPassword;
    static int Leaf;
    bool Success = true;

    protected override void Awake()
    {
        base.Awake();
        current = this;
        OnPasswordSuccess = new Desktop_Node.NodeIndexEvent();
        OnPasswordFail = new Desktop_Node.NodeIndexEvent();
        TargetPIN = -1;
        CurrentPIN = -1;
        current.gameObject.SetActive(false);
    }
    public override void LostFocus()
    {
        //base.LostFocus();
        CancelButton();
    }

    public static void SummonNumpad(int PIN, int LeafIndex, Vector2? Position = null, string CustomHeader = null)
    {
        current.Success = false;
        //TODO: if we already have a numpad open (IE we clicked on something that wants a password while the numpad is currently open), the close and open commands conflict with one another
        //  maybe just instantiate a new pin pad and let the old one close?
        if (CustomHeader == null)
        {
            current.WindowTitle.text = "PASSWORD";
        }
        else
        {
            current.WindowTitle.text = CustomHeader;
        }
        //current.root.gameObject.SetActive(true);
        current.OpenWindow();
        current.FocusWindow();
        if (Position != null)
        {
            Vector3 PositionOffset = ((RectTransform)current.transform).sizeDelta;
            PositionOffset /= 2;
            PositionOffset.x *= -1;

            current.transform.position = (Vector3)Position;
            current.transform.localPosition += PositionOffset;
        }
        //TODO: replace int PIN with string (int doesn't support leading 0s)
        TargetPIN = PIN;
        CurrentPIN = -1;
        Leaf = LeafIndex;

        OnPasswordSuccess.RemoveAllListeners();
        OnPasswordFail.RemoveAllListeners();
        current.UpdateReadout();
    }

    //TODO: keyboard input
    public void NumberButton(int number)
    {
        if (CurrentPIN < 0)
        {
            CurrentPIN = number;
            UpdateReadout();
        }
        else if (CurrentPIN < MaxPIN)
        {
            CurrentPIN *= 10;
            CurrentPIN += number;
            UpdateReadout();
        }
    }
    public void BackspaceButton()
    {
        if (CurrentPIN > 9)
        {
            CurrentPIN -= CurrentPIN % 10;
            CurrentPIN /= 10;
        }
        else
        {
            CurrentPIN = -1;
        }
        UpdateReadout();
    }

    public void ClearButton() //probably won't actually put this on the pad, but might as well put the function in
    {
        CurrentPIN = -1;
        UpdateReadout();
    }
    public void EnterButton()
    {
        if (CurrentPIN == TargetPIN)
        {
            OnPasswordSuccess.Invoke(Leaf);
            OnPasswordFail.RemoveAllListeners();
            OnPasswordSuccess.RemoveAllListeners();
            Success = true;
        }
        else
        {
            CancelButton();
        }
        CloseWindow();
    }
    public void CancelButton()
    {
        CloseWindow();
    }
    public override void CloseWindow()
    {
        base.CloseWindow();
        OnPasswordFail.Invoke(Leaf);
        OnPasswordFail.RemoveAllListeners();
        OnPasswordSuccess.RemoveAllListeners();
        transform.SetAsLastSibling();//pop in front when we lose focus, to show that we're closing
        if (!Success)
        {
            titleBar.SetColor(ColorWrongPassword);
        }
    }

    void UpdateReadout()
    {
        if (CurrentPIN < 0)
        {
            current.EnteredPINReadout.text = "";
        }
        else
        {
            current.EnteredPINReadout.text = CurrentPIN.ToString();
        }
    }

}
