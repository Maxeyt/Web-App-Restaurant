using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.PointPickup
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public List<MaxFood.Models.PointPickup> PointPickupList { get; set; } = new();

        public async Task OnGetAsync()
        {
            PointPickupList = await _context.PointPickups
                .Where(p => p.IsActive)
                .OrderBy(p => p.PointName)
                .ToListAsync();
        }
    }
}