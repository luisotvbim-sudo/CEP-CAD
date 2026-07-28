using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaInfo : INotifyPropertyChanged
    {
        private bool _plotar = true;
        private bool _gerarDwg;
        private bool _subirRevisao;
        private string _nomeArquivo;
        private string _erroNome;
        private string _erroRevisao;
        private string _originalNameBeforeRevision;
        private bool _hasRevisionSnapshot;
        private RevisionNameFailureKind _revisionFailureKind;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Sequencia { get; set; }

        public ObjectId BlockReferenceId { get; set; }

        public ObjectId LayoutId { get; set; }

        public string LayoutName { get; set; }

        public SheetSpaceKind SpaceKind { get; set; }

        public bool IsModelSpace
        {
            get { return SpaceKind == SheetSpaceKind.Model; }
        }

        public string BlockName { get; set; }

        public string Formato { get; set; }

        public Extents2d Limites { get; set; }

        public double PlotScaleFactor { get; set; } = 1.0;

        public double Largura
        {
            get { return Limites.MaxPoint.X - Limites.MinPoint.X; }
        }

        public double Altura
        {
            get { return Limites.MaxPoint.Y - Limites.MinPoint.Y; }
        }

        public double LarguraPapel
        {
            get
            {
                return Math.Abs(PlotScaleFactor) > 0.0
                    ? Largura / Math.Abs(PlotScaleFactor)
                    : Largura;
            }
        }

        public double AlturaPapel
        {
            get
            {
                return Math.Abs(PlotScaleFactor) > 0.0
                    ? Altura / Math.Abs(PlotScaleFactor)
                    : Altura;
            }
        }

        public bool LimitePadronizadoEncontrado { get; set; }

        public IList<string> Erros { get; } = new List<string>();

        public IList<string> Avisos { get; } = new List<string>();

        public bool Plotar
        {
            get { return _plotar; }
            set { SetBoolean(ref _plotar, value, nameof(Plotar)); }
        }

        public bool GerarDwg
        {
            get { return _gerarDwg; }
            set { SetBoolean(ref _gerarDwg, value, nameof(GerarDwg)); }
        }

        public bool SubirRevisao
        {
            get { return _subirRevisao; }
            set { SetBoolean(ref _subirRevisao, value, nameof(SubirRevisao)); }
        }

        internal bool HasRevisionSnapshot
        {
            get { return _hasRevisionSnapshot; }
        }

        internal string OriginalNameBeforeRevision
        {
            get { return _originalNameBeforeRevision; }
        }

        internal RevisionNameFailureKind RevisionFailureKind
        {
            get { return _revisionFailureKind; }
        }

        public string NomeArquivo
        {
            get { return _nomeArquivo; }
            set
            {
                if (string.Equals(_nomeArquivo, value, StringComparison.Ordinal))
                {
                    return;
                }

                _nomeArquivo = value;
                RaisePropertyChanged(nameof(NomeArquivo));
                RaisePropertyChanged(nameof(Status));
                RaisePropertyChanged(nameof(Valida));
            }
        }

        public string ErroNome
        {
            get { return _erroNome; }
            set { SetValidationError(ref _erroNome, value, nameof(ErroNome)); }
        }

        public string ErroRevisao
        {
            get { return _erroRevisao; }
            private set
            {
                SetValidationError(
                    ref _erroRevisao,
                    value,
                    nameof(ErroRevisao));
            }
        }

        public bool Valida
        {
            get
            {
                return Erros.Count == 0 &&
                    string.IsNullOrWhiteSpace(ErroNome) &&
                    string.IsNullOrWhiteSpace(ErroRevisao);
            }
        }

        public string Status
        {
            get
            {
                if (Erros.Count > 0)
                {
                    return "Erro: " + string.Join("; ", Erros);
                }

                if (!string.IsNullOrWhiteSpace(ErroNome))
                {
                    return "Erro: " + ErroNome;
                }

                if (!string.IsNullOrWhiteSpace(ErroRevisao))
                {
                    return "Erro de revisão: " + ErroRevisao;
                }

                if (Avisos.Count > 0)
                {
                    return "Aviso: " + string.Join("; ", Avisos);
                }

                return "Pronta";
            }
        }

        public void NotifyValidationChanged()
        {
            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(Valida));
        }

        internal void BeginRevision()
        {
            _originalNameBeforeRevision = NomeArquivo;
            _hasRevisionSnapshot = true;
            _revisionFailureKind = RevisionNameFailureKind.None;
            ErroRevisao = null;
        }

        internal void CompleteRevision(string revisedName)
        {
            NomeArquivo = revisedName;
            _revisionFailureKind = RevisionNameFailureKind.None;
            ErroRevisao = null;
        }

        internal void FailRevision(
            string error,
            RevisionNameFailureKind failureKind)
        {
            _revisionFailureKind = failureKind;
            ErroRevisao = error;
        }

        internal void CancelRevision()
        {
            ResetRevision(true);
        }

        internal void ResetRevision(bool restoreOriginalName)
        {
            if (restoreOriginalName && _hasRevisionSnapshot)
            {
                NomeArquivo = _originalNameBeforeRevision;
            }

            _originalNameBeforeRevision = null;
            _hasRevisionSnapshot = false;
            _revisionFailureKind = RevisionNameFailureKind.None;
            ErroRevisao = null;
            SubirRevisao = false;
        }

        private void SetBoolean(
            ref bool field,
            bool value,
            string propertyName)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            RaisePropertyChanged(propertyName);
        }

        private void SetValidationError(
            ref string field,
            string value,
            string propertyName)
        {
            if (string.Equals(field, value, StringComparison.Ordinal))
            {
                return;
            }

            field = value;
            RaisePropertyChanged(propertyName);
            NotifyValidationChanged();
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
