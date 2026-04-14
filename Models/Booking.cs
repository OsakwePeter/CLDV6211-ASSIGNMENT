using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class Booking
    {
        public int BookingID { get; set; }

        [Required]
        public int EventID { get; set; }
        public Event? Event { get; set; }

        [Required]
        public int VenueID { get; set; }
        public Venue? Venue { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;
    }
}