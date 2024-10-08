using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FL2024_Assignment3_charrington.Data;
using FL2024_Assignment3_charrington.Models;
using FL2024_Assignment3_charrington.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VaderSharp2;

namespace FL2024_Assignment3_charrington.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actors.ToListAsync());
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actors
                .Include(a => a.MovieActors)
                .ThenInclude(ma => ma.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (actor == null)
            {
                return NotFound();
            }

            var viewModel = new ActorDetailsViewModel
            {
                Actor = actor,
                AssociatedMovies = actor.MovieActors.Select(ma => ma.Movie).ToList()
            };

            try
            {
                var redditData = await SearchRedditAsync(actor.Name);
                var sentimentData = AnalyzeSentiment(redditData);
                viewModel.RedditPostsAndSentiment = sentimentData.PostsAndSentiment;
                viewModel.OverallSentiment = sentimentData.AverageScore;
                viewModel.SentimentCategory = sentimentData.Category;
            }
            catch (Exception ex)
            {
                // Log the error and display a message to the user
                viewModel.RedditPostsAndSentiment = new Dictionary<string, float>();
                viewModel.OverallSentiment = 0;
                viewModel.SentimentCategory = "Unable to retrieve sentiment data (Request blocked)";
                // Optionally, log the exception
                Console.WriteLine($"Error fetching Reddit data: {ex.Message}");
            }

            return View(viewModel);
        }

        // Helper method to fetch Reddit posts related to the actor
        public static async Task<List<string>> SearchRedditAsync(string searchQuery)
        {
            var textToExamine = new List<string>();
            var queryText = searchQuery;
            var json = "";

            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                json = wc.DownloadString("https://www.reddit.com/search.json?limit=100&q=" + System.Web.HttpUtility.UrlEncode(queryText));
            }

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var childrenElement = doc.RootElement.GetProperty("data").GetProperty("children");
            foreach (var child in childrenElement.EnumerateArray())
            {
                if (child.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("selftext", out var selftext))
                    {
                        var selfTextValue = selftext.GetString();
                        if (!string.IsNullOrEmpty(selfTextValue))
                        {
                            textToExamine.Add(selfTextValue);
                        }
                        else if (data.TryGetProperty("title", out var title))
                        {
                            var titleValue = title.GetString();
                            if (!string.IsNullOrEmpty(titleValue))
                            {
                                textToExamine.Add(titleValue);
                            }
                        }
                    }
                }
            }
            return textToExamine;
        }

        // Helper method to analyze sentiment using VaderSharp2
        public static (Dictionary<string, float> PostsAndSentiment, float AverageScore, string Category) AnalyzeSentiment(List<string> textToExamine)
        {
            var analyzer = new SentimentIntensityAnalyzer();
            var sentimentResults = new Dictionary<string, float>();
            int validResults = 0;
            double resultsTotal = 0;

            foreach (string textValue in textToExamine)
            {
                var results = analyzer.PolarityScores(textValue);
                if (results.Compound != 0)
                {
                    if (!sentimentResults.ContainsKey(textValue))
                    {
                        sentimentResults.Add(textValue, (float)results.Compound);
                        resultsTotal += results.Compound;
                        validResults++;
                    }
                }
            }

            float avgResult = (float)Math.Round(resultsTotal / validResults, 2);
            string categorizedResult = CategorizeSentiment(avgResult);
            return (sentimentResults, avgResult, categorizedResult);
        }

        public static string CategorizeSentiment(double sentiment)
        {
            if (sentiment >= -1 && sentiment < -0.6)
                return "Extremely Negative";
            else if (sentiment >= -0.6 && sentiment < -0.2)
                return "Very Negative";
            else if (sentiment >= -0.2 && sentiment < 0)
                return "Slightly Negative";
            else if (sentiment >= 0 && sentiment < 0.2)
                return "Slightly Positive";
            else if (sentiment >= 0.2 && sentiment < 0.6)
                return "Very Positive";
            else if (sentiment >= 0.6 && sentiment <= 1)
                return "Highly Positive";
            else
                return "Invalid sentiment value";
        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            ViewBag.Movies = new SelectList(_context.Movies.ToList(), "Id", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActorCreateViewModel viewModel, IFormFile Photo, int[] selectedMovies)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var actor = new Actor
                    {
                        Name = viewModel.Actor.Name,
                        Gender = viewModel.Actor.Gender,
                        Age = viewModel.Actor.Age,
                        IMDBHyperlink = viewModel.Actor.IMDBHyperlink
                    };

                    if (Photo != null && Photo.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await Photo.CopyToAsync(memoryStream);
                            actor.Photo = memoryStream.ToArray();
                        }
                    }

                    _context.Add(actor);
                    await _context.SaveChangesAsync();

                    // Optional: Add movies only if any selected
                    if (selectedMovies != null && selectedMovies.Length > 0)
                    {
                        foreach (var movieId in selectedMovies)
                        {
                            _context.MovieActors.Add(new MovieActor
                            {
                                ActorId = actor.Id,
                                MovieId = movieId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                }
            }

            // This removes the validation error on SelectedMovies when it is optional
            ModelState.Remove("SelectedMovies");

            ViewBag.Movies = new SelectList(_context.Movies.ToList(), "Id", "Title");
            return View(viewModel);
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actors
                .Include(a => a.MovieActors)
                .ThenInclude(ma => ma.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (actor == null)
            {
                return NotFound();
            }

            var viewModel = new ActorEditViewModel
            {
                Actor = actor,
                SelectedMovies = actor.MovieActors
                    .Where(ma => ma.MovieId.HasValue)            // Filter out null values
                    .Select(ma => ma.MovieId.Value)              // Get the int value
                    .ToList(),
                AvailableMovies = new SelectList(_context.Movies, "Id", "Title") // Populate available movies here
            };

            ViewBag.Movies = viewModel.AvailableMovies; // Pass to ViewBag for usage in the View

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActorEditViewModel viewModel)
        {
            if (id != viewModel.Actor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var actor = await _context.Actors
                        .Include(a => a.MovieActors)
                        .FirstOrDefaultAsync(a => a.Id == viewModel.Actor.Id);

                    if (actor == null)
                    {
                        return NotFound();
                    }

                    // Update actor properties
                    actor.Name = viewModel.Actor.Name;
                    actor.Gender = viewModel.Actor.Gender;
                    actor.Age = viewModel.Actor.Age;
                    actor.IMDBHyperlink = viewModel.Actor.IMDBHyperlink;

                    // Handle photo upload
                    if (viewModel.Photo != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await viewModel.Photo.CopyToAsync(memoryStream);
                            actor.Photo = memoryStream.ToArray();
                        }
                    }

                    // Remove existing MovieActors
                    var existingMovieActors = _context.MovieActors.Where(ma => ma.ActorId == actor.Id);
                    _context.MovieActors.RemoveRange(existingMovieActors);

                    // Add selected movies
                    if (viewModel.SelectedMovies != null && viewModel.SelectedMovies.Any())
                    {
                        foreach (var movieId in viewModel.SelectedMovies)
                        {
                            _context.MovieActors.Add(new MovieActor
                            {
                                ActorId = actor.Id,
                                MovieId = movieId
                            });
                        }
                    }

                    _context.Update(actor);
                    await _context.SaveChangesAsync();

                    // Redirect to the Actors index page
                    return RedirectToAction("Index", "Actors");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(viewModel.Actor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            viewModel.AvailableMovies = new SelectList(_context.Movies, "Id", "Title", viewModel.SelectedMovies);
            return View(viewModel);
        }





        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actors.FindAsync(id);
            if (actor != null)
            {
                _context.Actors.Remove(actor);
                _context.MovieActors.RemoveRange(_context.MovieActors.Where(ma => ma.ActorId == id));
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return _context.Actors.Any(e => e.Id == id);
        }
    }
}