using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models
{
    public class Category
    {
        //[Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Display Order must be between 1 and 100.")]
        [Display(Name = "Display Order")]
        //[ValidateNever]
        public int DisplayOrder { get; set; }
    }
}
