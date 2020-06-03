using System;
using System.Data;
using System.IO;
using System.Linq;

namespace dFakto.States.Workers.SqlBulkInsert
{
	internal class CsvDataReader : IDataReader
	{
		private readonly IFormatProvider _formatProvider;
		private readonly bool _ownReader;
		private readonly string[] _headers;

		private CsvStreamReader _csvStreamReader;
		private string[] _values;

		/// <summary>
		/// Create a New CsvDatReader based on an UTF-8 file
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="separator"></param>
		/// <param name="headerIncluded"></param>
		/// <param name="formatProvider"></param>
		public CsvDataReader(string fileName, char separator = ',', bool headerIncluded = true,
			IFormatProvider formatProvider = null)
			: this(new CsvStreamReader(fileName, separator, headerIncluded), true, formatProvider)
		{
		}

		public CsvDataReader(TextReader reader, char separator = ',', bool headerIncluded = true,
			IFormatProvider formatProvider = null)
			: this(new CsvStreamReader(reader, separator, headerIncluded), true, formatProvider)
		{
		}

		public CsvDataReader(CsvStreamReader reader, IFormatProvider formatProvider = null)
			: this(reader, false, formatProvider)
		{
		}

		private CsvDataReader(CsvStreamReader reader, bool ownReader, IFormatProvider formatProvider = null)
		{
			_formatProvider = formatProvider ?? System.Globalization.CultureInfo.CurrentCulture;
			_csvStreamReader = reader;
			_ownReader = ownReader;
			_headers = reader.Headers.ToArray();
		}

		protected virtual void Dispose(bool disposing)
		{
		    if (disposing && _csvStreamReader != null && _ownReader)
		    {
		        _csvStreamReader.Dispose();
		        _csvStreamReader = null;
		    }
		}

	    ~CsvDataReader()
	    {
	        Dispose(false);
	    }

	    public void Dispose()
	    {
	        Dispose(true);
	        GC.SuppressFinalize(this);
	    }

		public string GetName(int i)
		{
			return _headers[i];
		}

		public string GetDataTypeName(int i)
		{
			return "VARCHAR (5000)";
		}

		public Type GetFieldType(int i)
		{
			return typeof (string);
		}

		public object GetValue(int i)
		{
			return string.IsNullOrEmpty(_values[i]) ? DBNull.Value : (object) _values[i];
		}

		public int GetValues(object[] values)
		{
			int count = 0;
			for (int i = 0; i < values.Length; i++)
			{
				if (i >= FieldCount)
					break;

				values[i] = GetValue(i);
				count++;
			}
			return count;
		}

		public int GetOrdinal(string name)
		{
			//GetOrdinal performs a case-sensitive lookup first. If it fails, a second case-insensitive search is made.
			// GetOrdinal is kana-width insensitive. If the index of the named field is not found,
			// an IndexOutOfRangeException is thrown. 
			for (int i = 0; i < _headers.Length; i++)
			{
				if (_headers[i] == name)
					return i;
			}
			for (int i = 0; i < _headers.Length; i++)
			{
				if (string.Equals(_headers[i],name,StringComparison.CurrentCultureIgnoreCase))
					return i;
			}
			throw new ArgumentOutOfRangeException(nameof(name), $"No field named {name} found ");
		}

		public bool GetBoolean(int i)
		{
			return bool.Parse(_values[i]);
		}

		public byte GetByte(int i)
		{
			return byte.Parse(_values[i]);
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		public char GetChar(int i)
		{
			return char.Parse(_values[i]);
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		public Guid GetGuid(int i)
		{
			return Guid.Parse(_values[i]);
		}

		public short GetInt16(int i)
		{
			return short.Parse(_values[i], _formatProvider);
		}

		public int GetInt32(int i)
		{
			return int.Parse(_values[i], _formatProvider);
		}

		public long GetInt64(int i)
		{
			return long.Parse(_values[i], _formatProvider);
		}

		public float GetFloat(int i)
		{
			return float.Parse(_values[i], _formatProvider);
		}

		public double GetDouble(int i)
		{
			return double.Parse(_values[i], _formatProvider);
		}

		public string GetString(int i)
		{
			return _values[i];
		}

		public decimal GetDecimal(int i)
		{
			return decimal.Parse(_values[i], _formatProvider);
		}

		public DateTime GetDateTime(int i)
		{
			return DateTime.Parse(_values[i], _formatProvider);
		}

		public IDataReader GetData(int i)
		{
			throw new NotImplementedException();
		}

		public bool IsDBNull(int i)
		{
			return string.IsNullOrEmpty(GetString(i));
		}

		public int FieldCount
		{
			get
			{
				if (_headers.Length != 0)
				{
					return _headers.Length;
				}

				return _values?.Length ?? 0;
			}
		}

		public int Length => -1;

		public string this[int i] => GetString(i);

		public string this[string name]
		{
			get
			{
				for (int i = 0; i < _headers.Length; i++)
				{
					if (_headers[i].Equals(name, StringComparison.InvariantCultureIgnoreCase))
					{
						if (_values.Length > i)
						{
							return _values[i];
						}
						return null; // defined in the headers, but no value exists
					}
				}
				throw new InvalidOperationException("Name not found");
			}
		}

		object IDataRecord.this[int i] => GetValue(i);

		object IDataRecord.this[string name]
		{
			get
			{
				for (int j = 0; j < _headers.Length; j++)
				{
					if (_headers[j].Equals(name, StringComparison.InvariantCultureIgnoreCase))
					{
						if (_values.Length <= j)
						{
							return _values[j];
						}
						return null; // defined in the headers, but no value exists
					}
				}
				throw new InvalidOperationException("Name not found");
			}
		}

		public void Close()
		{
			IsClosed = true;
		}

		public DataTable GetSchemaTable()
		{
			throw new NotImplementedException();
		}

		public bool NextResult()
		{
			return false;
		}

		public bool Read()
		{
			if (IsClosed)
				throw new InvalidOperationException("DataReader closed");

			_values = _csvStreamReader.ReadNextLine().ToArray();
			return _values.Length > 0;
		}

		public int Depth { get; private set; }
		public bool IsClosed { get; private set; }
		public int RecordsAffected { get; private set; }
	}
}