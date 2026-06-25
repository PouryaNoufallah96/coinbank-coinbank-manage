using PdfSharp.Fonts;
using System.Reflection;

namespace CoinBank.Services._Report.Exports
{
    public class ReportPdfFontResolver : IFontResolver
    {
        private const string FontFaceName = "Roboto#Regular";
        private const string FontResourceName = "CoinBank.Services._Report.Exports.Fonts.Roboto-Regular.ttf";
        private static readonly Lazy<byte[]> _regularFont = new(LoadRegularFont);

        public byte[] GetFont(string faceName)
        {
            return faceName == FontFaceName ? _regularFont.Value : null;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(FontFaceName);
        }

        private static byte[] LoadRegularFont()
        {
            var assembly = typeof(ReportPdfFontResolver).GetTypeInfo().Assembly;
            using var stream = assembly.GetManifestResourceStream(FontResourceName)
                ?? throw new InvalidOperationException($"Embedded report font not found: {FontResourceName}");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
