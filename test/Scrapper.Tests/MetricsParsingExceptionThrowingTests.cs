using System;
using System.Reflection;
using System.Globalization;
using FluentAssertions;
using Moq; 
using Scrapper.Domain; 
using Scrapper.Services; 
using Scrapper.Data; 
using Xunit;
using static Scrapper.Domain.MetricsCollectorService;

namespace Scrapper.Tests
{
    public class MetricsProcessorTests
    {
        private readonly MetricsProcessor _processor;
        private readonly Mock<IMetricsRepository> _mockRepo;
        private readonly DateTime _testTimestamp = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        public MetricsProcessorTests()
        {
            _mockRepo = new Mock<IMetricsRepository>();
            _processor = new MetricsProcessor(_mockRepo.Object);
        }

        // Хелпер-метод для виклику приватного методу ParseMetricLine
        private Metric InvokeParseMetricLine(string line)
        {
            var parameterTypes = new Type[] { typeof(string), typeof(DateTime) };
            var parseMetricLineMethod = typeof(MetricsProcessor).GetMethod("ParseMetricLine", 
                BindingFlags.NonPublic | BindingFlags.Instance, 
                null,
                new Type[] { typeof(string), typeof(DateTime) },
                null);
            
            if (parseMetricLineMethod == null)
            {
                throw new InvalidOperationException("ParseMetricLine method not found. Check signature.");
            }

            // Виклик з двома аргументами (line та _testTimestamp)
            return (Metric)parseMetricLineMethod.Invoke(_processor, new object[] { line, _testTimestamp })!;
        }
        
        [Fact]
        public void ParseMetricLine_ShouldReturnUnknownDevice_WhenNoLabelsArePresent()
        {
            // Arrange
            const string line = "node_up 1";

            // Act
            var result = InvokeParseMetricLine(line);

            // Assert
            result.Should().NotBeNull();
            result.MetricName.Should().Be("node_up");
            result.Value.Should().Be(1.0);
            result.Device.Should().Be("unknown"); 
            result.Timestamp.Should().Be(_testTimestamp);
        }

        [Fact]
        public void ParseMetricLine_ThrowsException_WhenLineFormatIsInvalid()
        {
            // Arrange 
            const string line = "node_up {device=\"eth0\"} 1 extra_part";

            // Act
            Action act = () => InvokeParseMetricLine(line);

            // Assert
            act.Should().Throw<TargetInvocationException>()
               .WithInnerException<MetricParseException>()
               .WithMessage("*two parts when split by space*");
        }
    }
}
