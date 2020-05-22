using System.Collections.Generic;

namespace CommonTypes
{
    namespace ObjectStructureModel
    {
        public class SecureObject
        {
            public List<Floor> Floors = new List<Floor>();
            public string name;
        }
        public class Floor
        {
            public List<Room> rooms = new List<Room>();
            public string name;
            public List<Camera> Cameras { get; set; } = new List<Camera>();
            public List<Door> Doors { get; set; } = new List<Door>();
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
        public class PointD
        {
            public double X { get; set; }
            public double Y { get; set; }
            public PointD(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}