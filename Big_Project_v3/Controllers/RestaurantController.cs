using Big_Project_v3.Models;
using Big_Project_v3.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Drawing.Printing;


namespace Big_Project_v3.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ITableDbContext _context;
        private const int PageSize = 2;

        public RestaurantController(ITableDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int Id, int? page)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var restaurant = await _context.Restaurants
                .Include(r => r.Announcements)
                .Include(r => r.Reviews).ThenInclude(rw => rw.User)
                .Include(r => r.Favorites).ThenInclude(f => f.User)
                .Include(r => r.Photos)
                .FirstOrDefaultAsync(m => m.RestaurantId == Id);

            if (restaurant == null)
            {
                return NotFound();
            }

            var isFavorite = userId.HasValue && await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.RestaurantId == Id);

            var announcementContent = restaurant.Announcements.FirstOrDefault()?.Content ?? string.Empty;
            var announcementParagraphs = new List<string>();

            if (!string.IsNullOrEmpty(announcementContent))
            {
                announcementParagraphs = announcementContent
                    .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            int currentPage = page ?? 1;
            var totalReviews = restaurant.Reviews.Count();
            var reviews = restaurant.Reviews
                .OrderByDescending(r => r.ReviewDate)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var viewModel = new RestaurantViewModel
            {
                Restaurant = restaurant,
                MainPhoto = restaurant.Photos.FirstOrDefault(p => p.PhotoType == "首圖")
                     ?? new Photo { PhotoUrl = "https://via.placeholder.com/1200x400?text=No+Image+Available" },
                EnvironmentPhotos = restaurant.Photos.Where(p => p.PhotoType == "餐廳環境").ToList(),
                MenuPhoto = restaurant.Photos.FirstOrDefault(p => p.PhotoType == "菜單")
                     ?? new Photo { PhotoUrl = "https://inline.app/get-printed-menus?cid=-LARHRYjmf_PDvmeH_2U&bid=-LARHRYjmf_PDvmeH_2V" },
                AnnouncementParagraphs = announcementParagraphs,
                Reviews = reviews,
                IsFavorite = isFavorite,  // 判斷使用者是否已收藏
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalReviews / (double)PageSize)
            };

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = (int)Math.Ceiling(totalReviews / (double)PageSize);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite([FromBody] int Id)
        {
            // 取得登入用戶的 ID
            var userId = HttpContext.Session.GetInt32("UserId");  // 從 Session 中取得 userId
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "請先登入會員" });  // 如果未登入
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "使用者不存在" });
            }

            // 檢查該使用者是否已經收藏該餐廳
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.UserId && f.RestaurantId == Id);

            if (existingFavorite != null)
            {
                // 移除收藏
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }
            else
            {
                // 新增收藏
                var newFavorite = new Favorite
                {
                    UserId = user.UserId,
                    RestaurantId = Id,
                    AddedAt = DateTime.Now
                };
                _context.Favorites.Add(newFavorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true });
            }
        }

    }
}
