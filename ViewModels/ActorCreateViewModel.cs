using FL2024_Assignment3_charrington.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace FL2024_Assignment3_charrington.ViewModels
{
    public class ActorCreateViewModel
    {
        public Actor Actor { get; set; } = new Actor();
        public IFormFile? Photo { get; set; }  // Ensure this property is present

        [ValidateNever]
        public List<int> SelectedMovies { get; set; } = new List<int>();
        public IEnumerable<SelectListItem> AvailableMovies { get; set; } = new List<SelectListItem>();
    }

}
