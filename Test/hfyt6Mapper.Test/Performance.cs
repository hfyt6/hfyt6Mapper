using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hfyt6Mapper.Test
{
    internal class Performance
    {
        private const int upperlimit = 1000_000;
        public static void TestMapper()
        {
            Mapper.Configure<SimpleObjA, SimpleObjB>(p =>
            {
                p.SetMapper(a => a.Name, b => b.Name);
                p.SetMapper(a => a.Id, b => b.Id);
                p.SetMapper(a => a.Photo, b => b.Photo);
                p.SetMapper(a => a.IsStudent, b => b.IsStudent);
                p.SetMapper(a => a.BrithDate, b => b.BrithDate);
                p.SetMapper(a => a.Description, b => b.Description);
                p.SetMapper(a => a.Guid, b => b.Guid);
                p.SetMapper(a => a.Money, b => b.Money);
                p.SetMapper(a => a.Weight, b => b.Weight);
            });

            SimpleObjA a = new SimpleObjA
            {
                Id = 1,
                Name = "a",
                Photo = new byte[] { 0x3f, 0x4f, 0x2a, 0x1c, 0x5b },
                IsStudent = true,
                BrithDate = new DateTime(1111, 2, 3, 4, 55, 6),
                Description = "a is a good object",
                Guid = Guid.NewGuid(),
                Money = 3.141592654,
                Weight = 2.718281828459045235360287M,
            };

            List<SimpleObjB> arr = new List<SimpleObjB>(); 
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < upperlimit; i++)
            {
                arr.Add(Mapper.Map<SimpleObjA, SimpleObjB>(a));
            }
            sw.Stop();
            Console.WriteLine(arr.Count + " " + sw.ElapsedMilliseconds.ToString());
        }

        public static void TestAssignment()
        {
            SimpleObjA a = new SimpleObjA
            {
                Id = 1,
                Name = "a",
                Photo = new byte[] { 0x3f, 0x4f, 0x2a, 0x1c, 0x5b },
                IsStudent = true,
                BrithDate = new DateTime(1111, 2, 3, 4, 55, 6),
                Description = "a is a good object",
                Guid = Guid.NewGuid(),
                Money = 3.141592654,
                Weight = 2.718281828459045235360287M,
            };

            List<SimpleObjB> arr = new List<SimpleObjB>();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < upperlimit; i++)
            {
                arr.Add(new SimpleObjB() 
                {
                    Id = a.Id,
                    Name = a.Name,
                    Photo = a.Photo,
                    Weight= a.Weight,
                    Money= a.Money,
                    BrithDate= a.BrithDate,
                    Description= a.Description,
                    Guid = a.Guid,
                    IsStudent = a.IsStudent,
                });
            }
            Console.WriteLine(arr.Count + " " + sw.ElapsedMilliseconds.ToString());
        }
    }
}
