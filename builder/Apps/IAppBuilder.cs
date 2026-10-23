using System.Threading.Tasks;
using NCBOX.Builder.Pipeline;

namespace NCBOX.Builder.Apps;

public interface IAppBuilder
{
    string Name { get; }
    string Description { get; }
    Task<int> BuildAsync(BuildContext ctx);
}
