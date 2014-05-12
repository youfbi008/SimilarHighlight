using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using SimilarHighlight.OverviewMargin;
using SimilarHighlight.SettingsStore;

namespace SimilarHighlight
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    
    [Name(RightMargin.Name)]
    [MarginContainer(PredefinedOverviewMarginNames.Overview)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    sealed class RightMarginFactory : IWpfTextViewMarginProvider
    {
        [Import(AllowDefault = true)]
        internal ISettingsStore _settingsStore { get; set; }

        //[Export]
        //[Name("CaretAdornmentLayer")]
        //[Order(After = PredefinedAdornmentLayers.Outlining, Before = PredefinedAdornmentLayers.Selection)]
        //internal AdornmentLayerDefinition caretLayerDefinition;

        public bool LoadOption(IEditorOptions options, string optionName)
        {
            if (_settingsStore != null)
            {
                return _settingsStore.LoadOption(options, optionName);
            }
            return false;
        }
        public RightMargin rightMargin;

        /// <summary>
        /// Create an instance of the CaretMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the CaretMargin will be displayed.</param>
        /// <returns>The newly created CaretMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            IOverviewMargin containerMarginAsOverviewMargin = containerMargin as IOverviewMargin;
            if (containerMarginAsOverviewMargin != null)
            {
                //The caret margin needs to know what the constitutes a word, which means using the text structure navigator
                //(since the definition of a word can change based on context).

                //Create the caret margin, passing it a newly instantiated text structure navigator for the view.
                this.rightMargin = new RightMargin(textViewHost, containerMarginAsOverviewMargin.ScrollBar, this);
                return this.rightMargin;
            }
            else
                return null;
        }
    }
}
