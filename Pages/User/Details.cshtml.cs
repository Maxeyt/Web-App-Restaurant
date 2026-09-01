using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.User
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFood.Data.MaxFoodDBContext _context;

        public DetailsModel(MaxFood.Data.MaxFoodDBContext context)
        {
            _context = context;
        }

        public AppUser AppUser { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appuser = await _context.AppUsers.FirstOrDefaultAsync(m => m.AppUserId == id);

            if (appuser is not null)
            {
                AppUser = appuser;

                return Page();
            }

            return NotFound();
        }
    }
}
