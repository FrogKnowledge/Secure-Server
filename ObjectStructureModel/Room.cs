using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CommonTypes
{
    namespace ObjectStructureModel
    {
        [Serializable]
        public class Room
        {

            public ObservableCollection<PointD> Points { get; set; } = new ObservableCollection<PointD>();

            public string name;


            public Room(PointD[] points,string name,NotifyCollectionChangedEventHandler handler)
            {
                SetChangeHandler(handler);
               
                this.name = name;
                for (int i = 0; i < points.Length; i++)
                {
                    Points.Add(points[i]);
                }
            }


            public PointD GetBarycenter()
            {
                PointD barycenter = new PointD(0, 0);
                for (int i = 0; i < Points.Count; i++)
                {
                    barycenter += Points[i];
                }
                return barycenter / Points.Count;
            }

            public void SetChangeHandler(NotifyCollectionChangedEventHandler handler)
            {
                Points.CollectionChanged += handler;
            }



        }

    }
}