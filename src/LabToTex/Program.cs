using LabToTex.Parsers;
using System.IO;

namespace LabToTex
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new MatlabParser();

            var doMultiple = false;

            if(doMultiple)
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

                parser.Parse(@"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotex\messtechnik_ubeung5.m",
                    @"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotex\test01.tex",
                    @"C:\Users\haggi\Documents\Uni\semester 1\latex\helloworld.tex");
            }
        }
    }
}
