using BookShopAPI.Models;
using AutoMapper;
using BookShopAPI.Dto.Author;
using BookShopAPI.Dto.Book;
using BookShopAPI.Dto.User;
using BookShopAPI.Dto.Order;

namespace BookShopAPI.Configuration
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<Author, AuthorResponseDto>();
            CreateMap<AuthorCreateDto, Author>();
            CreateMap<AuthorUpdateDto, Author>();

            CreateMap<Book, BookResponseDto>()
                .ForMember(
                    dest => dest.AuthorName,
                    opt => opt.MapFrom(src => src.Author != null ? $"{src.Author.FirstName} {src.Author.LastName}" : null)
                );
            CreateMap<BookCreateDto, Book>();
            CreateMap<BookUpdateDto, Book>();
            CreateMap<Customer, UserResponseDto>()
                .ForMember(
                    dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : "Customer")
                );
            CreateMap<CreateOrderRequestDto, Order>();
            CreateMap<Order, OrderResponseDto>();
            CreateMap<OrderItem, OrderItemDto>();
        }
    }
}
