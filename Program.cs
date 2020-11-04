using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PdfTest1
{
    class Program
    {
        // Method to convert current pdf to string
        public static string GetTextFromPDF(string file)
        {
            string path = System.IO.Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName, file);

            StringBuilder text = new StringBuilder();

            using (PdfReader reader = new PdfReader(path))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                }
            }

            return text.ToString();
        }

        public static void GetPDF()
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile("https://chfs.ky.gov/agencies/dph/covid19/COVID19DailyReport.pdf", @"C:\Users\thoma\Desktop\PDFTest\PdfTest1\COVIDDailySummary.pdf");
            }
        }

        public static void GetDate(string[] pdf)
        {
            var regex = new Regex(@"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b");

            foreach (Match m in regex.Matches(pdf[0]))
            {
                DateTime dt;

                if (DateTime.TryParseExact(m.Value, "MM/dd/yyyy", null, DateTimeStyles.None, out dt))
                {
                    Console.WriteLine($" COVID-19 data for {dt.ToShortDateString()}\n");
                }
            }
        }

        // Method to extract covid numbers from the first section of pdf and output data
        public static void GeneralStats(string[] pdf)
        {
            string vals = "";

            for (int i = 0; i < 10; i++)
            {
                vals += $"{pdf[i]} ";
            }

            List<long> nums = new List<long>();

            string[] demo = vals.Split(' ');

            bool newPDF = long.TryParse(demo[5], NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long x);

            foreach (string word in demo)
            {
                long temp;

                if (long.TryParse(word, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out temp))
                {
                    nums.Add(temp);
                }
            }

            if (newPDF)
            {
                Console.WriteLine($" Total Cases: {nums[0]} (Confirmed - {nums[2]}, Probable - {nums[4]})\n");
                Console.WriteLine($" Total Deaths: {nums[5]} (Confirmed - {nums[6]}, Probable - {nums[7]})\n");
                Console.WriteLine($" New Cases Today: {nums[1]}\n");
                Console.WriteLine($" New Deaths Today: {nums[3]}\n");
            }
            else
            {
                Console.WriteLine($" Total Cases: {nums[0]} (Confirmed - {nums[1]}, Probable - {nums[3]})\n");
                Console.WriteLine($" Total Deaths: {nums[4]} (Confirmed - {nums[6]}, Probable - {nums[7]})\n");
                Console.WriteLine($" New Cases Today: {nums[2]}\n");
                Console.WriteLine($" New Deaths Today: {nums[5]}\n");
            }
        }

        public static void Outcomes(string[] pdf)
        {
            List<string> hosptialized = new List<string>();
            List<string> icu = new List<string>();
            List<string> recovered = new List<string>();

            foreach (string item in pdf)
            {
                if (item.ToLower().Contains("number of patients ever hospitalized"))
                {
                    foreach (string word in item.Split(' '))
                    {
                        hosptialized.Add(word);
                    }
                }

                if (item.ToLower().Contains("number of patients ever in the icu"))
                {
                    foreach (string word in item.Split(' '))
                    {
                        icu.Add(word);
                    }
                }

                if (item.ToLower().Contains("number of patients who have recovered"))
                {
                    foreach (string word in item.Split(' '))
                    {
                        recovered.Add(word);
                    }
                }
            }

            Console.WriteLine($" Number of patients ever hospitalized: {hosptialized[5]}\n");
            Console.WriteLine($" Number of patients ever in ICU: {icu[7]}\n");
            Console.WriteLine($" Number of patients who have recovered: {recovered[6]}\n");
        }

        public static void AgeReference(string[] pdf)
        {
            List<string[]> ages = new List<string[]>();

            for (int i = 0; i < pdf.Length - 1; i++)
            {
                if (pdf[i].ToLower().Contains("age range"))
                {
                    for (int y = 0; y < 9; y++)
                    {
                        ages.Add(pdf[i + 1].Split(' ').ToArray());
                        i++;
                    }
                }
            }

            foreach (string[] item in ages)
            {
                Console.WriteLine($" Ages {item[1]}: {item[0]} Cases and {item[2]} Deaths");
            }
            Console.WriteLine();
        }

        public static void CountyTotalCases(string[] pdf, string county)
        {
            List<char> puncs = new List<char>();
            List<string> words = new List<string>();
            IEnumerable<string> val;

            bool contains = false;

            int index = 0;

            // Loop through each line of the converted pdf
            foreach (string item in pdf)
            {
                // Check if I'm past the Total Cases section 
                // Break out of loop if so
                if (item.ToLower().Contains("county new cases"))
                {
                    Console.WriteLine("No data for that county.");
                    break;
                }
                // Find all punctuations in the line
                // Split each word in the line, trim out puctuations, then add words to a collection
                // Search collection for the county provided in the argument and change variable "contains" to true or false 
                puncs = item.Where(char.IsPunctuation).Distinct().ToList();
                val = item.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(puncs.ToArray()));
                contains = val.Contains(county.ToLower(), StringComparer.OrdinalIgnoreCase);

                
                // If collection contains county
                if (contains)
                {
                    // Convert collection to list
                    // Find and set the index of the county element
                    words = val.Select(m => m.ToString()).ToList();
                    index = Array.IndexOf(words.ToArray(), county);
                    break;
                }
            }

            if (!contains)
                // Message if county was not found
                Console.WriteLine("No Cases Found");
            else
                // Message if county was found
                Console.WriteLine($" {words[index]} has had a total of {words[index + 1]} cases\n");
        }

        public static void CountyNewCases(string[] pdf, string county)
        {
            List<char> puncs = new List<char>();
            List<string> words = new List<string>();
            IEnumerable<string> val;

            bool contains = false;
            bool endPoint = false;

            int index = 0;

            foreach (string item in pdf)
            {
                if (item.ToLower().Contains("county deaths"))
                {
                    Console.WriteLine("No new cases.");
                    break;
                }
                // Find all punctuations in the line
                // Split each word in the line, trim out puctuations, then add words to a collection
                // Search collection for the county provided in the argument and change variable "contains" to true or false 
                puncs = item.Where(char.IsPunctuation).Distinct().ToList();
                val = item.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(puncs.ToArray()));
                contains = val.Contains(county.ToLower(), StringComparer.OrdinalIgnoreCase);

                if (endPoint)
                {
                    if (contains)
                    {
                        // Convert collection to list
                        // Find and set the index of the county element
                        words = val.Select(m => m.ToString()).ToList();
                        index = Array.IndexOf(words.ToArray(), county);
                        break;
                    }
                }

                if (item.ToLower().Contains("county new cases"))
                {
                    endPoint = true;
                }
            }

            if (!contains)
                // Message if county was not found
                Console.WriteLine("No Cases Found");
            else
                // Message if county was found
                Console.WriteLine($" {words[index]} has {words[index + 1]} new cases\n");
        }

        public static void CountyTotalDeaths(string[] pdf, string county)
        {
            List<char> puncs = new List<char>();
            List<string> words = new List<string>();
            IEnumerable<string> val;

            bool contains = false;
            bool endPoint = false;

            int index = 0;
            

            foreach (string item in pdf)
            {
                // Find all punctuations in the line
                // Split each word in the line, trim out puctuations, then add words to a collection
                // Search collection for the county provided in the argument and change variable "contains" to true or false 
                puncs = item.Where(char.IsPunctuation).Distinct().ToList();
                val = item.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(puncs.ToArray()));
                contains = val.Contains(county.ToLower(), StringComparer.OrdinalIgnoreCase);

                if (endPoint)
                {
                    if (contains)
                    {
                        // Convert collection to list
                        // Find and set the index of the county element
                        words = val.Select(m => m.ToString()).ToList();
                        index = Array.IndexOf(words.ToArray(), county);
                        break;
                    }
                }
                // Triggers when to start searching for county death data in string array
                if (item.ToLower().Contains("county deaths"))
                {
                    endPoint = true;
                }
            }

            if (!contains)
                // Message if county was not found
                Console.WriteLine(" No Cases Found");
            else
                // Message if county was found
                Console.WriteLine($" {words[index]} has had {words[index + 1]} deaths\n");
        }

        static void Main(string[] args)
        {
            GetPDF();

            var newpdf = GetTextFromPDF("COVIDDailySummary.pdf").Split('\n');

            GetDate(newpdf);
            GeneralStats(newpdf);
            Outcomes(newpdf);
            AgeReference(newpdf);

            CountyTotalCases(newpdf, "jefferson");
            CountyTotalDeaths(newpdf, "jefferson");
            CountyNewCases(newpdf, "jefferson");

            Console.ReadLine();
        }
    }
}
