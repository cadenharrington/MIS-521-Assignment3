using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FL2024_Assignment3_charrington.Data;
using FL2024_Assignment3_charrington.Models;

namespace FL2024_Assignment3_charrington.Controllers
{
    public class MovieActorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovieActorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MovieActors
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.MovieActors.Include(m => m.Actor).Include(m => m.Movie);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: MovieActors/Create
        public IActionResult Create()
        {
            ViewData["ActorId"] = new SelectList(_context.Actors, "Id", "Name");
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Title");
            return View();
        }

        // POST: MovieActors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieId,ActorId")] MovieActor movieActor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movieActor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ActorId"] = new SelectList(_context.Actors, "Id", "Name", movieActor.ActorId);
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Title", movieActor.MovieId);
            return View(movieActor);
        }

        // GET: MovieActors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movieActor = await _context.MovieActors.FindAsync(id);
            if (movieActor == null)
            {
                return NotFound();
            }
            ViewData["ActorId"] = new SelectList(_context.Actors, "Id", "Name", movieActor.ActorId);
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Title", movieActor.MovieId);
            return View(movieActor);
        }

        // POST: MovieActors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MovieId,ActorId")] MovieActor movieActor)
        {
            if (id != movieActor.Id) // Check the primary key here
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movieActor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieActorExists(movieActor.Id)) // Use the correct primary key check
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ActorId"] = new SelectList(_context.Actors, "Id", "Name", movieActor.ActorId);
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Title", movieActor.MovieId);
            return View(movieActor);
        }

        // GET: MovieActors/Delete/5
        public async Task<IActionResult> Delete(int? movieId, int? actorId)
        {
            if (movieId == null || actorId == null)
            {
                return NotFound();
            }

            var movieActor = await _context.MovieActors
                .Include(m => m.Actor)
                .Include(m => m.Movie)
                .FirstOrDefaultAsync(m => m.MovieId == movieId && m.ActorId == actorId);

            if (movieActor == null)
            {
                return NotFound();
            }

            return View(movieActor);
        }

        // POST: MovieActors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int movieId, int actorId)
        {
            var movieActor = await _context.MovieActors
                .FirstOrDefaultAsync(ma => ma.MovieId == movieId && ma.ActorId == actorId);

            if (movieActor != null)
            {
                _context.MovieActors.Remove(movieActor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }



        private bool MovieActorExists(int id)
        {
            return _context.MovieActors.Any(e => e.Id == id);
        }
    }
}
