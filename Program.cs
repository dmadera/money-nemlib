using System;
using System.IO;
using System.Text.RegularExpressions;

namespace money_nemlib
{
    class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        static int Main(string[] args)
        {
            ServiceProvider = Services.Startup.BuildServiceProvider();

            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} Converze nemlib txt objednávky do XML.", DateTime.Now);
            try
            {
                if (args.Length != 2 && args.Length != 1) throw new ArgumentException("Chyba vstupních argumentů. Povolený počet: 1 nebo 2.");

                string inputTxtFile = args[0];
                string outputTxtFile = inputTxtFile;
                if (args.Length == 2)
                {
                    outputTxtFile = args[1];
                }
                else
                {
                    MakeCopy(inputTxtFile);
                }

                if (!File.Exists(inputTxtFile)) throw new ArgumentException($"Nepodařilo se načíst vstupní XML soubor: {inputTxtFile}.");

                var conv = new NemlibToXml(inputTxtFile, System.Text.Encoding.UTF8);
                conv.Serialize(outputTxtFile);
                return 0;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ReadKey();
                return 1;
            }
        }

        private static void MakeCopy(string file)
        {
            string fname = Path.GetFileNameWithoutExtension(file);
            string newNameBase = fname + "_", newFile = "";
            int found = 0;
            var r = new Regex("^(.+_)([0-9]{1,3})$");
            if (r.IsMatch(fname))
            {
                Match match = r.Match(fname);
                string grpMatch = match.Groups[2].ToString();
                found = int.Parse(grpMatch);
                newNameBase = match.Groups[1].ToString();
            }
            while (true)
            {
                newFile = Path.Combine(Path.GetDirectoryName(file), newNameBase + (found + 1).ToString() + Path.GetExtension(file));
                if (!File.Exists(newFile))
                {
                    File.Copy(file, newFile);
                    break;
                }
                found++;
            }

        }
    }
}
