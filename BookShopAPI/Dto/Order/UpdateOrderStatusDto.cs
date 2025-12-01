using System.ComponentModel.DataAnnotations;

namespace BookShopAPI.Dto.Order
{
    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Status ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Status ID must be positive")]
        public int StatusId { get; set; }
    }
}
