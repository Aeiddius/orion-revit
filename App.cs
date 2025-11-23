using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Orion
{
    public class App : IExternalApplication
    {
        private const string tabName = "Orion";
        private readonly string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Tab
                application.CreateRibbonTab(tabName);

                // Section Panel
                RibbonPanel sectionPanel = application.CreateRibbonPanel(tabName, "Section Panel");
                SplitButtonData sbd = new SplitButtonData("OrionSectionBoxSplitButton", "Section Box Tools");

                ButtonSpec buttonSetSB = new ButtonSpec(
                    id: "cmdSetSectionBox",
                    title: "Section",
                    className: "Orion.Commands.SectionBox.SetSectionBoxCommand",
                    icon16: "Orion.Resource.SectionBox.SetSectionIcon-16x16.ico",
                    icon32: "Orion.Resource.SectionBox.SetSectionIcon-32x32.ico",
                    tooltip: "Creates a section box from the selected elements."
                    );

                ButtonSpec buttonMenuSB = new ButtonSpec(
                    id: "cmdMenuSectionBox",
                    title: "Menu",
                    className: "Orion.Commands.SectionBox.MenuSectionBoxCommand",
                    icon16: "Orion.Resource.SectionBox.MenuSectionBox-16x16.ico",
                    icon32: "Orion.Resource.SectionBox.MenuSectionBox-32x32.ico",
                    tooltip: "Opens the Detailed Menu for Section Box."
                    );

                ButtonSpec buttonGrowSB = new ButtonSpec(
                    id: "cmdGrowSectionBox",
                    title: "Grow",
                    className: "Orion.Commands.SectionBox.GrowSectionBoxCommand",
                    icon16: "Orion.Resource.SectionBox.GrowSectionIcon-16x16.ico",
                    icon32: "Orion.Resource.SectionBox.GrowSectionIcon-32x32.ico",
                    tooltip: "Grows the section box by a specified amount."
                    );

                ButtonSpec buttonShrinkSB = new ButtonSpec(
                    id: "cmdShrinkSectionBox",
                    title: "Shrink",
                    className: "Orion.Commands.SectionBox.ShrinkSectionBoxCommand",
                    icon16: "Orion.Resource.SectionBox.ShrinkSectionIcon-16x16.ico",
                    icon32: "Orion.Resource.SectionBox.ShrinkSectionIcon-32x32.ico",
                    tooltip: "Grows the section box by a specified amount."
                );

                ButtonSpec buttonToggleSB = new ButtonSpec(
                    id: "cmdToggleSectionBox",
                    title: "Toggle",
                    className: "Orion.Commands.SectionBox.ToggleSectionBoxCommand",
                    icon16: "Orion.Resource.SectionBox.ToggleSectionBox-16x16.ico",
                    icon32: "Orion.Resource.SectionBox.ToggleSectionBox-32x32.ico",
                    tooltip: "Toggled the current 3D Section Box on and off."
                );


                CreatePushButton(sectionPanel, new[] { buttonMenuSB, buttonSetSB });
                CreateStackedButtons(sectionPanel, new[] { buttonToggleSB, buttonGrowSB, buttonShrinkSB,  });

                // Filter
                RibbonPanel filterPanel = application.CreateRibbonPanel(tabName, "Filter Panel");
                ButtonSpec buttonFilter = new ButtonSpec(
                    id: "cmdFilterAdvanced",
                    title: "Filter+",
                    className: "Orion.Commands.SectionBox.SetSectionBoxCommand",
                    icon16: "Orion.Resource.SectionBox.FilterPlusIcon-16x16.ico",
                    icon32: "Orion.Resource.SectionBox.FilterPlusIcon-32x32.ico",
                    tooltip: "Advanced Filter mode"
                );

                CreatePushButton(filterPanel, new[] { buttonFilter });

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Orion OnStartup error", ex.ToString());
                return Result.Failed;
            }

        }

        private BitmapImage LoadImage(string path)
        {
            Uri uriImage = new Uri(path);
            return new BitmapImage(uriImage);

        }

        private BitmapImage LoadEmbeddedImage(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();

            using (Stream resourceStream = asm.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                // Copy to byte[] so we can close the original stream immediately.
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    resourceStream.CopyTo(ms);
                    bytes = ms.ToArray();
                }

                var bmp = new BitmapImage();
                using (var ms2 = new MemoryStream(bytes))
                {
                    bmp.BeginInit();
                    bmp.StreamSource = ms2;
                    bmp.CacheOption = BitmapCacheOption.OnLoad; // loads fully so ms2 can be closed
                    bmp.EndInit();
                }
                bmp.Freeze();
                return bmp;
            }
        }

        private class ButtonSpec
        {
            public string Id, Text, ClassName, Icon16, Icon32, ToolTip;
            public ButtonSpec(string id, string title, string className, string icon16, string icon32, string tooltip = null)
            {
                Id = id; Text = title; ClassName = className; Icon16 = icon16; Icon32 = icon32; ToolTip = tooltip;
            }
        }

        private void CreatePushButton(RibbonPanel panel, IEnumerable<ButtonSpec> buttonData)
        {
            foreach (ButtonSpec btnData in buttonData)
            {
                PushButtonData buttonSetSB = new PushButtonData(btnData.Id, btnData.Text, assemblyPath, btnData.ClassName)
                {
                    ToolTip = btnData.ToolTip,
                    LargeImage = LoadEmbeddedImage(btnData.Icon32),
                    Image = LoadEmbeddedImage(btnData.Icon16)
                };
                panel.AddItem(buttonSetSB);
            }
        }

        private void CreateStackedButtons(RibbonPanel panel, IEnumerable<ButtonSpec> buttonData)
        {
            List<PushButtonData> buttonItems = new List<PushButtonData>();
            foreach (ButtonSpec btnData in buttonData)
            {
                PushButtonData button = new PushButtonData(btnData.Id, btnData.Text, assemblyPath, btnData.ClassName)
                {
                    ToolTip = btnData.ToolTip,
                    LargeImage = LoadEmbeddedImage(btnData.Icon32),
                    Image = LoadEmbeddedImage(btnData.Icon16)
                };
                buttonItems.Add(button);

            }
            if (buttonItems.Count == 2)
            {
                panel.AddStackedItems(buttonItems[0], buttonItems[1]);
            }
            else if (buttonItems.Count == 3)
            {
                panel.AddStackedItems(buttonItems[0], buttonItems[1], buttonItems[2]);
            }
        }



    }
}
