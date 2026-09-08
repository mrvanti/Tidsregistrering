using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class AdminSessionTests
{
    [TestMethod]
    public void SignIn_RejectsWrongPinAndExpiresAfterThreeMinutes()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var session = new AdminSession("1234", () => now);

        Assert.IsFalse(session.TrySignIn("0000"));
        Assert.IsFalse(session.IsActive);
        Assert.IsTrue(session.TrySignIn("1234"));
        now = now.AddMinutes(2).AddSeconds(59);
        Assert.IsTrue(session.IsActive);
        now = now.AddSeconds(1);
        Assert.IsFalse(session.IsActive);
    }

    [TestMethod]
    public void RecordActivity_ExtendsActiveAdminSession()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var session = new AdminSession("1234", () => now);
        session.TrySignIn("1234");
        now = now.AddMinutes(2);
        session.RecordActivity();
        now = now.AddMinutes(2);

        Assert.IsTrue(session.IsActive);
        session.End();
        Assert.IsFalse(session.IsActive);
    }
}
