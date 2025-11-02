using Moq;
using Scrapper.Services;
using Xunit;

namespace Scrapper.Tests.Services
{
    public class MetricsProcessorIntegrationTests
    {
        private readonly Mock<IMetricsRepository> _mockRepository;
        private readonly MetricsProcessor _processor;

        public MetricsProcessorIntegrationTests()
        {
            _mockRepository = new Mock<IMetricsRepository>();
            _processor = new MetricsProcessor(_mockRepository.Object);
        }

        [Fact]
        public async Task ProcessAndSaveMetricsAsync_ValidNetworkMetric_ParsesCorrectly()
        {
            // Arrange
            var metrics = "node_network_receive_bytes_total{device=\"en0\"} 4.551458554e+09";
            _mockRepository.Setup(x => x.SaveMetricsAsync(It.IsAny<List<Metric>>()))
                          .ReturnsAsync(1);

            // Act
            var result = await _processor.ProcessAndSaveMetricsAsync(metrics);

            // Assert
            Assert.Equal(1, result);
            _mockRepository.Verify(x => x.SaveMetricsAsync(It.Is<List<Metric>>(m => 
                m.Count == 1 &&
                m[0].MetricName == "node_network_receive_bytes_total" &&
                m[0].Device == "en0" &&
                m[0].Value == 4.551458554e+09
            )), Times.Once);
        }
    }
}