using System;
using System.Collections.Generic;
using System.Linq;

namespace dFakto.States.Workers.Abstractions
{
    public class FileToken
    {
        public static readonly char[] DirectorySeparator = new[] { '/' , '\\' };

        public const string Year = "#YEAR#";
        public const string Month = "#MONTH#";
        public const string Day = "#DAY#";
        public const string Hour = "#HOUR#";
        public const string Minute = "#MINUTE#";
        public const string Second = "#SECOND#";
        public const string Timestamp = "#TIMESTAMP#";
        public const string Environment = "#ENVIRONMENT#";

        private static readonly IDictionary<string, Func<string>> PathTransformations =
            new Dictionary<string, Func<string>>
            {
                { Year, () => DateTime.Today.Year.ToString()},
                { Month, () => DateTime.Today.Month.ToString()},
                { Day, () => DateTime.Today.Day.ToString()},
                { Hour, () => DateTime.Now.Hour.ToString()},
                { Minute, () => DateTime.Now.Minute.ToString()},
                { Minute, () => DateTime.Now.Second.ToString()},
                { Timestamp, () => DateTime.Now.ToString("O")},
                { Environment, () => System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}
            };

        private readonly UriBuilder _builder;

        public FileToken(string stringToken)
        {
            _builder = new UriBuilder(new Uri(stringToken));
        }
        
        public FileToken(string type, string name)
        {
            _builder = new UriBuilder();
            _builder.Scheme = type;
            _builder.Host = name;
        }
        
        public string Type
        {
            get => _builder.Scheme;
            set => _builder.Scheme =value;
        }

        public string Name
        {
            get => _builder.Host;
            set => _builder.Host =value;
        }

        public string Path
        {
            get => _builder.Path.Substring(1);
            private set => _builder.Path = value;
        }

        public void SetPath(string path, Func<string, string> transformation = null)
        {
            string newPath = System.IO.Path.Combine(
                path.Split(DirectorySeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => PathTransformations.ContainsKey(x) ? PathTransformations[x]() : x)
                .ToArray());
            if(transformation != null)
            {
                newPath = transformation(newPath);
            }
            _builder.Path = newPath;
        }

        public override string ToString()
        {
            return _builder.Uri.ToString();
        }

        public static string ParseName(string fileToken)
        {
            if (!Uri.TryCreate(fileToken, UriKind.Absolute, out var val))
            {
                throw new Exception("Invalid file token");
            }

            return val.Host;
        }
        
        public static FileToken Parse(string fileToken, string expectedName)
        {
            if (!Uri.TryCreate(fileToken, UriKind.Absolute, out var val))
            {
                throw new Exception("Invalid file token");
            }

            if (!string.Equals(val.Host, expectedName, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("Unexpected FileToken name");
            }
            
            var token = new FileToken(val.Scheme, val.Host);
            token.Path = val.AbsolutePath;
            return token;
        }
    }
}