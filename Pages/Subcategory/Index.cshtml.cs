using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Subcategory
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        public IndexModel(MaxFoodDBContext context) => _context = context;

        public IList<MaxFood.Models.Subcategory> SubcategoriesList { get; set; } = [];

        public async Task OnGetAsync()
        {
            SubcategoriesList = await _context.Subcategories.ToListAsync();
        }
    }
}