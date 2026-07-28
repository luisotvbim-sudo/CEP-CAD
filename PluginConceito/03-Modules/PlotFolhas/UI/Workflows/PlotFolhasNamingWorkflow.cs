using System;
using System.Collections.Generic;
using System.Windows;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasNamingWorkflow
    {
        private readonly PlotFolhasNamingService _namingService;
        private readonly SheetRevisionService _revisionService;
        private readonly SeloBlockService _seloService;
        private readonly ITelemetry _telemetry;

        public PlotFolhasNamingWorkflow(
            PlotFolhasNamingService namingService,
            SheetRevisionService revisionService,
            SeloBlockService seloService,
            ITelemetry telemetry)
        {
            _namingService = namingService ?? throw new ArgumentNullException(nameof(namingService));
            _revisionService = revisionService ??
                throw new ArgumentNullException(nameof(revisionService));
            _seloService = seloService ?? throw new ArgumentNullException(nameof(seloService));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public IReadOnlyList<string> GetStampBlockNames()
        {
            try
            {
                return _seloService.GetBlockNames();
            }
            catch (Exception exception)
            {
                _telemetry.TrackException("SeloBlock.GetBlockNames", exception);
                return new List<string>();
            }
        }

        public void ApplyStructure(PlotFolhasWindow window)
        {
            if (window == null) return;

            _namingService.ApplyStructure(
                window.Sheets,
                window.NamingStructure);
            window.RefreshSheets();
            window.SetStatusMessage("Estrutura aplicada em todas as folhas.");
        }

        public void NormalizeEditedName(PlotFolhasWindow window)
        {
            if (window == null || window.EditedSheet == null) return;

            _namingService.NormalizeEditedName(window.EditedSheet, window.Sheets);
            window.RefreshSheets();
            window.SetStatusMessage(
                "Nome atualizado na folha " + window.EditedSheet.Sequencia + ".");
        }

        public void Save(PlotFolhasWindow window)
        {
            if (window == null) return;

            window.CommitChanges();
            PlotFolhasNameValidation validation = _namingService.NormalizeAndValidate(
                window.Sheets);
            window.RefreshSheets();
            if (!validation.IsValid)
            {
                ShowInvalidNamesWarning(window);
                return;
            }

            SaveValidatedNames(window);
        }

        public void LoadStampAttributes(PlotFolhasWindow window)
        {
            if (window == null) return;

            try
            {
                IReadOnlyList<string> attributes = string.IsNullOrWhiteSpace(
                    window.SelectedStampBlock)
                    ? new List<string>()
                    : _seloService.GetAttributeTags(window.SelectedStampBlock);
                window.SetStampAttributes(attributes);
            }
            catch (Exception exception)
            {
                _telemetry.TrackException("SeloBlock.GetAttributeTags", exception);
                window.SetStampAttributes(new List<string>());
                window.SetStatusMessage(
                    "Não foi possível carregar os atributos do selo: " + exception.Message);
            }
        }

        public void LoadNamesFromStamp(PlotFolhasWindow window)
        {
            if (window == null) return;

            if (string.IsNullOrWhiteSpace(window.SelectedStampBlock) ||
                string.IsNullOrWhiteSpace(window.SelectedStampAttribute))
            {
                window.SetStatusMessage(
                    "Escolha o bloco e o atributo que fornecerão os nomes.");
                return;
            }

            try
            {
                window.CommitChanges();
                _revisionService.Reset(
                    window.Sheets,
                    false);
                int copied = _seloService.CopyAttributeValuesToSheetNames(
                    window.Sheets,
                    window.SelectedStampBlock,
                    window.SelectedStampAttribute);
                _namingService.NormalizeAndValidate(window.Sheets);
                window.RefreshSheets();
                window.SetStatusMessage(string.Format(
                    "Nome carregado do atributo {0} em {1} de {2} folha(s).",
                    window.SelectedStampAttribute,
                    copied,
                    window.Sheets.Count));
            }
            catch (Exception exception)
            {
                _telemetry.TrackException(
                    "SeloBlock.CopyAttributeValuesToSheetNames",
                    exception);
                window.SetStatusMessage(
                    "Não foi possível carregar os nomes do selo: " + exception.Message);
                MessageBox.Show(
                    window,
                    exception.Message,
                    "Carregar nomes do selo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveValidatedNames(PlotFolhasWindow window)
        {
            try
            {
                window.SetBusy(true);
                int saved = _namingService.Save(window.Sheets);
                _seloService.FillSeloAttributes(
                    window.Sheets,
                    window.SelectedStampBlock,
                    window.SelectedStampAttribute);
                window.SetStatusMessage(
                    "Nomenclatura salva em " + saved + " folha(s).");
            }
            catch (Exception exception)
            {
                _telemetry.TrackException("CNT_PLOT_FOLHAS.SaveNames", exception);
                window.SetStatusMessage(
                    "Erro ao salvar nomenclatura: " + exception.Message);
                MessageBox.Show(
                    window,
                    exception.Message,
                    "Erro ao salvar nomenclatura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                window.SetBusy(false);
            }
        }

        private static void ShowInvalidNamesWarning(PlotFolhasWindow window)
        {
            MessageBox.Show(
                window,
                "Existem folhas com erro de nome. Corrija antes de salvar a nomenclatura.",
                "Salvar nomenclatura",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
