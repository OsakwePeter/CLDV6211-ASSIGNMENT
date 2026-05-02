using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobService;

        public EventController(ApplicationDbContext context, BlobStorageService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            var events = await _context.Event.Include(e => e.Venue).ToListAsync();
            return View(events);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.EventID == id);

            if (@event == null) return NotFound();
            return View(@event);
        }

        // GET: Event/Create
        public IActionResult Create()
        {
            ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
            return View();
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event @event)
        {
            ModelState.Remove(nameof(@event.Venue));
            ModelState.Remove(nameof(@event.ImageFile));

            if (@event.ImageFile != null && @event.ImageFile.Length > 0)
            {
                var fileError = _blobService.ValidateImageFile(@event.ImageFile);
                if (fileError != null)
                {
                    ModelState.AddModelError(nameof(@event.ImageFile), fileError);
                    ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                    return View(@event);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(@event);
            }

            if (@event.ImageFile != null && @event.ImageFile.Length > 0)
            {
                @event.ImageUrl = await _blobService.UploadImageAsync(@event.ImageFile);
                if (@event.ImageUrl == null)
                    TempData["WarningMessage"] = "Event saved, but the image could not be uploaded. Please ensure Azurite is running.";
            }

            _context.Add(@event);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Event '{@event.EventName}' was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Event/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var @event = await _context.Event.FindAsync(id);
            if (@event == null) return NotFound();
            ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
            return View(@event);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.EventID) return NotFound();

            ModelState.Remove(nameof(@event.Venue));
            ModelState.Remove(nameof(@event.ImageFile));
            ModelState.Remove(nameof(@event.ImageUrl));

            if (@event.ImageFile != null && @event.ImageFile.Length > 0)
            {
                var fileError = _blobService.ValidateImageFile(@event.ImageFile);
                if (fileError != null)
                {
                    ModelState.AddModelError(nameof(@event.ImageFile), fileError);
                    var ex2 = await _context.Event.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
                    @event.ImageUrl = ex2?.ImageUrl;
                    ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                    return View(@event);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(@event);
            }

            var existing = await _context.Event.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);

            if (@event.ImageFile != null && @event.ImageFile.Length > 0)
            {
                if (existing?.ImageUrl != null)
                    await _blobService.DeleteImageAsync(existing.ImageUrl);

                @event.ImageUrl = await _blobService.UploadImageAsync(@event.ImageFile);

                if (@event.ImageUrl == null)
                    TempData["WarningMessage"] = "Changes saved, but the new image could not be uploaded. Ensure Azurite is running.";
            }
            else
            {
                @event.ImageUrl = existing?.ImageUrl;
            }

            try
            {
                _context.Update(@event);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{@event.EventName}' was updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Event.AnyAsync(e => e.EventID == @event.EventID))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Event/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.EventID == id);

            if (@event == null) return NotFound();

            ViewBag.IsBooked = await _context.Booking.AnyAsync(b => b.EventID == id);
            ViewBag.BookingCount = await _context.Booking.CountAsync(b => b.EventID == id);

            return View(@event);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bookingCount = await _context.Booking.CountAsync(b => b.EventID == id);
            if (bookingCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete this event — it has {bookingCount} active booking(s). " +
                    "Please cancel all associated bookings first.";
                return RedirectToAction(nameof(Index));
            }

            var @event = await _context.Event.FindAsync(id);
            if (@event != null)
            {
                await _blobService.DeleteImageAsync(@event.ImageUrl);
                _context.Event.Remove(@event);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{@event.EventName}' was deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
