using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChainDegree.API.Tests
{
    public class MetricsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MetricsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Get_Metrics_Returns_200OK_With_PrometheusFormat()
        {
            // Act
            var response = await _client.GetAsync("/metrics");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
            Assert.NotEmpty(content);

            // Verify Prometheus metric header signatures & metric names
            Assert.True(content.Contains("# HELP") || content.Contains("# TYPE"), "Response content does not follow Prometheus format.");
            Assert.Contains("chaindegree_worker_queue_length", content);
            Assert.Contains("chaindegree_worker_batches_processed_total", content);
        }
    }
}
