using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Event.Include(e => e.Venue).ToListAsync();
            return View(events);
        }

        public IActionResult Create()
        {
            ViewData["Venues"] = _context.Venue.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Venues"] = _context.Venue.ToList();
            return View(@event);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event.FindAsync(id);
            if (@event == null) return NotFound();

            ViewData["Venues"] = _context.Venue.ToList();
            return View(@event);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.EventID) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Venues"] = _context.Venue.ToList();
            return View(@event);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(m => m.EventID == id);
            if (@event == null) return NotFound();

            var isBooked = await _context.Booking.AnyAsync(b => b.EventID == id);
            ViewBag.IsBooked = isBooked;

            return View(@event);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var isBooked = await _context.Booking.AnyAsync(b => b.EventID == id);
            if (isBooked)
            {
                TempData["ErrorMessage"] = "Cannot delete an event that has existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            var @event = await _context.Event.FindAsync(id);
            if (@event != null)
            {
                _context.Event.Remove(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Event
                .Include(e => e.Venue) // Include related venue if applicable
                .FirstOrDefaultAsync(m => m.EventID == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }
    }
}
