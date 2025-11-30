using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Orion.Views
{
    /// <summary>
    /// Interaction logic for FilterPlus.xaml
    /// </summary>
    public partial class FilterPlus : Window
    {
        public UIDocument uidoc { get; }
        public Document doc { get; }

        private static WeakReference<FilterPlus> _instanceRef;

        public FilterPlus(UIDocument uiDoc)
        {

            InitializeComponent();

            // For making sure instance of window is single
            this.Closed += (s, e) => _instanceRef = null;

            Setup(uiDoc);



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
            var wnd = new FilterPlus(uiDoc);
            new WindowInteropHelper(wnd) { Owner = uiApp.MainWindowHandle }; // parent to Revit
            wnd.Show();
            _instanceRef = new WeakReference<FilterPlus>(wnd);
        }



        public class TypeGroup
        {
            public string TypeName { get; set; }
            public List<Element> Elements { get; set; } = new List<Element>();
        }

        public void Setup(UIDocument uidoc)
        {
            IEnumerable<Element> sel = uidoc.Selection.GetElementIds().Select(id => doc.GetElement(id) as Element);

            var dict = new Dictionary<string, Dictionary<string, List<Element>> >();

            foreach (var el in sel)
            {   
                // Check Category
                string catName = el.Category?.Name ?? "<No Category>";
                if (!dict.ContainsKey(catName)) dict[catName] = new Dictionary<string, List<Element>>();

                // Check Family Type
                Element type = doc.GetElement(el.GetTypeId());
                string typeName = type?.Name ?? "<No Type>";


                if (!dict[catName].ContainsKey(typeName)) dict[catName][typeName] = new List<Element>();

                // Add Instance to Family Type list
                dict[catName][typeName].Add(el);
            }



            TreeViewItem root = new()
            {
                IsExpanded = true,
                Header = CreateHeader("root", "All Layers", isChecked: true, threeState: true)
            };



            // Layer A
            TreeViewItem layerA = new()
            {
                IsExpanded = true,
                Header = CreateHeader("layer_a", "Layer A", threeState: true)
            };

            // Layer A Children
                TreeViewItem a1 = new() {
                    Header = CreateHeader("layer_a_1", "A.1")
                };
                TreeViewItem a2 = new()
                {
                    Header=CreateHeader("layer_a_2", "A.2", isChecked: true)
                };
                layerA.Items.Add(a1);
                layerA.Items.Add(a2);



            // Layer B
            TreeViewItem layerB = new()
            {
                Header = CreateHeader("layer_b", "Layer B")
            };

                // Layer B Children
                TreeViewItem b1 = new()
                {
                    Header = CreateHeader("layer_b_1", "B.1")
                };
                layerB.Items.Add(b1);

            root.Items.Add(layerA);
            root.Items.Add(layerB);

            FilterTree.Items.Clear();
            FilterTree.Items.Add(root);
        }


        StackPanel CreateHeader(string tag, string text, bool isChecked = false, bool threeState = false)
        {
            StackPanel stack = new() { Orientation = Orientation.Horizontal };
            CheckBox checkbox = new()
            {
                Tag = tag,
                IsThreeState = threeState,
                IsChecked = isChecked,
                VerticalAlignment = VerticalAlignment.Center,
            };

            TextBlock textBox = new()
            {
                Text = text,
                Margin = new Thickness(0, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            stack.Children.Add(checkbox);
            stack.Children.Add(textBox);

            return stack;
        }
    }

}