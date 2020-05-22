using System.Collections.Generic;

namespace CommonTypes
{
    namespace ObjectStructureModel
    {
        public class Room
        {

            public List<PointD> Points { get; set; } = new List<PointD>();


            public Room(PointD[] points)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Points.Add(points[i]);
                }
            }





        }

    }
}