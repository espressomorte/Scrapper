using Microsoft.AspNetCore.Mvc;
using Moq;
using Scrapper.Controllers;
using Scrapper.Data;
using Scrapper.Domain;
using Xunit;

namespace Scrapper.Tests.Controllers
{
    public class MetricsControllerTests
    {
        private readonly Mock<IMetricsRepository> _mockRepository;
        private readonly MetricsController _controller;

        public MetricsControllerTests()
        {
            _mockRepository = new Mock<IMetricsRepository>();
            _controller = new MetricsController(_mockRepository.Object);
        }

        [Fact]
        public async Task GetMetricsData_ReturnsOk_WithMetrics()
        {
            // Arrange
            var mockData = new List<Metric>
            {
                new Metric { MetricName = "cpu_usage", Device = "utun8", Value = 12.5, Timestamp = DateTime.UtcNow }
            };

            _mockRepository.Setup(r => r.GetMetricsAsync(10, "utun8"))
                           .ReturnsAsync(mockData);

            // Act
            var result = await _controller.GetMetricsData(10, "utun8");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedMetrics = Assert.IsAssignableFrom<IEnumerable<Metric>>(okResult.Value);
            Assert.Single(returnedMetrics);
        }

        [Fact]
        public async Task GetMetricsData_ReturnsOk_WithEmptyList()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetMetricsAsync(50, null))
                           .ReturnsAsync(new List<Metric>());

            // Act
            var result = await _controller.GetMetricsData(50, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedMetrics = Assert.IsAssignableFrom<IEnumerable<Metric>>(okResult.Value);
            Assert.Empty(returnedMetrics);
        }

        [Fact]
        public async Task GetMetricsData_CallsRepository_WithCorrectParameters()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetMetricsAsync(It.IsAny<int>(), It.IsAny<string?>()))
                           .ReturnsAsync(new List<Metric>());

            // Act
            await _controller.GetMetricsData(5, "eth0");

            // Assert
            _mockRepository.Verify(r => r.GetMetricsAsync(5, "eth0"), Times.Once);
        }

        [Fact]
        public async Task GetMetricsData_WhenRepositoryThrows_Returns500()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetMetricsAsync(It.IsAny<int>(), It.IsAny<string?>()))
                           .ThrowsAsync(new Exception("DB failure"));

            // Act
            var result = await _controller.GetMetricsData(20, "wlan0");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
