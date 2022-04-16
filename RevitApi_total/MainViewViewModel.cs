using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitApi_total
{
    internal class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        public List<Level> Level { get; } = new List<Level>();
        public List<RoomTagType> RoomTags { get; }
        public Level SelectedLevel { get; set; }
        public RoomTagType SelectedTagType { get; set; }
        public DelegateCommand SaveCommand { get; }
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .OfType<Level>()
                .ToList();

            RoomTags = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .Cast<RoomTagType>()
                .ToList();

            SaveCommand = new DelegateCommand(OnSaveCommand);
        }
        private void OnSaveCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<Room> roomLev = new FilteredElementCollector(doc)
                   .OfCategory(BuiltInCategory.OST_Rooms)
                   .WhereElementIsNotElementType()
                   .Cast<Room>()
                   .ToList();

            if (SelectedLevel == null)
            {
                TaskDialog.Show("Ошибка", "Не выбран уровень");
                return;
            }

            List<Room> rooms = new List<Room>();
            foreach (Room room in roomLev)
            {
                Parameter roomLevel = room.get_Parameter(BuiltInParameter.LEVEL_NAME);
                var level = roomLevel.AsString();

                if (level == SelectedLevel.Name)
                    rooms.Add(room);
            }

            if (rooms.Count == 0)
            {
                TaskDialog.Show("Ошибка", "На выбранном уровне отсутствуют помещения");
                return;
            }

            if (SelectedTagType == null)
            {
                TaskDialog.Show("Ошибка", "Ошибка не выбран тип марки помещения");
                return;
            }

            foreach (Room room in rooms)
            {
                Transaction transaction = new Transaction(doc);
                transaction.Start();
                LocationPoint locationPoint = room.Location as LocationPoint;
                UV point = new UV(locationPoint.Point.X, locationPoint.Point.Y);
                RoomTag newTag = doc.Create.NewRoomTag(new LinkElementId(room.Id), point, null);
                newTag.RoomTagType = SelectedTagType;
                transaction.Commit();
            }
            RaiseCloseRequest();
        }
        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
