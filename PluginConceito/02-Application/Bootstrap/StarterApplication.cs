using System;
using System.Collections.Generic;
using System.Reflection;
using PluginConceito.Application.Contracts;
using PluginConceito.Application.Modules;
using PluginConceito.Application.Ribbon;
using PluginConceito.Application.Zwcad;
using ZwSoft.ZwCAD.Runtime;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(PluginConceito.Application.Bootstrap.StarterApplication))]

namespace PluginConceito.Application.Bootstrap
{
    public sealed class StarterApplication : IExtensionApplication
    {
        private RibbonHost _ribbonHost;
        private IReadOnlyList<RibbonItemDefinition> _ribbonDefinitions;
        private ZwcadContext _zwcad;
        private bool _waitingForRibbon;

        public void Initialize()
        {
            _zwcad = new ZwcadContext();

            try
            {
                Assembly assembly = typeof(StarterApplication).Assembly;
                var telemetry = new ZwcadTelemetry(_zwcad);
                var moduleContext = new ModuleContext(telemetry, _zwcad);
                InitializeModules(assembly, moduleContext, telemetry);

                var discovery = new RibbonDiscovery();
                IReadOnlyList<RibbonItemDefinition> discovered = discovery.Discover(assembly);
                RibbonValidationResult validation = new RibbonValidator().Validate(assembly, discovered);

                foreach (string error in validation.Errors)
                {
                    _zwcad.WriteMessage("Ribbon: " + error);
                }

                _ribbonDefinitions = validation.ValidDefinitions;
                _ribbonHost = new RibbonHost(assembly, message => _zwcad.WriteMessage(message));

                if (!TryCreateRibbon())
                {
                    ZwcadApplication.Idle += OnApplicationIdle;
                    _waitingForRibbon = true;
                }

                _zwcad.WriteMessage(
                    "Plugin inicializado. " + _ribbonDefinitions.Count + " comando(s) de Ribbon válido(s).");
            }
            catch (System.Exception exception)
            {
                _zwcad.WriteMessage("Falha na inicialização: " + exception);
            }
        }

        public void Terminate()
        {
            StopWaitingForRibbon();
        }

        private static void InitializeModules(
            Assembly assembly,
            IModuleContext context,
            ITelemetry telemetry)
        {
            var discovery = new ModuleDiscovery();
            foreach (Type moduleType in discovery.FindModuleTypes(assembly))
            {
                try
                {
                    var module = (ICntModule)Activator.CreateInstance(moduleType);
                    module.Initialize(context);
                    telemetry.TrackEvent("Module." + module.Id + ".Initialized");
                }
                catch (System.Exception exception)
                {
                    telemetry.TrackException("Module." + moduleType.FullName + ".Initialize", exception);
                }
            }
        }

        private void OnApplicationIdle(object sender, EventArgs eventArgs)
        {
            if (TryCreateRibbon())
            {
                StopWaitingForRibbon();
            }
        }

        private bool TryCreateRibbon()
        {
            try
            {
                return _ribbonHost != null &&
                    _ribbonDefinitions != null &&
                    _ribbonHost.TryBuild(_ribbonDefinitions);
            }
            catch (System.Exception exception)
            {
                _zwcad.WriteMessage("Falha ao criar a Ribbon: " + exception.Message);
                return false;
            }
        }

        private void StopWaitingForRibbon()
        {
            if (!_waitingForRibbon)
            {
                return;
            }

            ZwcadApplication.Idle -= OnApplicationIdle;
            _waitingForRibbon = false;
        }
    }
}
