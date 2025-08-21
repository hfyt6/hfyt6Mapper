using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hfyt6Mapper.Test
{
    public class CloneObject : ICloneable
    {
        public int Id { get; set; } 

        public string Name { get; set; }

        public object Clone()
        {
            return new CloneObject { Id = Id, Name = Name };
        }
    }

    public class CloneParentObjectA
    {
        public int Key { get; set; }
        public string Value { get; set; }
        public CloneObject CloneObj {  get; set; }
    }

    public class CloneParentObjectB
    {
        public int Key { get; set; }
        public string Value { get; set; }
        public CloneObject CloneObj { get; set; }

        public static void Test()
        {
            var a = new CloneParentObjectA { Key = 1 , Value = "1", CloneObj = new CloneObject() { Id = 111, Name="cloneObj 111" } };
            Mapper.Configure<CloneParentObjectA, CloneParentObjectB>();
            var b = Mapper.Map<CloneParentObjectA, CloneParentObjectB>(a);

        }
    }
}
