using System.Globalization;
using TextToTalk.Localization;
using TextToTalk.Resources;
using Xunit;

namespace TextToTalk.Tests.Localization;

public class LocalizerTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("ja")]
    public void UsesCultureForUiLanguage(string language)
    {
        var localizer = new Localizer(language);

        Assert.Equal(CultureInfo.GetCultureInfo(language), localizer.Culture);
    }

    [Fact]
    public void UsesEnglishResourcesWhenTranslationIsUnavailable()
    {
        _ = new Localizer("pt");

        Assert.Equal("TextToTalk Configuration", Strings.Get("ConfigurationTitle"));
    }

    [Fact]
    public void UsesEnumNameForLocalizedChatTypeResource()
    {
        _ = new Localizer("it");

        Assert.Equal("Dire", Strings.Get("ChatTypeSay"));
    }

}
