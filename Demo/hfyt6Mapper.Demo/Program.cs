using hfyt6Mapper;
using hfyt6Mapper.Demo;


Mapper.Configure<SimpleObjA, SimpleObjB>(p =>
{
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Name, b => b.Name);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Id, b => b.Id);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Photo, b => b.Photo);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.IsStudent, b => b.IsStudent);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.BrithDate, b => b.BrithDate);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Description, b => b.Description);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Guid, b => b.Guid);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Money, b => b.Money);
    p.ForMember<SimpleObjA, SimpleObjB>(a => a.Weight, b => b.Weight);
});

SimpleObjA a = new SimpleObjA
{
    Id = 1,
    Name = "a",
    Photo = new byte[] {0x3f, 0x4f, 0x2a, 0x1c, 0x5b},
    IsStudent = true,
    BrithDate = new DateTime(1111, 2, 3, 4, 55, 6),
    Description = "a is a good object",
    Guid = Guid.NewGuid(),
    Money = 3.141592654,
    Weight = 2.718281828459045235360287M,
};

SimpleObjB b =Mapper.Map<SimpleObjA, SimpleObjB>(a);

if (Math.Abs(a.Money - b.Money) > 1e-6 || Math.Abs(a.Weight - b.Weight) > 1e-9M ||
    a.Id != b.Id || a.Name != b.Name || a.Photo != b.Photo || a.IsStudent != b.IsStudent || a.BrithDate != b.BrithDate || a.Description != b.Description || a.Guid != b.Guid)
    Console.WriteLine("Incorrect copying object");
else
    Console.WriteLine("Copy correctly");

Console.ReadKey();