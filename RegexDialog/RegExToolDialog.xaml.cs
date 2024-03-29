﻿using CSScriptLibrary;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Microsoft.VisualStudio.Shell;
using System.Reflection;

namespace RegexDialog
{
    /// <summary>
    /// Logique d'interaction pour RegExToolDialog.xaml
    /// </summary>
    public partial class RegExToolDialog : UserControl
    {
        private List<RegExOptionViewModel> regExOptionViewModelsList = new List<RegExOptionViewModel>();
        List<Regex> bracketsRegexList = (new Regex[]
            {
                new Regex(@"(?<!(?<![\\])([\\]{2})*[\\])[\(\)]", RegexOptions.Compiled),
                new Regex(@"(?<!(?<![\\])([\\]{2})*[\\])[\[\]]", RegexOptions.Compiled),
                new Regex(@"(?<!(?<![\\])([\\]{2})*[\\])[{}]", RegexOptions.Compiled),
                new Regex(@"(?<!(?<![\\])([\\]{2})*[\\])[<>]", RegexOptions.Compiled)
            }).ToList();

        private ObservableCollection<string> regexHistory = new ObservableCollection<string>();
        private ObservableCollection<string> replaceHistory = new ObservableCollection<string>();

        private string[] openingBrackets = new string[] { "(", "[", "{", "<" };

        private string lastMatchesText = "";
        private int lastSelectionStart = 0;
        private int lastSelectionLength = 0;

        private BNpp _bnpp = null;
        private RExprItem _selectedExprItem = null;

        private bool mustSelectEditor = false;

        private BracketColorizer currentBracketColorizer = new BracketColorizer();
        private BracketColorizer matchingBracketColorizer = new BracketColorizer();

        public delegate string GetTextDelegate();
        public delegate string GetCurrentFileNameDelegate();
        public delegate void SetTextDelegate(string text);
        public delegate bool TryOpenDelegate(string fileName, bool onlyIfAlreadyOpen);
        public delegate void SetPositionDelegate(int index, int length);
        public delegate int GetIntDelegate();

        //        


        /// <summary>
        /// Fonction de récupération du texte à utiliser comme input pour l'expression régulière
        /// public delegate string GetTextDelegate()
        /// </summary>
        public GetTextDelegate GetText { get; set; }

        /// <summary>
        /// Fonction envoyant le résultat du replace dans une chaine texte
        /// public delegate void SetTextDelegate(string text)
        /// </summary>
        public SetTextDelegate SetText { get; set; }

        /// <summary>
        /// Fonction de récupération du texte sélectionné à utiliser comme input pour l'expression régulière
        /// public delegate string GetTextDelegate()
        /// </summary>
        public GetTextDelegate GetSelectedText { get; set; }

        /// <summary>
        /// Fonction envoyant le résultat du replace dans une chaine texte lorsque à remplacer dans la sélection
        /// public delegate void SetTextDelegate(string text)
        /// </summary>
        public SetTextDelegate SetSelectedText { get; set; }

        /// <summary>
        /// Fonction envoyant le résultat de l'extraction des matches
        /// public delegate void SetTextDelegate(string text)
        /// </summary>
        public SetTextDelegate SetTextInNew { get; set; }

        /// <summary>
        /// Try to Open or show in front in the editor the specified fileName
        /// </summary>
        public TryOpenDelegate TryOpen { get; set; }

        /// <summary>
        /// Save the document in the current tab
        /// </summary>
        public Action SaveCurrentDocument { get; set; }

        /// <summary>
        /// Get the name of the current fileName in the editor
        /// </summary>
        public GetCurrentFileNameDelegate GetCurrentFileName { get; set; }

        /// <summary>
        /// Fonction permettant de faire une sélection dans le text source
        /// public delegate void SetPositionDelegate(int index, int length)
        /// </summary>
        public SetPositionDelegate SetPosition { get; set; } = (x, y) => { };

        /// <summary>
        /// Fonction permettant d'ajouter une sélection de texte (La multi sélection doit être active sur le composant final)
        /// </summary>
        public SetPositionDelegate SetSelection { get; set; }

        /// <summary>
        /// Fonction qui récupère la position du début de la sélection dans le texte
        /// </summary>
        public GetIntDelegate GetSelectionStartIndex { get; set; }

        /// <summary>
        /// Fonction qui récupère la longueur de la sélection
        /// </summary>
        public GetIntDelegate GetSelectionLength { get; set; }

        /// <summary>
        /// L'expression régulière éditée dans la boite de dialogue
        /// </summary>
        public string RegexPatternText
        {
            get
            {
                return RegexEditor.Text;
            }

            set
            {
                RegexEditor.Text = value;
            }
        }

        /// <summary>
        /// Le text de remplacement à utiliser pour le replace de'expression régulière
        /// </summary>
        public string ReplacePatternText
        {
            get
            {
                return ReplaceEditor.Text;
            }

            set
            {
                ReplaceEditor.Text = value;
            }
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        public RegExToolDialog()
        {
            InitializeComponent();
            _bnpp = new BNpp();
            Init();
        }

        /// <summary>
        /// Initialisation des propriétés des éléments GUI
        /// </summary>
        private void Init()
        {
            Config.Instance.ExpressionLibrary.Items.Add(new RExprItem() { Name = "E1", MatchText = "UNO", ReplaceText = "DUe" });
            Config.Instance.ExpressionLibrary.Items.Add(new RExprItem() { Name = "E2", MatchText = "TRE", ReplaceText = "44" });
            // Initialisation des delegates de base
            _bnpp.DTE = CSharpRegexTool.RegexToolWindowCommand.Instance.InstanceDTE;

            GetText = () => _bnpp.Text;
            SetText = (string text) =>
            {
                /*
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    BNpp.NotepadPP.FileNew();
                }
                */
                _bnpp.Text = text;
            };

            SetTextInNew = (string text) =>
            {
                // BNpp.NotepadPP.FileNew();
                _bnpp.Text = text;
            };

            GetSelectedText = () => _bnpp.SelectedText;
            SetPosition = (int index, int length) => _bnpp.SelectTextAndShow(index, index + length);

            SetSelection = (int index, int length) => _bnpp.AddSelection(index, index + length);
            GetSelectionStartIndex = () => _bnpp.SelectionStart;
            GetSelectionLength = () => _bnpp.SelectionLength;

            SaveCurrentDocument = () => { };
            // BNpp.NotepadPP.SaveCurrentFile(),

            /*
                        TryOpen = (string fileName, bool onlyIfAlreadyOpen) =>
                        {
                            try
                            {
                                bool result = false;

                                if (BNpp.NotepadPP.CurrentFileName.ToLower().Equals(fileName.ToLower()))
                                    result = true;
                                else if (BNpp.NotepadPP.GetAllOpenedDocuments.Any((string s) => s.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    BNpp.NotepadPP.ShowOpenedDocument(fileName);
                                    result = true;
                                }
                                else if (!onlyIfAlreadyOpen)
                                {
                                    result = BNpp.NotepadPP.OpenFile(fileName);
                                }
                                else
                                {
                                    result = false;
                                }

                                hWnd = FindWindow(null, "C# Regex Tool");
                                if (hWnd.ToInt64() > 0)
                                {
                                    SetForegroundWindow(hWnd);
                                }

                                return result;
                            }
                            catch
                            {
                                return false;
                            }

                        },
                        */
            try
            {
                XmlReader reader = XmlReader.Create(new StringReader(CSharpRegexTool.SyntaxRes.Regex_syntax_color));
                RegexEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            catch (Exception ex)
            {
                throw;
            }

            try
            {
                // Application de la coloration syntaxique pour les chaines de remplacement
                XmlReader reader2 = XmlReader.Create(new StringReader(CSharpRegexTool.SyntaxRes.Replace_syntax_color));
                ReplaceEditor.SyntaxHighlighting = HighlightingLoader.Load(reader2, HighlightingManager.Instance);
            }
            catch (Exception ex2)
            {
                throw;
            }

            // Abonnement au changement de position du curseur de texte pour la coloration des parentèses
            RegexEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

            // Construit la liste des options pour les expressions régulières
            BuildRegexOptionsCheckBoxs();

            // Construit l'arbre des éléments de languages d'expression régulière.
            BuildRegexLanguageElements();

            // Construit l'arbre des éléments de languages de replace.
            BuildReplaceLanguageElements();

            // Rétablit le contenu des éditeur
            RegexEditor.Text = Config.Instance.RegexEditorText;
            ReplaceEditor.Text = Config.Instance.ReplaceEditorText;

            // Left = Config.Instance.DialogLeft ?? Left;
            // Top = Config.Instance.DialogTop ?? Top;
            Width = Config.Instance.DialogWidth ?? Width;
            Height = Config.Instance.DialogHeight ?? Height;

            // WindowState = Config.Instance.DialogMaximized ? WindowState.Maximized : WindowState.Normal;

            FirstColumn.Width = Config.Instance.GridFirstColumnWidth;
            SecondColumn.Width = Config.Instance.GridSecondColumnWidth;
            ThirdColumn.Width = Config.Instance.GridThirdColumnWidth;
            RegexEditorRow.Height = new GridLength(Config.Instance.GridRegexEditorRowHeight ?? RegexEditorRow.Height.Value);
            RegexLanguageElementFirstRow.Height = new GridLength(Config.Instance.GridRegexLanguageElementsFirstRowHeight ?? RegexLanguageElementFirstRow.Height.Value);

            // Set Treeview Matches Result base contextMenu
            MatchResultsTreeView.ContextMenu = MatchResultsTreeView.Resources["cmMatchResultsMenu"] as ContextMenu;
        }

        private void BuildRegexOptionsCheckBoxs()
        {
            Enum.GetValues(typeof(RegexOptions))
                .Cast<RegexOptions>()
                .ToList()
                .ForEach(delegate (RegexOptions regexOption)
                {
                    if (regexOption != RegexOptions.None && regexOption != RegexOptions.Compiled)
                    {
                        RegExOptionViewModel reovm = new RegExOptionViewModel
                        {
                            RegexOptions = regexOption
                        };

                        regExOptionViewModelsList.Add(reovm);
                    }
                });

            icRegexOptions.ItemsSource = regExOptionViewModelsList;
            miRegexOptions.ItemsSource = regExOptionViewModelsList;
        }

        private void BuildRegexLanguageElements()
        {
            RegexLanguageElements root = JsonConvert.DeserializeObject<RegexLanguageElements>(CSharpRegexTool.SyntaxRes.RegexLanguageElements);
            RegexLanguagesElementsTreeView.ItemsSource = root.Data;
        }

        private void BuildReplaceLanguageElements()
        {
            ReplaceLanguageElements root = JsonConvert.DeserializeObject<ReplaceLanguageElements>(CSharpRegexTool.SyntaxRes.ReplaceLanguageElements);
            ReplaceLanguageElementsListView.ItemsSource = root.Data;
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            try
            {
                if (RegexEditor.TextArea.TextView.LineTransformers.Contains(currentBracketColorizer))
                    RegexEditor.TextArea.TextView.LineTransformers.Remove(currentBracketColorizer);
                if (RegexEditor.TextArea.TextView.LineTransformers.Contains(matchingBracketColorizer))
                    RegexEditor.TextArea.TextView.LineTransformers.Remove(matchingBracketColorizer);

                Dictionary<string, int> posStringToMatchingPosDict = new Dictionary<string, int>();

                bracketsRegexList.ForEach(delegate (Regex regex)
                {
                    List<Match> matches = regex.Matches(RegexEditor.Text).Cast<Match>().ToList();
                    Stack<Match> stackMatches = new Stack<Match>();

                    matches.ForEach(delegate (Match match)
                    {
                        if (openingBrackets.Contains(match.Value))
                        {
                            stackMatches.Push(match);
                        }
                        else if (stackMatches.Count > 0)
                        {
                            Match beginingMatch = stackMatches.Pop();

                            posStringToMatchingPosDict[beginingMatch.Index.ToString()] = match.Index;
                            posStringToMatchingPosDict[match.Index.ToString()] = beginingMatch.Index;
                        }
                    });
                });

                int pos = RegexEditor.TextArea.Caret.Offset;

                bool handled = false;

                if (posStringToMatchingPosDict.ContainsKey((pos - 1).ToString()))
                {
                    currentBracketColorizer.StartOffset = pos - 1;
                    currentBracketColorizer.EndOffset = pos;
                    matchingBracketColorizer.StartOffset = posStringToMatchingPosDict[(pos - 1).ToString()];
                    matchingBracketColorizer.EndOffset = posStringToMatchingPosDict[(pos - 1).ToString()] + 1;

                    handled = true;
                }
                else if (posStringToMatchingPosDict.ContainsKey((pos).ToString()))
                {
                    currentBracketColorizer.StartOffset = pos;
                    currentBracketColorizer.EndOffset = pos + 1;
                    matchingBracketColorizer.StartOffset = posStringToMatchingPosDict[(pos).ToString()];
                    matchingBracketColorizer.EndOffset = posStringToMatchingPosDict[(pos).ToString()] + 1;

                    handled = true;
                }

                if (handled)
                {
                    RegexEditor.TextArea.TextView.LineTransformers.Add(currentBracketColorizer);
                    RegexEditor.TextArea.TextView.LineTransformers.Add(matchingBracketColorizer);
                }
            }
            catch { }

            RegexEditor.TextArea.TextView.InvalidateVisual();
        }

        private void ShowMatchesButton_Click(object sender, RoutedEventArgs e)
        {
            // some tests here
            var atd = _bnpp.ActiveTextDocument;

            // TEST 1 works as expected
            /*
            atd.Selection.MoveToAbsoluteOffset(14);
            var oldAnchor = atd.Selection.AnchorPoint.CreateEditPoint();
            var oldActive = atd.Selection.ActivePoint.CreateEditPoint();

            atd.Selection.MoveToAbsoluteOffset(36);
            var oldAnchor2 = atd.Selection.AnchorPoint.CreateEditPoint();
            var oldActive2 = atd.Selection.ActivePoint.CreateEditPoint();

            oldActive.Insert("CIAO");
            oldActive2.Insert("CIAO");
            */

            // TEST 2 works as expected (not what we want)
            /*
            atd.Selection.MoveToAbsoluteOffset(14);
            atd.Selection.Insert("CIAO");
            atd.Selection.MoveToAbsoluteOffset(36);
            atd.Selection.Insert("CIAO");
            */

            // TEST 3 doesn't work as excepted (works as TEST 2)
            /*
            var oldActive = atd.Selection.AnchorPoint.CreateEditPoint();
            oldActive.MoveToAbsoluteOffset(14);            
            oldActive.Insert("CIAO");

            var oldActive2 = atd.Selection.AnchorPoint.CreateEditPoint();
            oldActive2.MoveToAbsoluteOffset(36);
            oldActive2.Insert("CIAO");
            */

            // atd.Selection.SwapAnchor();
            // atd.Selection.MoveToAbsoluteOffset(oldActive.AbsoluteCharOffset, true);            
            try
            {
                ShowMatches();
            }
            catch { }
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private RegexOptions GetRegexOptions()
        {
            return regExOptionViewModelsList.FindAll(re => re.Selected)
                .Aggregate(RegexOptions.None,
                (total, next) => total |= next.RegexOptions);
        }

        private string GetCurrentText()
        {
            if (Config.Instance.TextSourceOn == RegexTextSource.CurrentSelection)
            {
                return GetSelectedText();
            }
            else
            {
                return GetText();
            }
        }


        private void ShowMatches()
        {
            try
            {
                lastSelectionStart = 0;
                lastSelectionLength = 0;

                using (Dispatcher.DisableProcessing())
                {
                    int i = 0;
                    int countAllCaptures = 0;

                    Regex regex = new Regex(RegexEditor.Text, GetRegexOptions());

                    Func<string, string, int, List<RegexResult>> GetMatchesFor = (string text, string fileName, int selectionIndex) =>
                    {
                        MatchCollection matches = regex.Matches(text);
                        var ml = matches
                            .Cast<Match>()
                            .ToList()
                            .FindAll(delegate (Match m)
                            {
                                countAllCaptures++;

                                return m.Length > 0 || Config.Instance.ShowEmptyMatches;
                            })
                            .ConvertAll(delegate (Match m)
                            {
                                // RegexElement.Index.ToString() + "," + RegexElement.Length.ToString()
                                _bnpp.ActiveTextDocument.Selection.MoveToAbsoluteOffset(m.Index + 1);
                                var oldAnchor = _bnpp.ActiveTextDocument.Selection.AnchorPoint.CreateEditPoint();
                                // var oldActive = _bnpp.ActiveTextDocument.Selection.ActivePoint.CreateEditPoint();
                                RegexResult result = new RegexMatchResult(regex, m, oldAnchor, i, fileName, selectionIndex);

                                i++;

                                return result;
                            });
                        return ml;
                    };


                    if (Config.Instance.TextSourceOn == RegexTextSource.Directory)
                    {
                        int ft = 0;
                        int ff = 0;

                        MatchResultsTreeView.ItemsSource = GetFiles()
                            .Select(fileName =>
                            {
                                ft++;

                                List<RegexResult> temp = GetMatchesFor(File.ReadAllText(fileName), fileName, 0);

                                if (temp.Count > 0)
                                    ff++;

                                return new RegexFileResult(regex, null, null, Config.Instance.TextSourceDirectoryShowNotMatchedFiles ? ft : ff, fileName)
                                {
                                    Children = temp
                                };
                            })
                            .Where(regexFileResult => Config.Instance.TextSourceDirectoryShowNotMatchedFiles || regexFileResult.Children.Count > 0);

                        MatchesResultLabel.Content = $"{i} matches [Index,Length] + {(countAllCaptures - i)} empties matches found in {ff}/{ft} files";
                    }
                    else
                    {
                        lastMatchesText = GetText();

                        if (Config.Instance.TextSourceOn == RegexTextSource.CurrentSelection)
                        {
                            lastSelectionStart = GetSelectionStartIndex?.Invoke() ?? 0;
                            lastSelectionLength = GetSelectionLength?.Invoke() ?? 0;

                            MatchResultsTreeView.ItemsSource = GetMatchesFor(GetCurrentText(), "", lastSelectionStart);
                        }
                        else
                        {
                            MatchResultsTreeView.ItemsSource = GetMatchesFor(GetCurrentText(), "", 0);
                        }

                        MatchesResultLabel.Content = $"{i} matches [Index,Length] + {(countAllCaptures - i)} empties matches";
                    }

                    if (i > 0)
                    {
                        ((RegexResult)MatchResultsTreeView.Items[0]).IsSelected = true;
                        MatchResultsTreeView.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int files = 0;
                string text = GetCurrentText();

                Regex regex = new Regex(RegexEditor.Text, GetRegexOptions());

                int nbrOfElementToReplace = Config.Instance.TextSourceOn == RegexTextSource.Directory ? 0 : regex.Matches(text).Count;
                {
                    switch (Config.Instance.TextSourceOn)
                    {
                        case RegexTextSource.Directory:
                            if (!Config.Instance.OpenFilesForReplace && MessageBox.Show("This will modify files directly on the disk.\r\nModifications can not be cancel\r\nDo you want to continue ?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                                return;

                            string replaceText = ReplaceEditor.Text;

                            GetFiles().ForEach(fileName =>
                            {
                                if (Config.Instance.OpenFilesForReplace)
                                {
                                    if (TryOpen?.Invoke(fileName, false) ?? false)
                                    {
                                        text = GetText();
                                        int matchesCount = regex.Matches(text).Count;
                                        nbrOfElementToReplace += matchesCount;
                                        if (matchesCount > 0)
                                        {
                                            SetText(regex.Replace(text, replaceText));

                                            try
                                            {
                                                SaveCurrentDocument?.Invoke();
                                            }
                                            catch { }

                                            files++;
                                        }
                                    }
                                }
                                else
                                {
                                    text = File.ReadAllText(fileName);
                                    int matchesCount = regex.Matches(text).Count;
                                    nbrOfElementToReplace += matchesCount;

                                    if (matchesCount > 0)
                                    {
                                        File.WriteAllText(fileName, regex.Replace(text, replaceText));

                                        files++;
                                    }
                                }
                            });

                            break;
                        case RegexTextSource.CurrentSelection:
                            SetSelectedText(regex.Replace(text, ReplaceEditor.Text));
                            break;
                        default:
                            SetText(regex.Replace(text, ReplaceEditor.Text));
                            break;
                    }
                }

                if (Config.Instance.TextSourceOn == RegexTextSource.Directory)
                    MessageBox.Show(nbrOfElementToReplace.ToString() + $" elements have been replaced in {files} files");
                else
                    MessageBox.Show(nbrOfElementToReplace.ToString() + " elements have been replaced");


                ShowMatches();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = GetCurrentText();
                Regex regex = new Regex(RegexEditor.Text, GetRegexOptions());
                List<Match> matches = regex.Matches(text)
                    .Cast<Match>()
                    .ToList();

                if (Config.Instance.TextSourceOn == RegexTextSource.CurrentSelection)
                {
                    lastSelectionStart = GetSelectionStartIndex?.Invoke() ?? 0;
                    lastSelectionLength = GetSelectionLength?.Invoke() ?? 0;
                }
                else
                {
                    lastSelectionStart = 0;
                    lastSelectionLength = 0;
                }

                if (matches.Count > 0)
                    SetPosition(matches[0].Index + lastSelectionStart, 0);
                else
                    SetPosition(0, 0);


                matches.ForEach(delegate (Match match)
                {
                    try
                    {
                        SetSelection(match.Index + lastSelectionStart, match.Length);
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExtractMatchesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int globalIndex = 0;
                Regex regex = new Regex(RegexEditor.Text, GetRegexOptions());
                int fileIndex = 0;
                dynamic script = null;

                /*
                if (CSharpReplaceCheckbox.IsChecked.GetValueOrDefault())
                    script = CSScript.Evaluator.LoadCode(CSharpRegexTool.SyntaxRes.CSharpReplaceContainer.Replace("//code", ReplaceEditor.Text));
                    */
                Action<string, string> Extract = (text, fileName) =>
                {
                    List<Match> matches = regex.Matches(text)
                        .Cast<Match>()
                        .ToList();

                    if (matches.Count > 0 || Config.Instance.TextSourceDirectoryShowNotMatchedFiles)
                    {
                        if (Config.Instance.PrintFileNameWhenExtract)
                            sb.AppendLine("\r\n" + fileName);
                        /*
                        if (CSharpReplaceCheckbox.IsChecked.GetValueOrDefault())
                        {
                            int index = 0;

                            matches.ForEach(match =>
                            {
                                sb.Append(script.Replace(match, index, fileName, globalIndex, fileIndex));
                                globalIndex++;
                                index++;
                            });
                        }
                        */
                        else
                        {
                            matches.ForEach(match => sb.AppendLine(match.Value));
                        }

                        fileIndex++;
                    }
                };



                if (Config.Instance.TextSourceOn == RegexTextSource.Directory)
                {
                    GetFiles().ForEach(fileName =>
                    {
                        Extract(File.ReadAllText(fileName), fileName);
                    });
                }
                else
                {
                    Extract(GetCurrentText(), GetCurrentFileName?.Invoke() ?? string.Empty);
                }
                try
                {
                    SetTextInNew(sb.ToString());
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }

        private List<string> GetFiles()
        {
            string filter = Config.Instance.TextSourceDirectorySearchFilter.Trim();

            if (filter.Equals(string.Empty))
                filter = "*";


            List<string> result = new List<string>();

            filter.Split(';', ',', '|')
                .ToList()
                .ForEach(pattern => result.AddRange(Directory.GetFiles(Config.Instance.TextSourceDirectoryPath, pattern, Config.Instance.TextSourceDirectorySearchSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)));

            return result;
        }

        private void IsMatchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Config.Instance.TextSourceOn == RegexTextSource.Directory)
                {
                    List<string> files = GetFiles();

                    bool found = false;
                    string fileName = string.Empty;

                    for (int i = 0; i < files.Count && !found; i++)
                    {
                        found = Regex.IsMatch(File.ReadAllText(files[i]), RegexEditor.Text, GetRegexOptions());

                        if (found)
                        {
                            fileName = files[i];
                            break;
                        }
                    }

                    MessageBox.Show(found ? $"Yes (Found in \"{fileName}\")" : $"No (In Any files of \"{Config.Instance.TextSourceDirectoryPath}\")");
                }
                else
                {
                    MessageBox.Show(Regex.IsMatch(GetCurrentText(), RegexEditor.Text, GetRegexOptions()) ? "Yes" : "No");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RegexEditor_TextChanged(object sender, EventArgs e)
        {
            Config.Instance.RegexEditorText = RegexEditor.Text;

            try
            {
                cmiReplaceGroupByNumber.Items.Clear();
                cmiReplaceGroupByName.Items.Clear();

                Regex regex = new Regex(RegexEditor.Text, GetRegexOptions());

                regex.GetGroupNames().ToList()
                    .ForEach(delegate (string groupName)
                        {
                            cmiReplaceGroupByName.Items.Add("${" + groupName + "}");
                        });

                regex.GetGroupNumbers().ToList()
                    .ForEach(delegate (int groupNumber)
                    {
                        cmiReplaceGroupByNumber.Items.Add("$" + groupNumber.ToString());
                    });
            }
            catch { }
            finally
            {
                cmiReplaceGroupByNumber.IsEnabled = cmiReplaceGroupByNumber.Items.Count > 0;
                cmiReplaceGroupByName.IsEnabled = cmiReplaceGroupByName.Items.Count > 0;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPosition();

            RegexEditor.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
        }

        private void ReplaceEditor_TextChanged(object sender, EventArgs e)
        {
            Config.Instance.ReplaceEditorText = ReplaceEditor.Text;
        }

        private void MatchResultsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //e.Handled = true;
            // return;
            try
            {
                try
                {
                    RegexResult regexResult = e.NewValue as RegexResult;

                    if (regexResult != null && regexResult.FileName.Length > 0)
                    {
                        if ((TryOpen?.Invoke(regexResult.FileName, true) ?? false) && !(regexResult is RegexFileResult))
                            SetPosition(regexResult.Index, regexResult.Length);
                    }
                    else if (regexResult != null && lastMatchesText.Equals(GetText()))
                    {
                        SetPosition(regexResult.Index, regexResult.Length);
                    }
                }
                catch (Exception ex)
                {
                    SetPosition(1, 1);
                }

                e.Handled = true;

                if (MatchResultsTreeView.SelectedValue != null)
                {
                    if (MatchResultsTreeView.SelectedValue is RegexGroupResult)
                        MatchResultsTreeView.ContextMenu = MatchResultsTreeView.Resources["cmMatchResultsGroupItemMenu"] as ContextMenu;
                    else
                        MatchResultsTreeView.ContextMenu = MatchResultsTreeView.Resources["cmMatchResultsItemMenu"] as ContextMenu;
                }
                else
                {
                    MatchResultsTreeView.ContextMenu = MatchResultsTreeView.Resources["cmMatchResultsMenu"] as ContextMenu;
                }
            }
            catch { }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is TreeViewItem)
                {
                    TreeViewItem treeViewItem = sender as TreeViewItem;
                    if (treeViewItem.DataContext is RegexResult)
                    {
                        RegexResult regexResult = treeViewItem.DataContext as RegexResult;
                        if (regexResult.FileName.Length > 0 && !GetCurrentFileName().ToLower().Equals(regexResult.FileName.ToLower()))
                        {
                            if (TryOpen?.Invoke(regexResult.FileName, false) ?? false)
                            {
                                if (!(regexResult is RegexFileResult))
                                    SetPosition?.Invoke(regexResult.Index, regexResult.Length);
                            }
                        }

                        e.Handled = true;
                    }
                }
            }
            catch { }
        }

        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && sender is TreeViewItem)
                {
                    TreeViewItem treeViewItem = sender as TreeViewItem;
                    if (treeViewItem.DataContext is RegexResult)
                    {
                        RegexResult regexResult = treeViewItem.DataContext as RegexResult;
                        if (regexResult.FileName.Length > 0 && !GetCurrentFileName().ToLower().Equals(regexResult.FileName.ToLower()))
                        {
                            if (TryOpen?.Invoke(regexResult.FileName, false) ?? false)
                            {
                                if (!(regexResult is RegexFileResult))
                                    SetPosition?.Invoke(regexResult.Index, regexResult.Length);
                            }

                            e.Handled = true;
                        }
                    }
                }
            }
            catch { }
        }

        private void RegexLanguageElement_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount >= 2 && sender is FrameworkElement)
                {

                    RegexLanguageElement rle = (RegexLanguageElement)((FrameworkElement)sender).DataContext;

                    int moveCaret = 0;

                    if (RegexEditor.SelectionLength > 0)
                    {
                        RegexEditor.Document.Remove(RegexEditor.SelectionStart, RegexEditor.SelectionLength);
                        moveCaret = rle.Value.Length;
                    }

                    RegexEditor.Document.Insert(RegexEditor.TextArea.Caret.Offset, rle.Value);

                    RegexEditor.TextArea.Caret.Offset += moveCaret;
                    RegexEditor.SelectionStart = RegexEditor.TextArea.Caret.Offset;
                    RegexEditor.SelectionLength = 0;

                    mustSelectEditor = true;

                    e.Handled = true;
                }
            }
            catch
            { }
        }

        private void RegexLanguageElement_StackPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mustSelectEditor)
            {
                RegexEditor.Focus();
                mustSelectEditor = false;
            }
        }

        private void ReplaceLanguageElement_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount >= 2 && sender is FrameworkElement)
                {

                    ReplaceLanguageElement rle = (ReplaceLanguageElement)((FrameworkElement)sender).DataContext;

                    int moveCaret = 0;

                    if (RegexEditor.SelectionLength > 0)
                    {
                        ReplaceEditor.Document.Remove(ReplaceEditor.SelectionStart, ReplaceEditor.SelectionLength);
                        moveCaret = rle.Value.Length;
                    }

                    ReplaceEditor.Document.Insert(ReplaceEditor.TextArea.Caret.Offset, rle.Value);

                    ReplaceEditor.TextArea.Caret.Offset += moveCaret;
                    ReplaceEditor.SelectionStart = ReplaceEditor.TextArea.Caret.Offset;
                    ReplaceEditor.SelectionLength = 0;

                    mustSelectEditor = true;

                    e.Handled = true;

                }
            }
            catch
            { }

        }

        private void ReplaceLanguageElement_StackPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (mustSelectEditor)
                {
                    ReplaceEditor.Focus();
                    mustSelectEditor = false;
                }
            }
            catch { }
        }

        private void RegexLanguagesElementsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (RegexLanguagesElementsTreeView.SelectedValue != null)
                {
                    if (RegexLanguagesElementsTreeView.SelectedValue is RegexLanguageElementGroup)
                        tbxRegexLanguageElementDescription.Text = ((RegexLanguageElementGroup)RegexLanguagesElementsTreeView.SelectedValue).Description;
                    if (RegexLanguagesElementsTreeView.SelectedValue is RegexLanguageElement)
                        tbxRegexLanguageElementDescription.Text = ((RegexLanguageElement)RegexLanguagesElementsTreeView.SelectedValue).Description;
                }
            }
            catch { }
        }

        private void ReplaceLanguageElementsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ReplaceLanguageElementsListView.SelectedValue != null)
                {
                    if (ReplaceLanguageElementsListView.SelectedValue is ReplaceLanguageElement)
                        tbxReplacLanguageElementDescription.Text = ((ReplaceLanguageElement)ReplaceLanguageElementsListView.SelectedValue).Description;
                }
            }
            catch
            { }
        }

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                SaveWindowPosition();

                // RegexHistoryPopup.IsOpen = false;
                // ReplaceHistoryPopup.IsOpen = false;
                SetMaxSizes();
            }
            catch { }
        }

        private void SaveWindowPosition()
        {
            try
            {
                Config.Instance.GridFirstColumnWidth = FirstColumn.Width;
                Config.Instance.GridSecondColumnWidth = SecondColumn.Width;
                Config.Instance.GridThirdColumnWidth = ThirdColumn.Width;
                Config.Instance.GridRegexEditorRowHeight = RegexEditorRow.ActualHeight;
                Config.Instance.GridRegexLanguageElementsFirstRowHeight = RegexLanguageElementFirstRow.ActualHeight;

                // Config.Instance.DialogLeft = this.Left;
                // Config.Instance.DialogTop = this.Top;
                Config.Instance.DialogWidth = this.ActualWidth;
                Config.Instance.DialogHeight = this.ActualHeight;

                // Config.Instance.DialogMaximized = this.WindowState == WindowState.Maximized;

                Config.Instance.Save();
            }
            catch { }
        }

        private void SetMaxSizes()
        {
            try
            {
                RegexEditorRow.MaxHeight = Root.ActualHeight - RegexEditor.TransformToAncestor(Root).Transform(new Point(0, 0)).Y - 5 - 10;
                if (OptionTabControl.SelectedItem.Equals(RegexTabItem))
                    RegexLanguageElementFirstRow.MaxHeight = Root.ActualHeight - RegexLanguagesElementsTreeView.TransformToAncestor(Root).Transform(new Point(0, 0)).Y - 5 - 40;
                if (OptionTabControl.SelectedItem.Equals(ReplaceTabItem))
                    ReplaceLanguageElementFirstRow.MaxHeight = Root.ActualHeight - ReplaceLanguageElementsListView.TransformToAncestor(Root).Transform(new Point(0, 0)).Y - 5 - 40;
                //FirstColumn.MaxWidth = Math.Max(Root.ActualWidth - 10 - 100, 0);
                //SecondColumn.MaxWidth = Math.Max(Root.ActualWidth - Math.Min(FirstColumn.ActualWidth, Root.ActualWidth) - 10 - 40, 0);
            }
            catch { }
        }

        private void RegexEditor_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                SetMaxSizes();
            }
            catch { }
        }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RegexEditor.Focus();
                RegexEditor.TextArea.Focus();
            }
            catch { }
        }

        private void RegexHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                /*
                RegexHistoryPopup.IsOpen = !RegexHistoryPopup.IsOpen;
                if (RegexHistoryPopup.IsOpen)
                {
                    RegexHistoryListBox.Focus();
                    if (RegexHistoryListBox.Items.Count > 0)
                        RegexHistoryListBox.SelectedIndex = 0;
                }
                */
            }
            catch { }
        }

        private void RegexHistoryListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                /*
                var focused = FocusManager.GetFocusedElement(this);

                var item = focused as ListBoxItem;
                if (focused != RegexHistoryButton && (item == null || !RegexHistoryListBox.Items.Contains(item.DataContext)))
                {
                    RegexHistoryPopup.IsOpen = false;
                }
                */
            }
            catch { }
        }

        private void Root_LocationChanged(object sender, EventArgs e)
        {
            try
            {
                SaveWindowPosition();

                // RegexHistoryPopup.IsOpen = false;
                // ReplaceHistoryPopup.IsOpen = false;
            }
            catch { }
        }

        private void RegexHistoryListBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                /*
                if (e.Key == Key.Enter && RegexHistoryListBox.SelectedValue != null)
                {
                    RegexEditor.Text = RegexHistoryListBox.SelectedValue.ToString();
                    RegexHistoryPopup.IsOpen = false;
                    SetToHistory(1);
                }
                else if (e.Key == Key.Escape)
                {
                    RegexHistoryPopup.IsOpen = false;
                    RegexHistoryButton.Focus();
                }
                */
            }
            catch { }
        }

        private void RegexHistoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                /*
                if (RegexHistoryListBox.SelectedValue != null)
                {
                    RegexEditor.Text = RegexHistoryListBox.SelectedValue.ToString();
                    RegexHistoryPopup.IsOpen = false;
                    SetToHistory(1);
                }
                */
            }
            catch { }
        }

        private void ReplaceHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                /*
                ReplaceHistoryPopup.IsOpen = !ReplaceHistoryPopup.IsOpen;
                if (ReplaceHistoryPopup.IsOpen)
                {
                    ReplaceHistoryListBox.Focus();
                    if (ReplaceHistoryListBox.Items.Count > 0)
                        ReplaceHistoryListBox.SelectedIndex = 0;
                }
                */
            }
            catch { }
        }

        private void ReplaceHistoryListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                /*
                var focused = FocusManager.GetFocusedElement(this);

                var item = focused as ListBoxItem;
                if (focused != ReplaceHistoryButton && (item == null || !ReplaceHistoryListBox.Items.Contains(item.DataContext)))
                {
                    ReplaceHistoryPopup.IsOpen = false;
                }
                */
            }
            catch { }
        }

        private void ReplaceHistoryListBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                /*
                if (e.Key == Key.Enter && ReplaceHistoryListBox.SelectedValue != null)
                {
                    ReplaceEditor.Text = ReplaceHistoryListBox.SelectedValue.ToString();
                    ReplaceHistoryPopup.IsOpen = false;
                    SetToHistory(2);
                }
                else if (e.Key == Key.Escape)
                {
                    ReplaceHistoryPopup.IsOpen = false;
                    ReplaceHistoryButton.Focus();
                }
                */
            }
            catch { }
        }

        private void ReplaceHistoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                /*
                if (ReplaceHistoryListBox.SelectedValue != null)
                {
                    ReplaceEditor.Text = ReplaceHistoryListBox.SelectedValue.ToString();
                    ReplaceHistoryPopup.IsOpen = false;
                    SetToHistory(2);
                }
                */
            }
            catch { }
        }

        private void ShowMatchesMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Config.Instance.MatchesShowLevel = 1;
                Config.Instance.Save();
                RefreshMatches();
            }
            catch { }
        }

        private void ShowGroupsMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Config.Instance.MatchesShowLevel = 2;
                Config.Instance.Save();
                RefreshMatches();
            }
            catch { }
        }

        private void ShowCapturesMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Config.Instance.MatchesShowLevel = 3;
                Config.Instance.Save();
                RefreshMatches();
            }
            catch { }
        }

        private void RefreshMatches()
        {
            try
            {
                if (lastMatchesText.Equals(GetText()))
                {
                    using (Dispatcher.DisableProcessing())
                    {
                        ((List<RegexResult>)MatchResultsTreeView.ItemsSource)
                            .ForEach(delegate (RegexResult regRes)
                            {
                                regRes.RefreshExpands();
                            });
                    }
                }
                else
                {
                    ShowMatches();
                }
            }
            catch
            { }
        }

        private void ReplaceInEditor_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MatchResultsTreeView.SelectedValue is RegexFileResult)
                {
                    RegexFileResult regexFileResult = MatchResultsTreeView.SelectedValue as RegexFileResult;
                    if (TryOpen.Invoke(regexFileResult.FileName, false))
                    {
                        string text = GetText();
                        Regex regex = regexFileResult.Regex;

                        int nbrOfElementToReplace = regex.Matches(text).Count;
                        /*
                        if (CSharpReplaceCheckbox.IsChecked.GetValueOrDefault())
                        {
                            dynamic script = CSScript.Evaluator.LoadCode(CSharpRegexTool.SyntaxRes.CSharpReplaceContainer.Replace("//code", ReplaceEditor.Text));

                            int index = -1;

                            SetText(regex.Replace(text, match =>
                            {
                                index++;
                                return script.Replace(match, index, regexFileResult.FileName, index + (regexFileResult.Children.Count > 0 ? regexFileResult.Children[0].RegexElementNb : 0), regexFileResult.RegexElementNb - 1);
                            }));

                            SaveCurrentDocument?.Invoke();
                        }
                        else
                        */
                        {
                            SetText(regex.Replace(text, ReplaceEditor.Text));
                            SaveCurrentDocument?.Invoke();
                        }

                        MessageBox.Show(nbrOfElementToReplace.ToString() + " elements has been replaced");
                    }

                }
                else if (MatchResultsTreeView.SelectedValue is RegexResult)
                {
                    RegexResult regexResult = MatchResultsTreeView.SelectedValue as RegexResult;
                    if (!string.IsNullOrEmpty(regexResult.FileName) && Config.Instance.OpenFilesForReplace && !(TryOpen?.Invoke(regexResult.FileName, false) ?? false))
                    {
                        return;
                    }
                    else if (!string.IsNullOrEmpty(regexResult.FileName) && !Config.Instance.OpenFilesForReplace &&
                        MessageBox.Show("This will modify the file directly on the disk.\r\nModifications can not be cancel\r\nDo you want to continue ?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        return;
                    }

                    string text = !string.IsNullOrEmpty(regexResult.FileName) && !Config.Instance.OpenFilesForReplace ? File.ReadAllText(regexResult.FileName) : GetText();
                    string posValue = text.Substring(regexResult.Index, regexResult.Length);

                    if (regexResult.Value.Equals(posValue))
                    {
                        string beforeMatch = text.Substring(0, regexResult.Index);
                        string afterMatch = text.Substring(regexResult.Index + regexResult.Length);
                        string newText = text;
                        /*
                        if (CSharpReplaceCheckbox.IsChecked.GetValueOrDefault())
                        {
                            dynamic script = CSScript.Evaluator.LoadCode(CSharpRegexTool.SyntaxRes.CSharpReplaceContainer.Replace("//code", ReplaceEditor.Text));

                            if (regexResult is RegexMatchResult)
                            {
                                RegexMatchResult regexMatchResult = regexResult as RegexMatchResult;
                                newText = beforeMatch + script.Replace((Match)regexMatchResult.RegexElement, regexMatchResult.RegexElementNb, regexResult.FileName, regexMatchResult.RegexElementNb, 0) + afterMatch;
                            }
                            else if (regexResult is RegexGroupResult)
                            {
                                RegexGroupResult regexGroupResult = regexResult as RegexGroupResult;
                                newText = beforeMatch + script.Replace((Match)regexGroupResult.Parent.RegexElement, (Group)regexGroupResult.RegexElement, regexResult.RegexElementNb, regexResult.FileName, regexResult.RegexElementNb, 0) + afterMatch;
                            }
                            else if (regexResult is RegexCaptureResult)
                            {
                                RegexCaptureResult regexCaptureResult = regexResult as RegexCaptureResult;
                                newText = beforeMatch + script.Replace((Match)regexCaptureResult.Parent.Parent.RegexElement, (Group)regexCaptureResult.Parent.RegexElement, (Capture)regexCaptureResult.RegexElement, regexResult.RegexElementNb, regexResult.FileName, regexResult.RegexElementNb, 0) + afterMatch;
                            }
                        }
                        else
                        */
                        {
                            Match superMatch = (regexResult.RegexElement as Match) ?? (regexResult.Parent?.RegexElement as Match) ?? (regexResult.Parent?.Parent?.RegexElement as Match);
                            string replaceText = Regex.Replace(ReplaceEditor.Text,
                                @"[$]((?<dollar>[$])|(?<number>\d+)|[{](?<name>[a-zA-Z][a-zA-Z0-9]+)[}]|(?<and>[&])|(?<before>[`])|(?<after>['])|(?<last>[+])|(?<all>[_]))",
                                match =>
                                {
                                    if (match.Groups["dollar"].Success)
                                        return "$";
                                    else if (match.Groups["number"].Success)
                                        return superMatch.Groups[int.Parse(match.Groups["number"].Value)].Value;
                                    else if (match.Groups["name"].Success)
                                        return superMatch.Groups[match.Groups["name"].Value].Value;
                                    else if (match.Groups["and"].Success)
                                        return superMatch.Value;
                                    else if (match.Groups["before"].Success)
                                        return beforeMatch;
                                    else if (match.Groups["after"].Success)
                                        return afterMatch;
                                    else if (match.Groups["last"].Success)
                                        return superMatch.Groups[superMatch.Groups.Count - 1].Value;
                                    else if (match.Groups["all"].Success)
                                        return text;
                                    else
                                        return match.Value;
                                });

                            newText = beforeMatch + replaceText + afterMatch;
                        }

                        if (!string.IsNullOrEmpty(regexResult.FileName) && !Config.Instance.OpenFilesForReplace)
                        {
                            File.WriteAllText(regexResult.FileName, newText);
                        }
                        else
                        {
                            SetText(newText);

                            SaveCurrentDocument?.Invoke();

                            SetPosition(regexResult.Index, ReplaceEditor.Text.Length);
                        }

                        ShowMatches();
                    }
                    else
                    {
                        MessageBox.Show("Text changed since last matches search.\nReload the search before replace", "Text changed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

        }

        private void InsertValueInReplaceField_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MatchResultsTreeView.SelectedValue is RegexFileResult)
                {
                    RegexFileResult regexfileResult = MatchResultsTreeView.SelectedValue as RegexFileResult;
                    ReplaceEditor.Document.Replace(ReplaceEditor.SelectionStart, ReplaceEditor.SelectionLength, regexfileResult.FileName);
                }
                else if (MatchResultsTreeView.SelectedValue is RegexResult)
                {
                    RegexResult regexResult = MatchResultsTreeView.SelectedValue as RegexResult;
                    ReplaceEditor.Document.Replace(ReplaceEditor.SelectionStart, ReplaceEditor.SelectionLength, regexResult.Value);
                }
            }
            catch { }
        }

        private void InsertGroupByNumberInReplaceField_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MatchResultsTreeView.SelectedValue is RegexGroupResult)
                {
                    RegexGroupResult regexGroupResult = MatchResultsTreeView.SelectedValue as RegexGroupResult;
                    ReplaceEditor.Document.Replace(ReplaceEditor.SelectionStart, ReplaceEditor.SelectionLength, "$" + regexGroupResult.RegexElementNb.ToString());
                }
            }
            catch { }
        }

        private void InsertGroupByNameInReplaceField_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MatchResultsTreeView.SelectedValue is RegexGroupResult)
                {
                    RegexGroupResult regexGroupResult = MatchResultsTreeView.SelectedValue as RegexGroupResult;
                    ReplaceEditor.Document.Replace(ReplaceEditor.SelectionStart, ReplaceEditor.SelectionLength, "${" + regexGroupResult.GroupName + "}");
                }
            }
            catch { }
        }

        private void InsertInReplaceFromContextMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReplaceEditor.Document.Replace(ReplaceEditor.SelectionStart, ReplaceEditor.SelectionLength, ((MenuItem)e.OriginalSource).Header.ToString());
            }
            catch { }
        }


        private void New_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // SetToHistory();
                Config.Instance.ExpressionLibrary = new RExprLibrary();
                RegexEditor.Text = "";
                ReplaceEditor.Text = "";

                regExOptionViewModelsList.ForEach(delegate (RegExOptionViewModel optionModel)
                {
                    optionModel.Selected = false;
                });
            }
            catch { }
        }

        private void Open_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenRegex();
            }
            catch { }
        }

        private void OpenRegex()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "Open a Regex",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = "Regex files|*.regex",
                    FilterIndex = 0
                };

                bool? result = dialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    if (File.Exists(dialog.FileName))
                    {
                        try
                        {
                            // save previous work if any
                            Config.Instance.ExpressionLibrary.Save();
                            //
                            Config.Instance.ExpressionLibrary = new RExprLibrary(dialog.FileName);
                            Config.Instance.ExpressionLibrary.Load();

                            ExpressionComboBox.SelectedItem = Config.Instance.ExpressionLibrary.Items.First();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch { }
        }

        private void Save_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Config.Instance.ExpressionLibrary.Filepath))
            {
                // it's first time we save our library
                Save_as_MenuItem_Click(null, null);
            }
            else
            {
                Config.Instance.ExpressionLibrary.Save();
            }
        }


        private void Save_as_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    DefaultExt = "regex",
                    Filter = "Regex files|*.regex",
                    FilterIndex = 0
                };

                bool? result = dialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    Config.Instance.ExpressionLibrary.Filepath = dialog.FileName;
                    try
                    {
                        Config.Instance.ExpressionLibrary.Save();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch { }
        }


        private void cmiRegexCopyForOnOneLine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(RegexPatternIndenter.SetOnOneLine(RegexEditor.SelectionLength > 0 ? RegexEditor.SelectedText : RegexEditor.Text));
            }
            catch { }
        }

        private void cmiRegexCopyForXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText((RegexEditor.SelectionLength > 0 ? RegexEditor.SelectedText : RegexEditor.Text).EscapeXml());
            }
            catch { }
        }

        private void cmiRegexPasteFromXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegexEditor.SelectedText = Clipboard.GetText().UnescapeXml();
            }
            catch { }
        }

        private void cmiRegexCut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegexEditor.Cut();
            }
            catch { }
        }

        private void cmiRegexCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegexEditor.Copy();
            }
            catch { }
        }

        private void cmiRegexPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegexEditor.Paste();
            }
            catch { }
        }

        private void cmiReplaceCut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReplaceEditor.Cut();
            }
            catch { }
        }

        private void cmiReplaceCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReplaceEditor.Copy();
            }
            catch { }
        }

        private void cmiReplacePaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReplaceEditor.Paste();
            }
            catch { }
        }

        private void cmiRegexSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegexEditor.SelectAll();
            }
            catch { }
        }

        private void cmiReplaceSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReplaceEditor.SelectAll();
            }
            catch { }
        }

        private void cmiRegexIndent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RegexEditor.SelectionLength > 0)
                {
                    RegexEditor.SelectedText = IndentRegexPattern(RegexEditor.SelectedText);
                }
                else
                    RegexEditor.Text = IndentRegexPattern(RegexEditor.Text);

                regExOptionViewModelsList.Find(vm => vm.RegexOptions == RegexOptions.IgnorePatternWhitespace).Selected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string IndentRegexPattern(string pattern)
        {
            return RegexPatternIndenter.IndentRegexPattern(pattern,
                Config.Instance.AutoIndentCharClassesOnOneLine,
                Config.Instance.AutoIndentKeepQuantifiersOnSameLine);
        }

        private void cmiRegexSetOnOneLine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RegexEditor.SelectionLength > 0)
                {
                    RegexEditor.SelectedText = RegexPatternIndenter.SetOnOneLine(RegexEditor.SelectedText);
                }
                else
                {
                    RegexEditor.Text = RegexPatternIndenter.SetOnOneLine(RegexEditor.Text);

                    if (regExOptionViewModelsList.Find(vm => vm.RegexOptions == RegexOptions.IgnorePatternWhitespace).Selected
                        && MessageBox.Show("Regex Option IgnorePatternWhitespace is checked.\nUncheck it ?", "IgnorePatternWhitespace",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        regExOptionViewModelsList.Find(vm => vm.RegexOptions == RegexOptions.IgnorePatternWhitespace).Selected = false;
                    }
                }
            }
            catch { }
        }

        private void miRegexOption_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem mi = sender as MenuItem;

                if (sender != null)
                {
                    mi.IsChecked = !mi.IsChecked;
                }
            }
            catch { }
        }


        private void SpecifiedDirectoryTextSourcePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog()
                {
                    Description = "Select source folder",
                    UseDescriptionForTitle = true
                };

                if (Directory.Exists(SpecifiedDirectoryTextSourcePathComboBox.Text))
                    folderBrowserDialog.SelectedPath = SpecifiedDirectoryTextSourcePathComboBox.Text;

                // if (folderBrowserDialog.ShowDialog(GetWindow(this)).GetValueOrDefault(false))
                if (folderBrowserDialog.ShowDialog().GetValueOrDefault(false))
                {
                    SpecifiedDirectoryTextSourcePathComboBox.Text = folderBrowserDialog.SelectedPath;
                }
            }
            catch { }
        }

        private void RestoreLastMachesSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetPosition?.Invoke(lastSelectionStart, lastSelectionLength);
            }
            catch { }
        }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private bool ShouldRExprItemBeSaved(RExprItem oldItem)
        {
            // TODO options
            if (oldItem.MatchText != RegexEditor.Text ||
                oldItem.ReplaceText != ReplaceEditor.Text ||
                oldItem.Options != regExOptionViewModelsList
                    .Aggregate<RegExOptionViewModel, string>("", (total, next) => total + (next.Selected ? next.Name + " " : ""))
                    .Trim())
            {
                return true;
            }
            return false;
        }

        private void SaveExpressionItem(RExprItem oldItem)
        {
            oldItem.MatchText = RegexEditor.Text;
            oldItem.ReplaceText = ReplaceEditor.Text;
            oldItem.Options = regExOptionViewModelsList
                .Aggregate<RegExOptionViewModel, string>("", (total, next) => total + (next.Selected ? next.Name + " " : ""))
                .Trim();
            //            
            // save
            Config.Instance.ExpressionLibrary.Save();
        }

        private void ComboBox_Selected(object sender, SelectionChangedEventArgs e)
        {
            int k = 0;
            if (e.RemovedItems.Count == 1)
            {
                // save data to items
                RExprItem oldItem = (RExprItem)e.RemovedItems[0];
                if (ShouldRExprItemBeSaved(oldItem))
                {
                    SaveExpressionItem(oldItem);
                }
            }
            if (e.AddedItems.Count == 1)
            {
                _selectedExprItem = (RExprItem)e.AddedItems[0];
                RegexEditor.Text = _selectedExprItem.MatchText;
                ReplaceEditor.Text = _selectedExprItem.ReplaceText;
                // set options
                string[] xOptions = !String.IsNullOrEmpty(_selectedExprItem.Options) ? _selectedExprItem.Options.Split(' ') : null;
                regExOptionViewModelsList.ForEach(delegate (RegExOptionViewModel optionModel)
                {
                    optionModel.Selected = (xOptions != null ? xOptions.Contains(optionModel.Name) : false);
                });
            }
        }

        private void RExprItemNew(object sender, RoutedEventArgs e)
        {
            if (_selectedExprItem != null && ShouldRExprItemBeSaved(_selectedExprItem))
            {
                SaveExpressionItem(_selectedExprItem);
            }
            InputBox ib = new InputBox("Nome della nuova espressione", "Nome");
            if (ib.ShowDialog() == true)
            {
                // Enum.
                RExprItem newItem = new RExprItem();
                newItem.Name = ib.Answer;
                Config.Instance.ExpressionLibrary.Items.Add(newItem);
                ExpressionComboBox.SelectedItem = newItem;
            }
        }

        private void RExprItemSave(object sender, RoutedEventArgs e)
        {
            if (_selectedExprItem != null && ShouldRExprItemBeSaved(_selectedExprItem))
            {
                SaveExpressionItem(_selectedExprItem);
                MessageBox.Show("Saved!");
            }
        }

        private void RExprItemDelete(object sender, RoutedEventArgs e)
        {
            if (_selectedExprItem != null)
            {
                Config.Instance.ExpressionLibrary.Items.Remove(_selectedExprItem);
                Config.Instance.ExpressionLibrary.Save();
                _selectedExprItem = null;
                // TODO: first element
                RegexEditor.Text = "";
                ReplaceEditor.Text = "";
            }
        }
    }
}
