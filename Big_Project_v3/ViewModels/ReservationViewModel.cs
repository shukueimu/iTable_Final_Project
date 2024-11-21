namespace Big_Project_v3.ViewModels
{
    public class ReservationViewModel
    {
        public int RestaurantID { get; set; }
        public string Name { get; set; }
        public string ReservationStatus { get; set; }
        public int NumAdults { get; set; }
        public int NumChildren { get; set; }
        public DateTime ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
    }
}
