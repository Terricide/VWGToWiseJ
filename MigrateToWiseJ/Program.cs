using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateToWiseJ
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please pass in the directory you want to scan");
                return;
            }

            var rootDir = args[0];

            ScanDir(rootDir);
        }

        static void ScanDir(string parentDir)
        {
            if (parentDir.Contains(".git"))
            {
                return;
            }

            if (parentDir.Contains(@"\obj\") || parentDir.Contains(@"\bin\"))
            {
                return;
            }

            foreach (var dir in Directory.GetDirectories(parentDir))
            {
                ScanDir(dir);
            }

            Console.WriteLine("Processing dir:" + parentDir);

            var validFilesExts = new string[] { ".cs", ".resx", ".csproj" };

            foreach (var file in Directory.GetFiles(parentDir, "*.*"))
            {
                Console.WriteLine("Processing file:" + file);
                var ext = Path.GetExtension(file);
                if (!validFilesExts.Contains(ext))
                {
                    continue;
                }

                ParseFile(file);
            }
        }

        public static string[] InvalidLines = new string[] { "CustomStyle = \"AnimatedPanel\"", ".CustomStyle = \"F\"", ".BorderColor = ", ".ClickOnce = true;",
            "BorderWidth = new Wisej.Web.BorderWidth(0)", "BorderWidth = new Gizmox.WebGUI.Forms.BorderWidth(0)",
            "<assembly alias=\"Gizmox.WebGUI.Forms\" name=\"Gizmox.WebGUI.Forms, Version = 4.6.5701.0, Culture = neutral, PublicKeyToken = c508b41386c60f1d\" />",
            "CustomStyle = \"OpacityPanel\"", "Opacity = ((Wisej.Web.Skins.OpacityValue)(resources.GetObject(", ".AutoValidate = ", ".CustomStyle = \"Logical\"",
            ".ClientMode = false", ".Appearance = Gizmox.WebGUI.Forms.TabAppearance.Logical", ".Appearance = Wisej.Web.TabAppearance.Logical", ".AutoGenerateColumns = true;",
            "ItemsPerPage = 20", ".CustomStyle = \"Masked\"", "FlatStyle = Wisej.Web.FlatStyle.Flat;", "FlatStyle = Gizmox.WebGUI.Forms.FlatStyle.Flat;", ".DropDownWidth = ",
            ".DataMember = null;", ".FullRowSelect = true;", ".ItemsPerPage = 200;", ".UseInternalPaging = true;", ".BorderStyle = Wisej.Web.BorderStyle.Fixed3D;",
            ".BorderStyle = Gizmox.WebGUI.Forms.BorderStyle.Fixed3D;", ".EnterKeyDown += " };

        private static void ParseFile(string file)
        {
            var text = File.ReadAllText(file);
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            bool hasChanges = false;
            foreach (var line in lines)
            {
                var newLine = line;

                bool invalidLine = false;
                foreach (var invLine in InvalidLines)
                {
                    if (newLine.IndexOf(invLine, StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        invalidLine = true;
                        break;
                    }
                }

                if (invalidLine)
                {
                    hasChanges = true;
                    continue;
                }

                newLine = ReplaceLine(newLine, "Gizmox.WebGUI.Forms.", "Wisej.Web.", ref hasChanges);
                newLine = ReplaceLine(newLine, "Gizmox.WebGUI.Forms;", "Wisej.Web;", ref hasChanges);
                newLine = ReplaceLine(newLine, "Gizmox.WebGUI.Common", "Wisej.Web", ref hasChanges);

                newLine = ReplaceLine(newLine, "Image = new Wisej.Web.Resources.IconResourceHandle(resources.GetString(", "ImageSource = ", ref hasChanges, (str) =>
                {
                    str = str.TrimEnd(new char[] { ')', ';' });
                    return str += ";";
                });
                newLine = ReplaceLine(newLine, "Image = new Wisej.Web.Resources.ImageResourceHandle(resources.GetString(", "ImageSource = ", ref hasChanges, (str) =>
                {
                    str = str.TrimEnd(new char[] { ')', ';' });
                    return str += ";";
                });

                newLine = ReplaceLine(newLine, "Image = new Wisej.Web.Resources.IconResourceHandle(", "ImageSource = ", ref hasChanges, (str) =>
                {
                    str = str.TrimEnd(new char[] { ')', ';' });
                    return str += ";";
                });
                newLine = ReplaceLine(newLine, "Image = new Wisej.Web.Resources.ImageResourceHandle(", "ImageSource = ", ref hasChanges, (str) =>
                {
                    str = str.TrimEnd(new char[] { ')', ';' });
                    return str += ";";
                });

                newLine = ReplaceLine(newLine, "Gizmox.WebGUI.Forms.BorderStyle.FixedSingle", "Wisej.Web.BorderStyle.Solid", ref hasChanges, (str) =>
                {
                    str = str.TrimEnd(new char[] { ')', ';' });
                    return str += ";";
                });

                newLine = ReplaceLine(newLine, "Gizmox.WebGUI.Forms.BorderStyle.FixedSingle", "Wisej.Web.BorderStyle.Solid", ref hasChanges);
                newLine = ReplaceLine(newLine, "BorderStyle.Clear", "BorderStyle.None", ref hasChanges);
                newLine = ReplaceLine(newLine, ".CurrentValue = new decimal(", ".Value = new decimal(", ref hasChanges);
                newLine = ReplaceLine(newLine, "Wisej.Web.Form.FormClosedEventHandler", "Wisej.Web.FormClosedEventHandler", ref hasChanges);
                newLine = ReplaceLine(newLine, "Wisej.Web.Resources.ResourceHandle", "System.Drawing.Bitmap", ref hasChanges);
                newLine = ReplaceLine(newLine, ", Gizmox.WebGUI.Forms", ", Wisej.Web", ref hasChanges);

                sb.Append(newLine + Environment.NewLine);
            }
            if (hasChanges)
            {
                File.WriteAllText(file, sb.ToString());
            }
            sb = null;
        }

        static string ReplaceLine(string newLine, string current, string replacement, ref bool hasChanges, Func<string, string> action = null)
        {
            if (newLine.IndexOf(current, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                hasChanges = true;
                newLine = newLine.ReplaceString(current, replacement, StringComparison.CurrentCultureIgnoreCase);
                if (action != null)
                {
                    newLine = action.Invoke(newLine);
                }
            }
            return newLine;
        }
    }

    static class Extend
    {
        public static string ReplaceString(this string str, string oldValue, string newValue, StringComparison comparison)
        {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }
}
