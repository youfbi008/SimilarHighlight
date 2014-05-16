using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SimilarHighlight.Option
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class OptionPage : DialogPage
    {
        private bool enabled = true;

        [Category("Highlight Settings")]
        [DisplayName("Enabled")]
        [Description("Enable the tool.")]
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        private SimilarityType similarityLevel = SimilarityType.High;
        public enum SimilarityType
        {
            High = 6,
            Stardard = 5,
            Low = 4
        }

        [Category("Highlight Settings")]
        [DisplayName("SimilarityLevel")]
        [Description("Set the similarity Level.")]
        public SimilarityType SimilarityLevel
        {
            get { return similarityLevel; }
            set { similarityLevel = value; }
        }

        private Color backgroundColor = Color.LightGreen;

        [Category("Highlight Settings")]
        [DisplayName("BackgroundColor")]
        [Description("Background Color")]
        [Editor(typeof(ColorEditor), typeof(UITypeEditor))]
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        private Color foregroundColor = Color.DarkBlue;

        [Category("Highlight Settings")]
        [DisplayName("ForegroundColor")]
        [Description("Foreground Color")]
        public Color ForegroundColor
        {
            get { return foregroundColor; }
            set { foregroundColor = value; }
        }

        private bool marginEnabled = true;

        [Category("Margin Settings")]
        [DisplayName("MarginEnabled")]
        [Description("Enable the Margin")]
        public bool MarginEnabled
        {
            get { return marginEnabled; }
            set { marginEnabled = value; }
        }

        private double marginWidth = 10.0;

        [Category("Margin Settings")]
        [DisplayName("MarginWidth")]
        [Description("Margin Width")]
        public double MarginWidth
        {
            get { return marginWidth; }
            set { marginWidth = value; }
        }

        private Color caretColor = Color.Red;

        [Category("Margin Settings")]
        [DisplayName("CaretColor")]
        [Description("Caret Color")]
        public Color CaretColor
        {
            get { return caretColor; }
            set { caretColor = value; }
        }

        private Color matchColor = Color.Blue;

        [Category("Margin Settings")]
        [DisplayName("MatchColor")]
        [Description("Match Color")]
        public Color MatchColor
        {
            get { return matchColor; }
            set { matchColor = value; }
        }

        private Color offScreenColor = Color.FromArgb(0x30, 0x00, 0x00, 0x00);

        [Category("Margin Settings")]
        [DisplayName("OffScreenColor")]
        [Description("OffScreen Color")]
        public Color OffScreenColor
        {
            get { return offScreenColor; }
            set { offScreenColor = value; }
        }

        private Color visibleColor = Color.FromArgb(0x00, 0xff, 0xff, 0xff);

        [Category("Margin Settings")]
        [DisplayName("VisibleColor")]
        [Description("Visible Color")]
        public Color VisibleColor
        {
            get { return visibleColor; }
            set { visibleColor = value; }
        }

        //private Color caretColor = Colors.Red;

        //[Category("Margin Settings")]
        //[DisplayName("CaretColor")]
        //[Description("My integer option")]
        //public Color CaretColor
        //{
        //    get { return caretColor; }
        //    set { caretColor = value; }
        //}

        //const char DefaultLetter = 'a';

        //public OptionPage()
        //{
        //    SetToDefaults();
        //}

        //[DefaultValue(DefaultLetter)]
        //public char BadLetter
        //{
        //    get;
        //    set;
        //}

        //public override void ResetSettings()
        //{
        //    base.ResetSettings();
        //    SetToDefaults();
        //}

        //private void SetToDefaults()
        //{
        //    this.BadLetter = DefaultLetter;
        //}
    }
}
