namespace PointOfSaleApp.Models
{
    public class RFIDScan
    {
        public int ScanID { get; set; }
        public string CardID { get; set; }
        public DateTime ScanTime { get; set; }
        public string Role { get; set; }
        public string Access { get; set; }
    }
}