﻿using System.Text.RegularExpressions;

namespace RegexDialog
{
    internal class RegexFileResult : RegexResult
    {
        public RegexFileResult(Regex regex, Capture regexElement, EnvDTE.EditPoint ep, int regexElementNb, string fileName) : base(regex, regexElement, ep, regexElementNb, fileName, 0)
        {}

        public override string Name
        {
            get
            {
                return $"File {RegexElementNb}: {Children.Count} matches found in \"{FileName}\"";
            }
        }

        public override bool IsExpanded { get { return true; } }

        public override void RefreshExpands()
        {
            Children.ForEach(delegate (RegexResult child)
            {
                child.RefreshExpands();
            });
        }

        public override string ElementType
        {
            get
            {
                return "File";
            }
        }
    }
}
