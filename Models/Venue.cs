using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    public class Venue
    {
        public int VenueID { get; set; }

        [Required(ErrorMessage = "Venue name is required.")]
        [StringLength(150, ErrorMessage = "Venue name cannot exceed 150 characters.")]
        public string? VenueName { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(250, ErrorMessage = "Location cannot exceed 250 characters.")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100,000.")]
        public int Capacity { get; set; }

        /// <summary>Stored blob URL from Azurite or Azure Storage.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Not mapped to DB — receives the uploaded file from the form.</summary>
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
