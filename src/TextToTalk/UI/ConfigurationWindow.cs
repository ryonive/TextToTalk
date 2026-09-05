using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using R3;
using TextToTalk.Backends;
using TextToTalk.Data.Model;
using TextToTalk.GameEnums;
using TextToTalk.Localization;
using TextToTalk.Services;
using TextToTalk.Resources;

namespace TextToTalk.UI
{
    public class ConfigurationWindow : Window, IDisposable
    {
        private PluginConfiguration config;
        private readonly IDataManager data;
        private readonly Localizer localizer;
        private readonly VoiceBackendManager backendManager;
        private readonly PlayerService players;
        private readonly NpcService npc;
        private readonly IConfigUIDelegates helpers;
        private readonly Subject<bool> onPresetOpenRequested;

        private IDictionary<Guid, string> playerWorldEditing = new Dictionary<Guid, string>();
        private IDictionary<Guid, bool> playerWorldValid = new Dictionary<Guid, bool>();
        private string playerName = string.Empty;
        private string playerWorld = string.Empty;
        private string playerWorldError = string.Empty;
        private string npcName = string.Empty;
        private string npcError = string.Empty;


        public ConfigurationWindow(PluginConfiguration config, IDataManager data, Localizer localizer, VoiceBackendManager backendManager,
            PlayerService players, NpcService npc, Window voiceUnlockerWindow) : base(
            $"{Strings.ConfigurationTitle}###TextToTalkConfig")
        {
            this.config = config;

            this.data = data;
            this.localizer = localizer;
            this.backendManager = backendManager;
            this.players = players;
            this.npc = npc;
            this.helpers = new ConfigUIDelegates { OpenVoiceUnlockerAction = () => voiceUnlockerWindow.IsOpen = true };
            this.onPresetOpenRequested = new Subject<bool>();

            Size = new Vector2(540, 480);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Open()
        {
            IsOpen = true;
        }

        public Observable<bool> OnPresetOpenRequested()
        {
            return this.onPresetOpenRequested;
        }

        public override void PreDraw()
        {
            WindowName =
                $"{Strings.ConfigurationTitle} (TTS {(this.config.Enabled ? Strings.EnabledSuffix : Strings.DisabledSuffix)})###TextToTalkConfig";

            var titleBarColor = this.backendManager.GetBackendTitleBarColor();
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, titleBarColor != default
                ? titleBarColor
                : ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TitleBgActive)));
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor();
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar($"TextToTalk##{MemoizedId.Create()}"))
            {
                if (ImGui.BeginTabItem(Strings.TabSynthesizerSettings))
                {
                    DrawSynthesizerSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Strings.TabPlayerVoices))
                {
                    DrawPlayerVoiceSettings();
                    ImGui.EndTabItem();
                }
                else if (this.playerWorldEditing.Count > 0)
                {
                    // Clear all user edits if the tab isn't selected anymore
                    this.playerWorldEditing = new Dictionary<Guid, string>();
                    this.playerWorldValid = new Dictionary<Guid, bool>();
                    this.playerName = string.Empty;
                    this.playerWorld = string.Empty;
                    this.playerWorldError = string.Empty;
                }

                if (ImGui.BeginTabItem(Strings.TabNpcVoices))
                {
                    DrawNpcVoiceSettings();
                    ImGui.EndTabItem();
                }
                else
                {
                    // Clear all user edits if the tab isn't selected anymore
                    this.npcName = string.Empty;
                    this.npcError = string.Empty;
                }

                if (ImGui.BeginTabItem(Strings.TabChannelSettings))
                {
                    DrawChannelSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Strings.TabTriggersExclusions))
                {
                    DrawTriggersExclusions();
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }


        private void DrawSynthesizerSettings() // I'm sure there's a cleaner method to create a dropdown box ¯\_(ツ)_/¯
        {
            if (ImGui.CollapsingHeader($"{Strings.SectionKeybinds}##{MemoizedId.Create()}"))
            {
                ConfigComponents.ToggleUseKeybind($"{Strings.EnableKeybind}##{MemoizedId.Create()}", this.config);

                ImGui.PushItemWidth(100f);
                var kItem1 = VirtualKey.EnumToIndex(this.config.ModifierKey);
                if (ImGui.Combo($"##{MemoizedId.Create()}", ref kItem1, VirtualKey.DisplayNames.Take(3).ToArray(), 3))
                {
                    this.config.ModifierKey = VirtualKey.IndexToEnum(kItem1);
                    this.config.Save();
                }

                ImGui.SameLine();
                var kItem2 = VirtualKey.EnumToIndex(this.config.MajorKey) - 3;
                if (ImGui.Combo($"{Strings.TtsToggleKeybind}##{MemoizedId.Create()}", ref kItem2,
                        VirtualKey.DisplayNames.Skip(3).ToArray(), VirtualKey.DisplayNames.Length - 3))
                {
                    this.config.MajorKey = VirtualKey.IndexToEnum(kItem2 + 3);
                    this.config.Save();
                }

                ImGui.PopItemWidth();
            }

            if (ImGui.CollapsingHeader(Strings.SectionGeneral))
            {
                Components.ChooseOutputAudioDevice($"{Strings.AudioOutputDevice}##{MemoizedId.Create()}", this.config);

                ImGui.Spacing();

                // Global volume is stored as a linear multiplier (0.0 - 2.0), but rendered
                // as a percentage (0% - 200%) so it reads more naturally to users.
                var volumePercent = this.config.GlobalVolume * 100f;
                if (ImGui.SliderFloat($"{Strings.Volume}##{MemoizedId.Create()}", ref volumePercent, 0f, 200f, "%.0f%%"))
                {
                    this.config.GlobalVolume = volumePercent / 100f;
                    this.config.Save();
                }

                Components.HelpTooltip(
                    Strings.VolumeHelp);

                ImGui.Spacing();

                ConfigComponents.ToggleReadFromQuestTalkAddon(
                    Strings.ReadNpcDialogueFromDialogueWindow,
                    this.config);

                if (this.config.ReadFromQuestTalkAddon)
                {
                    ImGui.Spacing();
                    ImGui.Indent();

                    ConfigComponents.ToggleCancelSpeechOnTextAdvance(
                        Strings.CancelSpeechOnTextAdvance,
                        this.config);
                    ConfigComponents.ToggleSkipVoicedQuestText(
                        Strings.SkipVoicedNpcDialogue,
                        this.config);

                    ImGui.Unindent();
                }

                ImGui.Spacing();
                ConfigComponents.ToggleReadFromBattleTalkAddon(
                    Strings.ReadNpcDialogueFromBattleWindow,
                    this.config);

                if (this.config.ReadFromBattleTalkAddon)
                {
                    ImGui.Spacing();
                    ImGui.Indent();

                    ConfigComponents.ToggleSkipVoicedBattleText(
                        Strings.SkipVoicedNpcDialogue,
                        this.config);

                    ImGui.Unindent();
                }

                ImGui.Spacing();
                ConfigComponents.ToggleSkipMessagesFromYou(Strings.SkipMessagesFromYou, this.config);

                ImGui.Spacing();
                ConfigComponents.ToggleOnlyMessagesFromYou(Strings.OnlyMessagesFromYou, this.config);

                ImGui.Spacing();
                ConfigComponents.ToggleEnableNameWithSay(Strings.EnableNameWithSay, this.config);

                if (this.config.EnableNameWithSay)
                {
                    ImGui.Spacing();
                    ImGui.Indent();

                    ConfigComponents.ToggleNameNpcWithSay(Strings.NameNpcWithSay, this.config);
                    ConfigComponents.ToggleSayPlayerWorldName(Strings.SayPlayerWorldName, this.config);
                    ConfigComponents.ToggleDisallowMultipleSay(
                        Strings.DisallowMultipleSay,
                        this.config);
                    ConfigComponents.ToggleSayPartialName(Strings.SayPartialName, this.config);

                    if (this.config.SayPartialName)
                    {
                        ImGui.Spacing();
                        ImGui.Indent();

                        var onlySayFirstOrLastName = (int)this.config.OnlySayFirstOrLastName;

                        if (ImGui.RadioButton(Strings.OnlySayForename, ref onlySayFirstOrLastName,
                                (int)FirstOrLastName.First))
                        {
                            this.config.OnlySayFirstOrLastName = FirstOrLastName.First;
                            this.config.Save();
                        }

                        if (ImGui.RadioButton(Strings.OnlySaySurname, ref onlySayFirstOrLastName,
                                (int)FirstOrLastName.Last))
                        {
                            this.config.OnlySayFirstOrLastName = FirstOrLastName.Last;
                            this.config.Save();
                        }

                        ImGui.Unindent();
                    }

                    ImGui.Unindent();
                }

                ConfigComponents.ToggleUsePlayerRateLimiter(Strings.LimitPlayerTtsFrequency, this.config);

                var messagesPerSecond = this.config.MessagesPerSecond;
                if (this.config.UsePlayerRateLimiter)
                {
                    ImGui.Indent();

                    if (ImGui.DragFloat("", ref messagesPerSecond, 0.1f, 0.1f, 30, Strings.MessagesPerSecondFormat))
                    {
                        this.config.MessagesPerSecond = messagesPerSecond;
                    }

                    ImGui.Unindent();
                }
            }

            if (ImGui.CollapsingHeader($"{Strings.SectionVoices}##{MemoizedId.Create()}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var backends = Enum.GetValues<TTSBackend>().OrderBy(b => b.GetDisplayOrder()).ToArray();
                var backendsDisplay = backends.Select(b => b.GetFormattedName(config)).ToArray();
                var backend = this.config.Backend;
                var backendIndex = Array.IndexOf(backends, backend);

                if (ImGui.Combo($"{Strings.VoiceBackend}##{MemoizedId.Create()}", ref backendIndex, backendsDisplay,
                        backends.Length))
                {
                    var newBackend = backends[backendIndex];

                    this.config.Backend = newBackend;
                    this.config.Save();

                    this.backendManager.SetBackend(newBackend);
                }

                if (!this.backendManager.BackendLoading)
                {
                    // Draw the settings for the specific backend we're using.
                    this.backendManager.DrawSettings(this.helpers);
                }
            }

            if (ImGui.CollapsingHeader(Strings.SectionExperimental, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ConfigComponents.ToggleRemoveStutterEnabled(
                    Strings.RemoveStutter,
                    this.config);
                Components.Tooltip(
                    Strings.RemoveStutterHelp);
            }
        }

        private void DrawPlayerVoiceSettings()
        {
            ImGui.TextColored(ImColor.HintColor, Strings.PlayerVoicesHint);

            ImGui.Spacing();

            ConfigComponents.ToggleUsePlayerVoicePresets(Strings.UsePlayerVoicePresets, this.config);

            ImGui.Spacing();

            var tableSize = new Vector2(0.0f, 300f);
            var presets = this.config.GetVoicePresetsForBackend(this.config.Backend).ToList();
            presets.Sort((a, b) => a.Id - b.Id);
            var presetArray = presets.Select(p => p.Name).ToArray();
            var toDelete = new List<Player>();
            Components.Table($"##{MemoizedId.Create()}", tableSize, ImGuiTableFlags.Borders,
                () =>
                {
                    ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
                    ImGui.TableSetupColumn($"##{MemoizedId.Create()}");
                    ImGui.TableSetupColumn(Strings.TableName);
                    ImGui.TableSetupColumn(Strings.TableWorld);
                    ImGui.TableSetupColumn(Strings.TablePreset);
                    ImGui.TableHeadersRow();
                },
                () => this.players
                    .GetAllPlayers()
                    .Select(row =>
                    {
                        if (!this.playerWorldEditing.TryGetValue(row.Id, out var worldName))
                        {
                            var world = data.GetExcelSheet<World>(this.localizer.GameDataLanguage)?.GetRow(row.WorldId);
                            this.playerWorldEditing[row.Id] = world?.Name.ToString() ?? "";
                        }

                        return (row.Id, row, worldName);
                    }),
                row =>
                {
                    var (id, playerInfo, _) = row;

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(
                            $"{FontAwesomeIcon.Trash.ToIconString()}##{MemoizedId.Create(uniq: id.ToString())}"))
                    {
                        toDelete.Add(playerInfo);
                    }

                    ImGui.PopFont();
                    Components.Tooltip(Strings.ActionDelete);
                },
                row =>
                {
                    var (id, playerInfo, worldName) = row;
                    var name = playerInfo.Name ?? "";

                    // Allow player names to be edited in the table
                    if (ImGui.InputText($"##{MemoizedId.Create(uniq: id.ToString())}", ref name, 32))
                    {
                        playerInfo.Name = name;
                        this.players.UpdatePlayer(playerInfo);
                        DetailedLog.Debug($"Updated player name: {playerInfo.Name}@{worldName ?? ""}");
                    }
                },
                row =>
                {
                    var (id, playerInfo, worldName) = row;

                    // Allow player worlds to be edited in the table
                    worldName ??= "";
                    if (ImGui.InputText($"##{MemoizedId.Create(uniq: id.ToString())}", ref worldName, 32))
                    {
                        this.playerWorldEditing[id] = worldName;

                        // Try to get the input world
                        var worldPending = GetWorldForUserInput(worldName);

                        // Only save the result if the name actually matches a world
                        if (worldPending != null)
                        {
                            this.playerWorldValid[id] = true;
                            playerInfo.WorldId = worldPending.Value.RowId;
                            this.players.UpdatePlayer(playerInfo);
                            DetailedLog.Debug($"Updated player world: {playerInfo.Name}@{worldPending.Value.Name}");
                        }
                        else
                        {
                            this.playerWorldValid[id] = false;
                        }
                    }

                    // Indicate if the operation succeeded
                    if (this.playerWorldValid.TryGetValue(id, out var valid))
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (valid)
                        {
                            ImGui.TextColored(ImColor.Green, FontAwesomeIcon.CheckCircle.ToIconString());
                        }
                        else
                        {
                            ImGui.TextColored(ImColor.Red, FontAwesomeIcon.MinusCircle.ToIconString());
                        }

                        ImGui.PopFont();
                    }
                },
                row =>
                {
                    var (id, playerInfo, worldName) = row;
                    var name = playerInfo.Name;
                    var currentBackend = this.config.Backend.ToString();
                
                    // Pass currentBackend to fetch the preset specific to this backend
                    var presetIndex = this.players.TryGetPlayerVoice(playerInfo, out var v, currentBackend)
                        ? presets.IndexOf(v)
                        : 0;
                
                    if (ImGui.Combo($"##{MemoizedId.Create(uniq: id.ToString())}", ref presetIndex, presetArray, presets.Count))
                    {
                        // SetPlayerVoice now handles the backend-specific composite record
                        if (this.players.SetPlayerVoice(playerInfo, presets[presetIndex]))
                        {
                            DetailedLog.Debug($"Updated voice for {name}@{worldName} on {currentBackend}: {presets[presetIndex].Name}");
                        }
                        else
                        {
                            DetailedLog.Warn($"Failed to update voice for {name}@{worldName}");
                        }
                    }
                });

            if (toDelete.Any())
            {
                foreach (var playerInfo in toDelete)
                {
                    this.players.DeletePlayer(playerInfo);
                }
            }

            ImGui.InputText($"{Strings.PlayerName}##{MemoizedId.Create()}", ref this.playerName, 32);
            ImGui.InputText($"{Strings.PlayerWorld}##{MemoizedId.Create()}", ref this.playerWorld, 32);
            if (!string.IsNullOrEmpty(this.playerWorldError))
            {
                ImGui.TextColored(ImColor.Red, this.playerWorldError);
            }

            if (ImGui.Button($"{Strings.ActionAddPlayer}##{MemoizedId.Create()}"))
            {
                // Validate data before saving the new player
                var world = GetWorldForUserInput(this.playerWorld);

                if (world.HasValue && this.players.AddPlayer(this.playerName, world.Value.RowId))
                {
                    DetailedLog.Info($"Added player: {this.playerName}@{world.ToString()}");
                }
                else
                {
                    this.playerWorldError = Strings.PlayerAddFailedDuplicate;
                    DetailedLog.Error("Failed to add player; this might be a duplicate entry");
                }
            }
        }

        private World? GetWorldForUserInput(string worldName)
        {
            return data.GetExcelSheet<World>(this.localizer.GameDataLanguage)?
                .Where(w => w.IsPublic)
                .Where(w => !string.IsNullOrWhiteSpace(w.Name.ToString()))
                .FirstOrDefault(w =>
                    string.Equals(w.Name.ExtractText(), worldName, StringComparison.InvariantCultureIgnoreCase));
        }

        private void DrawNpcVoiceSettings()
        {
            ImGui.TextColored(ImColor.HintColor, Strings.NpcVoicesHint);

            ImGui.Spacing();

            ConfigComponents.ToggleUseNpcVoicePresets(Strings.UseNpcVoicePresets, this.config);

            ImGui.Spacing();

            var tableSize = new Vector2(0.0f, 300f);
            var presets = this.config.GetVoicePresetsForBackend(this.config.Backend).ToList();
            presets.Sort((a, b) => a.Id - b.Id);
            var presetArray = presets.Select(p => p.Name).ToArray();
            var toDelete = new List<Npc>();
            Components.Table($"##{MemoizedId.Create()}", tableSize, ImGuiTableFlags.Borders,
                () =>
                {
                    ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
                    ImGui.TableSetupColumn($"##{MemoizedId.Create()}");
                    ImGui.TableSetupColumn(Strings.TableName);
                    ImGui.TableSetupColumn(Strings.TablePreset);
                    ImGui.TableHeadersRow();
                },
                () => this.npc
                    .GetAllNpcs()
                    .Select(npc => (npc.Id, npc)),
                row =>
                {
                    var (id, npcInfo) = row;

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(
                            $"{FontAwesomeIcon.Trash.ToIconString()}##{MemoizedId.Create(uniq: id.ToString())}"))
                    {
                        toDelete.Add(npcInfo);
                    }

                    ImGui.PopFont();
                    Components.Tooltip(Strings.ActionDelete);
                },
                row =>
                {
                    var (id, npcInfo) = row;
                    var name = npcInfo.Name ?? "";

                    // Allow NPC names to be edited in the table
                    if (ImGui.InputText($"##{MemoizedId.Create(uniq: id.ToString())}", ref name, 32))
                    {
                        npcInfo.Name = name;
                        this.npc.UpdateNpc(npcInfo);
                        DetailedLog.Debug($"Updated NPC name: {name}");
                    }
                },
                row =>
                {
                    var (id, npcInfo) = row;
                    var name = npcInfo.Name;

                    var currentBackend = this.config.Backend.ToString();
                
                    // Pass currentBackend to find the preset specifically for this backend
                    var presetIndex = this.npc.TryGetNpcVoice(npcInfo, currentBackend, out var v)
                        ? presets.IndexOf(v)
                        : -1; // Use -1 or a "None" index if no voice is set for this backend
                
                    if (ImGui.Combo($"##{MemoizedId.Create(uniq: id.ToString())}", ref presetIndex, presetArray, presets.Count))
                    {
                        if (presetIndex >= 0 && this.npc.SetNpcVoice(npcInfo, presets[presetIndex]))
                        {
                            DetailedLog.Debug($"Updated voice for {name} on {currentBackend}: {presets[presetIndex].Name}");
                        }
                        else
                        {
                            DetailedLog.Warn($"Failed to update voice for {name} ({id})");
                        }
                    }
                });

            if (toDelete.Any())
            {
                foreach (var npcInfo in toDelete)
                {
                    this.npc.DeleteNpc(npcInfo);
                }
            }

            ImGui.InputText($"{Strings.NpcName}##{MemoizedId.Create()}", ref this.npcName, 32);

            if (!string.IsNullOrEmpty(this.npcError))
            {
                ImGui.TextColored(ImColor.Red, this.npcError);
            }

            if (ImGui.Button($"{Strings.ActionAddNpc}##{MemoizedId.Create()}"))
            {
                if (this.npc.AddNpc(this.npcName))
                {
                    this.config.Save();
                    DetailedLog.Info($"Added NPC: {this.npcName}");
                }
                else
                {
                    this.npcError = Strings.NpcAddFailedDuplicate;
                    DetailedLog.Error("Failed to add NPC; this might be a duplicate entry");
                }
            }
        }

        private void DrawChannelSettings()
        {
            var currentEnabledChatTypesPreset = this.config.GetCurrentEnabledChatTypesPreset();

            var presets = this.config.EnabledChatTypesPresets.ToList();
            presets.Sort((a, b) => a.Id - b.Id);
            var presetIndex = presets.IndexOf(currentEnabledChatTypesPreset);
            if (ImGui.Combo($"{Strings.ChannelPreset}##{MemoizedId.Create()}", ref presetIndex, presets.Select(p => p.Name).ToArray(),
                    presets.Count))
            {
                this.config.CurrentPresetId = presets[presetIndex].Id;
                this.config.Save();
            }

            if (ImGui.Button($"{Strings.ActionNewPreset}##{MemoizedId.Create()}"))
            {
                var newPreset = this.config.NewChatTypesPreset();
                this.config.SetCurrentEnabledChatTypesPreset(newPreset.Id);
                this.onPresetOpenRequested.OnNext(true);
            }

            ImGui.SameLine();

            if (ImGui.Button($"{Strings.ActionEdit}##{MemoizedId.Create()}"))
            {
                this.onPresetOpenRequested.OnNext(true);
            }

            if (this.config.EnabledChatTypesPresets.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{Strings.ActionDelete}##{MemoizedId.Create()}"))
                {
                    var otherPreset =
                        this.config.EnabledChatTypesPresets.First(p => p.Id != currentEnabledChatTypesPreset.Id);
                    this.config.SetCurrentEnabledChatTypesPreset(otherPreset.Id);
                    this.config.EnabledChatTypesPresets.Remove(currentEnabledChatTypesPreset);
                }
            }

            ImGui.Spacing();

            ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.6f), Strings.RecommendedForTriggerUse);
            EnabledChatTypesPresetComponents.ToggleEnableAllChatTypes(
                Strings.EnableAllIncludingUndocumented,
                currentEnabledChatTypesPreset);

            if (currentEnabledChatTypesPreset.EnableAllChatTypes) return;
            ImGui.Spacing();

            var channels = Enum.GetNames(typeof(XivChatType))
                .Concat(Enum.GetNames(typeof(AdditionalChatType)))
                .Distinct();
            foreach (var channel in channels)
            {
                XivChatType enumValue;
                try
                {
                    enumValue = (XivChatType)Enum.Parse(typeof(XivChatType), channel);
                }
                catch (ArgumentException)
                {
                    enumValue = (XivChatType)(int)Enum.Parse(typeof(AdditionalChatType), channel);
                }

                var selected = currentEnabledChatTypesPreset.EnabledChatTypes?.Contains((int)enumValue) ?? false;
                if (!ImGui.Checkbox($"{FormatChatChannelName((int)enumValue)}##{channel}", ref selected)) continue;
                var isEnabled = currentEnabledChatTypesPreset.EnabledChatTypes?.Contains((int)enumValue) ?? false;
                if (isEnabled)
                {
                    currentEnabledChatTypesPreset.EnabledChatTypes?.Remove((int)enumValue);
                    this.config.Save();
                }
                else
                {
                    currentEnabledChatTypesPreset.EnabledChatTypes?.Add((int)enumValue);
                    this.config.Save();
                }
            }
        }

        private string FormatChatChannelName(int chatType)
        {
            if (ChatTypeMap.GmBaseChatTypes.TryGetValue((XivChatType)chatType, out var baseChatType))
            {
                if (this.localizer.HasGameDataSheet)
                {
                    if (ChatTypeMap.TryGetLogFilterName(this.data, this.localizer.GameDataLanguage, (int)baseChatType, out var baseName))
                    {
                        return $"GM {baseName}";
                    }
                }

                return $"GM {FormatChatChannelName((int)baseChatType)}";
            }

            if (ChatTypeMap.TryGetAdditionalChannelName(chatType, out var additionalName))
            {
                return additionalName;
            }

            if (this.localizer.HasGameDataSheet)
            {
                if (ChatTypeMap.TryGetLogFilterName(this.data, this.localizer.GameDataLanguage, chatType, out var logFilterName))
                {
                    return logFilterName;
                }

                if (ChatTypeMap.TryGetAddonName(this.data, this.localizer.GameDataLanguage, chatType, out var addonName))
                {
                    return addonName;
                }
            }

            if (!this.localizer.HasGameDataSheet &&
                    ChatTypeMap.TryGetResourceName(chatType, out var resourceName))
            {
                return resourceName;
            }

            var channel = Enum.GetName(typeof(XivChatType), (ushort)chatType)
                          ?? Enum.GetName(typeof(AdditionalChatType), chatType);
            return channel is null ? $"[{chatType}]" : FormatChatChannelName(channel);
        }

        private static string FormatChatChannelName(string channel)
        {
            // Split enum value name into words
            var split = channel == "PvPTeam" ? "PvP Team" : SplitWords(channel);

            // Handle linkshells
            return split.StartsWith("Ls ") ? split.ToUpper() : split;
        }

        private static string SplitWords(string oneWord)
        {
            var words = oneWord
                .Select(c => c)
                .Skip(1)
                .Aggregate("" + oneWord[0],
                    (acc, c) => acc + (c is >= 'A' and <= 'Z' or >= '0' and <= '9' ? " " + c : "" + c))
                .Split(' ');

            var finalWords = new StringBuilder(oneWord.Length + 3);
            for (var i = 0; i < words.Length - 1; i++)
            {
                finalWords.Append(words[i]);
                if (words[i].Length == 1 && words[i + 1].Length == 1)
                {
                    continue;
                }

                finalWords.Append(" ");
            }

            return finalWords.Append(words.Last()).ToString();
        }

        private void DrawTriggersExclusions()
        {
            var currentConfiguration = this.config.GetCurrentEnabledChatTypesPreset();
            EnabledChatTypesPresetComponents.ToggleEnableAllChatTypes(
                Strings.EnableAllChatTypesIncludingUndocumented,
                currentConfiguration);

            ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.6f), Strings.RecommendedForTriggerUse);
            ImGui.Dummy(new Vector2(0, 5));

            ExpandyList(Strings.TriggersHeading, "Trigger", this.config.Good);
            ExpandyList(Strings.ExclusionsHeading, "Exclusion", this.config.Bad);
        }

        private void ExpandyList(string heading, string kind, IList<Trigger> listItems)
        {
            ImGui.Text(heading);

            for (var i = 0; i < listItems.Count; i++)
            {
                var str = listItems[i].Text;
                if (ImGui.InputTextWithHint($"###{MemoizedId.Create(uniq: $"{kind}{i}")}", string.Format(Strings.TriggerInputHintFormat, kind),
                        ref str, 100))
                {
                    listItems[i].Text = str;
                    this.config.Save();
                }

                ImGui.SameLine();
                TriggerComponents.ToggleIsRegex(
                    $"{Strings.TriggersRegex}###{MemoizedId.Create(uniq: $"{kind}{i}")}",
                    listItems[i]);

                ImGui.SameLine();
                if (ImGui.Button($"{Strings.ActionRemove}###{MemoizedId.Create(uniq: $"{kind}{i}")}"))
                {
                    listItems[i].ShouldRemove = true;
                }
            }

            for (var j = 0; j < listItems.Count; j++)
            {
                if (listItems[j].ShouldRemove)
                {
                    listItems.RemoveAt(j);
                    this.config.Save();
                }
            }

            if (ImGui.Button($"{string.Format(Strings.ActionAddFormat, kind)}###{MemoizedId.Create(uniq: kind)}"))
            {
                listItems.Add(new Trigger(this.config));
            }
        }

        public void Dispose()
        {
            onPresetOpenRequested.Dispose();
        }
    }
}
