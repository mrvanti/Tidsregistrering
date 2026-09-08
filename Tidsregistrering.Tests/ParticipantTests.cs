using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class ParticipantTests
{
    [TestMethod]
    public void DisplayBirthDate_UsesMiddleSixDigitsOfTwelveDigitPersonalNumber()
    {
        var participant = new Participant { PersonalNumber = "200101011234" };

        Assert.AreEqual("010101", participant.DisplayBirthDate);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("20010101")]
    [DataRow("20010101-1234")]
    public void DisplayBirthDate_HidesMalformedPersonalNumber(string personalNumber)
    {
        var participant = new Participant { PersonalNumber = personalNumber };

        Assert.AreEqual(string.Empty, participant.DisplayBirthDate);
    }
}
