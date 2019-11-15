using System.Text.RegularExpressions;

namespace RegexDialog
{
    internal class RegexCaptureResult : RegexResult
    {
        public RegexCaptureResult(Regex regex, Capture capture, EnvDTE.EditPoint ep, int captureNb, string fileName = "", int selectionIndex = 0) : base(regex, capture, ep, captureNb, fileName, selectionIndex)
        {}

        public override string ElementType
        {
            get
            {
                return "Capture";
            }
        }
    }
}
