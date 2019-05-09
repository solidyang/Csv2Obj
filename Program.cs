using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CsvToObj
{
	class CsvToObj
	{
		enum ColType
		{
			Ignore = -1,
			Index = 0,
			Pos0,
			Pos1,
			Pos2,
			Normal0,
			Normal1,
			Normal2,
			Uv00,
			Uv01,
			Uv02,
			Uv03,
			Uv10,
			Uv11,
			Uv12,
			Uv13,
			//Color0,
			//Color1,
			//Color2,
			//Tan0,
			//Tan1,
			//Tan2,
			Count,
		}

		public void Convert(string srcPath)
		{
			string[] srcLines;
			try
			{
				srcLines = File.ReadAllLines(srcPath);
			}
			catch (Exception e)
			{
				Console.WriteLine(string.Format("read {0} failed, err = {1}", srcPath, e.Message));
				return;
			}

			if (srcLines == null || srcLines.Length == 0)
			{
				Console.WriteLine("file is empty!");
				return;
			}

			// title row
			string line = srcLines[0];
			var cols = line.Split(',');
			ParseColType(cols);

			// lines
			var lines = new List<float[]>();
			for (int i = 1; i < srcLines.Length; ++i)
			{
				cols = srcLines[i].Split(',');
				lines.Add(ParseLineData(cols));
			}

			var sb = new StringBuilder();
			var indexCol = m_typeCol[(int)ColType.Index];
			var indecis = new List<int>();
			if (indexCol >= 0)
			{
				for (int i = 0; ; ++i)
				{
					try
					{
						var l = lines.FindIndex(r => (int)r[indexCol] == i);
						if (l < 0)
							break;

						SaveVertexToObj(lines[l], sb);
						indecis.Add(l);
					}
					catch (Exception)
					{
						break;
					}
				}

				// save face index
				for (int i = 0; i < (lines.Count-1) / 3; ++i)
				{
					int i1 = (int) lines[i * 3][indexCol] + 1,
						i2 = (int) lines[i * 3 + 1][indexCol] + 1,
						i3 = (int) lines[i * 3 + 2][indexCol] + 1;
					sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", i1, i2, i3);
				}
			}
			else
			{
				foreach (float[] t in lines)
					SaveVertexToObj(t, sb);
			}

			// Save to .obj
			string outputPath = srcPath;
			var ext = srcPath.LastIndexOf('.');
			var slash = srcPath.LastIndexOfAny(new[] { '/', '\\' });
			if (ext >= slash)
				outputPath = srcPath.Substring(0, ext);

			outputPath += ".obj";

			try
			{
				File.WriteAllText(outputPath, sb.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine(string.Format("save to {0} failed, err = {1}", srcPath, e.Message));
			}
		}

		private void SaveVertexToObj(float[] line, StringBuilder sb)
		{
			// pos
			if (m_typeCol[(int)ColType.Pos0] >= 0)
				sb.AppendFormat("v {0} {1} {2}\n", line[m_typeCol[(int)ColType.Pos0]], line[m_typeCol[(int)ColType.Pos1]]
					, line[m_typeCol[(int)ColType.Pos2]]);

			// normal
			int normal0Col = m_typeCol[(int) ColType.Normal0], normal1Col = m_typeCol[(int)ColType.Normal1], normal2Col = m_typeCol[(int)ColType.Normal2];
			if (normal0Col >= 0)
			{
				float n0 = line[normal0Col], n1 = line[normal1Col], n2 = line[normal2Col];
				if (m_isDecimalNormal)
				{
					n0 = n0 / 255 - 0.5f;
					n1 = n1 / 255 - 0.5f;
					n2 = n2 / 255 - 0.5f;
				}
				sb.AppendFormat("vn {0} {1} {2}\n", n0, n1, n2);
			}

			//uv0
			if (m_typeCol[(int) ColType.Uv00] >= 0)
			{
				sb.AppendFormat("vt {0} {1}", line[m_typeCol[(int) ColType.Uv00]], line[m_typeCol[(int) ColType.Uv01]]);
				if (m_typeCol[(int) ColType.Uv02] >= 0)
					sb.Append(line[m_typeCol[(int) ColType.Uv02]]);
				if (m_typeCol[(int)ColType.Uv03] >= 0)
					sb.Append(line[m_typeCol[(int)ColType.Uv03]]);
				sb.Append("\n");
			}
		}

		private void ParseColType(string[] cols)
		{
			m_colType = new ColType[cols.Length];
			for (int i = 0; i < cols.Length; ++i)
			{
				var ci = cols[i];
				m_colType[i] = ColType.Ignore;

				if (ci == "Index")
					m_colType[i] = ColType.Index;
				else if (ci.Contains("POSITION0"))
				{
					if (ci.EndsWith("Component 0"))
						m_colType[i] = ColType.Pos0;
					else if (ci.EndsWith("Component 1"))
						m_colType[i] = ColType.Pos1;
					else if (ci.EndsWith("Component 2"))
						m_colType[i] = ColType.Pos2;
				}
				else if (ci.Contains("NORMAL0"))
				{
					if (ci.EndsWith("Component 0"))
						m_colType[i] = ColType.Normal0;
					else if (ci.EndsWith("Component 1"))
						m_colType[i] = ColType.Normal1;
					else if (ci.EndsWith("Component 2"))
						m_colType[i] = ColType.Normal2;

					if (ci.Contains("R8G8B8A8"))
						m_isDecimalNormal = true;
				}
				else if (ci.Contains("TEXCOORD0"))
				{
					if (ci.EndsWith("Component 0"))
						m_colType[i] = ColType.Uv00;
					else if (ci.EndsWith("Component 1"))
						m_colType[i] = ColType.Uv01;
					else if (ci.EndsWith("Component 2"))
						m_colType[i] = ColType.Uv02;
					else if (ci.EndsWith("Component 3"))
						m_colType[i] = ColType.Uv03;
				}
				else if (ci.Contains("TEXCOORD1"))
				{
					if (ci.EndsWith("Component 0"))
						m_colType[i] = ColType.Uv10;
					else if (ci.EndsWith("Component 1"))
						m_colType[i] = ColType.Uv11;
					else if (ci.EndsWith("Component 2"))
						m_colType[i] = ColType.Uv12;
					else if (ci.EndsWith("Component 3"))
						m_colType[i] = ColType.Uv13;
				}
				//ignore tangent
			}

			m_typeCol = new int[(int)ColType.Count];
			for (int i = 0; i < m_colType.Length; ++i)
			{
				if (m_colType[i] >= 0)
				{
					m_typeCol[(int)m_colType[i]] = i;
				}
			}
		}

		static float[] ParseLineData(string[] cols)
		{
			var data = new float[cols.Length];
			for (int i = 0; i < cols.Length; ++i)
			{
				float.TryParse(cols[i], out data[i]);
			}

			return data;
		}

		private int[] m_typeCol;
		private ColType[] m_colType;
		private bool m_isDecimalNormal, m_isDecimalTangent;
	}

	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length <= 0)
			{
				Console.WriteLine("Useage: CsvToObj.exe <cvs_file_path>");
				return;
			}

			var converter = new CsvToObj();
			converter.Convert(args[0]);
		}

		
	}
}
