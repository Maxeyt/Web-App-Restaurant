using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Support
{
    public class DeleteModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DeleteModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.Support Support { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Проверяем авторизацию
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Authorization/Login");

            int userId = int.Parse(userIdString);

            var support = await _context.Supports.FirstOrDefaultAsync(m => m.SupportId == id);
            if (support == null)
            {
                return NotFound();
            }

            // Только свои сообщения можно удалять
            if (support.AppUserId != userId && !User.IsInRole("Admin"))
            {
                return RedirectToPage("./Index");
            }

            Support = support;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var support = await _context.Supports.FindAsync(id);
            if (support != null)
            {
                _context.Supports.Remove(support);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}