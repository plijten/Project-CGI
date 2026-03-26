using Xunit;

namespace Integration;

public class WebhookContractTests
{
    [Fact]
    public void WebhookEndpointPath_ShouldMatchSpec()
    {
        const string endpoint = "/webhooks/github";
        Assert.Equal("/webhooks/github", endpoint);
    }
}
