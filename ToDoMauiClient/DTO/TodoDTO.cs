using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoMauiClient.DTO
{
    public class TodoListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TodoItemDTO> Items { get; set; } = new();
    }

    public class TodoItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public int TodoListId { get; set; }
    }

    public enum Priority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}
