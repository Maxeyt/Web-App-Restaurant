using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Promotion
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DetailsModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public MaxFood.Models.Promotion Promotion { get; set; } = null!;
        public bool IsPromocodeAlreadyUsed { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions.FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null)
            {
                return NotFound();
            }

            Promotion = promotion;

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && promotion.DiscountType == "promocode" && !string.IsNullOrEmpty(promotion.PromocodeValue))
            {
                int userId = int.Parse(userIdStr);

                IsPromocodeAlreadyUsed = await _context.BonusTransactions
                    .Include(bt => bt.Promocode)
                    .AnyAsync(bt => bt.AppUserId == userId
                                    && bt.Type == "promo"
                                    && bt.Promocode != null
                                    && bt.Promocode.PromoCode == promotion.PromocodeValue);
            }

            return Page();
        }
    }
}