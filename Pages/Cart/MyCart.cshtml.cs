using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using Models = MaxFood.Models;

namespace MaxFood.Pages.Cart
{
    public class MyCartModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public MyCartModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
        public decimal Total { get; set; }
        public bool IsAuthenticated { get; set; }

        public class CartItemDto
        {
            public int CartItemId { get; set; }
            public int DishVariantId { get; set; }
            public int DishId { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string SubcategoryName { get; set; } = string.Empty;
            public string DishName { get; set; } = string.Empty;
            public string? ShortDescription { get; set; }
            public string SizeName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public double? Weight { get; set; }
        }

        public async Task OnGetAsync()
        {
            var userId = GetUserId();
            IsAuthenticated = userId != null;

            if (userId == null)
            {
                CartItems = new List<CartItemDto>();
                Total = 0;
                return;
            }

            var cart = await _context.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (cart == null)
            {
                CartItems = new List<CartItemDto>();
                Total = 0;
                return;
            }

            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Where(c => c.CartId == cart.CartId)
                .Include(c => c.DishVariant)
                    .ThenInclude(v => v.Dish)
                        .ThenInclude(d => d.Category)
                .Include(c => c.DishVariant)
                    .ThenInclude(v => v.Dish)
                        .ThenInclude(d => d.Subcategory)
                .OrderBy(c => c.CartItemId)
                .ToListAsync();

            CartItems = cartItems.Select(c => new CartItemDto
            {
                CartItemId = c.CartItemId,
                DishVariantId = c.DishVariantId,
                DishId = c.DishVariant?.Dish?.DishId ?? 0,
                CategoryId = c.DishVariant?.Dish?.CategoryId ?? 0,
                CategoryName = c.DishVariant?.Dish?.Category?.CategoryName ?? "Без категории",
                SubcategoryName = c.DishVariant?.Dish?.Subcategory?.SubcategoryName ?? "",
                DishName = c.DishVariant?.Dish?.DishName ?? "Блюдо",
                ShortDescription = c.DishVariant?.Dish?.ShortDescription,
                SizeName = c.DishVariant?.SizeName ?? "Стандарт",
                Price = c.DishVariant?.Price ?? 0,
                Quantity = c.Quantity,
                Weight = c.DishVariant?.Weight != null ? (double?)c.DishVariant.Weight : null
            }).ToList();

            Total = CartItems.Sum(i => i.Price * i.Quantity);
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAddToCartAsync([FromBody] AddToCartRequest request)
        {
            if (request == null || request.DishVariantId <= 0 || request.Quantity <= 0)
                return new JsonResult(new { success = false, message = "Неверный запрос" });

            var userId = GetUserId();
            if (userId == null)
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var variant = await _context.DishVariants
                .Include(v => v.Dish)
                .FirstOrDefaultAsync(v => v.DishVariantId == request.DishVariantId && v.IsAvailable);

            if (variant == null)
                return new JsonResult(new { success = false, message = "Блюдо не найдено или недоступно" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (cart == null)
            {
                cart = new Models.Cart
                {
                    AppUserId = userId.Value,
                    CreatedDate = DateTime.Now,
                    TotalAmount = 0
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.DishVariantId == request.DishVariantId);
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                _context.CartItems.Add(new Models.CartItem
                {
                    CartId = cart.CartId,
                    DishVariantId = request.DishVariantId,
                    Quantity = request.Quantity,
                    PriceAtAdd = variant.Price,
                    SizeName = variant.SizeName,
                    DishName = variant.Dish?.DishName ?? "Блюдо"
                });
            }

            cart.TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.PriceAtAdd);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostUpdateQuantityAsync([FromBody] UpdateQuantityRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (cart == null)
                return new JsonResult(new { success = false, message = "Корзина не найдена" });

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CartItemId == request.CartItemId && c.CartId == cart.CartId);

            if (cartItem == null)
                return new JsonResult(new { success = false, message = "Позиция не найдена" });

            if (request.Quantity < 1 || request.Quantity > 99)
                return new JsonResult(new { success = false, message = "Недопустимое количество" });

            cartItem.Quantity = request.Quantity;
            await _context.SaveChangesAsync();

            var total = await _context.CartItems
                .Where(c => c.CartId == cart.CartId)
                .Include(c => c.DishVariant)
                .SumAsync(c => c.Quantity * (c.DishVariant != null ? c.DishVariant.Price : 0));

            cart.TotalAmount = total;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, total });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostRemoveItemAsync([FromBody] RemoveItemRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (cart == null)
                return new JsonResult(new { success = false, message = "Корзина не найдена" });

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CartItemId == request.CartItemId && c.CartId == cart.CartId);

            if (cartItem == null)
                return new JsonResult(new { success = false, message = "Позиция не найдена" });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var total = await _context.CartItems
                .Where(c => c.CartId == cart.CartId)
                .Include(c => c.DishVariant)
                .SumAsync(c => c.Quantity * (c.DishVariant != null ? c.DishVariant.Price : 0));

            cart.TotalAmount = total;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, total });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostClearCartAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (cart == null)
                return new JsonResult(new { success = false, message = "Корзина не найдена" });

            var itemsToDelete = await _context.CartItems.Where(ci => ci.CartId == cart.CartId).ToListAsync();
            if (itemsToDelete.Any())
            {
                _context.CartItems.RemoveRange(itemsToDelete);
                cart.TotalAmount = 0;
                await _context.SaveChangesAsync();
            }

            return new JsonResult(new { success = true, total = 0 });
        }

        public async Task<IActionResult> OnGetCountAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return new JsonResult(new { count = 0 });

            var cart = await _context.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AppUserId == userId);

            if (cart == null)
                return new JsonResult(new { count = 0 });

            var count = await _context.CartItems
                .Where(c => c.CartId == cart.CartId)
                .SumAsync(c => c.Quantity);

            return new JsonResult(new { count });
        }

        private int? GetUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
                return userId;
            return null;
        }

        public class AddToCartRequest
        {
            public int DishVariantId { get; set; }
            public int Quantity { get; set; }
        }

        public class UpdateQuantityRequest
        {
            public int CartItemId { get; set; }
            public int Quantity { get; set; }
        }

        public class RemoveItemRequest
        {
            public int CartItemId { get; set; }
        }
    }
}