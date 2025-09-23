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
    public class CollectionsController : ControllerBase
    {
        private readonly ICollectionService _service;
        private readonly IMapper _mapper;

        public CollectionsController(ICollectionService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollectionResponseDto>>> GetAll()
        {
            var cols = await _service.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<CollectionResponseDto>>(cols));
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<CollectionResponseDto>> Get(long id)
        {
            var c = await _service.GetByIdAsync(id);
            if (c == null) return NotFound();
            return Ok(_mapper.Map<CollectionResponseDto>(c));
        }

        [HttpPost]
        public async Task<ActionResult<CollectionResponseDto>> Create([FromBody] CollectionRequestDto request)
        {
            var entity = _mapper.Map<Collection>(request);
            var created = await _service.CreateAsync(entity);
            var resp = _mapper.Map<CollectionResponseDto>(created);
            return CreatedAtAction(nameof(Get), new { id = resp.Id }, resp);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CollectionRequestDto request)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            _mapper.Map(request, existing);
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
