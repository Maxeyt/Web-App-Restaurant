using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.FAQ
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
            return Page();
        }

        [BindProperty]
        public MaxFood.Models.FAQ FAQ { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.FAQs.Add(FAQ);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
