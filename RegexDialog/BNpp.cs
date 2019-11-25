using EnvDTE;
using System;
using System.Text;

namespace RegexDialog
{
    public class BNpp
    {
        private _DTE _dte;
        private Document _activeDocument = null;
        private TextDocument _textDocument = null;

        // public static NotepadPPGateway NotepadPP { get; private set; } = new NotepadPPGateway();
        public _DTE DTE
        {
            get { return _dte; }
            set
            {
                _dte = value;
            }
        }

        public Document ActiveDocument
        {
            get
            {
                /*
                if (_activeDocument == null)
                {
                    if (_dte != null)
                    {
                        _activeDocument = _dte.ActiveDocument;
                    }
                }
                */
                if (_dte != null)
                {
                    _activeDocument = _dte.ActiveDocument;
                } else {
                    _activeDocument = null;
                }
                return _activeDocument;
            }
        }

        public TextDocument ActiveTextDocument
        {
            get
            {
                /*
                if (_textDocument == null)
                {
                    if (ActiveDocument != null)
                    {
                        _textDocument = ActiveDocument.Object() as TextDocument;
                    }
                }
                */
                if (ActiveDocument != null)
                {
                    _textDocument = ActiveDocument.Object() as TextDocument;
                } else
                {
                    _textDocument = null;
                }
                return _textDocument;
            }
        }



        /// <summary>
        /// Récupère les caractères de fin de lignes courant
        /// !!! Attention pour le moment bug. !!! Enlève la coloration syntaxique du fichier courant
        /// </summary>
        public string CurrentEOL
        {
            get
            {
                return Environment.NewLine;
            }
        }

        /// <summary>
        /// Récupère ou attribue le texte complet du tab Notepad++ courant
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        public string Text
        {
            get
            {
                /*
                IScintillaGateway scintilla = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                // Multiply by 2 to managed 2 bytes encoded chars
                return BEncoding.GetUtf8TextFromScintillaText(scintilla.GetText(scintilla.GetTextLength() * 2));
                */
                if (ActiveTextDocument == null)
                {
                    return String.Empty;
                }
                var text = ActiveTextDocument.CreateEditPoint(ActiveTextDocument.StartPoint).GetText(ActiveTextDocument.EndPoint);
                // TOCHECK!!!! Could avoid this???
                return text.Replace(Environment.NewLine, "\n");
                // return text;
            }

            set
            {
                if (ActiveTextDocument == null)
                {
                    return;
                }
                ActiveTextDocument.Selection.SelectAll();
                ActiveTextDocument.Selection.Insert(value, (int)vsInsertFlags.vsInsertFlagsContainNewText);
                // ActiveTextDocument.CreateEditPoint(ActiveTextDocument.StartPoint).(ActiveTextDocument.EndPoint);
                /*
                IScintillaGateway scintilla = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                string text = BEncoding.GetScintillaTextFromUtf8Text(value, out int length);
                scintilla.SetText(text);
                */
            }
        }

        /// <summary>
        /// Récupère ou attribue le début de la sélection de texte
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        public int SelectionStart
        {
            get
            {
                // return new ScintillaGateway(PluginBase.GetCurrentScintilla()).GetSelectionStart();
                if (ActiveTextDocument == null)
                {
                    return -1;
                }
                if (ActiveTextDocument.Selection.IsEmpty)
                {
                    return 1;
                }
                // just first TextRange will be consider
                TextRange rng = ActiveTextDocument.Selection.TextRanges.Item(1);
                if (rng == null)
                {
                    return 1;
                }
                return rng.StartPoint.AbsoluteCharOffset;
            }

            set
            {
                if (ActiveTextDocument == null)
                {
                    return;
                }
                // TODO
                // new ScintillaGateway(PluginBase.GetCurrentScintilla()).SetSelectionStart(new Position(value));
                // ActiveTextDocument.Selection.Mo
                // TextRange rng = ActiveTextDocument.Selection.TextRanges.Item(0);                
            }
        }

        /// <summary>
        /// Récupère ou attribue la fin de la sélection de texte
        /// <br/>si aucun texte n'est sélectionné SelectionEnd = SelectionStart
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        public int SelectionEnd
        {
            get
            {
                /*
                int curPos = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETSELECTIONEND, 0, 0);
                IScintillaGateway scintilla = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                string beginingText = scintilla.GetText(curPos);
                string text = BEncoding.GetScintillaTextFromUtf8Text(beginingText, out int length);
                return length;
                */
                if (ActiveTextDocument == null)
                {
                    return -1;
                }
                if (ActiveTextDocument.Selection.IsEmpty)
                {
                    return 1;
                }
                // just first TextRange will be consider
                TextRange rng = ActiveTextDocument.Selection.TextRanges.Item(1);
                if (rng == null)
                {
                    return 1;
                }
                return rng.EndPoint.AbsoluteCharOffset;
            }

            set
            {
                if (ActiveTextDocument == null)
                {
                    return;
                }
                // TODO

                /*
                string allText = Text;
                int endToUse = value;

                if (value < 0)
                {
                    endToUse = 0;
                }
                else if (value > allText.Length)
                {
                    endToUse = allText.Length;
                }

                string afterText = allText.Substring(0, endToUse);
                string afterTextInDefaultEncoding = BEncoding.GetScintillaTextFromUtf8Text(afterText, out int defaultEnd);

                Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETSELECTIONEND, defaultEnd, 0);
                */
            }
        }

        /// <summary>
        /// Récupère ou attribue la longueur de la sélection de texte
        /// <br/>Si aucun texte n'est sélectionné SelectionEnd = 0
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        public int SelectionLength
        {
            get
            {
                return SelectionEnd - SelectionStart;
            }

            set
            {
                SelectionEnd = SelectionStart + (value < 0 ? 0 : value);
            }
        }

        /// <summary>
        /// Récupère ou remplace le texte actuellement sélectionné
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        public string SelectedText
        {
            get
            {
                if (ActiveTextDocument == null)
                {
                    return String.Empty;
                }
                return ActiveTextDocument.Selection.Text;
                /*
                IScintillaGateway scintillaGateway = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                int start = scintillaGateway.GetSelectionStart().Value;
                int end = scintillaGateway.GetSelectionEnd().Value;

                return end - start == 0 ? "" : Text.Substring(start, end - start);
                */
            }

            set
            {
                if (ActiveTextDocument == null)
                {
                    return;
                }
                ActiveTextDocument.Selection.Text = value;
                /*
                string defaultNewText = BEncoding.GetScintillaTextFromUtf8Text(value);
                Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_REPLACESEL, 0, defaultNewText);
                */
            }
        }

        private static int CountLines(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (str == string.Empty)
                return 0;
            int index = -1;
            int count = 0;
            while (-1 != (index = str.IndexOf(Environment.NewLine, index + 1)))
                count++;

            return count + 1;
        }

        // HACK
        private static int CorrectOffset(string str)
        {
            string completeStr = str;
            if (str.EndsWith("\r") && Environment.NewLine == "\r\n")
            {
                completeStr = str + '\n';
            }

            int index = -1;
            int count = 0;
            while (-1 != (index = completeStr.IndexOf(Environment.NewLine, index + 1)))
            {
                count++;
            }
            return completeStr.Length - count;
        }

        /// <summary>
        /// Sélectionne dans le tab Notepad++ courant le texte entre start et end
        /// et positionne le scroll pour voir la sélection.
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        /// <param name="start">Position du début du texte à sélectionner dans le texte entier<br/> Si plus petit que 0 -> forcé à zéro<br/> Si plus grand que Text.Length -> forcé à Text.Length</param>
        /// <param name="end">Position de fin du texte à sélectionner dans le texte entier<br/> Si plus petit que 0 -> forcé à zéro<br/> Si plus grand que Text.Length -> forcé à Text.Length<br/> Si plus petit que start -> forcé à start</param>
        public void SelectTextAndShow(int start, int end)
        {
            string allText = Text;
            int startToUse = start;
            int endToUse = end;

            if (startToUse < 0)
            {
                startToUse = 0;
            }
            else if (startToUse > allText.Length)
            {
                startToUse = allText.Length;
            }

            if (endToUse < 0)
            {
                endToUse = 0;
            }
            else if (endToUse > allText.Length)
            {
                endToUse = allText.Length;
            }
            else if (endToUse < startToUse)
            {
                endToUse = startToUse;
            }

            int defaultStart, defaultEnd;
            // string beforeText = allText.Substring(0, startToUse);
            // defaultStart = CorrectOffset(beforeText) + 1; 
            defaultStart = startToUse + 1;
            // string endText = allText.Substring(0, endToUse);
            // defaultEnd = CorrectOffset(endText);
            defaultEnd = endToUse + 1;

            /*
            int defaultStart, defaultEnd;
            string beforeText = allText.Substring(0, startToUse);
            defaultStart = CountLines(beforeText) - 1;
            // string beforeTextInDefaultEncoding = BEncoding.GetScintillaTextFromUtf8Text(beforeText, out defaultStart);
            string endText = allText.Substring(0, endToUse);
            defaultEnd = CountLines(endText) - 1;
            */
            /*
            var result = Encoding.Default.GetString(Encoding.UTF8.GetBytes(endText));
            Encoding ANSI = Encoding.GetEncoding(1252);
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(endText);
            byte[] ansiBytes = Encoding.Convert(Encoding.UTF8, ANSI, utf8Bytes);

            var result2 = ANSI.GetString(ansiBytes);
            */

            // string endTextInDefaultEncoding = BEncoding.GetScintillaTextFromUtf8Text(endText, out defaultEnd);            
            TextSelection selection = ActiveTextDocument.Selection as TextSelection;
            if (selection != null)
            {
                // ActiveDocument.Activate();
                var pt = selection.ActivePoint;
                selection.MoveToAbsoluteOffset(defaultStart, false);
                selection.MoveToAbsoluteOffset(defaultEnd, true);
            }

            // TODO
            // Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GOTOPOS, defaultStart, 0);
            // Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETSELECTIONEND, defaultEnd, 0);
        }

        /// <summary>
        /// Si la sélection multiple est activée ajoute la sélection spécifié
        /// <br/>(Gère la conversion d'encodage Npp/C#)
        /// </summary>
        /// <param name="start">Position du début du texte à sélectionner dans le texte entier<br/> Si plus petit que 0 -> forcé à zéro<br/> Si plus grand que Text.Length -> forcé à Text.Length</param>
        /// <param name="end">Position de fin du texte à sélectionner dans le texte entier<br/> Si plus petit que 0 -> forcé à zéro<br/> Si plus grand que Text.Length -> forcé à Text.Length<br/> Si plus petit que start -> forcé à start</param>

        public void AddSelection(int start, int end)
        {
            string allText = Text;
            int startToUse = start;
            int endToUse = end;

            if (start < 0)
            {
                startToUse = 0;
            }
            else if (start > allText.Length)
            {
                startToUse = allText.Length;
            }

            if (end < 0)
            {
                endToUse = 0;
            }
            else if (end > allText.Length)
            {
                endToUse = allText.Length;
            }
            else if (endToUse < startToUse)
            {
                endToUse = startToUse;
            }

            int defaultStart, defaultEnd;
            string beforeText = allText.Substring(0, startToUse);
            defaultStart = beforeText.Length;
            // string beforeTextInDefaultEncoding = BEncoding.GetScintillaTextFromUtf8Text(beforeText, out defaultStart);
            string endText = allText.Substring(0, endToUse);
            defaultEnd = endText.Length;
            // string endTextInDefaultEncoding = BEncoding.GetScintillaTextFromUtf8Text(endText, out defaultEnd);

            // TODO
            // Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_ADDSELECTION, defaultStart, defaultEnd);
        }


        /// <summary>
        /// Récupère le texte de la ligne spécifiée
        /// </summary>
        /// <param name="lineNb">Numéro de la ligne dont on veut récupérer le texte</param>
        /// <returns>Le texte de la ligne spécifiée</returns>
        public string GetLineText(int lineNb)
        {
            string result = "";

            try
            {
                result = Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[lineNb - 1];
            }
            catch { }

            return result;
        }
    }

    /// <summary>
    /// Offre des fonctions simples pour convertir l'encodage d'un texte
    /// entre l'encodage du document courant dans Notepad++ et l'encodage en C# (UTF8)
    /// </summary>
    /*
    internal static class BEncoding
    {
        private static Encoding utf8 = Encoding.UTF8;

        /// <summary>
        /// Convertit le texte spécifier de l'encodage du document Notepad++ courant à l'encodage C# (UTF8)
        /// </summary>
        public static string GetUtf8TextFromScintillaText(string scText)
        {
            string result = "";
            int iEncoding = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, SciMsg.SCI_GETCODEPAGE, 0, 0);

            switch (iEncoding)
            {
                case 65001: // UTF8
                    result = utf8.GetString(Encoding.Default.GetBytes(scText));
                    break;
                default:
                    Encoding ANSI = Encoding.GetEncoding(1252);

                    byte[] ansiBytes = ANSI.GetBytes(scText);
                    byte[] utf8Bytes = Encoding.Convert(ANSI, Encoding.UTF8, ansiBytes);

                    result = Encoding.UTF8.GetString(utf8Bytes);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Convertit le texte spécifier de l'encodage C# (UTF8) à l'encodage document Notepad++ courant
        /// </summary>
        public static string GetScintillaTextFromUtf8Text(string utf8Text)
        {
            string result = "";
            int iEncoding = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, SciMsg.SCI_GETCODEPAGE, 0, 0);

            switch (iEncoding)
            {
                case 65001: // UTF8
                    result = Encoding.Default.GetString(utf8.GetBytes(utf8Text));
                    break;
                default:
                    Encoding ANSI = Encoding.GetEncoding(1252);

                    byte[] utf8Bytes = utf8.GetBytes(utf8Text);
                    byte[] ansiBytes = Encoding.Convert(Encoding.UTF8, ANSI, utf8Bytes);

                    result = ANSI.GetString(ansiBytes);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Convertit le texte spécifier de l'encodage C# (UTF8) à l'encodage document Notepad++ courant
        /// </summary>
        public static string GetScintillaTextFromUtf8Text(string utf8Text, out int length)
        {
            string result = "";
            int iEncoding = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, SciMsg.SCI_GETCODEPAGE, 0, 0);

            byte[] utf8Bytes = utf8.GetBytes(utf8Text);
            length = utf8Bytes.Length;

            switch (iEncoding)
            {
                case 65001: // UTF8
                    result = Encoding.Default.GetString(utf8.GetBytes(utf8Text));
                    break;
                default:
                    Encoding ANSI = Encoding.GetEncoding(1252);
                    byte[] ansiBytes = Encoding.Convert(Encoding.UTF8, ANSI, utf8Bytes);
                    result = ANSI.GetString(ansiBytes);
                    break;
            }

            return result;
        }       
    }
    */
}
