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
            // 從 Session 中取得 UserId
            var userId = HttpContext.Session.GetInt32("UserId");

            // 如果未登入，重定向到登入頁
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            // 根據 UserId 查詢用戶資料
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserViewModel
                {
                    UserID = u.UserId,
                    UserName = u.UserName,
                    Name = u.Name
                })
                .FirstOrDefaultAsync();

            // 如果查無資料，重新導向登入
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            // 將用戶資料傳遞到視圖
            return View(user);
        }
    }
}
