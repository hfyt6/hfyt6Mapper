using hfyt6Mapper;
using hfyt6Mapper.Demo;
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
    Photo = new byte[] {0x3f, 0x4f, 0x2a, 0x1c, 0x5b},
    IsStudent = true,
    BrithDate = new DateTime(1111, 2, 3, 4, 55, 6),
    Description = "a is a good object",
    Guid = Guid.NewGuid(),
    Money = 3.141592654,
    Weight = 2.718281828459045235360287M,
};

SimpleObjB b = Mapper.Map<SimpleObjA, SimpleObjB>(a);

if (Math.Abs(a.Money - b.Money) > 1e-6 || Math.Abs(a.Weight - b.Weight) > 1e-9M ||
    a.Id != b.Id || a.Name != b.Name || a.Photo != b.Photo || a.IsStudent != b.IsStudent || a.BrithDate != b.BrithDate || a.Description != b.Description || a.Guid != b.Guid)
    Console.WriteLine("Incorrect copying object");
else
    Console.WriteLine("Copy correctly");

Console.ReadKey();