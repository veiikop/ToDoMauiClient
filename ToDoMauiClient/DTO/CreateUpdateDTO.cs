using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoMauiClient.DTO
{
    public class CreateTodoListDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateTodoItemDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public int TodoListId { get; set; }
    }

    public class UpdateTodoItemDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public Priority? Priority { get; set; }
        public bool? IsCompleted { get; set; }
    }
}
