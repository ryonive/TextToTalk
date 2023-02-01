﻿using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using TextToTalk.Backends;
using TextToTalk.UI;

// ReSharper disable UnusedMember.Global

namespace TextToTalk.Modules
{
    public class MainCommandModule : IDisposable
    {
        private readonly ChatGui chat;
        private readonly CommandManager commandManager;

        private readonly PluginConfiguration config;
        private readonly VoiceBackendManager backendManager;
        private readonly ConfigurationWindow configurationWindow;

        private readonly IList<string> commandNames;

        public MainCommandModule(ChatGui chat, CommandManager commandManager, PluginConfiguration config,
            VoiceBackendManager backendManager, ConfigurationWindow configurationWindow)
        {
            this.chat = chat;
            this.commandManager = commandManager;

            this.config = config;
            this.backendManager = backendManager;
            this.configurationWindow = configurationWindow;

            this.commandNames = new List<string>();

            AddCommand("/canceltts", CancelTts, "Cancel all queued TTS messages.");
            AddCommand("/toggletts", ToggleTts, "Toggle TextToTalk's text-to-speech.");
            AddCommand("/disabletts", DisableTts, "Disable TextToTalk's text-to-speech.");
            AddCommand("/enabletts", EnableTts, "Enable TextToTalk's text-to-speech.");
            AddCommand("/tttconfig", ToggleConfig, "Toggle TextToTalk's configuration window.");
        }

        public void CancelTts(string command = "", string args = "")
        {
            this.backendManager.CancelAllSpeech();
        }

        public void ToggleTts(string command = "", string args = "")
        {
            if (this.config.Enabled)
                DisableTts();
            else
                EnableTts();
        }

        public void DisableTts(string command = "", string args = "")
        {
            this.config.Enabled = false;
            CancelTts();
            this.chat.Print("TTS disabled.");
            DetailedLog.Info("TTS disabled.");
        }

        public void EnableTts(string command = "", string args = "")
        {
            this.config.Enabled = true;
            this.chat.Print("TTS enabled.");
            DetailedLog.Info("TTS enabled.");
        }

        public void ToggleConfig(string command = "", string args = "")
        {
            this.configurationWindow.Toggle();
        }

        private void AddCommand(string name, CommandInfo.HandlerDelegate method, string help)
        {
            this.commandManager.AddHandler(name, new CommandInfo(method)
            {
                HelpMessage = help,
                ShowInHelp = true,
            });
            this.commandNames.Add(name);
        }

        private void RemoveCommand(string name)
        {
            this.commandManager.RemoveHandler(name);
        }

        public void Dispose()
        {
            foreach (var commandName in this.commandNames)
            {
                RemoveCommand(commandName);
            }
        }
    }
}