using System;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SheetZoomService
    {
        private readonly IZwcadContext _zwcad;

        public SheetZoomService(IZwcadContext zwcad)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
        }

        public void ZoomTo(FolhaInfo sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Nao existe desenho ativo.");

            ZoomWindow window = ZoomWindow.Create(sheet);
            _zwcad.WriteMessage(window.Describe(sheet));

            using (DocumentLock documentLock = document.LockDocument())
            {
                LayoutManager.Current.CurrentLayout = sheet.LayoutName;
                Editor editor = document.Editor;
                SwitchToPaperSpace(editor);
                ApplyView(editor, window);
            }
        }

        private static void SwitchToPaperSpace(Editor editor)
        {
            try { editor.SwitchToPaperSpace(); }
            catch { }
        }

        private static void ApplyView(Editor editor, ZoomWindow window)
        {
            ViewTableRecord view = editor.GetCurrentView();
            try
            {
                window.FitTo(view.Width, view.Height);
                view.CenterPoint = window.Center;
                view.Width = window.Width;
                view.Height = window.Height;
                editor.SetCurrentView(view);
                editor.UpdateScreen();
                editor.Regen();
            }
            finally
            {
                view.Dispose();
            }
        }

        private sealed class ZoomWindow
        {
            private ZoomWindow(Point2d center, double width, double height)
            {
                Center = center;
                Width = width;
                Height = height;
            }

            public Point2d Center { get; private set; }
            public double Width { get; private set; }
            public double Height { get; private set; }

            public static ZoomWindow Create(FolhaInfo sheet)
            {
                double width = Math.Abs(sheet.Largura);
                double height = Math.Abs(sheet.Altura);
                double margin = Math.Max(20.0, Math.Max(width, height) * 0.05);
                return new ZoomWindow(
                    new Point2d(
                        (sheet.Limites.MinPoint.X + sheet.Limites.MaxPoint.X) / 2.0,
                        (sheet.Limites.MinPoint.Y + sheet.Limites.MaxPoint.Y) / 2.0),
                    Math.Max(1.0, width + margin * 2.0),
                    Math.Max(1.0, height + margin * 2.0));
            }

            public void FitTo(double viewportWidth, double viewportHeight)
            {
                if (viewportWidth <= 0.0 || viewportHeight <= 0.0) return;

                double viewportRatio = viewportWidth / viewportHeight;
                if (Width / Height > viewportRatio) Height = Width / viewportRatio;
                else Width = Height * viewportRatio;
            }

            public string Describe(FolhaInfo sheet)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Zoom folha {0:00}: layout={1}, bloco={2}, formato={3}, LL={4:0.###},{5:0.###}, UR={6:0.###},{7:0.###}, centro={8:0.###},{9:0.###}, janela={10:0.###}x{11:0.###}",
                    sheet.Sequencia, sheet.LayoutName, sheet.BlockName, sheet.Formato,
                    sheet.Limites.MinPoint.X, sheet.Limites.MinPoint.Y,
                    sheet.Limites.MaxPoint.X, sheet.Limites.MaxPoint.Y,
                    Center.X, Center.Y, Width, Height);
            }
        }
    }
}
