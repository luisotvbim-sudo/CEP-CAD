using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using PluginConceito.Application.Contracts;
using PluginConceito.Application.Zwcad;
using ZwSoft.Windows;

namespace PluginConceito.Application.Ribbon
{
    public sealed class RibbonHost
    {
        private readonly RibbonIconLoader _iconLoader;
        private readonly Action<string> _log;

        public RibbonHost(Assembly assembly, Action<string> log)
        {
            _iconLoader = new RibbonIconLoader(assembly);
            _log = log ?? delegate { };
        }

        public bool TryBuild(IEnumerable<RibbonItemDefinition> definitions)
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon == null)
            {
                return false;
            }

            var orderedItems = definitions
                .OrderBy(item => item.RibbonAttribute.Order)
                .ThenBy(item => item.RibbonAttribute.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var tabGroup in orderedItems.GroupBy(item => item.RibbonAttribute.TabId))
            {
                RibbonItemDefinition firstTabItem = tabGroup.First();
                RibbonTab tab = GetOrCreateTab(
                    ribbon,
                    firstTabItem.RibbonAttribute.TabId,
                    firstTabItem.RibbonAttribute.TabTitle);

                foreach (var panelGroup in tabGroup.GroupBy(item => item.RibbonAttribute.PanelId))
                {
                    RibbonItemDefinition firstPanelItem = panelGroup.First();
                    RibbonPanel panel = GetOrCreatePanel(
                        tab,
                        firstPanelItem.RibbonAttribute.PanelId,
                        firstPanelItem.RibbonAttribute.PanelTitle);

                    foreach (RibbonItemDefinition definition in panelGroup)
                    {
                        TryAddButton(panel.Source, definition);
                    }
                }
            }

            return true;
        }

        private static RibbonTab GetOrCreateTab(RibbonControl ribbon, string id, string title)
        {
            RibbonTab tab = ribbon.Tabs.FirstOrDefault(
                item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

            if (tab != null)
            {
                return tab;
            }

            tab = new RibbonTab
            {
                Id = id,
                Title = title,
                Name = id
            };

            ribbon.Tabs.Add(tab);
            return tab;
        }

        private static RibbonPanel GetOrCreatePanel(RibbonTab tab, string id, string title)
        {
            RibbonPanel panel = tab.Panels.FirstOrDefault(item =>
                item.Source != null &&
                string.Equals(item.Source.Id, id, StringComparison.OrdinalIgnoreCase));

            if (panel != null)
            {
                return panel;
            }

            var source = new RibbonPanelSource
            {
                Id = id,
                Title = title,
                Name = id
            };

            panel = new RibbonPanel
            {
                Source = source
            };

            tab.Panels.Add(panel);
            return panel;
        }

        private void TryAddButton(RibbonPanelSource panel, RibbonItemDefinition definition)
        {
            CntRibbonCommandAttribute attribute = definition.RibbonAttribute;
            bool alreadyExists = panel.Items
                .OfType<RibbonButton>()
                .Any(item => string.Equals(item.Id, attribute.ButtonId, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                return;
            }

            try
            {
                var button = new RibbonButton
                {
                    Id = attribute.ButtonId,
                    Name = attribute.ButtonId,
                    Text = attribute.DisplayName,
                    Description = attribute.ToolTip,
                    ShowText = true,
                    ShowImage = !string.IsNullOrWhiteSpace(attribute.IconResource),
                    Size = attribute.Size,
                    Orientation = Orientation.Vertical,
                    CommandHandler = new ZwcadCommandDispatcher(attribute.CommandName)
                };

                SetOptionalProperty(button, "ToolTip", attribute.ToolTip);
                SetOptionalProperty(button, "TooltipTitle", attribute.DisplayName);

                if (!string.IsNullOrWhiteSpace(attribute.IconResource))
                {
                    var icon = _iconLoader.Load(attribute.IconResource);

                    // ZWCAD 2024 ignora LargeImage quando Image já contém a mesma instância.
                    button.LargeImage = icon;
                    button.Image = icon;
                }

                panel.Items.Add(button);
            }
            catch (Exception exception)
            {
                _log("Falha ao criar o botão " + attribute.ButtonId + ": " + exception.Message);
            }
        }

        private static void SetOptionalProperty(object target, string propertyName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            try
            {
                property.SetValue(target, value, null);
            }
            catch
            {
                // Propriedade opcional entre versões do ZWCAD.
            }
        }
    }
}
