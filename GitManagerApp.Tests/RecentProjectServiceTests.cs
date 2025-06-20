using GitManagerApp.Services;
using System.IO;
using Xunit;

namespace GitManagerApp.Tests
{
    public class RecentProjectServiceTests
    {
        [Fact]
        public void Save_ShouldLimitRecentProjectsToFiveAndOrderCorrectly()
        {
            // Arrange
            string path = Path.GetTempFileName();
            var service = new RecentProjectService(path);

            // Act
            for (int i = 0; i < 10; i++)
                service.Save($"Project{i}");

            // Assert
            Assert.Equal(5, service.Projects.Count);
            Assert.Equal("Project9", service.Projects[0]); // 最新が先頭
            Assert.Equal("Project5", service.Projects[4]); // 古い方が最後

            // Cleanup
            File.Delete(path);
        }
    }
}
