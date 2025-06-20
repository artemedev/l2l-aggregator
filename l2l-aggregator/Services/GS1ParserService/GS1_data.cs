﻿using l2l_aggregator.Services.GS1ParserService.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.GS1ParserService
{
    public struct GS1_data
    {
        public bool GS1isCorrect = false;
        public string GTIN = "";
        public string DMData = "";
        public string SerialNumber = "";
        public string Crypto = "";
        public string ProdGroup = "";

        public GS1_data() { }
    }

    public class GS1Parser
    {
        public Dictionary<string, string> Result { get; private set; } = new Dictionary<string, string>();

        public bool HasCheckSum { get; set; } = false;

        public GS1Definition GS1 { get; private set; }

        public GS1Parser()
        {
            GS1 = new GS1Definition();
        }

        public GS1Parser(GS1Definition Gs1_Definition)
        {
            GS1 = Gs1_Definition;
        }

        public SortedDictionary<string, AI> GetGS1Dict()
        {
            if (GS1 is null)
                return null;
            else
                return GS1.AIDict;
        }
        public GS1_data ParseGTIN(string DM)
        {
            GS1_data newGS = new GS1_data();
            newGS.GS1isCorrect = false;
            var result = new Dictionary<string, string>();
            try
            {
                result = Parse(DM);
            }
            catch (Exception ex)
            {
                //Loger.Log(Loger.Type.Error, "ParseGTIN", $"Error of parse code {DM}");
                return newGS;
            }
            // return each element of the GS1 Code to console
            foreach (KeyValuePair<string, string> item in result)
            {
                switch (item.Key)
                {
                    case "01":
                        {
                            newGS.GS1isCorrect = true;
                            newGS.GTIN = item.Value;
                        }
                        break;
                    case "21":
                        {
                            newGS.SerialNumber = item.Value;
                        }
                        break;
                }
            }
            return newGS;
        }

        public string[,] GetGS1DefinitionTable()
        {
            string[,] table = new string[GS1.AIDict.Count + 1, 6];
            table[0, 0] = "AI";
            table[0, 1] = "Description";
            table[0, 2] = "LengthOfAI";
            table[0, 3] = "DataDescription";
            table[0, 4] = "LengthOfData";
            table[0, 5] = "GroupSeparatorPresent";

            int row = 1;

            foreach (KeyValuePair<string, AI> item in GS1.AIDict)
            {
                table[row, 0] = item.Value.Id;
                table[row, 1] = item.Value.Description;
                table[row, 2] = item.Value.LengthOfAI.ToString();
                table[row, 3] = item.Value.DataDescription.ToString();
                table[row, 4] = item.Value.LengthOfData.ToString();
                table[row, 5] = item.Value.GroupSeparatorPresent.ToString();
                row++;
            }

            return table;
        }

        public string[,] ConvertResultDictToArray(Dictionary<string, string> ParsedResults)
        {
            string[,] table = new string[ParsedResults.Count, 2];
            int row = 0;

            foreach (KeyValuePair<string, string> item in ParsedResults)
            {
                table[row, 0] = item.Key;
                table[row, 1] = item.Value;
                row++;
            }
            return table;
        }


        /// <summary>
        /// Parses the code and saves the result in the internal result property
        /// </summary>
        /// <param name="data">The raw scanner data</param>
        /// <param name="throwException">Whether an exception will be thrown if an AI cannot be found</param>
        /// <returns>The different parts of the gs1 code</returns>
        public Dictionary<string, string> Parse(string data, bool throwException = false)
        {
            Result = new Dictionary<string, string>();

            data = PreProcess(data);

            if (new GtinValidator().Validate(data))
            {
                Result[EGS1AI.GTIN] = data;
                return Result;
            }

            int index = 0;

            while (index < data.Length)
            {
                if (!TryGetAI(data, ref index, out AI ai, throwException))
                {
                    return Result;
                }

                string code = GetCode(data, ai, ref index);
                Result[ai.Id] = code;
            }

            return Result;
        }

        private string PreProcess(string data)
        {
            data = data.Replace(GS1.GROUP_SEPARATOR_ALTERNATIVE, GS1.GROUP_SEPARATOR);

            foreach (var code in GS1.START_CODES)
            {
                if (data.StartsWith(code))
                {
                    data = data.Substring(code.Length);
                    break;
                }
            }
            // cut off the check sum
            if (HasCheckSum)
            {
                data = data.Substring(0, data.Length - 2);
            }

            return data;
        }

        /// <summary>
        /// Try to get the AI at the current position
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="ai"></param>
        /// <param name="throwException"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private bool TryGetAI(string data, ref int index, out AI ai, bool throwException = false)
        {
            ai = GetAI(data, ref index);
            if (ai == null)
            {
                if (throwException)
                {
                    throw new InvalidOperationException("AI not found");
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the AI at the current position
        /// </summary>
        /// <param name="data">The row data from the scanner</param>
        /// <param name="index">The refrence of the current position</param>
        /// <param name="usePlaceHolder">Sets if the last character of the AI should replaced with a placehoder ("d")</param>
        /// <returns>The current AI or null if no match was found</returns>
        private AI GetAI(string data, ref int index, bool usePlaceHolder = false)
        {
            AI Ai = null;
            // Step through the different lengths of the AIs
            for (int i = GS1.MinLengthOfAI; i <= GS1.MaxLengthOfAI; i++)
            {
                string ai = GetAISubstring(data, index, usePlaceHolder, i, out int numberOfDecimals);

                if (GS1.TryGetAI(ai, out Ai))
                {
                    index += i;
                    Ai.NumberOfDecimals = numberOfDecimals; // to then parse the correct decimals
                    return Ai;
                }
            }
            // if no AI found here, than try it with placeholders. Assumed that is the first step where usePlaceHolder is false
            if (!usePlaceHolder)
            {
                Ai = GetAI(data, ref index, true);
            }
            return Ai;
        }

        private string GetAISubstring(string data, int index, bool usePlaceHolder, int i, out int numberOfDecimals)
        {
            string ai = data.Substring(index, i);
            numberOfDecimals = 0;
            if (usePlaceHolder)
            {
                numberOfDecimals = int.Parse(ai.Last().ToString());

                ai = ai.Remove(ai.Length - 1) + GS1.PLACEHOLDER;
            }
            return ai;
        }

        /// <summary>
        /// Get the current code to the AI
        /// </summary>
        /// <param name="data">The row data from the scanner</param>
        /// <param name="ai">The current AI</param>
        /// <param name="index">The refrence of the current position</param>
        /// <returns>the data of the current AI</returns>
        private string GetCode(string data, AI ai, ref int index)
        {
            // get the max length to read.
            int lengthToRead = Math.Min(ai.LengthOfData, data.Length - index);
            // get the data of the current AI
            string code = data.Substring(index, lengthToRead);

            if (ai.GroupSeparatorPresent)
            {
                code = GetCodeUntilGroupSeparator(code, data, index, ai, ref lengthToRead);
            }

            index += lengthToRead;
            return code;
        }

        private string GetCodeUntilGroupSeparator(string codeSubstring, string data, int index, AI ai, ref int lengthToRead)
        {
            lengthToRead = GetLengthToReadWithSeparator(codeSubstring, data, index, ai, lengthToRead);

            return data.Substring(index, lengthToRead).Split(GS1.GROUP_SEPARATOR).FirstOrDefault();
        }

        private int GetLengthToReadWithSeparator(string codeSubstring, string data, int index, AI ai, int lengthToRead)
        {
            int indexOfGroupTermination = codeSubstring.IndexOf(GS1.GROUP_SEPARATOR);
            if (indexOfGroupTermination >= 0)
            {
                lengthToRead = indexOfGroupTermination + 1;
            }
            else if (ai.LengthOfData == lengthToRead) // if the code fills the max amount until the group separator and the code is not at the end, e.g. 8005000365@10123456
            {
                if (index + lengthToRead < data.Length)
                {
                    lengthToRead += 1;
                }
            }
            return lengthToRead;
        }

        public DateTime GetDateTime(string date)
        {
            return DateTime.ParseExact(date, GS1.DATE_FORMAT, null);
        }

        public dynamic GetValue(string ai)
        {
            if (GS1.TryGetAI(ai, out AI Ai))
            {
                if (Ai.DataDescription == DataType.Date)
                {
                    return GetDate(ai);
                }
                else if (Ai.DataDescription == DataType.Numeric)
                {
                    return GetDecimal(Ai);
                }
                else
                {
                    return GetString(ai);
                }
            }
            return null;
        }

        public string GetString(string ai)
        {
            if (Result.TryGetValue(ai, out string value))
            {
                return value;
            }
            return string.Empty;
        }

        public string GetStringWithoutLeadingZeroes(string ai)
        {
            return GetString(ai).TrimStart('0');
        }

        public DateTime GetDate(string ai)
        {
            if (Result.TryGetValue(ai, out string value))
            {
                return GetDateTime(value);
            }
            return DateTime.MinValue;
        }

        public decimal GetDecimal(AI ai)
        {
            if (Result.TryGetValue(ai.Id, out string value))
            {
                var info = new NumberFormatInfo();
                if (ai.NumberOfDecimals > 0)
                {
                    value = value.Insert(value.Length - ai.NumberOfDecimals, info.NumberDecimalSeparator);
                }
                return Convert.ToDecimal(value, info);
            }
            return 0;
        }

        public decimal GetDecimal(string ai)
        {
            if (GS1.TryGetAI(ai, out AI Ai))
            {
                return GetDecimal(Ai);
            }
            return 0;
        }

        public int GetInt(string ai)
        {
            if (Result.TryGetValue(ai, out string value))
            {
                return int.Parse(value);
            }
            return 0;
        }

        public bool ContainsAI(string ai)
        {
            return Result.ContainsKey(ai);
        }

        public bool ContainsAnyAI(params string[] ais)
        {
            if (Result.Count > 0)
            {
                foreach (var ai in ais)
                {
                    if (Result.ContainsKey(ai))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
    public class GtinValidator
    {
        private readonly string ValidationRegex = "^(\\d{12,14}|\\d{8})$";

        private int ToInt(string numString)
        {
            return int.Parse(numString);
        }

        private static bool IsOdd(int num)
        {
            return (num % 2) == 1;
        }

        public bool Validate(string barcode)
        {
            if (!Regex.IsMatch(barcode, ValidationRegex))
            {
                return false;
            }

            int checksum = ToInt(barcode.Substring(barcode.Length - 1));
            int calcChecksum = this.CalculateChecksum(barcode);

            return (checksum == calcChecksum);
        }

        private int CalculateChecksum(string gtin)
        {
            var chunks = gtin.ToCharArray()
                .Select(n => ToInt(n.ToString()))
                .Reverse()
                .ToList();
            int checksum = 0;

            // Remove first chuck (checksum)
            chunks.RemoveAt(0);

            for (int i = 0; i < chunks.Count; i++)
            {
                checksum += IsOdd(i) ? chunks[i] : chunks[i] * 3;
            }

            // calc checksum
            checksum %= 10;
            checksum = (checksum == 0) ? 0 : (10 - checksum);

            return checksum;
        }
    }
}
