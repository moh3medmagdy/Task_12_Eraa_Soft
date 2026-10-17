using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Controllers
{
    public class TodoItemController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TodoItemController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

       
        public async Task<IActionResult> Index(DateTime? searchDate, int page = 1)
        {
            int pageSize = 5; 

            var tasksQuery = _context.TodoItems.AsQueryable();

            
            if (searchDate.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.Deadline.Date == searchDate.Value.Date);
            }

            var totalItems = await tasksQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

          
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

           
            var tasks = await tasksQuery
                .OrderBy(t => t.Deadline)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");

            return View(tasks);
        }

        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoItem todoItem, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                if (file != null && file.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    todoItem.FilePath = "/uploads/" + uniqueFileName;
                }

                _context.Add(todoItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(todoItem);
        }

     
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();

            return View(todoItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TodoItem todoItem, IFormFile? file)
        {
            if (id != todoItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                      
                        if (!string.IsNullOrEmpty(todoItem.FilePath))
                        {
                            string oldPath = Path.Combine(_env.WebRootPath, todoItem.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        todoItem.FilePath = "/uploads/" + uniqueFileName;
                    }

                    _context.Update(todoItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TodoItems.Any(e => e.Id == todoItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(todoItem);
        }

        // POST: TodoItem/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem != null)
            {
                if (!string.IsNullOrEmpty(todoItem.FilePath))
                {
                    string fullPath = Path.Combine(_env.WebRootPath, todoItem.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.TodoItems.Remove(todoItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

      
        public IActionResult DownloadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return NotFound();

            var absolutePath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));

            if (!System.IO.File.Exists(absolutePath))
                return NotFound("File not found on server.");

            var fileName = Path.GetFileName(absolutePath);
            var contentType = "application/octet-stream";

            return PhysicalFile(absolutePath, contentType, fileName);
        }
    }
}