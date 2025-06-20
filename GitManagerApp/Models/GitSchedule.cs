using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitManagerApp.Models
{
    public class GitSchedule
    {
        public string? ProjectName { get; set; }
        public required string TargetDir { get; set; }
        public string? BranchName { get; set; }
        public string? CommitMessage { get; set; }
        public DateTime ExecuteAt { get; set; }
        public bool Executed { get; set; }
    }
}
