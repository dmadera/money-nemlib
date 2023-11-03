using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using Services;
using Microsoft.Data.SqlClient;


using NEMLIB_OBJ;
using System.Data;
using System;
using System.Linq;


namespace money_nemlib
{
    class NemlibToXml
    {
        private List<S5DataObjednavkaPrijata> _objednavky = new List<S5DataObjednavkaPrijata>();

        private string prijatyDoklad;

        public NemlibToXml(string inputFile, Encoding enc)
        {
            var rawlines = File.ReadAllLines(inputFile, enc);
            var lines = new List<string>();
            foreach (var line in rawlines)
            {
                if (line.Trim() == "") continue;
                lines.Add(line);
            }

            if (lines.Count < 4) throw new InvalidDataException("Neplatný formát souboru - chybí kontrolní řádky.");

            prijatyDoklad = lines[0].Trim();

            if (prijatyDoklad == "") throw new InvalidDataException("Neplatný formát souboru - chybí na prvním řádku přijatý doklad.");

            if (!lines[1].StartsWith("===")) throw new InvalidDataException("Neplatný formát souboru - chybí na začátku řádek ===.");
            if (!lines[2].StartsWith("___")) throw new InvalidDataException("Neplatný formát souboru - chybí řádek oddělující objednávky ___.");
            if (!lines[lines.Count - 1].StartsWith("===")) throw new InvalidDataException("Neplatný formát souboru - chybí na konci řádek ===.");

            lines.RemoveRange(0, 3);
            lines.RemoveAt(lines.Count - 1);

            ProcessFile(lines);
        }

        private void ProcessFile(List<string> lines)
        {
            var obj = new S5DataObjednavkaPrijata();
            var polozky = new List<S5DataObjednavkaPrijataPolozkyPolozkaObjednavkyPrijate>();
            int cisloPolozky = 1;
            var prefix = "KNL_";

            foreach (var line in lines)
            {
                if (line.StartsWith("___"))
                {
                    obj.Polozky = new S5DataObjednavkaPrijataPolozky()
                    {
                        PolozkaObjednavkyPrijate = polozky.ToArray()
                    };
                    _objednavky.Add(obj);
                    obj = new S5DataObjednavkaPrijata();
                    polozky = new List<S5DataObjednavkaPrijataPolozkyPolozkaObjednavkyPrijate>();
                    cisloPolozky = 1;
                    continue;
                }

                if (line.Trim() == "") continue;

                var values = line.Split("|");

                if (values.Length != 6) throw new InvalidDataException("Neplatný formát souboru - split |.");

                if (obj.Adresa == null)
                {
                    var kodOdb = prefix + values[0].Trim();

                    obj.Adresa = new S5DataObjednavkaPrijataAdresa()
                    {
                        Firma = new S5DataObjednavkaPrijataAdresaFirma()
                        {
                            KodOdb_UserData = kodOdb
                        }
                    };

                    obj.AdresaKoncovehoPrijemce = new S5DataObjednavkaPrijataAdresaKoncovehoPrijemce()
                    {
                        Firma = new S5DataObjednavkaPrijataAdresaKoncovehoPrijemceFirma()
                        {
                            KodOdb_UserData = kodOdb
                        }
                    };

                    obj.AdresaPrijemceFaktury = new S5DataObjednavkaPrijataAdresaPrijemceFaktury()
                    {
                        Firma = new S5DataObjednavkaPrijataAdresaPrijemceFakturyFirma()
                        {
                            KodOdb_UserData = kodOdb
                        }
                    };

                    var rowData = GetCompany(kodOdb);
                    obj.Odkaz = prijatyDoklad;
                    obj.Sleva = rowData["HodnotaSlevy"];
                    obj.ZpusobDopravy = new S5DataObjednavkaPrijataZpusobDopravy()
                    {
                        ID = rowData["ZpusobDopravy_ID"]
                    };
                }

                polozky.Add(new S5DataObjednavkaPrijataPolozkyPolozkaObjednavkyPrijate()
                {
                    CisloPolozky = cisloPolozky.ToString(),
                    TypObsahu = new enum_TypObsahuPolozky() { Value = enum_TypObsahuPolozky_value.Item1 },
                    ObsahPolozky = new S5DataObjednavkaPrijataPolozkyPolozkaObjednavkyPrijateObsahPolozky()
                    {
                        Sklad = new S5DataObjednavkaPrijataPolozkyPolozkaObjednavkyPrijateObsahPolozkySklad()
                        {
                            Kod = "HL"
                        },
                        Artikl = new S5DataObjednavkaPrijataPolozkyPolozkaObjednavkyPrijateObsahPolozkyArtikl()
                        {
                            Kod = values[3].Trim()
                        }
                    },
                    Mnozstvi = values[4].Trim()
                });

                cisloPolozky++;
            }

            obj.Polozky = new S5DataObjednavkaPrijataPolozky()
            {
                PolozkaObjednavkyPrijate = polozky.ToArray()
            };
            _objednavky.Add(obj);
        }

        public void Serialize(string outputFile)
        {
            var xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true,
                CheckCharacters = false,
            };
            var serializer = new XmlSerializer(typeof(S5Data));
            using (var stream = XmlWriter.Create(outputFile, xmlWriterSettings))
            {
                var data = new S5Data()
                {
                    ObjednavkaPrijataList = _objednavky.ToArray()
                };
                serializer.Serialize(stream, data, null, "");
            }

            string text = File.ReadAllText(outputFile);
            text = text.Replace("&amp;#13;&amp;#10;", "&#xD;&#xA;");
            File.WriteAllText(outputFile, text);
        }

        private static Dictionary<string, string> GetCompany(string kodOdb)
        {
            using (var connection = Sql.CreateConnection())
            {
                connection.Open();
                var sql = $"SELECT TOP 1 * FROM USER_FIRMA WHERE KodOdb_UserData = '{kodOdb}'";

                using SqlCommand command = new SqlCommand(sql, connection);
                using SqlDataReader reader = command.ExecuteReader();
                if (!reader.Read()) throw new Exception($"DATA NOT FOUND. {sql}");

                var rowData = new Dictionary<string, string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    rowData.Add(reader.GetName(i), reader.GetValue(i).ToString());
                }
                return rowData;
            }
        }

    }
}
