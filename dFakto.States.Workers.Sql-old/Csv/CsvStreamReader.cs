using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace dFakto.States.Workers.Sql.Csv
{
	internal class CsvStreamReader : IDisposable
	{
		private int _position;
		private int _length;
		private readonly char _separator;
		private TextReader _textReader;
		private readonly bool _ownReader;
		private readonly char[] _buffer = new char[4086];
		private bool _isEndOfLine = false;
		private bool _lastCharIsSeparator = false;
		private bool _noDataOnTheLine = true;
		private const char EndOfStream = '\0';
		private string[] _firstLine;

		public CsvStreamReader(string aFileName, char aSeparator = ',', bool containsHeaders = false)
			: this(File.OpenText(aFileName), true, aSeparator, containsHeaders)
		{
		}

		public CsvStreamReader(TextReader aReader, char aSeparator = ',', bool containsHeaders = false)
			: this(aReader, false, aSeparator, containsHeaders)
		{
		}

		internal CsvStreamReader(TextReader aReader, bool ownReader, char aSeparator = ',', bool containsHeaders = false)
		{
			_textReader = aReader;
			_ownReader = ownReader;
			_separator = aSeparator;
			Headers = ReadNextLine().ToArray();
			_firstLine = containsHeaders ? null : Headers.ToArray();
			CurrentLineNumber = 0;
		}

		public string ReadNextValue()
		{
			if (_isEndOfLine)
			{
				// previous item was last in line, start new line
				_isEndOfLine = false;
				_noDataOnTheLine = true;
				_lastCharIsSeparator = false;
				return null;
			}

			bool quoted = false;
			bool predata = true;
			bool postdata = false;
			bool hasvalue = false;
			StringBuilder item = new StringBuilder();

			while (true)
			{
				char c = GetNextChar(true);

				if (c == EndOfStream)
				{
					if (_lastCharIsSeparator)
					{
						_lastCharIsSeparator = false;
						return item.ToString();
					}
					return hasvalue ? item.ToString() : null;
				}

				if ((postdata || !quoted) && c == _separator)
				{
					_lastCharIsSeparator = true;
					_noDataOnTheLine = false;
					// end of item, return
					return item.ToString();
				}

				if ((predata || postdata || !quoted) && (c == '\n' || c == '\r'))
				{
					// we are at the end of the line, eat newline characters and exit
					if (c == '\r' && GetNextChar(false) == '\n')
						// new line sequence is \r\n
						GetNextChar(true);

					if (_noDataOnTheLine)
						return null;

					_isEndOfLine = true;
					return item.ToString();
				}

				// TODO: weird to trim if post or pre, should trim between the separator and the first or last charcater found 
				// (sample @"""\"";""test 2ème champs""" should trim nothing)
				if ((predata || postdata) && c == ' ')
					// whitespace preceeding data, discard
					continue;

				if (predata && c == '"')
				{
					// quoted data is starting
					quoted = true;
					predata = false;
					hasvalue = true;
					continue;
				}

				if (predata)
				{
					// data is starting without quotes
					predata = false;
					hasvalue = true;
					_noDataOnTheLine = false;
					item.Append(c);
					continue;
				}

				if (c == '"' && quoted)
				{
					if (GetNextChar(false) == '"')
						// double quotes within quoted string means add a quote
						item.Append(GetNextChar(true));
					else
					// end-quote reached
						postdata = true;
					continue;
				}

				if (c == '\\' && quoted)
				{
					item.Append(GetNextChar(true));
					continue;
				}

				// all cases covered, character must be data
				_lastCharIsSeparator = false;
				_noDataOnTheLine = false;
				item.Append(c);
			}
		}

		public IEnumerable<string> ReadNextLine()
		{
			//If there is no header, the first line has already been read
			if (_firstLine != null)
			{
				_firstLine = null;
				foreach (var str in Headers)
					yield return str;
			}
			else
			{
				string value = ReadNextValue();
				while (value != null)
				{
					yield return value;
					value = ReadNextValue();
				}
			}
			CurrentLineNumber++;
		}

		private char GetNextChar(bool aMustEat)
		{
			if (_position >= _length)
			{
				_length = _textReader.ReadBlock(_buffer, 0, _buffer.Length);
				if (_length == 0)
				{
					return EndOfStream;
				}
				_position = 0;
			}
			return aMustEat ? _buffer[_position++] : _buffer[_position];
		}

		public int CurrentLineNumber { get; private set; }
		public IEnumerable<string> Headers { get; }

	    protected virtual void Dispose(bool disposing)
	    {
	        if (disposing && _textReader != null && _ownReader)
	        {
	            _textReader.Dispose();
	            _textReader = null;
	        }
	    }

	    ~CsvStreamReader()
	    {
	        Dispose(false);
	    }

	    public void Dispose()
	    {
	        Dispose(true);
	        GC.SuppressFinalize(this);
	    }
	}
}