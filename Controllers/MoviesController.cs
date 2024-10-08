using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FL2024_Assignment3_charrington.Data;
using FL2024_Assignment3_charrington.Models;
using FL2024_Assignment3_charrington.ViewModels;
using VaderSharp2;
using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Numerics;

namespace FL2024_Assignment3_charrington.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movies.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                .ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            var viewModel = new MovieDetailsViewModel
            {
                Movie = movie,
                AssociatedActors = movie.MovieActors.Select(ma => ma.Actor).ToList()
            };

            try
            {
                var redditData = await SearchRedditAsync(movie.Title);
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


        // GET: Movies/Create
        public IActionResult Create()
        {
            var viewModel = new MovieCreateViewModel
            {
                AvailableActors = new SelectList(_context.Actors.ToList(), "Id", "Name")
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieCreateViewModel viewModel, int[] selectedActors)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var movie = new Movie
                    {
                        Title = viewModel.Movie.Title,
                        Genre = viewModel.Movie.Genre,
                        YearOfRelease = viewModel.Movie.YearOfRelease,
                        IMDBHyperlink = viewModel.Movie.IMDBHyperlink
                    };

                    _context.Add(movie);
                    await _context.SaveChangesAsync();

                    // Optional: Add actors only if any are selected
                    if (selectedActors != null && selectedActors.Length > 0)
                    {
                        foreach (var actorId in selectedActors)
                        {
                            _context.MovieActors.Add(new MovieActor
                            {
                                MovieId = movie.Id,
                                ActorId = actorId
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

            // This removes the validation error on SelectedActors when it is optional
            ModelState.Remove("SelectedActors");

            ViewBag.Actors = new SelectList(_context.Actors.ToList(), "Id", "Name");
            return View(viewModel);
        }



        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieEditViewModel viewModel)
        {
            if (id != viewModel.Movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var movie = await _context.Movies
                        .Include(m => m.MovieActors)
                        .FirstOrDefaultAsync(m => m.Id == viewModel.Movie.Id);

                    if (movie == null)
                    {
                        return NotFound();
                    }

                    // Update movie properties
                    movie.Title = viewModel.Movie.Title;
                    movie.Genre = viewModel.Movie.Genre;
                    movie.YearOfRelease = viewModel.Movie.YearOfRelease;
                    movie.IMDBHyperlink = viewModel.Movie.IMDBHyperlink;

                    // Handle poster upload
                    if (viewModel.PosterURL != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await viewModel.PosterURL.CopyToAsync(memoryStream);
                            movie.PosterURL = memoryStream.ToArray();
                        }
                    }

                    // Remove existing MovieActors
                    var existingMovieActors = _context.MovieActors.Where(ma => ma.MovieId == movie.Id);
                    _context.MovieActors.RemoveRange(existingMovieActors);

                    // Add selected actors
                    if (viewModel.SelectedActors != null && viewModel.SelectedActors.Any())
                    {
                        foreach (var actorId in viewModel.SelectedActors)
                        {
                            _context.MovieActors.Add(new MovieActor
                            {
                                MovieId = movie.Id,
                                ActorId = actorId
                            });
                        }
                    }

                    _context.Update(movie);
                    await _context.SaveChangesAsync();

                    // Redirect to the Movies index page
                    return RedirectToAction("Index", "Movies");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(viewModel.Movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            viewModel.AvailableActors = new SelectList(_context.Actors, "Id", "Name", viewModel.SelectedActors);
            return View(viewModel);
        }




        // GET: Movies/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                .ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            var viewModel = new MovieEditViewModel
            {
                Movie = movie,
                SelectedActors = movie.MovieActors
                    .Where(ma => ma.ActorId.HasValue)            // Filter out null values
                    .Select(ma => ma.ActorId.Value)              // Get the int value
                    .ToList(),
                AvailableActors = new SelectList(_context.Actors, "Id", "Name") // Populate available actors here
            };

            ViewBag.Actors = viewModel.AvailableActors; // Pass to ViewBag for usage in the View

            return View(viewModel);
        }



        // Helper methods for analyzing Reddit data
        public static async Task<List<string>> SearchRedditAsync(string searchQuery)
        {
            var textToExamine = new List<string>();
            var json = "";

            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                json = wc.DownloadString("https://www.reddit.com/search.json?limit=100&q=" + HttpUtility.UrlEncode(searchQuery));
            }

            var doc = JsonDocument.Parse(json);
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

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                _context.MovieActors.RemoveRange(_context.MovieActors.Where(ma => ma.MovieId == id));
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}
