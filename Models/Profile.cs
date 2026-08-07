using System.Collections.Generic;

namespace Deskout.Models
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public List<TaskItem> Tasks { get; set; } = new();
    }
}
