using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Support
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public CreateModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["AppUserId"] = new SelectList(_context.AppUsers, "AppUserId", "PasswordHash");
            return Page();
        }

        [BindProperty]
        public MaxFood.Models.Support Support { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Supports.Add(Support);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}