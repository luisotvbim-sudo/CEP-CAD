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
        private string _nomeArquivo;
        private string _erroNome;

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
            set
            {
                if (_plotar == value)
                {
                    return;
                }

                _plotar = value;
                RaisePropertyChanged(nameof(Plotar));
            }
        }

        public bool GerarDwg
        {
            get { return _gerarDwg; }
            set
            {
                if (_gerarDwg == value)
                {
                    return;
                }

                _gerarDwg = value;
                RaisePropertyChanged(nameof(GerarDwg));
            }
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
            set
            {
                if (string.Equals(_erroNome, value, StringComparison.Ordinal))
                {
                    return;
                }

                _erroNome = value;
                RaisePropertyChanged(nameof(ErroNome));
                RaisePropertyChanged(nameof(Status));
                RaisePropertyChanged(nameof(Valida));
            }
        }

        public bool Valida
        {
            get { return Erros.Count == 0 && string.IsNullOrWhiteSpace(ErroNome); }
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

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
