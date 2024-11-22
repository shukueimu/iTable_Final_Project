using Big_Project_v3.Models;
using Big_Project_v3.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Big_Project_v3.Controllers
{
    public class MemberController : Controller
    {
        private readonly ITableDbContext _context;

        public MemberController(ITableDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            // 查詢用戶資料
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserViewModel
                {
                    UserID = u.UserId,
                    UserName = u.UserName,
                    Name = u.Name,
                    Reservations = _context.Reservations
                        .Where(r => r.UserId == userId)
                        .Select(r => new ReservationViewModel
                        {
                            Name = r.Restaurant != null ? r.Restaurant.Name : "未知餐廳",
                            RestaurantID = r.RestaurantId ?? 0,
                            ReservationStatus = r.ReservationStatus,
                            NumAdults = r.NumAdults ?? 0,
                            NumChildren = r.NumChildren ?? 0,
                            ReservationDate = r.ReservationDate.HasValue
                                ? r.ReservationDate.Value.ToDateTime(TimeOnly.MinValue)
                                : DateTime.MinValue,
                            ReservationTime = r.ReservationTime.HasValue
                                ? r.ReservationTime.Value.ToTimeSpan()
                                : TimeSpan.Zero
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            return View(user);
        }



        [HttpGet]
        public async Task<IActionResult> LoadPartialView(string partialViewName)
        {
            // 從 Session 取得目前使用者的 UserId
            var userId = HttpContext.Session.GetInt32("UserId");

            // 如果未登錄，重定向到登入頁面
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            if (partialViewName == "_Reservation")
            {
                var reservations = await _context.Reservations
                    .Include(r => r.Restaurant) // 手動載入與餐廳的關聯資料
                    .Where(r => r.UserId == userId)
                    .Select(r => new ReservationViewModel
                    {
                        RestaurantID = r.RestaurantId ?? 0,
                        Name = r.Restaurant != null ? r.Restaurant.Name : "未知餐廳",
                        ReservationStatus = r.ReservationStatus,
                        NumAdults = r.NumAdults ?? 0,
                        NumChildren = r.NumChildren ?? 0,
                        ReservationDate = r.ReservationDate.HasValue
                            ? r.ReservationDate.Value.ToDateTime(TimeOnly.MinValue)
                            : DateTime.MinValue,
                        ReservationTime = r.ReservationTime.HasValue
                            ? r.ReservationTime.Value.ToTimeSpan()
                            : TimeSpan.Zero,
                        PhotoUrl = _context.Photos              // 從 Photos 表中查詢餐廳圖片
                            .Where(p => p.RestaurantId == r.RestaurantId && p.PhotoType == "LOGO")
                            .Select(p => p.PhotoUrl)
                            .FirstOrDefault(),                  // 僅取第一張圖片
                    })
                    .ToListAsync();

                // 如果使用者沒有訂位記錄，返回搜尋欄的部分視圖
                if (!reservations.Any())
                {
                    return Content("<h1>您目前沒有任何訂位記錄</h1>", "text/html");
                }

                // 傳回訂位記錄部分視圖
                return PartialView("PartialView/_MemberFolder/_Reservation", reservations);
            }

            if (partialViewName == "_FavoriteRestaurants")
            {
                var favoriteRestaurants = await _context.Favorites
                    .Where(f => f.UserId == userId) // 過濾目前的使用者
                    .Include(f => f.Restaurant)    // 載入關聯的餐廳資料
                    .Select(f => new FavoriteViewModel
                    {
                        Name = f.Restaurant.Name,                // 餐廳名稱
                        Description = f.Restaurant.Description, // 餐廳描述
                        PhotoUrl = _context.Photos              // 從 Photos 表中查詢餐廳圖片
                            .Where(p => p.RestaurantId == f.RestaurantId && p.PhotoType == "LOGO")
                            .Select(p => p.PhotoUrl)
                            .FirstOrDefault(),                  // 僅取第一張圖片
                        AddedAt = f.AddedAt.HasValue ? f.AddedAt.Value.ToString("yyyy/MM/dd") : "未知日期",
                        AverageRating = f.Restaurant.AverageRating ?? 0
                    })
                    .ToListAsync();


                // 如果使用者沒有收藏任何餐廳，回傳提示訊息
                if (!favoriteRestaurants.Any())
                {
                    return Content("<h1>您目前沒有收藏任何餐廳</h1>", "text/html");
                }

                // 傳回收藏餐廳部分視圖
                return PartialView("PartialView/_MemberFolder/_FavoriteRestaurants", favoriteRestaurants);
            }

            // 如果請求的是其他部分視圖
            if (!string.IsNullOrEmpty(partialViewName))
            {
                return PartialView($"PartialView/_MemberFolder/{partialViewName}");
            }

            // 預設回傳錯誤頁面或空白視圖（避免缺少回傳值路徑）
            return BadRequest("Invalid partial view name.");
        }


        [HttpGet]
        public async Task<IActionResult> LoadFavoriteRestaurants()
        {
            // 從 Session 中取得 UserId
            var userId = HttpContext.Session.GetInt32("UserId");

            // 如果未登入，重定向到登入頁
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            // 查詢目前使用者的收藏餐廳
            var favoriteRestaurants = await _context.Favorites
                .Where(f => f.UserId == userId) // 過濾目前的使用者
                .Include(f => f.Restaurant)    // 顯式載入關聯的餐廳資料
                .Select(f => new FavoriteViewModel
                {
                    Name = f.Restaurant.Name,
                    AddedAt = f.AddedAt.HasValue ? f.AddedAt.Value.ToString("yyyy/MM/dd") : "未知日期",
                    AverageRating = f.Restaurant.AverageRating ?? 0, // 餐廳評分
                    Description = f.Restaurant.Description // 餐廳描述
                })
                .ToListAsync();

            var test = await _context.Favorites
    .Where(f => f.UserId == userId)
    .Include(f => f.Restaurant)
    .ToListAsync();

            foreach (var favorite in test)
            {
                Console.WriteLine($"餐廳名稱: {favorite.Restaurant?.Name}, 描述: {favorite.Restaurant?.Description}");
            }


            foreach (var item in favoriteRestaurants)
            {
                Console.WriteLine($"餐廳名稱: {item.Name}, 評分: {item.AverageRating}, 收藏日期: {item.AddedAt}");
            }


            // 如果沒有收藏餐廳，顯示訊息
            if (!favoriteRestaurants.Any())
            {
                return Content("<h1>您目前沒有收藏任何餐廳</h1>", "text/html");
            }

            // 返回收藏餐廳的部分視圖
            return PartialView("PartialView/_MemberFolder/_FavoriteRestaurants", favoriteRestaurants);
        }

        [HttpGet]
        public async Task<IActionResult> TestFavoriteDescriptions()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return Content("尚未登入，無法測試。");
            }

            // 查詢收藏餐廳並載入關聯的餐廳資料
            var test = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Restaurant)
                .Select(f => new
                {
                    FavoriteID = f.FavoriteId,
                    RestaurantName = f.Restaurant.Name,
                    RestaurantDescription = f.Restaurant.Description,
                    AddedAt = f.AddedAt
                })
                .ToListAsync();
            Console.WriteLine($"查詢結果：找到 {test.Count} 筆記錄");
            foreach (var item in test)
            {
                Console.WriteLine($"FavoriteID: {item.FavoriteID}, 餐廳名稱: {item.RestaurantName}, 描述: {item.RestaurantDescription}");
            }
            // 將結果轉換成 JSON 返回
            return Json(test);
        }

    }
}
