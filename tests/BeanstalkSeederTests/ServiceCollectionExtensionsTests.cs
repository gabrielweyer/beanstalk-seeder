using Amazon;
using BeanstalkSeeder.Configuration;
using Xunit;

namespace BeanstalkSeederTests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void GivenValidSqsQueueUrl_WhenGetRegionEndpoint_ThenExpectedRegionEndpoint()
        {
            // Arrange

            const string sqsQueueUrl = "https://sqs.ap-southeast-2.amazonaws.com/375985941080/dev-gabriel";
            var expectedRegionEndpoint = RegionEndpoint.APSoutheast2;

            // Act

            var actualRegionEndpoint = ServiceCollectionExtensions.GetRegionEndpoint(sqsQueueUrl);

            // Assert

            Assert.Equal(expectedRegionEndpoint, actualRegionEndpoint);
        }
    }
}