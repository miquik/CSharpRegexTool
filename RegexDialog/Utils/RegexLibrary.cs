using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RegexDialog
{
    public class RExprItem
    {
        public enum RExprType
        {
            FULL = 1,
            MATCH_ONLY = 2,
            REPLACE_ONLY = 4
        };

        public string Name { get; set; }

        public string MatchText { get; set; }
        public string ReplaceText { get; set; }
        public string Options { get; set; }

        public RExprType Type
        {
            get
            {
                if (!String.IsNullOrEmpty(MatchText) && String.IsNullOrEmpty(ReplaceText))
                {
                    return RExprType.MATCH_ONLY;
                }
                else if (String.IsNullOrEmpty(MatchText) && !String.IsNullOrEmpty(ReplaceText))
                {
                    return RExprType.REPLACE_ONLY;
                }
                return RExprType.FULL;
            }
        }
    }


    public class RExprLibrary : INotifyPropertyChanged
    {
        string _filepath;
        ObservableCollection<RExprItem> _items;

        public RExprLibrary()
        {
            _filepath = "";
            _items = new ObservableCollection<RExprItem>();
        }

        public RExprLibrary(string path)
        {
            _filepath = path;
            _items = new ObservableCollection<RExprItem>();
        }


        public string Filepath
        {
            get { return _filepath; }
            set { _filepath = value; }
        }
        public string Name
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(Filepath); }
        }
        public ObservableCollection<RExprItem> Items
        {
            get { return _items; }
        }


        public void Save()
        {
            if (String.IsNullOrEmpty(Filepath))
            {
                // don't know where to save
                return;
            }

            XDocument doc = new XDocument();
            XElement root = new XElement("rexpr_library");

            foreach (RExprItem item in Items)
            {
                XElement xelem = new XElement("rexpr_item", 
                    new XAttribute("name", System.Net.WebUtility.HtmlEncode(item.Name)));

                if (item.Type == RExprItem.RExprType.MATCH_ONLY || item.Type == RExprItem.RExprType.FULL)
                {
                    XElement mitem = new XElement("matchtext", 
                        new XCData(System.Net.WebUtility.HtmlEncode(item.MatchText)));
                    xelem.Add(mitem);
                }
                if (item.Type == RExprItem.RExprType.REPLACE_ONLY || item.Type == RExprItem.RExprType.FULL)
                {
                    XElement ritem = new XElement("replacetext",
                        new XCData(System.Net.WebUtility.HtmlEncode(item.ReplaceText)));
                    xelem.Add(ritem);
                }
                XElement oitem = new XElement("options", item.Options);
                xelem.Add(oitem);
                root.Add(xelem);
            }

            doc.Add(root);
            doc.Save(Filepath);
        }

        public bool Load()
        {
            _items.Clear();
            if (System.IO.File.Exists(Filepath) == false)
            {
                return false;
            }
            //
            XDocument doc = null;
            try
            {
                doc = XDocument.Load(Filepath);
                // parse file
                if (doc.Root.Name != "rexpr_library")
                {
                    // not valid rexpr library
                    return false;
                }
                //
                IEnumerable<XElement> items = doc.Root.Elements("rexpr_item");
                if (items.Count() == 0)
                {
                    return false;
                }
                //
                int count = 1;
                foreach (XElement item in items)
                {
                    RExprItem ritem = new RExprItem();
                    if (item.Attribute("name") != null)
                    {
                        ritem.Name = System.Net.WebUtility.HtmlDecode(item.Attribute("name").Value);                        
                    }
                    else
                    {
                        ritem.Name = "Expression_" + count.ToString();
                        count++;                        
                    }
                    // parse content
                    ritem.MatchText = String.Empty;
                    ritem.ReplaceText = String.Empty;
                    if (item.Element("matchtext") != null && item.Element("matchtext").Nodes().Count() > 0)
                    {
                        // only one node of type CDATA allowed
                        XCData cdata = (XCData)item.Element("matchtext").Nodes().Where(x => x is XCData).FirstOrDefault();
                        if (cdata != null)
                        {
                            ritem.MatchText = System.Net.WebUtility.HtmlDecode(cdata.Value);
                        }
                    }
                    //
                    if (item.Element("replacetext") != null && item.Element("replacetext").Nodes().Count() > 0)
                    {
                        // only one node of type CDATA allowed
                        XCData cdata = (XCData)item.Element("replacetext").Nodes().Where(x => x is XCData).FirstOrDefault();
                        if (cdata != null)
                        {
                            ritem.ReplaceText = System.Net.WebUtility.HtmlDecode(cdata.Value);
                        }
                    }
                    ritem.Options = "";
                    if (item.Element("options") != null && String.IsNullOrEmpty(item.Element("options").Value) == false)
                    {
                        ritem.Options = item.Element("options").Value;
                    }
                    // TODO options
                    _items.Add(ritem);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return true;
        }

        #region On PropertyChanged 

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion



        public static string XmlString(string text)
        {
            var sb = new StringBuilder(text.Length);

            foreach (var chr in text)
            {
                switch (chr)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    //			case '\"':
                    //				sb.Append("&quot;");
                    //				break;
                    case '&':
                        sb.Append("&amp;");
                        break;

                    // legal control characters
                    case '\n':
                        sb.Append("\n");
                        break;
                    case '\r':
                        sb.Append("\r");
                        break;
                    case '\t':
                        sb.Append("\t");
                        break;

                    default:
                        if (chr < 32)
                            throw new InvalidOperationException("Invalid character in Xml String. Chr " + Convert.ToInt16(chr) + " is illegal.");
                        else
                            sb.Append(chr);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
