using System.ComponentModel.DataAnnotations;

namespace Students.Models.DTOS
{
    public record CreateStudentDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        public int Age { get; set; }
    }
}
