using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.GUI.WinForms.Controls
{
    /// <summary>
    /// A control which provides the means to select a node formatter
    /// </summary>
    public partial class FormatterControl : UserControl
    {
        private Formatter _defaultFormatter;
        private readonly List<Formatter> _formatters = new List<Formatter>();

        /// <summary>
        /// Creates a new formatter control
        /// </summary>
        public FormatterControl()
        {
            InitializeComponent();

            //Load Formatters
            Type targetType = typeof (INodeFormatter);
            foreach (Type t in Assembly.GetAssembly(targetType).GetTypes())
            {
                if (t.Namespace == null) continue;

                if (!t.Namespace.Equals("VDS.RDF.Writing.Formatting")) continue;
                if (!t.GetInterfaces().Contains(targetType)) continue;
                try
                {
                    INodeFormatter formatter = (INodeFormatter) Activator.CreateInstance(t);
                    _formatters.Add(new Formatter(formatter.GetType(), formatter.ToString()));
                }
                catch
                {
                    //Ignore this Formatter
                }
            }
            _formatters.Sort();

            cboFormat.DataSource = _formatters;
            cboFormat.SelectedItem = _defaultFormatter ?? _formatters.First();
            cboFormat.SelectedIndexChanged += cboFormat_SelectedIndexChanged;
            RaiseFormatterChanged();
        }

        private void cboFormat_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            if (cboFormat.SelectedItem == null) return;
            CurrentFormatter = (Formatter) cboFormat.SelectedItem;
            RaiseFormatterChanged();
        }

        /// <summary>
        /// Gets/Sets the default formatter
        /// </summary>
        public Type DefaultFormatter
        {
            get { return _defaultFormatter != null ? _defaultFormatter.Type : null; }
            set
            {
                if (value == null) return;
                Formatter formatter = _formatters.FirstOrDefault(f => f.Type == value);
                if (formatter == null) return;
                _defaultFormatter = formatter;
                cboFormat.SelectedItem = _defaultFormatter;
                RaiseFormatterChanged();
            }
        }

        /// <summary>
        /// Gets/Sets the current formatter
        /// </summary>
        public Formatter CurrentFormatter { get; set; }

        /// <summary>
        /// Gets an instance of the currently selected formatter
        /// </summary>
        /// <returns></returns>
        public INodeFormatter GetFormatter()
        {
            return GetFormatter(null);
        }

        /// <summary>
        /// Gets an instance of the currently selected formatter using the given namespaces if possible
        /// </summary>
        /// <param name="namespaces">Namespaces</param>
        /// <returns></returns>
        public INodeFormatter GetFormatter(INamespaceMapper namespaces)
        {
            return CurrentFormatter.CreateInstance(namespaces);
        }

        /// <summary>
        /// Helper method for raising the FormatterChanged event
        /// </summary>
        protected void RaiseFormatterChanged()
        {
            FormatterChanged d = FormatterChanged;
            if (d == null) return;
            d(this, CurrentFormatter);
        }

        /// <summary>
        /// Event which is raised when the formatter is changed
        /// </summary>
        public event FormatterChanged FormatterChanged;
    }

    /// <summary>
    /// Represents a formatter
    /// </summary>
    public class Formatter
        : IComparable<Formatter>
    {
        /// <summary>
        /// Creates a new formatter
        /// </summary>
        /// <param name="t">Formatter Type</param>
        /// <param name="name">Friendly Name</param>
        public Formatter(Type t, string name)
        {
            Type = t;
            Name = name;
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets/Sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Creates an instance of the given formatter using the namespaces provided if possible
        /// </summary>
        /// <param name="namespaces">Namespaces</param>
        /// <returns></returns>
        public INodeFormatter CreateInstance(INamespaceMapper namespaces)
        {
            if (namespaces != null)
            {
                try
                {
                    INodeFormatter formatter = (INodeFormatter) Activator.CreateInstance(Type, new object[] {namespaces});
                    return formatter;
                }
                catch
                {
                    // Ignore
                }
            }
            try
            {
                INodeFormatter formatter = (INodeFormatter) Activator.CreateInstance(Type);
                return formatter;
            }
            catch (Exception)
            {
                // Fallback to default formatter
                return new SparqlFormatter();
            }
        }

        /// <summary>
        /// Gets the string representation of the formatter which is its friendly name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Compares this formatter to another
        /// </summary>
        /// <param name="other">Other formatter</param>
        /// <returns></returns>
        public int CompareTo(Formatter other)
        {
            return string.CompareOrdinal(Name, other.Name);
        }
    }
}