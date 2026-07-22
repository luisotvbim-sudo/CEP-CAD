using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class ModelEntityVisibility
    {
        public static bool IsGloballyVisible(
            Entity entity,
            DatabaseLayerEditScope layerStates)
        {
            try
            {
                if (!entity.Visible) return false;
                return layerStates.WasOriginallyVisible(entity.LayerId);
            }
            catch
            {
                // Entidades proxy podem nao expor a layer corretamente; preserva por seguranca.
                return true;
            }
        }
    }
}
