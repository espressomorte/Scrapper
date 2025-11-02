using Moq;
using Scrapper.Services;
using Xunit;

namespace Scrapper.Tests.Services
{
    public class MetricsProcessorTests
    {
        private readonly Mock<IMetricsRepository> _mockRepository;
        private readonly MetricsProcessor _processor;

        public MetricsProcessorTests()
        {
            _mockRepository = new Mock<IMetricsRepository>();
            _processor = new MetricsProcessor(_mockRepository.Object);
        }

        // Basic test to verify setup works
        [Fact]
        public void Constructor_WithRepository_CreatesInstance()
        {
            // Arrange & Act
            var processor = new MetricsProcessor(_mockRepository.Object);

            // Assert
            Assert.NotNull(processor);
        }
    }
}