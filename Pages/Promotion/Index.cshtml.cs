using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Promotion
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public List<MaxFood.Models.Promotion> PromotionsList { get; set; } = [];

        public async Task OnGetAsync()
        {
            PromotionsList = await _context.Promotions
                .Where(p => p.IsActive && p.ValidFrom <= DateTime.Now && p.ValidTo >= DateTime.Now)
                .OrderBy(p => p.ValidFrom)
                .ToListAsync();
        }
    }
}