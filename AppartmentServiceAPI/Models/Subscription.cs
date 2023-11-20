namespace ApartmentServiceAPI.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Link { get; set; }
        public string? Price { get; set; }   
    }
}
