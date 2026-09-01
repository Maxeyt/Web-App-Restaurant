using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.User
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
        ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
        ViewData["SelectedPickupPointId"] = new SelectList(_context.PointPickups, "PointPickupId", "Address");
        ViewData["ProfileId"] = new SelectList(_context.UserProfiles, "ProfileId", "Email");
            return Page();
        }

        [BindProperty]
        public AppUser AppUser { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.AppUsers.Add(AppUser);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
