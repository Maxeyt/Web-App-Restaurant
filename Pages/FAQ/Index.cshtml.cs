using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.FAQ
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public IList<MaxFood.Models.FAQ> FAQs { get; set; } = new List<MaxFood.Models.FAQ>();

        public async Task OnGetAsync()
        {
            FAQs = await _context.FAQs
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();
        }
    }
}