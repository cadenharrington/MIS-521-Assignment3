using FL2024_Assignment3_charrington.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace FL2024_Assignment3_charrington.ViewModels
{
    public class MovieCreateViewModel
    {
        public Movie Movie { get; set; } = new Movie();
        public IFormFile? PosterURL { get; set; }

        [ValidateNever]
        public List<int> SelectedActors { get; set; } = new List<int>();
        public IEnumerable<SelectListItem> AvailableActors { get; set; } = new List<SelectListItem>();
    }
}