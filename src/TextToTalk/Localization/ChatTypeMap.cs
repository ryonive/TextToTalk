using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using TextToTalk.GameEnums;
using TextToTalk.Resources;

namespace TextToTalk.Localization
{
    internal static class ChatTypeMap
    {
        private static readonly Dictionary<(ClientLanguage Language, int ChatType), string> LogFilterNames = new();

        internal static readonly IReadOnlyDictionary<int, uint> AddonRowIds =
            new Dictionary<int, uint>
            {
                [(int)XivChatType.Urgent] = 2251,
                [(int)XivChatType.Notice] = 12874,
            };

        internal static readonly IReadOnlyDictionary<XivChatType, XivChatType> GmBaseChatTypes =
            new Dictionary<XivChatType, XivChatType>
            {
                [XivChatType.GmTell] = XivChatType.TellIncoming,
                [XivChatType.GmSay] = XivChatType.Say,
                [XivChatType.GmShout] = XivChatType.Shout,
                [XivChatType.GmYell] = XivChatType.Yell,
                [XivChatType.GmParty] = XivChatType.Party,
                [XivChatType.GmFreeCompany] = XivChatType.FreeCompany,
                [XivChatType.GmLinkshell1] = XivChatType.Ls1,
                [XivChatType.GmLinkshell2] = XivChatType.Ls2,
                [XivChatType.GmLinkshell3] = XivChatType.Ls3,
                [XivChatType.GmLinkshell4] = XivChatType.Ls4,
                [XivChatType.GmLinkshell5] = XivChatType.Ls5,
                [XivChatType.GmLinkshell6] = XivChatType.Ls6,
                [XivChatType.GmLinkshell7] = XivChatType.Ls7,
                [XivChatType.GmLinkshell8] = XivChatType.Ls8,
                [XivChatType.GmNoviceNetwork] = XivChatType.NoviceNetwork,
             };

        internal static bool IsChatTypeEnabled(IList<int>? enabledChatTypes, bool enableAllChatTypes, XivChatType chatType)
        {
            return enableAllChatTypes || enabledChatTypes?.Contains((int)chatType) == true;
        }

        internal static bool TryGetResourceName(int chatType, out string name)
        {
            var enumName = Enum.GetName(typeof(XivChatType), (ushort)chatType)
                           ?? Enum.GetName(typeof(AdditionalChatType), chatType);
            name = enumName is null ? string.Empty : Strings.Get($"ChatType{enumName}");
            return name.Length > 0 && !name.StartsWith("[", StringComparison.Ordinal);
        }

        internal static bool TryGetAdditionalChannelName(int chatType, out string name)
        {
            name = chatType switch
            {
                (int)XivChatType.Debug => Strings.ChatTypeDebug,
                (int)XivChatType.CrossParty => Strings.ChatTypeCrossParty,
                (int)XivChatType.Orchestrion => Strings.ChatTypeOrchestrion,
                (int)XivChatType.TellIncoming => Strings.ChatTypeTellIncoming,
                (int)XivChatType.TellOutgoing => Strings.ChatTypeTellOutgoing,
                (int)AdditionalChatType.EnemyDefeatedByYou => Strings.ChatTypeEnemyDefeatedByYou,
                _ => string.Empty,
            };
            return name.Length > 0;
        }

        internal static bool TryGetLogFilterName(IDataManager data, ClientLanguage language, int chatType, out string name)
        {
            var key = (language, chatType);
            if (!LogFilterNames.TryGetValue(key, out name!))
            {
                name = string.Empty;
                if (data.GetExcelSheet<LogFilter>(language)?.FirstOrDefault(row =>
                        (int)row.LogKind == chatType && !string.IsNullOrWhiteSpace(row.Name.ToString())) is { } row)
                {
                    name = row.Name.ToString();
                }

                LogFilterNames[key] = name;
            }

            return !string.IsNullOrWhiteSpace(name);
        }

        internal static bool TryGetAddonName(IDataManager data, ClientLanguage language, int chatType, out string name)
        {
            name = string.Empty;
            return AddonRowIds.TryGetValue(chatType, out var rowId)
                   && data.GetExcelSheet<Addon>(language)?.TryGetRow(rowId, out var row) == true
                   && !string.IsNullOrWhiteSpace(name = row.Text.ToString());
        }
    }
}
