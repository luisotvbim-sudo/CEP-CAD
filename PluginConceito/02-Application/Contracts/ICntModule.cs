namespace PluginConceito.Application.Contracts
{
    public interface ICntModule
    {
        string Id { get; }

//TESTE_GUIT
        void Initialize(IModuleContext context);
    }
}
