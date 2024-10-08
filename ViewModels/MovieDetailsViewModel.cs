using FL2024_Assignment3_charrington.Models;
using System.Collections.Generic;

namespace FL2024_Assignment3_charrington.ViewModels
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; }
        public Dictionary<string, float> RedditPostsAndSentiment { get; set; }
        public float OverallSentiment { get; set; }
        public string SentimentCategory { get; set; }

        // Associated entities
        public List<Actor> AssociatedActors { get; set; }
    }
}
