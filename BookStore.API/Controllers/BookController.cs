using AutoMapper;
using BookStore.API.DTO.Request;
using BookStore.API.DTO.Response;
using BookStore.Data.Model;
using BookStore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _service;
        private readonly IMapper _mapper;

        public BooksController(IBookService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetAll()
        {
            var books = await _service.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<BookResponseDto>>(books));
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<BookResponseDto>> Get(long id)
        {
            var book = await _service.GetByIdAsync(id);
            if (book == null) return NotFound();
            return Ok(_mapper.Map<BookResponseDto>(book));
        }

        [HttpPost]
        public async Task<ActionResult<BookResponseDto>> Create([FromBody] BookRequestDto request)
        {
            var entity = _mapper.Map<Book>(request);
            var created = await _service.CreateAsync(entity);
            var resp = _mapper.Map<BookResponseDto>(created);
            return CreatedAtAction(nameof(Get), new { id = resp.Id }, resp);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] BookRequestDto request)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            _mapper.Map(request, existing); // map request into existing entity
            await _service.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _service.DeleteAsync(existing);
            return NoContent();
        }
    }
}
