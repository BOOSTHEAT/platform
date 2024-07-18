using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services.Project;

internal interface IProjectOperation<in INPUT, OUTPUT>
{
    Task<OUTPUT> Execute(INPUT input);
}