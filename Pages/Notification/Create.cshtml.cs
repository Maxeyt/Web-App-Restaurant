using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Notification
{
    public class CreateModel : PageModel
    {
        private readonly MaxFood.Data.MaxFoodDBContext _context;

        public CreateModel(MaxFood.Data.MaxFoodDBContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["AppUserId"] = new SelectList(_context.AppUsers, "AppUserId", "PasswordHash");
            return Page();
        }

        [BindProperty]
        public MaxFood.Models.Notification Notification { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Notifications.Add(Notification);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
