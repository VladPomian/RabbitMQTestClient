using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ClientRabbit
{
    public class Decoder
    {
        public static void DecodeErrorDataAndShowMessage(string encodedXml)
        {
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(encodedXml);
                string decodedXmlStr = Encoding.UTF8.GetString(decodedBytes);

                try
                {
                    XDocument xmlDoc = XDocument.Parse(decodedXmlStr);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Decoded XML:");

                    bool categoriesFound = false;

                    foreach (var category in xmlDoc.Descendants())
                    {
                        if (category.Name == "CME" || category.Name == "FLR" || category.Name == "GST")
                        {
                            categoriesFound = true;

                            sb.AppendLine($"Category: {category.Name}");

                            foreach (var record in category.Descendants("record"))
                            {
                                string? date = record.Element("date")?.Value;
                                string? value = record.Element("value")?.Value;

                                if (category.Name == "CME" && !string.IsNullOrEmpty(value) &&
                                    double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numericCMEValue) &&
                                    numericCMEValue >= 730)
                                {
                                    sb.AppendLine($"Date: {date}, Value: {value}");
                                }

                                if (category.Name == "FLR" && !string.IsNullOrEmpty(value) &&
                                    double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numericFLRValue) &&
                                    numericFLRValue >= 5.3)
                                {
                                    sb.AppendLine($"Date: {date}, Value: {value}");
                                }

                                if (category.Name == "GST" && !string.IsNullOrEmpty(value) &&
                                    double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numericGSTValue) &&
                                    numericGSTValue >= 7.2)
                                {
                                    sb.AppendLine($"Date: {date}, Value: {value}");
                                }
                            }
                        }
                    }

                    if (!categoriesFound)
                    {
                        sb.AppendLine("Категории не найдены в XML.");
                    }

                    MessageBox.Show(sb.ToString(), "Decoded Error Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (System.Xml.XmlException)
                {
                    MessageBox.Show($"Decoded Error Message: {decodedXmlStr}", "Parsed String", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid Base64 encoded data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
