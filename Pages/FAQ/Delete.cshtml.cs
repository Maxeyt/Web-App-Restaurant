using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.FAQ
{
    public class DeleteModel : PageModel
    {
        private readonly MaxFood.Data.MaxFoodDBContext _context;

        public DeleteModel(MaxFood.Data.MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.FAQ FAQ { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faq = await _context.FAQs.FirstOrDefaultAsync(m => m.FAQId == id);

            if (faq is not null)
            {
                FAQ = faq;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faq = await _context.FAQs.FindAsync(id);
            if (faq != null)
            {
                FAQ = faq;
                _context.FAQs.Remove(FAQ);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
