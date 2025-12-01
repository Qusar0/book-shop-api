using AutoMapper;
using BookShopAPI.Dto.Author;
using BookShopAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookShopAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly BookShopContext _db;
        private readonly IMapper _mapper;

        public AuthorsController(BookShopContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuthorResponseDto>>> GetAuthors()
        {
            var authors = await _db.Authors.ToListAsync();
            var authorDtos = _mapper.Map<List<AuthorResponseDto>>(authors);
            return Ok(authorDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuthorResponseDto>> GetAuthor(int id)
        {
            var author = await _db.Authors.FindAsync(id);

            if (author == null)
                return NotFound();

            var authorDto = _mapper.Map<AuthorResponseDto>(author);
            return Ok(authorDto);
        }

        [HttpPost]
        public async Task<ActionResult<AuthorResponseDto>> CreateAuthor(AuthorCreateDto authorCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var author = _mapper.Map<Author>(authorCreateDto);

            _db.Authors.Add(author);
            await _db.SaveChangesAsync();

            var authorResponseDto = _mapper.Map<AuthorResponseDto>(author);
            return CreatedAtAction(nameof(GetAuthor), new { id = author.AuthorId }, authorResponseDto);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAuthor(AuthorUpdateDto authorUpdateDto)
        {
            var author = await _db.Authors.FindAsync(authorUpdateDto.AuthorId);
            if (author == null)
                return NotFound();

            _mapper.Map(authorUpdateDto, author);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(authorUpdateDto.AuthorId))
                    return NotFound();
                else
                    throw;
            }

            return Ok(authorUpdateDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            var author = await _db.Authors.FindAsync(id);
            if (author == null)
                return NotFound();

            _db.Authors.Remove(author);
            await _db.SaveChangesAsync();

            return Ok();
        }

        private bool AuthorExists(int id)
        {
            return _db.Authors.Any(e => e.AuthorId == id);
        }
    }
}