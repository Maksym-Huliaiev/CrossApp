using System;
using Core;

EnvironmentReport report = EnvironmentInfo.Collect();

Console.WriteLine("CrossApp - інформація про середовище");
Console.WriteLine("Студент: Гуляєв Максим ФЕІ 37");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"OC               : {report.OsDescription}");
Console.WriteLine($"Runtime          : {report.FrameworkDescription}");
Console.WriteLine($"Архітектура      : {report.ProcessArchitecture}");
Console.WriteLine($"RID (визначено)  : {report.DetectedRid}");
Console.WriteLine($"RID (від .NET)   : {report.ReportedRid}");
Console.WriteLine($"Каталог          : {report.BaseDirectory}");
Console.WriteLine($"Примітка збірки  : {report.BuildNote}"); 
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Склад (товари, партії, залишки, переміщення)");