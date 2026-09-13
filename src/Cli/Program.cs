using System;
using System.Runtime.InteropServices;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        var info = new
        {
            Project = "CrossApp — практикум з крос-платформного програмування",
            Student = "Гуляєв Максим ФЕІ 37",
            OSDescription = RuntimeInformation.OSDescription,
            OSEnvironment = Environment.OSVersion.ToString(),
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            NetVersion = Environment.Version.ToString(),
            Runtime = RuntimeInformation.FrameworkDescription,
            AppDir = AppContext.BaseDirectory,
            CurrentDir = Environment.CurrentDirectory,
            Domain = "Склад (товари, партії, залишки, переміщення)"
        };

        bool useJson = false;
        foreach (var arg in args)
        {
            if (arg == "--json")
            {
                useJson = true;
                break;
            }
        }

        if (useJson)
        {
            // Оновлені налаштування JSON, щоб українські літери відображалися нормально
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonString = JsonSerializer.Serialize(info, options);
            Console.WriteLine(jsonString);
        }
        else
        {
            Console.WriteLine(info.Project);
            Console.WriteLine($"Студент: {info.Student}");
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine($"ОС (OSDescription): {info.OSDescription}");
            Console.WriteLine($"ОС (Environment)  : {info.OSEnvironment}");
            Console.WriteLine($"Архітектура процесу: {info.Architecture}");
            Console.WriteLine($"Версія .NET (CLR): {info.NetVersion}");
            Console.WriteLine($"Runtime : {info.Runtime}");
            Console.WriteLine($"Каталог застосунку: {info.AppDir}");
            Console.WriteLine($"Поточний каталог : {info.CurrentDir}");
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine($"Предметна область: {info.Domain}");
        }
    }
}