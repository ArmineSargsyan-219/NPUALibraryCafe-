using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NPUALibraryCafe.DTOs.Menu;

namespace NPUALibraryCafe.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuController : ControllerBase
{
    private readonly IMenuitemRepository _menuRepository;

    public MenuController(IMenuitemRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMenuItems()
    {
        var items = await _menuRepository.GetAllAvailableAsync();
        return Ok(items.Select(ToDto));
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var items = await _menuRepository.GetByCategoryAsync(category);
        return Ok(items.Select(ToDto));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var items = await _menuRepository.SearchAsync(query);
        return Ok(items.Select(ToDto));
    }

    private static MenuItemResponseDto ToDto(Menuitem m) => new()
    {
        Id = m.Itemid,
        Name = m.Itemname,
        Description = m.Description,
        CategoryId = m.CategoryId,
        Price = m.Price,
        ImagePath = m.Imagepath,
        Available = m.Available,
        Rating = m.Rating
    };
}