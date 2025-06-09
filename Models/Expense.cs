using System.ComponentModel.DataAnnotations;

namespace ExpenseMate.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public string Category { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public string UserID { get; set; }
    }
}
