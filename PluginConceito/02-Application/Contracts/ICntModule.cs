namespace PluginConceito.Application.Contracts
{
    public interface ICntModule
    {
        string Id { get; }

        void Initialize(IModuleContext context);
    }
}
