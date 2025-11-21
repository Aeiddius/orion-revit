using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Orion.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop; // Required for WindowInteropHelper

namespace Orion.Commands.SectionBox
{
    public static class SectionBoxLogic
    {

        public static Result AdjustSize(Document doc, XYZ offset, string commandName)
        {
            if (doc.ActiveView.ViewType != ViewType.ThreeD) return Result.Cancelled;

            View3D view3D = doc.ActiveView as View3D;
            BoundingBoxXYZ bbox = view3D.GetSectionBox();

            XYZ newMin = bbox.Min - offset;
            XYZ newMax = bbox.Max + offset;

            // Check if too much shrink
            double minSize = 0.1;
            if ((newMax.X - newMin.X) < minSize ||
                (newMax.Y - newMin.Y) < minSize ||
                (newMax.Z - newMin.Z) < minSize)
            {
                return Result.Cancelled;
            }

            bbox.Min = newMin;
            bbox.Max = newMax;
            using (Transaction tx = new Transaction(doc, $"{commandName} of {view3D.Name}"))
            {
                tx.Start();
                view3D.SetSectionBox(bbox);
                tx.Commit();
            }
            return Result.Succeeded;
        }

        public static Result Toggle(Document doc, string commandName)
        { 
            View3D view3D = doc.ActiveView as View3D;
            string commandString = $"{commandName} of {view3D.Name}";

            using (TransactionGroup tg = new TransactionGroup(doc, commandName))
            {
                tg.Start();

                // CASE A: Section Box Active, turn it off
                if (view3D.IsSectionBoxActive)
                {
                    BoundingBoxXYZ currentBox = view3D.GetSectionBox();

                    if (currentBox != null)
                    {
                        SectionBoxStorage.SaveBoundingBox(view3D, currentBox, doc);
                    }

                    using (Transaction tx = new Transaction(doc, commandString))
                    {
                        tx.Start();
                        view3D.IsSectionBoxActive = false;
                        tx.Commit();
                    }

                // CASE B: Section Box Inactive, turn it on
                }
                else
                {
                    bool restored  = false;

                    // Get saved entity storage
                    if (SectionBoxStorage.TryGetSavedBoundingBox(view3D, out BoundingBoxXYZ savedBox))
                    {
                        using (Transaction tx = new Transaction(doc, commandString))
                        {
                            tx.Start();
                            view3D.SetSectionBox(savedBox);
                            view3D.IsSectionBoxActive = true;
                            tx.Commit();
                        }
                        restored = true;
                    }
                    
                    if (!restored)
                    {
                        using (Transaction tx = new Transaction(doc, commandString))
                        {
                            tx.Start();
                            view3D.IsSectionBoxActive = true;
                            tx.Commit();
                        }
                    }
                }

                tg.Assimilate();
                return Result.Succeeded;

            }
        }

        public static Result SetSection(UIDocument uidoc, bool viewHandlerFired, string commandName, string mode, double? specifiedSize = null)
        {

            Document doc = uidoc.Document;
            UIApplication uiapp = uidoc.Application;

            // Get list of selections and check if there's nothing
            ICollection<ElementId> sel = uidoc.Selection.GetElementIds();
            if (sel.Count == 0) return Result.Cancelled;


            // Create bbox and check if Elements have no geometry
            BoundingBoxXYZ bbox = SectionBoxLogic.NewBoundingBox(doc, sel);
            if (bbox == null) return Result.Cancelled;

            // will default to element if specified.value is null
            if (specifiedSize.HasValue)
            {
                if (string.Equals(mode, "specific", StringComparison.OrdinalIgnoreCase))
                {
                    bool working = CreateSpecificBoundingBox(bbox, specifiedSize.Value, out bbox);
                }
                else if (string.Equals(mode, "elements", StringComparison.OrdinalIgnoreCase))
                {
                    double p = specifiedSize.Value;
                    XYZ offset = new XYZ(p, p, p);
                    bbox.Min -= offset;
                    bbox.Max += offset;
                }
            }

            // Check if the default 3D view is already opened
            IEnumerable<View3D> view3ds = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>();

            View3D default3D = view3ds.FirstOrDefault(v => v.Name.Contains($"3D - {doc.Application.Username}") || v.Name.Contains("{3D}"));
            if (default3D != null)
            {
                bool isOpen = uidoc.GetOpenUIViews().Any(uiv => uiv.ViewId == default3D.Id);
                Open3DView(uidoc, default3D, bbox, commandName);
                return Result.Succeeded;
            }


            // Open Default 3D View and wait for it to be activated
            void handler(object sender, ViewActivatedEventArgs args)
            {
                if (viewHandlerFired) return;
                viewHandlerFired = true;
                void _run()
                {
                    try
                    {
                        if (args.Document.Equals(doc))
                        {
                            View view = args.CurrentActiveView;
                            if (view is View3D v3d && !v3d.IsTemplate)
                            {
                                Open3DView(uidoc, v3d, bbox, commandName);
                                return;
                            }
                            else
                            {
                                IEnumerable<View3D> _view3ds = new FilteredElementCollector(doc)
                                    .OfClass(typeof(View3D))
                                    .Cast<View3D>();

                                View3D _default3D = _view3ds.FirstOrDefault(v => v.Name.Contains($"3D - {doc.Application.Username}") || v.Name.Contains("{3D}"));
                                if (_default3D == null)
                                {
                                    _default3D = CreateNew3DView(doc);
                                }
                                Open3DView(uidoc, _default3D, bbox, commandName);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", ex.Message);
                    }
                }
                _run();
                // Unsubscribe from the event after it has been handled
                uiapp.ViewActivated -= handler;
                viewHandlerFired = false;
            }

            uiapp.ViewActivated += handler;

            // Open Default 3D View
            RevitCommandId cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.Default3DView);
            uiapp.PostCommand(cmdId);

            return Result.Succeeded;
        }


        private static Result Open3DView(UIDocument uidoc, View3D view3d, BoundingBoxXYZ bbox, string commandName)
        {
            Document doc = uidoc.Document;

            // ViewNavigationToolSettings nav = ViewNavigationToolSettings.GetViewNavigationToolSettings(doc);
            // Set the BBox
            using (Transaction tx = new Transaction(doc, commandName))
            {
                tx.Start();
                view3d.SetSectionBox(bbox);
                // targetView3D.SetOrientation(nav.GetHomeCamera());
                tx.Commit();
            }

            // Remove current selection to fix weird revit bug of creating long lines
            // that makes the zoomtofit so fucking wide
            // Select the Section Box for a tight tight stable control

            ElementCategoryFilter sectionBoxFilter = new ElementCategoryFilter(BuiltInCategory.OST_SectionBox);
            ElementId sectionBox = view3d.GetDependentElements(sectionBoxFilter)[0];
            uidoc.Selection.SetElementIds(new List<ElementId>() { sectionBox });

            uidoc.RequestViewChange(view3d);

            // Create Idle Event Handler for zoom to fit
            void idling(object sender, IdlingEventArgs e)
            {
                try
                {
                    var uiView = uidoc.GetOpenUIViews().FirstOrDefault(v => v.ViewId == view3d.Id);
                    uidoc?.RefreshActiveView();
                    uiView?.ZoomToFit();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                }
                uidoc.Application.Idling -= idling;
            }
            uidoc.Application.Idling += idling;

            return Result.Succeeded;
        }


        private static BoundingBoxXYZ NewBoundingBox(Document doc, ICollection<ElementId> sel)
        {

            // Get BoundingBox of Selections
            BoundingBoxXYZ bbox = null;
            foreach (ElementId id in sel)
            {
                Element e = doc.GetElement(id);
                BoundingBoxXYZ b = e.get_BoundingBox(null);
                if (b == null) continue;
                if (bbox == null)
                {
                    bbox = new BoundingBoxXYZ
                    {
                        Min = b.Min,
                        Max = b.Max
                    };
                }
                else
                {
                    bbox.Min = new XYZ(
                        Math.Min(bbox.Min.X, b.Min.X),
                        Math.Min(bbox.Min.Y, b.Min.Y),
                        Math.Min(bbox.Min.Z, b.Min.Z)
                    );
                    bbox.Max = new XYZ(
                        Math.Max(bbox.Max.X, b.Max.X),
                        Math.Max(bbox.Max.Y, b.Max.Y),
                        Math.Max(bbox.Max.Z, b.Max.Z)
                    );
                }
            }
            return bbox;
        }

        private static bool CreateSpecificBoundingBox(BoundingBoxXYZ selBbox, double size, out BoundingBoxXYZ newbbox)
        {

            // 2) compute center and half-extent in the same coordinate space as the existing bbox
            //    (use the existing bbox center so we don't move the box away from the selection)
            XYZ center = new XYZ(
                (selBbox.Min.X + selBbox.Max.X) * 0.5,
                (selBbox.Min.Y + selBbox.Max.Y) * 0.5,
                (selBbox.Min.Z + selBbox.Max.Z) * 0.5
            );

            double half = size / 2.0;

            var newBox = new BoundingBoxXYZ
            {
                Min = new XYZ(center.X - half, center.Y - half, center.Z - half),
                Max = new XYZ(center.X + half, center.Y + half, center.Z + half)
            };

            // 3) preserve original transform (orientation) if present
            if (selBbox.Transform != null)
                newBox.Transform = selBbox.Transform;

            // 4) optional: sanity-check minimum size
            const double MinAcceptable = 0.1; // internal units (feet) adjust as needed
            if ((newBox.Max.X - newBox.Min.X) < MinAcceptable ||
                (newBox.Max.Y - newBox.Min.Y) < MinAcceptable ||
                (newBox.Max.Z - newBox.Min.Z) < MinAcceptable)
            {
                // abort or adjust
                newbbox = selBbox;
                return false;
            }

            newbbox = newBox;
            return true;
        }

        private static View3D CreateNew3DView(Document doc)
        {
            ViewFamilyType viewType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);

            View3D new3D;
            using (Transaction tx = new Transaction(doc, "Create 3D View"))
            {
                tx.Start();
                new3D = View3D.CreateIsometric(doc, viewType.Id);
                new3D.Name = $"3D - {doc.Application.Username}";
                tx.Commit();
            }
            return new3D;
        }



    }
}