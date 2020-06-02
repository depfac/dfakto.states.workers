namespace dFakto.States.Workers.Sql.PostgreSQL
{
    public class CopyQueryParameters
    {
            /// <summary>
            /// The name (optionally schema-qualified) of an existing table.
            /// </summary>
            public string TableName { get; set; }
            
            /// <summary>
            /// The absolute path name of the input or output file. Windows users might need to use an E'' string
            /// and double any backslashes used in the path name.
            /// </summary>
            public string FileName { get; set; }
            
            /// <summary>
            /// Selects the data format to be read or written: text, csv (Comma Separated Values), or binary. The default is text.
            /// </summary>
            public string Format { get; set; }
            
            /// <summary>
            /// Specifies copying the OID for each row. (An error is raised if OIDS is specified for a table
            /// that does not have OIDs, or in the case of copying a query.)
            /// </summary>
            public bool? Oids { get; set; }
            
            /// <summary>
            /// Specifies the character that separates columns within each row (line) of the file. The default is a tab
            /// character in text format, a comma in CSV format. This must be a single one-byte character.
            /// This option is not allowed when using binary format.
            /// </summary>
            public string Delimiter { get; set; }
            
            /// <summary>
            /// Specifies the string that represents a null value. The default is \N (backslash-N) in text format,
            /// and an unquoted empty string in CSV format. You might prefer an empty string even in text format for
            /// cases where you don't want to distinguish nulls from empty strings. This option is not allowed when
            /// using binary format.
            /// </summary>
            public string Null { get; set; }
            
            /// <summary>
            /// Specifies that the file contains a header line with the names of each column in the file. On output,
            /// the first line contains the column names from the table, and on input, the first line is ignored.
            /// This option is allowed only when using CSV format.
            /// </summary>
            public bool? Header { get; set; }
            
            /// <summary>
            /// Specifies the quoting character to be used when a data value is quoted. The default is double-quote.
            /// This must be a single one-byte character. This option is allowed only when using CSV format.
            /// </summary>
            public char? Quote { get; set; }
            
            /// <summary>
            /// Specifies the character that should appear before a data character that matches the QUOTE value.
            /// The default is the same as the QUOTE value (so that the quoting character is doubled if it appears
            /// in the data). This must be a single one-byte character. This option is allowed only when using CSV format.
            /// </summary>
            public char? Escape { get; set; }
            
            /// <summary>
            /// Forces quoting to be used for all non-NULL values in each specified column. NULL output is never quoted.
            /// If * is specified, non-NULL values will be quoted in all columns. This option is allowed only in COPY TO,
            /// and only when using CSV format.
            /// </summary>
            public string[] ForceQuote{ get; set; }
            
            /// <summary>
            /// Do not match the specified columns' values against the null string. In the default case where the null
            /// string is empty, this means that empty values will be read as zero-length strings rather than nulls,
            /// even when they are not quoted. This option is allowed only in COPY FROM, and only when using CSV format.
            /// </summary>
            public string[] ForceNotNull{ get; set; }
            
            /// <summary>
            /// Specifies that the file is encoded in the encoding_name. If this option is omitted, the current client
            /// encoding is used. 
            /// </summary>
            public string Encoding { get; set; }

    }
}