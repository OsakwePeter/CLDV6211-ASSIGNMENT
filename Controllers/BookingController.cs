using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking — consolidated view with search
        public async Task<IActionResult> Index(string? searchTerm)
        {
            ViewData["SearchTerm"] = searchTerm;

            var query = _context.Booking
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                if (int.TryParse(term, out int bookingId))
                {
                    // Exact match on BookingID
                    query = query.Where(b => b.BookingID == bookingId);
                }
                else
                {
                    // Case-insensitive partial match on Event Name
                    var lowerTerm = term.ToLower();
                    query = query.Where(b =>
                        b.Event != null &&
                        b.Event.EventName != null &&
                        b.Event.EventName.ToLower().Contains(lowerTerm));
                }
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Booking
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingID == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // GET: Booking/Create
        public IActionResult Create()
        {
            ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
            ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
            return View();
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            ModelState.Remove(nameof(booking.Event));
            ModelState.Remove(nameof(booking.Venue));

            if (!ModelState.IsValid)
            {
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            // ─── Double-booking check ───
            // Step 1: Load the selected event to get its date
            var selectedEvent = await _context.Event.FindAsync(booking.EventID);
            if (selectedEvent == null)
            {
                ModelState.AddModelError(nameof(booking.EventID), "Selected event does not exist.");
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            // Step 2: Check for any booking at the same venue on the same calendar date
            var selectedDate = selectedEvent.EventDate.Date;
            var conflict = await _context.Booking
                .Include(b => b.Event)
                .AnyAsync(b =>
                    b.VenueID == booking.VenueID &&
                    b.Event != null &&
                    b.Event.EventDate.Date == selectedDate);

            if (conflict)
            {
                ModelState.AddModelError(string.Empty,
                    $"⚠ Booking conflict: '{_context.Venue.Find(booking.VenueID)?.VenueName}' is already booked " +
                    $"on {selectedDate:dd MMM yyyy}. Please choose a different venue or date.");
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            booking.BookingDate = DateTime.Now;
            _context.Add(booking);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Catches the unique index violation (VenueID, EventID) as a secondary guard
                ModelState.AddModelError(string.Empty,
                    "⚠ This venue is already booked for that event. Please choose a different venue.");
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            TempData["SuccessMessage"] = $"Booking #{booking.BookingID} was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Booking/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Booking.FindAsync(id);
            if (booking == null) return NotFound();
            ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
            ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
            return View(booking);
        }

        // POST: Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.BookingID) return NotFound();

            ModelState.Remove(nameof(booking.Event));
            ModelState.Remove(nameof(booking.Venue));

            if (!ModelState.IsValid)
            {
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            // ─── Double-booking check (excluding self) ───
            var selectedEvent = await _context.Event.FindAsync(booking.EventID);
            if (selectedEvent == null)
            {
                ModelState.AddModelError(nameof(booking.EventID), "Selected event does not exist.");
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            var selectedDate = selectedEvent.EventDate.Date;
            var conflict = await _context.Booking
                .Include(b => b.Event)
                .AnyAsync(b =>
                    b.BookingID != id &&               // exclude self
                    b.VenueID == booking.VenueID &&
                    b.Event != null &&
                    b.Event.EventDate.Date == selectedDate);

            if (conflict)
            {
                ModelState.AddModelError(string.Empty,
                    $"⚠ Booking conflict: '{_context.Venue.Find(booking.VenueID)?.VenueName}' is already booked " +
                    $"on {selectedDate:dd MMM yyyy}. Please choose a different venue or date.");
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            try
            {
                _context.Update(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Booking #{booking.BookingID} was updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Booking.AnyAsync(b => b.BookingID == booking.BookingID))
                    return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
                    "⚠ This venue is already booked for that event. Please choose a different venue.");
                ViewData["Events"] = _context.Event.OrderBy(e => e.EventName).ToList();
                ViewData["Venues"] = _context.Venue.OrderBy(v => v.VenueName).ToList();
                return View(booking);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Booking
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingID == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Booking.FindAsync(id);
            if (booking != null)
            {
                _context.Booking.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Booking #{id} was cancelled and removed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
