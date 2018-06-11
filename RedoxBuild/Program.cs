using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleHotKey;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using RedoxDataModelBuilder;

namespace RedoxBuild
	{
	public static class ExtensionMethods
		{
		public static string RemoveNonAlphaNumeric(this string s)
			{
			char[] arr = s.Where(c => (char.IsLetterOrDigit(c))).ToArray();
			return new string(arr);
			}
		}
	class Program
		{

		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		const int SW_HIDE = 0;
		const int SW_SHOW = 5;

		static bool usecmd = false;
		static void Main(string[] args)
			{
			var handle = GetConsoleWindow();

			string nameSpace = "";
			string usercatlist = "";
			string filepath= "";
			string url = "";
			string libtype = "DLL";
			Uri uriResult;
			DirectoryInfo di;
			string ext;
			List<string> categorylist = new List<string>();
			categorylist.AddRange(new string[] { "all", "claim", "clinicaldecisions", "clinicalsummary", "device", "financial",
				"flowsheet", "inventory", "media", "notes", "order", "patientadmin", "patientsearch", "provider", "referral", "results",
				"scheduling", "sso", "surgicalscheduling", "vaccination" });

			Dictionary<string, string> argDict = new Dictionary<string, string>();
			List<string> arglist = args.ToList();
			usecmd = (arglist.Count > 0);
			if (usecmd)
				{
				bool isArg = false;
				string cmdstr = "";
				string lastparam = "";
				foreach (string cmd_arg in arglist)
					{
					if (!isArg)
						{
						if (cmd_arg == "/?")
							{
							Console.WriteLine("Usage:");
							Console.WriteLine("/showwin: ");
							Console.WriteLine("\t show/hide Console window (\"true\" or \"false\"");
							Console.WriteLine("/outputtype: ");
							Console.WriteLine("\t The output file library Type (\"DLL\" or \"CS\")");
							Console.WriteLine("/outputfile: ");
							Console.WriteLine("\t The full file output path");
							Console.WriteLine("/url: ");
							Console.WriteLine("\t The URL of the Redox Schema ZIP file");
							Console.WriteLine("/namespace: ");
							Console.WriteLine("\t The top level namespace for the generated classes");
							Console.WriteLine("/models: ");
							Console.WriteLine("\t A Comma-separated list of Models to use, OR \"all\":");
							Console.WriteLine("\t\t" + string.Join("\r\n\t\t", categorylist.ToArray()));
							Console.WriteLine("");
							userExit(-2, true);
							}
						cmdstr = cmd_arg.RemoveNonAlphaNumeric();
						if (argDict.ContainsKey(cmdstr))
							{
							WriteLine("Duplicate command: " + cmd_arg);
							userExit(-2, false);
							}
						else
							argDict.Add(cmdstr, "");
						isArg = true;
						lastparam = cmdstr;
						}
					else
						{

						switch (lastparam)
							{
							case "outputtype":
								libtype = cmd_arg.ToUpper();
								break;
							case "outputfile":
								filepath = cmd_arg;
								break;
							case "url":
								url = cmd_arg;
								break;
							case "namespace":
								nameSpace = cmd_arg;
								break;
							case "models":
								usercatlist = cmd_arg;
								break;
							case "showwin":
								if (cmd_arg == "false")
									ShowWindow(handle, SW_HIDE);
								break;
							default:
								WriteLine("Unknown command: " + lastparam);
								userExit(-2, false);
								break;
							}
						argDict[lastparam] = cmd_arg;
						isArg = false;
						lastparam = "";
						}


					}
				}

			if (string.IsNullOrWhiteSpace(filepath)) 
				filepath = @"D:\KioskUI\iRedox\bin\Debug\RedoxModels.dll";
			if (string.IsNullOrWhiteSpace(url))
				url = "https://developer.redoxengine.com/data-models/schemas.zip";

			HotKeyManager.RegisterHotKey(Keys.Q, KeyModifiers.Control);
			HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);
			//Console.TreatControlCAsInput = true;

			bool invalidpath = true, invalidinput = true;

			WriteLine("Redox Data Model Builder Helper Application v1");
			WriteLine("Press Control + Q to Quit");

			Console.WriteLine("");

			while (invalidpath)
				{
				invalidinput = true;
				Write("Please specify the URL to the Redox schema ZIP file: ");
				if (!usecmd)
					SendKeys.SendWait(url);
				if (!usecmd)
					url = ReadLine(); 
				ext = url.Substring(url.Length - 3);

				invalidpath = !((Uri.TryCreate(url, UriKind.Absolute, out uriResult)
					 && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) && ext.ToLower() == "zip");
				
				if (invalidpath)
					{
					Console.WriteLine("Invalid URL: " + filepath);
					Console.WriteLine("Requirements: Valid http(s) url, filetype must be \"zip\"");
					url = "";
					if (usecmd) { userExit(-3, false); }
					}
				else
					invalidpath = false;
				WriteLine("");
				}

			invalidinput = true;
			invalidpath = true;

			while (invalidinput)
				{
				Write("Please specify Library type. (\"D\" = DLL, \"C\" = CSharp file: ");
				if (!usecmd)
					libtype = ReadLine().ToUpper();
				if (libtype == "C" || libtype == "D")
					{
					libtype = (libtype == "C") ? "cs" : "dll";
					invalidinput = false;
					}
				else
					{
					Console.WriteLine("Invalid Input: " + libtype);
					if (usecmd) { userExit(-3, false); }
					}
				WriteLine("");
				}

			invalidinput = true;
			invalidpath = true;

			while (invalidpath)
				{
				invalidinput = true;
				Write("Please specify a path to output the library file: ");
				if (!usecmd)
					SendKeys.SendWait(filepath);
				if (!usecmd)
					filepath = ReadLine();
				ext = "";
				if (!string.IsNullOrWhiteSpace(filepath) && FilePathIsValid(filepath))
					{ 
					di = Directory.CreateDirectory(Path.GetDirectoryName(filepath));
					invalidinput = !di.Exists;
					ext = Path.GetExtension(filepath).Replace(".", string.Empty);
					}
				if (invalidinput || ext.ToLower() != libtype.ToLower())
					{
					Console.WriteLine("Invalid Path: " + filepath);
					Console.WriteLine("Requirements: Valid file path, filetype must be \"" + libtype + "\"");
					filepath = "";
					if (usecmd) { userExit(-3, false); }
					}
				else
					invalidpath = false;
				WriteLine("");
				}

			invalidinput = true;

			while (invalidinput)
				{
				Write("Please specify a Namespace name: ");
				if (!usecmd)
					nameSpace = ReadLine();
				if (!IsAlphaNum(nameSpace))
					{
					Console.WriteLine("Invalid namespace: " + nameSpace);
					Console.WriteLine("Requirements: must be Alpha-Numeric, no whitespace");
					nameSpace = "";
					if (usecmd) { userExit(-3, false); }
					}
				else
					invalidinput = false;
				WriteLine("");
				}

			invalidinput = true;


			string error = "";

			List<string> usercategories = new List<string>();
			while (invalidinput)
				{
				invalidinput = true;
				WriteLine("Redox DataModel Categories: ");
				WriteLine(string.Join("\r\n", categorylist.ToArray()));
				WriteLine("");
				WriteLine("Please type a distinct list of comma separated Models to include");
				WriteLine("Or type \"all\" to include all models.");
				if (!string.IsNullOrEmpty(error))
					{ 
					Console.WriteLine(error);
					if (usecmd) { userExit(-3, false); }
					}
				if (!usecmd)
					usercatlist = ReadLine().ToLower();
				if (!IsCommaList(usercatlist))
					{
					error = "Error: Must be a distinct comma-separated list of valid Model Categories, OR \"all\"";
					usercatlist = "";
					if (usecmd) { userExit(-3, false); }
					}
				else
					{
					usercatlist = usercatlist.Replace(" ", "");
					if (usercatlist == "all")
						{
						usercategories.AddRange(new string[] { "all" });
						invalidinput = false;
						}
					else { 
						usercategories = usercatlist.Split(',').ToList();
						invalidinput = false;
						foreach (string cat in usercategories)
							{
							if (!categorylist.Contains(cat) || usercategories.Contains("all") || usercategories.Count != usercategories.Distinct().Count())
								{
								error = "Error: Invalid Model Category: " + cat + "\r\nError: Must be a distinct comma-separated list of valid Model Categories, OR \"all\"";
								invalidinput = true;
								usercategories.Clear();
								usercatlist = "";
								break;
								}
							}
						}
					}
				WriteLine("");
				}

			WriteLine("");
			WriteLine("");
			WriteLine("Downloading Schema Zip: " + url);


			List<string> categories = new List<string>();


			string errorout;
			List<string> errors;
			FileManager fm = new FileManager();
			string temp = Path.GetTempPath().TrimEnd('\\');
			string newTemp = temp + "\\Redoxschema";
			string fileName = temp + "\\schemas.zip";
			Directory.CreateDirectory(newTemp);
			fm.GetFile(url, fileName, out errorout);

			if (!string.IsNullOrEmpty(errorout))
				{
				Console.WriteLine("Download file error: " + errorout);
				userExit(-4, !usecmd);
				}

			WriteLine("Unzipping file: " + fileName);

			fm.UnzipFile(fileName, newTemp, out errorout);

			if (!string.IsNullOrEmpty(errorout))
				{
				Console.WriteLine("Unzip file error: " + errorout);
				userExit(-4, !usecmd);
				}

			WriteLine("Getting file list in: " + newTemp);

			List<string> fileList = fm.GetFileList(newTemp, out errorout);

			if (!string.IsNullOrEmpty(errorout))
				{
				Console.WriteLine("Unzip file error: " + errorout);
				userExit(-4, !usecmd);
				}

			if (fileList.Count <= 0)
				{
				Console.WriteLine("Error: Schema zip contains no files!");
				userExit(-4, !usecmd);
				}

			WriteLine("Building Endpoint list...");

			string schemaText = "";
			bool failed;
			Dictionary<string, List<string>> redoxCategoryIndexLists = new Dictionary<string, List<string>>();

			if (usercategories[0] == "all")
				categories.AddRange(fileList.GetRange(0, fileList.Count));
			else
				{
				foreach (string file in fileList)
					{
					string[] lvls = file.Split('\\');
					string category = lvls[lvls.Length - 2];
					if (redoxCategoryIndexLists.ContainsKey(category))
						redoxCategoryIndexLists[category].Add(file);
					else
						{
						redoxCategoryIndexLists.Add(category, new List<string>());
						redoxCategoryIndexLists[category].Add(file);
						}
					}
				foreach (string cat in usercategories)
					{
					categories.AddRange(redoxCategoryIndexLists[cat]);
					}
				}

			WriteLine("Adding schemas...");

			ClassBuilder.eOutputType outtype = (libtype.ToLower() == "dll") ? ClassBuilder.eOutputType.Library : ClassBuilder.eOutputType.CSharpFile;

			ClassBuilder cb = new ClassBuilder(nameSpace, outtype);
			foreach (string endpoint in categories)
				{
				WriteLine("Adding Schema: " + endpoint);
				schemaText = fm.GetFileString(endpoint, out errorout);
				if (!string.IsNullOrEmpty(errorout))
					{
					Console.WriteLine("Getting File contents error: " + errorout);
					userExit(-4, !usecmd);
					}
				cb.AddSchema(schemaText, endpoint, out errorout);
				if (!string.IsNullOrEmpty(errorout))
					{
					Console.WriteLine("Error Adding Schema: " + errorout);
					userExit(-4, !usecmd);
					}
				}
			WriteLine("Generating Class Library: " + filepath);
			cb.GenerateClassLibrarys(out errors, filepath);
			if (errors.Count() > 0)
				{
				Console.WriteLine("Compilation errors: ");
				foreach (string err in errors)
					Console.WriteLine(err);
				userExit(-4, !usecmd);
				}

			Console.WriteLine("File generated successfully");
			userExit(0, !usecmd);
			

			}



		public static bool IsAlphaNum(string str)
			{
			if (string.IsNullOrWhiteSpace(str))
				return false;

			return (str.ToCharArray().All(c => Char.IsLetter(c) || Char.IsNumber(c)));
			}
		public static bool IsCommaList(string str)
			{
			if (string.IsNullOrWhiteSpace(str))
				return false;

			return (str.ToCharArray().All(c => Char.IsLetter(c) || c.Equals(',') || c.Equals(' ')));
			}
		public static bool FilePathIsValid(string path)
			{
			FileInfo fi = null;
			int invalididx;
			char[] invalidpathchars = { '/', '*', '?', '\"', '<', '>', '|' };
			invalididx = path.IndexOfAny(invalidpathchars);
			try
				{
				fi = new FileInfo(path);
				}
			catch (ArgumentException) { }
			catch (PathTooLongException) { }
			catch (NotSupportedException) { }
			if (ReferenceEquals(fi, null))
				{
				return false;
				}
			bool test = Path.IsPathRooted(path);
			return (invalididx == -1 && !String.IsNullOrWhiteSpace(path)
		  && path.IndexOfAny(Path.GetInvalidPathChars().ToArray()) == -1
		  && Path.IsPathRooted(path)
		  && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));
			}
		public static string ReadLine()
			{
			string str = Console.ReadLine();
			if (str == null)
				return "";
			return str;
			}
		public static void WriteLine(string str)
			{
			if (!usecmd)
				Console.WriteLine(str);
			}
		public static void Write(string str)
			{
			if (!usecmd)
				Console.Write(str);
			}

		static void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
			{
			userExit(-1, false);
			}
		static void userExit(int code, bool pause)
			{
			if (pause)
				{
				Console.WriteLine("Press any key to Exit...");
				Console.ReadKey(true);
				}
			Environment.Exit(code);
			}
		}
	}
