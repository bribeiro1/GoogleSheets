using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleSheet
{
    class Program
    {
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "Current Legislators";
        static readonly string SpreadsheetId = "1mBhkJOFAKd-sFo7A7PVxsxrhXtGNG36AZKmjVNExwZ4";
        static readonly string sheet = "engenharia_de_software";
        static SheetsService service;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Gerenciador de Notas \n\n");
                GoogleCredential credential;
                using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(Scopes);
                }

                // Create Google Sheets API service.
                service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                Console.WriteLine("Processando planilha...\n\n");
                var values = ReadEntries();
                ExecuteService(values);
                Console.WriteLine("\n\nFinalizado!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Erro: " + e.Message);
            }
        }

        static void ExecuteService(IList<IList<object>> values)
        {
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    Console.WriteLine("Calculando situação para aluno(a) " + row[1]);

                    string line = Utils.FromToLines(values.IndexOf(row));

                    var media = Service.CalcMediaAluno(Convert.ToInt32(row[3]), Convert.ToInt32(row[4]), Convert.ToInt32(row[5]));

                    media.Values.ToArray()[0] = Service.CalcFaltas(Convert.ToInt32(row[2]), media.Values.ToArray()[0]);
                    Console.WriteLine("Média: " + media.Keys.ToArray()[0]);
                    Console.WriteLine("Situação final para aluno: " + media.Values.ToArray()[0]);

                    if (row.Count() == 8)
                    {
                        Console.WriteLine("Calculando NaF do aluno...");
                        string naf = Service.CalNaF(Convert.ToInt32(row[7]), media.Keys.ToArray()[0], media.Values.ToArray()[0]);
                        UpdateEntry("H", line, naf.ToString());
                    }

                    Console.WriteLine("Atualizando planilha...\n");
                    UpdateEntry("G", line, media.Values.ToArray()[0]);
                }
            }
            else
            {
                Console.WriteLine("Não existe dados na planilha.");
            }
        }

        private static IList<IList<object>> ReadEntries()
        {
            var range = $"{sheet}!A4:H";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            return response.Values;
        }

        private static void UpdateEntry(string column, string line, string situation)
        {
            var range = $"{sheet}!{column}{line}";
            var valueRange = new ValueRange();

            var oblist = new List<object>() { situation };
            valueRange.Values = new List<IList<object>> { oblist };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            _ = updateRequest.Execute();
        }
    }
}

