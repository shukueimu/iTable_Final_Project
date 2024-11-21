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
                            : TimeSpan.Zero
                    })
                    .ToListAsync();


                // 如果使用者沒有訂位記錄，返回搜尋欄的部分視圖
                if (!reservations.Any())
                {
                    return PartialView("PartialView/_SearchBar");
                }

                // 傳回訂位記錄部分視圖
                return PartialView("PartialView/_MemberFolder/_Reservation", reservations);
            }

            // 如果請求的是其他部分視圖
            if (!string.IsNullOrEmpty(partialViewName))
            {
                return PartialView($"PartialView/_MemberFolder/{partialViewName}");
            }

            // 預設回傳錯誤頁面或空白視圖（避免缺少回傳值路徑）
            return BadRequest("Invalid partial view name.");
        }


    }
}
