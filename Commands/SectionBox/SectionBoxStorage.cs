using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Orion.Commands.SectionBox
{
    internal class SectionBoxStorage
    {
        private static readonly Guid SchemaGuid = new Guid("D4F5A6B7-C8D9-4E0F-8A1B-2C3D4E5F6A7B");
        private const string SchemaName = "OrionSectionBoxStorage";

        private static Schema GetOrCreateSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema != null) return schema;

            SchemaBuilder builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName(SchemaName);

            builder.AddSimpleField("Min", typeof(string));
            builder.AddSimpleField("Max", typeof(string));

            return builder.Finish();
        }

        private static string SerializeXYZ(XYZ point)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", point.X, point.Y, point.Z);
        }

        private static XYZ DeserializeXYZ(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Data string is null or empty.", nameof(data));
            }

            IEnumerable<string> parts = data.Split(',');
            if (parts.Count() != 3)
            {
                throw new FormatException($"Data string should have three values. {data}");
            }

            double x = double.Parse(parts.ElementAt(0), CultureInfo.InvariantCulture);
            double y = double.Parse(parts.ElementAt(1), CultureInfo.InvariantCulture);
            double z = double.Parse(parts.ElementAt(2), CultureInfo.InvariantCulture);
            return new XYZ(x, y, z);
        }

        public static void SaveBoundingBox(View3D view3D, BoundingBoxXYZ bbox, Document doc)
        {
            if (view3D == null || bbox == null) throw new ArgumentNullException();

            Schema schema = GetOrCreateSchema();
            Entity entity = new Entity(schema);
            entity.Set<string>(schema.GetField("Min"), SerializeXYZ(bbox.Min));
            entity.Set<string>(schema.GetField("Max"), SerializeXYZ(bbox.Max));

            using (Transaction tx = new Transaction(doc, "Save Section Box BBox"))
            {
                tx.Start();
                view3D.SetEntity(entity);
                tx.Commit();
            }
        }

        public static bool TryGetSavedBoundingBox(View3D view, out BoundingBoxXYZ bbox)
        {
            bbox = null;
            
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return false;

            Entity entity = view.GetEntity(schema);
            if (!entity.IsValid()) return false;

            string minData = entity.Get<string>(schema.GetField("Min"));
            string maxData = entity.Get<string>(schema.GetField("Max"));

            if (string.IsNullOrWhiteSpace(minData) || string.IsNullOrWhiteSpace(maxData)) return false;

            try
            {
                XYZ min = DeserializeXYZ(minData);
                XYZ max = DeserializeXYZ(maxData);
                bbox = new BoundingBoxXYZ() { Min = min, Max = max };
                return true;
            } 
            catch
            {
                return false;
            }

        }

        public static void ClearSavedBoundingBox(View3D view3D, Document doc)
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return;

            Entity newEntity = new Entity(schema);
            newEntity.Set<string>(schema.GetField("Min"), string.Empty);

            using (Transaction tx = new Transaction(doc, "Clear Section Box BBox"))
            {
                tx.Start();
                view3D.SetEntity(newEntity);
                tx.Commit();
            }
        }
    }
}
