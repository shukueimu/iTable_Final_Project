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
                    .Where(r => r.Name.Contains(keyword) || r.Address.Contains(keyword) || r.Description.Contains(keyword))
                    .ToList();


            //----------測試回傳數據----------
            Console.WriteLine("搜尋結果數量: " + results.Count);
            foreach (var restaurant in results)
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

        [HttpPost]
        public IActionResult SortByRating([FromBody] SortRequest request)
        {
            // 篩選條件：基於關鍵字和選中的地區
            var sortedRestaurants = _context.Restaurants
                .Where(r =>
                    (string.IsNullOrEmpty(request.Keyword) || r.Name.Contains(request.Keyword) || r.Address.Contains(request.Keyword)) &&
                    (request.SelectedDistricts == null || !request.SelectedDistricts.Any() || request.SelectedDistricts.Any(d => r.Address != null && r.Address.Contains(d))))
                .OrderByDescending(r => r.AverageRating ?? 0) // 按評分排序
                .Select(r => new SearchRestaurantViewModel
                {
                    Id = r.RestaurantId,
                    Name = r.Name,
                    AverageRating = r.AverageRating ?? 0,
                    Address = r.Address,
                    Description = r.Description
                })
                .ToList();

            // 測試回傳數據
            Console.WriteLine($"排序後的餐廳數量: {sortedRestaurants.Count}");
            foreach (var restaurant in sortedRestaurants)
            {
                Console.WriteLine($"餐廳名稱: {restaurant.Name}, 地址: {restaurant.Address}, 評分: {restaurant.AverageRating}");
            }

            return PartialView("PartialView/_SearchRestaurantFolder/_SearchRestaurantSorting", sortedRestaurants);
        }

        // 定義請求的 DTO
        public class SortRequest
        {
            public string Keyword { get; set; } // 關鍵字（可選）
            public List<string> SelectedDistricts { get; set; } // 勾選的地區
        }


        [HttpPost]
        public IActionResult FilterByDistrict([FromBody] List<string> selectedDistricts)
        {
            if (selectedDistricts == null || !selectedDistricts.Any())
            {
                // 如果未選擇任何地區，返回空的部分視圖
                return PartialView("PartialView/_SearchRestaurantFolder/_SearchDistrictEmpty");
            }

            // 測試輸出接收到的地區
            Console.WriteLine("接收到的地區名稱: " + string.Join(", ", selectedDistricts));

            // 篩選餐廳地址包含地區名稱的餐廳
            var filteredRestaurants = _context.Restaurants
                .Where(r => selectedDistricts.Any(d => r.Address != null && r.Address.Contains(d))) // 篩選條件
                .ToList();

            //在後端 FilterByDistrict 中加入測試輸出
            Console.WriteLine("返回的 JSON 資料: ");
            foreach (var restaurant in filteredRestaurants)
            {
                Console.WriteLine($"Name: {restaurant.Name}, Address: {restaurant.Address}");
            }

            // 測試輸出篩選結果
            Console.WriteLine("符合篩選條件的餐廳數量: " + filteredRestaurants.Count);
            foreach (var restaurant in filteredRestaurants)
            {
                Console.WriteLine($"餐廳名稱: {restaurant.Name}, 地址: {restaurant.Address}");
            }

            // 如果沒有符合條件的餐廳，返回提示
            if (!filteredRestaurants.Any())
            {
                // 如果沒有匹配的結果，返回空部分視圖
                return PartialView("PartialView/_SearchRestaurantFolder/_SearchDistrictEmpty");
            }

            // 返回篩選結果，渲染為部分視圖
            return PartialView("PartialView/_SearchRestaurantFolder/_SearchDistrict", filteredRestaurants);
        }

        [HttpPost]
        public IActionResult SortRestaurantsByLocation([FromBody] LocationViewModel userLocation)
        {
            double userLat = userLocation.Latitude;
            double userLng = userLocation.Longitude;
            bool sortByDistance = userLocation.SortByDistance;

            // 查詢餐廳並按距離或名稱排序
            var restaurants = _context.Restaurants
                .AsEnumerable()
                .Select(r => new LocationViewModel
                {
                    Id = r.RestaurantId,
                    Name = r.Name,
                    Address = r.Address,
                    Description = r.Description,
                    AverageRating = r.AverageRating ?? 0,
                    Latitude = ExtractCoordinates(r.GoogleMapAddress).Latitude,
                    Longitude = ExtractCoordinates(r.GoogleMapAddress).Longitude,
                })
                .ToList();

            // 按距離或名稱排序
            if (sortByDistance)
            {
                // 按距離排序
                restaurants = restaurants
                    .OrderBy(r => CalculateDistance(userLat, userLng, r.Latitude, r.Longitude))
                    .ToList();
            }
            else
            {
                // 按名稱排序
                restaurants = restaurants
                    .OrderBy(r => r.Name)
                    .ToList();
            }

            // 返回部分視圖
            return PartialView("~/Views/Shared/PartialView/_SearchRestaurantFolder/_SearchDistance.cshtml", restaurants);
        }


        // 私有方法：提取經緯度
        private (double Latitude, double Longitude) ExtractCoordinates(string googleMapAddress)
        {
            if (string.IsNullOrEmpty(googleMapAddress) || !googleMapAddress.Contains("@"))
                return (0, 0); // 返回默認值

            var coordinatePart = googleMapAddress.Split('@')[1].Split(','); // 按 ',' 分割經緯度
            if (coordinatePart.Length < 2)
                return (0, 0); // 返回默認值

            if (double.TryParse(coordinatePart[0], out double latitude) &&
                double.TryParse(coordinatePart[1], out double longitude))
            {
                return (latitude, longitude);
            }

            return (0, 0); // 解析失敗，返回默認值
        }

        // 私有方法：計算距離
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double EarthRadius = 6371; // 地球半徑（公里）
            double dLat = ToRadians(lat2 - lat1);
            double dLng = ToRadians(lng2 - lng1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadius * c; // 返回距離
        }

        // 私有方法：角度轉弧度
        private double ToRadians(double angle)
        {
            return angle * Math.PI / 180;
        }

        [HttpPost]
        public IActionResult FilterRestaurantsByPrice([FromBody] PriceFilterViewModel filter)
        {
            // 根據價位條件篩選餐廳
            var priceRange = filter.PriceRange;
            var restaurants = _context.Restaurants
                .AsEnumerable()
                .Where(r =>
                {
                    // 提取 NT$ 價格並轉換為數字
                    var priceString = r.PriceRange?.Replace("NT$", "").Replace(",", "").Trim();
                    if (!int.TryParse(priceString, out int price)) return false;

                    // 價位篩選條件
                    return priceRange switch
                    {
                        "$" => price >= 0 && price <= 500,
                        "$$" => price >= 501 && price <= 1000,
                        "$$$" => price >= 1001 && price <= 1500,
                        "$$$$" => price > 1500,
                        _ => false,
                    };
                })
                .Select(r => new LocationViewModel
                {
                    Id = r.RestaurantId,
                    Name = r.Name,
                    Address = r.Address,
                    Description = r.Description,
                    AverageRating = r.AverageRating ?? 0,
                    Latitude = ExtractCoordinates(r.GoogleMapAddress).Latitude,
                    Longitude = ExtractCoordinates(r.GoogleMapAddress).Longitude,
                    SearchKeyword = priceRange, // 儲存篩選條件
                })
                .ToList();

            // 返回篩選結果的部分視圖
            return PartialView("~/Views/Shared/PartialView/_SearchRestaurantFolder/_SearchMoney.cshtml", restaurants);
        }

    }
}