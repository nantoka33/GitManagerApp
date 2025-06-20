using GitManagerApp.Models;
using GitManagerApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GitManagerApp.Tests
{
    public class ScheduleManagerTests
    {
        [Fact]
        public void SaveAndLoad_ShouldPreserveScheduleData()
        {
            // Arrange
            string tempPath = Path.GetTempFileName();
            var manager = new ScheduleManager(tempPath);

            var schedules = new List<GitSchedule>
            {
                new GitSchedule
                {
                    ProjectName = "TestProject",
                    TargetDir = "C:\\\\Test",
                    BranchName = "feature/test-branch",
                    CommitMessage = "Initial commit",
                    ExecuteAt = DateTime.Now.AddMinutes(10),
                    Executed = false
                }
            };

            // Act
            manager.Save(schedules);
            var loaded = manager.Load();

            // Assert
            Assert.Single(loaded);
            Assert.Equal("TestProject", loaded[0].ProjectName);
            Assert.Equal("feature/test-branch", loaded[0].BranchName);

            // Cleanup
            File.Delete(tempPath);
        }
    }
}
