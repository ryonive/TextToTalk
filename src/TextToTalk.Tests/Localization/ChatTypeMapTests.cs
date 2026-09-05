using System.Collections.Generic;
using Dalamud.Game.Text;
using TextToTalk.Localization;
using Xunit;

namespace TextToTalk.Tests.Localization;

public class ChatTypeMapTests
{
    [Theory]
    [InlineData(XivChatType.Say, XivChatType.GmSay)]
    [InlineData(XivChatType.GmSay, XivChatType.Say)]
    [InlineData(XivChatType.Party, XivChatType.GmParty)]
    [InlineData(XivChatType.GmParty, XivChatType.Party)]
    public void IsChatTypeEnabled_KeepsGmAndBaseChannelsSeparate(
        XivChatType enabledChatType,
        XivChatType receivedChatType)
    {
        Assert.False(ChatTypeMap.IsChatTypeEnabled(
            new List<int> { (int)enabledChatType },
            enableAllChatTypes: false,
            receivedChatType));
    }

    [Fact]
    public void IsChatTypeEnabled_DoesNotEnableUnrelatedChannels()
    {
        Assert.False(ChatTypeMap.IsChatTypeEnabled(
            new List<int> { (int)XivChatType.Say },
            enableAllChatTypes: false,
            XivChatType.Party));
    }

    [Fact]
    public void IsChatTypeEnabled_EnableAllAcceptsAnyChannel()
    {
        Assert.True(ChatTypeMap.IsChatTypeEnabled(
            enabledChatTypes: null,
            enableAllChatTypes: true,
            XivChatType.GmLinkshell8));
    }
}
