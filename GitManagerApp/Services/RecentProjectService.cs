using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Controls;

namespace GitManagerApp.Services
{
    public class RecentProjectService
    {
        private readonly string filePath;
        public List<string> Projects { get; private set; } = new();

        public RecentProjectService(string path)
        {
            filePath = path;
            Load();
        }

        public void Save(string projectName)
        {
            Projects.Remove(projectName);
            Projects.Insert(0, projectName);

            if (Projects.Count > 5)
                Projects = Projects.Take(5).ToList();

            File.WriteAllText(filePath, JsonSerializer.Serialize(Projects));
        }

        private void Load()
        {
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Projects = new List<string>();
                return;
            }

            Projects = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }


        public void PopulateComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            foreach (var item in Projects)
                comboBox.Items.Add(item);
        }
    }
}