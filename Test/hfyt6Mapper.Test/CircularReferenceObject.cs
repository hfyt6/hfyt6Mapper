using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hfyt6Mapper.Test
{
    public class CircularReferenceObjectA
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public CircularReferenceObjectB B {  get; set; }
    }

    public class CircularReferenceObjectB
    {

        public int Id { get; set; }

        public double Price { get; set; }

        public string Name { get; set; }

        public CircularReferenceObjectB B { get; set; }

        public static void Test()
        {
            CircularReferenceObjectA a = new CircularReferenceObjectA();
            CircularReferenceObjectB b = new CircularReferenceObjectB();

            a.Id = 1;
            a.Name = "testA";
            a.B = b;

            b.Price = 1.23456;
            b.Name = "testB";
            b.B = b;

            Mapper.Configure<CircularReferenceObjectA, CircularReferenceObjectA>();
            Mapper.Configure<CircularReferenceObjectB, CircularReferenceObjectB>();
            Mapper.Configure<CircularReferenceObjectA, CircularReferenceObjectB>(cfg =>
            {
                cfg.SetMapper(x => x.Id * 3.1415, y => y.Price);
                cfg.UsePreserveReferences();
            });


            var bb = Mapper.Map<CircularReferenceObjectA, CircularReferenceObjectB>(a);
        }
    }
}
