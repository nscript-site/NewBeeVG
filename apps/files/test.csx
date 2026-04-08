using C = (string Name, string IP);
using D = System.Collections.Generic.Dictionary<string, object>;

var d = new D(){
    ["a"] = 3
};

void Print(C c)
{
    Console.WriteLine(c.Name);
    Console.WriteLine(c.IP);
}

Print(new C("Name","IP"));