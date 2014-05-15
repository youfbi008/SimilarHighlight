using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using SimilarHighlight.ContainerMargin;
using SimilarHighlight.SettingsStore;
using SimilarHighlight.ContainerwMargin;

namespace SimilarHighlight
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(SimilarMargin.Name)]
    [MarginContainer(PredefinedContainerMargin.Container)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    sealed class SimilarMarginFactory : IWpfTextViewMarginProvider
    {
        [Import(AllowDefault = true)]
        internal ISettingsStore _settingsStore { get; set; }

        public bool LoadOption(IEditorOptions options, string optionName)
        {
            if (_settingsStore != null)
            {
                return _settingsStore.LoadOption(options, optionName);
            }
            return false;
        }
        public SimilarMargin similarMargin;

        /// <summary>
        /// Create an instance of the CaretMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the CaretMargin will be displayed.</param>
        /// <returns>The newly created CaretMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            IContainerMargin containerMarginAsContainerMargin = containerMargin as IContainerMargin;
            if (containerMarginAsContainerMargin != null)
            {
                //The caret margin needs to know what the constitutes a word, which means using the text structure navigator
                //(since the definition of a word can change based on context).

                //Create the caret margin, passing it a newly instantiated text structure navigator for the view.
                this.similarMargin = new SimilarMargin(textViewHost, containerMarginAsContainerMargin.ScrollBar, this);
                return this.similarMargin;
            }
            else
                return null;
        }
    }
}
