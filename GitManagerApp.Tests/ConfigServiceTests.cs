using GitManagerApp.Models;
using GitManagerApp.Services;
using System.IO;
using Xunit;

namespace GitManagerApp.Tests
{
    public class ConfigServiceTests
    {
        [Fact]
        public void SaveAndLoad_ShouldPreserveScheduleFilePath()
        {
            // Arrange
            string path = Path.GetTempFileName();
            var service = new ConfigService(path);
            var original = new AppConfig { ScheduleFilePath = "schedules.json" };

            // Act
            service.Save(original);
            var loaded = service.Load();

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal("schedules.json", loaded?.ScheduleFilePath);

            // Cleanup
            File.Delete(path);
        }
    }
}
