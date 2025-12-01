using System.ComponentModel.DataAnnotations;

namespace BookShopAPI.Dto.Order
{
    public class CreateOrderRequestDto
    {
        [Required(ErrorMessage = "Shipping address is required")]
        [StringLength(255, ErrorMessage = "Shipping address cannot exceed 255 characters")]
        public string ShippingAddress { get; set; } = null!;

        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new();
    }
}
