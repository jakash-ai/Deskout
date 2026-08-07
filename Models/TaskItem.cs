using System;
using System.Collections.Generic;

namespace Deskout.Models
{
    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public bool IsChecked { get; set; }
        // Days of week this task is active (empty = all days)
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    }
}
