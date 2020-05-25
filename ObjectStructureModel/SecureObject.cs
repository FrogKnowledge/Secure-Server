using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace CommonTypes
{
    namespace ObjectStructureModel
    {
        [Serializable]
        public class SecureObject
        {
            public List<Floor> Floors = new List<Floor>();
            public string name;
        }
        [Serializable]
        public class Floor
        {
            public ObservableCollection<Room> rooms = new ObservableCollection<Room>();
            public string Name { get; set; }
            public Floor(string name, NotifyCollectionChangedEventHandler handler)
            {
                SetChangeHandler(handler);
                Name = name;
            }

            public void SetChangeHandler(NotifyCollectionChangedEventHandler handler)
            {
                rooms.CollectionChanged += handler;
                Cameras.CollectionChanged += handler;
                Doors.CollectionChanged += handler;
                for(int i = 0; i < rooms.Count; i++)
                {
                    rooms[i].SetChangeHandler(handler);
                }
            }
            public ObservableCollection<Camera> Cameras { get; set; } = new ObservableCollection<Camera>();
            public ObservableCollection<Door> Doors { get; set; } = new ObservableCollection<Door>();
            [Serializable]
            public struct Door
            {
                
                public PointD point1;
                public PointD point2;
                public Door(PointD point1, PointD point2)
                {
                    this.point1 = point1;
                    this.point2 = point2;
                }
            }
        }
        [Serializable]
        public class Camera
        {
            public PointD Point { get; set; }
            public string Stream { get; set; }

            public Camera(PointD point, string stream)
            {
                Point = point;
                Stream = stream;
            }
        }
        [Serializable]
        public class PointD
        {
            public double X { get; set; }
            public double Y { get; set; }
            public PointD(double x, double y)
            {
                X = x;
                Y = y;
            }
            public static PointD operator -( PointD point1, PointD point2)
            {
                return new PointD(point1.X - point2.X, point1.Y - point2.Y);
            }
            public static PointD operator +(PointD point1, PointD point2)
            {
                return new PointD(point1.X + point2.X, point1.Y + point2.Y);
            }
            public static PointD operator /(PointD point1, double num)
            {
                return new PointD(point1.X / num, point1.Y / num);
            }
            public static PointD operator *(PointD point1, double num)
            {
                return new PointD(point1.X * num, point1.Y * num);
            }
            public override string ToString()
            {
                return '('+(Math.Round(X/10)/10).ToString(CultureInfo.InvariantCulture)+';'+ (Math.Round(Y / 10) / 10).ToString(CultureInfo.InvariantCulture) + ')';
            }
        }
    }
}