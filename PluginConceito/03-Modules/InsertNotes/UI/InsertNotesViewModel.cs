using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class InsertNotesViewModel : INotifyPropertyChanged
    {
        private bool _isUpdating;
        private bool _hasLevel1;
        private bool _hasLevel2;
        private DisciplineViewModel _selectedDiscipline;
        private string _searchText;
        private List<NoteViewModel> _allNotes = new List<NoteViewModel>();

        public ObservableCollection<DisciplineViewModel> Level0 { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<DisciplineViewModel> Level1 { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<DisciplineViewModel> Level2 { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<DisciplineViewModel> Stages { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<DisciplineViewModel> Importance { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<NoteViewModel> Notes { get; } = new ObservableCollection<NoteViewModel>();

        public bool HasNotes
        {
            get { return _allNotes.Count > 0; }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged();
                FilterNotes();
            }
        }

        private readonly Dictionary<string, string[]> _notesByDiscipline = new Dictionary<string, string[]>
        {
            {
                "ELETRICA E AFINS", new[]
                {
                    "NOTA DE INSTALACOES ELETRICAS CONFORME PROJETO.",
                    "NOTA DE QUADROS DE DISTRIBUICAO CONFORME DIAGRAMA.",
                    "NOTA DE ATERRAMENTO CONFORME NBR 5410.",
                    "NOTA DE CONDUTORES ISOLADOS ANTICHAMA.",
                    "NOTA DE ELETRODUTOS EMBUTIDOS NA ALVENARIA.",
                    "NOTA DE CAIXAS DE PASSAGEM CONFORME DETALHE.",
                    "NOTA DE DISJUNTORES CONFORME DIAGRAMA UNIFILAR.",
                    "NOTA DE BARRAMENTO DE TERRA PRINCIPAL.",
                    "NOTA DE PROTECAO CONTRA SURTOS CONFORME NBR 5410."
                }
            },
            {
                "ELETRICA", new[]
                {
                    "NOTA DE ELETRODUTO FLEXIVEL.",
                    "NOTA DE ELETRODUTO RIGIDO.",
                    "NOTA DE PROJETO FEITO SOB NORMA 5410.",
                    "NOTA DE CABOS ISOLADOS PARA REDE INTERNA.",
                    "NOTA DE QUADROS PARCIAIS CONFORME PROJETO.",
                    "NOTA DE TOMADAS DE USO GERAL E ESPECIFICO.",
                    "NOTA DE ILUMINACAO LED CONFORME PROJETO.",
                    "NOTA DE SENSORES DE PRESENCA NAS AREAS COMUNS.",
                    "NOTA DE BACKUP EM NO-BREAK PARA CIRCUITOS CRITICOS."
                }
            },
            {
                "SPDA", new[]
                {
                    "NOTA DE SPDA CONFORME NBR 5419.",
                    "NOTA DE MALHA DE ATERRAMENTO CONFORME PROJETO.",
                    "NOTA DE DESCIDAS EM NUMERO SUFICIENTE.",
                    "NOTA DE CAPTORES TIPO FRANKLIN CONFORME AREA.",
                    "NOTA DE ANEL DE TERRA NA BASE DO EDIFICIO.",
                    "NOTA DE EQUALIZACAO DE POTENCIAIS.",
                    "NOTA DE PROTECAO CONTRA DESCARGAS ATMOSFERICAS.",
                    "NOTA DE CONEXOES DE DESCIDA COM CONECTOR APROPRIADO.",
                    "NOTA DE MEDICAO DE RESISTENCIA DE TERRA APOS INSTALACAO."
                }
            },
            {
                "ENTRADA DE ENERGIA", new[]
                {
                    "NOTA DE ENTRADA DE ENERGIA CONFORME CONCESSIONARIA.",
                    "NOTA DE MEDICAO AGRUPADA CONFORME PADRAO.",
                    "NOTA DE RAMAL DE LIGACAO SUBTERRANEO.",
                    "NOTA DE POSTE DE ENTRADA CONFORME NORMA LOCAL.",
                    "NOTA DE CABINE DE BARRAMENTO PRINCIPAL.",
                    "NOTA DE TRANSFORMADOR CONFORME CARGA INSTALADA.",
                    "NOTA DE CHAVE SECCIONADORA NA ENTRADA.",
                    "NOTA DE PROJETO APROVADO PELA CONCESSIONARIA.",
                    "NOTA DE GERADOR DE EMERGENCIA CONFORME PROJETO."
                }
            },
            {
                "TELECOM", new[]
                {
                    "NOTA DE INFRAESTRUTURA PARA OPERADORAS.",
                    "NOTA DE CAIXAS DE PASSAGEM CONFORME PROJETO.",
                    "NOTA DE CABEAMENTO CATEGORIA 6.",
                    "NOTA DE RACK DE DISTRIBUICAO PRINCIPAL.",
                    "NOTA DE PATCH PANEL COM IDENTIFICACAO.",
                    "NOTA DE REDE WI-FI CORPORATIVA.",
                    "NOTA DE SWITCHES GERENCIAVEIS.",
                    "NOTA DE CONTROLE DE ACESSO POR BIOMETRIA.",
                    "NOTA DE MONITORAMENTO POR CFTV INTEGRADO."
                }
            },
            {
                "CABEAMENTO ESTRUTURADO", new[]
                {
                    "NOTA DE PATCH PANEL 24 PORTAS.",
                    "NOTA DE CERTIFICACAO DE CABEAMENTO.",
                    "NOTA DE IDENTIFICACAO DE PONTOS CONFORME PROJETO.",
                    "NOTA DE CABOS UTP CAT-6 LIVRES DE HALOGENIOS.",
                    "NOTA DE GUIAS E PASSAGENS DEDICADAS PARA DADOS.",
                    "NOTA DE PONTOS DE TELECOM POR ESTACAO DE TRABALHO.",
                    "NOTA DE ORGANIZADORES HORIZONTAIS NOS RACKS.",
                    "NOTA DE CERTIFICACAO COM EQUIPAMENTO CALIBRADO.",
                    "NOTA DE BACKBONE DE FIBRA OPTICA MULTIMODO."
                }
            },
            {
                "CFTV", new[]
                {
                    "NOTA DE CAMERAS CONFORME PROJETO DE COBERTURA.",
                    "NOTA DE DVR COM ARMAZENAMENTO MINIMO DE 30 DIAS.",
                    "NOTA DE FONTE DE ALIMENTACAO CENTRALIZADA.",
                    "NOTA DE CAMERAS IP COM RESOLUCAO FULL HD.",
                    "NOTA DE GRAVACAO ININTERRUPTA 24 HORAS.",
                    "NOTA DE MONITORES DE VISUALIZACAO NA PORTARIA.",
                    "NOTA DE ACESSO REMOTO PARA MONITORAMENTO.",
                    "NOTA DE INFRAESTRUTURA DE CABOS SEPARADOS.",
                    "NOTA DE NO-BREAK EXCLUSIVO PARA SISTEMA CFTV."
                }
            },
            {
                "TELEFONIA", new[]
                {
                    "NOTA DE PONTOS TELEFONICOS CONFORME PROJETO.",
                    "NOTA DE CENTRAL TELEFONICA CONFORME ESPECIFICACAO.",
                    "NOTA DE CABEAMENTO TELEFONICO CI-50.",
                    "NOTA DE BLOCOS DE CONEXAO TIPO KRONE.",
                    "NOTA DE RAMAIS DIGITAIS PARA SETORES ADMINISTRATIVOS.",
                    "NOTA DE LINHAS TRONCO CONFORME DEMANDA.",
                    "NOTA DE ATENDIMENTO ELETRONICO DE CHAMADAS.",
                    "NOTA DE IDENTIFICACAO DE CHAMADAS (BINA).",
                    "NOTA DE INTERLIGACAO COM PORTARIA E RECEPCAO."
                }
            },
            {
                "INTERFONIA", new[]
                {
                    "NOTA DE INTERFONE COM TRAVA ELETRICA.",
                    "NOTA DE BOTOEIRA DE ACIONAMENTO POR APARTAMENTO.",
                    "NOTA DE FONTE DE ALIMENTACAO PARA INTERFONIA.",
                    "NOTA DE CENTRAL DE PORTARIA ELETRONICA.",
                    "NOTA DE MONITORES INDIVIDUAIS COLORIDOS.",
                    "NOTA DE CABEAMENTO PROPRIO PARA VIDEO.",
                    "NOTA DE FECHADURA ELETROIMANETICA.",
                    "NOTA DE INTERCOMUNICACAO ENTRE APARTAMENTOS.",
                    "NOTA DE INTEGRACAO COM CONTROLE DE ACESSO."
                }
            },
            {
                "CATV", new[]
                {
                    "NOTA DE DISTRIBUICAO DE SINAL A CABO.",
                    "NOTA DE AMPLIFICADOR DE SINAL CONFORME NECESSARIO.",
                    "NOTA DE CABEAMENTO COAXIAL RG-6.",
                    "NOTA DE SPLITTERS E DERIVADORES CONFORME PROJETO.",
                    "NOTA DE PONTOS DE TV POR UNIDADE HABITACIONAL.",
                    "NOTA DE ANTENA COLETIVA DIGITAL.",
                    "NOTA DE CABECAL DE DISTRIBUICAO PRINCIPAL.",
                    "NOTA DE ATENUACAO DE SINAL DENTRO DOS LIMITES.",
                    "NOTA DE CONECTORES TIPO F COMPRIMIDOS."
                }
            },
            {
                "HIDRAULICA", new[]
                {
                    "NOTA DE TUBULACAO DE AGUA FRIA CONFORME PROJETO.",
                    "NOTA DE RESERVATORIO SUPERIOR E INFERIOR.",
                    "NOTA DE BARRILETE E COLUNAS DE DISTRIBUICAO.",
                    "NOTA DE AQUECEDOR CENTRAL CONFORME PROJETO.",
                    "NOTA DE TUBULACAO DE AGUA QUENTE ISOLADA TERMICAMENTE.",
                    "NOTA DE CAIXAS DE DESCARGA DUPLO ACIONAMENTO.",
                    "NOTA DE REGISTROS DE GAVETA E PRESSOES.",
                    "NOTA DE ESGOTO COM CAIXAS DE INSPECAO.",
                    "NOTA DE VENTILACAO PRIMARIA E SECUNDARIA."
                }
            },
            {
                "PPCI", new[]
                {
                    "NOTA DE HIDRANTES CONFORME NBR 13714.",
                    "NOTA DE SPINKLERS CONFORME PROJETO.",
                    "NOTA DE BOMBA DE INCENDIO CONFORME ESPECIFICACAO.",
                    "NOTA DE RESERVA TECNICA DE INCENDIO DIMENSIONADA.",
                    "NOTA DE DETECTORES DE FUMACA ENDERECAVEIS.",
                    "NOTA DE CENTRAL DE ALARME DE INCENDIO.",
                    "NOTA DE SIRENES AUDIOVISUAIS POR PAVIMENTO.",
                    "NOTA DE EXTINTORES DISTRIBUIDOS CONFORME NORMA.",
                    "NOTA DE SINALIZACAO DE ROTA DE FUGA FOTOLUMINESCENTE."
                }
            },
            {
                "GAS", new[]
                {
                    "NOTA DE TUBULACAO DE GAS CONFORME NBR 15526.",
                    "NOTA DE MEDIDORES INDIVIDUAIS POR UNIDADE.",
                    "NOTA DE VENTILACAO PERMANENTE CONFORME NORMA.",
                    "NOTA DE ABRIGO DE GAS CONFORME DETALHE.",
                    "NOTA DE TUBOS DE COBRE OU ACO GALVANIZADO.",
                    "NOTA DE VALVULAS DE BLOQUEIO POR UNIDADE.",
                    "NOTA DE PRUMADA DE GAS EXTERNA AO EDIFICIO.",
                    "NOTA DE TESTE DE ESTANQUEIDADE NA REDE.",
                    "NOTA DE REGULADOR DE PRESSAO PRIMARIO E SECUNDARIO."
                }
            },
            {
                "MECANICA", new[]
                {
                    "NOTA DE DUTOS DE AR CONDICIONADO CONFORME PROJETO.",
                    "NOTA DE UNIDADES CONDENSADORAS CONFORME LOCAL.",
                    "NOTA DE EXAUSTAO MECANICA CONFORME NBR 16401.",
                    "NOTA DE VENTILACAO DE GARAGENS CONFORME NORMA.",
                    "NOTA DE PRESSURIZACAO DE ESCADAS DE EMERGENCIA.",
                    "NOTA DE DIFUSORES E GRELHAS DE INSUFLAMENTO.",
                    "NOTA DE ISOLAMENTO TERMICO DE DUTOS.",
                    "NOTA DE CHILLERS E TORRES DE RESFRIAMENTO.",
                    "NOTA DE SISTEMA DE AUTOMACAO PREDIAL."
                }
            },
            {
                "INFRAESTRUTURA", new[]
                {
                    "NOTA DE ELETRODUTOS ENTERRADOS CONFORME PROJETO.",
                    "NOTA DE POCOS DE VISITA CONFORME DETALHE.",
                    "NOTA DE CAIXAS DE PASSAGEM TIPO CONCRETO.",
                    "NOTA DE BANCO DE DUTOS PARA REDE DE DISTRIBUICAO.",
                    "NOTA DE PAVIMENTACAO ASFALTICA CONFORME PROJETO.",
                    "NOTA DE GUIA E SARJETA EM CONCRETO.",
                    "NOTA DE DRENAGEM PLUVIAL COM BOCA DE LOBO.",
                    "NOTA DE MURO DE ARRIMO CONFORME CALCULO ESTRUTURAL.",
                    "NOTA DE ILUMINACAO EXTERNA EM POSTES DECORATIVOS."
                }
            },
            {
                "ESTRUTURA", new[]
                {
                    "NOTA DE CONCRETO ARMADO CONFORME PROJETO ESTRUTURAL.",
                    "NOTA DE ACO CA-50 E CA-60 CONFORME ESPECIFICACAO.",
                    "NOTA DE FUNDACOES CONFORME SONDAGEM DO TERRENO.",
                    "NOTA DE LAJES NERVURADAS CONFORME DETALHE.",
                    "NOTA DE PILARES E VIGAS DIMENSIONADOS CONFORME CALCULO.",
                    "NOTA DE COBRIENTO DE ARMADURA CONFORME CLASSE DE AGRESSIVIDADE.",
                    "NOTA DE CONTROLE TECNOLOGICO DO CONCRETO.",
                    "NOTA DE ESTRUTURA METALICA COM PINTURA INTUMESCENTE.",
                    "NOTA DE JUNTAS DE DILATACAO CONFORME PROJETO."
                }
            }
        };

        public bool HasLevel1
        {
            get { return _hasLevel1; }
            private set { _hasLevel1 = value; OnPropertyChanged(); }
        }

        public bool HasLevel2
        {
            get { return _hasLevel2; }
            private set { _hasLevel2 = value; OnPropertyChanged(); }
        }

        public InsertNotesViewModel()
        {
            List<DisciplineViewModel> tree = BuildTree();

            foreach (DisciplineViewModel item in tree)
            {
                Level0.Add(item);
                WireUp(item);
            }

            foreach (DisciplineViewModel stage in BuildStages())
            {
                Stages.Add(stage);
                stage.PropertyChanged += OnExclusivePropertyChanged;
            }

            foreach (DisciplineViewModel importance in BuildImportance())
            {
                Importance.Add(importance);
                importance.PropertyChanged += OnImportancePropertyChanged;
            }
        }

        private void WireUp(DisciplineViewModel item)
        {
            item.PropertyChanged += OnDisciplinePropertyChanged;

            foreach (DisciplineViewModel child in item.Children)
            {
                WireUp(child);
            }
        }

        private void OnDisciplinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DisciplineViewModel.IsChecked))
            {
                return;
            }

            if (_isUpdating)
            {
                return;
            }

            var discipline = (DisciplineViewModel)sender;

            if (Level0.Contains(discipline))
            {
                if (discipline.IsChecked)
                {
                    _isUpdating = true;

                    foreach (DisciplineViewModel sibling in Level0)
                    {
                        if (sibling != discipline)
                        {
                            sibling.IsChecked = false;
                        }
                    }

                    _isUpdating = false;

                    Level1.Clear();
                    Level2.Clear();
                    HasLevel1 = false;
                    HasLevel2 = false;

                    foreach (DisciplineViewModel child in discipline.Children)
                    {
                        Level1.Add(child);
                    }

                    HasLevel1 = Level1.Count > 0;
                    _selectedDiscipline = discipline;
                    RefreshNotes();
                }
                else
                {
                    Level1.Clear();
                    Level2.Clear();
                    HasLevel1 = false;
                    HasLevel2 = false;
                    _selectedDiscipline = null;
                    Notes.Clear();
                    _allNotes.Clear();
                    _searchText = null;
                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(HasNotes));
                }
            }
            else if (Level1.Contains(discipline))
            {
                if (discipline.IsChecked)
                {
                    _isUpdating = true;

                    foreach (DisciplineViewModel sibling in Level1)
                    {
                        if (sibling != discipline)
                        {
                            sibling.IsChecked = false;
                        }
                    }

                    _isUpdating = false;

                    Level2.Clear();
                    HasLevel2 = false;

                    foreach (DisciplineViewModel child in discipline.Children)
                    {
                        Level2.Add(child);
                    }

                    HasLevel2 = Level2.Count > 0;
                    _selectedDiscipline = discipline;
                    RefreshNotes();
                }
                else
                {
                    Level2.Clear();
                    HasLevel2 = false;
                    _selectedDiscipline = GetSelectedAncestor(discipline);
                    RefreshNotes();
                }
            }
            else if (Level2.Contains(discipline))
            {
                if (discipline.IsChecked)
                {
                    _isUpdating = true;

                    foreach (DisciplineViewModel sibling in Level2)
                    {
                        if (sibling != discipline)
                        {
                            sibling.IsChecked = false;
                        }
                    }

                    _isUpdating = false;
                    _selectedDiscipline = discipline;
                    RefreshNotes();
                }
                else
                {
                    _selectedDiscipline = GetSelectedAncestor(discipline);
                    RefreshNotes();
                }
            }
        }

        private static List<DisciplineViewModel> BuildTree()
        {
            var telecom = new DisciplineViewModel("TELECOM");
            telecom.Children.Add(new DisciplineViewModel("CABEAMENTO ESTRUTURADO"));
            telecom.Children.Add(new DisciplineViewModel("CFTV"));
            telecom.Children.Add(new DisciplineViewModel("TELEFONIA"));
            telecom.Children.Add(new DisciplineViewModel("INTERFONIA"));
            telecom.Children.Add(new DisciplineViewModel("CATV"));

            var eletrica = new DisciplineViewModel("ELÉTRICA E AFINS");
            eletrica.Children.Add(new DisciplineViewModel("ELÉTRICA"));
            eletrica.Children.Add(telecom);
            eletrica.Children.Add(new DisciplineViewModel("SPDA"));
            eletrica.Children.Add(new DisciplineViewModel("ENTRADA DE ENERGIA"));

            return new List<DisciplineViewModel>
            {
                eletrica,
                new DisciplineViewModel("HIDRAULICA"),
                new DisciplineViewModel("PPCI"),
                new DisciplineViewModel("GÁS"),
                new DisciplineViewModel("MECANICA"),
                new DisciplineViewModel("INFRAESTRUTURA"),
                new DisciplineViewModel("ESTRUTURA")
            };
        }

        private static List<DisciplineViewModel> BuildStages()
        {
            return new List<DisciplineViewModel>
            {
                new DisciplineViewModel("EP"),
                new DisciplineViewModel("AP"),
                new DisciplineViewModel("EX"),
                new DisciplineViewModel("LO"),
                new DisciplineViewModel("AS")
            };
        }

        private static List<DisciplineViewModel> BuildImportance()
        {
            return new List<DisciplineViewModel>
            {
                new DisciplineViewModel("OBRIGATORIO"),
                new DisciplineViewModel("OPCIONAL")
            };
        }

        private void UpdateNotes(DisciplineViewModel discipline)
        {
            Notes.Clear();
            _allNotes.Clear();
            _searchText = null;
            OnPropertyChanged(nameof(SearchText));

            if (_notesByDiscipline.TryGetValue(discipline.Name, out string[] noteNames))
            {
                foreach (string noteName in noteNames)
                {
                    var note = new NoteViewModel(noteName);
                    _allNotes.Add(note);
                    Notes.Add(note);
                }
            }

            OnPropertyChanged(nameof(HasNotes));
        }

        private void FilterNotes()
        {
            Notes.Clear();

            string query = (_searchText ?? string.Empty).Trim().ToUpperInvariant();

            if (query.Length == 0)
            {
                foreach (NoteViewModel note in _allNotes)
                {
                    Notes.Add(note);
                }
            }
            else
            {
                string[] keywords = query.Split(' ');

                foreach (NoteViewModel note in _allNotes)
                {
                    string noteUpper = note.Name.ToUpperInvariant();
                    bool matches = false;

                    foreach (string keyword in keywords)
                    {
                        if (keyword.Length > 0 && noteUpper.Contains(keyword))
                        {
                            matches = true;
                            break;
                        }
                    }

                    if (matches)
                    {
                        Notes.Add(note);
                    }
                }
            }

            OnPropertyChanged(nameof(HasNotes));
        }

        private void RefreshNotes()
        {
            bool hasImportance = false;

            foreach (DisciplineViewModel imp in Importance)
            {
                if (imp.IsChecked)
                {
                    hasImportance = true;
                    break;
                }
            }

            if (_selectedDiscipline != null && hasImportance)
            {
                UpdateNotes(_selectedDiscipline);
            }
            else
            {
                Notes.Clear();
                _allNotes.Clear();
                _searchText = null;
                OnPropertyChanged(nameof(SearchText));
                OnPropertyChanged(nameof(HasNotes));
            }
        }

        private DisciplineViewModel GetSelectedAncestor(DisciplineViewModel child)
        {
            if (Level2.Contains(child) && HasLevel2)
            {
                foreach (DisciplineViewModel l1 in Level1)
                {
                    if (l1.IsChecked)
                    {
                        return l1;
                    }
                }
            }

            if (Level1.Contains(child) && HasLevel1)
            {
                foreach (DisciplineViewModel l0 in Level0)
                {
                    if (l0.IsChecked)
                    {
                        return l0;
                    }
                }
            }

            return null;
        }

        private void OnImportancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DisciplineViewModel.IsChecked))
            {
                return;
            }

            RefreshNotes();
        }

        private void OnExclusivePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DisciplineViewModel.IsChecked))
            {
                return;
            }

            if (_isUpdating)
            {
                return;
            }

            var item = (DisciplineViewModel)sender;

            if (!item.IsChecked)
            {
                return;
            }

            _isUpdating = true;

            ObservableCollection<DisciplineViewModel> collection = Stages.Contains(item) ? Stages : Importance;

            foreach (DisciplineViewModel sibling in collection)
            {
                if (sibling != item)
                {
                    sibling.IsChecked = false;
                }
            }

            _isUpdating = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Validate()
        {
            DisciplineViewModel selectedDiscipline = null;

            foreach (DisciplineViewModel level0 in Level0)
            {
                if (level0.IsChecked)
                {
                    selectedDiscipline = level0;
                    break;
                }
            }

            if (selectedDiscipline == null)
            {
                return "Selecione uma disciplina.";
            }

            DisciplineViewModel selectedStage = null;

            foreach (DisciplineViewModel stage in Stages)
            {
                if (stage.IsChecked)
                {
                    selectedStage = stage;
                    break;
                }
            }

            if (selectedStage == null)
            {
                return "Selecione uma etapa de projeto.";
            }

            return null;
        }

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
