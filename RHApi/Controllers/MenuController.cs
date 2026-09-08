using Microsoft.AspNetCore.Mvc;
using RHInfrastructure.Menu;

namespace RHApi.Controllers;

[Route("api/menu")]
[ApiController]
public class MenuController : ControllerBase
{
    private readonly MenuService _menuService;
    public MenuController(MenuService menuService) => _menuService = menuService;

    [HttpGet]
    public async Task<IActionResult> GetTree()
    {
        var tree = await _menuService.GetTreeAsync();
        return Ok(tree);
    }
}
