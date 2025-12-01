namespace BookShopAPI.Dto.Book
{
    public class BookResponseDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = null!;
        public int AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? Isbn { get; set; }
        public decimal Price { get; set; }
        public int? Pages { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
