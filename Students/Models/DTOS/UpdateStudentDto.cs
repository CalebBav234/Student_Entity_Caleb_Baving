namespace Students.Models.DTOS
{
    public record UpdateStudentDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
    }
}
