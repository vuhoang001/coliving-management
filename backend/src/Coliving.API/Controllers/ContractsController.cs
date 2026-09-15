using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class ContractsController : BaseApiController
{
    private readonly IContractService _service;
    public ContractsController(IContractService service) => _service = service;

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<List<ContractDto>>> GetAll([FromQuery] string? status)
        => Ok(await _service.GetAllAsync(status));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPost("booking/{bookingId:int}")]
    public async Task<ActionResult<ContractDto>> Generate(int bookingId, GenerateContractDto dto)
        => Ok(await _service.GenerateAsync(bookingId, dto));

    [HttpPost("{id:int}/sign")]
    public async Task<ActionResult<ContractDto>> Sign(int id, SignContractDto dto)
        => Ok(await _service.SignAsync(CurrentUserId, id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}/terminate")]
    public async Task<ActionResult<ContractDto>> Terminate(int id, [FromQuery] string? reason)
        => Ok(await _service.TerminateAsync(id, reason));
}
