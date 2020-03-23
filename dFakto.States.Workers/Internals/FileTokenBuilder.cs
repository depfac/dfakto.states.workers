using System;
using System.Text.Encodings.Web;
using System.Web;

namespace dFakto.States.Workers.Internals
{
    public class FileToken
    {
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
            set => _builder.Path = value;
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
                throw new Exception("Unexpected FileTOken name");
            }
            
            var token = new FileToken(val.Scheme, val.Host);
            token.Path = val.AbsolutePath;
            return token;
        }
    }
}