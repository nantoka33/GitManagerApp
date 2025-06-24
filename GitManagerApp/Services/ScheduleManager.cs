using GitManagerApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

namespace GitManagerApp.Services
{
    public class ScheduleManager
    {
        private readonly string scheduleFilePath;

        public ScheduleManager(string path)
        {
            scheduleFilePath = path;
        }

        public List<GitSchedule> Load()
        {
            if (!File.Exists(scheduleFilePath)) return new();
            var json = File.ReadAllText(scheduleFilePath);
            return JsonSerializer.Deserialize<List<GitSchedule>>(json) ?? new();
        }

        public void Save(List<GitSchedule> schedules)
        {
            var json = JsonSerializer.Serialize(schedules);
            File.WriteAllText(scheduleFilePath, json);
        }

        public void RemoveExecuted(ref List<GitSchedule> schedules)
        {
            schedules = schedules.Where(s => s.Executed).ToList();
            Save(schedules);
        }

        public void RemoveSchedule(List<GitSchedule> schedules, GitSchedule target)
        {
            schedules.Remove(target);
            Save(schedules);
        }
    }
}