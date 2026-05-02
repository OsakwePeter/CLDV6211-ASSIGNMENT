using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class VenueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobService;

        public VenueController(ApplicationDbContext context, BlobStorageService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: Venue
        public async Task<IActionResult> Index()
        {
            return View(await _context.Venue.ToListAsync());
        }

        // GET: Venue/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var venue = await _context.Venue.FirstOrDefaultAsync(m => m.VenueID == id);
            if (venue == null) return NotFound();
            return View(venue);
        }

        // GET: Venue/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Venue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue)
        {
            // ImageFile is optional — remove it from validation
            ModelState.Remove(nameof(venue.ImageFile));

            // Server-side file validation
            if (venue.ImageFile != null && venue.ImageFile.Length > 0)
            {
                var fileError = _blobService.ValidateImageFile(venue.ImageFile);
                if (fileError != null)
                {
                    ModelState.AddModelError(nameof(venue.ImageFile), fileError);
                    return View(venue);
                }
            }

            if (!ModelState.IsValid)
                return View(venue);

            // Upload to Azurite — gracefully handles Azurite being offline
            if (venue.ImageFile != null && venue.ImageFile.Length > 0)
            {
                venue.ImageUrl = await _blobService.UploadImageAsync(venue.ImageFile);
                if (venue.ImageUrl == null)
                    TempData["WarningMessage"] = "Venue saved, but the image could not be uploaded. Please ensure Azurite is running.";
            }

            _context.Add(venue);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Venue '{venue.VenueName}' was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Venue/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var venue = await _context.Venue.FindAsync(id);
            if (venue == null) return NotFound();
            return View(venue);
        }

        // POST: Venue/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue)
        {
            if (id != venue.VenueID) return NotFound();

            ModelState.Remove(nameof(venue.ImageFile));
            ModelState.Remove(nameof(venue.ImageUrl));

            if (venue.ImageFile != null && venue.ImageFile.Length > 0)
            {
                var fileError = _blobService.ValidateImageFile(venue.ImageFile);
                if (fileError != null)
                {
                    ModelState.AddModelError(nameof(venue.ImageFile), fileError);
                    // Restore existing image URL so the preview still shows
                    var existing2 = await _context.Venue.AsNoTracking().FirstOrDefaultAsync(v => v.VenueID == id);
                    venue.ImageUrl = existing2?.ImageUrl;
                    return View(venue);
                }
            }

            if (!ModelState.IsValid)
                return View(venue);

            // Retrieve current image URL before update
            var existing = await _context.Venue.AsNoTracking().FirstOrDefaultAsync(v => v.VenueID == id);

            if (venue.ImageFile != null && venue.ImageFile.Length > 0)
            {
                // Delete old blob, upload new one
                if (existing?.ImageUrl != null)
                    await _blobService.DeleteImageAsync(existing.ImageUrl);

                venue.ImageUrl = await _blobService.UploadImageAsync(venue.ImageFile);

                if (venue.ImageUrl == null)
                    TempData["WarningMessage"] = "Changes saved, but the new image could not be uploaded. Ensure Azurite is running.";
            }
            else
            {
                // Keep existing image URL if no new file was chosen
                venue.ImageUrl = existing?.ImageUrl;
            }

            try
            {
                _context.Update(venue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Venue '{venue.VenueName}' was updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Venue.AnyAsync(v => v.VenueID == venue.VenueID))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Venue/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venue.FirstOrDefaultAsync(m => m.VenueID == id);
            if (venue == null) return NotFound();

            // Warn user if this venue has bookings
            ViewBag.HasBookings = await _context.Booking.AnyAsync(b => b.VenueID == id);
            ViewBag.BookingCount = await _context.Booking.CountAsync(b => b.VenueID == id);

            return View(venue);
        }

        // POST: Venue/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Hard block: reject deletion if active bookings exist
            var bookingCount = await _context.Booking.CountAsync(b => b.VenueID == id);
            if (bookingCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete this venue — it has {bookingCount} active booking(s). " +
                    "Please cancel all associated bookings first.";
                return RedirectToAction(nameof(Index));
            }

            var venue = await _context.Venue.FindAsync(id);
            if (venue != null)
            {
                await _blobService.DeleteImageAsync(venue.ImageUrl);
                _context.Venue.Remove(venue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Venue '{venue.VenueName}' was deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
