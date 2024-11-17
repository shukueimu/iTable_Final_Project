using Big_Project_v3.Models;
using Big_Project_v3.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace Big_Project_v3.Controllers
{
    public class SearchPageController : Controller // 繼承 Controller 類別，才能使用 MVC 的功能
    {
        private readonly ITableDbContext _context;

        public SearchPageController(ITableDbContext context)
        {
            _context = context;
        }
        // 定義一個 Index 方法，返回視圖
        public IActionResult Index()
        {
            //var now = DateTime.Now;
            //var availableTimes = new List<string>();

            //// 將時間向上調整至下一個整點或半點
            //if (now.Minute > 0 && now.Minute <= 30)
            //{
            //    now = now.AddMinutes(30 - now.Minute); // 調整至下一個半點
            //}
            //else if (now.Minute > 30)
            //{
            //    now = now.AddMinutes(60 - now.Minute); // 調整至下一個整點
            //}

            //// 從調整過的時間開始，生成當日每半小時的時間選項
            //for (var time = now; time < now.Date.AddDays(1); time = time.AddMinutes(30))
            //{
            //    availableTimes.Add(time.ToString("tt h:mm")); // 12 小時制顯示格式，例如 "下午 12:00"
            //}

            //ViewBag.AvailableTimes = availableTimes; // 傳遞可用的時間選項至視圖
            return View();
        }

        [HttpGet] // 指定此方法只接受 POST 請求
        //[Route("SearchPage/SearchRestaurants")]
        public IActionResult SearchRestaurants(string keyword) // 定義搜尋餐廳的方法，接受關鍵字參數
        {
            // 使用 Console.WriteLine 確認接收到的 keyword 是否正確
            Console.WriteLine("搜尋關鍵字: " + keyword);

            // 初始化查詢變數，將其設為 IQueryable，以便延遲查詢到資料庫層級
            //IQueryable<Restaurant> query = _context.Restaurants;

            // 若關鍵字為空白，直接查詢所有餐廳
            var results = string.IsNullOrWhiteSpace(keyword)
                ? _context.Restaurants.ToList()
                : _context.Restaurants
                    .Where(r => r.Name.Contains(keyword))
                    .ToList();
             

            //----------測試回傳數據----------
            Console.WriteLine("搜尋結果數量: " + results.Count);
            foreach (   var restaurant in results)
            {
                Console.WriteLine("符合條件的餐廳名稱: " + restaurant.Name);
            }
            //----------測試回傳數據----------

            // 建立 ViewModel，將篩選結果和關鍵字傳遞給部分檢視
            var viewModel = new SearchRestaurantViewModel
            {
                Restaurants = results,   // 將搜尋結果賦值給 ViewModel 中的 Restaurants 屬性
                SearchKeyword = keyword  // 將關鍵字賦值給 ViewModel 中的 SearchKeyword 屬性
            };

            // 返回部分檢視 `_SearchRestaurant`，並傳遞 ViewModel
            return PartialView("PartialView/_SearchRestaurant", viewModel); // 指定部分檢視路徑並提供資料
        }

        public IActionResult GetAvailableTimes(DateTime selectedDate)
        {
            // 取得當前的日期和時間
            var now = DateTime.Now;

            // 初始化一個字串列表，用於存放可用的時間選項
            var availableTimes = new List<string>();

            // 判斷選擇的日期是否為今天
            if (selectedDate.Date == now.Date)
            {
                // 如果選擇的是今天，則生成從當前時間的下一個整點或半點開始的時間選項

                // 如果當前分鐘數在 1 到 30 之間，將時間調整到下一個半點
                if (now.Minute > 0 && now.Minute <= 30)
                {
                    now = now.AddMinutes(30 - now.Minute); // 調整至下一個半點
                }
                else if (now.Minute > 30)
                {
                    // 如果當前分鐘數大於 30，則將時間調整到下一個整點
                    now = now.AddMinutes(60 - now.Minute); // 調整至下一個整點
                }

                // 從調整過的時間開始，每半小時生成一次時間選項，直到當日結束
                for (var time = now; time < now.Date.AddDays(1); time = time.AddMinutes(30))
                {
                    // 將時間格式化為 12 小時制（上午/下午），並加入列表中
                    availableTimes.Add(time.ToString("tt h:mm"));
                }
            }
            else
            {
                // 如果選擇的是未來日期（非今天），則生成完整的 00:00 到 23:30 的時間選項

                // 設定一天的起始時間為選擇日期的 00:00
                var startOfDay = selectedDate.Date;

                // 從 00:00 開始，每半小時生成一次時間選項，直到 23:30
                for (var time = startOfDay; time < startOfDay.AddDays(1); time = time.AddMinutes(30))
                {
                    // 將時間格式化為 12 小時制（上午/下午），並加入列表中
                    availableTimes.Add(time.ToString("tt h:mm"));
                }
            }

            // 返回 JSON 格式的時間選項列表，供前端使用
            return Json(availableTimes);
        }
    }
}