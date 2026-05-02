using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    public class Event
    {
        public int EventID { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters.")]
        public string? EventName { get; set; }

        [Required(ErrorMessage = "Event date is required.")]
        public DateTime EventDate { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1,000 characters.")]
        public string? Description { get; set; }

        public int? VenueID { get; set; }
        public Venue? Venue { get; set; }

        /// <summary>Stored blob URL from Azurite or Azure Storage.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Not mapped to DB — receives the uploaded file from the form.</summary>
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
