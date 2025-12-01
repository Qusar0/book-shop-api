using System.ComponentModel.DataAnnotations;

namespace BookShopAPI.Dto.Book
{
    public class BookUpdateDto
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public int AuthorId { get; set; }

        [StringLength(20)]
        public string? Isbn { get; set; }

        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [Range(1, 10000)]
        public int? Pages { get; set; }

        [Range(0, 10000)]
        public int? StockQuantity { get; set; }

        public bool? IsAvailable { get; set; }
    }
}
