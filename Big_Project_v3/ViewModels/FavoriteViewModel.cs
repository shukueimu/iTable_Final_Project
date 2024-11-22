using Big_Project_v3.Models;

namespace Big_Project_v3.ViewModels
{
    public class Favorite
    {
        public int FavoriteId { get; set; } // 主鍵
        public int? UserId { get; set; }    // 使用者ID
        public int? RestaurantId { get; set; } // 餐廳ID
        public DateTime? AddedAt { get; set; } // 收藏的日期時間

        public virtual Restaurant? Restaurant { get; set; } // 關聯到餐廳表
    }

}
