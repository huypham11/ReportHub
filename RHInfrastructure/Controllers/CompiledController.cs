using RHDomain.Controllers;

namespace RHInfrastructure.Controllers;

public class CompiledController
{
    public string Code { get; set; } = "";
    public ControllerDefinitionBase Definition { get; set; } = null!;
    public FieldBuilder Fields { get; set; } = null!;
    public ViewBuilder View { get; set; } = null!;
}
