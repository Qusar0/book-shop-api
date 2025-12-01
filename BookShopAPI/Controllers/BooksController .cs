using AutoMapper;
using BookShopAPI.Dto.Book;
using BookShopAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookShopAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly BookShopContext _db;
        private readonly IMapper _mapper;

        public BooksController(BookShopContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetBooks(
            [FromQuery] string? search,
            [FromQuery] int? authorId)
        {
            var query = _db.Books.Include(b => b.Author).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search));
            }

            if (authorId.HasValue)
            {
                query = query.Where(b => b.AuthorId == authorId.Value);
            }

            var books = await query.ToListAsync();
            var bookDtos = _mapper.Map<List<BookResponseDto>>(books);
            return Ok(bookDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookResponseDto>> GetBook(int id)
        {
            var book = await _db.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
                return NotFound();

            var bookDto = _mapper.Map<BookResponseDto>(book);
            return Ok(bookDto);
        }

        [HttpPost]
        public async Task<ActionResult<BookResponseDto>> CreateBook(BookCreateDto bookCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authorExists = await _db.Authors.AnyAsync(a => a.AuthorId == bookCreateDto.AuthorId);
            if (!authorExists)
                return BadRequest("Указанный автор не существует");

            if (!string.IsNullOrEmpty(bookCreateDto.Isbn))
            {
                var isbnExists = await _db.Books.AnyAsync(b => b.Isbn == bookCreateDto.Isbn);
                if (isbnExists)
                    return BadRequest("Книга с таким ISBN уже существует");
            }

            var book = _mapper.Map<Book>(bookCreateDto);

            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            await _db.Entry(book).Reference(b => b.Author).LoadAsync();

            var bookResponseDto = _mapper.Map<BookResponseDto>(book);
            return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, bookResponseDto);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateBook(BookUpdateDto bookUpdateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var book = await _db.Books.FindAsync(bookUpdateDto.BookId);
            if (book == null)
                return NotFound();

            var authorExists = await _db.Authors.AnyAsync(a => a.AuthorId == bookUpdateDto.AuthorId);
            if (!authorExists)
                return BadRequest("Указанный автор не существует");

            if (!string.IsNullOrEmpty(bookUpdateDto.Isbn))
            {
                var isbnExists = await _db.Books.AnyAsync(
                    b => b.Isbn == bookUpdateDto.Isbn && b.BookId != bookUpdateDto.BookId
                );
                if (isbnExists)
                    return BadRequest("Книга с таким ISBN уже существует");
            }

            _mapper.Map(bookUpdateDto, book);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(bookUpdateDto.BookId))
                    return NotFound();
                else
                    throw;
            }

            return Ok(bookUpdateDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            var hasOrderItems = await _db.OrderItems.AnyAsync(oi => oi.BookId == id);
            if (hasOrderItems)
                return BadRequest("Невозможно удалить книгу, так как она связана с заказами");

            _db.Books.Remove(book);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private bool BookExists(int id)
        {
            return _db.Books.Any(e => e.BookId == id);
        }
    }
}
