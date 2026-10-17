using System.ComponentModel.DataAnnotations;

namespace ToDoList.Models
{
    public class TodoItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Deadline is required")]
        public DateTime Deadline { get; set; }

        public string? FilePath { get; set; }
    }
}
        
      
        
   