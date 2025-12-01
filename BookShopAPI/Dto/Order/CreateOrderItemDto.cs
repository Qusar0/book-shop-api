using System.ComponentModel.DataAnnotations;

namespace BookShopAPI.Dto.Order
{
    public class CreateOrderItemDto
    {
        [Required(ErrorMessage = "Book ID is required")]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; }
    }
}
