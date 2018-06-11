using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using Newtonsoft.Json.Linq;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

public static class ZipArchiveExtensionMethods
	{
	public static void ExtractToDirectory2(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
		{
		if (!overwrite)
			{
			archive.ExtractToDirectory(destinationDirectoryName);
			return;
			}
		foreach (ZipArchiveEntry file in archive.Entries)
			{
			string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
			if (file.Name == "")
				{// Assuming Empty for Directory
				Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
				continue;
				}
			// create dirs
			var dirToCreate = destinationDirectoryName;
			for (var i = 0; i < file.FullName.Split('/').Length - 1; i++)
				{
				var s = file.FullName.Split('/')[i];
				dirToCreate = Path.Combine(dirToCreate, s);
				if (!Directory.Exists(dirToCreate))
					Directory.CreateDirectory(dirToCreate);
				}
			file.ExtractToFile(completeFileName, true);
			}
		}
	}

namespace RedoxDataModelBuilder
{
	public class FileManager
		{
		private static WebClient cli = new WebClient();
		public FileManager() { }
		public byte[] GetFile(string url, string file, out string error)
			{
			byte[] data;
			try
				{
				data = cli.DownloadData(url);
				File.WriteAllBytes(file, data);
				error = "";
				}
			catch (WebException wex)
				{
				data = null;
				error = wex.Message;
				}
			return data;
			}
		public bool UnzipFile(string file, string unzipPath, out string error)
			{
			try
				{
				using (FileStream zipToOpen = new FileStream(file, FileMode.Open))
					{
					using (ZipArchive archive = new ZipArchive(zipToOpen))
						{
						archive.ExtractToDirectory2(unzipPath, true);
						}
					}
				error = "";
				return true;
				}
			catch (Exception ex)
				{
				error = ex.Message;
				}
			return false;
			}
		public List<string> GetFileList(string dir, out string error)
			{
			error = "";
			string[] fileList = null;
			try
				{
				return Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories).ToList();
				}
			catch (Exception ex)
				{
				error = ex.Message;
				return fileList.ToList();
				}
			}
		public string GetFileString(string file, out string error)
			{
			error = "";
			try
				{
				return File.ReadAllText(file);
				}
			catch (Exception ex)
				{
				error = ex.Message;
				return "";
				}
			}
		}
	public class ClassBuilder
		{
		private bool CompileCSharpCode(string script, string filename, out List<string> errors)
			{
			errors = new List<string>();
			try
				{
				CSharpCodeProvider provider = new CSharpCodeProvider();
				CompilerParameters cp = new CompilerParameters
					{
					GenerateInMemory = false,
					GenerateExecutable = false,
					IncludeDebugInformation = true,
					OutputAssembly = filename,
					};
				cp.ReferencedAssemblies.Add("System.dll");
				cp.ReferencedAssemblies.Add("Newtonsoft.Json.dll");
				cp.ReferencedAssemblies.Add("System.ComponentModel.DataAnnotations.dll");
				CompilerResults cr = provider.CompileAssemblyFromSource(cp, script);
				if (cr.Errors.Count > 0)
					{
					foreach (CompilerError ce in cr.Errors)
						{
						errors.Add(ce.ToString());
						}
					return false;
					}
				provider.Dispose();
				}
			catch (Exception e)
				{
				errors.Add("Error: " + e.Message + e.StackTrace.ToString());
				return false;
				}
			return true;
			}

		private Dictionary<string, string> ObjectTypeDict = new Dictionary<string, string>();
		private Dictionary<string, string> ComplexObjectDict = new Dictionary<string, string>();
		private Dictionary<string, List<JObject>> ComplexObjectLists = new Dictionary<string, List<JObject>>();
		private List<JObject> AllObjects = new List<JObject>();
		private JObject defs = new JObject();
		public enum eOutputType
			{
				Library = 0,
				CSharpFile = 1
			}
		private eOutputType outputAs = eOutputType.Library;
		private class RedoxJsonSchema
			{
			public string Category;
			public string EndPointName;
			public JObject Schema;
			public RedoxJsonSchema(string c, string epn, string st)
				{
				Category = c;
				EndPointName = epn;
				Schema = JObject.Parse(st);
				}
			}
		private string GetNewName(string name, Dictionary<string, string> dict)
			{
			int curappend = 0;
			int idx = name.LastIndexOf('_');
			if (idx != -1)
				name = name.Substring(0, idx);
			string newname = name;
			while (dict.ContainsKey(newname))
			{
				curappend += 1;
				newname = name + "_" + curappend.ToString();
			}
			return newname;
			}
		private string GetNewName(string name, Dictionary<string, JProperty> dict)
			{
			int curappend = 0;
			int idx = name.LastIndexOf('_');
			if (idx != -1)
				name = name.Substring(0, idx - 1);
			string newname = name;
			while (dict.ContainsKey(newname))
				{
				curappend += 1;
				newname = name + "_" + curappend.ToString();
				}
			return newname;
			}
		private bool IsSimpleObject(JObject jo, out string error)
			{
			error = "";
			int test = 0;
			bool isSimple = true;
			try
				{
				JProperty jp = jo.Children<JProperty>().FirstOrDefault(p => p.Name == "properties");
				JEnumerable<JProperty> jpcl;
				JObject jocc;
				if (jp == null) { return true; }
				foreach (JObject joc in jp.Children())
					{
					jpcl = joc.Children<JProperty>();
					foreach (JProperty jpcc in jpcl)
						{
						jocc = jpcc.Children<JObject>().FirstOrDefault();
						if (jocc["type"].ToString() == "object" || jocc["type"].ToString() == "array")
							return false;
						else
							isSimple = IsSimpleObject(jocc, out error);
						test += 1;
						}
					}
				}
			catch (Exception ex)
				{
				error = ex.Message;
				}
			return isSimple;
			}
		private string ParseSchemaDefinitions(RedoxJsonSchema rs)
			{
			string tokenName;
			string typeName;
			string error;
			try
				{
				JObject tokenItems;
				JToken token;
				JProperty tokenParent;
				var sroot = new JObject();
				var objs = sroot.Descendants();
				var comparer = new JTokenEqualityComparer();
				var objectToken = JToken.FromObject("object");
				sroot = rs.Schema;
				objs = sroot.Descendants()
				.OfType<JObject>()
				.Where(t => comparer.Equals(t["type"], objectToken))
				.ToList();
				foreach (JObject o in objs)
					{
					AllObjects.Add(o);
					tokenParent = (JProperty)o.Parent;
					tokenName = tokenParent.Name;
					if (tokenName == "items") { continue; }
					token = o;
					if (!IsSimpleObject(o, out error))
						continue;
					if (!string.IsNullOrEmpty(error))
						return error;
					if (!ObjectTypeDict.ContainsKey(tokenName))
						{
						ObjectTypeDict.Add(tokenName, token.Parent.ToString());
						token["title"] = tokenName;
						defs.Add(new JProperty(tokenName, token));
						}
					else if (!ObjectTypeDict.ContainsValue(token.Parent.ToString()))
						{
						tokenName = GetNewName(tokenName, ObjectTypeDict);
						ObjectTypeDict.Add(tokenName, token.Parent.ToString());
						token["title"] = tokenName;
						defs.Add(new JProperty(tokenName, token));
						}
					else
						token["title"] = tokenName;
					}
				var arrayToken = JToken.FromObject("array");
				var arrObjs = sroot.Descendants()
				.OfType<JObject>()
				.Where(t => comparer.Equals(t["type"], arrayToken))
				.ToList();
				foreach (JToken a in arrObjs)
					{
					tokenName = ((JProperty)a.Parent).Name;
					tokenItems = (JObject)a["items"];
					typeName = tokenName + "Item";
					if (!ObjectTypeDict.ContainsKey(typeName))
						{
						if (IsSimpleObject(tokenItems, out error))
							{
							defs.Add(new JProperty(typeName, tokenItems));
							ObjectTypeDict.Add(typeName, tokenItems.Parent.ToString());
							}
						if (!string.IsNullOrEmpty(error))
							return error;
						tokenItems["title"] = typeName;
						}
					else if (!ObjectTypeDict.ContainsValue(tokenItems.Parent.ToString()))
						{
						typeName = GetNewName(typeName, ObjectTypeDict);
						if (IsSimpleObject(tokenItems, out error))
							{
							defs.Add(new JProperty(typeName, tokenItems));
							ObjectTypeDict.Add(typeName, tokenItems.Parent.ToString());
							}
						if (!string.IsNullOrEmpty(error))
							return error;
						tokenItems["title"] = typeName;
						}
					else
						tokenItems["title"] = typeName;
					}
				foreach (JObject o in objs)
					{
					tokenParent = (JProperty)o.Parent;
					tokenName = tokenParent.Name;
					if (tokenName == "items") { continue; }
					if (!IsSimpleObject(o, out error))
						{
						if (!string.IsNullOrEmpty(error))
							return error;
						if (!ComplexObjectDict.ContainsKey(tokenName) && !ObjectTypeDict.ContainsKey(tokenName))
							{
							if (ComplexObjectDict.ContainsValue(o.Parent.ToString()))
								tokenName = GetNewName(tokenName, ComplexObjectDict);
							ComplexObjectDict.Add(tokenName, o.Parent.ToString());
							ComplexObjectLists.Add(tokenName, new List<JObject>());
							ComplexObjectLists[tokenName].Add(o);
							}
						else
							{
							if (ComplexObjectDict.ContainsValue(o.Parent.ToString()))
								{
								if (ComplexObjectLists[tokenName] == null)
									ComplexObjectLists[tokenName] = new List<JObject>();
								ComplexObjectLists[tokenName].Add(o);
								}
							else
								{
								tokenName = GetNewName(tokenName, ComplexObjectDict);
								ComplexObjectDict.Add(tokenName, o.Parent.ToString());
								ComplexObjectLists.Add(tokenName, new List<JObject>());
								ComplexObjectLists[tokenName].Add(o);
								}
							}
						o["title"] = tokenName;
						}
					if (!string.IsNullOrEmpty(error))
						return error;
					}
				}
			catch (Exception ex)
				{
				return ex.Message;
				}
			return "";
			}

		private string ParseSchemas(out string error)
			{
			error = "";
			JObject root = new JObject();
			try
				{
				root["$schema"] = "http://json-schema.org/draft-04/schema#";
				root["type"] = "object";
				JObject rootProperties = new JObject();
				string tokenName;
				JToken token;
				JProperty tokenParent;
				var sroot = new JObject();
				var objs = sroot.Descendants();
				var comparer = new JTokenEqualityComparer();
				var objectToken = JToken.FromObject("object");
				foreach (RedoxJsonSchema rs in schemaTextList)
					{
					sroot = rs.Schema;
					sroot["title"] = rs.Category + "_" + rs.EndPointName;
					sroot.Property("$schema").Remove();
					rootProperties.Add(new JProperty(rs.Category + "_" + rs.EndPointName, sroot));
					}
				foreach (JObject o in AllObjects)
					{
					token = o;
					tokenParent = (JProperty)token.Parent;
					tokenName = tokenParent.Name;
					if (token["title"] == null || !IsSimpleObject(o, out error)) { continue; }
					if (!string.IsNullOrEmpty(error))
						return "";
					if (ObjectTypeDict.ContainsKey(token["title"].ToString()))
						{
						token.Parent.Replace(new JProperty(tokenName, JObject.Parse("{ \"$ref\" : \"#/definitions/" + token["title"].ToString() + "\" }")));
						}
					}
				root.Add(new JProperty("properties", rootProperties));
				foreach (KeyValuePair<string, List<JObject>> def in ComplexObjectLists)
					{
					if (def.Value.Count() < 2)
						continue;
					if (!defs.ContainsKey(def.Key))
						defs.Add(new JProperty(def.Key, def.Value[0]));
					foreach (JObject jo in def.Value)
						if (jo.Parent.Parent != null)
							jo.Parent.Replace(new JProperty(def.Key, JObject.Parse("{ \"$ref\" : \"#/definitions/" + def.Key + "\" }")));
					}
				JObject defschk = (JObject)root["definitions"];
				if (defschk == null)
					root.Add(new JProperty("definitions", defs));
				else
					{
					defschk.Remove();
					root.Add(new JProperty("definitions", defs));
					}
				}
			catch (Exception ex)
				{
				error = ex.Message;
				return "";
				}
			return root.ToString();
			}
		private static CSharpTypeResolver resolver;
		private static CSharpGeneratorSettings csgs;
		private static CSharpGenerator generator;
		private static List<RedoxJsonSchema> schemaTextList;
		public ClassBuilder(string nameSpace, eOutputType outputtype)
			{
			outputAs = outputtype;
			JsonSchema4 js = new JsonSchema4();
			csgs = new CSharpGeneratorSettings();
			csgs.Namespace = nameSpace;
			resolver = new CSharpTypeResolver(csgs);
			CSharpGenerator generator = new CSharpGenerator(js, csgs, resolver);
			schemaTextList = new List<RedoxJsonSchema>();
			AllObjects.Clear();
			ObjectTypeDict.Clear();
			ComplexObjectDict.Clear();
			ComplexObjectLists.Clear();
			defs = new JObject();
			}
		private async Task<JsonSchema4> FromJsonAsync(string schematext)
			{
			return await JsonSchema4.FromJsonAsync(schematext);
			}
		public bool AddSchema(string schemaText, string filename, out string error)
			{
			filename = Path.ChangeExtension(filename, null);
			string[] lvls = filename.Split('\\');
			string category = lvls[lvls.Length - 2];
			string endpointname = lvls[lvls.Length - 1];
			endpointname = char.ToUpper(endpointname[0]) + endpointname.Substring(1);
			category = char.ToUpper(category[0]) + category.Substring(1);
			RedoxJsonSchema rs = new RedoxJsonSchema(category, endpointname, schemaText);
			schemaTextList.Add(rs);
			error = ParseSchemaDefinitions(rs);
			return string.IsNullOrEmpty(error);
			}
		public bool GenerateClassLibrarys(out List<string> errors, string outFileName)
			{
			errors = new List<string>();
			outFileName = Path.Combine(System.IO.Path.GetDirectoryName(outFileName), Path.GetFileNameWithoutExtension(outFileName));
			string error;
			string curSchema;
			Task<JsonSchema4> schema;
			curSchema = ParseSchemas(out error);
			if (!string.IsNullOrEmpty(error))
				{
				errors.Add(error);
				return false;
				}
			try
				{
				schema = FromJsonAsync(curSchema);
				schema.Result.Title = "Models";
				generator = new CSharpGenerator(schema.Result, csgs, resolver);
				generator.GenerateTypes(schema.Result, "");
				string file = generator.GenerateFile();
				if (outputAs == eOutputType.Library)
					{
					return CompileCSharpCode(file, outFileName + ".dll", out errors);
					}
				try
					{
					File.WriteAllText(outFileName + ".cs", file);
					}
				catch (Exception ex)
					{
					errors.Add(ex.Message);
					return false;
					}
				}
			catch (Exception ex)
				{
				errors.Add(ex.Message);
				return false;
				}
			return true;
			}
		}
}
