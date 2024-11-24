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
        private const int PageSize = 2; // 每頁顯示的評論數量
        public RestaurantController(ITableDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int id, int? page)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Announcements)                         //串公告
                .Include(r => r.Reviews).ThenInclude(rw => rw.User)    //串評論
                .Include(r => r.Favorites).ThenInclude(f => f.User)    //串收藏
                .Include(r => r.Photos)                                //串照片
                .FirstOrDefaultAsync(m => m.RestaurantId == id); 

            if (restaurant == null)
            {
                return NotFound();
            }

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
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalReviews / (double)PageSize)
            };

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = (int)Math.Ceiling(totalReviews / (double)PageSize);

            return View(viewModel);
        }

        public async Task<IActionResult> GetReviews(int id, int? page)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Reviews).ThenInclude(rw => rw.User)
                .FirstOrDefaultAsync(m => m.RestaurantId == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            int currentPage = page ?? 1;
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
                AnnouncementParagraphs = new List<string>(),  // 初始化為空列表
                Reviews = reviews,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(restaurant.Reviews.Count() / (double)PageSize)
            };

            return PartialView("_ReviewsPartial", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddFavorite(int userId, int restaurantId) 
        { 
            var favorite = new Favorite 
            { 
                UserId = userId, RestaurantId = restaurantId, AddedAt = DateTime.Now
            };
            _context.Favorites.Add(favorite); 
            await _context.SaveChangesAsync(); 
            return Ok(new { success = true }); }

        [HttpPost] 
        public async Task<IActionResult> RemoveFavorite(int userId, int restaurantId) {
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.RestaurantId == restaurantId); 
            if (favorite != null) { _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Ok(new { success = true }); 
            } 
            return BadRequest(new { success = false }); 
        }
    }
}
