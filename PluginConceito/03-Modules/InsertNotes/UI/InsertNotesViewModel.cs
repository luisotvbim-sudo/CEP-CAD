using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class InsertNotesViewModel
    {
        public ObservableCollection<DisciplineViewModel> AllDisciplines { get; }

        public InsertNotesViewModel()
        {
            List<DisciplineViewModel> root = BuildTree();
            AllDisciplines = new ObservableCollection<DisciplineViewModel>(Flatten(root));
        }

        private static List<DisciplineViewModel> BuildTree()
        {
            var telecom = new DisciplineViewModel("TELECOM", 1);
            telecom.Children.Add(new DisciplineViewModel("CABEAMENTO ESTRUTURADO", 2));
            telecom.Children.Add(new DisciplineViewModel("CFTV", 2));
            telecom.Children.Add(new DisciplineViewModel("TELEFONIA", 2));
            telecom.Children.Add(new DisciplineViewModel("INTERFONIA", 2));
            telecom.Children.Add(new DisciplineViewModel("CATV", 2));

            var eletrica = new DisciplineViewModel("ELÉTRICA E AFINS", 0);
            eletrica.Children.Add(telecom);
            eletrica.Children.Add(new DisciplineViewModel("SPDA", 1));
            eletrica.Children.Add(new DisciplineViewModel("ENTRADA DE ENERGIA", 1));

            return new List<DisciplineViewModel>
            {
                eletrica,
                new DisciplineViewModel("HIDRAULICA", 0),
                new DisciplineViewModel("PPCI", 0),
                new DisciplineViewModel("GÁS", 0),
                new DisciplineViewModel("MECANICA", 0),
                new DisciplineViewModel("INFRAESTRUTURA", 0),
                new DisciplineViewModel("ESTRUTURA", 0)
            };
        }

        private static List<DisciplineViewModel> Flatten(List<DisciplineViewModel> nodes)
        {
            var result = new List<DisciplineViewModel>();
            foreach (DisciplineViewModel node in nodes)
            {
                result.Add(node);
                result.AddRange(Flatten(node.Children));
            }
            return result;
        }
    }
}
