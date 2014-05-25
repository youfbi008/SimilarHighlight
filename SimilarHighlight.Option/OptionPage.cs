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
            High = 9,
            Stardard = 8,
            Low = 7
        }

        [Category("Highlight Settings")]
        [DisplayName("SimilarityLevel")]
        [Description("Set the similarity Level.")]
        public SimilarityType SimilarityLevel
        {
            get { return similarityLevel; }
            set { similarityLevel = value; }
        }

        private bool marginEnabled = true;

        [Category("Margin Settings")]
        [DisplayName("MarginEnabled")]
        [Description("Enable the new Margin.")]
        public bool MarginEnabled
        {
            get { return marginEnabled; }
            set { marginEnabled = value; }
        }

        private double marginWidth = 10.0;

        [Category("Margin Settings")]
        [DisplayName("MarginWidth")]
        [Description("the Width of the new margin")]
        public double MarginWidth
        {
            get { return marginWidth; }
            set { marginWidth = value; }
        }

        private Color caretColor = Color.Red;

        [Category("Margin Settings")]
        [DisplayName("CaretColor")]
        [Description("the mark color of current selection in the new margin")]
        public Color CaretColor
        {
            get { return caretColor; }
            set { caretColor = value; }
        }

        private Color matchColor = Color.Blue;

        [Category("Margin Settings")]
        [DisplayName("MatchColor")]
        [Description("the mark color of highlighted elements in the new margin")]
        public Color MatchColor
        {
            get { return matchColor; }
            set { matchColor = value; }
        }

        private Color backScreenColor = Color.FromArgb(0x30, 0x00, 0x00, 0x00);

        [Category("Margin Settings")]
        [DisplayName("BackScreenColor")]
        [Description("the background color of the new margin")]
        public Color BackScreenColor
        {
            get { return backScreenColor; }
            set { backScreenColor = value; }
        }

        private Color scrollColor = Color.FromArgb(0x00, 0xff, 0xff, 0xff);

        [Category("Margin Settings")]
        [DisplayName("ScrollColor")]
        [Description("the scoll color of the new margin")]
        public Color ScrollColor
        {
            get { return scrollColor; }
            set { scrollColor = value; }
        }

        private bool outputEnabled = true;

        [Category("Output Settings")]
        [DisplayName("OutputEnabled")]
        [Description("Enable the Output Window.")]
        public bool OutputEnabled
        {
            get { return outputEnabled; }
            set { outputEnabled = value; }
        }

        public override void ResetSettings()
        {
            base.ResetSettings();
            SetToDefaults();
        }

        private void SetToDefaults()
        {
            this.BackScreenColor = Color.FromArgb(0x30, 0x00, 0x00, 0x00);
        }
    }
}
