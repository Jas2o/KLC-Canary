﻿using System;
using System.Windows.Input;

namespace KLC_Finch {

    public enum DecodeMode { RawYUV, RawY, BitmapRGB }

    public interface IRemoteControl {
        RCstate state { get; set; }
        bool IsPrivate { get; }

        //DecodeMode DecodeMode { get; set; }

        void CaptureNextScreen();

        void ChangeScreen(string screen_id);

        void ChangeTSSession(string session_id);

        void Disconnect(/*string sessionId*/);

        void Reconnect();

        void SendAutotype(string text);

        void SendBlackScreenBlockInput(bool blackOutScreen, bool blockMouseKB);

        void SendClipboard(string clipboard);

        void SendKeyDown(int javascriptKeyCode, int uSBKeyCode);

        void SendKeyUp(int javascriptKeyCode, int uSBKeyCode);

        void SendMouseDown(MouseButton changedButton);

        void SendMouseDown(System.Windows.Forms.MouseButtons changedButton);

        void SendMousePosition(int x, int y);

        void SendMouseUp(MouseButton changedButton);

        void SendMouseUp(System.Windows.Forms.MouseButtons changedButton);

        void SendMouseWheel(int delta);

        void SendPanicKeyRelease();

        void SendPasteClipboard(string text);

        void SendSecureAttentionSequence();

        void ShowCursor(bool enabled);

        void UpdateScreens(string jsonstr);

        void UploadDrop(string file, Progress<int> progress, bool showExplorer);

        void UpdateScreensHack();
    }
}