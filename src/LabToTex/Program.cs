using LabToTex.Parsers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LabToTex
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new MatlabParser();

            var doMultiple = false;

            var filetouse = 1;
            var files = new List<string>
            {
                @"C:\Users\haggi\Documents\Uni\semester 2\Messtechnik\Übung 1\e_1.m",
                @"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotex\test01.m"
            };

            if (doMultiple)
            {
                var directory = new DirectoryInfo(@"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotexv2");

                foreach (var currentFile in directory.GetFiles())
                {
                    if (currentFile.Extension != ".m")
                        File.Delete(currentFile.FullName);
                }

                foreach (var currentFile in directory.GetFiles("*.m"))
                {
                    var outputFileName = Path.Combine(directory.FullName, Path.GetFileNameWithoutExtension(currentFile.FullName) + ".tex");

                    parser.Parse(currentFile.FullName,
                        outputFileName,
                        @"C:\Users\haggi\Documents\Uni\semester 1\latex\helloworld.tex");
                }
            }
            else
            {
                parser.Parse(files[filetouse],
                    @"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotex\test01.tex",
                    @"C:\Users\haggi\Documents\Uni\semester 1\latex\helloworld.tex");

            }
        }
    }
}
