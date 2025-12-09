using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Orion.Commands.LevelSectioner;
using Orion.Commands.SectionBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;


namespace Orion.Views
{
    /// <summary>
    /// Interaction logic for LevelSectioner.xaml
    /// </summary>
    public partial class LevelSectioner : Window
    {
        public UIDocument uidoc { get; }
        public Document doc { get; }
        private static WeakReference<LevelSectioner> _instanceRef;

        private ExternalEvent _sectionerEvent;
        private LevelSectionerHandler _sectionerManualHandler;

        private ExternalEvent _testerEvent;
        private LevelSectionerTestHandler _testerHandler;

        private XYZ copiedMin;
        private XYZ copiedMax;

        public LevelSectioner(UIDocument uiDoc)
        {
            uidoc = uiDoc;
            doc = uiDoc.Document;
            InitializeComponent();

            _sectionerManualHandler = new LevelSectionerHandler();
            _sectionerEvent = ExternalEvent.Create(_sectionerManualHandler);

            _testerHandler = new LevelSectionerTestHandler();
            _testerEvent = ExternalEvent.Create(_testerHandler);


            Setup(doc);
        }

        private void Setup(Document doc)
        {
            IEnumerable<Level> levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OrderBy(level => (level as Level).Elevation)
                .Cast<Level>();

            List<ComboItem> comboboxLevelItems = levels
                .Select(level => new ComboItem { elem = level, Name = level.Name, id = level.Id })
                .ToList();

            ComboItem fromDefault = comboboxLevelItems.FirstOrDefault();
            ComboItem toDefault = comboboxLevelItems.Skip(1).FirstOrDefault();

            FromLevels.ItemsSource = comboboxLevelItems;
            FromLevels.SelectedIndex = 0;
            ToLevels.ItemsSource = comboboxLevelItems;
            ToLevels.SelectedIndex = comboboxLevelItems.Count > 1 ? 1 : 0;

        }

        public static void ShowOrActivate(UIApplication uiApp, UIDocument uiDoc)
        {
            // 1) try get existing instance
            if (_instanceRef != null && _instanceRef.TryGetTarget(out var existing) && existing != null)
            {
                // if window is loaded (not disposed)
                if (existing.IsLoaded)
                {
                    // ensure it has Revit as owner (important for z-order & modality)
                    new WindowInteropHelper(existing) { Owner = uiApp.MainWindowHandle };

                    // make sure it's visible and not minimized
                    if (!existing.IsVisible) existing.Show();
                    if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;

                    // attempt to bring to front: Activate() and brief Topmost toggle (works around Windows focus rules)
                    existing.Activate();
                    existing.Topmost = true;
                    existing.Topmost = false;
                    existing.Focus();

                    return; // <--- DO NOT create a new window
                }
            }

            // 2) no usable instance found — create one and store weak ref
            var wnd = new LevelSectioner(uiDoc);
            new WindowInteropHelper(wnd) { Owner = uiApp.MainWindowHandle }; // parent to Revit
            wnd.Show();
            _instanceRef = new WeakReference<LevelSectioner>(wnd);
        }

        private class ComboItem
        {
            public Level elem { get; set; }
            public string Name { get; set; }

            public ElementId id { get; set; }
        }

        private void GenerateManual(object sender, RoutedEventArgs e)
        {
            _sectionerManualHandler.fromLevel = (FromLevels.SelectedItem as ComboItem).elem as Level;
            _sectionerManualHandler.toLevel = (ToLevels.SelectedItem as ComboItem).elem as Level;
            _sectionerManualHandler.xPos = ParseOrDefault(XPos.Text);
            _sectionerManualHandler.xNeg = ParseOrDefault(XNeg.Text);
            _sectionerManualHandler.yPos = ParseOrDefault(YPos.Text);
            _sectionerManualHandler.yNeg = ParseOrDefault(YNeg.Text);
            _sectionerEvent.Raise();
        }

        private void GenerateCopy(object sender, RoutedEventArgs e)
        {
            if (copiedMin == null || copiedMax == null) {
                TaskDialog.Show("Report", "Click copy to copy current 3d active view XY Section box");
                return;
            }
            
            _sectionerManualHandler.fromLevel = (FromLevels.SelectedItem as ComboItem).elem as Level;
            _sectionerManualHandler.toLevel = (ToLevels.SelectedItem as ComboItem).elem as Level;
            _sectionerManualHandler.xPos = copiedMax.X;
            _sectionerManualHandler.xNeg = -copiedMin.X;
            _sectionerManualHandler.yPos = copiedMax.Y;
            _sectionerManualHandler.yNeg = -copiedMin.Y;
            _sectionerEvent.Raise();
        }

        private void CopyBox(object sender, RoutedEventArgs e)
        {
            if (doc.ActiveView is not View3D)
            {
                TaskDialog.Show("Warning", "Please set current active view to a 3d View");
                return;
            }
            View3D view3d = doc.ActiveView as View3D;

            BoundingBoxXYZ bbox = view3d.GetSectionBox();
            copiedMin = bbox.Min;
            copiedMax = bbox.Max;

            CopiedMinLabel.Content = $"Min: ({Math.Round(copiedMin.X, 2)}, {Math.Round(copiedMin.Y, 2)})";
            CopiedMaxLabel.Content = $"Min: ({Math.Round(copiedMax.X, 2)}, {Math.Round(copiedMax.Y, 2)})";
        }

        private void TestBox(object sender, RoutedEventArgs e)
        {
            if (doc.ActiveView is not View3D view3d)
            {
                TaskDialog.Show("Warning", "Please set current active view to a 3d View");
                return;
            }

            _testerHandler.xPos = ParseOrDefault(XPos.Text);
            _testerHandler.xNeg = ParseOrDefault(XNeg.Text);
            _testerHandler.yPos = ParseOrDefault(YPos.Text);
            _testerHandler.yNeg = ParseOrDefault(YNeg.Text);
            _testerEvent.Raise();
        }

        private double ParseOrDefault(string text, double defaultValue = 0.0)
        {
            return double.TryParse(text, out double result) ? result : defaultValue;
        }

        public void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                Regex regex = new Regex("[^0-9.-]+"); // Use appropriate regex
                if (regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void CloseMenu(object sender, RoutedEventArgs e)
        {
            _instanceRef = null;

            // close the window
            this.Close();
        }
    }


}
