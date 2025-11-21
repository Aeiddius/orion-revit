using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Orion.Commands.SectionBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;


namespace Orion.Views
{
    /// <summary>
    /// Interaction logic for SectionMenu.xaml
    /// </summary>
    public partial class SectionMenu : Window
    {
        public UIDocument uidoc { get; }
        public Document doc { get; }
        private static WeakReference<SectionMenu> _instanceRef;

        private ExternalEvent _adjustEvent;
        private AdjustSectionBoxHandler _adjustHandler;

        private ExternalEvent _setEvent;
        private SetSectionBoxHandler _setHandler;

        private ExternalEvent _toggleEvent;
        private ToggleSectionBoxHandler _toggleHandler;

        private static readonly ForgeTypeId[] metricUnits = new[] { UnitTypeId.Millimeters, UnitTypeId.Centimeters, UnitTypeId.Meters };
        private static readonly ForgeTypeId[] imperialUnits = new[] { UnitTypeId.Feet, UnitTypeId.FeetFractionalInches, UnitTypeId.Inches, UnitTypeId.FractionalInches };

        public SectionMenu(UIDocument uiDoc)
        {
            uidoc = uiDoc;
            doc = uiDoc.Document;
            InitializeComponent();
            
            // For making sure instance of window is single
            this.Closed += (s, e) => _instanceRef = null;


            _adjustHandler = new AdjustSectionBoxHandler();
            _adjustEvent = ExternalEvent.Create(_adjustHandler);

            _setHandler = new SetSectionBoxHandler();
            _setEvent = ExternalEvent.Create(_setHandler);

            _toggleHandler = new ToggleSectionBoxHandler();
            _toggleEvent = ExternalEvent.Create(_toggleHandler);

            var renderImages = new FilteredElementCollector(doc)
                .OfClass(typeof(ImageType))
                .Cast<ImageType>()
                .ToList();

            HashSet<ElementId> viewsWithRenders = renderImages
                .Where(img => img.OwnerViewId != ElementId.InvalidElementId)
                .Select(img => img.OwnerViewId)
                .ToHashSet();

            // Set the Views Combobox
            List<ComboItem> views = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(v => !v.IsTemplate && !v.IsLocked && !viewsWithRenders.Contains(v.Id))
                .Select(v => new ComboItem { Id = v.Id, Name = v.Name })
                .ToList();

            Combobox3DViews.ItemsSource = views;

            ComboItem default3D = views.FirstOrDefault(v => v.Name.Contains($"3D - {doc.Application.Username}") || v.Name.Contains("{3D}"));
            if (default3D != null) Combobox3DViews.SelectedValue = default3D.Id;
            else if (views.Count > 0) Combobox3DViews.SelectedIndex = 0;
            else Combobox3DViews.SelectedIndex = -1;


            // Set the View Template Combobox
            List<ComboItem> viewTemplates = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(v => v.IsTemplate && !string.IsNullOrWhiteSpace(v.Name))
                .Select(v => new ComboItem { Id = v.Id, Name = v.Name })
                .ToList();

            ComboboxTemplate.ItemsSource = views;

            if (views.Count > 0) ComboboxTemplate.SelectedIndex = 0;
            else ComboboxTemplate.SelectedIndex = -1;

            // Default Step
            SetDefaultStep();
        }

        private class ComboItem
        {
            public ElementId Id { get; set; }
            public string Name { get; set; }
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
            var wnd = new SectionMenu(uiDoc);
            new WindowInteropHelper(wnd) { Owner = uiApp.MainWindowHandle }; // parent to Revit
            wnd.Show();
            _instanceRef = new WeakReference<SectionMenu>(wnd);
        }


        private void GrowSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(val, val, val);
            _adjustEvent.Raise();
        }
        private void GrowXSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(val, 0.0, 0.0);
            _adjustEvent.Raise();
        }

        private void GrowYSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(0.0, val, 0.0);
            _adjustEvent.Raise();
        }

        private void GrowZSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(0.0, 0.0, val);
            _adjustEvent.Raise();
        }

        private void ShrinkSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(-val, -val, -val);
            _adjustEvent.Raise();
        }
        private void ShrinkXSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(-val, 0.0, 0.0);
            _adjustEvent.Raise();
        }

        private void ShrinkYSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(0.0, -val, 0.0);
            _adjustEvent.Raise();
        }

        private void ShrinkZSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            double val = ParseInput(StepInput.Text);
            _adjustHandler.Delta = new XYZ(0.0, 0.0, -val);
            _adjustEvent.Raise();
        }

        private void SetDefaultStep()
        {
            Units docUnits = doc.GetUnits();
            FormatOptions fmt = docUnits.GetFormatOptions(SpecTypeId.Length);
            ForgeTypeId displayUnitId = fmt.GetUnitTypeId();
            var map = new Dictionary<ForgeTypeId, string>
            {
                { UnitTypeId.Millimeters, "1000" },
                { UnitTypeId.Centimeters, "100"  },
                { UnitTypeId.Meters,      "1"    },
                { UnitTypeId.Feet, "1'-0\"" },
                { UnitTypeId.FeetFractionalInches, "1'-0\"" },
                { UnitTypeId.Inches, "12\"" },
                { UnitTypeId.FractionalInches, "12\"" },
            };
            StepInput.Text = map.TryGetValue(displayUnitId, out var v) ? v : "1";

        }

        private double ParseInput(string stringInput)
        {

            if (string.IsNullOrEmpty(stringInput)) SetDefaultStep();
            
            bool parsed = UnitFormatUtils.TryParse(
                doc.GetUnits(),
                SpecTypeId.Length,
                stringInput.Trim(),
                out double parsedValue,
                out string message
            );
            
            if (parsed) return parsedValue;
            return 1.0;
        }

        private void SetSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            string txt = SetSizeBox.Text?.Trim();

            string mode = (ComboboxSizeMethod.SelectedItem as ComboBoxItem)?.Tag as string; // "specific" or "elements"
            _setHandler.Mode = mode;
            _setHandler.Size = string.IsNullOrWhiteSpace(txt)
                ? (double?)null
                : ParseInput(txt);   // assumes ParseInput returns internal-units double and throws on invalid

            _setEvent.Raise();
        }

        private void ToggleSectionBoxCommand(object sender, RoutedEventArgs e)
        {
            _toggleEvent.Raise();
        }
    }
}
