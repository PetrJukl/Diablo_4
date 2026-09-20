using Diablo4.WinUI.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Diablo4.WinUI.Tests.Models;

[TestClass]
public class TrackedApplicationCatalogTests
{
    [TestMethod]
    public void AllProcessNames_ContainsDd2WithoutExecutableExtension()
    {
        CollectionAssert.Contains(TrackedApplications.AllProcessNames, "DD2");
        CollectionAssert.DoesNotContain(TrackedApplications.AllProcessNames, "DD2.exe");
    }

    [TestMethod]
    public void AllProcessNames_ContainsGitHubWithoutExecutableExtension()
    {
        CollectionAssert.Contains(TrackedApplications.AllProcessNames, "github");
        CollectionAssert.DoesNotContain(TrackedApplications.AllProcessNames, "github.exe");
    }

    [TestMethod]
    public void AllProcessNames_ContainsChatGptWithoutExecutableExtension()
    {
        CollectionAssert.Contains(TrackedApplications.AllProcessNames, "ChatGPT");
        CollectionAssert.DoesNotContain(TrackedApplications.AllProcessNames, "ChatGPT.exe");
    }

    [TestMethod]
    public void WeekendMotivationGames_ContainsPreviouslyBackgroundTrackedGames()
    {
        var gameNames = TrackedApplications.WeekendMotivationGames
            .Select(application => application.DisplayName)
            .ToArray();

        CollectionAssert.Contains(gameNames, "Dragon Age II");
        CollectionAssert.Contains(gameNames, "Dragon Age: Origins");
        CollectionAssert.Contains(gameNames, "Dragon's Dogma 2");
        CollectionAssert.Contains(gameNames, "The Elder Scrolls IV: Oblivion Remastered");
    }
}
