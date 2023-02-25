using NanoleafAPI;
using System.Net;
using static NanoleafAPI.StateEvent;

namespace NanoleafAPI_Tests
{
    public class ToolsTests
    {
        string[] validTokens = new string[]
        {
            "hioYpUTL6FlsDiClLSk5Lb5z4EgxZqiF",
            "7lOFIqsyqmO8c8H2bYco74z4fK2DmXqK",
            "6uzv0O0Ir1YFWn5jfLjoSmCWpc1g1FRG",
            "Ws5ltXFCD3iVN0sHFJoouRm15ueIetR4",
            "BSkQhH8I44pXz8bAItu3S2gCRVhTT0Wr",
            "cuJ5crqhvQb0TkY4TLcojprLftBx3wCz",
            "JJcQhBvsJNxXiGJ9IjfaRdIvjw93B0A4",
            "YrpEdxTb6N137TEeCIDEk5K1ZFYwWwU0",
            "5g3trAD9GsuTpdrw3INZwmz6mjTpW1qR",
            "DbQjnUsLb3ciX9RU0ppHoN5iLjGFbHuu",
            "rMyMu86xo05ytpnBLJg3nz1zUlwKe56R",
            "JhvfVRcjiU6SvTnXXDr9546uFFkuuEYj",
        };
    
#pragma warning disable CS8625
        string[] invalidTokens = new string[]
        {
            "hioYpUTL6FlsDiClLSk5Lb5z4EgqiF",
            "7lOFIqsyqmO8c8H2bYco74z4fK2DmXK",
            "hioYpUTL6Fl<#iClLSk5Lb5z4EgxZqiF",
            "",
            null
        };
#pragma warning restore CS8625

        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();
        }


        [Test]
        public void TestTokenValidation()
        {
            foreach(var token in validTokens)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Tools.IsTokenValid(token), Is.True, $"Token: {token}");
                });
            }
            foreach (var token in invalidTokens)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Tools.IsTokenValid(token), Is.False, $"Token: {token}");
                });
            }
        }
    }
}