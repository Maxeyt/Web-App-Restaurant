using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Dish
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DetailsModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public Models.Dish? Dish { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var dish = await _context.Dishes
                .Include(d => d.Category)
                .Include(d => d.Subcategory)
                .Include(d => d.DishVariants)
                .FirstOrDefaultAsync(m => m.DishId == id);

            if (dish == null) return NotFound();

            Dish = dish;
            return Page();
        }

        // API метод для модального окна
        public async Task<IActionResult> OnGetGetDetailsJsonAsync(int id)
        {
            var dish = await _context.Dishes
                .Include(d => d.Category)
                .Include(d => d.Subcategory)
                .Include(d => d.DishVariants)
                .FirstOrDefaultAsync(d => d.DishId == id);

            if (dish == null)
            {
                return new JsonResult(new { error = "Блюдо не найдено" }) { StatusCode = 404 };
            }

            var result = new
            {
                dishId = dish.DishId,
                dishName = dish.DishName,
                shortDescription = dish.ShortDescription,
                fullDescription = dish.FullDescription,
                ingredients = dish.Ingredients,
                nutritionalValue = dish.NutritionalValue,
                calories = dish.Calories,
                preparationTime = dish.PreparationTime,
                categoryName = dish.Category?.CategoryName,
                subcategoryName = dish.Subcategory?.SubcategoryName,
                dishVariants = dish.DishVariants.Select(v => new
                {
                    dishVariantId = v.DishVariantId,
                    sizeName = v.SizeName,
                    price = v.Price,
                    weight = v.Weight,
                    calories = v.Calories,
                    isAvailable = v.IsAvailable
                })
            };

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int dishVariantId, int quantity)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var tempSessionId = HttpContext.Session.GetString("TempCartSessionId");

            var variant = await _context.DishVariants
                .Include(v => v.Dish)
                .FirstOrDefaultAsync(v => v.DishVariantId == dishVariantId);

            if (variant == null)
            {
                return new JsonResult(new { success = false, message = "Блюдо не найдено" });
            }

            Models.Cart cart = null;

            if (!string.IsNullOrEmpty(userIdStr))
            {
                // Авторизованный пользователь
                int userId = int.Parse(userIdStr);
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.AppUserId == userId);

                if (cart == null)
                {
                    cart = new Models.Cart
                    {
                        AppUserId = userId,
                        CreatedDate = DateTime.Now,
                        TotalAmount = 0
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Неавторизованный пользователь (гость)
                if (string.IsNullOrEmpty(tempSessionId))
                {
                    tempSessionId = Guid.NewGuid().ToString();
                    HttpContext.Session.SetString("TempCartSessionId", tempSessionId);
                }

                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.Name == tempSessionId);

                if (cart == null)
                {
                    cart = new Models.Cart
                    {
                        AppUserId = 0,
                        Name = tempSessionId,
                        CreatedDate = DateTime.Now,
                        TotalAmount = 0
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
            }

            // Добавляем или обновляем товар
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.DishVariantId == dishVariantId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    DishVariantId = dishVariantId,
                    Quantity = quantity,
                    PriceAtAdd = variant.Price,
                    SizeName = variant.SizeName,
                    DishName = variant.Dish.DishName
                };
                _context.CartItems.Add(cartItem);
            }

            cart.TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.PriceAtAdd);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}